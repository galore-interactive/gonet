using NetcodeIO.NET;
using ReliableNetcode;
using System;

namespace GONet
{
    public class GONetClient
    {
        GONetConnection_ClientToServer connectionToServer;

        private Client client;

        public delegate void ServerMessageDelegate(byte[] messageBytes, int bytesUsedCount);
        public event ServerMessageDelegate MessageReceived;

        public GONetClient(Client client)
        {
            this.client = client;

            connectionToServer = new GONetConnection_ClientToServer(client);

            client.OnStateChanged += OnStateChanged;

            connectionToServer.ReceiveCallback = OnReceiveCallback;
        }

        private void OnReceiveCallback(byte[] messageBytes, int bytesUsedCount)
        {
            GONetMain.ProcessIncomingBytes(connectionToServer, messageBytes, bytesUsedCount);

            MessageReceived?.Invoke(messageBytes, bytesUsedCount);
        }

        public void ConnectToServer(string serverIP, int serverPort)
        {
            connectionToServer.Connect(serverIP, serverPort);
        }

        public void SendBytesToServer(byte[] bytes, int bytesUsedCount, QosType qualityOfService = QosType.Reliable)
        {
            connectionToServer.SendMessage(bytes, bytesUsedCount, qualityOfService);
        }

        /// <summary>
        /// Call this every frame in order to process all network traffic in a timely manner.
        /// </summary>
        public void Update()
        {
            connectionToServer.Update();
        }

        public void Disconnect()
        {
            connectionToServer.Disconnect();
        }

        private void OnStateChanged(ClientState state)
        {
            UnityEngine.Debug.Log("state changed to: " + Enum.GetName(typeof(ClientState), state)); // TODO remove unity references from this code base!
        }

    }
}
