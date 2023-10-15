using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using System.Threading;
using GONet.Utils;
using System.Collections.Concurrent;
using GONet;

namespace NetcodeIO.NET.Utils.IO
{
	internal interface ISocketContext : IDisposable
	{
        int AvailableToReadCount { get; }
		int BoundPort { get; }
		void Close();
		void Bind(EndPoint endpoint);
		void SendTo(byte[] data, EndPoint remoteEP);
		void SendTo(byte[] data, int length, EndPoint remoteEP);
		void SendToAsync(byte[] data, int length, EndPoint remoteEP);
		bool Read(out Datagram packet);
		void Pump();
	}

	internal class UDPSocketContext : ISocketContext
	{
		public int BoundPort => ((IPEndPoint)internalSocket.LocalEndPoint).Port;

        public int AvailableToReadCount => datagramQueue.Count;

        private Socket internalSocket;
		private Thread readFromSocketThread;
		private volatile bool isReadSocketRunning;

		private DatagramQueue datagramQueue;

		public UDPSocketContext(AddressFamily addressFamily)
		{
			datagramQueue = new DatagramQueue();
			
			internalSocket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);

			{
				// NOTE: @ 10 Hz GONet tick rate, 100 APR characters for 1 client ... server is sending 656 Kbps => 8,400 bytes/tick => 6 packets/tick
				//const int SocketBufferSize = 1024 * 1024; // 1MB TODO make this user configurable
				internalSocket.ReceiveBufferSize = 8400 * 2 * 2; // ~represents a client's incoming needs for 20 Hz server send rate and double that need for safety
				internalSocket.SendBufferSize = 8400 * 2 * 100; // ~represents a server's outgoing needs for 100 clients and therefore 100 APR characters
			}
		}

		public void Bind(EndPoint endpoint)
		{
			internalSocket.Bind(endpoint);

			readFromSocketThread = new Thread(ReadFromSocket_SeparateThread);
			readFromSocketThread.Name = "GONet Socket Reads";
			readFromSocketThread.Priority = ThreadPriority.AboveNormal;
			readFromSocketThread.IsBackground = true; // do not prevent process from exiting when foreground thread(s) end
			readFromSocketThread.Start();
		}

		/// <summary>
		/// IMPORTANT: This is NOT an async send.
		/// </summary>
		public void SendTo(byte[] data, EndPoint remoteEP)
		{
			internalSocket.SendTo(data, remoteEP);
            //GONet.GONetLog.Debug("sending...length[]: " + data.Length);
        }

		/// <summary>
		/// IMPORTANT: This is NOT an async send.
		/// </summary>
		public void SendTo(byte[] data, int length, EndPoint remoteEP)
		{
			internalSocket.SendTo(data, length, SocketFlags.None, remoteEP);
		}

        #region Async send related stuff

        static readonly ConcurrentDictionary<ArrayPool<byte>, ConcurrentQueue<byte[]>> asyncSendBorrowedPayloadsByPool = new ConcurrentDictionary<ArrayPool<byte>, ConcurrentQueue<byte[]>>(3, 3);
		static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> asyncSendPoolByThread = new ConcurrentDictionary<Thread, ArrayPool<byte>>(2, 2);

		static readonly ConcurrentDictionary<ObjectPool<AsyncSendPackaging>, ConcurrentQueue<AsyncSendPackaging>> asyncSendBorrowedPackagingByPool = new ConcurrentDictionary<ObjectPool<AsyncSendPackaging>, ConcurrentQueue<AsyncSendPackaging>>(3, 3);
		static readonly ConcurrentDictionary<Thread, ObjectPool<AsyncSendPackaging>> asyncSendPackagingPoolByThread = new ConcurrentDictionary<Thread, ObjectPool<AsyncSendPackaging>>(2, 2);

		public class AsyncSendPackaging : SocketAsyncEventArgs
		{
			public byte[] payload;
			public int payloadSize;
			public ArrayPool<byte> payloadBorrowedFromPool;
			public ObjectPool<AsyncSendPackaging> thisBorrowedFromPool;

			internal void Init(EndPoint remoteEP, byte[] payload, int payloadSize, ArrayPool<byte> payloadBorrowedFromPool, ObjectPool<AsyncSendPackaging> thisBorrowedFromPool, EventHandler<SocketAsyncEventArgs> onCompleted)
            {
				RemoteEndPoint = remoteEP;
				SetBuffer(payload, 0, payloadSize);

				this.payload = payload;
				this.payloadSize = payloadSize;
				this.payloadBorrowedFromPool = payloadBorrowedFromPool;
				this.thisBorrowedFromPool = thisBorrowedFromPool;

				Completed -= onCompleted; // this ensures we do not just keep adding again and again...not sure how else to ensure only one
				Completed += onCompleted;
            }
        }

		/// <summary>
		/// IMPORTANT: This is an ASYNC send.
		/// </summary>
		public void SendToAsync(byte[] data, int length, EndPoint remoteEP)
		{
			ArrayPool<byte> asyncSendPool;
			if (!asyncSendPoolByThread.TryGetValue(Thread.CurrentThread, out asyncSendPool))
            {
				asyncSendPoolByThread[Thread.CurrentThread] = asyncSendPool = new ArrayPool<byte>(10, 1, 1024 * 4, 1024 * 32);
			}

			ObjectPool<AsyncSendPackaging> asyncSendPackagingPool;
			if (!asyncSendPackagingPoolByThread.TryGetValue(Thread.CurrentThread, out asyncSendPackagingPool))
            {
				asyncSendPackagingPoolByThread[Thread.CurrentThread] = asyncSendPackagingPool = new ObjectPool<AsyncSendPackaging>(50, 2);
            }

			byte[] payload = asyncSendPool.Borrow(length);
			Buffer.BlockCopy(data, 0, payload, 0, length);

			AsyncSendPackaging sendPackaging = asyncSendPackagingPool.Borrow();
			sendPackaging.Init(remoteEP, payload, length, asyncSendPool, asyncSendPackagingPool, OnSendToAsyncComplete);
			
			if (!internalSocket.SendToAsync(sendPackaging))
			{
				GONetLog.Error($"Ran into something possibly serious trying to initiate this SendToAsync call.");
			}

			ReturnBorrowedSendStuffsCompleted(Thread.CurrentThread); // memory management like this is required due to array pools not being multithread capable and we send and endSend on different threads

			//GONet.GONetLog.Debug("sending...length: " + length);
		}

		private void ReturnBorrowedSendStuffsCompleted(Thread onlyProcessForThread)
        {
			ArrayPool<byte> borrowedFromPool = asyncSendPoolByThread[onlyProcessForThread];
			ConcurrentQueue<byte[]> borrowedPayloads;
			if (asyncSendBorrowedPayloadsByPool.TryGetValue(borrowedFromPool, out borrowedPayloads))
			{
				int arrayCount = borrowedPayloads.Count;
				int returnedCount = 0;
				byte[] borrowedPayload;
				while (returnedCount < arrayCount && borrowedPayloads.TryDequeue(out borrowedPayload))
				{
					borrowedFromPool.Return(borrowedPayload);
					++returnedCount;
				}
			}

            ObjectPool<AsyncSendPackaging> asyncSendPackagingPool = asyncSendPackagingPoolByThread[onlyProcessForThread];
			ConcurrentQueue<AsyncSendPackaging> borrowedPackagings;
			if (asyncSendBorrowedPackagingByPool.TryGetValue(asyncSendPackagingPool, out borrowedPackagings))
			{
				int arrayCount = borrowedPackagings.Count;
				int returnedCount = 0;
				AsyncSendPackaging borrowedPackaging;
				while (returnedCount < arrayCount && borrowedPackagings.TryDequeue(out borrowedPackaging))
				{
					asyncSendPackagingPool.Return(borrowedPackaging);
					++returnedCount;
				}
			}

		}

		private void OnSendToAsyncComplete(object sender, SocketAsyncEventArgs sendToAsyncArgs)
		{
			if (sendToAsyncArgs.SocketError == SocketError.Success)
			{
				if (sendToAsyncArgs.LastOperation == SocketAsyncOperation.SendTo)
				{
					AsyncSendPackaging sendPackaging = (AsyncSendPackaging)sendToAsyncArgs;
					int bytesSentCount = sendPackaging.BytesTransferred;

					{ // the memory management required due to running stuff on differnet threads and array pools are not multithred capable
						ConcurrentQueue<byte[]> borrowedPayloads;
						if (!asyncSendBorrowedPayloadsByPool.TryGetValue(sendPackaging.payloadBorrowedFromPool, out borrowedPayloads))
						{
							asyncSendBorrowedPayloadsByPool[sendPackaging.payloadBorrowedFromPool] = borrowedPayloads = new ConcurrentQueue<byte[]>();
						}
						borrowedPayloads.Enqueue(sendPackaging.payload);
					}

					if (bytesSentCount != sendPackaging.payloadSize)
					{
						GONetLog.Warning($"Call to BeginSendTo did not end up sending all the payload of size: {sendPackaging.payloadSize} and instead only sent size: {bytesSentCount}");
					}

					{ // the memory management required due to running stuff on differnet threads and array pools are not multithred capable
						ConcurrentQueue<AsyncSendPackaging> borrowedPackagings;
						if (!asyncSendBorrowedPackagingByPool.TryGetValue(sendPackaging.thisBorrowedFromPool, out borrowedPackagings))
						{
							asyncSendBorrowedPackagingByPool[sendPackaging.thisBorrowedFromPool] = borrowedPackagings = new ConcurrentQueue<AsyncSendPackaging>();
						}
						borrowedPackagings.Enqueue(sendPackaging);
					}
				}
			}
			else
			{
				throw new SocketException((int)sendToAsyncArgs.SocketError);
			}
		}

		private void OnBeginSendToComplete(IAsyncResult beginSendToResult)
        {
            try
            {
			}
			catch (ObjectDisposedException ode)
            {
				GONetLog.Info($"Trying to end the BeginSendTo call.  This is most likely happening as a result of a normal shutdown procedure. Message: {ode.Message}");
			}
			catch (Exception e)
            {
				GONetLog.Error($"Ran into something possibly serious trying to wrap up this BeginSendTo call.  Exception Type: {e.GetType().FullName} Message: {e.Message}");
            }
        }

        #endregion

        public void Pump()
		{
		}

		public bool Read(out Datagram packet)
		{
			if (datagramQueue.Count > 0)
			{
				return datagramQueue.TryDequeue(out packet);
			}

			packet = default;
			return false;
		}

		public void Close()
		{
			internalSocket.Close();
			isReadSocketRunning = false;
		}

		public void Dispose()
		{
			Close();
		}

		private void ReadFromSocket_SeparateThread()
		{
			isReadSocketRunning = true;

			/*
			const int ACTIVE_RECEIVE_THREADS = 5; // TODO promote up to some user configration setting
			datagramQueue.BeginReceiving(internalSocket, ACTIVE_RECEIVE_THREADS);
			
			while (isReadSocketRunning)
			{
				datagramQueue.ReturnBorrowedReceivePayloadsCompleted(Thread.CurrentThread);

				Thread.Sleep(5);
			}
			*/

			//*
			while (isReadSocketRunning)
            {
				try
                {
					datagramQueue.ReadFrom(internalSocket);
				}
				catch
				{
				}
			}
			//*/
		}
	}

	internal class NetworkSimulatorSocketManager
	{
		public int LatencyMS = 0;
		public int JitterMS = 0;
		public int PacketLossChance = 0;
		public int DuplicatePacketChance = 0;

		public bool AutoTime = true;

		public double Time
		{
			get
			{
				if (AutoTime)
					return DateTime.UtcNow.GetTotalSeconds();
				else
					return time;
			}
		}

		private Dictionary<EndPoint, NetworkSimulatorSocketContext> sockets = new Dictionary<EndPoint, NetworkSimulatorSocketContext>();
		private double time;

		public void Update(double time)
		{
			this.time = time;
		}

		public NetworkSimulatorSocketContext CreateContext(EndPoint endpoint)
		{
			var socket = new NetworkSimulatorSocketContext();
			socket.Manager = this;

			return socket;
		}

		public void ChangeContext(NetworkSimulatorSocketContext socket, EndPoint endpoint)
		{
			if (sockets.ContainsKey(endpoint))
				throw new SocketException();

			sockets.Add(endpoint, socket);
		}

		public void RemoveContext(EndPoint endpoint)
		{
			sockets.Remove(endpoint);
		}

		public NetworkSimulatorSocketContext FindContext(EndPoint endpoint)
		{
			if (!sockets.ContainsKey(endpoint))
				return null;

			return sockets[endpoint];
		}
	}

	internal class NetworkSimulatorSocketContext : ISocketContext
	{
		public int BoundPort => ((IPEndPoint)endpoint).Port;

        public int AvailableToReadCount => datagramQueue.Count;

        private struct simulatedPacket
		{
			public double receiveTime;
			public byte[] packetData;
			public EndPoint sender;
		}

		public NetworkSimulatorSocketManager Manager;

		private EndPoint endpoint;
		private List<simulatedPacket> simulatedPackets = new List<simulatedPacket>();
		private DatagramQueue datagramQueue = new DatagramQueue();
		private object mutex = new object();

		private Random rand = new Random();
		private bool running = false;

		public void Bind(EndPoint endpoint)
		{
			if (this.endpoint != null && this.endpoint.Equals(endpoint)) return;

			this.running = true;
			this.endpoint = endpoint;

			Manager.ChangeContext(this, this.endpoint);
		}

		public void SimulateReceive(byte[] packetData, EndPoint sender)
		{
			if (!running) return;

			double receiveTime = Manager.Time;

			// add latency+jitter to receive time
			receiveTime += (Manager.LatencyMS / 1000.0) + (rand.Next(-Manager.JitterMS, Manager.JitterMS) / 2000.0);

			lock (mutex)
			{
				simulatedPackets.Add(new simulatedPacket()
				{
					receiveTime = receiveTime,
					packetData = packetData,
					sender = sender
				});
			}
		}

		public void SendTo(byte[] data, EndPoint remoteEP)
		{
			if (!running) throw new SocketException();

			// randomly drop packets
			if (rand.Next(100) < Manager.PacketLossChance)
			{
				return;
			}

			byte[] temp = new byte[data.Length];
			Buffer.BlockCopy(data, 0, temp, 0, data.Length);

			var endSocket = Manager.FindContext(remoteEP);

			if (endSocket != null)
			{
				endSocket.SimulateReceive(temp, this.endpoint);

				// randomly duplicate packets
				if (rand.Next(100) < Manager.DuplicatePacketChance)
					endSocket.SimulateReceive(temp, this.endpoint);
			}
		}

		/// <summary>
		/// IMPORTANT: I am a lier and I am not ASYNC sending.
		/// </summary>
		public void SendToAsync(byte[] data, int length, EndPoint remoteEP)
		{
			SendTo(data, length, remoteEP);
		}

		public void SendTo(byte[] data, int length, EndPoint remoteEP)
        {
			if (!running) throw new SocketException();

			byte[] temp = new byte[length];
			Buffer.BlockCopy(data, 0, temp, 0, length);

			SendTo(temp, remoteEP);
		}

		public void Pump()
		{
			if (simulatedPackets.Count > 0)
			{
				lock (simulatedPackets)
				{
					// enqueue packets ready to be received
					for (int i = 0; i < simulatedPackets.Count; i++)
					{
						if (Manager.Time >= simulatedPackets[i].receiveTime)
						{
							var receivePacket = simulatedPackets[i];
							simulatedPackets.RemoveAt(i);

							byte[] receiveBuffer = BufferPool.GetBuffer(2048);
							Buffer.BlockCopy(receivePacket.packetData, 0, receiveBuffer, 0, receivePacket.packetData.Length);

							Datagram datagram = new Datagram();
							datagram.payload = receiveBuffer;
							datagram.payloadSize = receivePacket.packetData.Length;
							datagram.sender = receivePacket.sender;
							datagramQueue.Enqueue(datagram);
						}
					}
				}
			}
		}

		public bool Read(out Datagram packet)
		{
			if (datagramQueue.Count > 0)
			{
				return datagramQueue.TryDequeue(out packet);
			}

			packet = new Datagram();
			return false;
		}

		public void Close()
		{
			running = false;
			Manager.RemoveContext(this.endpoint);
		}

		public void Dispose()
		{
			Close();
		}
	}
}
