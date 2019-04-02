using GONet.Utils;
using Microsoft.IO;
using ReliableNetcode;
using System;
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

        public static GONetSessionContext GlobalSessionContext { get; internal set; } // TODO when set, update MySessionContext_Participant
        public static GONetParticipant GlobalSessionContext_Participant { get; private set; }

        public static GONetSessionContext MySessionContext { get; internal set; } // TODO when set, update MySessionContext_Participant
        public static GONetParticipant MySessionContext_Participant { get; private set; }
        public static uint MyAuthorityId => MySessionContext_Participant.OwnerAuthorityId;

        internal static bool isServerOverride = false;
        public static bool IsServer => isServerOverride || MySessionContext_Participant?.OwnerAuthorityId == GONetParticipant.OwnerAuthorityId_Server; // TODO cache this since it will not change and too much processing to get now

        internal static GONetServer gonetServer; // TODO FIXME make this private.....temporary for testing, its internal
        internal static GONetClient gonetClient; // TODO FIXME make this private.....temporary for testing, its internal

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
            ProcessAutoMagicalSyncStuffs();

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

        internal static void ProcessIncomingBytes(ReliableEndpoint sourceEndpoint, byte[] messageBytes, int bytesUsedCount)
        {
            using (var memoryStream = new MemoryStream(messageBytes))
            {
                using (var bitStream = new Utils.BitStream(memoryStream)) // NOTE: This implementation does NOT advance memoryStream.Position on bitStream.Read(), hence the manual advances below
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
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetId = GONetParticipant.GONetId_Unset;
        static readonly Dictionary<GONetParticipant, List<AutoMagicalSync_ValueMonitoringSupport>> autoSyncMemberDataByGONetParticipant = new Dictionary<GONetParticipant, List<AutoMagicalSync_ValueMonitoringSupport>>(1000);

        class AutoMagicalSync_ValueMonitoringSupport
        {
            /// <summary>
            /// NOTE: The list this is an index for is a list value inside <see cref="autoSyncMemberDataByGONetParticipant"/>.
            /// IMPORTANT: The list it indexes therein MUST be predictably ordered on all sides of the networking fence in order for this to be useful!
            /// </summary>
            internal uint indexInList;
            internal GONetParticipant gonetParticipant;
            internal MonoBehaviour syncMemberOwner;
            internal MemberInfo syncMember;
            internal GONetAutoMagicalSyncAttribute syncAttribute;
            internal object lastKnownValue;
            internal object lastKnownValue_previous;

            internal bool HasValueChangedSinceLastSync;

            internal void UpdateLastKnownValue() // TODO FIXME need to use some code generation up in this piece for increased runtime/execution performance instead of reflection herein
            {
                lastKnownValue_previous = lastKnownValue;
                lastKnownValue = syncMember.MemberType == MemberTypes.Property
                                    ? ((PropertyInfo)syncMember).GetValue(syncMemberOwner)
                                    : ((FieldInfo)syncMember).GetValue(syncMemberOwner); // ASSuming field here since only field and property allowed

                HasValueChangedSinceLastSync = !Equals(lastKnownValue, lastKnownValue_previous); // NOTE: using != must be somehow not comparing values and instead comparing memory addresses because of object declaration even though if they are floats they have same value
            }
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnEnable method.
        /// </summary>
        internal static void OnEnable_StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            {
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

                        AutoMagicalSync_ValueMonitoringSupport monitoringSupport = new AutoMagicalSync_ValueMonitoringSupport();

                        monitoringSupport.indexInList = (uint)iSyncMember;
                        monitoringSupport.gonetParticipant = gonetParticipant;
                        monitoringSupport.syncMemberOwner = monoBehaviour;
                        monitoringSupport.syncMember = syncMember;
                        monitoringSupport.syncAttribute = (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true);

                        monitoringSupport.UpdateLastKnownValue();

                        monitoringSupports.Add(monitoringSupport);
                    }
                }

                if (monitoringSupports.Count > 0)
                {
                    autoSyncMemberDataByGONetParticipant[gonetParticipant] = monitoringSupports;
                }
            }

            AssignGONetId_IfAppropriate(gonetParticipant);
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

        static readonly List<AutoMagicalSync_ValueMonitoringSupport> valueChangesSinceLastSync = new List<AutoMagicalSync_ValueMonitoringSupport>(1000);
        static void ProcessAutoMagicalSyncStuffs()
        {
            valueChangesSinceLastSync.Clear();

            var enumerator = autoSyncMemberDataByGONetParticipant.GetEnumerator();
            while (enumerator.MoveNext())
            {
                List<AutoMagicalSync_ValueMonitoringSupport> monitoringSupports = enumerator.Current.Value;
                int length = monitoringSupports.Count;
                for (int i = 0; i < length; ++i)
                {
                    AutoMagicalSync_ValueMonitoringSupport monitoringSupport = monitoringSupports[i];
                    monitoringSupport.UpdateLastKnownValue();
                    if (monitoringSupport.HasValueChangedSinceLastSync)
                    {
                        valueChangesSinceLastSync.Add(monitoringSupport);
                        monitoringSupport.HasValueChangedSinceLastSync = false;
                    }
                }
            }

            if (valueChangesSinceLastSync.Count > 0)
            {
                int bytesUsedCount;
                byte[] changesSerialized = SerializeChangesBundle(valueChangesSinceLastSync, out bytesUsedCount);

                SendBytesToRemoteConnections(changesSerialized, bytesUsedCount);

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
                        .Select(a => a.GetTypes().Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(Message)) && !t.IsAbstract).OrderBy(t2 => t2.FullName)))
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

        abstract class Message
        {
        }

        class AutoMagicalSync_ValueChangesMessage : Message
        {
            List<AutoMagicalSync_ValueMonitoringSupport> changes;
        }

        /// <summary>
        /// PRE: <paramref name="changes"/> size is greater than 0
        /// IMPORTANT: The caller is responsible for returning the returned byte[] to <see cref="valueChangeSerializationArrayPool"/>!
        /// </summary>
        private static byte[] SerializeChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport> changes, out int bytesUsedCount)
        {
            using (var memoryStream = new RecyclableMemoryStream(valueChangesMemoryStreamManager))
            {
                using (Utils.BitStream bitStream = new Utils.BitStream(memoryStream))
                {
                    { // header...just message type/id
                        uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChangesMessage)];
                        bitStream.WriteUInt(messageID);
                    }

                    SerializeChangesBundle_AppendStream(changes, bitStream); // body

                    bitStream.WriteCurrentPartialByte();

                    bytesUsedCount = (int)memoryStream.Length;
                    byte[] bytes = valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                    Array.Copy(memoryStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                    return bytes;
                }
            }
        }

        private static void SerializeChangesBundle_AppendStream(List<AutoMagicalSync_ValueMonitoringSupport> changes, Utils.BitStream bitStream)
        {
            int count = changes.Count;
            bitStream.WriteUShort((ushort)count);
            for (int i = 0; i < count; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport monitoringSupport = changes[i];

                bool shouldSendUnityUniquePathId = false; // TODO this should be true when this change represents the assignment of the GONetId since the other side will not have it yet and cannot use it to look anything up
                bitStream.WriteBit(shouldSendUnityUniquePathId);
                if (shouldSendUnityUniquePathId)
                {
                    // TODO FIXME make/manage/use the full unique path hash code hash table lookup dudio (this should be added to during OnEnable_
                }
                else
                {
                    bitStream.WriteUInt(monitoringSupport.gonetParticipant.GONetId); // should we order change list by this id ascending and just put diff from last value?
                }

                bitStream.WriteUInt(monitoringSupport.indexInList);

                bool isFloatValue = monitoringSupport.lastKnownValue is float;
                bitStream.WriteBit(isFloatValue);
                if (isFloatValue)
                {
                    bitStream.WriteFloat((float)monitoringSupport.lastKnownValue); // TODO FIXME this only works with floats for now
                    // TODO include monitoringSupport.lastKnownValue_previous, which just moght be null and not a float!
                }
            }
        }

        private static void DeserializeBody_ChangesBundle(Utils.BitStream bitStream_headerAlreadyRead)
        {
            ushort count;
            bitStream_headerAlreadyRead.ReadUShort(out count);
            for (int i = 0; i < count; ++i)
            {
                bool shouldSendUnityUniquePathId;
                bitStream_headerAlreadyRead.ReadBit(out shouldSendUnityUniquePathId);

                uint GONetId = default(uint);
                if (shouldSendUnityUniquePathId)
                {
                    // TODO FIXME make/manage/use the full unique path hash code hash table lookup dudio (this should be added to during OnEnable_
                }
                else
                {
                    bitStream_headerAlreadyRead.ReadUInt(out GONetId); // should we order change list by this id ascending and just put diff from last value?
                }

                uint indexInList;
                bitStream_headerAlreadyRead.ReadUInt(out indexInList);

                bool isFloatValue;
                bitStream_headerAlreadyRead.ReadBit(out isFloatValue);
                if (isFloatValue)
                {
                    float lastKnownValue;
                    bitStream_headerAlreadyRead.ReadFloat(out lastKnownValue); // TODO FIXME this only works with floats for now
                    // TODO include monitoringSupport.lastKnownValue_previous, which just moght be null and not a float!

                    Debug.Log(string.Concat("just read in auto magic change val.....GONetId: ", GONetId, " indedInList: ", indexInList, " lastKnownValue: ", lastKnownValue));
                }
            }
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnDisable method.
        /// </summary>
        internal static void OnDisable_StopMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            autoSyncMemberDataByGONetParticipant.Remove(gonetParticipant);

            // do we need to send event to disable this thing?
        }

        #endregion
    }
}
