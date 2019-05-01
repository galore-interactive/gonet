using GONet.Utils;
using NetcodeIO.NET;
using ReliableNetcode;
using System.Net;

namespace GONet
{
    public abstract class GONetConnection : ReliableEndpoint
    {
        public uint OwnerAuthorityId { get; internal set; }

        #region round trip time stuffs (RTT)

        private float rtt_latest;
        /// <summary>
        /// Unit of measure is seconds here.
        /// </summary>
        public float RTT_Latest
        {
            get { return rtt_latest; }
            internal set
            {
                rtt_latest = value;

                if (++iLast_rtt_recent == RTT_HISTORY_COUNT)
                {
                    iLast_rtt_recent = 0;
                }
                rtt_recent[iLast_rtt_recent] = value;

                if (hasBeenSetOnce_rtt_latest)
                {
                    float sum = 0f;
                    for (int i = 0; i < RTT_HISTORY_COUNT; ++i)
                    {
                        sum += rtt_recent[i];
                    }
                    RTT_RecentAverage = sum / RTT_HISTORY_COUNT;
                }
                else
                {
                    for (int i = 0; i < RTT_HISTORY_COUNT; ++i)
                    {
                        rtt_recent[i] = value;
                    }
                    hasBeenSetOnce_rtt_latest = true;
                    RTT_RecentAverage = value;
                }
            }
        }
        
        /// <summary>
        /// This is useful to reference/use instead of <see cref="RTT_Latest"/> in order to account for jitter (i.e., RTT variation) by averaging recent values.
        /// Unit of measure is seconds here.
        /// </summary>
        public float RTT_RecentAverage { get; private set; }

        private const int RTT_HISTORY_COUNT = 5;
        bool hasBeenSetOnce_rtt_latest = false;
        int iLast_rtt_recent = -1;
        readonly float[] rtt_recent = new float[RTT_HISTORY_COUNT];

        #endregion

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

            OwnerAuthorityId = GONetMain.OwnerAuthorityId_Server;

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
