using NetcodeIO.NET;
using System;

using GONetChannelId = System.Byte;

namespace GONet
{
    public class GONetClient
    {
        public bool IsConnectedToServer => ConnectionState == ClientState.Connected;

        public ClientState ConnectionState { get; private set; } = ClientState.Disconnected;

        internal GONetConnection_ClientToServer connectionToServer;

        private Client client;

        public GONetClient(Client client)
        {
            this.client = client;

            connectionToServer = new GONetConnection_ClientToServer(client);

            client.OnStateChanged += OnStateChanged;
        }

        public void ConnectToServer(string serverIP, int serverPort)
        {
            connectionToServer.Connect(serverIP, serverPort);
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
}
