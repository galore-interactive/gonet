using GONet;
using GONet.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace NetcodeIO.NET.Utils
{
	internal struct Datagram
	{
		public byte[] payload;
		public int payloadSize;
		public EndPoint sender;

		public void Release()
		{
			BufferPool.ReturnBuffer(payload);
		}
	}

	internal class DatagramQueue
	{
		protected readonly ConcurrentQueue<Datagram> datagramQueue = new ConcurrentQueue<Datagram>();
		protected readonly Queue<EndPoint> endpointPool = new Queue<EndPoint>();

		public int Count => datagramQueue.Count;

		public void Clear()
		{
            while (!datagramQueue.IsEmpty)
            {
                Datagram item;
                datagramQueue.TryDequeue(out item);
            }

			endpointPool.Clear();
		}

        static readonly ConcurrentDictionary<ArrayPool<byte>, ConcurrentQueue<byte[]>> asyncReceiveBorrowedPayloadsByPool = new ConcurrentDictionary<ArrayPool<byte>, ConcurrentQueue<byte[]>>(3, 3);
        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> asyncReceivePoolByThread = new ConcurrentDictionary<Thread, ArrayPool<byte>>(2, 2);

        static readonly ArrayPool<byte> asyncReceivePool = new ArrayPool<byte>(10, 1, 1024 * 4, 1024 * 32);
        struct AsyncReceivePackaging // TODO FIXME this has to turn into a class just like AsyncSendPackaging because passing an instance into BeginReceiveFrom will box into object and put on heap!!! GC beyotch!
        {
            public byte[] payload;
            public byte[] payload_transient;
            public ArrayPool<byte> payloadBorrowedFromPool;
            public Socket socket;
            public EndPoint sender;
        }

        public void BeginReceiving(Socket socket, int activeReceiveThreads)
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);

            for (int i = 0; i < activeReceiveThreads; ++i)
            {
                const int LENGTH = 1024 * 2;
                byte[] payload = BufferPool.GetBuffer(LENGTH);
                byte[] payload_transient = asyncReceivePool.Borrow(LENGTH);

                var receivePackaging = new AsyncReceivePackaging()
                { 
                    payload = payload, 
                    payload_transient = payload_transient,
                    payloadBorrowedFromPool = asyncReceivePool,
                    socket = socket,
                    sender = sender
                };

                socket.BeginReceiveFrom(receivePackaging.payload_transient, 0, receivePackaging.payload_transient.Length, SocketFlags.None, ref sender, OnBeginReceiveFromComplete, receivePackaging);
            }
        }

        internal void ReturnBorrowedReceivePayloadsCompleted(Thread onlyProcessForThread)
        {
            ArrayPool<byte> borrowedFromPool;
            if (asyncReceivePoolByThread.TryGetValue(onlyProcessForThread, out borrowedFromPool))
            {
                ConcurrentQueue<byte[]> borrowedPayloads;
                if (asyncReceiveBorrowedPayloadsByPool.TryGetValue(borrowedFromPool, out borrowedPayloads))
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
            }
        }

        private void OnBeginReceiveFromComplete(IAsyncResult beginReceiveFromResult)
        {
            AsyncReceivePackaging receivePackaging = (AsyncReceivePackaging)beginReceiveFromResult.AsyncState;
            try
            {
                int recv = receivePackaging.socket.EndReceiveFrom(beginReceiveFromResult, ref receivePackaging.sender);

                if (recv > 0)
                {
                    Buffer.BlockCopy(receivePackaging.payload_transient, 0, receivePackaging.payload, 0, recv);

                    //GONet.GONetLog.Debug("receiving...length: " + recv);
                    Datagram packet = new Datagram();
                    packet.sender = receivePackaging.sender;
                    packet.payload = receivePackaging.payload;
                    packet.payloadSize = recv;

                    datagramQueue.Enqueue(packet);
                }

                { // the memory management required due to running stuff on differnet threads and array pools are not multithred capable
                    ConcurrentQueue<byte[]> borrowedPayloads;
                    if (!asyncReceiveBorrowedPayloadsByPool.TryGetValue(receivePackaging.payloadBorrowedFromPool, out borrowedPayloads))
                    {
                        asyncReceiveBorrowedPayloadsByPool[receivePackaging.payloadBorrowedFromPool] = borrowedPayloads = new ConcurrentQueue<byte[]>();
                    }
                    borrowedPayloads.Enqueue(receivePackaging.payload_transient);
                }
            }
            catch (ObjectDisposedException ode)
            {
                GONetLog.Info($"Trying to end the BeginSendTo call.  This is most likely happening as a result of a normal shutdown procedure. Message: {ode.Message}");
            }
            catch (Exception e)
            {
                GONetLog.Error($"Ran into something possibly serious trying to wrap up this BeginSendTo call.  Exception Type: {e.GetType().FullName} Message: {e.Message}\nStacTrace: {e.StackTrace}");
            }
            finally
            {
                BeginReceiving(receivePackaging.socket, 1); // start the async receive process over again for this "slot"
            }
        }

        internal void ReadFrom(Socket internalSocket)
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            byte[] receiveBuffer = BufferPool.GetBuffer(2048);
            int recv = internalSocket.ReceiveFrom(receiveBuffer, ref sender);
            if (recv > 0)
            {
                //GONet.GONetLog.Debug("receiving...length: " + recv);
                Datagram packet = new Datagram();
                packet.sender = sender;
                packet.payload = receiveBuffer;
                packet.payloadSize = recv;

                datagramQueue.Enqueue(packet);
            }
        }

        /* TODO see if I need to get into this cut/paste from C# source to modify for higher performance and reduced the crazy amount of GC that occur in here!!!
		public unsafe void ReadFrom( Socket socket )
		{
			EndPoint sender;

			lock (endpoint_mutex)
			{
				if (endpointPool.Count > 0)
					sender = endpointPool.Dequeue();
				else
					sender = new IPEndPoint(IPAddress.Any, 0);
			}

			byte[] receiveBuffer = BufferPool.GetBuffer(2048);

			EndPoint endPointSnapshot = sender;
			SocketAddress socketAddress = SnapshotAndSerialize(ref endPointSnapshot, socket);

			fixed (byte* pinnedBuffer = receiveBuffer)
			{
				int recv = recvfrom(socket.Handle, pinnedBuffer, receiveBuffer.Length, SocketFlags.None, socketAddress.m_Buffer, ref socketAddress.m_Size);
				//socket.ReceiveFrom(receiveBuffer, ref sender);

				if (recv > 0)
				{
					//GONet.GONetLog.Debug("receiving...length: " + recv);
					Datagram packet = new Datagram();
					packet.sender = sender;
					packet.payload = receiveBuffer;
					packet.payloadSize = recv;

					lock (datagram_mutex)
						datagramQueue.Enqueue(packet);
				}

			}
		}

        // a little perf app measured these times when comparing the internal
        // buffer implemented as a managed byte[] or unmanaged memory IntPtr
        // that's why we use byte[]
        // byte[] total ms:19656
        // IntPtr total ms:25671

        /// <devdoc>
        ///    <para>
        ///       This class is used when subclassing EndPoint, and provides indication
        ///       on how to format the memeory buffers that winsock uses for network addresses.
        ///    </para>
        /// </devdoc>
        public class SocketAddress
        {

            internal const int IPv6AddressSize = 28;
            internal const int IPv4AddressSize = 16;

            internal int m_Size;
            internal byte[] m_Buffer;

            private const int WriteableOffset = 2;
            private const int MaxSize = 32; // IrDA requires 32 bytes
            private bool m_changed = true;
            private int m_hash;

            //
            // Address Family
            //
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public AddressFamily Family
            {
                get
                {
                    int family;
#if BIGENDIAN
                family = ((int)m_Buffer[0]<<8) | m_Buffer[1];
#else
                    family = m_Buffer[0] | ((int)m_Buffer[1] << 8);
#endif
                    return (AddressFamily)family;
                }
            }
            //
            // Size of this SocketAddress
            //
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public int Size
            {
                get
                {
                    return m_Size;
                }
            }

            //
            // access to unmanaged serialized data. this doesn't
            // allow access to the first 2 bytes of unmanaged memory
            // that are supposed to contain the address family which
            // is readonly.
            //
            // <SECREVIEW> you can still use negative offsets as a back door in case
            // winsock changes the way it uses SOCKADDR. maybe we want to prohibit it?
            // maybe we should make the class sealed to avoid potentially dangerous calls
            // into winsock with unproperly formatted data? </SECREVIEW>
            //
            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public byte this[int offset]
            {
                get
                {
                    //
                    // access
                    //
                    if (offset < 0 || offset >= Size)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    return m_Buffer[offset];
                }
                set
                {
                    if (offset < 0 || offset >= Size)
                    {
                        throw new IndexOutOfRangeException();
                    }
                    if (m_Buffer[offset] != value)
                    {
                        m_changed = true;
                    }
                    m_Buffer[offset] = value;
                }
            }

            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public SocketAddress(AddressFamily family) : this(family, MaxSize)
            {
            }

            /// <devdoc>
            ///    <para>[To be supplied.]</para>
            /// </devdoc>
            public SocketAddress(AddressFamily family, int size)
            {
                if (size < WriteableOffset)
                {
                    //
                    // it doesn't make sense to create a socket address with less tha
                    // 2 bytes, that's where we store the address family.
                    //
                    throw new ArgumentOutOfRangeException("size");
                }
                m_Size = size;
                m_Buffer = new byte[(size / IntPtr.Size + 2) * IntPtr.Size];//sizeof DWORD

#if BIGENDIAN
            m_Buffer[0] = unchecked((byte)((int)family>>8));
            m_Buffer[1] = unchecked((byte)((int)family   ));
#else
                m_Buffer[0] = unchecked((byte)((int)family));
                m_Buffer[1] = unchecked((byte)((int)family >> 8));
#endif
            }

            internal SocketAddress(IPAddress ipAddress)
                : this(ipAddress.AddressFamily,
                    ((ipAddress.AddressFamily == AddressFamily.InterNetwork) ? IPv4AddressSize : IPv6AddressSize))
            {

                // No Port
                m_Buffer[2] = (byte)0;
                m_Buffer[3] = (byte)0;

                if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    // No handling for Flow Information
                    m_Buffer[4] = (byte)0;
                    m_Buffer[5] = (byte)0;
                    m_Buffer[6] = (byte)0;
                    m_Buffer[7] = (byte)0;

                    // Scope serialization
                    long scope = ipAddress.ScopeId;
                    m_Buffer[24] = (byte)scope;
                    m_Buffer[25] = (byte)(scope >> 8);
                    m_Buffer[26] = (byte)(scope >> 16);
                    m_Buffer[27] = (byte)(scope >> 24);

                    // Address serialization
                    byte[] addressBytes = ipAddress.GetAddressBytes();
                    for (int i = 0; i < addressBytes.Length; i++)
                    {
                        m_Buffer[8 + i] = addressBytes[i];
                    }
                }
                else
                {
                    // IPv4 Address serialization
                    m_Buffer[4] = unchecked((byte)(ipAddress.Address));
                    m_Buffer[5] = unchecked((byte)(ipAddress.Address >> 8));
                    m_Buffer[6] = unchecked((byte)(ipAddress.Address >> 16));
                    m_Buffer[7] = unchecked((byte)(ipAddress.Address >> 24));
                }
            }

            internal SocketAddress(IPAddress ipaddress, int port)
                : this(ipaddress)
            {
                m_Buffer[2] = (byte)(port >> 8);
                m_Buffer[3] = (byte)port;
            }
            internal const int IPv4AddressBytes = 4;
            internal const int IPv6AddressBytes = 16;
            internal IPAddress GetIPAddress()
            {
                if (Family == AddressFamily.InterNetworkV6)
                {
                    Contract.Assert(Size >= IPv6AddressSize);

                    byte[] address = new byte[IPv6AddressBytes];
                    for (int i = 0; i < address.Length; i++)
                    {
                        address[i] = m_Buffer[i + 8];
                    }

                    long scope = (long)((m_Buffer[27] << 24) +
                                        (m_Buffer[26] << 16) +
                                        (m_Buffer[25] << 8) +
                                        (m_Buffer[24]));

                    return new IPAddress(address, scope);

                }
                else if (Family == AddressFamily.InterNetwork)
                {
                    Contract.Assert(Size >= IPv4AddressSize);

                    long address = (long)(
                            (m_Buffer[4] & 0x000000FF) |
                            (m_Buffer[5] << 8 & 0x0000FF00) |
                            (m_Buffer[6] << 16 & 0x00FF0000) |
                            (m_Buffer[7] << 24)
                            ) & 0x00000000FFFFFFFF;

                    return new IPAddress(address);

                }
                else
                {
                    throw new SocketException((int)SocketError.AddressFamilyNotSupported);
                }
            }
            internal IPEndPoint GetIPEndPoint()
            {
                IPAddress address = GetIPAddress();
                int port = (int)((m_Buffer[2] << 8 & 0xFF00) | (m_Buffer[3]));
                return new IPEndPoint(address, port);
            }

            //
            // For ReceiveFrom we need to pin address size, using reserved m_Buffer space
            //
            internal void CopyAddressSizeIntoBuffer()
            {
                m_Buffer[m_Buffer.Length - IntPtr.Size] = unchecked((byte)(m_Size));
                m_Buffer[m_Buffer.Length - IntPtr.Size + 1] = unchecked((byte)(m_Size >> 8));
                m_Buffer[m_Buffer.Length - IntPtr.Size + 2] = unchecked((byte)(m_Size >> 16));
                m_Buffer[m_Buffer.Length - IntPtr.Size + 3] = unchecked((byte)(m_Size >> 24));
            }
            //
            // Can be called after the above method did work
            //
            internal int GetAddressSizeOffset()
            {
                return m_Buffer.Length - IntPtr.Size;
            }
            //
            //
            // For ReceiveFrom we need to update the address size upon IO return
            //
            internal unsafe void SetSize(IntPtr ptr)
            {
                // Apparently it must be less or equal the original value since ReceiveFrom cannot reallocate the address buffer
                m_Size = *(int*)ptr;
            }
            public override bool Equals(object comparand)
            {
                SocketAddress castedComparand = comparand as SocketAddress;
                if (castedComparand == null || this.Size != castedComparand.Size)
                {
                    return false;
                }
                for (int i = 0; i < this.Size; i++)
                {
                    if (this[i] != castedComparand[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public override int GetHashCode()
            {
                if (m_changed)
                {
                    m_changed = false;
                    m_hash = 0;

                    int i;
                    int size = Size & ~3;

                    for (i = 0; i < size; i += 4)
                    {
                        m_hash ^= (int)m_Buffer[i]
                                | ((int)m_Buffer[i + 1] << 8)
                                | ((int)m_Buffer[i + 2] << 16)
                                | ((int)m_Buffer[i + 3] << 24);
                    }
                    if ((Size & 3) != 0)
                    {

                        int remnant = 0;
                        int shift = 0;

                        for (; i < Size; ++i)
                        {
                            remnant |= ((int)m_Buffer[i]) << shift;
                            shift += 8;
                        }
                        m_hash ^= remnant;
                    }
                }
                return m_hash;
            }

            public override string ToString()
            {
                StringBuilder bytes = new StringBuilder();
                for (int i = WriteableOffset; i < this.Size; i++)
                {
                    if (i > WriteableOffset)
                    {
                        bytes.Append(",");
                    }
                    bytes.Append(this[i].ToString(NumberFormatInfo.InvariantInfo));
                }
                return Family.ToString() + ":" + Size.ToString(NumberFormatInfo.InvariantInfo) + ":{" + bytes.ToString() + "}";
            }

        } // class SocketAddress

        private SocketAddress SnapshotAndSerialize(ref EndPoint remoteEP, Socket socket)
		{
			IPEndPoint ipSnapshot = remoteEP as IPEndPoint;

			if (ipSnapshot != null)
			{
				ipSnapshot = new IPEndPoint(Snapshot(ipSnapshot.Address), ipSnapshot.Port);
				remoteEP = RemapIPEndPoint(ipSnapshot, socket);
			}

            var bob = remoteEP.Serialize();
            return Unsafe.As<System.Net.SocketAddress, SocketAddress>(ref bob);
		}
		// DualMode: Automatically re-map IPv4 addresses to IPv6 addresses
		private IPEndPoint RemapIPEndPoint(IPEndPoint input, Socket socket)
		{
			if (input.AddressFamily == AddressFamily.InterNetwork && socket.DualMode)
			{
				return new IPEndPoint(input.Address.MapToIPv6(), input.Port);
			}
			return input;
		}
		internal IPAddress Snapshot(IPAddress inn)
		{
			switch (inn.AddressFamily)
			{
				case AddressFamily.InterNetwork:
					return new IPAddress(inn.Address);
			}

			throw new Exception("Internal");
		}

#if !FEATURE_PAL
		private const string WS2_32 = "ws2_32.dll";
#else
        private const string WS2_32 = ExternDll.Kernel32; // Resolves to rotor_pal
#endif // !FEATURE_PAL

		// This method is always blocking, so it uses an IntPtr.
		[DllImport(WS2_32, SetLastError = true)]
		internal unsafe static extern int recvfrom(
										 [In] IntPtr socketHandle,
										 [In] byte* pinnedBuffer,
										 [In] int len,
										 [In] SocketFlags socketFlags,
										 [Out] byte[] socketAddress,
										 [In, Out] ref int socketAddressSize
										 );
        */
        public void Enqueue(Datagram datagram)
		{
			datagramQueue.Enqueue(datagram);
		}

		public bool TryDequeue(out Datagram item)
		{
			return datagramQueue.TryDequeue(out item);
		}
	}
}
