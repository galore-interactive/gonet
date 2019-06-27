/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using GONet.Utils;
using NetcodeIO.NET;
using ReliableNetcode;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using GONetChannelId = System.Byte;

namespace GONet
{
    public abstract class GONetConnection : ReliableEndpoint
    {
        public const int MTU = 1400;
        private const int MTU_X2 = MTU << 1;

        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> messageByteArrayPoolByThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

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
        private const string DO_NOT_USE = "Do not use this method.  Use SendMessageOverChannel(byte[], int, GONetChannelId) instead.";
        bool hasBeenSetOnce_rtt_latest = false;
        int iLast_rtt_recent = -1;
        readonly float[] rtt_recent = new float[RTT_HISTORY_COUNT];

        #endregion

        protected GONetConnection()
        {
            ReceiveCallback = OnReceiveCallback;
        }

        [Obsolete(DO_NOT_USE, true)]
        public new void SendMessage(byte[] messageBytes, int bytesUsedCount, QosType qualityOfService)
        {
            throw new NotImplementedException(DO_NOT_USE);
        }

        /// <summary>
        /// IMPORTANT: You must use this method instead of <see cref="ReliableEndpoint.SendMessage(byte[], int, QosType)"/> in order for the channel stuff to work properly!
        /// </summary>
        public void SendMessageOverChannel(byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            int headerSize = sizeof(GONetChannelId) + sizeof(int);
            int bodySize_withHeader = bytesUsedCount + headerSize;
            byte[] messageBytes_withHeader = BorrowByteArray(bodySize_withHeader);

            Utils.BitConverter.GetBytes(channelId, messageBytes_withHeader, 0);
            Utils.BitConverter.GetBytes(bytesUsedCount, messageBytes_withHeader, sizeof(GONetChannelId));
            Buffer.BlockCopy(messageBytes, 0, messageBytes_withHeader, headerSize, bytesUsedCount);

            GONetChannel channel = GONetChannel.ById(channelId);
            base.SendMessage(messageBytes_withHeader, bodySize_withHeader, channel.QualityOfService); // IMPORTANT: this should be the ONLY call to this method in all of GONet! including user codebases!

            ReturnByteArray(messageBytes_withHeader);
        }

        private void OnReceiveCallback(byte[] messageBytes, int bytesUsedCount)
        {
            int headerSize = sizeof(GONetChannelId) + sizeof(int);
            int bodySize_expected = bytesUsedCount - headerSize;

            uint bodySize_readFromMessage;
            GONetChannelId channelId_readFromMessage;

            byte[] messageBytes_withoutHeader = BorrowByteArray(bodySize_expected);
            Buffer.BlockCopy(messageBytes, headerSize, messageBytes_withoutHeader, 0, bodySize_expected);

            using (var memoryStream = new MemoryStream(messageBytes))
            {
                using (var bitStream = new Utils.BitStream(memoryStream))
                {
                    channelId_readFromMessage = (GONetChannelId)bitStream.ReadByte();
                    bitStream.ReadUInt(out bodySize_readFromMessage);
                }
            }

            GONetMain.ProcessIncomingBytes(this, messageBytes_withoutHeader, (int)bodySize_readFromMessage, channelId_readFromMessage);

            ReturnByteArray(messageBytes_withoutHeader);
        }

        /// <summary>
        /// Use this to borrow byte arrays as needed for the GetBytes calls.
        /// Ensure you subsequently call <see cref=""/>
        /// </summary>
        /// <returns>byte array of size 8</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] BorrowByteArray(int minimumSize)
        {
            ArrayPool<byte> arrayPool;
            if (!messageByteArrayPoolByThreadMap.TryGetValue(Thread.CurrentThread, out arrayPool))
            {
                arrayPool = new ArrayPool<byte>(25, 5, MTU, MTU_X2);
                messageByteArrayPoolByThreadMap[Thread.CurrentThread] = arrayPool;
            }
            return arrayPool.Borrow(minimumSize);
        }

        /// <summary>
        /// PRE: Required that <paramref name="borrowed"/> was returned from a call to <see cref="BorrowByteArray(int)"/> and not already passed in here.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReturnByteArray(byte[] borrowed)
        {
            messageByteArrayPoolByThreadMap[Thread.CurrentThread].Return(borrowed);
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
