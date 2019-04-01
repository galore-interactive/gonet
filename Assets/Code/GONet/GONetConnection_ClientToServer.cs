
using System;
using System.Net;
using NetcodeIO.NET;
using ReliableNetcode;

namespace GONet
{
    public class GONetConnection_ClientToServer : ReliableEndpoint
    {
        private Client client;

        public GONetConnection_ClientToServer(Client client)
        {
            this.client = client;

            client.OnMessageReceived += OnReceivedFromServer_AnyLittleThingTheProtocolLayerDeemsNecessary;
            client.OnStateChanged += OnStateChanged;

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
            ulong clientID = (ulong)Guid.NewGuid().ToString().GetHashCode(); // TODO replace with UID.Generate().aslong or something similar
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

        private void OnStateChanged(ClientState state)
        {
            UnityEngine.Debug.Log("state changed to: " + Enum.GetName(typeof(ClientState), state)); // TODO remove unity references from this code base!
        }
    }
}
