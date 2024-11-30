using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

using NetcodeIO.NET.Utils;
using NetcodeIO.NET.Utils.IO;
using NetcodeIO.NET.Internal;
using GONet;

namespace NetcodeIO.NET
{
	/// <summary>
	/// Represents a remote client connected to a server
	/// </summary>
	public class RemoteClient
	{
		/// <summary>
		/// The unique ID of the client as assigned by the token server
		/// </summary>
		public ulong ClientID;

		/// <summary>
		/// The index as assigned by the server
		/// </summary>
		public uint ClientIndex;

		/// <summary>
		/// The remote endpoint of the client
		/// </summary>
		public EndPoint RemoteEndpoint;

		/// <summary>
		/// 256 bytes of arbitrary user data
		/// </summary>
		public byte[] UserData;

		internal bool Connected;
		internal bool Confirmed;

		internal NetcodeReplayProtection replayProtection;
		internal Server server;

		internal double lastResponseAtSeconds;
		internal int timeoutSeconds;

		public RemoteClient(Server server)
		{
			this.server = server;
		}

		/// <summary>
		/// Send a payload to this client
		/// </summary>
		public void SendPayload(byte[] payload, int payloadSize)
		{
			server.SendPayload(this, payload, payloadSize);
		}

		internal void Touch(double currentSeconds)
		{
			lastResponseAtSeconds = currentSeconds;
		}
	}

	/// <summary>
	/// Event handler for when a client connects to the server
	/// </summary>
	public delegate void RemoteClientConnectedEventHandler(RemoteClient client);

	/// <summary>
	/// Event handler for when a client disconnects from the server
	/// </summary>
	public delegate void RemoteClientDisconnectedEventHandler(RemoteClient client);

	/// <summary>
	/// Event handler for when payload packets are received from a connected client
	/// </summary>
	public delegate void RemoteClientMessageReceivedEventHandler(RemoteClient sender, byte[] payload, int payloadSize);

	/// <summary>
	/// Class for starting a Netcode.IO server and accepting connections from remote clients
	/// </summary>
	public sealed class Server
	{
		#region embedded types

		private struct usedConnectToken
		{
			public byte[] mac;
			public EndPoint endpoint;
			public double time;
		}

		#endregion

		#region Public fields/properties

		/// <summary>
		/// Event triggered when a remote client connects
		/// </summary>
		public event RemoteClientConnectedEventHandler OnClientConnected;

		/// <summary>
		/// Event triggered when a remote client disconnects
		/// </summary>
		public event RemoteClientDisconnectedEventHandler OnClientDisconnected;

		/// <summary>
		/// Event triggered when a payload is received from a remote client
		/// </summary>
		public event RemoteClientMessageReceivedEventHandler OnClientMessageReceived;

		/// <summary>
		/// Log level for messages
		/// </summary>
		public NetcodeLogLevel LogLevel = NetcodeLogLevel.Debug;

		/// <summary>
		/// Gets the port this server is listening on (or -1 if not listening)
		/// </summary>
		public int Port
		{
			get
			{
				if (listenSocket == null)
					return -1;

				return listenSocket.BoundPort;
			}
		}

        private int tickrate;
        /// <summary>
        /// Gets or sets the internal tickrate of the server in ticks per second. Value must be between 1 and 1000.
        /// </summary>
        public int Tickrate
		{
			get { return tickrate; }
			set
			{
				if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(Tickrate));
				tickrate = value;
			}
		}

		/// <summary>
		/// Gets the current number of connected clients
		/// </summary>
		public int NumConnectedClients
		{
			get
			{
				int connectedClients = 0;
				for (int i = 0; i < clientSlots.Length; i++)
					if (clientSlots[i] != null && clientSlots[i].Confirmed) connectedClients++;

				return connectedClients;
			}
		}

		#endregion

		#region Private fields

		internal bool debugIgnoreConnectionRequest = false;
		internal bool debugIgnoreChallengeResponse = false;

		private ISocketContext listenSocket;
		private IPEndPoint listenEndpoint;

		internal bool IsRunning => isRunning;
		private bool isRunning = false;

		private ulong protocolID;

		private RemoteClient[] clientSlots;
		private int maxSlots;

		private usedConnectToken[] connectTokenHistory;
		private int maxConnectTokenEntries;

		private ulong nextSequenceNumber = 0;
		private ulong nextChallengeSequenceNumber = 0;

		private byte[] privateKey;
		private byte[] challengeKey;

		private EncryptionManager encryptionManager;

		double _totalSeconds;
		internal double totalSeconds { get => _totalSeconds; set { _totalSeconds = value; /*GONet.GONetLog.Debug("totalSeconds: " + totalSeconds);*/ } }

        private bool disposed = false;

		#endregion

		public Server(int maxSlots, string address, int port, ulong protocolID, byte[] privateKey)
		{
			this.tickrate = 60;

			this.maxSlots = maxSlots;
			this.maxConnectTokenEntries = this.maxSlots * 8;
			this.connectTokenHistory = new usedConnectToken[this.maxConnectTokenEntries];
			initConnectTokenHistory();

			this.clientSlots = new RemoteClient[maxSlots];
			this.encryptionManager = new EncryptionManager(maxSlots);

			this.listenEndpoint = new IPEndPoint(IPAddress.Parse(address), port);

			if (this.listenEndpoint.AddressFamily == AddressFamily.InterNetwork)
				this.listenSocket = new UDPSocketContext(AddressFamily.InterNetwork);
			else
				this.listenSocket = new UDPSocketContext(AddressFamily.InterNetworkV6);

			this.protocolID = protocolID;

			this.privateKey = privateKey;

			// generate a random challenge key
			this.challengeKey = new byte[32];
			KeyUtils.GenerateKey(this.challengeKey);
		}

		internal Server(ISocketContext socketContext, int maxSlots, string address, int port, ulong protocolID, byte[] privateKey)
		{
			this.tickrate = 60;

			this.maxSlots = maxSlots;
			this.maxConnectTokenEntries = this.maxSlots * 8;
			this.connectTokenHistory = new usedConnectToken[this.maxConnectTokenEntries];
			initConnectTokenHistory();

			this.clientSlots = new RemoteClient[maxSlots];
			this.encryptionManager = new EncryptionManager(maxSlots);

			this.listenEndpoint = new IPEndPoint(IPAddress.Parse(address), port);

			this.listenSocket = socketContext;

			this.protocolID = protocolID;

			this.privateKey = privateKey;

			// generate a random challenge key
			this.challengeKey = new byte[32];
			KeyUtils.GenerateKey(this.challengeKey);
		}

		#region Public Methods

		/// <summary>
		/// Start the server and listen for incoming connections
		/// </summary>
		public void Start(int tickHertz)
		{
            Tickrate = tickHertz;
			Start(true);
		}

		internal void Start(bool autoTick)
		{
			if (disposed) throw new InvalidOperationException("Can't restart disposed server, please create a new server");

			resetConnectTokenHistory();

			IPEndPoint any = new IPEndPoint(IPAddress.Any, this.listenEndpoint.Port);
			this.listenSocket.Bind(any);
			isRunning = true;

			if (autoTick)
			{
				this.totalSeconds = DateTime.UtcNow.GetTotalSeconds();

				Thread tickThread = new Thread(serverTick_SeparateThread);
				tickThread.Name = "GONet Server Tick";
				tickThread.Priority = ThreadPriority.AboveNormal;
				tickThread.IsBackground = true; // do not prevent process from exiting when foreground thread(s) end
				tickThread.Start();
			}
		}

		/// <summary>
		/// Stop the server and disconnect any clients
		/// </summary>
		public void Stop()
		{
			disposed = true;

			disconnectAll();
			isRunning = false;
			this.listenSocket.Close();

			if (OnClientConnected != null)
			{
				foreach (var receiver in OnClientConnected.GetInvocationList())
					OnClientConnected -= (RemoteClientConnectedEventHandler)receiver;
			}

			if (OnClientDisconnected != null)
			{
				foreach (var receiver in OnClientDisconnected.GetInvocationList())
					OnClientDisconnected -= (RemoteClientDisconnectedEventHandler)receiver;
			}

			if (OnClientMessageReceived != null)
			{
				foreach (var receiver in OnClientMessageReceived.GetInvocationList())
					OnClientMessageReceived -= (RemoteClientMessageReceivedEventHandler)receiver;
			}
		}

		/// <summary>
		/// Send a payload to the remote client
		/// </summary>
		public void SendPayload(RemoteClient client, byte[] payload, int payloadSize)
		{
			sendPayloadToClient(client, payload, payloadSize);
		}

		/// <summary>
		/// Disconnect the remote client
		/// </summary>
		public void Disconnect(RemoteClient client)
		{
			disconnectClient(client);
		}

		#endregion

		public delegate void Nathaniel();
		public event Nathaniel TickBeginning;

		#region Core

		double secondsSinceLastKeepAlive = 0.0;
		internal void Tick(double newTotalSeconds)
		{
			TickBeginning?.Invoke();

			listenSocket.Pump();

			double deltaSeconds = newTotalSeconds - totalSeconds;
			totalSeconds = newTotalSeconds;

			// send keep alive to clients 10 times per second
			secondsSinceLastKeepAlive += deltaSeconds;
			while (secondsSinceLastKeepAlive >= 0.1)
			{
				secondsSinceLastKeepAlive -= 0.1;
				for (int i = 0; i < clientSlots.Length; i++)
				{
					if (clientSlots[i] != null)
					{
						sendKeepAlive(clientSlots[i]);
					}
				}
			}

			// disconnect any clients which have not responded for timeout seconds
			for (int i = 0; i < clientSlots.Length; i++)
			{
				if (clientSlots[i] == null) continue;

				double timeRemaining = newTotalSeconds - clientSlots[i].lastResponseAtSeconds;

				// timeout < 0 disables timeouts
				if (clientSlots[i].timeoutSeconds >= 0 &&
					(newTotalSeconds - clientSlots[i].lastResponseAtSeconds) >= clientSlots[i].timeoutSeconds)
				{
					OnClientDisconnected?.Invoke(clientSlots[i]);

					log("Client {0} timed out", NetcodeLogLevel.Debug, clientSlots[i].RemoteEndpoint.ToString());
					disconnectClient(clientSlots[i]);
				}
			}

			// process datagram queue
			Datagram packet;
            int countAvailable = listenSocket == null ? 0 : listenSocket.AvailableToReadCount;
            int countProcessed = 0;
            while (countProcessed < countAvailable && listenSocket.Read(out packet))
            {
                processDatagram(packet.payload, packet.payloadSize, packet.sender);
				packet.Release();
				++countProcessed;
			}
		}

		private void serverTick_SeparateThread(Object stateInfo)
		{
			long lastStartTicks = DateTime.UtcNow.Ticks;
			double tickLength = 1.0 / tickrate;
			long tickDurationTicks = TimeSpan.FromSeconds(tickLength).Ticks;
			
			while (isRunning)
			{
                try
                {
					var utcNow = DateTime.UtcNow;
					lastStartTicks = utcNow.Ticks;

					Tick(utcNow.GetTotalSeconds());
                }
                catch (Exception e)
                {
                    GONet.GONetLog.Error(string.Concat("Unexpected error while ticking in separate thread.  Exception.Type: ", e.GetType().Name, " Exception.Message: ", e.Message, " \nException.StackTrace: ", e.StackTrace));
                }
                finally
                {
					long ticksToSleep = tickDurationTicks - (DateTime.UtcNow.Ticks - lastStartTicks);
					if (ticksToSleep > 0)
					{
						Thread.Sleep(TimeSpan.FromTicks(ticksToSleep));
					}
				}
			}
		}

		// process a received datagram
		private void processDatagram(byte[] payload, int size, EndPoint sender)
		{
			using (var reader = ByteArrayReaderWriter.Get(payload))
			{
				NetcodePacketHeader packetHeader = new NetcodePacketHeader();
				packetHeader.Read(reader);

				if (packetHeader.PacketType == NetcodePacketType.ConnectionRequest)
				{
					if (!debugIgnoreConnectionRequest)
						processConnectionRequest(reader, size, sender);
				}
				else
				{
					switch (packetHeader.PacketType)
					{
						case NetcodePacketType.ChallengeResponse:
							if (!debugIgnoreChallengeResponse)
								processConnectionResponse(reader, packetHeader, size, sender);
							break;
						case NetcodePacketType.ConnectionKeepAlive:
							processConnectionKeepAlive(reader, packetHeader, size, sender);
							break;
						case NetcodePacketType.ConnectionPayload:
							processConnectionPayload(reader, packetHeader, size, sender);
							break;
						case NetcodePacketType.ConnectionDisconnect:
							processConnectionDisconnect(reader, packetHeader, size, sender);
							break;
					}
				}
			}
		}

		#endregion

		#region Receive Packet Methods

		// check the packet against the client's replay protection, returning true if packet was replayed, false otherwise
		private bool checkReplay(NetcodePacketHeader header, EndPoint sender)
		{
			var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(sender, totalSeconds);
            if (cryptIdx == -1) {
                log("Replay protection failed to find encryption mapping", NetcodeLogLevel.Debug);
                return true;
            }

			var clientIndex = encryptionManager.GetClientID(cryptIdx);
			var client = clientSlots[clientIndex];

            if (client == null) {
                log("Replay protection failed to find client", NetcodeLogLevel.Debug);
                return true;
            }

			return client.replayProtection.AlreadyReceived(header.SequenceNumber);
		}

		// process an incoming disconnect message
		private void processConnectionDisconnect(ByteArrayReaderWriter reader, NetcodePacketHeader header, int size, EndPoint sender)
        {
            if (checkReplay(header, sender))
			{
				return;
			}

			// encryption mapping was not registered, so don't bother
			int cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(sender, totalSeconds);
			if (cryptIdx == -1)
			{
				log("No crytpo key for sender", NetcodeLogLevel.Debug);
				return;
            }

            var decryptKey = encryptionManager.GetReceiveKey(cryptIdx);

			var disconnectPacket = new NetcodeDisconnectPacket() { Header = header };
			if (!disconnectPacket.Read(reader, size - (int)reader.ReadPosition, decryptKey, protocolID))
				return;

            // locate the client by endpoint and free their slot
            var clientIndex = encryptionManager.GetClientID(cryptIdx);

			var client = clientSlots[clientIndex];
            if (client == null) return;

            clientSlots[clientIndex] = null;

			encryptionManager.RemoveAllEncryptionMappings(sender);

            // make sure all other clients still have their encryption mappings
            foreach (RemoteClient otherClient in clientSlots)
            {
                if (otherClient == null) continue;
                if (encryptionManager.GetEncryptionMappingIndexForTime(otherClient.RemoteEndpoint, totalSeconds) == -1)
                    log("Encryption mapping removed wrong mapping!", NetcodeLogLevel.Debug);
            }

			// trigger client disconnect callback
			OnClientDisconnected?.Invoke(client);

			log("Client {0} disconnected", NetcodeLogLevel.Info, client.RemoteEndpoint);
		}

		// process an incoming payload
		private void processConnectionPayload(ByteArrayReaderWriter reader, NetcodePacketHeader header, int size, EndPoint sender)
		{
			if (checkReplay(header, sender))
			{
				return;
			}

			// encryption mapping was not registered, so don't bother
			int cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(sender, totalSeconds);
			if (cryptIdx == -1)
			{
				log("No crytpo key for sender", NetcodeLogLevel.Debug);
				return;
			}

			// grab the decryption key and decrypt the packet
			var decryptKey = encryptionManager.GetReceiveKey(cryptIdx);

			var payloadPacket = new NetcodePayloadPacket() { Header = header };
			if (!payloadPacket.Read(reader, size - (int)reader.ReadPosition, decryptKey, protocolID))
				return;

			var clientIndex = encryptionManager.GetClientID(cryptIdx);
			var client = clientSlots[clientIndex];

			// trigger callback
			OnClientMessageReceived?.Invoke(client, payloadPacket.Payload, payloadPacket.Length);

			payloadPacket.Release();
		}

		// process an incoming connection keep alive packet
		private void processConnectionKeepAlive(ByteArrayReaderWriter reader, NetcodePacketHeader header, int size, EndPoint sender)
		{
			if (checkReplay(header, sender))
			{
                log("Detected replay in keep-alive", NetcodeLogLevel.Error);
				return;
			}

			// encryption mapping was not registered, so don't bother
			int cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(sender, totalSeconds);
			if (cryptIdx == -1)
			{
				log("No crytpo key for sender", NetcodeLogLevel.Error);
				return;
			}

			// grab the decryption key and decrypt the packet
			var decryptKey = encryptionManager.GetReceiveKey(cryptIdx);

			var keepAlivePacket = new NetcodeKeepAlivePacket() { Header = header };
			if (!keepAlivePacket.Read(reader, size - (int)reader.ReadPosition, decryptKey, protocolID))
			{
				log(string.Concat("Failed to decrypt..sender: ", sender.ToString()), NetcodeLogLevel.Error);
				return;
			}

			if (keepAlivePacket.ClientIndex >= maxSlots)
			{
				log("Invalid client index", NetcodeLogLevel.Error);
				return;
			}

			var client = this.clientSlots[(int)keepAlivePacket.ClientIndex];
            if (client == null) {
                log("Failed to find client for endpoint", NetcodeLogLevel.Error);
                return;
            }

			if (!client.RemoteEndpoint.Equals(sender))
			{
				log("Client does not match sender", NetcodeLogLevel.Error);
				return;
			}

			if (!client.Confirmed)
			{
				// trigger callback
				OnClientConnected?.Invoke(client);

				log("Client {0} connected", NetcodeLogLevel.Info, client.RemoteEndpoint);
			}

			client.Confirmed = true;

			client.Touch(totalSeconds);

			int index = encryptionManager.GetEncryptionMappingIndexForTime(client.RemoteEndpoint, totalSeconds);
			encryptionManager.Touch(index, client.RemoteEndpoint, totalSeconds);
		}

		// process an incoming connection response packet
		private void processConnectionResponse(ByteArrayReaderWriter reader, NetcodePacketHeader header, int size, EndPoint sender)
		{
			log("Got connection response from sender: {0}", NetcodeLogLevel.Info, sender.ToString());

			// encryption mapping was not registered, so don't bother
			int cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(sender, totalSeconds);
			if (cryptIdx == -1)
			{
				log("No crytpo key for sender: {0}", NetcodeLogLevel.Error, sender.ToString());
				return;
			}

			// grab the decryption key and decrypt the packet
			var decryptKey = encryptionManager.GetReceiveKey(cryptIdx);

			var connectionResponsePacket = new NetcodeConnectionChallengeResponsePacket() { Header = header };
			if (!connectionResponsePacket.Read(reader, size - (int)reader.ReadPosition, decryptKey, protocolID))
			{
				log("Failed to decrypt packet for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				return;
			}

			var challengeToken = new NetcodeChallengeToken();
			if (!challengeToken.Read(connectionResponsePacket.ChallengeTokenBytes, connectionResponsePacket.ChallengeTokenSequence, challengeKey))
			{
				log("Failed to read challenge token for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				connectionResponsePacket.Release();
				return;
			}

			// if a client from packet source IP / port is already connected, ignore the packet
			if (clientSlots.Any(x => x != null && x.RemoteEndpoint.Equals(sender)))
			{
				log("Client {0} already connected", NetcodeLogLevel.Debug, sender.ToString());
				return;
			}

			// if a client with the same id is already connected, ignore the packet
			if (clientSlots.Any(x => x != null && x.ClientID == challengeToken.ClientID))
			{
				log("Client ID {0} already connected", NetcodeLogLevel.Debug, challengeToken.ClientID);
				return;
			}

			// if the server is full, deny the connection
			int nextSlot = getFreeClientSlot();
			if (nextSlot == -1)
			{
				log("Server full, denying connection for sender: {0}", NetcodeLogLevel.Info, sender.ToString());
				denyConnection(sender, encryptionManager.GetSendKey(cryptIdx));
				return;
			}

			// assign the endpoint and client ID to a free client slot and set connected to true
			RemoteClient client = new RemoteClient(this);
			client.ClientID = challengeToken.ClientID;
			client.RemoteEndpoint = sender;
			client.Connected = true;
			client.replayProtection = new NetcodeReplayProtection();

			// assign timeout to client
			client.timeoutSeconds = encryptionManager.GetTimeoutSeconds(cryptIdx);

			// assign client to a free slot
			client.ClientIndex = (uint)nextSlot;
			this.clientSlots[nextSlot] = client;

			encryptionManager.SetClientID(cryptIdx, client.ClientIndex);

			// copy user data so application can make use of it, and set confirmed to false
			client.UserData = challengeToken.UserData;
			client.Confirmed = false;

			client.Touch(totalSeconds);

			// respond with a connection keep alive packet
			sendKeepAlive(client);
		}

		// process an incoming connection request packet
		private void processConnectionRequest(ByteArrayReaderWriter reader, int size, EndPoint sender)
		{
			log("Got connection request for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());

			var connectionRequestPacket = new NetcodeConnectionRequestPacket();
			if (!connectionRequestPacket.Read(reader, size - (int)reader.ReadPosition, protocolID))
			{
				log("Failed to read request for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				return;
			}

			// expiration timestamp should be greater than current timestamp
			if (connectionRequestPacket.Expiration <= (ulong)Math.Truncate(totalSeconds))
			{
				log("Connect token expired for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				connectionRequestPacket.Release();
				return;
			}

			var privateConnectToken = new NetcodePrivateConnectToken();
			if (!privateConnectToken.Read(connectionRequestPacket.ConnectTokenBytes, privateKey, protocolID, connectionRequestPacket.Expiration, connectionRequestPacket.TokenSequenceNum))
			{
				log("Failed to read private token for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				connectionRequestPacket.Release();
				return;
			}

			// if this server's public IP is not in the list of endpoints, packet is not valid
			bool serverAddressInEndpoints = privateConnectToken.ConnectServers.Any(x => x.Endpoint.CompareEndpoint(this.listenEndpoint, this.Port));
			if (!serverAddressInEndpoints)
			{
				log($"Server address not listen in token.  this.listenEndpoint: {this.listenEndpoint}", NetcodeLogLevel.Debug);

				{
					// SHAUN TODO: when server is listening on ANY or LOOPBACK, it will not show up in token from client
					//return; put me in coach!

					privateConnectToken.ConnectServers.ToList().ForEach(x => log($"\tprivateConnectionToken.ConnectServers[i].Endpoint: {x.Endpoint}", NetcodeLogLevel.Debug));
				}
            }

			// if a client from packet source IP / port is already connected, ignore the packet
			if (clientSlots.Any(x => x != null && x.RemoteEndpoint.Equals(sender)))
			{
				log("Client {0} already connected", NetcodeLogLevel.Debug, sender.ToString());
				return;
			}

			// if a client with the same id as the connect token is already connected, ignore the packet
			if (clientSlots.Any(x => x != null && x.ClientID == privateConnectToken.ClientID))
			{
				log("Client ID {0} already connected", NetcodeLogLevel.Debug, privateConnectToken.ClientID);
				return;
			}

			// if the connect token has already been used by a different endpoint, ignore the packet
			// otherwise, add the token hmac and endpoint to the used token history
			// compares the last 16 bytes (token mac)
			byte[] token_mac = BufferPool.GetBuffer(Defines.MAC_SIZE);
			System.Array.Copy(connectionRequestPacket.ConnectTokenBytes, Defines.NETCODE_CONNECT_TOKEN_PRIVATE_BYTES - Defines.MAC_SIZE, token_mac, 0, Defines.MAC_SIZE);
			if (!findOrAddConnectToken(sender, token_mac, totalSeconds))
			{
				log("Token already used for sender: {0}", NetcodeLogLevel.Debug, sender.ToString());
				BufferPool.ReturnBuffer(token_mac);
				return;
			}

			BufferPool.ReturnBuffer(token_mac);

			// if we have no slots, we need to respond with a connection denied packet
			var nextSlot = getFreeClientSlot();
			if (nextSlot == -1)
			{
				denyConnection(sender, privateConnectToken.ServerToClientKey);
				log("Server is full, denying connection for sender: {0}", NetcodeLogLevel.Info, sender.ToString());
				return;
			}

			// add encryption mapping for this endpoint as well as timeout
			// packets received from this endpoint are to be decrypted with the client-to-server key
			// packets sent to this endpoint are to be encrypted with the server-to-client key
			// if no messages are received within timeout from this endpoint, it is disconnected (unless timeout is negative)
			if (!encryptionManager.
                AddEncryptionMapping(
                    sender,
				    privateConnectToken.ServerToClientKey,
				    privateConnectToken.ClientToServerKey,
				    totalSeconds,
				    totalSeconds + 30,
				    privateConnectToken.TimeoutAfterSeconds,
				    0))
			{
				log("Failed to add encryption mapping for sender: {0}", NetcodeLogLevel.Error, sender.ToString());
				return;
			}

			// finally, send a connection challenge packet
			sendConnectionChallenge(privateConnectToken, sender);
		}

		#endregion

		#region Send Packet Methods

		// disconnect all clients
		private void disconnectAll()
		{
			for (int i = 0; i < clientSlots.Length; i++)
			{
				if (clientSlots[i] != null)
					disconnectClient(clientSlots[i]);
			}
		}

		// sends a disconnect packet to the client
		private void disconnectClient(RemoteClient client)
		{
			for (int i = 0; i < clientSlots.Length; i++)
			{
				if (clientSlots[i] == client)
				{
					clientSlots[i] = null;
					break;
				}
			}

			var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(client.RemoteEndpoint, totalSeconds);
			if (cryptIdx == -1)
			{
				return;
			}

			var cryptKey = encryptionManager.GetSendKey(cryptIdx);

			for (int i = 0; i < Defines.NUM_DISCONNECT_PACKETS; i++)
			{
				serializePacket(new NetcodePacketHeader() { PacketType = NetcodePacketType.ConnectionDisconnect }, NoOpWrite, client.RemoteEndpoint, cryptKey);
			}
		}

		// sends a connection denied packet to the endpoint
		private void denyConnection(EndPoint endpoint, byte[] cryptKey)
		{
			if (cryptKey == null)
			{
				var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(endpoint, totalSeconds);
				if (cryptIdx == -1) return;

				cryptKey = encryptionManager.GetSendKey(cryptIdx);
			}

            serializePacket(new NetcodePacketHeader() { PacketType = NetcodePacketType.ConnectionDenied }, NoOpWrite, endpoint, cryptKey);
		}

        /// <summary>
        /// Does nothing.
        /// </summary>
        private void NoOpWrite(ByteArrayReaderWriter writer) { }


		/* TODO come back to figure this refactor out to avoid GC for calls like writer => writer.WriteBuffer(payload, payloadSize)
		internal interface IReadWritePacket { }

		internal ref struct PayloadPacket : IReadWritePacket
        {
			public byte[] payload;
			public int payloadSize;


		}
		*/

        // send a payload to a client
        private void sendPayloadToClient(RemoteClient client, byte[] payload, int payloadSize)
		{
			// if the client isn't confirmed, send a keep-alive packet before this packet
			if (!client.Confirmed)
				sendKeepAlive(client);

			var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(client.RemoteEndpoint, totalSeconds);
            if (cryptIdx == -1)
            {
                GONet.GONetLog.Warning($"This is some bogus turdmeal.  Trying to send payload to client, but no encryption mapping is going go cause not sending it.  Double you tea, Eff?  client.RemoteEndpoint: {client.RemoteEndpoint}\n\ttotalSeconds: {totalSeconds}\n\t{encryptionManager.GetAllEncryptionMappingAddresses()}");
                return;
            }

            byte[] cryptKey = encryptionManager.GetSendKey(cryptIdx);

			// TODO this is part of the come back from above PayloadPacket payloadPacket = new PayloadPacket() { payload = payload, payloadSize = payloadSize };

			serializePacket(
				new NetcodePacketHeader() { PacketType = NetcodePacketType.ConnectionPayload }, 
				writer => writer.WriteBuffer(payload, payloadSize), 
				client.RemoteEndpoint, 
				cryptKey);
		}

		// send a keep-alive packet to the client
		private void sendKeepAlive(RemoteClient client)
		{
			var packet = new NetcodeKeepAlivePacket() { ClientIndex = client.ClientIndex, MaxSlots = (uint)this.maxSlots };

			var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(client.RemoteEndpoint, totalSeconds);
			if (cryptIdx == -1) return;

			var cryptKey = encryptionManager.GetSendKey(cryptIdx);

			serializePacket(new NetcodePacketHeader() { PacketType = NetcodePacketType.ConnectionKeepAlive }, writer => packet.Write(writer), client.RemoteEndpoint, cryptKey);
		}

		// sends a connection challenge packet to the endpoint
		private void sendConnectionChallenge(NetcodePrivateConnectToken connectToken, EndPoint endpoint)
		{
			log("Sending connection challenge", NetcodeLogLevel.Debug);

			var challengeToken = new NetcodeChallengeToken();
			challengeToken.ClientID = connectToken.ClientID;
			challengeToken.UserData = connectToken.UserData;

			ulong challengeSequence = nextChallengeSequenceNumber++;

			byte[] tokenBytes = BufferPool.GetBuffer(300);
			using (var tokenWriter = ByteArrayReaderWriter.Get(tokenBytes))
				challengeToken.Write(tokenWriter);

			byte[] encryptedToken = BufferPool.GetBuffer(300);
			int encryptedTokenBytes;

			try
			{
				encryptedTokenBytes = PacketIO.EncryptChallengeToken(challengeSequence, tokenBytes, challengeKey, encryptedToken);
			}
			catch
			{
				BufferPool.ReturnBuffer(tokenBytes);
				BufferPool.ReturnBuffer(encryptedToken);
				return;
			}

			var challengePacket = new NetcodeConnectionChallengeResponsePacket();
			challengePacket.ChallengeTokenSequence = challengeSequence;
			challengePacket.ChallengeTokenBytes = encryptedToken;

			var cryptIdx = encryptionManager.GetEncryptionMappingIndexForTime(endpoint, totalSeconds);
			if (cryptIdx == -1) return;

			var cryptKey = encryptionManager.GetSendKey(cryptIdx);

			serializePacket(new NetcodePacketHeader() { PacketType = NetcodePacketType.ConnectionChallenge }, writer => challengePacket.Write(writer), endpoint, cryptKey);

			BufferPool.ReturnBuffer(tokenBytes);
			BufferPool.ReturnBuffer(encryptedToken);
		}

		// encrypts a packet and sends it to the endpoint
		private void sendPacketToClient(NetcodePacketHeader packetHeader, byte[] packetData, int packetDataLen, EndPoint endpoint, byte[] key)
		{
			// assign a sequence number to this packet
			packetHeader.SequenceNumber = this.nextSequenceNumber++;

			// encrypt packet data
			byte[] encryptedPacketBuffer = BufferPool.GetBuffer(2048);

			int encryptedBytes = PacketIO.EncryptPacketData(packetHeader, protocolID, packetData, packetDataLen, key, encryptedPacketBuffer);

			int packetLen = 0;

			// write packet to byte array
			var packetBuffer = BufferPool.GetBuffer(2048);
			using (var packetWriter = ByteArrayReaderWriter.Get(packetBuffer))
			{
				packetHeader.Write(packetWriter);
				packetWriter.WriteBuffer(encryptedPacketBuffer, encryptedBytes);

				packetLen = (int)packetWriter.WritePosition;
			}

            // send packet
            //GONet.GONetLog.Debug("sending...packetLen: " + packetLen);
            listenSocket.SendTo(packetBuffer, packetLen, endpoint);

			BufferPool.ReturnBuffer(packetBuffer);
			BufferPool.ReturnBuffer(encryptedPacketBuffer);
		}

		private void serializePacket(NetcodePacketHeader packetHeader, Action<ByteArrayReaderWriter> write, EndPoint endpoint, byte[] key)
		{
			byte[] tempPacket = BufferPool.GetBuffer(2048);
			int writeLen = 0;
			using (var writer = ByteArrayReaderWriter.Get(tempPacket))
			{
				write(writer);
				writeLen = (int)writer.WritePosition;
			}

			sendPacketToClient(packetHeader, tempPacket, writeLen, endpoint, key);
			BufferPool.ReturnBuffer(tempPacket);
		}

		#endregion

		#region Misc Util Methods

		// find or add a connect token entry
		// intentional constant time worst case search
		private bool findOrAddConnectToken(EndPoint address, byte[] mac, double time)
		{
			int matchingTokenIndex = -1;
			int oldestTokenIndex = -1;
			double oldestTokenTime = 0.0;

			for (int i = 0; i < connectTokenHistory.Length; i++)
			{
				var token = connectTokenHistory[i];
				if (MiscUtils.CompareHMACConstantTime(token.mac, mac))
					matchingTokenIndex = i;

				if (oldestTokenIndex == -1 || token.time < oldestTokenTime)
				{
					oldestTokenTime = token.time;
					oldestTokenIndex = i;
				}
			}

			// if no entry is found with the mac, this is a new connect token. replace the oldest token entry.
			if (matchingTokenIndex == -1)
			{
				connectTokenHistory[oldestTokenIndex].time = time;
				connectTokenHistory[oldestTokenIndex].endpoint = address;
				Buffer.BlockCopy(mac, 0, connectTokenHistory[oldestTokenIndex].mac, 0, mac.Length);
				return true;
			}

			// allow connect tokens we have already seen from the same address
			if (connectTokenHistory[matchingTokenIndex].endpoint.Equals(address))
				return true;

			return false;
		}

		// reset connect token history
		private void resetConnectTokenHistory()
		{
			for (int i = 0; i < connectTokenHistory.Length; i++)
			{
				connectTokenHistory[i].endpoint = null;
				Array.Clear(connectTokenHistory[i].mac, 0, 16);
				connectTokenHistory[i].time = -1000.0;
			}
		}

		// initialize connect token history
		private void initConnectTokenHistory()
		{
			for (int i = 0; i < connectTokenHistory.Length; i++)
			{
				connectTokenHistory[i].mac = new byte[Defines.MAC_SIZE];
			}

			resetConnectTokenHistory();
		}

		/// <summary>
		/// allocate the next free client slot
		/// </summary>
		private int getFreeClientSlot()
		{
			for (int i = 0; i < maxSlots; i++)
			{
				if (clientSlots[i] == null)
					return i;
			}

			return -1;
		}

		private void log(string log, NetcodeLogLevel logLevel)
		{
			if (logLevel > this.LogLevel)
				return;

            switch (logLevel)
            {
                case NetcodeLogLevel.Debug:
                    GONet.GONetLog.Debug(log);
                    break;
                case NetcodeLogLevel.Info:
                    GONet.GONetLog.Info(log);
                    break;
                case NetcodeLogLevel.Error:
                    GONet.GONetLog.Error(log);
                    break;
            }
        }

		private void log(string log, NetcodeLogLevel logLevel, params object[] args)
		{
			if (logLevel > this.LogLevel)
				return;

            switch (logLevel)
            {
                case NetcodeLogLevel.Debug:
                    GONet.GONetLog.Debug(string.Format(log, args));
                    break;
                case NetcodeLogLevel.Info:
                    GONet.GONetLog.Info(string.Format(log, args));
                    break;
                case NetcodeLogLevel.Error:
                    GONet.GONetLog.Error(string.Format(log, args));
                    break;
            }
        }

		#endregion
	}
}
