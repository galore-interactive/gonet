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

#if CSHARP_7_3_OR_NEWER
            GetAddressOfField_Example.Go();
            NotSoSoftCour.FreeAllGCHandles();
#endif
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
        static readonly Dictionary<GONetParticipant, List<AutoMagicalSync_ValueMonitoringSupport>> autoSyncMemberDataByGONetParticipantMap = new Dictionary<GONetParticipant, List<AutoMagicalSync_ValueMonitoringSupport>>(1000);

        internal class AutoMagicalSync_ValueMonitoringSupport
        {
            /// <summary>
            /// NOTE: The list this is an index for is a list value inside <see cref="autoSyncMemberDataByGONetParticipantMap"/>.
            /// IMPORTANT: The list it indexes therein MUST be predictably ordered on all sides of the networking fence in order for this to be useful!
            /// </summary>
            internal uint indexInList;
            internal GONetParticipant gonetParticipant;
            internal MonoBehaviour syncMemberOwner;
            internal MemberInfo syncMember;
            internal Type syncMemberValueType;
            internal GONetAutoMagicalSyncAttribute syncAttribute;
            internal object lastKnownValue;
            internal object lastKnownValue_previous;
#if CSHARP_7_3_OR_NEWER
            internal IntPtr? syncMemberAddress_fieldOnly;
#endif
            internal bool hasValueChangedSinceLastSync;

            internal AutoMagicalSync_ValueMonitoringSupport(uint indexInList, GONetParticipant gonetParticipant, MonoBehaviour syncMemberOwner, MemberInfo syncMember, GONetAutoMagicalSyncAttribute syncAttribute)
            {
                this.indexInList = indexInList;
                this.gonetParticipant = gonetParticipant;
                this.syncMemberOwner = syncMemberOwner;
                this.syncMember = syncMember;
                this.syncAttribute = syncAttribute;

                syncMemberValueType = syncMember.MemberType == MemberTypes.Field ? ((FieldInfo)syncMember).FieldType : ((PropertyInfo)syncMember).PropertyType;

#if CSHARP_7_3_OR_NEWER
                if (syncMember.MemberType == MemberTypes.Field)
                {
                    //syncMemberAddress_fieldOnly = NotSoSoftCour.GetAddressOfField(syncMemberOwner, syncMember.Name);
                }
#endif

                UpdateLastKnownValue();
            }

            internal void UpdateLastKnownValue() // TODO FIXME need to use some code generation up in this piece for increased runtime/execution performance instead of reflection herein
            {
                lastKnownValue_previous = lastKnownValue;
                lastKnownValue = syncMember.MemberType == MemberTypes.Property
                                    ? ((PropertyInfo)syncMember).GetValue(syncMemberOwner)
                                    : ((FieldInfo)syncMember).GetValue(syncMemberOwner); // ASSuming field here since only field and property allowed

                hasValueChangedSinceLastSync = !Equals(lastKnownValue, lastKnownValue_previous); // NOTE: using != must be somehow not comparing values and instead comparing memory addresses because of object declaration even though if they are floats they have same value

#if CSHARP_7_3_OR_NEWER
                if (syncMemberAddress_fieldOnly.HasValue && syncMemberValueType == typeof(float))
                {
                    unsafe
                    {
                        float* valuePointer = (float*)syncMemberAddress_fieldOnly.Value;
                        GONetLog.Debug(string.Concat("Current Value: ", *valuePointer));
                    }
                }
#endif
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
                    MonoBehaviour[] monoBehaviours = gonetParticipant.gameObject.GetComponents<MonoBehaviour>();
                    List<AutoMagicalSync_ValueMonitoringSupport> monitoringSupports = new List<AutoMagicalSync_ValueMonitoringSupport>(25);
                    int length = monoBehaviours.Length;
                    for (int i = 0; i < length; ++i)
                    {
                        MonoBehaviour monoBehaviour = monoBehaviours[i];
                        IEnumerable<MemberInfo> syncMembers = monoBehaviour
                            .GetType()
                            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                            .Where(member => (member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                                            && member.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true) != null);

                        int syncMemberCount = syncMembers.Count();
                        for (int iSyncMember = 0; iSyncMember < syncMemberCount; ++iSyncMember)
                        {
                            MemberInfo syncMember = syncMembers.ElementAt(iSyncMember);

                            AutoMagicalSync_ValueMonitoringSupport monitoringSupport = new AutoMagicalSync_ValueMonitoringSupport(
                                (uint)monitoringSupports.Count, // since this Count is being checked prior to adding monitoring to that list, the Count now will end up being the index in that list after the Add
                                gonetParticipant,
                                monoBehaviour,
                                syncMember,
                                (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true)
                            );

                            monitoringSupports.Add(monitoringSupport);
                        }
                    }

                    if (monitoringSupports.Count > 0)
                    {
                        autoSyncMemberDataByGONetParticipantMap[gonetParticipant] = monitoringSupports;
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
        static readonly List<AutoMagicalSync_ValueMonitoringSupport> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport>(1000);
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

            { // TODO: PERF: look into putting this in another thread...perhaps checking on a frequency....I "believe" MemberInfo.GetValue(obj) is thread safe since it is only a read
                var enumerator = autoSyncMemberDataByGONetParticipantMap.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    List<AutoMagicalSync_ValueMonitoringSupport> monitoringSupports = enumerator.Current.Value;
                    int length = monitoringSupports.Count;
                    for (int i = 0; i < length; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport monitoringSupport = monitoringSupports[i];
                        monitoringSupport.UpdateLastKnownValue();
                        if (isProcessingAllStateRegardlessOfChange || monitoringSupport.hasValueChangedSinceLastSync)
                        {
                            syncValuesToSend.Add(monitoringSupport);

                            if (!isProcessingAllStateRegardlessOfChange)
                            {
                                monitoringSupport.hasValueChangedSinceLastSync = false;
                            }
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
        private static byte[] SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport> changes, out int bytesUsedCount)
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

        class AutoMagicalSyncChangePriorityComparer : IComparer<AutoMagicalSync_ValueMonitoringSupport>
        {
            internal static readonly AutoMagicalSyncChangePriorityComparer Instance = new AutoMagicalSyncChangePriorityComparer();

            private AutoMagicalSyncChangePriorityComparer() { }

            public int Compare(AutoMagicalSync_ValueMonitoringSupport x, AutoMagicalSync_ValueMonitoringSupport y)
            {
                int xPriority = x.syncAttribute.ProcessingPriority_GONetInternalOverride != 0 ? x.syncAttribute.ProcessingPriority_GONetInternalOverride : x.syncAttribute.ProcessingPriority;
                int yPriority = y.syncAttribute.ProcessingPriority_GONetInternalOverride != 0 ? y.syncAttribute.ProcessingPriority_GONetInternalOverride : y.syncAttribute.ProcessingPriority;

                return yPriority.CompareTo(xPriority); // descending...highest priority first!
            }
        }

        private static void SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport> changes, Utils.BitStream bitStream_headerAlreadyWritten)
        {
            int count = changes.Count;
            bitStream_headerAlreadyWritten.WriteUShort((ushort)count);
            GONetLog.Debug(string.Concat("about to send changes bundle...count: " + count));

            changes.Sort(AutoMagicalSyncChangePriorityComparer.Instance);

            for (int i = 0; i < count; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport monitoringSupport = changes[i];

                bool canASSumeGONetId = monitoringSupport.syncMember.Name == nameof(GONetParticipant.GONetId);

                bool shouldSendUnityFullUniquePath_insteadOfGONetId = canASSumeGONetId;
                bitStream_headerAlreadyWritten.WriteBit(shouldSendUnityFullUniquePath_insteadOfGONetId);
                if (shouldSendUnityFullUniquePath_insteadOfGONetId)
                {
                    string fullUniquePath = HierarchyUtils.GetFullUniquePath(monitoringSupport.gonetParticipant.gameObject);
                    bitStream_headerAlreadyWritten.WriteString(fullUniquePath);

                    GONetLog.Debug("sending full path: " + fullUniquePath);
                }
                else
                {
                    bitStream_headerAlreadyWritten.WriteUInt(monitoringSupport.gonetParticipant.GONetId); // should we order change list by this id ascending and just put diff from last value?
                }

                bitStream_headerAlreadyWritten.WriteUInt(monitoringSupport.indexInList);

                bool isFloatValue = monitoringSupport.lastKnownValue is float;
                bitStream_headerAlreadyWritten.WriteBit(isFloatValue);
                if (isFloatValue)
                {
                    bitStream_headerAlreadyWritten.WriteFloat((float)monitoringSupport.lastKnownValue); // TODO FIXME this only works with floats for now
                    // TODO include monitoringSupport.lastKnownValue_previous, which just moght be null and not a float!
                }
                else // TODO we are really going down wrong path here with all this if else figuring of what types/fields etc..., but this is PoC land...right..its ok...we'll get code generation in soon-ish
                {
                    if (monitoringSupport.lastKnownValue is uint)
                    {
                        if (canASSumeGONetId)
                        {
                            bitStream_headerAlreadyWritten.WriteUInt((uint)monitoringSupport.lastKnownValue);

                            GONetLog.Debug(string.Concat("just wrote new assignment of GONetId: ", monitoringSupport.lastKnownValue));
                        }
                        else if (monitoringSupport.syncMember.Name == nameof(GONetParticipant.OwnerAuthorityId))
                        {
                            bitStream_headerAlreadyWritten.WriteUInt((uint)monitoringSupport.lastKnownValue);
                        }
                    }
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
                bool didReceiveUnityFullUniquePath_insteadOfGONetId;
                bitStream_headerAlreadyRead.ReadBit(out didReceiveUnityFullUniquePath_insteadOfGONetId);

                GONetParticipant gonetParticipant = null;
                uint GONetId = default(uint);
                if (didReceiveUnityFullUniquePath_insteadOfGONetId)
                {
                    string fullUniquePath;
                    bitStream_headerAlreadyRead.ReadString(out fullUniquePath);

                    GONetLog.Debug("received full path: " + fullUniquePath);

                    GameObject gonetParticipantGO = HierarchyUtils.FindByFullUniquePath(fullUniquePath);
                    gonetParticipant = gonetParticipantGO.GetComponent<GONetParticipant>();
                    // NOTE: cannot do the following as we are ASSuming this message here actually represents the assignment of the gonetId for first time: GONetId = gonetParticipant.GONetId;
                }
                else
                {
                    bitStream_headerAlreadyRead.ReadUInt(out GONetId); // should we order change list by this id ascending and just put diff from last value?

                    GONetLog.Debug("did ***not*** receive full path.........process count: " + i + " GONetId: " + GONetId);

                    gonetParticipant = gonetParticipantByGONetIdMap[GONetId];
                }

                uint indexInList;
                bitStream_headerAlreadyRead.ReadUInt(out indexInList);

                AutoMagicalSync_ValueMonitoringSupport monitoringSupport = autoSyncMemberDataByGONetParticipantMap[gonetParticipant][(int)indexInList];
                bool canASSumeGONetId = monitoringSupport.syncMember.Name == nameof(GONetParticipant.GONetId);

                bool isFloatValue;
                bitStream_headerAlreadyRead.ReadBit(out isFloatValue);
                if (isFloatValue)
                {
                    float lastKnownValue;
                    bitStream_headerAlreadyRead.ReadFloat(out lastKnownValue); // TODO FIXME this only works with floats for now
                    // TODO include monitoringSupport.lastKnownValue_previous, which just moght be null and not a float!

                    GONetLog.Debug(string.Concat("just read in auto magic change val.....GONetId: ", GONetId, " indedInList: ", indexInList, " lastKnownValue: ", lastKnownValue));
                }
                else
                {
                    if (didReceiveUnityFullUniquePath_insteadOfGONetId && canASSumeGONetId)
                    {// if in here, we are ASSuming this message here actually represents the assignment of the gonetId for first time
                        bitStream_headerAlreadyRead.ReadUInt(out GONetId);

                        gonetParticipant.GONetId = GONetId;

                        GONetLog.Debug(string.Concat("just processed new <over network> assignment of GONetId: ", GONetId));
                    }
                    else if (monitoringSupport.syncMember.Name == nameof(GONetParticipant.OwnerAuthorityId))
                    {
                        uint ownerAuthorityId;
                        bitStream_headerAlreadyRead.ReadUInt(out ownerAuthorityId);

                        gonetParticipant.OwnerAuthorityId = ownerAuthorityId;
                    }
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
                autoSyncMemberDataByGONetParticipantMap.Remove(gonetParticipant);

                // do we need to send event to disable this thing?
            }
        }

        #endregion
    }
}
