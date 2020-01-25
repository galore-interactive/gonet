/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
        /// This would be set for (a) client-server topology with a client host (i.e., no dedigated server) or (b) peer to peer client host
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

        public delegate void InitializedWithServerDelegate(GONetClient client);
        public event InitializedWithServerDelegate InitializedWithServer;

        internal GONetConnection_ClientToServer connectionToServer;

        private Client client;

        public GONetClient(Client client)
        {
            this.client = client;

            connectionToServer = new GONetConnection_ClientToServer(client);

            client.OnStateChanged += OnStateChanged;
        }

        public void ConnectToServer(string serverIP, int serverPort, int ongoingTimeoutSeconds)
        {
            connectionToServer.Connect(serverIP, serverPort, ongoingTimeoutSeconds);
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

        private void OnStateChanged(ClientState state)
        {
            ConnectionState = state;
            GONetLog.Debug("state changed to: " + Enum.GetName(typeof(ClientState), state)); // TODO remove unity references from this code base!
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
