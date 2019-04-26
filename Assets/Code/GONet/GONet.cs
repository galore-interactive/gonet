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

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Update()
        {
            ProcessIncomingBytes_QueuedNetworkData();
            ProcessAutoMagicalSyncStuffs(); // TODO may want to call this at end of frame in same frame as the other MonoBehaviour instances in teh game have run to make changes...send out same frame as opposed to "beginning" of next frame?

            if (IsServer)
            {
                gonetServer?.Update();
            }
            else
            {
                gonetClient?.Update();
            }
        }

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

        /// <summary>
        /// All incoming network bytes need to come here.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread.
        /// </summary>
        internal static void ProcessIncomingBytes(GONetConnection sourceConnection, byte[] messageBytes, int bytesUsedCount)
        {
            NetworkData networkData = new NetworkData()
            {
                sourceConnection = sourceConnection,
                messageBytes = incomingNetworkDataArrayPool.Borrow(bytesUsedCount),
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
                            // header...just message type/id
                            uint messageID;
                            bitStream.ReadUInt(out messageID);

                            Type messageType = messageTypeByMessageIDMap[messageID];
                            if (messageType == typeof(AutoMagicalSync_ValueChangesMessage))
                            {
                                DeserializeBody_ChangesBundle(bitStream, networkData.sourceConnection);
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

                    // TODO this should only deserialize the message....and then send over to an EventBus where subscribers to that event/message from the bus can process accordingly

                    incomingNetworkDataArrayPool.Return(networkData.messageBytes);
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

            ProcessAutoMagicalSyncStuffs(gonetConnection_ServerToClient);
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
                    { // header...just message type/id
                        uint messageID = messageTypeToMessageIDMap[typeof(OwnerAuthorityIdAssignmentMessage)];
                        bitStream.WriteUInt(messageID);
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

        internal class AutoMagicalSync_ValueMonitoringSupport_ChangedValue
        {
            internal byte index;
            internal GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority_GONetInternalOverride"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority_GONetInternalOverride;

            internal object lastKnownValue;
            internal object lastKnownValue_previous;

            /// <summary>
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

        /// <summary>
        /// Determines what has changed since last call (and stores it in <see cref="syncValuesToSend"/>)
        /// and then, depending on the value of <paramref name="isProcessingAllStateRegardlessOfChange"/>, 
        /// 
        /// either (if true) sends all current values...
        /// or (if false) sends only the changed things...
        /// 
        /// ...to all remote connections (or just to <paramref name="sendAllCurrentValuesToOnlyThisConnection"/> if not null)
        /// </summary>
        static void ProcessAutoMagicalSyncStuffs(ReliableEndpoint sendAllCurrentValuesToOnlyThisConnection = null)
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
                    if (sendAllCurrentValuesToOnlyThisConnection != null)
                    {
                        // TODO can we just loop over all gonet participants and serialize all via its companion instead of each individual here? ... one reason answer is no for now is ensureing the order of priority is adhered to (e.g., GONetId processes very first!!!)
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
                if (sendAllCurrentValuesToOnlyThisConnection == null)
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
                    sendAllCurrentValuesToOnlyThisConnection.SendMessage(changesSerialized, bytesUsedCount, QosType.Reliable);
                    valueChangeSerializationArrayPool.Return(changesSerialized);
                }
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
                    { // header...just message type/id
                        uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChangesMessage)];
                        bitStream.WriteUInt(messageID);
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
