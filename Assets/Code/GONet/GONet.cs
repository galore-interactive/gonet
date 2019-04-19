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
            }
        }
        public static GONetParticipant MySessionContext_Participant { get; private set; }
        public static uint MyAuthorityId => MySessionContext_Participant.OwnerAuthorityId;

        internal static bool isServerOverride = NetworkUtils.IsIPAddressOnLocalMachine(Simpeesimul.serverIP) && !NetworkUtils.IsLocalPortListening(Simpeesimul.serverPort); // TODO FIXME gotta iron out good startup process..this is quite temporary
        public static bool IsServer => isServerOverride || MySessionContext_Participant?.OwnerAuthorityId == GONetParticipant.OwnerAuthorityId_Server; // TODO cache this since it will not change and too much processing to get now

        private static GONetServer _gonetServer; // TODO remove this once we make gonetServer private again!
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetServer gonetServer
        {
            get { return _gonetServer; }
            set
            {
                _gonetServer = value;
                _gonetServer.ClientConnected += Server_OnClientConnected_SendClientCurrentState;
            }
        }

        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetClient gonetClient;

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdMap = new Dictionary<uint, GONetParticipant>(1000);

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
            public ReliableEndpoint sourceEndpoint;
            public byte[] messageBytes;
            public int bytesUsedCount;
        }

        static readonly ArrayPool<byte> incomingNetworkDataArrayPool = new ArrayPool<byte>(1000, 10, 1024, 2048);
        static readonly ConcurrentQueue<NetworkData> incomingNetworkData = new ConcurrentQueue<NetworkData>();

        /// <summary>
        /// All incoming network bytes need to come here.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread.
        /// </summary>
        internal static void ProcessIncomingBytes(ReliableEndpoint sourceEndpoint, byte[] messageBytes, int bytesUsedCount)
        {
            NetworkData networkData = new NetworkData()
            {
                sourceEndpoint = sourceEndpoint,
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
            NetworkData tmp;
            int count = incomingNetworkData.Count;
            for (int i = 0; i < count && !incomingNetworkData.IsEmpty; ++i)
            {
                if (incomingNetworkData.TryDequeue(out tmp))
                {
                    using (var memoryStream = new MemoryStream(tmp.messageBytes))
                    {
                        using (var bitStream = new Utils.BitStream(memoryStream))
                        {
                            // header...just message type/id
                            uint messageID;
                            bitStream.ReadUInt(out messageID);

                            Type messageType = messageTypeByMessageIDMap[messageID];
                            if (messageType == typeof(AutoMagicalSync_ValueChangesMessage))
                            {
                                DeserializeBody_ChangesBundle(bitStream);
                            } // else?  TODO lookup proper deserialize method instead of if-else-if statement(s)
                        }
                    }

                    // TODO this should only deserialize the message....and then send over to an EventBus where subscribers to that event/message from the bus can process accordingly

                    incomingNetworkDataArrayPool.Return(tmp.messageBytes);
                }
                else
                {
                    GONetLog.Warning("Trying to dequeue from queued up incoming network data elements and cannot....WHY?");
                }
            }
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            ProcessAutoMagicalSyncStuffs(true, gonetConnection_ServerToClient);
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
                gonetParticipant.OwnerAuthorityId = GONetParticipant.OwnerAuthorityId_Server;
            }
            else
            {
                bool isInstantiated = false; // TODO FIXME have to figure out if this is happening as a result of a spawn/instantiate or the related GO is in the scene.
                if (isInstantiated)
                {
                    gonetParticipant.OwnerAuthorityId = MyAuthorityId;
                } // else the server will do the assigning once it processes this in the scene load up and the value will propogate to clients via the auto-magical sync
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
        /// ...to all remote connections (or just to <paramref name="onlySendToEndpoint"/> if not null)
        /// </summary>
        static void ProcessAutoMagicalSyncStuffs(bool isProcessingAllStateRegardlessOfChange = false, ReliableEndpoint onlySendToEndpoint = null)
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
                    monitoringSupport.UpdateLastKnownValues();
                    if (isProcessingAllStateRegardlessOfChange || monitoringSupport.HaveAnyValuesChangedSinceLastCheck())
                    {
                        monitoringSupport.AppendListWithChangesSinceLastCheck(syncValuesToSend);

                        if (!isProcessingAllStateRegardlessOfChange)
                        {
                            monitoringSupport.OnValueChangeCheck_Reset();
                        }
                    }
                }
            }

            if (syncValuesToSend.Count > 0)
            {
                int bytesUsedCount;
                byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, out bytesUsedCount);

                if (onlySendToEndpoint == null)
                {
                    GONetLog.Debug("sending changed auto-magical sync values to all connections");
                    SendBytesToRemoteConnections(changesSerialized, bytesUsedCount);
                }
                else
                {
                    onlySendToEndpoint.SendMessage(changesSerialized, bytesUsedCount, QosType.Reliable);
                }

                valueChangeSerializationArrayPool.Return(changesSerialized);
            }
        }

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
        private static byte[] SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, out int bytesUsedCount)
        {
            using (var memoryStream = new RecyclableMemoryStream(valueChangesMemoryStreamManager))
            {
                using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                {
                    { // header...just message type/id
                        uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChangesMessage)];
                        bitStream.WriteUInt(messageID);
                    }

                    SerializeBody_ChangesBundle(changes, bitStream); // body

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

        private static void SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitStream bitStream_headerAlreadyWritten)
        {
            int count = changes.Count;
            bitStream_headerAlreadyWritten.WriteUShort((ushort)count);
            GONetLog.Debug(string.Concat("about to send changes bundle...count: " + count));

            changes.Sort(AutoMagicalSyncChangePriorityComparer.Instance);

            for (int i = 0; i < count; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = changes[i];

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

        private static void DeserializeBody_ChangesBundle(Utils.BitStream bitStream_headerAlreadyRead)
        {
            ushort count;
            bitStream_headerAlreadyRead.ReadUShort(out count);
            GONetLog.Debug(string.Concat("about to read changes bundle...count: " + count));
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
                }
            }
            GONetLog.Debug(string.Concat("************done reading changes bundle"));
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
