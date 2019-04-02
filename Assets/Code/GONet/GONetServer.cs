using System;
using System.Collections.Generic;
using NetcodeIO.NET;
using ReliableNetcode;

namespace GONet
{
    public class GONetServer
    {
        public int MaxSlots { get; private set; }

        Server server;
        public uint numConnections = 0;
        public GONetConnection_ServerToClient[] remoteClients;
        Dictionary<RemoteClient, GONetConnection_ServerToClient> remoteClientToGONetConnectionMap = new Dictionary<RemoteClient, GONetConnection_ServerToClient>(10);

        public GONetServer(int maxSlots, string address, int port)
        {
            GONetMain.isServerOverride = true; // wherever the server is running is automatically considered "the" server

            MaxSlots = maxSlots;

            server = new Server(maxSlots, address, port, GONetMain.noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest, GONetMain._privateKey);

            remoteClients = new GONetConnection_ServerToClient[maxSlots];

            server.LogLevel = NetcodeLogLevel.Debug;

            server.OnClientConnected += OnClientConnected;
            server.OnClientDisconnected += OnClientDisconnected;
            server.OnClientMessageReceived += OnClientMessageReceived;
        }

        public void Start()
        {
            server.Start();
        }

        /// <summary>
        /// Call this every frame in order to process all network traffic in a timely manner.
        /// </summary>
        public void Update()
        {
            for (int iConnection = 0; iConnection < numConnections; ++iConnection)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = remoteClients[iConnection];
                gONetConnection_ServerToClient.Update(); // have to do this in order for anything to really be processed, in or out.
            }
        }

        public void SendBytesToAllClients(byte[] bytes, int bytesUsedCount, QosType qualityOfService = QosType.Reliable)
        {
            for (int iConnection = 0; iConnection < numConnections; ++iConnection)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = remoteClients[iConnection];
                gONetConnection_ServerToClient.SendMessage(bytes, bytesUsedCount, qualityOfService);
            }
        }

        private void OnClientMessageReceived(RemoteClient sender, byte[] payload, int payloadSize)
        {
            GONetConnection_ServerToClient serverToClientConnection;
            if (remoteClientToGONetConnectionMap.TryGetValue(sender, out serverToClientConnection))
            {
                serverToClientConnection.ReceivePacket(payload, payloadSize);
            }
            else // we will ASSume the first time through is the connection message and OnClientConnected will not have been called yet and we will just go straight to process
            {
                // nothing to do i suppose
            }
        }

        private void OnClientDisconnected(RemoteClient client)
        {
            UnityEngine.Debug.Log("client DISconnected"); // TODO remove unity stuffs
        }

        private void OnClientConnected(RemoteClient client)
        {
            UnityEngine.Debug.Log("client connected"); // TODO remove unity stuffs

            if (numConnections < MaxSlots)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = new GONetConnection_ServerToClient(client);
                remoteClients[numConnections++] = gONetConnection_ServerToClient;
                remoteClientToGONetConnectionMap[client] = gONetConnection_ServerToClient;
            }
        }

        public void Stop()
        {
            server.Stop();
        }
    }
}
