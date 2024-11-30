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

using GONet.Utils;
using NetcodeIO.NET;
using ReliableNetcode;
using System;
using System.Net;
using GONetChannelId = System.Byte;

namespace GONet
{
    public abstract class GONetConnection : ReliableEndpoint
    {
        /// <summary>
        /// Whether this connection is client side and represents the connection to the server or it is server side and represents the connection to the client, 
        /// this value here is the unique ID of the connection between the two computers as was initially set/created by the client first starting the connection.
        /// There is even the potential for future releases of GONet where this connection represents a client (peer) to client (peer) connection, but one of the two
        /// had to initiate the connection.
        /// Both parties connected will have the same value in this field.
        /// </summary>
        public ulong InitiatingClientConnectionUID { get; protected set; }

        public ushort OwnerAuthorityId { get; internal set; }

        #region round trip time stuffs (RTT)

        public float RTTMilliseconds_LowLevelTransportProtocol => RTTMilliseconds;

        private float rtt_latest;
        /// <summary>
        /// GONet owned data that represents more than just the low level network "wire" time.
        /// If you want internally calculated value of RTT from lower level transport/protocol impl, see/use <see cref="RTTMilliseconds_LowLevelTransportProtocol"/> (which is just a reflection of <see cref="ReliableEndpoint.RTTMilliseconds"/>) instead.
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
        /// GONet owned data that represents more than just the low level network "wire" time.
        /// If you want internally calculated value of RTT from lower level transport/protocol impl, see/use <see cref="RTTMilliseconds_LowLevelTransportProtocol"/> (which is just a reflection of <see cref="ReliableEndpoint.RTTMilliseconds"/>) instead.
        /// This is useful to reference/use instead of <see cref="RTT_Latest"/> in order to account for jitter (i.e., RTT variation) by averaging recent values.
        /// Unit of measure is seconds here.
        /// </summary>
        public float RTT_RecentAverage { get; private set; }

        private const int RTT_HISTORY_COUNT = 5;
        private const string DO_NOT_USE = "Do not use this method.  Use SendMessageOverChannel(byte[], int, GONetChannelId) instead.";
        bool hasBeenSetOnce_rtt_latest = false;
        int iLast_rtt_recent = -1;
        readonly float[] rtt_recent = new float[RTT_HISTORY_COUNT];

        #endregion

        protected GONetConnection()
        {
            ReceiveCallback = OnReceiveCallback;
        }

        /// <summary>
        /// IMPORTANT: You must NOT use this method.  Instead, use <see cref="SendMessageOverChannel(GONetChannelId[], int, GONetChannelId)"/> in order for the channel stuff to work properly!
        /// </summary>
        [Obsolete(DO_NOT_USE, true)]
        public new void SendMessage(byte[] messageBytes, int bytesUsedCount, QosType qualityOfService)
        {
            throw new NotImplementedException(DO_NOT_USE);
        }

        /// <summary>
        /// IMPORTANT: You **MUST** use this method instead of <see cref="ReliableEndpoint.SendMessage(byte[], int, QosType)"/> in order for the channel stuff to work properly!
        /// </summary>
        public void SendMessageOverChannel(byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            int headerSize = sizeof(GONetChannelId) + sizeof(int);
            int bodySize_withHeader;

            byte[] messageBytesCompressed = null;
            ushort messageBytesCompressedUsedCount;

            bool isCompressionUsed = GONetMain.AutoCompressEverything != null;
            if (isCompressionUsed)
            {
                GONetMain.AutoCompressEverything.Compress(messageBytes, (ushort)bytesUsedCount, out messageBytesCompressed, out messageBytesCompressedUsedCount);
                messageBytes = messageBytesCompressed;
                bytesUsedCount = messageBytesCompressedUsedCount;
            }

            bodySize_withHeader = bytesUsedCount + headerSize;

            byte[] messageBytes_withHeader = SerializationUtils.BorrowByteArray(bodySize_withHeader);
            Utils.BitConverter.GetBytes(channelId, messageBytes_withHeader, 0);

            Utils.BitConverter.GetBytes(bytesUsedCount, messageBytes_withHeader, sizeof(GONetChannelId));
            Buffer.BlockCopy(messageBytes, 0, messageBytes_withHeader, headerSize, bytesUsedCount);

            GONetChannel channel = GONetChannel.ById(channelId);
            base.SendMessage(messageBytes_withHeader, bodySize_withHeader, channel.QualityOfService); // IMPORTANT: this should be the ONLY call to this method in all of GONet! including user codebases!

            { // memory management:
                SerializationUtils.ReturnByteArray(messageBytes_withHeader);

                if (isCompressionUsed)
                {
                    SerializationUtils.ReturnByteArray(messageBytesCompressed);
                }
            }
        }

        private void OnReceiveCallback(byte[] messageBytes, int bytesUsedCount)
        {
            int headerSize = sizeof(GONetChannelId) + sizeof(int);
            int bodySize_expected = bytesUsedCount - headerSize;

            uint bodySize_readFromMessage;
            GONetChannelId channelId_readFromMessage;

            byte[] messageBytes_withoutHeader = SerializationUtils.BorrowByteArray(bodySize_expected);
            Buffer.BlockCopy(messageBytes, headerSize, messageBytes_withoutHeader, 0, bodySize_expected);

            using (var bitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(messageBytes, bytesUsedCount))
            {
                channelId_readFromMessage = (GONetChannelId)bitStream.ReadByte();
                bitStream.ReadUInt(out bodySize_readFromMessage);
            }

            byte[] messageBytesUncompressed = messageBytes_withoutHeader;
            ushort messageBytesUncompressedUsedCount;

            bool isCompressionUsed = GONetMain.AutoCompressEverything != null;
            if (isCompressionUsed)
            {
                GONetMain.AutoCompressEverything.Uncompress(messageBytes_withoutHeader, (ushort)bodySize_expected, out messageBytesUncompressed, out messageBytesUncompressedUsedCount);
                bodySize_readFromMessage = messageBytesUncompressedUsedCount;
            }

            GONetMain.ProcessIncomingBytes_TriageFromAnyThread(this, messageBytesUncompressed, (int)bodySize_readFromMessage, channelId_readFromMessage);

            { // memory management:
                SerializationUtils.ReturnByteArray(messageBytes_withoutHeader);

                if (isCompressionUsed)
                {
                    SerializationUtils.ReturnByteArray(messageBytesUncompressed);
                }
            }
        }
    }

    public class GONetConnection_ClientToServer : GONetConnection
    {
        private Client client;

        private IPEndPoint mostRecentConnectInfo;

        public ClientState State => client.State;

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

        private const int CONNECTION_TOKEN_TIMOUT_SECONDS = 120;

        /// <summary>
        /// </summary>
        /// <param name="serverIP"></param>
        /// <param name="serverPort"></param>
        /// <param name="timeoutSeconds">
        /// This value serves two purposes:
        /// 1) Prior to connection being established, this represents how many seconds the client will attempt to connect to the server before giving up and considering the connected timed out (i.e., <see cref="ClientState.ConnectionRequestTimedOut"/>).  NOTE: During this time period, the connection will be attempted 10 times per second.
        /// 2) After connection is established, this represents how many seconds have to transpire with no communication for this connection to be considered timed out...then will be auto-disconnected.
        /// </param>
        public void Connect(string serverIP, int serverPort, int timeoutSeconds)
        {
            TokenFactory factory = new TokenFactory(GONetMain.noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest, GONetMain._privateKey);

            bool isChangingConnectInfo = default;
            IPAddress currenetServerIP = default;
            try
            {
                currenetServerIP = IPAddress.Parse(serverIP);
                isChangingConnectInfo = mostRecentConnectInfo == null || !IPAddress.Equals(mostRecentConnectInfo.Address, currenetServerIP) || mostRecentConnectInfo.Port != serverPort;
                if (isChangingConnectInfo)
                {
                    mostRecentConnectInfo = new IPEndPoint(currenetServerIP, serverPort);
                }
            }
            catch
            {
                // ASSuME serverIP actually represents a hostname and needs to be processed differently than an IP address
                IPEndPoint currentServerEndPoint = NetworkUtils.GetIPEndPointFromHostName(serverIP, serverPort);
                currenetServerIP = currentServerEndPoint.Address;
                isChangingConnectInfo = mostRecentConnectInfo == null || !IPAddress.Equals(mostRecentConnectInfo.Address, currenetServerIP) || mostRecentConnectInfo.Port != serverPort;
                if (isChangingConnectInfo)
                {
                    mostRecentConnectInfo = currentServerEndPoint;
                }
            }

            if (InitiatingClientConnectionUID == default || isChangingConnectInfo)
            {
                InitiatingClientConnectionUID = (ulong)GUID.Generate().AsInt64();
            }

            byte[] connectToken = factory.GenerateConnectToken(new IPEndPoint[] { mostRecentConnectInfo },
                CONNECTION_TOKEN_TIMOUT_SECONDS,
                timeoutSeconds,
                1UL,
                InitiatingClientConnectionUID,
                new byte[256]);

            client.Connect(connectToken);
        }

        /// <summary>
        /// Will log a warning if <see cref="client"/> is not in a <see cref="State"/> of <see cref="ClientState.Connected"/>; however, the deeper internal call to disconnect will still process.
        /// </summary>
        public void Disconnect()
        {
            if (State != ClientState.Connected)
            {
                const string STATE = "Calling Disconnect on a client connection to the server that is not currently in a connected state.  Actual state: ";
                GONetLog.Warning(string.Concat(STATE, Enum.GetName(typeof(ClientState), State)));
            }

            client.Disconnect();
        }
    }

    public class GONetConnection_ServerToClient : GONetConnection
    {
        private readonly RemoteClient remoteClient;

        public bool IsConnectedToClient => remoteClient.Connected;

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
