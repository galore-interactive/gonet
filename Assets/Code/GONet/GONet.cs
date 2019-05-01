using GONet.Generation;
using GONet.Utils;
using Microsoft.IO;
using ReliableNetcode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace GONet
{
    public static class GONetMain
    {
        public const ulong noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest = 0x1122334455667788L;

        public static readonly byte[] _privateKey = new byte[] // TODO generate this!?
        {
            0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
            0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
            0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
            0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1,
        };

        private static GONetSessionContext globalSessionContext;
        public static GONetSessionContext GlobalSessionContext
        {
            get { return globalSessionContext; }
            internal set
            {
                globalSessionContext = value;
                GlobalSessionContext_Participant = (object)globalSessionContext == null ? null : globalSessionContext.gameObject.GetComponent<GONetParticipant>();
            }
        }

        public static GONetParticipant GlobalSessionContext_Participant { get; private set; }

        private static GONetSessionContext mySessionContext;
        public static GONetSessionContext MySessionContext
        {
            get { return mySessionContext; }
            internal set
            {
                mySessionContext = value;
                MySessionContext_Participant = (object)mySessionContext == null ? null : mySessionContext.gameObject.GetComponent<GONetParticipant>();

                // TODO FIXME need to update the connection associated with OwnerAuthorityId
            }
        }
        public static GONetParticipant MySessionContext_Participant { get; private set; } // TODO FIXME need to spawn this for everyone and set it here!
        public static uint MyAuthorityId { get; private set; }

        internal static bool isServerOverride = NetworkUtils.IsIPAddressOnLocalMachine(Simpeesimul.serverIP) && !NetworkUtils.IsLocalPortListening(Simpeesimul.serverPort); // TODO FIXME gotta iron out good startup process..this is quite temporary
        public static bool IsServer => isServerOverride || MyAuthorityId == OwnerAuthorityId_Server; // TODO cache this since it will not change and too much processing to get now

        private static GONetServer _gonetServer; // TODO remove this once we make gonetServer private again!
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetServer gonetServer
        {
            get { return _gonetServer; }
            set
            {
                MyAuthorityId = OwnerAuthorityId_Server;
                _gonetServer = value;
                _gonetServer.ClientConnected += Server_OnClientConnected_SendClientCurrentState;
            }
        }

        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetClient gonetClient;

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdMap = new Dictionary<uint, GONetParticipant>(1000);

        public const uint OwnerAuthorityId_Unset = 0;
        public const uint OwnerAuthorityId_Server = uint.MaxValue;

        /// <summary>
        /// Only used/applicable if <see cref="IsServer"/> is true.
        /// </summary>
        private static uint server_lastAssignedAuthorityId = OwnerAuthorityId_Unset;

        internal static readonly Temporal Time = new Temporal();

        static GONetMain()
        {
            InitMessageTypeToMessageIDMap();
        }

        #region public methods

        public static void SendBytesToRemoteConnections(byte[] bytes, int bytesUsedCount, QosType qualityOfService = QosType.Reliable)
        {
            if (IsServer)
            {
                gonetServer?.SendBytesToAllClients(bytes, bytesUsedCount, qualityOfService);
            }
            else
            {
                gonetClient?.SendBytesToServer(bytes, bytesUsedCount, qualityOfService);
            }
        }

        #endregion

        #region internal methods

        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_reliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS, AutoMagicalSyncReliability.Reliable);
        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_unreliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS, AutoMagicalSyncReliability.Unreliable);

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Update()
        {
            Time.Update();

            ProcessIncomingBytes_QueuedNetworkData();

            AutoMagicalSyncProcessing_SingleGrouping itemsToProcessEveryFrame;
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_reliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_unreliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }

            if (IsServer)
            {
                gonetServer?.Update();
            }
            else
            {
                Client_SyncTimeWithServer_Initiate_IfAppropriate();
                gonetClient?.Update();
            }
        }

        #region time sync client-server-client

        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS = TimeSpan.FromSeconds(1f / 4f).Ticks;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT = (float)CLIENT_SYNC_TIME_EVERY_TICKS;
        static bool client_hasSentSyncTimeRequest;
        static DateTime client_lastSyncTimeRequestSent;
        const int CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE = 60;
        static readonly Dictionary<long, RequestMessage> client_lastFewTimeSyncsSentByUID = new Dictionary<long, RequestMessage>(CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE);
        static long client_mostRecentTimeSyncResponseSentTicks;

        /// <summary>
        /// "IfAppropriate" is to indicate this runs on a schedule....if it is not the right time, this will do nothing.
        /// </summary>
        private static void Client_SyncTimeWithServer_Initiate_IfAppropriate()
        {
            DateTime now = DateTime.Now;
            bool isAppropriate = !client_hasSentSyncTimeRequest || (now - client_lastSyncTimeRequestSent).Duration().Ticks > CLIENT_SYNC_TIME_EVERY_TICKS;
            if (isAppropriate)
            {
                client_hasSentSyncTimeRequest = true;
                client_lastSyncTimeRequestSent = now;

                { // the actual sync request:
                    RequestMessage timeSync = new RequestMessage();
                    timeSync.ElapsedTicksAtSend = Time.ElapsedTicks;

                    client_lastFewTimeSyncsSentByUID[timeSync.UID] = timeSync;
                    if (client_lastFewTimeSyncsSentByUID.Count > CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE)
                    {
                        // TODO do not let client_lastFewTimeSyncsSentByUID get larger than TIME_SYNCS_SENT_QUEUE_SIZE.....delete oldest
                    }

                    using (var memoryStream = new RecyclableMemoryStream(miscMessagesMemoryStreamManager))
                    {
                        using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                        {
                            { // header...just message type/id...well, and now time 
                                uint messageID = messageTypeToMessageIDMap[typeof(RequestMessage)];
                                bitStream.WriteUInt(messageID);

                                bitStream.WriteLong(timeSync.ElapsedTicksAtSend);
                            }

                            // body
                            bitStream.WriteLong(timeSync.UID);

                            bitStream.WriteCurrentPartialByte();

                            int bytesUsedCount = (int)memoryStream.Length;
                            byte[] bytes = valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                            Array.Copy(memoryStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                            SendBytesToRemoteConnections(bytes, bytesUsedCount, QosType.Unreliable);

                            valueChangeSerializationArrayPool.Return(bytes);
                        }
                    }
                }
            }
        }

        private static void Server_SyncTimeWithClient_Respond(long requestUID, GONetConnection connectionToClient)
        {
            using (var memoryStream = new RecyclableMemoryStream(miscMessagesMemoryStreamManager))
            {
                using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                {
                    { // header...just message type/id...well, and now time 
                        uint messageID = messageTypeToMessageIDMap[typeof(ResponseMessage)];
                        bitStream.WriteUInt(messageID);

                        bitStream.WriteLong(Time.ElapsedTicks);
                    }

                    // body
                    bitStream.WriteLong(requestUID);

                    bitStream.WriteCurrentPartialByte();

                    int bytesUsedCount = (int)memoryStream.Length;
                    byte[] bytes = valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                    Array.Copy(memoryStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                    connectionToClient.SendMessage(bytes, bytesUsedCount, QosType.Unreliable);

                    valueChangeSerializationArrayPool.Return(bytes);
                }
            }
        }

        private static void Client_SyncTimeWithServer_ProcessResponse(long requestUID, long server_elapsedTicksAtSendResponse)
        {
            RequestMessage requestMessage;
            if (client_lastFewTimeSyncsSentByUID.TryGetValue(requestUID, out requestMessage)) // if we cannot find it, we cannot really do anything....now can we?
            {
                if (server_elapsedTicksAtSendResponse > client_mostRecentTimeSyncResponseSentTicks) // only process the latest send from server, ignore older stuff
                {
                    client_mostRecentTimeSyncResponseSentTicks = server_elapsedTicksAtSendResponse;

                    long responseReceivedTicks_Client = Time.ElapsedTicks;
                    long requestSentTicks_Client = requestMessage.ElapsedTicksAtSend;
                    long rtt_ticks = responseReceivedTicks_Client - requestSentTicks_Client;
                    gonetClient.connectionToServer.RTT_Latest = (float)TimeSpan.FromTicks(rtt_ticks).TotalSeconds;
                    long assumedNetworkDelayTicks = rtt_ticks >> 1; // divide by 2
                    long newClientTimeTicks = server_elapsedTicksAtSendResponse + assumedNetworkDelayTicks;

                    Time.SetFromAuthority(newClientTimeTicks);
                }
            }
        }

        #endregion

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Shutdown()
        {
            if (IsServer)
            {
                gonetServer?.Stop();
            }
            else
            {
                gonetClient?.Disconnect();
            }
        }

        struct NetworkData
        {
            public GONetConnection sourceConnection;
            public byte[] messageBytes;
            public int bytesUsedCount;
        }

        static readonly ArrayPool<byte> incomingNetworkDataArrayPool = new ArrayPool<byte>(1000, 10, 1024, 2048);
        static readonly ConcurrentQueue<NetworkData> incomingNetworkData = new ConcurrentQueue<NetworkData>();

        public sealed class Temporal
        {
            public delegate void TimeChangeArgs(double fromElapsedSeconds, double toElapsedSeconds);
            public event TimeChangeArgs TimeSetFromAuthority;

            long baselineTicks;
            long lastSetFromAuthorityDiffTicks;
            long lastSetFromAuthorityAtTicks;

            public const double ElapsedSecondsUnset = -1;

            public long ElapsedTicks { get; private set; }
            double elapsedSeconds = ElapsedSecondsUnset;
            public double ElapsedSeconds => elapsedSeconds;

            public long UpdateCount { get; private set; } = 0;

            internal void SetFromAuthority(long elapsedTicksFromAuthority)
            {
                lastSetFromAuthorityDiffTicks = elapsedTicksFromAuthority - ElapsedTicks;

                double elapsedSecondsBefore = elapsedSeconds;

                lastSetFromAuthorityAtTicks = HighResolutionTimeUtils.Now.Ticks;
                baselineTicks = lastSetFromAuthorityAtTicks - elapsedTicksFromAuthority;
                ElapsedTicks = lastSetFromAuthorityAtTicks - baselineTicks;
                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                //* if you want debugging in log
                const string STR_ElapsedTimeClient = "ElapsedTime client of: ";
                const string STR_BeingOverwritten = " is being overwritten from authority source to: ";
                const string STR_DoubleCheck = " seconds and here is new client value to double check it worked correctly: ";
                const string STR_Diff = " lastSetFromAuthorityDiffTicks (well, as ms): ";
                GONetLog.Info(string.Concat(STR_ElapsedTimeClient, elapsedSecondsBefore, STR_BeingOverwritten, TimeSpan.FromTicks(elapsedTicksFromAuthority).TotalSeconds, STR_DoubleCheck, elapsedSeconds, STR_Diff, TimeSpan.FromTicks(lastSetFromAuthorityDiffTicks).TotalMilliseconds));
                //*/

                TimeSetFromAuthority?.Invoke(elapsedSecondsBefore, elapsedSeconds);
            }

            /// <summary>
            /// IMPORTANT: Call this every engine tick.
            /// See <see cref="UpdateCount"/> to know how many times this has been called this session.
            /// </summary>
            internal void Update()
            {
                ++UpdateCount;

                if (elapsedSeconds == ElapsedSecondsUnset)
                {
                    baselineTicks = HighResolutionTimeUtils.Now.Ticks;
                }

                long elapsedTicks_withoutEasement = HighResolutionTimeUtils.Now.Ticks - baselineTicks;
                ElapsedTicks = elapsedTicks_withoutEasement - GetTicksToSubtractForSetFromAuthorityEasing();

                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private long GetTicksToSubtractForSetFromAuthorityEasing()
            {
                if (lastSetFromAuthorityDiffTicks != 0)
                { // IMPORTANT: This code eases the adjustment (i.e., diff) back to resync time over the entire period between resyncs to avoid a possibly dramatic jump in time just after a resync!
                    long ticksSinceLastSetFromAuthority = HighResolutionTimeUtils.Now.Ticks - lastSetFromAuthorityAtTicks;
                    float inverseLerpBetweenSyncs = ticksSinceLastSetFromAuthority / CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT;
                    if (inverseLerpBetweenSyncs < 1f) // if 1 or greater there will be nothing to add based on calculations
                    {
                        return (long)(lastSetFromAuthorityDiffTicks * (1f - inverseLerpBetweenSyncs));
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// All incoming network bytes need to come here.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread.
        /// </summary>
        internal static void ProcessIncomingBytes(GONetConnection sourceConnection, byte[] messageBytes, int bytesUsedCount)
        {
            NetworkData networkData = new NetworkData()
            {
                sourceConnection = sourceConnection,
                messageBytes = incomingNetworkDataArrayPool.Borrow(bytesUsedCount), // TODO FIXME there needs to be one pool per incoming thread!..and then return to correct one too!
                bytesUsedCount = bytesUsedCount
            };

            Buffer.BlockCopy(messageBytes, 0, networkData.messageBytes, 0, bytesUsedCount);

            incomingNetworkData.Enqueue(networkData);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Call this from the main Unity thread!
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData()
        {
            NetworkData networkData;
            int count = incomingNetworkData.Count;
            for (int i = 0; i < count && !incomingNetworkData.IsEmpty; ++i)
            {
                if (incomingNetworkData.TryDequeue(out networkData))
                {
                    using (var memoryStream = new MemoryStream(networkData.messageBytes))
                    {
                        using (var bitStream = new Utils.BitStream(memoryStream))
                        {
                            Type messageType;
                            ////////////////////////////////////////////////////////////////////////////
                            // header...just message type/id...well, now it is send time too
                            uint messageID;
                            bitStream.ReadUInt(out messageID);
                            messageType = messageTypeByMessageIDMap[messageID];

                            long elapsedTicksAtSend;
                            bitStream.ReadLong(out elapsedTicksAtSend);
                            ////////////////////////////////////////////////////////////////////////////


                            {  // body:
                                if (messageType == typeof(AutoMagicalSync_ValueChangesMessage))
                                {
                                    DeserializeBody_ChangesBundle(bitStream, networkData.sourceConnection);
                                }
                                else if (messageType == typeof(RequestMessage))
                                {
                                    long requestUID;
                                    bitStream.ReadLong(out requestUID);

                                    Server_SyncTimeWithClient_Respond(requestUID, networkData.sourceConnection);
                                }
                                else if (messageType == typeof(ResponseMessage))
                                {
                                    long requestUID;
                                    bitStream.ReadLong(out requestUID);

                                    Client_SyncTimeWithServer_ProcessResponse(requestUID, elapsedTicksAtSend);
                                }
                                else if (messageType == typeof(OwnerAuthorityIdAssignmentMessage)) // this should be the first message ever received....but since only sent once per client, do not put it first in the if statements list of message type check
                                {
                                    uint ownerAuthorityId;
                                    bitStream.ReadUInt(out ownerAuthorityId);

                                    if (!IsServer) // this only applied to clients....should NEVER happen on server
                                    {
                                        MyAuthorityId = ownerAuthorityId;
                                    } // else log warning?
                                } // else?  TODO lookup proper deserialize method instead of if-else-if statement(s)
                            }
                        }
                    }

                    // TODO this should only deserialize the message....and then send over to an EventBus where subscribers to that event/message from the bus can process accordingly

                    incomingNetworkDataArrayPool.Return(networkData.messageBytes); // TODO FIXME there needs to be one pool per incoming thread!..and then return to correct one too!
                }
                else
                {
                    GONetLog.Warning("Trying to dequeue from queued up incoming network data elements and cannot....WHY?");
                }
            }
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            Server_AssignNewClientAuthorityId(gonetConnection_ServerToClient);

            Server_SendClientCurrentState_AllAutoMagicalSync(gonetConnection_ServerToClient);
        }

        private static void Server_AssignNewClientAuthorityId(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            // first assign locally
            gonetConnection_ServerToClient.OwnerAuthorityId = ++server_lastAssignedAuthorityId;

            // then send the assignment to the client
            using (var memoryStream = new RecyclableMemoryStream(miscMessagesMemoryStreamManager))
            {
                using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                {
                    { // header...just message type/id...well, and now time 
                        uint messageID = messageTypeToMessageIDMap[typeof(OwnerAuthorityIdAssignmentMessage)];
                        bitStream.WriteUInt(messageID);

                        bitStream.WriteLong(Time.ElapsedTicks);
                    }

                    { // body
                        bitStream.WriteUInt(gonetConnection_ServerToClient.OwnerAuthorityId);
                    }

                    bitStream.WriteCurrentPartialByte();

                    gonetConnection_ServerToClient.SendMessage(memoryStream.GetBuffer(), (int)memoryStream.Length, QosType.Reliable);
                }
            }
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetId = GONetParticipant.GONetId_Unset;
        /// <summary>
        /// For every runtime instance of <see cref="GONetParticipant"/>, there will be one and only one item in one and only one of the <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/>'s <see cref="Dictionary{TKey, TValue}.Values"/>.
        /// The key into this is the <see cref="GONetParticipant.codeGenerationId"/>.
        /// TODO: once implementation supports it, this replaces <see cref="autoSyncMemberDataByGONetParticipantMap"/> and make sure to remove it.
        /// </summary>
        static readonly Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> activeAutoSyncCompanionsByCodeGenerationIdMap = new Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>>(byte.MaxValue);
        static readonly Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping> autoSyncProcessingSupportByFrequencyMap = new Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping>(5);

        internal class AutoMagicalSync_ValueMonitoringSupport_ChangedValue
        {
            internal byte index;
            internal GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;
            
            #region properties copied off of GONetAutoMagicalSyncAttribute
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority_GONetInternalOverride"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority_GONetInternalOverride;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>
            /// </summary>
            internal float syncAttribute_SyncChangesEverySeconds;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.Reliability"/>
            /// </summary>
            internal AutoMagicalSyncReliability syncAttribute_Reliability;
            #endregion

            internal object lastKnownValue;
            internal object lastKnownValue_previous;

            /// <summary>
            /// This used to keep track of who (i.e., which network owner authority) made the last change to the value.
            /// It is used to know if we need to avoid sending the value to anyone or not (i.e., do not send to owner who made the change...that would be redundant, unnecessary and unwanted traffic/processing).
            /// NOTE: When this is set to <see cref="GONetParticipant.OwnerAuthorityId_Unset"/>, the ASSumption is that it was set locally by "me."
            /// </summary>
            internal uint lastKnownValue_SetByAuthorityId = OwnerAuthorityId_Unset;

            /// <summary>
            /// DO NOT USE THIS.
            /// Public default constructor is required for object pool instantiation under current impl of <see cref="ObjectPool{T}"/>;
            /// </summary>
            public AutoMagicalSync_ValueMonitoringSupport_ChangedValue() { }

            internal AutoMagicalSync_ValueMonitoringSupport_ChangedValue(
                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion,
                byte index,
                int syncAttribute_ProcessingPriority,
                int syncAttribute_ProcessingPriority_GONetInternalOverride)
            {
                this.syncCompanion = syncCompanion;
                this.index = index;
                this.syncAttribute_ProcessingPriority = syncAttribute_ProcessingPriority;
                this.syncAttribute_ProcessingPriority_GONetInternalOverride = syncAttribute_ProcessingPriority_GONetInternalOverride;
            }
        }

        /// <summary>
        /// Only (re)used in <see cref="OnEnable_StartMonitoringForAutoMagicalNetworking"/>.
        /// </summary>
        static readonly HashSet<SyncBundleUniqueGrouping> uniqueSyncGroupings = new HashSet<SyncBundleUniqueGrouping>();

        internal struct SyncBundleUniqueGrouping
        {
            internal readonly float scheduleFrequency;
            internal readonly AutoMagicalSyncReliability reliability;

            internal SyncBundleUniqueGrouping(float scheduleFrequency, AutoMagicalSyncReliability reliability)
            {
                this.scheduleFrequency = scheduleFrequency;
                this.reliability = reliability;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SyncBundleUniqueGrouping))
                {
                    return false;
                }

                var grouping = (SyncBundleUniqueGrouping)obj;
                return scheduleFrequency == grouping.scheduleFrequency &&
                       reliability == grouping.reliability;
            }

            public override int GetHashCode()
            {
                var hashCode = 460550935;
                hashCode = hashCode * -1521134295 + scheduleFrequency.GetHashCode();
                hashCode = hashCode * -1521134295 + reliability.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnEnable method.
        /// </summary>
        internal static void OnEnable_StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                GONetLog.Debug("OnEnable");

                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
                    if (!activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions))
                    {
                        autoSyncCompanions = new Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                        activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId] = autoSyncCompanions;
                    }
                    GONetParticipant_AutoMagicalSyncCompanion_Generated companion = GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.CreateInstance(gonetParticipant);
                    autoSyncCompanions[gonetParticipant] = companion;

                    uniqueSyncGroupings.Clear();
                    for (int i = 0; i < companion.valuesCount; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = companion.valuesChangesSupport[i];
                        SyncBundleUniqueGrouping grouping = new SyncBundleUniqueGrouping(monitoringSupport.syncAttribute_SyncChangesEverySeconds, monitoringSupport.syncAttribute_Reliability);
                        uniqueSyncGroupings.Add(grouping); // since it is a set, duplicates will be discarded
                    }
                    foreach (SyncBundleUniqueGrouping uniqueSyncGrouping in uniqueSyncGroupings)
                    {
                        if (!autoSyncProcessingSupportByFrequencyMap.ContainsKey(uniqueSyncGrouping))
                        {
                            var autoSyncProcessingSupport = 
                                new AutoMagicalSyncProcessing_SingleGrouping(uniqueSyncGrouping, activeAutoSyncCompanionsByCodeGenerationIdMap); // IMPORTANT: this starts the thread!

                            autoSyncProcessingSupportByFrequencyMap[uniqueSyncGrouping] = autoSyncProcessingSupport;
                        }
                    }
                }

                AssignGONetId_IfAppropriate(gonetParticipant);
            }
        }

        private static void AssignGONetId_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (IsServer)
            {
                gonetParticipant.GONetId = ++lastAssignedGONetId;
            }

            // TODO move this to OnInstantiated type deal:  becasue this is not every doing anything right now anyway..just leaving in so find all references sees it!!
            else
            {
                bool isInstantiated = false; // TODO FIXME have to figure out if this is happening as a result of a spawn/instantiate or the related GO is in the scene.
                if (isInstantiated)
                {
                    gonetParticipant.OwnerAuthorityId = MyAuthorityId;
                } // else the server will do the assigning once it processes this in the scene load up and the value will propogate to clients via the auto-magical sync
            }
        }

        internal static void AssignOwnerAuthorityIds_IfAppropriate(List<GONetParticipant> gonetParticipantsInConsideration)
        {
            if (IsServer)
            {
                MyAuthorityId = OwnerAuthorityId_Server; // NOTE: at time of writing, MyAuthorityId is not set quite yet, which is why we go ahead and manually set here

                int count = gonetParticipantsInConsideration.Count;
                for (int i = 0; i < count; ++i)
                {
                    GONetParticipant item = gonetParticipantsInConsideration[i];
                    item.OwnerAuthorityId = MyAuthorityId;
                }
            }
        }

        /// <summary>
        /// Just a helper data structure just for use in <see cref="ProcessAutoMagicalSyncStuffs(bool, ReliableEndpoint)"/>
        /// </summary>
        static readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);

        static void Server_SendClientCurrentState_AllAutoMagicalSync(ReliableEndpoint connectionToClient)
        {
            syncValuesToSend.Clear();

            var enumeratorOuter = activeAutoSyncCompanionsByCodeGenerationIdMap.GetEnumerator();
            while (enumeratorOuter.MoveNext())
            {
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                var enumeratorInner = currentMap.GetEnumerator();
                while (enumeratorInner.MoveNext())
                {
                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;
                    monitoringSupport.UpdateLastKnownValues(); // need to call this for every single one to keep track of changes
                    if (connectionToClient != null)
                    {
                        // THOUGHT: can we just loop over all gonet participants and serialize all via its companion instead of each individual here? ... one reason answer is no for now is ensureing the order of priority is adhered to (e.g., GONetId processes very first!!!)
                        monitoringSupport.AppendListWithAllValues(syncValuesToSend);
                    }
                    else if (monitoringSupport.HaveAnyValuesChangedSinceLastCheck())
                    {
                        monitoringSupport.AppendListWithChangesSinceLastCheck(syncValuesToSend);
                        monitoringSupport.OnValueChangeCheck_Reset();
                    }
                }
            }

            if (syncValuesToSend.Count > 0)
            {
                int bytesUsedCount;
                if (connectionToClient == null)
                { // if in here, we are sending only changes (since last send) to everyone
                    //GONetLog.Debug("sending changed auto-magical sync values to all connections");
                    if (IsServer)
                    {
                        // if its the server, we have to consider who we are sending to and ensure we do not send then changes that initially came from them!
                        gonetServer?.ForEachClient((clientConnection) =>
                        {
                            byte[] changesSerialized_clientSpecific = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount, clientConnection.OwnerAuthorityId);
                            clientConnection.SendMessage(changesSerialized_clientSpecific, bytesUsedCount, QosType.Reliable);
                            valueChangeSerializationArrayPool.Return(changesSerialized_clientSpecific);
                        });
                    }
                    else
                    {
                        byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount, OwnerAuthorityId_Server); // don't send anything the server sent to us back to the server
                        SendBytesToRemoteConnections(changesSerialized, bytesUsedCount);
                        valueChangeSerializationArrayPool.Return(changesSerialized);
                    }
                }
                else
                {
                    byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount);
                    connectionToClient.SendMessage(changesSerialized, bytesUsedCount, QosType.Reliable);
                    valueChangeSerializationArrayPool.Return(changesSerialized);
                }
            }
        }

        /// <summary>
        /// For every unique value encountered for <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>, an instance of this 
        /// class will be created and used to process only those fields/properties set to be sync'd on that frequency.
        /// </summary>
        internal sealed class AutoMagicalSyncProcessing_SingleGrouping
        {
            Thread thread;
            volatile bool isThreadRunning;
            volatile bool shouldProcessASAP = false;
            long lastProcessCompleteTicks;

            static readonly long END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS = TimeSpan.FromSeconds(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS).Ticks;

            SyncBundleUniqueGrouping uniqueGrouping;
            long scheduleFrequencyTicks;
            Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency;
            QosType uniqueGrouping_qualityOfService;

            /// <summary>
            /// Indicates whether or not <see cref="ProcessASAP"/> must be called (manually) from an outside part in order for sync processing to occur.
            /// </summary>
            internal bool DoesRequireManualProcessInitiation => scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS;

            /// <summary>
            /// Just a helper data structure just for use in <see cref="ProcessAutoMagicalSyncStuffs(bool, ReliableEndpoint)"/>
            /// </summary>
            readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);

            /// <summary>
            /// IMPORTANT: If a value of <see cref="AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS"/> is passed in here for <paramref name="scheduleFrequency"/>,
            ///            then nothing will happen in here automatically....<see cref="GONetMain"/> or some other party will have to manually call <see cref="ProcessASAP"/>.
            /// </summary>
            internal AutoMagicalSyncProcessing_SingleGrouping(SyncBundleUniqueGrouping uniqueGrouping, Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency)
            {
                this.uniqueGrouping = uniqueGrouping;
                scheduleFrequencyTicks = TimeSpan.FromSeconds(uniqueGrouping.scheduleFrequency).Ticks;
                uniqueGrouping_qualityOfService = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? QosType.Reliable : QosType.Unreliable;

                this.everythingMap_evenStuffNotOnThisScheduleFrequency = everythingMap_evenStuffNotOnThisScheduleFrequency;

                thread = new Thread(ContinuallyProcess);
                isThreadRunning = true;
                thread.Start();
            }

            ~AutoMagicalSyncProcessing_SingleGrouping()
            {
                isThreadRunning = false;
                thread.Abort();
            }

            private void ContinuallyProcess()
            {
                bool doesRequireManualProcessInitiation = DoesRequireManualProcessInitiation;
                while (isThreadRunning)
                {
                    if (doesRequireManualProcessInitiation && !shouldProcessASAP)
                    {
                        Thread.Sleep(1); // TODO come up with appropriate sleep time/value 
                    }
                    else
                    {
                        { // process:
                            // loop over everythingMap_evenStuffNotOnThisScheduleFrequency only processing the items inside that match scheduleFrequency
                            syncValuesToSend.Clear();

                            var enumeratorOuter = everythingMap_evenStuffNotOnThisScheduleFrequency.GetEnumerator();
                            while (enumeratorOuter.MoveNext())
                            {
                                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                                var enumeratorInner = currentMap.GetEnumerator();
                                while (enumeratorInner.MoveNext())
                                {
                                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;

                                    // need to call this for every single one to keep track of changes, BUT we only want to consider/process ones that match the current frequency:
                                    monitoringSupport.UpdateLastKnownValues(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    if (monitoringSupport.HaveAnyValuesChangedSinceLastCheck(uniqueGrouping)) // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    {
                                        monitoringSupport.AppendListWithChangesSinceLastCheck(syncValuesToSend, uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                        monitoringSupport.OnValueChangeCheck_Reset(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    }
                                }
                            }

                            if (syncValuesToSend.Count > 0)
                            {
                                int bytesUsedCount;
                                //GONetLog.Debug("sending changed auto-magical sync values to all connections");
                                if (IsServer)
                                {
                                    // if its the server, we have to consider who we are sending to and ensure we do not send then changes that initially came from them!
                                    gonetServer?.ForEachClient((clientConnection) =>
                                    {
                                        byte[] changesSerialized_clientSpecific = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount, clientConnection.OwnerAuthorityId);
                                        clientConnection.SendMessage(changesSerialized_clientSpecific, bytesUsedCount, uniqueGrouping_qualityOfService);
                                        valueChangeSerializationArrayPool.Return(changesSerialized_clientSpecific);
                                    });
                                }
                                else
                                {
                                    byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount, OwnerAuthorityId_Server); // don't send anything the server sent to us back to the server
                                    SendBytesToRemoteConnections(changesSerialized, bytesUsedCount, qualityOfService: uniqueGrouping_qualityOfService);
                                    valueChangeSerializationArrayPool.Return(changesSerialized);
                                }
                            }

                            shouldProcessASAP = false; // reset this
                        }

                        if (!doesRequireManualProcessInitiation)
                        { // (auto sync) frequency control:
                            long nextProcessStartTicks = lastProcessCompleteTicks + scheduleFrequencyTicks;
                            long nowTicks = HighResolutionTimeUtils.Now.Ticks;
                            lastProcessCompleteTicks = nowTicks;
                            long ticksToSleep = nextProcessStartTicks - nowTicks;
                            if (ticksToSleep > 0)
                            {
                                Thread.Sleep(TimeSpan.FromTicks(ticksToSleep));
                            }
                        }
                    }
                }
            }

            internal void ProcessASAP()
            {
                shouldProcessASAP = true;
            }
        }

        static readonly RecyclableMemoryStreamManager miscMessagesMemoryStreamManager = new RecyclableMemoryStreamManager();
        static readonly RecyclableMemoryStreamManager valueChangesMemoryStreamManager = new RecyclableMemoryStreamManager();
        static readonly ArrayPool<byte> valueChangeSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);

        static readonly Dictionary<Type, uint> messageTypeToMessageIDMap = new Dictionary<Type, uint>(4096);
        static readonly Dictionary<uint, Type> messageTypeByMessageIDMap = new Dictionary<uint, Type>(4096);
        static uint nextMessageID;

        private static void InitMessageTypeToMessageIDMap()
        {
            try
            {
                foreach (var types in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName)
                        .Select(a => a.GetTypes().Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(IGONetEvent)) && !t.IsAbstract).OrderBy(t2 => t2.FullName)))
                {
                    foreach (var type in types)
                    {
                        uint messageID = nextMessageID++;
                        messageTypeToMessageIDMap[type] = messageID;
                        messageTypeByMessageIDMap[messageID] = type;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex); // since our log stuffs does not work in static context within unity editor, use unity logging for this one
            }
        }

        /// <summary>
        /// PRE: <paramref name="changes"/> size is greater than 0
        /// IMPORTANT: The caller is responsible for returning the returned byte[] to <see cref="valueChangeSerializationArrayPool"/>!
        /// </summary>
        private static byte[] SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, out int bytesUsedCount, uint doNotSendIfThisAuthorityId = OwnerAuthorityId_Unset)
        {
            using (var memoryStream = new RecyclableMemoryStream(valueChangesMemoryStreamManager))
            {
                using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                {
                    { // header...just message type/id...well, and now time 
                        uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChangesMessage)];
                        bitStream.WriteUInt(messageID);

                        bitStream.WriteLong(Time.ElapsedTicks);
                    }

                    SerializeBody_ChangesBundle(changes, bitStream, doNotSendIfThisAuthorityId); // body

                    bitStream.WriteCurrentPartialByte();

                    bytesUsedCount = (int)memoryStream.Length;
                    byte[] bytes = valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                    Array.Copy(memoryStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                    return bytes;
                }
            }
        }

        class AutoMagicalSyncChangePriorityComparer : IComparer<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>
        {
            internal static readonly AutoMagicalSyncChangePriorityComparer Instance = new AutoMagicalSyncChangePriorityComparer();

            private AutoMagicalSyncChangePriorityComparer() { }

            public int Compare(AutoMagicalSync_ValueMonitoringSupport_ChangedValue x, AutoMagicalSync_ValueMonitoringSupport_ChangedValue y)
            {
                int xPriority = x.syncAttribute_ProcessingPriority_GONetInternalOverride != 0 ? x.syncAttribute_ProcessingPriority_GONetInternalOverride : x.syncAttribute_ProcessingPriority;
                int yPriority = y.syncAttribute_ProcessingPriority_GONetInternalOverride != 0 ? y.syncAttribute_ProcessingPriority_GONetInternalOverride : y.syncAttribute_ProcessingPriority;

                return yPriority.CompareTo(xPriority); // descending...highest priority first!
            }
        }

        private static void SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitStream bitStream_headerAlreadyWritten, uint doNotSendIfThisAuthorityId = OwnerAuthorityId_Unset)
        {
            int countTotal = changes.Count;
            int countFiltered = 0;
            if (doNotSendIfThisAuthorityId == OwnerAuthorityId_Unset)
            {
                countFiltered = countTotal;
            }
            else
            {
                // filter out changes that are for doNotSendIfThisAuthorityId
                countFiltered = changes.Count(change => change.lastKnownValue_SetByAuthorityId != doNotSendIfThisAuthorityId);
            }

            bitStream_headerAlreadyWritten.WriteUShort((ushort)countFiltered);
            //GONetLog.Debug(string.Concat("about to send changes bundle...countFiltered: " + countFiltered));

            changes.Sort(AutoMagicalSyncChangePriorityComparer.Instance);

            for (int i = 0; i < countTotal; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = changes[i];
                if (doNotSendIfThisAuthorityId != OwnerAuthorityId_Unset && monitoringSupport.lastKnownValue_SetByAuthorityId == doNotSendIfThisAuthorityId)
                {
                    monitoringSupport.lastKnownValue_SetByAuthorityId = OwnerAuthorityId_Unset; // reset this as it served its purpose
                    continue; // skip this guy (i.e., apply the "filter")
                }

                bool canASSumeNetId = monitoringSupport.index == GONetParticipant.ASSumed_GONetId_INDEX;
                bitStream_headerAlreadyWritten.WriteBit(canASSumeNetId);
                if (canASSumeNetId)
                {
                    // this will use GONetId_InitialAssignment_CustomSerializer and write the full unique path and the gonetId:
                    monitoringSupport.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, GONetParticipant.ASSumed_GONetId_INDEX);
                }
                else
                {
                    bitStream_headerAlreadyWritten.WriteUInt(monitoringSupport.syncCompanion.gonetParticipant.GONetId); // have to write the gonetid first before each changed value
                    bitStream_headerAlreadyWritten.WriteByte(monitoringSupport.index); // then have to write the index, otherwise other end does not know which index to deserialize
                    monitoringSupport.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, monitoringSupport.index);
                }
            }
        }

        private static void DeserializeBody_ChangesBundle(Utils.BitStream bitStream_headerAlreadyRead, GONetConnection sourceOfChangeConnection)
        {
            ushort count;
            bitStream_headerAlreadyRead.ReadUShort(out count);
            //GONetLog.Debug(string.Concat("about to read changes bundle...count: " + count));
            for (int i = 0; i < count; ++i)
            {
                bool canASSumeNetId;
                bitStream_headerAlreadyRead.ReadBit(out canASSumeNetId);
                if (canASSumeNetId)
                {
                    GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead);
                }
                else
                {
                    uint gonetId;
                    bitStream_headerAlreadyRead.ReadUInt(out gonetId);

                    GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = companionMap[gonetParticipant];

                    byte index = (byte)bitStream_headerAlreadyRead.ReadByte();
                    syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index);

                    syncCompanion.valuesChangesSupport[index].lastKnownValue_SetByAuthorityId = sourceOfChangeConnection.OwnerAuthorityId; // keep track of the authority of every change!
                }
            }
            //GONetLog.Debug(string.Concat("************done reading changes bundle"));
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnDisable method.
        /// </summary>
        internal static void OnDisable_StopMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                    if (!autoSyncCompanions.Remove(gonetParticipant))
                    {
                        const string PORK = "Expecting to find active auto-sync companion in order to de-active/remove it upon gonetParticipant.OnDisable, but did not. gonetParticipant.GONetId: ";
                        const string NAME = " gonetParticipant.gameObject.name: ";
                        GONetLog.Warning(string.Concat(PORK, gonetParticipant.GONetId, NAME, gonetParticipant.gameObject.name));
                    }
                }

                // do we need to send event to disable this thing?
            }
        }

        #endregion
    }
}
