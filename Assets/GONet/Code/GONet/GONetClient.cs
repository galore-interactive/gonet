/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using NetcodeIO.NET;
using System;
using System.Collections.Generic;
using GONetChannelId = System.Byte;

namespace GONet
{
    [Flags]
    public enum ClientTypeFlags : byte
    {
        None = 0,

        Player_Standard = 1 << 0,

        /// <summary>
        /// This would be set for (a) client-server topology with a client host (i.e., no dedicated server) or (b) peer to peer client host
        /// </summary>
        ServerHost = 1 << 1,

        /* this likely does not belong here,...but a thought nonetheless:
        Replay_Recorder =       1 << 2,
        */
    }

    public class GONetClient
    {
        private ClientTypeFlags _clientTypeFlags = ClientTypeFlags.Player_Standard;
        internal ClientTypeFlags ClientTypeFlags
        {
            get => _clientTypeFlags;

            set
            {
                var previous = _clientTypeFlags;
                _clientTypeFlags = value;
                if (value != previous)
                {
                    GONetMain.EventBus.Publish(new ClientTypeFlagsChangedEvent(GONetMain.Time.ElapsedTicks, GONetMain.MyAuthorityId, previous, value));
                }
            }
        }

        public bool IsConnectedToServer => ConnectionState == ClientState.Connected;

        /// <summary>
        /// Current state of this client connection to server.
        /// Subscribe to <see cref="ClientStateChangedEvent"/> via <see cref="GONetMain.EventBus"/>'s <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/>
        /// if you want notification each time this changes.
        /// </summary>
        public ClientState ConnectionState { get; private set; } = ClientState.Disconnected;

        internal readonly Queue<GONetMain.NetworkData> incomingNetworkData_mustProcessAfterClientInitialized = new Queue<GONetMain.NetworkData>(100);

        bool isInitializedWithServer;
        public bool IsInitializedWithServer
        {
            get => IsConnectedToServer && isInitializedWithServer;
            internal set
            {
                bool before = IsInitializedWithServer;

                isInitializedWithServer = value;

                if (!before && IsInitializedWithServer)
                {
                    InitializedWithServer?.Invoke(this);
                }
            }
        }

        public delegate void ClientDelegate(GONetClient client);
        public event ClientDelegate InitializedWithServer;

        /// <summary>
        /// This *will* be called from main Unity thread.
        /// Also, consider subscribing to <see cref="ClientStateChangedEvent"/> to be informed of changes 
        /// that go beyond just connect/disconnect (e.g., all other values of <see cref="ClientState"/>).
        /// </summary>
        public event ClientDelegate ClientConnected;

        /// <summary>
        /// This *will* be called from main Unity thread.
        /// Also, consider subscribing to <see cref="ClientStateChangedEvent"/> to be informed of changes 
        /// that go beyond just connect/disconnect (e.g., all other values of <see cref="ClientState"/>).
        /// </summary>
        public event ClientDelegate ClientDisconnected;

        /// <summary>
        /// This auto-assigned UID is used to correlate this client's connection to the server both on client side and server side.
        /// See <see cref="ClientStateChangedEvent.InitiatingClientConnectionUID"/> and <see cref="RemoteClientStateChangedEvent.InitiatingClientConnectionUID"/>.
        /// IMPORTANT: This value changes inside the call to <see cref="ConnectToServer(string, int, int)"/> and <see cref="ConnectToServer(string, int, int, int)"/>, which means you should always access this property instead of storing the value off elsewhere.
        /// </summary>
        public ulong InitiatingClientConnectionUID => connectionToServer.InitiatingClientConnectionUID;

        internal readonly GONetConnection_ClientToServer connectionToServer;

        private readonly Client client;

        public GONetClient(Client client)
        {
            this.client = client;

            connectionToServer = new GONetConnection_ClientToServer(client);

            client.OnStateChanged += OnStateChanged_BubbleEventUp;
            client.TickBeginning += Client_TickBeginning_PossibleSeparateThread;

            // Since the OnStateChanged_BubbleEventUp can occur on non-main Unity thread, this
            // provides a way to process (i.e., invoke public event) on the main thread since
            // GONet event bus subscriptions are processed on the main thread.
            GONetMain.EventBus.Subscribe<ClientStateChangedEvent>(OnStateChanged_BubbleEventUp_MainThread);
        }

        private void Client_TickBeginning_PossibleSeparateThread()
        {
            connectionToServer.ProcessSendBuffer_IfAppropriate();
        }

        /// <summary>
        /// NOTE: Consider subscribing to <see cref="ClientStateChangedEvent"/> (via <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/>) prior to calling this so you can react to any changes to the state.
        ///       If you do subscribe, ensure the subscription filter/predicate compares its <see cref="ClientStateChangedEvent.InitiatingClientConnectionUID"/> to <see cref="InitiatingClientConnectionUID"/>.
        ///       See <see cref="GONetSampleSpawner.OnClientStateChanged_LogIt(GONetEventEnvelope{ClientStateChangedEvent})"/> for example.
        /// </summary>
        /// <param name="serverIP"></param>
        /// <param name="serverPort"></param>
        /// <param name="timeoutSeconds">
        /// This value serves two purposes:
        /// 1) Prior to connection being established, this represents how many seconds the client will attempt to connect to the server before giving up and considering the connected timed out (i.e., <see cref="ClientState.ConnectionRequestTimedOut"/>).  NOTE: During this time period, the connection will be attempted 10 times per second.
        /// 2) After connection is established, this represents how many seconds have to transpire with no communication for this connection to be considered timed out...then will be auto-disconnected by the server.
        /// </param>
        public void ConnectToServer(string serverIP, int serverPort, int timeoutSeconds)
        {
            connectionToServer.Connect(serverIP, serverPort, timeoutSeconds);
        }

        public void SendBytesToServer(byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            connectionToServer.SendMessageOverChannel(bytes, bytesUsedCount, channelId);
        }

        /// <summary>
        /// Call this every frame (from the main Unity thread!) in order to process all network traffic in a timely manner.
        /// </summary>
        public void Update()
        {
            if (ConnectionState == ClientState.Connected)
            {
                connectionToServer.Update();
            }
        }

        public void Disconnect()
        {
            connectionToServer.Disconnect();
        }

        /// <summary>
        /// Since the <see cref="client"/> is private, the event it publishes for state change is not visible to GONet users.
        /// So, this bubbles it up and fires a GONet event for them (i.e., <see cref="ClientStateChangedEvent"/>).
        /// </summary>
        private void OnStateChanged_BubbleEventUp(ClientState state)
        {
            var previous = ConnectionState;
            ConnectionState = state;

            const string CLIENT = "Client state changed to: ";
            const string AUTH = ".  My client guid: ";
            GONetLog.Debug(string.Concat(CLIENT, Enum.GetName(typeof(ClientState), state), AUTH, connectionToServer.InitiatingClientConnectionUID));

            // NOTE: The following will cause OnStateChanged_BubbleEventUp_MainThread to be called:
            if (previous != state)
            {
                GONetMain.EventBus.PublishASAP(new ClientStateChangedEvent(GONetMain.Time.ElapsedTicks, connectionToServer.InitiatingClientConnectionUID, previous, state));
            }
        }

        private void OnStateChanged_BubbleEventUp_MainThread(GONetEventEnvelope<ClientStateChangedEvent> eventEnvelope)
        {
            switch (eventEnvelope.Event.StateNow)
            {
                case ClientState.Connected:
                    ClientConnected?.Invoke(this);
                    break;
                case ClientState.Disconnected:
                    ClientDisconnected?.Invoke(this);
                    break;
            }
        }
    }

    public class GONetRemoteClient
    {
        public RemoteClient RemoteClient { get; private set; }

        public GONetConnection_ServerToClient ConnectionToClient { get; private set; }

        bool isInitializedWithServer;
        public bool IsInitializedWithServer
        {
            get => isInitializedWithServer;
            internal set
            {
                bool before = isInitializedWithServer;
                isInitializedWithServer = value;
                if (before != value && value)
                {
                    InitializedWithServer?.Invoke(this);
                }
            }
        }

        public delegate void InitializedWithServerDelegate(GONetRemoteClient remoteClient);
        public event InitializedWithServerDelegate InitializedWithServer;

        public GONetRemoteClient(RemoteClient remoteClient, GONetConnection_ServerToClient connectionToClient)
        {
            RemoteClient = remoteClient;
            ConnectionToClient = connectionToClient;
        }
    }
}
