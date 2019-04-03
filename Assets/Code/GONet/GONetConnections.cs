using GONet.Utils;
using NetcodeIO.NET;
using ReliableNetcode;
using System.Net;

namespace GONet
{
    public abstract class GONetConnection : ReliableEndpoint
    {
        protected GONetConnection()
        {
            ReceiveCallback = OnReceiveCallback;
        }

        private void OnReceiveCallback(byte[] messageBytes, int bytesUsedCount)
        {
            GONetMain.ProcessIncomingBytes(this, messageBytes, bytesUsedCount);
        }
    }

    public class GONetConnection_ClientToServer : GONetConnection
    {
        private Client client;

        public GONetConnection_ClientToServer(Client client) : base()
        {
            this.client = client;

            client.OnMessageReceived += OnReceivedFromServer_AnyLittleThingTheProtocolLayerDeemsNecessary;

            TransmitCallback = SendToServer_AnyLittleThingTheProtocolLayerDeemsNecessary;
        }

        private void SendToServer_AnyLittleThingTheProtocolLayerDeemsNecessary(byte[] payloadBytes, int payloadSize)
        {
            client.Send(payloadBytes, payloadSize);
        }

        private void OnReceivedFromServer_AnyLittleThingTheProtocolLayerDeemsNecessary(byte[] payloadBytes, int payloadSize)
        {
            ReceivePacket(payloadBytes, payloadSize);
        }

        public void Connect(string serverIP, int serverPort)
        {
            TokenFactory factory = new TokenFactory(GONetMain.noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest, GONetMain._privateKey);
            ulong clientID = (ulong)GUID.Generate().AsInt64();
            byte[] connectToken = factory.GenerateConnectToken(new IPEndPoint[] { new IPEndPoint(IPAddress.Parse(serverIP), serverPort) },
                30,
                5,
                1UL,
                clientID,
                new byte[256]);

            client.Connect(connectToken);
        }

        public void Disconnect()
        {
            client.Disconnect();
        }
    }

    public class GONetConnection_ServerToClient : GONetConnection
    {
        private RemoteClient remoteClient;

        public GONetConnection_ServerToClient(RemoteClient remoteClient) : base()
        {
            this.remoteClient = remoteClient;

            TransmitCallback = SendToMyClient_AnyLittleThingTheProtocolLayerDeemsNecessary;
        }

        private void SendToMyClient_AnyLittleThingTheProtocolLayerDeemsNecessary(byte[] payloadBytes, int payloadSize)
        {
            remoteClient.SendPayload(payloadBytes, payloadSize);
        }
    }
}
