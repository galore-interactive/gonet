/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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

using GONet.Generation;
using GONet.Utils;
using ReliableNetcode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

using GONetCodeGenerationId = System.Byte;
using GONetChannelId = System.Byte;
using System.IO;
using System.Runtime.Serialization;
using System.Net;
using System.Collections;

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

        public static GONetGlobal Global { get; private set; }

        private static GONetSessionContext globalSessionContext;
        public static GONetSessionContext GlobalSessionContext
        {
            get { return globalSessionContext; }
            private set
            {
                globalSessionContext = value;
                GlobalSessionContext_Participant = (object)globalSessionContext == null ? null : globalSessionContext.gameObject.GetComponent<GONetParticipant>();
            }
        }

        public static GONetParticipant GlobalSessionContext_Participant { get; private set; }

        public static GONetLocal myLocal;
        public static GONetLocal MyLocal // TODO FIXME: setting this will be a problem  when a server can be/have a client as well!!!
        {
            get => myLocal;
            private set
            {
                myLocal = value;
                MySessionContext = value == null ? null : value.GetComponent<GONetSessionContext>();
            }
        }

        public const long SessionGUID_Unset = default;
        static long sessionGUID = SessionGUID_Unset;
        public static long SessionGUID
        {
            get => sessionGUID;
            private set
            {
                if (sessionGUID == SessionGUID_Unset)
                {
                    sessionGUID = value;
                }
                else
                {
                    const string SUIDX = "For some reason, something is attempting to change the SessionGUID; however this is not allowed.  This could be due to host migration, which is not currently support...so, Hmmm....";
                    GONetLog.Warning(SUIDX);
                }
            }
        }

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

        /// <summary>
        /// <para>This is used to automatically to compress **EVERYTHING** GONet sends!</para>
        /// <para>Default is LZ4 compression.</para>
        /// <para>Set to null if you prefer not to use compression.</para>
        /// <para>WARNING: We will open up this API soon...as of now, to chan change this value during runtime, you would have to be very cautious as to the timing and ensure it is not somehow changed between calls to compress/uncompress...since we are not going to figure the timing of all that right now, we will leave setter private.</para>
        /// </summary>
        public static IByteArrayCompressionSupport AutoCompressEverything { get; private set; } = LZ4CompressionSupport.Instance;

        static long ticksAtLastInit_UtcNow;

        internal static void InitOnUnityMainThread(GONetGlobal gONetGlobal, GONetSessionContext gONetSessionContext, int valueBlendingBufferLeadTimeMilliseconds)
        {
            const string ENV = "Environment.ProcessorCount: ";
            GONetLog.Info(ENV + Environment.ProcessorCount);

            IsUnityApplicationEditor = Application.isEditor;
            mainUnityThread = Thread.CurrentThread;

            Global = gONetGlobal;
            GlobalSessionContext = gONetSessionContext;
            SetValueBlendingBufferLeadTimeFromMilliseconds(valueBlendingBufferLeadTimeMilliseconds);
            InitEventSubscriptions();
            InitPersistence();
            InitQuantizers();

            ticksAtLastInit_UtcNow = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Need to create an instance of each generated child class of <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated"/> in order to get access to each of its unique <see cref="QuantizerSettingsGroup"/> values
        /// to ensure a corresponding Quantizer instance is created here in the main thread to avoid using ConcurrentDictionary (i.e. runtime GC) for runtime adds and lookups.
        /// </summary>
        private static void InitQuantizers()
        {
            foreach (QuantizerSettingsGroup settings in GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.GetAllPossibleUniqueQuantizerSettingsGroups())
            {
                // not all quantizer settings that are generated will actually equate to a quantizer being used since everything gets a quantizer setting..so check before causing exception
                bool canBeUsedForQuantization = settings.quantizeToBitCount > 0;
                if (canBeUsedForQuantization)
                {
                    Quantizer.EnsureQuantizerExistsForGroup(settings);
                }
            }
        }

        internal const int SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE = 1000;
        const int MAX_SYNC_EVENTS_RETURN_PER_FRAME_THRESHOLD = SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE * 2;
        const int STARTING_MAX_SYNC_EVENTS_RETURN_PER_FRAME = 25;
        const int MAX_SYNC_EVENTS_RETURN_PER_FRAME_INCREASEBY_WHENBUSY = 5;
        private static string persistenceFilePath;
        private static FileStream persistenceFileStream;
        const string DATE_FORMAT = "yyyy_MM_dd___HH-mm-ss-fff";
        const string TRIPU = "___";
        const string SGUID = "SGUID";
        const string MOAId = "MOAId";
        const string DB_EXT = ".mpb";
        const string DATABASE_PATH_RELATIVE = "database/";
        private static void InitPersistence()
        {
            persistenceFilePath = string.Concat(DATABASE_PATH_RELATIVE, Math.Abs(Application.productName.GetHashCode()), TRIPU, DateTime.Now.ToString(DATE_FORMAT), TRIPU, SGUID, TRIPU, MOAId, DB_EXT);
            Directory.CreateDirectory(DATABASE_PATH_RELATIVE);
            persistenceFileStream = new FileStream(persistenceFilePath, FileMode.Append);

            IEnumerable<Type> syncEventTypes = GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.GetAllUniqueSyncEventTypes();
            foreach (Type syncEventType in syncEventTypes)
            {
                syncEventsToSaveQueueByEventType[syncEventType] = new SyncEventsSaveSupport();
            }
        }

        public static GONetParticipant MySessionContext_Participant { get; private set; } // TODO FIXME need to spawn this for everyone and set it here!
        public static ushort MyAuthorityId { get; private set; }

        internal static bool isServerOverride = NetworkUtils.IsIPAddressOnLocalMachine(GONetSampleClientOrServer.serverIP) && !NetworkUtils.IsLocalPortListening(GONetSampleClientOrServer.serverPort); // TODO FIXME gotta iron out good startup process..this is quite temporary
        /// <summary>
        /// IMPORTANT: This can be true even when <see cref="IsClient"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsServer => isServerOverride || MyAuthorityId == OwnerAuthorityId_Server; // TODO cache this since it will not change and too much processing to get now

        /// <summary>
        /// IMPORTANT: This can return true even when <see cref="IsServer"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsClientType(ClientTypeFlags requiredFlags)
        {
            return (MyClientTypeFlags & requiredFlags) == requiredFlags;
        }

        public static ClientTypeFlags MyClientTypeFlags => _gonetClient == null ? ClientTypeFlags.None : _gonetClient.ClientTypeFlags;

        /// <summary>
        /// IMPORTANT: This can be true even when <see cref="IsServer"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsClient => _gonetClient == null ? false : _gonetClient.ClientTypeFlags != ClientTypeFlags.None;

        /// <summary>
        /// Since the value of <see cref="GONetParticipant.GONetId"/> can change (i.e., <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/> called),
        /// this is the mechanism to find the original value at time of initial instantiation.  Not sure how this helps others, but internally to GONet it is useful.
        /// </summary>
        public static uint GetGONetIdAtInstantiation(uint currentGONetId)
        {
            GONetParticipant gonetParticipant;
            if (gonetParticipantByGONetIdMap.TryGetValue(currentGONetId, out gonetParticipant))
            {
                return gonetParticipant.GONetIdAtInstantiation;
            }
            else
            {
                return GONetParticipant.GONetId_Unset;
            }
        }

        public static uint GetCurrentGONetIdByIdAtInstantiation(uint gonetIdAtInstantiation)
        {
            GONetParticipant gonetParticipant = null;
            if (gonetParticipant_by_gonetIdAtInstantiation.TryGetValue(gonetIdAtInstantiation, out gonetParticipant))
            {
                return gonetParticipant.GONetId;
            }
            return GONetParticipant.GONetId_Unset;
        }

        /// <summary>
        /// IMPORTANT: Prior to things being initialized with network connection(s), we may not know if we are a client or a server...in which case, this will return false!
        /// </summary>
        public static bool IsClientVsServerStatusKnown => IsServer || IsClient;

        private static GONetServer _gonetServer; // TODO remove this once we make gonetServer private again!
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetServer gonetServer
        {
            get { return _gonetServer; }
            set
            {
                if (value != null)
                {
                    SessionGUID = GUID.Generate().AsInt64();
                }

                MyAuthorityId = OwnerAuthorityId_Server;
                _gonetServer = value;
                _gonetServer.ClientConnected += Server_OnClientConnected_SendClientCurrentState;

                MyLocal = UnityEngine.Object.Instantiate(Global.gonetLocalPrefab);
            }
        }

        public static GONetEventBus EventBus => GONetEventBus.Instance;

        public const string REQUIRED_CALL_UNITY_MAIN_THREAD = "Not allowed to call this from any other thread than the main Unity thread.";
        private static Thread mainUnityThread;
        public static bool IsUnityMainThread => mainUnityThread == Thread.CurrentThread;

        public static bool IsUnityApplicationEditor { get; private set; } = false;

        /// <summary>
        /// IMPORTANT: This will NOT include ALL events that implement <see cref="IPersistentEvent"/> as it may sound *IF* anything cancelled out another/previous event (i.e., <see cref="ICancelOutOtherEvents"/>).
        /// </summary>
        static readonly LinkedList<IPersistentEvent> persistentEventsThisSession = new LinkedList<IPersistentEvent>();

        static readonly List<uint> gonetIdsDestroyedViaPropagation = new List<uint>(500);

        internal class SyncEventsSaveSupport
        {
            internal readonly Queue<SyncEvent_ValueChangeProcessed> queue_needsSavingASAP = new Queue<SyncEvent_ValueChangeProcessed>(SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE);
            internal readonly Queue<SyncEvent_ValueChangeProcessed> queue_needsSaving = new Queue<SyncEvent_ValueChangeProcessed>(SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE);
            internal readonly ConcurrentQueue<SyncEvent_ValueChangeProcessed> queue_needsReturnToPool = new ConcurrentQueue<SyncEvent_ValueChangeProcessed>();

            internal int maxToReturnPerFrame = STARTING_MAX_SYNC_EVENTS_RETURN_PER_FRAME;
            internal volatile bool IsSaving;
            internal readonly AutoResetEvent IsSavingMutex = new AutoResetEvent(true);

            internal SyncEventsSaveSupport()
            {
                { // just ensure this data structure has enough internal memory stuffs now so no allocations and GC crap has to happen later!
                    for (int i = 0; i < SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE; ++i)
                    {
                        queue_needsReturnToPool.Enqueue(new SyncEvent_GONetParticipant_GONetId());
                    }

                    SyncEvent_ValueChangeProcessed item;
                    for (int i = 0; i < SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE; ++i)
                    {
                        queue_needsReturnToPool.TryDequeue(out item);
                    }
                }
            }

            /// <summary>
            /// Transfer queued ASAP items to save queue that will be processed in another thread.
            /// IMPORTANT: Call this from main Unity thread!
            /// POST: <see cref="IsSaving"/> will be true.
            /// </summary>
            internal void InitiateSave_MainUnityThread()
            {
                IsSavingMutex.WaitOne();
                IsSaving = true;

                lock (queue_needsSaving)
                {
                    var enumeratorASAP = queue_needsSavingASAP.GetEnumerator();
                    while (enumeratorASAP.MoveNext())
                    {
                        queue_needsSaving.Enqueue(enumeratorASAP.Current);
                    }
                }
                queue_needsSavingASAP.Clear();
            }

            /// <summary>
            /// PRE: This is expected to NOT be called from Main Unity thread, but rather from what we will call the "save thread" (which at time of writing is the <see cref="endOfLineSendAndSaveThread"/>)
            /// POST: <see cref="queue_needsSaving"/> will be cleared and <see cref="queue_needsReturnToPool"/> will contain all items previously in <see cref="queue_needsSaving"/> and <see cref="IsSaving"/> will be false.
            /// </summary>
            internal void OnAfterAllSaved_SaveThread()
            {
                lock (queue_needsSaving)
                {
                    SyncEvent_ValueChangeProcessed syncEvent;
                    while (queue_needsSaving.Count > 0 && (syncEvent = queue_needsSaving.Dequeue()) != null)
                    {
                        queue_needsReturnToPool.Enqueue(syncEvent);
                    }
                }

                IsSavingMutex.Set();
                IsSaving = false;
            }

            internal void ReturnSaved_SpreadOverFrames_MainUnityThread()
            {
                int queueCount = queue_needsReturnToPool.Count;
                if (queueCount > MAX_SYNC_EVENTS_RETURN_PER_FRAME_THRESHOLD)
                {
                    maxToReturnPerFrame += MAX_SYNC_EVENTS_RETURN_PER_FRAME_INCREASEBY_WHENBUSY; // TODO try a better calculation for what actually makes sense here
                }

                int actualReturnCount = maxToReturnPerFrame;
                int remainingCount = actualReturnCount;
                SyncEvent_ValueChangeProcessed syncEventToReturn;
                while (remainingCount > 0 && queue_needsReturnToPool.TryDequeue(out syncEventToReturn))
                {
                    syncEventToReturn.Return();
                    --remainingCount;
                }

                //if (actualReturnCount > 0) GONetLog.Debug("just returned "+actualReturnCount+", how many remain? queue_needsReturnToPool.Count: " + queue_needsReturnToPool.Count);
            }
        }

        static readonly Dictionary<Type, SyncEventsSaveSupport> syncEventsToSaveQueueByEventType = new Dictionary<Type, SyncEventsSaveSupport>(100);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread
        /// </summary>
        static readonly Dictionary<Thread, Queue<SyncEvent_ValueChangeProcessed>> syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap = new Dictionary<Thread, Queue<SyncEvent_ValueChangeProcessed>>(12);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread (i.e., transfer data from <see cref="syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap"/> once the time is right) but also read from and dequeued from the main unity thread when time to publish the events!
        /// </summary>
        static readonly Dictionary<Thread, ConcurrentQueue<SyncEvent_ValueChangeProcessed>> syncValueChanges_SendToOthersQueue_ByThreadMap = new Dictionary<Thread, ConcurrentQueue<SyncEvent_ValueChangeProcessed>>(12);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread (i.e., transfer data from <see cref="syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap"/> once the time is right) but also read from and dequeued from the main unity thread when time to publish the events!
        /// </summary>
        static readonly Queue<SyncEvent_ValueChangeProcessed> syncValueChanges_ReceivedFromOtherQueue = new Queue<SyncEvent_ValueChangeProcessed>(100);

        internal static GONetClient _gonetClient;
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetClient GONetClient
        {
            get => _gonetClient;

            set
            {
                ClientTypeFlags flagsPrevious = MyClientTypeFlags;

                _gonetClient = value;

                ClientTypeFlags flagsNow = MyClientTypeFlags;

                if (flagsNow != flagsPrevious)
                {
                    EventBus.Publish(new ClientTypeFlagsChangedEvent(Time.ElapsedTicks, MyAuthorityId, flagsPrevious, flagsNow));
                }

                _gonetClient.InitializedWithServer += Client_gonetClient_InitializedWithServer;
            }
        }

        private static void Client_gonetClient_InitializedWithServer(GONetClient client)
        {
            MyLocal = UnityEngine.Object.Instantiate(Global.gonetLocalPrefab);

            while (client.incomingNetworkData_mustProcessAfterClientInitialized.Count > 0)
            {
                NetworkData item = client.incomingNetworkData_mustProcessAfterClientInitialized.Dequeue();
                ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(item);
            }
        }

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdMap = new Dictionary<uint, GONetParticipant>(1000);

        public const ushort OwnerAuthorityId_Unset = 0;
        public const ushort OwnerAuthorityId_Server = unchecked((ushort)(ushort.MaxValue << GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_UNUSED)) >> GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_UNUSED;

        /// <summary>
        /// Only used/applicable if <see cref="IsServer"/> is true.
        /// </summary>
        private static ushort server_lastAssignedAuthorityId = OwnerAuthorityId_Unset;

        /// <summary>
        /// <para>IMPORTANT: Up until some time during <see cref="GONetParticipant.Start"/>, the value of <see cref="GONetParticipant.OwnerAuthorityId"/> will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown, which means this method will return false for everyone (even the actual owner).  Once the owner is known, <see cref="GONetParticipant.OwnerAuthorityId"/> value will change and the <see cref="SyncEvent_GONetParticipant_OwnerAuthorityId"/> event will fire (i.e., you should call <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/> on <see cref="EventBus"/>)</para>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// <para>From a GONet perspective, this checks if the <paramref name="gameObject"/> has a <see cref="GONetParticipant"/> and if so, whether or not you own it.</para>
        /// <para>If you already have access to the <see cref="GONetParticipant"/> associated with this <paramref name="gameObject"/>, then use the sister method instead: <see cref="IsMine(GONetParticipant)"/></para>
        /// </summary>
        public static bool IsMine(GameObject gameObject)
        {
            return gameObject.GetComponent<GONetParticipant>()?.OwnerAuthorityId == MyAuthorityId; // TODO cache instead of lookup/get each time!
        }

        /// <summary>
        /// <para>IMPORTANT: Up until some time during <see cref="GONetParticipant.Start"/>, the value of <see cref="GONetParticipant.OwnerAuthorityId"/> will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown, which means this method will return false for everyone (even the actual owner).  Once the owner is known, <see cref="GONetParticipant.OwnerAuthorityId"/> value will change and the <see cref="SyncEvent_GONetParticipant_OwnerAuthorityId"/> event will fire (i.e., you should call <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/> on <see cref="EventBus"/>)</para>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMine(GONetParticipant gonetParticipant)
        {
            return gonetParticipant.OwnerAuthorityId == MyAuthorityId;
        }

        /// <summary>
        /// <para>IMPORTANT: Keep in mind if <paramref name="gonetParticipant"/> has many auto sync members, *ALL* of them have to have enough values in history to support a smooth assumption of authority.</para>
        /// <para>           Even if only transform position and rotation are auto sync'd, both of them have had to changed at least <see cref="ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE"/> times in order for this to return true.</para>
        /// <para>           Therefore, this method is only to be required to return true prior to calling <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/> when it is known to make sense!</para>
        /// <para>           This is up to you to use or not.  You can still call <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/> when this return false and the world will not end and the assumption of authority will still occur (ASSuming <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/> returns true)!</para>
        /// </summary>
        /// <param name="gonetParticipant"></param>
        /// <returns></returns>
        public static bool Server_HasEnoughValueBlendHistoryToSmoothly_AssumeAuthorityOver(GONetParticipant gonetParticipant)
        {
            if (!IsServer || gonetParticipant.OwnerAuthorityId == OwnerAuthorityId_Unset || IsMine(gonetParticipant))
            {
                return false;
            }

            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
            GONetParticipant_AutoMagicalSyncCompanion_Generated autoSyncCompanion;
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions) &&
                autoSyncCompanions.TryGetValue(gonetParticipant, out autoSyncCompanion))
            {
                byte valuesCount = autoSyncCompanion.valuesCount;
                for (int i = 0; i < valuesCount; ++i)
                {
                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangesSupport = autoSyncCompanion.valuesChangesSupport[i];
                    if (valueChangesSupport.mostRecentChanges != null) // TODO FIXME have to include a check for the IsPositionSyncd and IsRotationSyncd check dealios
                    {
                        if (valueChangesSupport.mostRecentChanges_usedSize < ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// <para>If you on running on the server and need to "assume ownership" over something (e.g., a client instantiated projectile), this is the method to do so.</para>
        /// <para>
        /// If you want the value blending to go smoothly, you can ensure only to call this once <see cref="Server_HasEnoughValueBlendHistoryToSmoothly_AssumeAuthorityOver(GONetParticipant)"/> returns true.
        /// This can still be called and work out just ~fine when that method returns false, but there might be a one-frame warp/teleport from old values to new for the original/previous owner
        /// </para>
        /// <para>POST: *if* this method returns true, the value of <paramref name="gonetParticipant"/>'s <see cref="GONetParticipant.OwnerAuthorityId"/> will be changed to <see cref="MyAuthorityId"/> AND a new value for <see cref="GONetParticipant.gonetId_raw"/> will be assigned.</para>
        /// </summary>
        public static bool Server_AssumeAuthorityOver(GONetParticipant gonetParticipant)
        {
            if (IsServer && gonetParticipant.OwnerAuthorityId != OwnerAuthorityId_Unset && !IsMine(gonetParticipant))
            {
                Server_AssumeAuthorityOver_MakeCurrentAndStopValueBlending(gonetParticipant);

                gonetParticipant.OwnerAuthorityId = MyAuthorityId; // NOTE: this will propagate to all other parties through auto sync support
                AssignGONetIdRaw_IfAppropriate(gonetParticipant, true); // IMPORTANT: whatever the gonetId_raw value was before was only valid for the previous owner, we have to assign that anew here now!

                OnGONetIdComponentChanged_EnsureMapKeysUpdated(gonetParticipant, gonetParticipant.GONetId); // NOTE: yes, this will also be handled via subscribers to SyncEvent_GONetParticipant_GONetId AND SyncEvent_GONetParticipant_OwnerAuthorityId, but it is best to do it immediately here since we already know it changed and those events are fired at end of frame!

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Helper method to <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/>.
        /// Clear out all value blending data/support from previous owner since I/server will now be the owner and having this value blending data around could be problematic:
        /// </summary>
        private static void Server_AssumeAuthorityOver_MakeCurrentAndStopValueBlending(GONetParticipant gonetParticipant)
        {
            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
            GONetParticipant_AutoMagicalSyncCompanion_Generated autoSyncCompanion;
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions) &&
                autoSyncCompanions.TryGetValue(gonetParticipant, out autoSyncCompanion))
            {
                byte valuesCount = autoSyncCompanion.valuesCount;
                for (int i = 0; i < valuesCount; ++i)
                {
                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangesSupport = autoSyncCompanion.valuesChangesSupport[i];
                    if (valueChangesSupport.mostRecentChanges != null)
                    {
                        GONetSyncableValue valueBefore = valueChangesSupport.syncCompanion.GetAutoMagicalSyncValue((byte)i);

                        /*This happens every time on at least one property/index...so it seems spammy:
                        if (valueChangesSupport.mostRecentChanges_usedSize < ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE)
                        {
                            const string NO_EXTRAP = "While transferring ownership to server, there is not enough information for ApplyValueBlending_IfAppropriate to extrapolate to right now right now, because it would seem highly prefferable to be able to extrapolate to now instead of staying at the value we had from back at negative GONetMain.valueBlendingBufferLeadTicks ago.  GONetId: ";
                            const string IDX = "  Value index: ";
                            GONetLog.Warning(string.Concat(NO_EXTRAP, gonetParticipant.GONetId, IDX, i)); // TODO printing out the index is not useful!  print a name of property or something!!!
                        }
                        */
                        valueChangesSupport.ApplyValueBlending_IfAppropriate(0); // make sure we update it to the latest value for right now right now (i.e., pass 0 instead of GONetMain.valueBlendingBufferLeadTicks) before we transfer ownership
                        valueChangesSupport.ClearMostRecentChanges(); // most recent changes is only useful for value blending...and since we are now the owner (or will be soon below), no sense in keeping this around

                        GONetSyncableValue valueAfter = valueChangesSupport.syncCompanion.GetAutoMagicalSyncValue((byte)i);
                        valueChangesSupport.lastKnownValue = valueChangesSupport.lastKnownValue_previous = valueAfter; // IMPORTANT: now that we are taking over ownership (below), we need to keep tabs on when changes occur and this is first step to baseline things from this point forward
                    }
                }
            }
            else
            {
                const string TRANS = "Transferring ownership to server and expecting to find an active auto sync support/companion instance, but did not.  NOTE: The transfer will still occur.  GONetId: ";
                GONetLog.Warning(string.Concat(TRANS, gonetParticipant.GONetId));
            }
        }

        private static void OnOwnerAuthorityIdChanged(GONetEventEnvelope<SyncEvent_GONetParticipant_OwnerAuthorityId> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            OnGONetIdComponentChanged_EnsureMapKeysUpdated(gonetParticipant, eventEnvelope.Event.valuePrevious);

            if ((object)gonetParticipant != null && gonetParticipant.gonetId_raw != GONetParticipant.GONetId_Unset)
            {
                gonetParticipant.SetRigidBodySettingsConsideringOwner();
            }
            else
            {
                const string EXP = "Expecting to receive a non-null GNP, but it is null.";
                GONetLog.Warning(EXP);
            }
        }

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipant_by_gonetIdAtInstantiation = new Dictionary<uint, GONetParticipant>(5000);

        internal static void OnGONetIdAboutToBeSet(uint gonetId_new, uint gonetId_raw_new, ushort ownerAuthorityId_new, GONetParticipant gonetParticipant)
        {
            if (gonetId_new == gonetParticipant.GONetIdAtInstantiation)
            {
                gonetParticipant_by_gonetIdAtInstantiation[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;
                gonetParticipantByGONetIdMap[gonetId_new] = gonetParticipant;
            }
            else
            {
                ushort ownerAuthorityId_asRepresentedInside_gonetIdAtInstantiation = (ushort)((gonetParticipant.GONetIdAtInstantiation << GONetParticipant.GONET_ID_BIT_COUNT_USED) >> GONetParticipant.GONET_ID_BIT_COUNT_USED);
                uint gonetId_raw_asRepresentedInside_gonetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation >> GONetParticipant.GONET_ID_BIT_COUNT_UNUSED;

                bool areAllComponentsChanging =
                    ownerAuthorityId_asRepresentedInside_gonetIdAtInstantiation != ownerAuthorityId_new &&
                    gonetId_raw_asRepresentedInside_gonetIdAtInstantiation != gonetId_raw_new;

                if (areAllComponentsChanging)
                {
                    gonetParticipant_by_gonetIdAtInstantiation[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;

                    gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetIdAtInstantiation);
                    gonetParticipantByGONetIdMap[gonetId_new] = gonetParticipant; // TODO first check for collision/overwrite and throw exception....or warning at least!
                }
            }
        }

        private static void OnGONetIdChanged(GONetEventEnvelope<SyncEvent_GONetParticipant_GONetId> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            OnGONetIdComponentChanged_EnsureMapKeysUpdated(eventEnvelope.GONetParticipant, eventEnvelope.Event.valuePrevious);
        }

        /// <summary>
        /// Now that OwnerAuthorityId changed, that means any data structures (namely dictionary) using OwnerAuthorityId =OR= GONetId (since it is a composite value that includes OwnerAuthorityId) as a key, need to be updated!
        /// </summary>
        private static void OnGONetIdComponentChanged_EnsureMapKeysUpdated(GONetParticipant gonetParticipant, uint previousGONetId)
        {
            if ((object)gonetParticipant != null && gonetParticipant.GONetId != GONetParticipant.GONetId_Unset)
            {
                if (gonetParticipant.GONetId == gonetParticipant.GONetIdAtInstantiation)
                {
                    gonetParticipantByGONetIdMap[gonetParticipant.GONetId] = gonetParticipant;
                }
                else
                {
                    ushort ownerAuthorityId_asRepresentedInside_gonetIdAtInstantiation = (ushort)((gonetParticipant.GONetIdAtInstantiation << GONetParticipant.GONET_ID_BIT_COUNT_USED) >> GONetParticipant.GONET_ID_BIT_COUNT_USED);
                    uint gonetId_raw_asRepresentedInside_gonetIdAtInstantiation = gonetParticipant.GONetIdAtInstantiation >> GONetParticipant.GONET_ID_BIT_COUNT_UNUSED;

                    bool areAllComponentsChanging =
                        ownerAuthorityId_asRepresentedInside_gonetIdAtInstantiation != gonetParticipant.OwnerAuthorityId &&
                        gonetId_raw_asRepresentedInside_gonetIdAtInstantiation != gonetParticipant.gonetId_raw;

                    if (areAllComponentsChanging)
                    {
                        gonetParticipant_by_gonetIdAtInstantiation[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;

                        gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetIdAtInstantiation);
                        gonetParticipantByGONetIdMap[gonetParticipant.GONetId] = gonetParticipant; // TODO first check for collision/overwrite and throw exception....or warning at least!
                    }
                }
            }
            else
            {
                const string EXP = "Expecting to receive a non-null GNP for ensuring map keys updated, but it is null.  Proper maintenance is likely not happening as a result.  All we have is previousGONetId: ";
                GONetLog.Warning(string.Concat(EXP, previousGONetId, " reference null? ", (object)gonetParticipant == null));
            }

            // well, looks like at time of writing there are no other ones to consider.....ok...we will monitor and hopefully keep this in mind if we add other Dictionary<uint, blah> later!
        }

        /// <summary>
        /// NOTE: The time maintained within is only updated once per main thread frame tick (i.e., call to <see cref="Update"/>).
        /// </summary>
        internal static readonly SecretaryOfTemporalAffairs Time = new SecretaryOfTemporalAffairs();

        /// <summary>
        /// This is used to know which instances were instantiated due to a remote spawn message being received/processed.
        /// See <see cref="Instantiate_Remote(InstantiateGONetParticipantEvent)"/> and <see cref="Start_AutoPropagateInstantiation_IfAppropriate(GONetParticipant)"/>.
        /// </summary>
        static readonly List<GONetParticipant> remoteSpawns_avoidAutoPropagateSupport = new List<GONetParticipant>(1000);

        static GONetMain()
        {
            //Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitMessageTypeToMessageIDMap();
            InitShouldSkipSyncSupport();
        }

        private static void InitEventSubscriptions()
        {
            EventBus.Subscribe<IGONetEvent>(OnAnyEvent_RelayToRemoteConnections_IfAppropriate);
            EventBus.Subscribe<IPersistentEvent>(OnPersistentEvent_KeepTrack);
            EventBus.Subscribe<PersistentEvents_Bundle>(OnPersistentEventsBundle_ProcessAll_Remote, envelope => envelope.IsSourceRemote);
            EventBus.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent_Remote, envelope => envelope.IsSourceRemote);

            var subscription = EventBus.Subscribe<DestroyGONetParticipantEvent>(OnDestroyEvent_Remote, envelope => envelope.IsSourceRemote);
            subscription.SetSubscriptionPriority_INTERNAL(int.MinValue); // process internally LAST since the GO will be destroyed and other subscribers may want to do something just prior to it being destroyed

            EventBus.Subscribe<SyncEvent_ValueChangeProcessed>(OnSyncValueChangeProcessed_Persist_Local);

            EventBus.Subscribe<SyncEvent_GONetParticipant_GONetId>(OnGONetIdChanged);
            EventBus.Subscribe<SyncEvent_GONetParticipant_OwnerAuthorityId>(OnOwnerAuthorityIdChanged);
        }

        private static void OnSyncValueChangeProcessed_Persist_Local(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            OnSyncValueChangeProcessed_Persist_Local(eventEnvelope.Event);
        }

        private static void OnSyncValueChangeProcessed_Persist_Local(SyncEvent_ValueChangeProcessed @event, bool doesRequireCopy = true)
        {
            SyncEvent_ValueChangeProcessed instanceToEnqueue;
            if (doesRequireCopy)
            {
                // IMPORTANT: have to make a copy since these are pooled and we are not using the data immediately and GONet will return the event to the pool after this method exits...we need to keep a copy with good data until later on when we actually save
                SyncEvent_ValueChangeProcessed copy = GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateCopy(@event);
                instanceToEnqueue = copy;
            }
            else
            {
                instanceToEnqueue = @event;
            }

            instanceToEnqueue.ProcessedAtElapsedTicks = Time.ElapsedTicks;

            SyncEventsSaveSupport syncEventsToSaveQueue = syncEventsToSaveQueueByEventType[@event.GetType()];
            syncEventsToSaveQueue.queue_needsSavingASAP.Enqueue(instanceToEnqueue); // NOTE: instanceToEnqueu will get returned to its pool when this queue is processed!
        }

        private static void OnPersistentEventsBundle_ProcessAll_Remote(GONetEventEnvelope<PersistentEvents_Bundle> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            foreach (var item in eventEnvelope.Event.PersistentEvents)
            {
                persistentEventsThisSession.AddLast(item);

                if (item is InstantiateGONetParticipantEvent)
                {
                    Instantiate_Remote((InstantiateGONetParticipantEvent)item);
                }
            }
        }

        /// <summary>
        /// Definition of "if appropriate":
        ///     -The server will always send to remote connections....clients only send to remote connections (i.e., just to server) when locally sourced!
        /// </summary>
        private static void OnAnyEvent_RelayToRemoteConnections_IfAppropriate(GONetEventEnvelope<IGONetEvent> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            if (eventEnvelope.Event is ILocalOnlyPublish)
            {
                return;
            }

            if (IsServer && eventEnvelope.IsSourceRemote) // in this case we have to be more selective and avoid sending to the remote originator!
            {
                int returnBytesUsedCount;
                byte[] bytes = SerializationUtils.SerializeToBytes(eventEnvelope.Event, out returnBytesUsedCount); // TODO FIXME if the envelope is processed from a remote source, then we SHOULD attach the bytes to it and reuse them!

                uint count = _gonetServer.numConnections;// remoteClients.Length;
                for (int i = 0; i < count; ++i)
                {
                    GONetConnection_ServerToClient remoteClientConnection = _gonetServer.remoteClients[i].ConnectionToClient;
                    if (remoteClientConnection.OwnerAuthorityId != eventEnvelope.SourceAuthorityId)
                    {
                        GONetChannelId channelId = GONetChannel.EventSingles_Reliable; // TODO FIXME the envelope should have this on it as well if remote source
                        SendBytesToRemoteConnection(remoteClientConnection, bytes, returnBytesUsedCount, channelId);
                    }
                }

                SerializationUtils.ReturnByteArray(bytes);
            }
            else if (IsServer || !eventEnvelope.IsSourceRemote)
            {
                int returnBytesUsedCount;
                byte[] bytes = SerializationUtils.SerializeToBytes(eventEnvelope.Event, out returnBytesUsedCount);
                bool shouldSendRelilably = true; // TODO support unreliable events?
                SendBytesToRemoteConnections(bytes, returnBytesUsedCount, shouldSendRelilably ? GONetChannel.EventSingles_Reliable : GONetChannel.EventSingles_Unreliable);
                SerializationUtils.ReturnByteArray(bytes);
            }
        }

        private static void OnPersistentEvent_KeepTrack(GONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            bool doesCurrentEventCancelOutPreviousEvent = false;

            IPersistentEvent lastConsideredEvent = null;
            ICancelOutOtherEvents iCancelOthers = eventEnvelope.Event as ICancelOutOtherEvents;
            if (iCancelOthers != null)
            {
                var enumerator = persistentEventsThisSession.GetEnumerator(); // TODO consider narrowing down to just the items in there that match iCancelOthers.OtherEventTypeCancelledOut, but Linq is not desired...not sure how best to do this
                while (!doesCurrentEventCancelOutPreviousEvent && enumerator.MoveNext())
                {
                    lastConsideredEvent = enumerator.Current;
                    if (lastConsideredEvent.GetType() == iCancelOthers.OtherEventTypeCancelledOut && iCancelOthers.DoesCancelOutOtherEvent(lastConsideredEvent))
                    {
                        doesCurrentEventCancelOutPreviousEvent = true;
                    }
                }
            }

            if (doesCurrentEventCancelOutPreviousEvent)
            {
                persistentEventsThisSession.Remove(lastConsideredEvent); // remove the cancelled out earlier event
            }
            else
            {
                persistentEventsThisSession.AddLast(eventEnvelope.Event);
            }
        }

        private static void OnInstantiationEvent_Remote(GONetEventEnvelope<InstantiateGONetParticipantEvent> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            //const string IR = "pub/sub Instantiate REMOTE about to process...";
            //GONetLog.Debug(IR);

            GONetParticipant instance = Instantiate_Remote(eventEnvelope.Event);

            if (IsServer)
            {
                GONetLocal gonetLocal = instance.gameObject.GetComponent<GONetLocal>();
                if (gonetLocal != null)
                {
                    Server_OnNewClientInstantiatedItsGONetLocal(gonetLocal);
                }
            }

            if (instance.ShouldHideDuringRemoteInstantiate && valueBlendingBufferLeadSeconds > 0)
            {
                GlobalSessionContext.StartCoroutine(OnInstantiationEvent_Remote_HideDuringBufferLeadTime(instance));
            }
        }

        /// <summary>
        /// PRE: <see cref="valueBlendingBufferLeadSeconds"/> is greater than 0
        /// IMPORTANT: If there is a transition of authority (i.e., a call to <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/>) and 
        ///            <see cref="GONetParticipant.IsMine"/> becomes true during this waiting period, then the renders will bet set to enabled instead of waiting.
        /// </summary>
        private static IEnumerator OnInstantiationEvent_Remote_HideDuringBufferLeadTime(GONetParticipant instance)
        {
            Renderer[] activeRenderers = instance.GetComponentsInChildren<Renderer>(false);

            int count = activeRenderers.Length;
            for (int i = 0; i < count; ++i)
            {
                activeRenderers[i].enabled = false;
            }

            long startTicks = Time.ElapsedTicks;
            while (instance != null && !instance.IsMine && ((Time.ElapsedTicks - startTicks) < valueBlendingBufferLeadTicks))
            {
                yield return null;
            }

            for (int i = 0; i < count; ++i)
            {
                Renderer renderer = activeRenderers[i];
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
        }

        private static void OnDestroyEvent_Remote(GONetEventEnvelope<DestroyGONetParticipantEvent> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            GONetParticipant gonetParticipant;
            if (gonetParticipantByGONetIdMap.TryGetValue(eventEnvelope.Event.GONetId, out gonetParticipant))
            {
                gonetIdsDestroyedViaPropagation.Add(gonetParticipant.GONetId); // this container must have the gonetId added first in order to prevent OnDestroy_AutoPropagateRemoval_IfAppropriate from thinking it is appropriate to propagate more when it is already being propagated

                if (gonetParticipant == null || gonetParticipant.gameObject == null)
                {
                    const string REC = "Received remote notification to destroy a GNP, but Unity says it's already null.  Ensure only the owner (i.e., GNP.IsMine) is the one who calls Unity's Destroy() method and the propogation across the network will be automatic via GONet.";
                    GONetLog.Error(REC);
                }
                else
                {
                    //const string DEAD = "Received remote notification to destroy a GNP.GONetId: ";
                    //GONetLog.Info(string.Concat(DEAD, gonetParticipant.GONetId));

                    UnityEngine.Object.Destroy(gonetParticipant.gameObject);
                }
            }
            else
            {
                const string DGNP = "Destroy GONetParticipant event received from remote source, but we have no record of this to destroy it.  GONetId: ";
                GONetLog.Warning(string.Concat(DGNP, eventEnvelope.Event.GONetId));
            }
        }

        private static void InitShouldSkipSyncSupport()
        {
            GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsRotationSyncd] = IsRotationNotSyncd;

            GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsPositionSyncd] = IsPositionNotSyncd;
        }

        private static bool IsRotationNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
        {
            return !monitoringSupport.syncCompanion.gonetParticipant.IsRotationSyncd;
        }

        private static bool IsPositionNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
        {
            return !monitoringSupport.syncCompanion.gonetParticipant.IsPositionSyncd;
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            const string EM = "Error Message: ";
            const string ST = "\nError Stacktrace:\n";
            GONetLog.Error(string.Concat(EM, e.Exception.Message, ST, e.Exception.StackTrace));
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            const string EM = "Error Message: ";
            const string ST = "\nError Stacktrace:\n";
            Exception exception = (e.ExceptionObject as Exception);
            GONetLog.Error(string.Concat(EM, exception.Message, ST, exception.StackTrace));
        }

        #region public methods

        #region instantiate special support

        /// <summary>
        /// <para>This is the option to instantiate/spawn something that uses one original/prefab/template for the authority/owner/originator and a different one for everyone else (i.e., non-authorities).</para>
        /// <para>This is useful in some cases for instantiating/spawning things like players where the authority (i.e., the player) has certain scripts attached and only a model/mesh with arms and legs and non-authorities get less scripts and the full model/mesh.</para>
        /// <para>Only the authority/owner/originator can call this method (i.e., the resulting instance's <see cref="GONetParticipant.OwnerAuthorityId"/> will be set to <see cref="MyAuthorityId"/>).</para>
        /// <para>It operates within GONet just like <see cref="UnityEngine.Object.Instantiate{T}(T)"/>, where there is automatic spawn propagation support to all other machines in this game/session on the network.</para>
        /// <para>However, the difference is using this method ensures the other non-owner (networked) parties automatically instantiate <paramref name="nonAuthorityAlternateOriginal"/> instead of <paramref name="authorityOriginal"/>, which will be instantiated here for the authority/owner.</para>
        /// <para>Therefore, if you simply want to instantiate something across the network and it should be the same original <see cref="UnityEngine.Object"/> template, then use <see cref=""/></para>
        /// </summary>
        /// <param name="authorityOriginal"></param>
        /// <param name="nonAuthorityAlternateOriginal"></param>
        /// <returns></returns>
        public static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal)
        {
            return GONetSpawnSupport_Runtime.Instantiate_WithNonAuthorityAlternate(authorityOriginal, nonAuthorityAlternateOriginal);
        }

        /// <summary>
        /// <para>This is the option to instantiate/spawn something that uses one original/prefab/template for the authority/owner/originator and a different one for everyone else (i.e., non-authorities).</para>
        /// <para>This is useful in some cases for instantiating/spawning things like players where the authority (i.e., the player) has certain scripts attached and only a model/mesh with arms and legs and non-authorities get less scripts and the full model/mesh.</para>
        /// <para>Only the authority/owner/originator can call this method (i.e., the resulting instance's <see cref="GONetParticipant.OwnerAuthorityId"/> will be set to <see cref="MyAuthorityId"/>).</para>
        /// <para>It operates within GONet just like <see cref="UnityEngine.Object.Instantiate{T}(T)"/>, where there is automatic spawn propagation support to all other machines in this game/session on the network.</para>
        /// <para>However, the difference is using this method ensures the other non-owner (networked) parties automatically instantiate <paramref name="nonAuthorityAlternateOriginal"/> instead of <paramref name="authorityOriginal"/>, which will be instantiated here for the authority/owner.</para>
        /// <para>Therefore, if you simply want to instantiate something across the network and it should be the same original <see cref="UnityEngine.Object"/> template, then use <see cref=""/></para>
        /// </summary>
        /// <param name="authorityOriginal"></param>
        /// <param name="nonAuthorityAlternateOriginal"></param>
        /// <returns></returns>
        public static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal, Vector3 position, Quaternion rotation)
        {
            return GONetSpawnSupport_Runtime.Instantiate_WithNonAuthorityAlternate(authorityOriginal, nonAuthorityAlternateOriginal, position, rotation);
        }

        #endregion

        /// <summary>
        /// Returns null if not found.
        /// </summary>
        public static GONetParticipant GetGONetParticipantById(uint gonetId)
        {
            GONetParticipant gonetParticipant = null;
            gonetParticipantByGONetIdMap.TryGetValue(gonetId, out gonetParticipant);
            return gonetParticipant;
        }

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread"/>
        /// </summary>
        public static void SendBytesToRemoteConnections(byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            SendBytesToRemoteConnection(null, bytes, bytesUsedCount, channelId); // passing null will result in sending to all remote connections
        }

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread"/>
        /// </summary>
        private static void SendBytesToRemoteConnection(GONetConnection sendToConnection, byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            ConcurrentQueue<NetworkData> readyToReturnQueue;
            if (readyToReturnQueue_ThreadMap.TryGetValue(Thread.CurrentThread, out readyToReturnQueue))
            {
                int processedCount = 0;
                int readyCount = readyToReturnQueue.Count;
                NetworkData readyToReturn;
                while (processedCount < readyCount && readyToReturnQueue.TryDequeue(out readyToReturn))
                {
                    readyToReturn.messageBytesBorrowedFromPool.Return(readyToReturn.messageBytes); // since we now know we are on the correct thread (i.e.., same as borrowed on) we can return it to pool
                    ++processedCount;
                }
            }
            else
            {
                readyToReturnQueue_ThreadMap[Thread.CurrentThread] = new ConcurrentQueue<NetworkData>();
            }

            ArrayPool<byte> endOfTheLineSendArrayPool;
            if (!netThread_outgoingNetworkDataArrayPool_ThreadMap.TryGetValue(Thread.CurrentThread, out endOfTheLineSendArrayPool))
            {
                endOfTheLineSendArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);
                netThread_outgoingNetworkDataArrayPool_ThreadMap[Thread.CurrentThread] = endOfTheLineSendArrayPool;
            }

            byte[] bytesCopy = endOfTheLineSendArrayPool.Borrow(bytesUsedCount);
            Buffer.BlockCopy(bytes, 0, bytesCopy, 0, bytesUsedCount);

            NetworkData networkData = new NetworkData()
            {
                messageBytesBorrowedFromPool = endOfTheLineSendArrayPool,
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                messageBytes = bytesCopy,
                bytesUsedCount = bytesUsedCount,
                relatedConnection = sendToConnection,
                channelId = channelId
            };

            endOfTheLineSendQueue.Enqueue(networkData);
        }

        static readonly ConcurrentQueue<NetworkData> endOfTheLineSendQueue = new ConcurrentQueue<NetworkData>();
        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> netThread_outgoingNetworkDataArrayPool_ThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

        internal static ulong tickCount_endOfTheLineSendAndSave_Thread;
        private static volatile bool isRunning_endOfTheLineSendAndSave_Thread;
        private static void SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread()
        {
            tickCount_endOfTheLineSendAndSave_Thread = 0;

            while (isRunning_endOfTheLineSendAndSave_Thread)
            {
                { // Do send stuffs
                    int processedCount = 0;
                    int count = endOfTheLineSendQueue.Count;
                    NetworkData networkData;
                    while (processedCount < count && endOfTheLineSendQueue.TryDequeue(out networkData))
                    {
                        if (networkData.relatedConnection == null)
                        {
                            if (IsServer)
                            {
                                if (gonetServer != null)
                                {
                                    //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds + " size: " + networkData.bytesUsedCount);
                                    gonetServer.SendBytesToAllClients(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                                }
                            }
                            else
                            {
                                if (GONetClient != null)
                                {
                                    while (isRunning_endOfTheLineSendAndSave_Thread && !GONetClient.IsConnectedToServer)
                                    {
                                        const string SLEEP = "SLEEP!  I am not yet connected.  I have data to send, but need to wait to be connected in order to send it.";
                                        GONetLog.Info(SLEEP);

                                        Thread.Sleep(33); // TODO FIXME I am sure things will eventually get into strange states out in the wild where clients spotty network puts them here too often and I wonder if this is problematic...certainly quick/dirty and nieve!
                                    }

                                    if (isRunning_endOfTheLineSendAndSave_Thread)
                                    {
                                        //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds + " size: " + networkData.bytesUsedCount);
                                        GONetClient.SendBytesToServer(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                                    }
                                }
                            }
                        }
                        else
                        {
                            networkData.relatedConnection.SendMessageOverChannel(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                        }

                        { // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                            ConcurrentQueue<NetworkData> readyToReturnQueue = readyToReturnQueue_ThreadMap[networkData.messageBytesBorrowedOnThread];
                            readyToReturnQueue.Enqueue(networkData);
                        }
                    }
                }

                { // Do save stuffs
                    var syncvEventsEnumerator = syncEventsToSaveQueueByEventType.GetEnumerator();
                    while (syncvEventsEnumerator.MoveNext())
                    {
                        SyncEventsSaveSupport saveSupport = syncvEventsEnumerator.Current.Value;
                        if (saveSupport.queue_needsSaving.Count > 0 && saveSupport.IsSaving)
                        {
                            lock (saveSupport.queue_needsSaving)
                            {
                                AppendToDatabaseFile_SaveThread(saveSupport.queue_needsSaving); // this is the act of saving...after this, they no longer need saving
                            }
                            saveSupport.OnAfterAllSaved_SaveThread();
                        }
                    }
                }

                ++tickCount_endOfTheLineSendAndSave_Thread;
            }
        }

        #endregion

        #region internal methods

        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_reliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Reliable, false);
        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_unreliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Unreliable, false);

        static Thread endOfLineSendAndSaveThread;

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Update()
        {
            Time.Update();

            ProcessIncomingBytes_QueuedNetworkData_MainThread();

            AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable itemsToProcessEveryFrame;
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_reliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_unreliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }

            int mainThreadSupportCount = autoSyncProcessingSupports_UnityMainThread.Count;
            for (int i = 0; i < mainThreadSupportCount; ++i)
            {
                AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable autoSyncProcessingSupport_mainThread = autoSyncProcessingSupports_UnityMainThread[i];
                autoSyncProcessingSupport_mainThread.ProcessASAP();
            }

            var enumerator_activeAutoSyncCompanionsMapByCodeGenerationId = activeAutoSyncCompanionsByCodeGenerationIdMap.GetEnumerator();
            while (enumerator_activeAutoSyncCompanionsMapByCodeGenerationId.MoveNext())
            {
                var kvp_activeAutoSyncCompanionsMapForCodeGenerationId = enumerator_activeAutoSyncCompanionsMapByCodeGenerationId.Current;

                var enumerator_activeAutoSyncCompanionsMap = kvp_activeAutoSyncCompanionsMapForCodeGenerationId.Value.GetEnumerator();
                while (enumerator_activeAutoSyncCompanionsMap.MoveNext())
                {
                    var kvp_activeAutoSyncCompanion = enumerator_activeAutoSyncCompanionsMap.Current;
                    var activeAutoSyncCompanion = kvp_activeAutoSyncCompanion.Value;
                    int length_valueChangesSupport = activeAutoSyncCompanion.valuesChangesSupport.Length;
                    for (int i = 0; i < length_valueChangesSupport; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = activeAutoSyncCompanion.valuesChangesSupport[i];
                        if (valueChangeSupport != null)
                        {
                            valueChangeSupport.ApplyValueBlending_IfAppropriate(valueBlendingBufferLeadTicks);
                        }
                    }
                }
            }

            PublishEvents_SyncValueChanges_SentToOthers();
            PublishEvents_SyncValueChanges_ReceivedFromOthers();
            SaveEventsInQueueASAP_IfAppropriate();

            if (endOfLineSendAndSaveThread == null)
            {
                isRunning_endOfTheLineSendAndSave_Thread = true;
                endOfLineSendAndSaveThread = new Thread(SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread);
                endOfLineSendAndSaveThread.Start();
            }

            if (IsServer)
            {
                gonetServer?.Update();
            }

            if (IsClient)
            {
                Client_SyncTimeWithServer_Initiate_IfAppropriate();
                GONetClient?.Update();
            }

            foreach (var gnp in gnpsAwaitingCompanion)
            {
                if (gnp != null)
                {
                    GONetLog.Debug("gnp now not unity null...gnp.gonetId: " + gnp.GONetId);

                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap;
                    if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gnp.codeGenerationId, out companionMap))
                    {
                        if (companionMap.ContainsKey(gnp))
                        {
                            GONetLog.Debug("gnp also now in map.....can now proceed with processing the remaining bytes!");
                        }
                    }
                }
            }
        }

        private static void SaveEventsInQueueASAP_IfAppropriate(bool shouldForceAppropriateness = false) // TODO put all this in another thread to not disrupt the main thread with saving!!!
        {
            var enumerator = syncEventsToSaveQueueByEventType.GetEnumerator();
            while (enumerator.MoveNext())
            {
                SyncEventsSaveSupport syncEventsToSaveQueue = enumerator.Current.Value;
                int count = syncEventsToSaveQueue.queue_needsSavingASAP.Count;
                bool isAppropriate = shouldForceAppropriateness || (!syncEventsToSaveQueue.IsSaving && count >= SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE); // TODO add in another condition that makes it appropriate: enough time passed since last save (e.g., 30 seconds)
                if (isAppropriate)
                {
                    syncEventsToSaveQueue.InitiateSave_MainUnityThread();
                }

                { // return some that are ready...just be sure to spread it out over multiple frames
                    syncEventsToSaveQueue.ReturnSaved_SpreadOverFrames_MainUnityThread();
                }
            }
        }

        static readonly Queue<SyncEvent_ValueChangeProcessed> syncEventsToSaveQueue_hereUseMeToAvoidMultiLevelEnumerationErrors = new Queue<SyncEvent_ValueChangeProcessed>(SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 100);
        /// <summary>
        /// PRE: call this not from the main unity thread, but rather the "save thread" (which is <see cref="endOfLineSendAndSaveThread"/>)
        /// </summary>
        private static void AppendToDatabaseFile_SaveThread(Queue<SyncEvent_ValueChangeProcessed> syncEventsToSaveQueue)
        {
            syncEventsToSaveQueue_hereUseMeToAvoidMultiLevelEnumerationErrors.Clear();
            var sourceEnumerator = syncEventsToSaveQueue.GetEnumerator();
            while (sourceEnumerator.MoveNext())
            {
                syncEventsToSaveQueue_hereUseMeToAvoidMultiLevelEnumerationErrors.Enqueue(sourceEnumerator.Current);
            }

            SyncEvent_PersistenceBundle.Instance.bundle = syncEventsToSaveQueue_hereUseMeToAvoidMultiLevelEnumerationErrors;
            int returnBytesUsedCount;
            byte[] bytes = SerializationUtils.SerializeToBytes(SyncEvent_PersistenceBundle.Instance, out returnBytesUsedCount);

            persistenceFileStream.Write(bytes, 0, returnBytesUsedCount);
            persistenceFileStream.Flush(true);

            //GONetLog.Debug("WROTE DB!!!! ++++++++++++++++++++++++++++++ count: " + syncEventsToSaveQueue_hereUseMeToAvoidMultiLevelEnumerationErrors.Count);

            /*{ // example of reading from file:
                byte[] allBytes = File.ReadAllBytes(persistenceFilePath);
                int bytesRead = 0;
                while (bytesRead < allBytes.Length)
                {
                    int bytesReadInner;
                    SyncEvent_PersistenceBundle bundle = SerializationUtils.DeserializeFromBytes<SyncEvent_PersistenceBundle>(allBytes, bytesRead, out bytesReadInner);
                    bytesRead += bytesReadInner;
                }
            }*/

            SerializationUtils.ReturnByteArray(bytes);
        }

        private static void PublishEvents_SyncValueChanges_ReceivedFromOthers()
        {
            int count = syncValueChanges_ReceivedFromOtherQueue.Count;
            for (int i = 0; i < count; ++i)
            {
                var @event = syncValueChanges_ReceivedFromOtherQueue.Dequeue();
                try
                {
                    EventBus.Publish(@event);
                }
                catch (Exception e)
                {
                    const string BOO = "Boo.  Publishing this sync value change event failed.  Error.Message: ";
                    GONetLog.Error(string.Concat(BOO, e.Message));
                }
            }
        }

        private static void PublishEvents_SyncValueChanges_SentToOthers()
        {
            var eventDictionaryEnumerator = syncValueChanges_SendToOthersQueue_ByThreadMap.GetEnumerator();
            while (eventDictionaryEnumerator.MoveNext())
            {
                ConcurrentQueue<SyncEvent_ValueChangeProcessed> eventQueue = eventDictionaryEnumerator.Current.Value;
                int count = eventQueue.Count;
                SyncEvent_ValueChangeProcessed @event;
                while (count > 0 && eventQueue.TryDequeue(out @event))
                {
                    try
                    {
                        EventBus.Publish(@event);
                    }
                    catch (Exception e)
                    {
                        const string BOO = "Boo.  Publishing this sync value change event failed.  Error.Message: ";
                        GONetLog.Error(string.Concat(BOO, e.Message));
                    }
                    finally
                    {
                        --count;
                    }
                }
            }
        }

        #region time sync client-server-client

        /// <summary>
        /// How close the clients time must be to the server before the gap is considered closed and time can go
        /// from being sync'd every <see cref="CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED"/> ticks 
        /// to every <see cref="CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED"/> ticks for maintenance.
        /// </summary>
        static readonly long CLIENT_SYNC_TIME_GAP_TICKS = TimeSpan.FromSeconds(1f / 60f).Ticks;
        static bool client_hasClosedTimeSyncGapWithServer;
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED = TimeSpan.FromSeconds(1f / 5f).Ticks;
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED = TimeSpan.FromSeconds(5f).Ticks;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__UNTIL_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__POST_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED;
        static readonly long DIFF_TICKS_TOO_BIG_FOR_EASING = TimeSpan.FromSeconds(1f).Ticks; // if you are over a second out of sync...do not ease as that will take forever
        static bool client_hasSentSyncTimeRequest;
        static DateTime client_lastSyncTimeRequestSent;
        const int CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE = 60;
        static readonly Dictionary<long, RequestMessage> client_lastFewTimeSyncsSentByUID = new Dictionary<long, RequestMessage>(CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE);
        static long client_mostRecentTimeSyncResponseSentTicks;

        internal static readonly float BLENDING_BUFFER_LEAD_SECONDS_DEFAULT = 0.25f; // 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: 0.25f
        internal static float valueBlendingBufferLeadSeconds = BLENDING_BUFFER_LEAD_SECONDS_DEFAULT;
        internal static long valueBlendingBufferLeadTicks = TimeSpan.FromSeconds(BLENDING_BUFFER_LEAD_SECONDS_DEFAULT).Ticks;

        /// <summary>
        /// 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: TimeSpan.FromMilliseconds(250).Ticks;
        /// </summary>
        private static void SetValueBlendingBufferLeadTimeFromMilliseconds(int valueBlendingBufferLeadTimeMilliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(valueBlendingBufferLeadTimeMilliseconds);
            valueBlendingBufferLeadSeconds = (float)timeSpan.TotalSeconds;
            valueBlendingBufferLeadTicks = timeSpan.Ticks;
        }

        /// <summary>
        /// "IfAppropriate" is to indicate this runs on a schedule....if it is not the right time, this will do nothing.
        /// </summary>
        private static void Client_SyncTimeWithServer_Initiate_IfAppropriate()
        {
            DateTime now = DateTime.UtcNow;
            long syncEveryTicks = client_hasClosedTimeSyncGapWithServer ? CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED : CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED;
            bool isAppropriate = !client_hasSentSyncTimeRequest || (now - client_lastSyncTimeRequestSent).Duration().Ticks > syncEveryTicks;
            if (isAppropriate)
            {
                client_hasSentSyncTimeRequest = true;
                client_lastSyncTimeRequestSent = now;

                { // the actual sync request:
                    RequestMessage timeSync = new RequestMessage(Time.ElapsedTicks);

                    client_lastFewTimeSyncsSentByUID[timeSync.UID] = timeSync;
                    if (client_lastFewTimeSyncsSentByUID.Count > CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE)
                    {
                        // TODO do not let client_lastFewTimeSyncsSentByUID get larger than TIME_SYNCS_SENT_QUEUE_SIZE.....delete oldest
                    }

                    using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
                    {
                        { // header...just message type/id...well, and now time 
                            uint messageID = messageTypeToMessageIDMap[typeof(RequestMessage)];
                            bitStream.WriteUInt(messageID);

                            bitStream.WriteLong(timeSync.OccurredAtElapsedTicks);
                        }

                        // body
                        bitStream.WriteLong(timeSync.UID);

                        bitStream.WriteCurrentPartialByte();

                        int bytesUsedCount = bitStream.Length_WrittenBytes;
                        byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                        Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                        SendBytesToRemoteConnections(bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable);

                        //GONetLog.Debug("just sent time sync to server....my time (seconds): " + TimeSpan.FromTicks(timeSync.OccurredAtElapsedTicks).TotalSeconds);

                        mainThread_miscSerializationArrayPool.Return(bytes);
                    }
                }
            }
        }

        private static void Server_SyncTimeWithClient_Respond(long requestUID, GONetConnection connectionToClient)
        {
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(ResponseMessage)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                // body
                bitStream.WriteLong(requestUID);

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                //GONetLog.Debug("about to send time sync to client....");

                SendBytesToRemoteConnection(connectionToClient, bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable);

                mainThread_miscSerializationArrayPool.Return(bytes);
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
                    long requestSentTicks_Client = requestMessage.OccurredAtElapsedTicks;
                    long rtt_ticks = responseReceivedTicks_Client - requestSentTicks_Client;

                    GONetClient.connectionToServer.RTT_Latest = (float)TimeSpan.FromTicks(rtt_ticks).TotalSeconds;
                    //GONetLog.Debug("RTT_Latest: " + gonetClient.connectionToServer.RTT_Latest + " RTT_RecentAverage: " + gonetClient.connectionToServer.RTT_RecentAverage + " their.rtt: " + gonetClient.connectionToServer.RTT);

                    long assumedNetworkDelayTicks = TimeSpan.FromSeconds(GONetClient.connectionToServer.RTT_RecentAverage).Ticks >> 1; // divide by 2
                    long newClientTimeTicks = server_elapsedTicksAtSendResponse + assumedNetworkDelayTicks;

                    long previous = Time.ElapsedTicks;
                    Time.SetFromAuthority(newClientTimeTicks);

                    OnSyncValueChangeProcessed_Persist_Local(
                        SyncEvent_Time_ElapsedTicks_SetFromAuthority.Borrow(previous, newClientTimeTicks, GONetClient.connectionToServer.RTT_Latest, GONetClient.connectionToServer.RTT_RecentAverage, GONetClient.connectionToServer.RTTMilliseconds_LowLevelTransportProtocol),
                        false); // NOTE: false is to indicate no copy needed like normally needed due to processing flow of normal/automatic sync events, which this is not

                    if (!client_hasClosedTimeSyncGapWithServer)
                    {
                        long diffTicksABS = Math.Abs(responseReceivedTicks_Client - newClientTimeTicks);
                        if (diffTicksABS < CLIENT_SYNC_TIME_GAP_TICKS)
                        {
                            client_hasClosedTimeSyncGapWithServer = true;
                        }
                    }

                }
                //else { GONetLog.Warning("The time sync response from server is somehow (i.e., UDP out of order packets) older than the another response already process.  It makes no sense to use this out of date information."); }
            }
        }

        #endregion

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Shutdown()
        {
            isRunning_endOfTheLineSendAndSave_Thread = false;

            if (IsServer)
            {
                gonetServer?.Stop();
            }
            else
            {
                GONetClient?.Disconnect();
            }

            var enumeratorThread = autoSyncProcessingSupportByFrequencyMap.GetEnumerator();
            while (enumeratorThread.MoveNext())
            {
                enumeratorThread.Current.Value.Dispose();
            }

            {
                SaveEventsInQueueASAP_IfAppropriate(true);
                if (persistenceFileStream != null)
                {
                    persistenceFileStream.Close();
                }

                RemitEula_IfAppropriate(persistenceFilePath); // IMPORTANT: this MUST come AFTER SaveEventsInQueue_IfAppropriate(true) and closing stream to ensure all the stuffs is written than is to be executed remit eula style
            }
        }

        private static void RemitEula_IfAppropriate(string eulaFilePath)
        {
            if (File.Exists(eulaFilePath))
            {
                bool isEulaRequirementMetOtherMeans = (DateTime.UtcNow.Ticks - ticksAtLastInit_UtcNow) < 3007410000 || (IsServer && server_lastAssignedAuthorityId == OwnerAuthorityId_Unset);
                if (!isEulaRequirementMetOtherMeans)
                {
                    const string EULA_REMIT_URL = "https://unitygo.net/wp-json/eula/v1/remit";
                    const string HDR_FN = "Filename";
                    const string KAPUT = "PUT";
                    const string OCCY = "application/octet-stream";

                    WebRequest www = WebRequest.Create(EULA_REMIT_URL);
                    www.Headers[HDR_FN] = string.Concat(Path.GetFileName(eulaFilePath).Replace(SGUID, Math.Abs(SessionGUID).ToString()).Replace(MOAId, MyAuthorityId.ToString()));
                    www.Method = KAPUT;
                    www.ContentType = OCCY;

                    byte[] eulaFileBytes = File.ReadAllBytes(eulaFilePath);
                    www.ContentLength = eulaFileBytes.Length;
                    using (var requestDataStream = www.GetRequestStream())
                    {
                        requestDataStream.Write(eulaFileBytes, 0, eulaFileBytes.Length);
                    }

                    using (WebResponse response = www.GetResponse())
                    {
                        using (var dataStream = response.GetResponseStream())
                        {
                            StreamReader reader = new StreamReader(dataStream);
                            string responseFromServer = reader.ReadToEnd();
                            GONetLog.Debug(responseFromServer);
                        }
                    }
                }

                File.Delete(eulaFilePath); // keep HD maintenance up by removing unneeded file
            }
        }

        internal struct NetworkData
        {
            public GONetConnection relatedConnection;
            public ArrayPool<byte> messageBytesBorrowedFromPool;
            public Thread messageBytesBorrowedOnThread;
            public byte[] messageBytes;
            public int bytesUsedCount;
            public GONetChannelId channelId;
        }

        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> netThread_incomingNetworkDataArrayPool_ThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();
        static readonly ConcurrentQueue<NetworkData> incomingNetworkData = new ConcurrentQueue<NetworkData>();
        static readonly ConcurrentDictionary<Thread, ConcurrentQueue<NetworkData>> readyToReturnQueue_ThreadMap = new ConcurrentDictionary<Thread, ConcurrentQueue<NetworkData>>();

        /// <summary>
        /// Lengthy and nerdy way of naming this class "Time" just like <see cref="UnityEngine.Time"/> to avoid conflicts in a memorable way!
        /// </summary>
        public sealed class SecretaryOfTemporalAffairs
        {
            public delegate void TimeChangeArgs(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks);
            public event TimeChangeArgs TimeSetFromAuthority;

            long baselineTicks;
            long lastSetFromAuthorityDiffTicks;
            long lastSetFromAuthorityAtTicks;

            public const double ElapsedSecondsUnset = -1;

            public long ElapsedTicks { get; private set; }
            double elapsedSeconds = ElapsedSecondsUnset;
            public double ElapsedSeconds => elapsedSeconds;

            internal volatile int updateCount = 0;
            public int UpdateCount
            {
                get { return updateCount; }
                private set { updateCount = value; }
            }

            internal void SetFromAuthority(long elapsedTicksFromAuthority)
            {
                lastSetFromAuthorityDiffTicks = elapsedTicksFromAuthority - ElapsedTicks;

                long elapsedTicksBefore = ElapsedTicks;
                double elapsedSecondsBefore = elapsedSeconds;

                lastSetFromAuthorityAtTicks = HighResolutionTimeUtils.Now.Ticks;
                baselineTicks = lastSetFromAuthorityAtTicks - elapsedTicksFromAuthority;
                ElapsedTicks = lastSetFromAuthorityAtTicks - baselineTicks;
                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                /* if you want debugging in log
                const string STR_ElapsedTimeClient = "ElapsedTime client of: ";
                const string STR_BeingOverwritten = " is being overwritten from authority source to: ";
                const string STR_DoubleCheck = " seconds and here is new client value to double check it worked correctly: ";
                const string STR_Diff = " lastSetFromAuthorityDiffTicks (well, as ms): ";
                GONetLog.Info(string.Concat(STR_ElapsedTimeClient, elapsedSecondsBefore, STR_BeingOverwritten, TimeSpan.FromTicks(elapsedTicksFromAuthority).TotalSeconds, STR_DoubleCheck, elapsedSeconds, STR_Diff, TimeSpan.FromTicks(lastSetFromAuthorityDiffTicks).TotalMilliseconds));
                */

                TimeSetFromAuthority?.Invoke(elapsedSecondsBefore, elapsedSeconds, elapsedTicksBefore, ElapsedTicks);
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
                bool shouldEase = Math.Abs(lastSetFromAuthorityDiffTicks) < DIFF_TICKS_TOO_BIG_FOR_EASING;
                if (shouldEase)
                { // IMPORTANT: This code eases the adjustment (i.e., diff) back to resync time over the entire period between resyncs to avoid a possibly dramatic jump in time just after a resync!
                    if (lastSetFromAuthorityDiffTicks != 0)
                    {
                        long ticksSinceLastSetFromAuthority = HighResolutionTimeUtils.Now.Ticks - lastSetFromAuthorityAtTicks;
                        float syncEveryTicks = client_hasClosedTimeSyncGapWithServer ? CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__POST_GAP_CLOSED : CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__UNTIL_GAP_CLOSED;
                        float inverseLerpBetweenSyncs = ticksSinceLastSetFromAuthority / syncEveryTicks;
                        if (inverseLerpBetweenSyncs < 1f) // if 1 or greater there will be nothing to add based on calculations
                        {
                            return (long)(lastSetFromAuthorityDiffTicks * (1f - inverseLerpBetweenSyncs));
                        }
                    }
                }
                //else
                //{
                //const string SENSE = "Does not make sense to apply time sync from this client to the server time using an easing over time due to the large gap between what this client has as the time and what the server says in the time.  Diff (seconds): ";
                //GONetLog.Info(string.Concat(SENSE, TimeSpan.FromTicks(lastSetFromAuthorityDiffTicks).TotalSeconds));
                //}

                return 0;
            }
        }

        /// <summary>
        /// All incoming network bytes need to come here first, then <see cref="ProcessIncomingBytes_QueuedNetworkData_MainThread"/>.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread and eventually, the triage here will eventually send the incoming bytes to <see cref="ProcessIncomingBytes_QueuedNetworkData_MainThread"/>.
        /// </summary>
        internal static void ProcessIncomingBytes_TriageFromAnyThread(GONetConnection sourceConnection, byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            //GONetLog.Debug("received something.... size: " + bytesUsedCount);

            ArrayPool<byte> pool;
            if (!netThread_incomingNetworkDataArrayPool_ThreadMap.TryGetValue(Thread.CurrentThread, out pool))
            {
                pool = new ArrayPool<byte>(1000, 10, 1024, 2048);
                netThread_incomingNetworkDataArrayPool_ThreadMap[Thread.CurrentThread] = pool;
            }

            ConcurrentQueue<NetworkData> readyToReturnQueue;
            if (readyToReturnQueue_ThreadMap.TryGetValue(Thread.CurrentThread, out readyToReturnQueue))
            {
                int processedCount = 0;
                int readyCount = readyToReturnQueue.Count;
                NetworkData readyToReturn;
                while (processedCount < readyCount && readyToReturnQueue.TryDequeue(out readyToReturn))
                {
                    readyToReturn.messageBytesBorrowedFromPool.Return(readyToReturn.messageBytes); // since we now know we are on the correct thread (i.e.., same as borrowed on) we can return it to pool
                    ++processedCount;
                }
            }
            else
            {
                readyToReturnQueue_ThreadMap[Thread.CurrentThread] = new ConcurrentQueue<NetworkData>();
            }

            NetworkData networkData = new NetworkData()
            {
                relatedConnection = sourceConnection,
                messageBytes = pool.Borrow(bytesUsedCount),
                messageBytesBorrowedFromPool = pool,
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                bytesUsedCount = bytesUsedCount,
                channelId = channelId
            };

            Buffer.BlockCopy(messageBytes, 0, networkData.messageBytes, 0, bytesUsedCount);

            incomingNetworkData.Enqueue(networkData);
        }

        #endregion

        #region private methods

        /// <summary>
        /// This is where ***all*** incoming message are run through the handling/processing logic.
        /// Call this from the main Unity thread!
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread()
        {
            NetworkData networkData;
            int count = incomingNetworkData.Count;
            for (int i = 0; i < count && !incomingNetworkData.IsEmpty; ++i)
            {
                if (incomingNetworkData.TryDequeue(out networkData))
                {
                    try
                    {
                        if (!IsChannelClientInitializationRelated(networkData.channelId) && IsClient && !_gonetClient.IsInitializedWithServer) // IMPORTANT: This check must come first as it exits early if condition met!
                        {
                            GONetClient.incomingNetworkData_mustProcessAfterClientInitialized.Enqueue(networkData);
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        GONetLog.Error(string.Concat("Error Message: ", e.Message, "\nError Stacktrace:\n", e.StackTrace));
                    }

                    ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(networkData);
                }
                else
                {
                    GONetLog.Warning("Trying to dequeue from queued up incoming network data elements and cannot....WHY?");
                }
            }
        }

        /// <summary>
        /// POST: <paramref name="networkData"/> is returned to the associated/proper queue in <see cref="readyToReturnQueue_ThreadMap"/>
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(NetworkData networkData)
        {
            try
            {
                if (networkData.channelId == GONetChannel.ClientInitialization_EventSingles_Reliable || networkData.channelId == GONetChannel.EventSingles_Reliable || networkData.channelId == GONetChannel.EventSingles_Unreliable)
                {
                    DeserializeBody_EventSingle(networkData.messageBytes, networkData.relatedConnection);
                }
                else
                {
                    using (var bitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(networkData.messageBytes, networkData.bytesUsedCount))
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

                        //GONetLog.Debug("received something....networkData.bytesUsedCount: " + networkData.bytesUsedCount);

                        {  // body:
                            if (messageType == typeof(AutoMagicalSync_ValueChanges_Message))
                            {
                                DeserializeBody_ChangesBundle(bitStream, networkData.relatedConnection, networkData.channelId, elapsedTicksAtSend);
                            }
                            else if (messageType == typeof(RequestMessage))
                            {
                                long requestUID;
                                bitStream.ReadLong(out requestUID);

                                Server_SyncTimeWithClient_Respond(requestUID, networkData.relatedConnection);
                            }
                            else if (messageType == typeof(ResponseMessage))
                            {
                                long requestUID;
                                bitStream.ReadLong(out requestUID);

                                Client_SyncTimeWithServer_ProcessResponse(requestUID, elapsedTicksAtSend);
                            }
                            else if (messageType == typeof(OwnerAuthorityIdAssignmentEvent)) // this should be the first message ever received....but since only sent once per client, do not put it first in the if statements list of message type check
                            {
                                ushort ownerAuthorityId;
                                bitStream.ReadUShort(out ownerAuthorityId, GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED);

                                long sessionGUIDremote;
                                bitStream.ReadLong(out sessionGUIDremote);
                                SessionGUID = sessionGUIDremote;

                                if (!IsServer) // this only applied to clients....should NEVER happen on server
                                {
                                    const string REC = " ***************************** this client received from server my assigned ownerAuthorityId: ";
                                    GONetLog.Debug(string.Concat(REC, ownerAuthorityId));

                                    MyAuthorityId = ownerAuthorityId;
                                } // else log warning?
                            }
                            else if (messageType == typeof(AutoMagicalSync_AllCurrentValues_Message))
                            {
                                DeserializeBody_AllValuesBundle(bitStream, networkData.bytesUsedCount, networkData.relatedConnection, elapsedTicksAtSend);
                            }
                            else if (messageType == typeof(ServerSaysClientInitializationCompletion))
                            {
                                if (IsClient)
                                {
                                    GONetClient.IsInitializedWithServer = true;
                                }
                            } // else?  TODO lookup proper deserialize method instead of if-else-if statement(s)
                        }
                    }
                }

                // TODO this should only deserialize the message....and then send over to an EventBus where subscribers to that event/message from the bus can process accordingly
            }
            catch (GONetOutOfOrderHorseDickoryException outOfOrderException)
            {
                GONetLog.Error(outOfOrderException.Message);
            }
            catch (Exception e)
            {
                GONetLog.Error(string.Concat("Error Message: ", e.Message, "\nError Stacktrace:\n", e.StackTrace));
            }
            finally
            {
                { // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                    ConcurrentQueue<NetworkData> readyToReturnQueue = readyToReturnQueue_ThreadMap[networkData.messageBytesBorrowedOnThread];
                    readyToReturnQueue.Enqueue(networkData);
                }
            }
        }

        private static void DeserializeBody_EventSingle(byte[] messageBytes, GONetConnection relatedConnection)
        {
            IGONetEvent @event = SerializationUtils.DeserializeFromBytes<IGONetEvent>(messageBytes);
            EventBus.Publish(@event, relatedConnection.OwnerAuthorityId);
        }

        static bool isCurrentlyProcessingInstantiateGNPEvent;
        /// <summary>
        /// only relevant while <see cref="isCurrentlyProcessingInstantiateGNPEvent"/> is true.
        /// </summary>
        static InstantiateGONetParticipantEvent currentlyProcessingInstantiateGNPEvent;

        /// <summary>
        /// Process instantiation event from remote source.
        /// </summary>
        /// <param name="instantiateEvent"></param>
        private static GONetParticipant Instantiate_Remote(InstantiateGONetParticipantEvent instantiateEvent)
        {
            isCurrentlyProcessingInstantiateGNPEvent = true;
            currentlyProcessingInstantiateGNPEvent = instantiateEvent;

            GONetParticipant template = GONetSpawnSupport_Runtime.LookupTemplateFromDesignTimeLocation(instantiateEvent.DesignTimeLocation);
            GONetParticipant instance = UnityEngine.Object.Instantiate(template, instantiateEvent.Position, instantiateEvent.Rotation);

            if (!string.IsNullOrWhiteSpace(instantiateEvent.InstanceName))
            {
                instance.gameObject.name = instantiateEvent.InstanceName;
            }

            //const string INSTANTIATE = "Instantiate_Remote, Instantiate complete....go.name: ";
            //const string ID = " event.gonetId: ";
            //GONetLog.Debug(string.Concat(INSTANTIATE, instance.gameObject.name, ID + instantiateEvent.GONetId));

            instance.OwnerAuthorityId = instantiateEvent.OwnerAuthorityId;
            if (instantiateEvent.GONetId != GONetParticipant.GONetId_Unset)
            {
                instance.GONetId = instantiateEvent.GONetId; // TODO when/if replay support is added, this might overwrite what will automatically be done in OnEnable_AssignGONetId_IfAppropriate...maybe that one should be prevented..going to comment there now too
            }
            remoteSpawns_avoidAutoPropagateSupport.Add(instance);
            instance.IsOKToStartAutoMagicalProcessing = true;

            isCurrentlyProcessingInstantiateGNPEvent = false;

            return instance;
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient connectionToClient)
        {
            Server_AssignNewClientAuthorityId(connectionToClient);
            Server_SendClientPersistentEventsSinceStart(connectionToClient);
            Server_SendClientCurrentState_AllAutoMagicalSync(connectionToClient);
            Server_SendClientIndicationOfInitializationCompletion(connectionToClient); // NOTE: sending this will cause the client to instantiate its GONetLocal
        }

        private static void Server_OnNewClientInstantiatedItsGONetLocal(GONetLocal newClientGONetLocal)
        {
            GONetRemoteClient remoteClient = gonetServer.GetRemoteClientByAuthorityId(newClientGONetLocal.GONetParticipant.OwnerAuthorityId);
            remoteClient.IsInitializedWithServer = true;
        }

        private static void Server_SendClientPersistentEventsSinceStart(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            if (persistentEventsThisSession.Count > 0)
            {
                PersistentEvents_Bundle bundle = new PersistentEvents_Bundle(Time.ElapsedTicks, persistentEventsThisSession);
                int returnBytesUsedCount;
                byte[] bytes = SerializationUtils.SerializeToBytes<IGONetEvent>(bundle, out returnBytesUsedCount); // EXTREMELY important to include the <IGONetEvent> because there are multiple options for MessagePack to serialize this thing based on BobWad_Generated.cs' usage of [MessagePack.Union] for relevant interfaces this concrete class implements and the other end's call to deserialize will be to DeserializeBody_EventSingle and <IGONetEvent> will be used there too!!!
                SendBytesToRemoteConnection(gonetConnection_ServerToClient, bytes, returnBytesUsedCount, GONetChannel.ClientInitialization_EventSingles_Reliable);
                SerializationUtils.ReturnByteArray(bytes);
            }
        }

        private static void Server_AssignNewClientAuthorityId(GONetConnection_ServerToClient connectionToClient)
        {
            // first assign locally
            connectionToClient.OwnerAuthorityId = ++server_lastAssignedAuthorityId;
            gonetServer.OnConnectionToClientAuthorityIdAssigned(connectionToClient, connectionToClient.OwnerAuthorityId); // TODO this should automatically happen via event...i.e., update the setter above to do event stuff on change!

            // then send the assignment to the client
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(OwnerAuthorityIdAssignmentEvent)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                { // body
                    bitStream.WriteUShort(connectionToClient.OwnerAuthorityId, GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED);
                    bitStream.WriteLong(SessionGUID);
                }

                bitStream.WriteCurrentPartialByte();

                SendBytesToRemoteConnection(connectionToClient, bitStream.GetBuffer(), bitStream.Length_WrittenBytes, GONetChannel.ClientInitialization_CustomSerialization_Reliable);
            }
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetId = GONetParticipant.GONetId_Unset;
        /// <summary>
        /// For every runtime instance of <see cref="GONetParticipant"/>, there will be one and only one item in one and only one of the <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/>'s <see cref="Dictionary{TKey, TValue}.Values"/>.
        /// The key into this is the <see cref="GONetParticipant.codeGenerationId"/>.
        /// </summary>
        static readonly Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> activeAutoSyncCompanionsByCodeGenerationIdMap =
            new Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>>(byte.MaxValue);

        static readonly Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupportByFrequencyMap =
            new Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        static readonly List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupports_UnityMainThread =
            new List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        internal class AutoMagicalSync_ValueMonitoringSupport_ChangedValue
        {
            internal byte index;
            internal GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;

            #region properties copied off of GONetAutoMagicalSyncAttribute
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.MustRunOnUnityMainThread"/>
            /// </summary>
            internal bool syncAttribute_MustRunOnUnityMainThread;
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
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived"/>
            /// </summary>
            internal bool syncAttribute_ShouldBlendBetweenValuesReceived;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ShouldSkipSync"/>
            /// </summary>
            internal Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool> syncAttribute_ShouldSkipSync;
            /// <summary>
            /// Matches/corresponds with/to each of the following members:
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeDownToBitCount"/>
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeLowerBound"/>
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeUpperBound"/>
            /// </summary>
            internal QuantizerSettingsGroup syncAttribute_QuantizerSettingsGroup;
            #endregion

            internal GONetSyncableValue lastKnownValue;
            internal GONetSyncableValue lastKnownValue_previous;

            /// <summary>
            /// TODO replace this with <see cref="GONetSyncableValue"/>
            /// </summary>
            [StructLayout(LayoutKind.Explicit)]
            internal struct NumericValueChangeSnapshot // TODO : IEquatable<T>
            {
                [FieldOffset(0)]
                internal long elapsedTicksAtChange;

                [FieldOffset(8)]
                internal GONetSyncableValue numericValue;

                NumericValueChangeSnapshot(long elapsedTicksAtChange, GONetSyncableValue value) : this()
                {
                    this.elapsedTicksAtChange = elapsedTicksAtChange;
                    numericValue = value;
                }

                internal static NumericValueChangeSnapshot Create(long elapsedTicksAtChange, GONetSyncableValue value)
                {
                    if (value.GONetSyncType == GONetSyncableValueTypes.System_Single ||
                        value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3 ||
                        value.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion) // TODO move this to some public static list for folks to reference! e.g. Allowed Blendy Value Types
                    {
                        return new NumericValueChangeSnapshot(elapsedTicksAtChange, value);
                    }

                    throw new ArgumentException("Type not supported.", nameof(value.GONetSyncType));
                }

                public override bool Equals(object obj)
                {
                    if (!(obj is NumericValueChangeSnapshot))
                    {
                        return false;
                    }

                    var snapshot = (NumericValueChangeSnapshot)obj;
                    return elapsedTicksAtChange == snapshot.elapsedTicksAtChange &&
                           EqualityComparer<GONetSyncableValue>.Default.Equals(numericValue, snapshot.numericValue);
                }

                public override int GetHashCode()
                {
                    var hashCode = -1529925349;
                    hashCode = hashCode * -1521134295 + elapsedTicksAtChange.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<GONetSyncableValue>.Default.GetHashCode(numericValue);
                    return hashCode;
                }
            }

            internal const int MOST_RECENT_CHANGEs_SIZE_MINIMUM = 10;
            internal const int MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED = 100;
            internal static readonly ArrayPool<NumericValueChangeSnapshot> mostRecentChangesPool = new ArrayPool<NumericValueChangeSnapshot>(1000, 50, MOST_RECENT_CHANGEs_SIZE_MINIMUM, MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED);
            internal static readonly long AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS = TimeSpan.FromSeconds(2).Ticks;

            /// <summary>
            /// This will be null when <see cref="syncAttribute_ShouldBlendBetweenValuesReceived"/> is false AND/OR if the value type is NOT numeric (although, the latter will be identified early on in either generation or runtime and cause an exception to essentially disallow that!).
            /// IMPORTANT: This is always sorted in most recent with lowest index to oldest with highest index order.
            /// </summary>
            internal NumericValueChangeSnapshot[] mostRecentChanges;
            internal int mostRecentChanges_capacitySize;
            internal int mostRecentChanges_usedSize = 0;
            private ushort mostRecentChanges_UpdatedByAuthorityId;

            /// <summary>
            /// DO NOT USE THIS.
            /// Public default constructor is required for object pool instantiation under current impl of <see cref="ObjectPool{T}"/>;
            /// </summary>
            public AutoMagicalSync_ValueMonitoringSupport_ChangedValue() { }

            /// <summary>
            /// This is called in generated code (i.e., sub-classes of <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated"/>) for any
            /// member decorated with <see cref="GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived"/> set to true.
            /// </summary>
            internal void AddToMostRecentChangeQueue_IfAppropriate(long elapsedTicksAtChange, GONetSyncableValue value)
            {
                for (int i = 0; i < mostRecentChanges_usedSize; ++i)
                {
                    var item = mostRecentChanges[i];
                    if (item.elapsedTicksAtChange < elapsedTicksAtChange)
                    {
                        // insert new guy, who is more recent than current, here at i; but first, move all the ones down a notch as they are all older than the new guy:
                        for (int j = mostRecentChanges_usedSize; j >= i; --j)
                        {
                            if (j < (mostRecentChanges_capacitySize - 1))
                            {
                                mostRecentChanges[j + 1] = mostRecentChanges[j];
                            }
                        }

                        mostRecentChanges[i] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                        if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                        {
                            ++mostRecentChanges_usedSize;
                        }
                        //LogBufferContentsIfAppropriate();
                        return;
                    }
                }

                if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                {
                    mostRecentChanges[mostRecentChanges_usedSize] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                    ++mostRecentChanges_usedSize;
                }

                //LogBufferContentsIfAppropriate();
            }

            long lastLogBufferContentsTicks;

            private void LogBufferContentsIfAppropriate()
            {
                if (mostRecentChanges_usedSize == mostRecentChanges_capacitySize && (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastLogBufferContentsTicks).TotalSeconds > 20))
                {
                    lastLogBufferContentsTicks = DateTime.UtcNow.Ticks;
                    GONetLog.Debug("==============================================================================================");
                    for (int k = 0; k < mostRecentChanges_usedSize; ++k)
                    {
                        GONetLog.Debug(string.Concat("item: ", k, " value: ", mostRecentChanges[k].numericValue, " changed @ time (seconds): ", TimeSpan.FromTicks(mostRecentChanges[k].elapsedTicksAtChange).TotalSeconds));
                    }
                }
            }

            /// <summary>
            /// <para>Expected that this is called each frame.</para>
            /// <para>IMPORTANT: This method will do nothing (i.e., not appripriate) if <see cref="syncCompanion"/>'s <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated.gonetParticipant"/> is mine (<see cref="GONetMain.IsMine(GONetParticipant)"/>) - do not value blend on something I own...value blending is only something that makes sense for GNPs that others own</para>
            /// <para>Loop through the recent changes to interpolate or extrapolate is possible.</para>
            /// <para>POST: The related/associated value is updated to what is believed to be the current value based on recent changes accumulated from owner/source.</para>
            /// </summary>
            internal void ApplyValueBlending_IfAppropriate(long useBufferLeadTicks)
            {
                if (syncCompanion.gonetParticipant.IsMine)
                {
                    return;
                }

                if (syncCompanion.gonetParticipant.IsNoLongerMine)
                {
                    useBufferLeadTicks = 0;
                }

                GONetSyncableValue blendedValue;
                if (ValueBlendUtils.TryGetBlendedValue(this, Time.ElapsedTicks - useBufferLeadTicks, out blendedValue))
                {
                    syncCompanion.SetAutoMagicalSyncValue(index, blendedValue);
                }
            }

            /// <summary>
            /// At time of writing, the only case for this is when transferring ownership of client owned thing over to server ownership and on server there will no longer be value blending as it will be the owner/source for others
            /// </summary>
            internal void ClearMostRecentChanges()
            {
                mostRecentChanges_usedSize = 0; // TODO there really may need to be some more housekeeping to do here!
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
            internal readonly bool mustRunOnUnityMainThread;

            internal SyncBundleUniqueGrouping(float scheduleFrequency, AutoMagicalSyncReliability reliability, bool mustRunOnUnityMainThread)
            {
                this.scheduleFrequency = scheduleFrequency;
                this.reliability = reliability;
                this.mustRunOnUnityMainThread = mustRunOnUnityMainThread;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SyncBundleUniqueGrouping))
                {
                    return false;
                }

                var grouping = (SyncBundleUniqueGrouping)obj;
                return scheduleFrequency == grouping.scheduleFrequency &&
                       reliability == grouping.reliability &&
                       mustRunOnUnityMainThread == grouping.mustRunOnUnityMainThread;
            }

            public override int GetHashCode()
            {
                var hashCode = -1343937139;
                hashCode = hashCode * -1521134295 + scheduleFrequency.GetHashCode();
                hashCode = hashCode * -1521134295 + reliability.GetHashCode();
                hashCode = hashCode * -1521134295 + mustRunOnUnityMainThread.GetHashCode();
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
                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
                    if (!activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions))
                    {
                        autoSyncCompanions = new Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                        activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId] = autoSyncCompanions; // NOTE: This is the only place we add to the outer dictionary and this is always run in the main unity thread, THEREFORE no need for Concurrent....just on the inner ones
                    }
                    GONetParticipant_AutoMagicalSyncCompanion_Generated companion = GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.CreateInstance(gonetParticipant);
                    autoSyncCompanions[gonetParticipant] = companion; // NOTE: This is the only place where the inner dictionary is added to and is ensured to run on unity main thread since OnEnable, so no need for concurrency as long as we can say the same about removes

                    uniqueSyncGroupings.Clear();
                    for (int i = 0; i < companion.valuesCount; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = companion.valuesChangesSupport[i];
                        SyncBundleUniqueGrouping grouping =
                            new SyncBundleUniqueGrouping(
                                monitoringSupport.syncAttribute_SyncChangesEverySeconds,
                                monitoringSupport.syncAttribute_Reliability,
                                monitoringSupport.syncAttribute_MustRunOnUnityMainThread);

                        uniqueSyncGroupings.Add(grouping); // since it is a set, duplicates will be discarded
                    }

                    if (gonetParticipant.animatorSyncSupport != null)
                    { // auto-sync stuffs, but this time for animation controller parameters
                        var animatorSyncSupportEnum = gonetParticipant.animatorSyncSupport.GetEnumerator();
                        while (animatorSyncSupportEnum.MoveNext())
                        {
                            string parameterName = animatorSyncSupportEnum.Current.Key;
                            GONetParticipant.AnimatorControllerParameter parameter = animatorSyncSupportEnum.Current.Value;

                            //GONetLog.Debug(string.Concat("animator parameter....name: ", parameterName, " type: ", parameter.valueType, " isSyncd: ", parameter.isSyncd));
                        }
                    }

                    foreach (SyncBundleUniqueGrouping uniqueSyncGrouping in uniqueSyncGroupings)
                    {
                        if (!autoSyncProcessingSupportByFrequencyMap.ContainsKey(uniqueSyncGrouping))
                        {
                            var autoSyncProcessingSupport =
                                new AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable(uniqueSyncGrouping, activeAutoSyncCompanionsByCodeGenerationIdMap); // IMPORTANT: this starts the thread!

                            autoSyncProcessingSupportByFrequencyMap[uniqueSyncGrouping] = autoSyncProcessingSupport;

                            if (uniqueSyncGrouping.mustRunOnUnityMainThread)
                            {
                                autoSyncProcessingSupports_UnityMainThread.Add(autoSyncProcessingSupport);
                            }
                        }
                    }
                }

                if (gonetParticipant.GONetId != GONetParticipant.GONetId_Unset) // FYI, the normal case is that at this point, GONetId will be 0/unset, because this is happening as a result of Instantiate being called in which case the actual GONetId assignment will not occur until just AFTER OnEnable is finished!
                {
                    gonetParticipantByGONetIdMap[gonetParticipant.GONetId] = gonetParticipant; // be doubly sure we have this (the case where it would not already is if gnp was started-disabled-enabled
                }

                uint gonetIdThatIsGoingToBePopulated = isCurrentlyProcessingInstantiateGNPEvent ? currentlyProcessingInstantiateGNPEvent.GONetId : gonetParticipant.GONetId;
                var enableEvent = new GONetParticipantEnabledEvent(gonetIdThatIsGoingToBePopulated);
                PublishEventAsSoonAsGONetIdAssigned(enableEvent, gonetParticipant);
            }
        }

        public static bool IsChannelClientInitializationRelated(GONetChannelId channelId)
        {
            return
                channelId == GONetChannel.ClientInitialization_EventSingles_Reliable ||
                channelId == GONetChannel.ClientInitialization_CustomSerialization_Reliable;
        }

        public static bool WasDefinedInScene(GONetParticipant gonetParticipant)
        {
            return definedInSceneParticipantInstanceIDs.Contains(gonetParticipant.GetInstanceID());
        }

        internal static void Start_AutoPropagateInstantiation_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying)
            {
                if (WasDefinedInScene(gonetParticipant))
                {
                    if (IsServer) // stuff defined in the scene will be owned by the server and therefore needs to be assigned a GONetId by server
                    {
                        AssignGONetIdRaw_IfAppropriate(gonetParticipant);
                    }
                }
                else
                {
                    //GONetLog.Debug("Start...NOT defined in scene...name: " + gonetParticipant.gameObject.name);

                    bool isThisCondisideredTheMomentOfInitialInstantiation = !remoteSpawns_avoidAutoPropagateSupport.Contains(gonetParticipant);
                    if (isThisCondisideredTheMomentOfInitialInstantiation)
                    {
                        gonetParticipant.OwnerAuthorityId = MyAuthorityId; // With the flow of methods and such, this looks like the first point in time we know to set this to my authority id
                        AssignGONetIdRaw_IfAppropriate(gonetParticipant);

                        AutoPropagateInitialInstantiation(gonetParticipant);
                    }
                    else
                    {
                        // this data item has now served its purpose (i.e., avoid auto propagate since it already came from remote source!), so remove it
                        remoteSpawns_avoidAutoPropagateSupport.Remove(gonetParticipant);
                    }
                }

                var startEvent = new GONetParticipantStartedEvent(gonetParticipant);
                PublishEventAsSoonAsGONetIdAssigned(startEvent, gonetParticipant);
            }
        }

        /// <summary>
        /// PRE: <paramref name="event"/> must also implement <see cref="IHaveRelatedGONetId"/>.
        /// </summary>
        private static void PublishEventAsSoonAsGONetIdAssigned(IGONetEvent @event, GONetParticipant gonetParticipant)
        {
            if (!((object)@event is IHaveRelatedGONetId))
            {
                throw new ArgumentException("Argument must an event that implements IHaveRelatedGONetId for this to make any sense and work....the way the event classes/interfaces was implemented causes this unsightly inability to just use IHaveRelatedGONetId as the param type, but do it!", nameof(@event));
            }

            if (gonetParticipant.DoesGONetIdContainAllComponents() && gonetParticipantByGONetIdMap[gonetParticipant.GONetId] == gonetParticipant)
            {
                EventBus.Publish<IGONetEvent>(@event);
            }
            else
            {
                GlobalSessionContext_Participant.StartCoroutine(PublishEventAsSoonAsGONetIdAssigned_Coroutine(@event, gonetParticipant));
            }
        }

        /// <summary>
        /// PRE: <paramref name="event"/> must also implement <see cref="IHaveRelatedGONetId"/>.
        /// This method should only ever be called on a client and as a result of having an event ready to go (e.g., <see cref="GONetParticipantStartedEvent"/> or <see cref="GONetParticipantEnabledEvent"/>)
        /// but since the associated <see cref="GONetParticipant"/> was defined in a unity scene and since the server will assign its <see cref="GONetParticipant.GONetId"/> and this client
        /// will get it momentarily after this initialization causing this event to be raised is processed...we need a mechanism to postpone the event publish until gonetid assigned so the
        /// event publish process of placing into an envelope with a reference to the actual GNP will find the GNP since the proper gonetid is known.
        /// </summary>
        private static IEnumerator PublishEventAsSoonAsGONetIdAssigned_Coroutine(IGONetEvent @event, GONetParticipant gonetParticipant)
        {
            GONetParticipant mappedGNP;
            while (!gonetParticipant.DoesGONetIdContainAllComponents() || !gonetParticipantByGONetIdMap.TryGetValue(gonetParticipant.GONetId, out mappedGNP) || mappedGNP != gonetParticipant)
            {
                yield return null;
            }

            ((IHaveRelatedGONetId)@event).GONetId = gonetParticipant.GONetId;
            EventBus.Publish<IGONetEvent>(@event);
        }

        private static void AssignGONetIdRaw_IfAppropriate(GONetParticipant gonetParticipant, bool shouldForceChangeEventIfAlreadySet = false)
        {
            if (shouldForceChangeEventIfAlreadySet || gonetParticipant.gonetId_raw == GONetParticipant.GONetId_Unset) // TODO need to avoid this when this guy is coming from replay too! gonetParticipant.WasInstantiated true is all we have now...will have WasFromReplay later
            {
                if (lastAssignedGONetId < GONetParticipant.GONetId_Raw_MaxValue)
                {
                    uint gonetId_raw = ++lastAssignedGONetId;
                    gonetParticipant.GONetId = (gonetId_raw << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED) | gonetParticipant.OwnerAuthorityId;
                }
                else
                {
                    throw new OverflowException("Unable to assign a new GONetId, because lastAssignedGONetId has reached the max value of GONetParticipant.GONetId_Raw_MaxValue, which is: " + GONetParticipant.GONetId_Raw_MaxValue);
                }
            }
        }

        private static void AutoPropagateInitialInstantiation(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event;

            string nonAuthorityDesignTimeLocation;
            if (GONetSpawnSupport_Runtime.TryGetNonAuthorityDesignTimeLocation(gonetParticipant, out nonAuthorityDesignTimeLocation))
            {
                @event = InstantiateGONetParticipantEvent.Create_WithNonAuthorityInfo(gonetParticipant, nonAuthorityDesignTimeLocation);
            }
            else
            {
                @event = InstantiateGONetParticipantEvent.Create(gonetParticipant);
            }

            //GONetLog.Debug("Publish InstantiateGONetParticipantEvent now."); /////////////////////////// DREETS!
            EventBus.Publish(@event); // this causes the auto propagation via local handler to send to all remotes (i.e., all clients if server, server if client)

            gonetParticipant.IsOKToStartAutoMagicalProcessing = true; // VERY IMPORTANT that this comes AFTER publishing the event so the flood gates to start syncing data come AFTER other parties are made aware of the GNP in the above event!
        }

        internal static void OnDestroy_AutoPropagateRemoval_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying)
            {
                if (IsMine(gonetParticipant))
                {
                    AutoPropagateInitialDestroy(gonetParticipant);
                }
                else if (!gonetIdsDestroyedViaPropagation.Contains(gonetParticipant.GONetId))
                {
                    const string NOD = "GONetParticipant being destroyed and IsMine is false, which means the only other GONet-approved reason this should be destroyed is through automatic propagation over the network as a response to the owner destroying it; HOWEVER, that is not the case right now and the ASSumption is that you inadvertantly called UnityEngine.Object.Destroy() on something not owned by you.  GONetId: ";
                    GONetLog.Warning(string.Concat(NOD, gonetParticipant.GONetId));
                }
            }
        }

        /// <summary>
        /// PRE: <paramref name="gonetParticipant"/> is owned by me.
        /// </summary>
        private static void AutoPropagateInitialDestroy(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant.GONetId == GONetParticipant.GONetId_Unset) // almost impossible for this to be true, but check anyway
            {
                const string NOID = "GONetParticipant that I own was destroyed by me, but it has not been assigned a GONetId yet, which is highly problematic.  Unable to propagate the destroy to others for that reason.  GameObject.name: ";
                GONetLog.Error(string.Concat(NOID, gonetParticipant.gameObject.name));
                return;
            }

            DestroyGONetParticipantEvent @event = new DestroyGONetParticipantEvent() { GONetId = gonetParticipant.GONetId };
            //GONetLog.Debug("Publish DestroyGONetParticipantEvent now."); /////////////////////////// DREETS!
            EventBus.Publish(@event); // this causes the auto propagation via local handler to send to all remotes (i.e., all clients if server, server if client)
        }

        static readonly HashSet<int> definedInSceneParticipantInstanceIDs = new HashSet<int>();

        internal static void RecordParticipantsAsDefinedInScene(List<GONetParticipant> gonetParticipantsInLevel)
        {
            gonetParticipantsInLevel.ForEach(gonetParticipant => definedInSceneParticipantInstanceIDs.Add(gonetParticipant.GetInstanceID()));
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
        /// PRE: <paramref name="connectionToClient"/> already has been assigned a good value to <see cref="GONetConnection.OwnerAuthorityId"/>.
        /// </summary>
        static void Server_SendClientCurrentState_AllAutoMagicalSync(GONetConnection connectionToClient)
        {
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_AllCurrentValues_Message)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                SerializeBody_AllCurrentValuesBundle(bitStream); // body

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] allValuesSerialized = mainThread_valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, allValuesSerialized, 0, bytesUsedCount);

                SendBytesToRemoteConnection(connectionToClient, allValuesSerialized, bytesUsedCount, GONetChannel.ClientInitialization_CustomSerialization_Reliable); // NOT using GONetChannel.AutoMagicalSync_Reliable because that one is reserved for things as they are happening and not this one time blast to a new client for all things
                mainThread_valueChangeSerializationArrayPool.Return(allValuesSerialized);
            }
        }

        static void Server_SendClientIndicationOfInitializationCompletion(GONetConnection_ServerToClient connectionToClient)
        {
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(ServerSaysClientInitializationCompletion)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = mainThread_valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                SendBytesToRemoteConnection(connectionToClient, bytes, bytesUsedCount, GONetChannel.ClientInitialization_CustomSerialization_Reliable);
                mainThread_valueChangeSerializationArrayPool.Return(bytes);
            }
        }

        /// <summary>
        /// For every unique value encountered for <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>, an instance of this 
        /// class will be created and used to process only those fields/properties set to be sync'd on that frequency.
        /// </summary>
        internal sealed class AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable : IDisposable
        {
            bool isSetupToRunInSeparateThread;
            /// <summary>
            /// Only non-null when <see cref="isSetupToRunInSeparateThread"/> is true
            /// </summary>
            Thread thread;
            /// <summary>
            /// Can only be true when <see cref="isSetupToRunInSeparateThread"/> is true
            /// </summary>
            volatile bool isThreadRunning;

            volatile bool shouldProcessInSeparateThreadASAP = false;
            long lastProcessCompleteTicks;

            static readonly long END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS = TimeSpan.FromSeconds(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS).Ticks;

            SyncBundleUniqueGrouping uniqueGrouping;
            long scheduleFrequencyTicks;
            Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency;
            QosType uniqueGrouping_qualityOfService;
            GONetChannelId uniqueGrouping_channelId;

            /// <summary>
            /// Indicates whether or not <see cref="ProcessASAP"/> must be called (manually) from an outside part in order for sync processing to occur.
            /// </summary>
            internal bool DoesRequireManualProcessInitiation => scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS || !isSetupToRunInSeparateThread;

            /// <summary>
            /// Just a helper data structure just for use in <see cref="ProcessAutoMagicalSyncStuffs(bool, ReliableEndpoint)"/>
            /// </summary>
            readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);

            readonly ArrayPool<byte> myThread_valueChangeSerializationArrayPool;

            static readonly SecretaryOfTemporalAffairs myThread_Time = new SecretaryOfTemporalAffairs();

            /// <summary>
            /// IMPORTANT: If a value of <see cref="AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS"/> is passed in here for <paramref name="scheduleFrequency"/>,
            ///            then nothing will happen in here automatically....<see cref="GONetMain"/> or some other party will have to manually call <see cref="ProcessASAP"/>.
            /// </summary>
            internal AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable(SyncBundleUniqueGrouping uniqueGrouping, Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency)
            {
                autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap[this] = myThread_valueChangeSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);

                this.uniqueGrouping = uniqueGrouping;
                scheduleFrequencyTicks = TimeSpan.FromSeconds(uniqueGrouping.scheduleFrequency).Ticks;
                uniqueGrouping_qualityOfService = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? QosType.Reliable : QosType.Unreliable;
                uniqueGrouping_channelId = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? GONetChannel.AutoMagicalSync_Reliable : GONetChannel.AutoMagicalSync_Unreliable;

                this.everythingMap_evenStuffNotOnThisScheduleFrequency = everythingMap_evenStuffNotOnThisScheduleFrequency;

                Time.TimeSetFromAuthority += Time_TimeSetFromAuthority;

                isSetupToRunInSeparateThread = !uniqueGrouping.mustRunOnUnityMainThread;
                if (isSetupToRunInSeparateThread)
                {
                    thread = new Thread(ContinuallyProcess_NotMainThread);

                    syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap[thread] = new Queue<SyncEvent_ValueChangeProcessed>(100); // we're on main thread, safe to deal with regular dict here
                    syncValueChanges_SendToOthersQueue_ByThreadMap[thread] = new ConcurrentQueue<SyncEvent_ValueChangeProcessed>(); // we're on main thread, safe to deal with regular dict here

                    isThreadRunning = true;
                    thread.Start();
                }
                else
                {
                    if (!syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap.ContainsKey(Thread.CurrentThread))
                    {
                        syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread] = new Queue<SyncEvent_ValueChangeProcessed>(100); // we're on main thread, safe to deal with regular dict here
                        syncValueChanges_SendToOthersQueue_ByThreadMap[Thread.CurrentThread] = new ConcurrentQueue<SyncEvent_ValueChangeProcessed>(); // we're on main thread, safe to deal with regular dict here
                    }
                }
            }

            private void Time_TimeSetFromAuthority(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks)
            {
                myThread_Time.SetFromAuthority(toElapsedTicks);
            }

            ~AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable()
            {
                Dispose();
            }

            private void ContinuallyProcess_NotMainThread()
            {
                bool doesRequireManualProcessInitiation = DoesRequireManualProcessInitiation;
                while (isThreadRunning)
                {
                    if (IsNotSafeToProcess() || (doesRequireManualProcessInitiation && !shouldProcessInSeparateThreadASAP))
                    {
                        Thread.Sleep(1); // TODO come up with appropriate sleep time/value 
                    }
                    else
                    {
                        Process();
                        shouldProcessInSeparateThreadASAP = false; // reset this

                        if (!doesRequireManualProcessInitiation)
                        { // (auto sync) frequency control:
                            long nextProcessStartTicks = lastProcessCompleteTicks + scheduleFrequencyTicks;
                            long nowTicks = DateTime.UtcNow.Ticks;// NOTE: avoiding using high resolution as follows because that class is not thread-safe (yet): HighResolutionTimeUtils.Now.Ticks;
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

            private bool IsNotSafeToProcess()
            {
                return MyAuthorityId == OwnerAuthorityId_Unset ||
                    !IsClientVsServerStatusKnown ||
                    (IsClient && !GONetClient.IsConnectedToServer);
            }

            /// <summary>
            /// Caller is responsible for knowing the value of and dealing with <see cref="IsNotSafeToProcess"/>.
            /// </summary>
            private void Process()
            {
                myThread_Time.Update();
                long myTicks = myThread_Time.ElapsedTicks;
                // loop over everythingMap_evenStuffNotOnThisScheduleFrequency only processing the items inside that match scheduleFrequency
                syncValuesToSend.Clear();

                var enumeratorOuter = everythingMap_evenStuffNotOnThisScheduleFrequency.GetEnumerator();
                while (enumeratorOuter.MoveNext())
                {
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                    if (currentMap == null)
                    {
                        GONetLog.Error("currentMap == null");
                    }
                    var enumeratorInner = currentMap.GetEnumerator();
                    while (enumeratorInner.MoveNext())
                    {
                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;
                        if (monitoringSupport == null)
                        {
                            GONetLog.Error("monitoringSupport == null");
                        }

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
                        if (gonetServer != null)
                        {
                            for (int iConnection = 0; iConnection < gonetServer.numConnections; ++iConnection)
                            {
                                GONetConnection_ServerToClient gONetConnection_ServerToClient = gonetServer.remoteClients[iConnection].ConnectionToClient;
                                byte[] changesSerialized_clientSpecific = SerializeWhole_ChangesBundle(syncValuesToSend, myThread_valueChangeSerializationArrayPool, out bytesUsedCount, gONetConnection_ServerToClient.OwnerAuthorityId, myTicks);
                                if (changesSerialized_clientSpecific != EMPTY_CHANGES_BUNDLE && bytesUsedCount > 0)
                                {
                                    //GONetLog.Debug("AutoMagicalSync_ValueChanges_Message sending right after this. bytesUsedCount: " + bytesUsedCount);  /////////////////////////// DREETS!
                                    SendBytesToRemoteConnection(gONetConnection_ServerToClient, changesSerialized_clientSpecific, bytesUsedCount, uniqueGrouping_channelId);
                                    myThread_valueChangeSerializationArrayPool.Return(changesSerialized_clientSpecific);
                                }
                            }
                        }
                    }
                    else
                    {
                        byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, myThread_valueChangeSerializationArrayPool, out bytesUsedCount, MyAuthorityId, myTicks);
                        if (changesSerialized != EMPTY_CHANGES_BUNDLE && bytesUsedCount > 0)
                        {
                            SendBytesToRemoteConnections(changesSerialized, bytesUsedCount, uniqueGrouping_channelId);
                            myThread_valueChangeSerializationArrayPool.Return(changesSerialized);
                        }
                    }

                    PublishEvents_SyncValueChangesSentToOthers_ASAP();
                }
            }
            /// <summary>
            /// Promote Local Thread Events To Main Thread For Publishing since calling <see cref="GONetEventBus.Publish{T}(T, uint?)"/> is not to be called from multiple threads!
            /// </summary>
            private void PublishEvents_SyncValueChangesSentToOthers_ASAP()
            {
                Queue<SyncEvent_ValueChangeProcessed> queueAwaiting = syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread];
                ConcurrentQueue<SyncEvent_ValueChangeProcessed> queueSend = syncValueChanges_SendToOthersQueue_ByThreadMap[Thread.CurrentThread];
                while (queueAwaiting.Count > 0)
                {
                    var @event = queueAwaiting.Dequeue();
                    queueSend.Enqueue(@event);
                }
            }

            /// <summary>
            /// IMPORTANT: When <see cref="isSetupToRunInSeparateThread"/> is false, calling this will NOT yield a call to <see cref="Process"/> and caller must keep calling this method each frame until proper schedule cycle permits the call to <see cref="Process"/> to go through.
            /// </summary>
            internal void ProcessASAP()
            {
                if (isSetupToRunInSeparateThread)
                {
                    shouldProcessInSeparateThreadASAP = true;
                }
                else if (!IsNotSafeToProcess())
                {
                    if (scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS)
                    {
                        Process();
                    }
                    else
                    {
                        long nowTicks = DateTime.UtcNow.Ticks;// NOTE: avoiding using high resolution as follows because that class is not thread-safe (yet): HighResolutionTimeUtils.Now.Ticks;
                        bool isASAPNow = (nowTicks - lastProcessCompleteTicks) > scheduleFrequencyTicks;
                        if (isASAPNow)
                        {
                            Process();
                            lastProcessCompleteTicks = nowTicks;
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (isSetupToRunInSeparateThread)
                {
                    isThreadRunning = false;
                    thread.Abort();
                }
            }
        }

        /// <summary>
        /// this is used as changes are taking place over time....unlike <see cref="mainThread_valueChangeSerializationArrayPool"/>
        /// </summary>
        static readonly ConcurrentDictionary<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable, ArrayPool<byte>> autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap =
            new ConcurrentDictionary<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable, ArrayPool<byte>>();
        /// <summary>
        /// This is used when sending currente state to newly connecting clients unlike <see cref="autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap"/>
        /// </summary>
        static readonly ArrayPool<byte> mainThread_valueChangeSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);

        static readonly ArrayPool<byte> mainThread_miscSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);


        static readonly Dictionary<Type, uint> messageTypeToMessageIDMap = new Dictionary<Type, uint>(4096);
        static readonly Dictionary<uint, Type> messageTypeByMessageIDMap = new Dictionary<uint, Type>(4096);
        static uint nextMessageID;

        private static readonly byte[] EMPTY_CHANGES_BUNDLE = null;

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
        /// PRE: <paramref name="filterUsingOwnerAuthorityId"/> is not <see cref="OwnerAuthorityId_Unset"/> otherwise an exception is thrown
        /// POST: return a serialized packet with only the stuff that excludes <paramref name="filterUsingOwnerAuthorityId"/> as to not send to them (i.e., likely because they are the one who owns this data in the first place and already know this change occurred!)
        /// IMPORTANT: The caller is responsible for returning the returned byte[] to <paramref name="byteArrayPool"/>
        /// </summary>
        private static byte[] SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, ArrayPool<byte> byteArrayPool, out int bytesUsedCount, ushort filterUsingOwnerAuthorityId, long elapsedTicksAtCapture)
        {
            if (filterUsingOwnerAuthorityId == OwnerAuthorityId_Unset)
            {
                throw new ArgumentOutOfRangeException(nameof(filterUsingOwnerAuthorityId));
            }

            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChanges_Message)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(elapsedTicksAtCapture);
                }

                int changesInBundleCount = SerializeBody_ChangesBundle(changes, bitStream, filterUsingOwnerAuthorityId); // body
                if (changesInBundleCount > 0)
                {
                    bitStream.WriteCurrentPartialByte();

                    bytesUsedCount = bitStream.Length_WrittenBytes;
                    byte[] bytes = byteArrayPool.Borrow(bytesUsedCount);
                    Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                    return bytes;
                }
                else
                {
                    bytesUsedCount = 0;
                    return EMPTY_CHANGES_BUNDLE;
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

                int priorityComparison = yPriority.CompareTo(xPriority); // descending...highest priority first!

                if (priorityComparison == 0)
                { // if the priority is the same, then we want to put the most recent (i.e., highest value) changes in authority last as to not possibly cause issue during deserialize of an entire bundle because the owner authority change has not been processed yet!
                    return x.syncCompanion.gonetParticipant.OwnerAuthorityId_LastChangedElapsedSeconds
                        .CompareTo(y.syncCompanion.gonetParticipant.OwnerAuthorityId_LastChangedElapsedSeconds);
                }

                return priorityComparison;
            }
        }

        private static void SerializeBody_AllCurrentValuesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten)
        {
            var enumeratorOuter = activeAutoSyncCompanionsByCodeGenerationIdMap.GetEnumerator();
            while (enumeratorOuter.MoveNext())
            {
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                var enumeratorInner = currentMap.GetEnumerator();
                while (enumeratorInner.MoveNext())
                {
                    var current = enumeratorInner.Current;

                    GONetParticipant gonetParticipant = current.Key;
                    GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Serialize(bitStream_headerAlreadyWritten, gonetParticipant, gonetParticipant.GONetId);

                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = current.Value;
                    monitoringSupport.SerializeAll(bitStream_headerAlreadyWritten);
                }
            }
        }

        /// <summary>
        /// Returns the number of changes actually included in/added to the <paramref name="bitStream_headerAlreadyWritten"/> AFTER any filtering this method does (e.g., checking <paramref name="filterUsingOwnerAuthorityId"/>).
        /// </summary>
        private static int SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten, ushort filterUsingOwnerAuthorityId)
        {
            int countTotal = changes.Count;
            int countMinus1 = countTotal - 1;

            int countFiltered = 0;
            for (int iSort = 0; iSort < countMinus1; ++iSort) // manual sort to avoid GC
            {
                var changeA = changes[iSort];
                var changeB = changes[iSort + 1];
                if (AutoMagicalSyncChangePriorityComparer.Instance.Compare(changeA, changeB) > 0)
                {
                    changes[iSort + 1] = changeA;
                    changes[iSort] = changeB;
                }

                if (ShouldSendChange(changes[iSort], filterUsingOwnerAuthorityId)) // use this manual check to avoid Linq.Count(....) GC/perf hit
                {
                    ++countFiltered;
                }
            }
            if (ShouldSendChange(changes[countMinus1], filterUsingOwnerAuthorityId)) // use this manual check to avoid Linq.Count(....) GC/perf hit
            {
                ++countFiltered;
            }

            if (countFiltered == 0)
            {
                return 0; // <<<<<<<<<<<============================================================================  bail out early if there is nothing to add to bundle!!!!
            }

            bitStream_headerAlreadyWritten.WriteUShort((ushort)countFiltered);
            //GONetLog.AppendLine(string.Concat("about to send changes bundle...countFiltered: " + countFiltered));

            Queue<SyncEvent_ValueChangeProcessed> syncEventQueue = syncValueChanges_Serialized_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread];
            for (int i = 0; i < countTotal; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport_ChangedValue change = changes[i];
                if (!ShouldSendChange(change, filterUsingOwnerAuthorityId))
                {
                    continue; // skip this guy (i.e., apply the "filter")
                }

                syncEventQueue.Enqueue(GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateInstance(SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers, Time.ElapsedTicks, filterUsingOwnerAuthorityId, change.syncCompanion, change.index));

                if (change.syncCompanion.gonetParticipant.gonetId_raw == GONetParticipant.GONetId_Unset)
                {
                    const string SNAFU = "Snafoo....gonetid 0.....why are we about to send change? ...makes no sense! ShouldSendChange(change, filterUsingOwnerAuthorityId): ";
                    const string FUOA = " filterUsingOwnerAuthorityId: ";
                    GONetLog.Error(string.Concat(SNAFU, ShouldSendChange(change, filterUsingOwnerAuthorityId), FUOA, filterUsingOwnerAuthorityId));
                }

                if (change.syncCompanion.gonetParticipant.GONetIdAtInstantiation == GONetParticipant.GONetId_Unset)
                {
                    const string SNAFU = "Snafoo....gonetIdAtInstantiation 0.....how is this possible? gnp.gonetId: ";
                    GONetLog.Error(string.Concat(SNAFU, change.syncCompanion.gonetParticipant.GONetId));
                }

                //GONetLog.Append(change.syncCompanion.gonetParticipant.GONetIdAtInstantiation + ", ");
                bitStream_headerAlreadyWritten.WriteUInt(change.syncCompanion.gonetParticipant.GONetIdAtInstantiation); // have to write the gonetid first before each changed value
                bitStream_headerAlreadyWritten.WriteByte(change.index); // then have to write the index, otherwise other end does not know which index to deserialize
                change.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, change.index);
            }
            //GONetLog.Append_FlushDebug();

            return countFiltered;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldSendChange(AutoMagicalSync_ValueMonitoringSupport_ChangedValue change, ushort filterUsingOwnerAuthorityId)
        {
            return
                change.syncCompanion.gonetParticipant.GONetId != GONetParticipant.GONetId_Unset &&
                (IsServer
                    ? (gonetServer.GetRemoteClientByAuthorityId(filterUsingOwnerAuthorityId).IsInitializedWithServer && // only send to a client if that client is considered initialized with the server
                        (change.syncCompanion.gonetParticipant.OwnerAuthorityId != filterUsingOwnerAuthorityId // In most circumstances, the server should send every change except for changes back to the owner itself
                            // TODO try to make this work as an option: || IsThisChangeTheMomentOfInception(change)
                            || change.index == GONetParticipant.ASSumed_GONetId_INDEX)) // this is the one exception, if the server is assigning the instantiator/owner its GONetId for the first time, it DOES need to get sent back to itself
                    : change.syncCompanion.gonetParticipant.OwnerAuthorityId == filterUsingOwnerAuthorityId); // clients should only send out changes it owns
        }

        /* the initial idea behind this was good for a test, but the more I thought about it, the impl details did not actually make sense (perf and functionality)....keeping for now as reference
        private static bool IsThisChangeTheMomentOfInception(AutoMagicalSync_ValueMonitoringSupport_ChangedValue change)
        {
            bool shouldConsiderOlderItems = true;
            var enumerator = persistentEventsThisSession.GetEnumerator();

            Type syncEventBaseType = typeof(SyncEvent_ValueChangeProcessed);
            long tooOldTicks = TimeSpan.FromSeconds(0.5f).Ticks;

            while (shouldConsiderOlderItems && enumerator.MoveNext())
            {
                var lastConsideredEvent = enumerator.Current;
                if (TypeUtils.IsTypeAInstanceOfTypeB(lastConsideredEvent.GetType(), syncEventBaseType))
                {
                    SyncEvent_ValueChangeProcessed syncEvent = (SyncEvent_ValueChangeProcessed)lastConsideredEvent;
                    dynamic syncEventDynamic = syncEvent;
                    if (syncEvent.GONetId == change.syncCompanion.gonetParticipant.GONetId &&
                        syncEvent.CodeGenerationId == change.syncCompanion.CodeGenerationId &&
                        syncEvent.SyncMemberIndex == change.index &&
                        syncEventDynamic.valueNew == change.lastKnownValue)
                    {
                        return true;
                    }
                }

                shouldConsiderOlderItems = Time.ElapsedTicks - lastConsideredEvent.OccurredAtElapsedTicks < tooOldTicks;
            }

            return false;
        }
        */

        private static void DeserializeBody_AllValuesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, int bytesUsedCount, GONetConnection sourceOfChangeConnection, long elapsedTicksAtSend)
        {
            while (bitStream_headerAlreadyRead.Position_Bytes < bytesUsedCount) // while more data to read/process
            {
                uint gonetId = GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead).System_UInt32;

                GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId][gonetParticipant];

                syncCompanion.DeserializeInitAll(bitStream_headerAlreadyRead, elapsedTicksAtSend);
            }
        }

        /// <summary>
        /// Awaiting to not be unity null and to have an entry in the corresponding entry/map in <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/> for its codeGenerationId.
        /// </summary>
        static readonly List<GONetParticipant> gnpsAwaitingCompanion = new List<GONetParticipant>(1000);

        private static void DeserializeBody_ChangesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, GONetConnection sourceOfChangeConnection, GONetChannelId channelId, long elapsedTicksAtSend)
        {
            ushort count;
            bitStream_headerAlreadyRead.ReadUShort(out count);
            //GONetLog.AppendLine(string.Concat("about to read changes bundle...count: " + count));
            for (int i = 0; i < count; ++i)
            {
                uint gonetIdAtInstantiation;
                bitStream_headerAlreadyRead.ReadUInt(out gonetIdAtInstantiation);
                //GONetLog.Append(gonetIdAtInstantiation + ", ");

                uint gonetId = GetCurrentGONetIdByIdAtInstantiation(gonetIdAtInstantiation);

                if (!gonetParticipantByGONetIdMap.ContainsKey(gonetId))
                {
                    //GONetLog.Append_FlushDebug();

                    QosType channelQuality = GONetChannel.ById(channelId).QualityOfService;
                    if (channelQuality == QosType.Reliable)
                    {
                        const string GLAD = "Reliable changes bundle being process and GONetParticipant NOT FOUND by GONetId: ";
                        const string INST = " gonetId@instantiation(as found in serialized body): ";
                        const string COUNT = "  This will cause us not to be able to process this and the rest of the bundle, which means we will not process count: ";
                        throw new GONetOutOfOrderHorseDickoryException(string.Concat(GLAD, gonetId, INST, gonetIdAtInstantiation, COUNT, (count - i)));
                    }
                    else
                    {
                        const string NTS = "Received some unreliable GONetAutoMagicalSync data prior to some necessary prerequisite reliable data and we are unable to process this message.  Since it was sent unreliably, just pretend it did not arrive at all.  If this message streams in the log, perhaps you should be worried; however, it may appear from time to time around initialization and spawning under what is considered \"normal circumstances.\"  gonetId(from message, which is expected to be at instantiation): ";
                        const string LOCAL = " gonetId (from lookup, supposed to be current): ";
                        GONetLog.Warning(string.Concat(NTS, gonetIdAtInstantiation, LOCAL, gonetId));
                        return;
                    }
                }

                GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];

                if (gonetParticipant == null)
                {
                    GONetLog.Error("dude's Unity null...the rest will fail.  reference null too? " + ((object)gonetParticipant == null) + " gonetId: " + ((object)gonetParticipant == null ? GONetParticipant.GONetId_Unset : gonetParticipant.GONetId));
                    gnpsAwaitingCompanion.Add(gonetParticipant);
                }

                try
                {
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = companionMap[gonetParticipant];

                    byte index = (byte)bitStream_headerAlreadyRead.ReadByte();
                    syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend);

                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[index];

                    syncValueChanges_ReceivedFromOtherQueue.Enqueue(GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateInstance(SyncEvent_ValueChangeProcessedExplanation.InboundFromOther, elapsedTicksAtSend, sourceOfChangeConnection.OwnerAuthorityId, changedValue.syncCompanion, changedValue.index));
                }
                catch (Exception e)
                {
                    GONetLog.Error("BOOM! bitStream_headerAlreadyRead    position_bytes: " + bitStream_headerAlreadyRead.Position_Bytes + " Length_WrittenBytes: " + bitStream_headerAlreadyRead.Length_WrittenBytes);

                    throw e;
                }
            }
            //GONetLog.Append_FlushDebug("\n************done reading changes bundle");
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
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;
                    if (!autoSyncCompanions.TryGetValue(gonetParticipant, out syncCompanion) || !autoSyncCompanions.Remove(gonetParticipant)) // NOTE: This is the only place where the inner dictionary is removed from and is ensured to run on unity main thread since OnDisable, so no need for concurrency as long as we can say the same about adds
                    {
                        const string PORK = "Expecting to find active auto-sync companion in order to de-active/remove it upon gonetParticipant.OnDisable, but did not. gonetParticipant.GONetId: ";
                        const string NAME = " gonetParticipant.gameObject.name: ";
                        GONetLog.Warning(string.Concat(PORK, gonetParticipant.GONetId, NAME, gonetParticipant.gameObject.name));
                    }
                    if (syncCompanion != null)
                    {
                        syncCompanion.Dispose();
                    }
                }

                var disabledEvent = new GONetParticipantDisabledEvent(gonetParticipant);
                EventBus.Publish<IGONetEvent>(disabledEvent); // make sure this comes before gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetId); or else the GNP will not be found to attach to the envelope and the subscription handlers will not have what they are expecing

                gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetId);
            }
        }

        #endregion
    }

    [Serializable]
    internal class GONetOutOfOrderHorseDickoryException : Exception
    {
        public GONetOutOfOrderHorseDickoryException()
        {
        }

        public GONetOutOfOrderHorseDickoryException(string message) : base(message)
        {
        }

        public GONetOutOfOrderHorseDickoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GONetOutOfOrderHorseDickoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class GONetChannel
    {
        private static readonly Dictionary<GONetChannelId, GONetChannel> byIdMap = new Dictionary<GONetChannelId, GONetChannel>(byte.MaxValue);
        private static GONetChannelId nextAvailableId = 0;

        public static readonly GONetChannel TimeSync_Unreliable;
        public static readonly GONetChannel AutoMagicalSync_Reliable;
        public static readonly GONetChannel AutoMagicalSync_Unreliable;
        public static readonly GONetChannel CustomSerialization_Reliable;
        public static readonly GONetChannel CustomSerialization_Unreliable;
        public static readonly GONetChannel EventSingles_Reliable;
        /// <summary>
        /// <para>Using this probably only makes sense when the event implements <see cref="ITransientEvent"/>.</para>
        /// <para>If it implements <see cref="IPersistentEvent"/>, then it likely makes more sense to use <see cref="EventSingles_Reliable"/> instead.</para>
        /// </summary>
        public static readonly GONetChannel EventSingles_Unreliable;

        /// <summary>
        /// GONet internal use only!
        /// </summary>
        public static readonly GONetChannel ClientInitialization_EventSingles_Reliable;
        /// <summary>
        /// GONet internal use only!
        /// </summary>
        public static readonly GONetChannel ClientInitialization_CustomSerialization_Reliable;

        public GONetChannelId Id { get; private set; }

        public QosType QualityOfService { get; private set; }

        static GONetChannel()
        {
            TimeSync_Unreliable = new GONetChannel(QosType.Unreliable);
            AutoMagicalSync_Reliable = new GONetChannel(QosType.Reliable);
            AutoMagicalSync_Unreliable = new GONetChannel(QosType.Unreliable);
            CustomSerialization_Reliable = new GONetChannel(QosType.Reliable);
            CustomSerialization_Unreliable = new GONetChannel(QosType.Unreliable);
            EventSingles_Reliable = new GONetChannel(QosType.Reliable);
            EventSingles_Unreliable = new GONetChannel(QosType.Unreliable);

            ClientInitialization_EventSingles_Reliable = new GONetChannel(QosType.Reliable);
            ClientInitialization_CustomSerialization_Reliable = new GONetChannel(QosType.Reliable);
        }

        internal GONetChannel(QosType qualityOfService)
        {
            Id = nextAvailableId++;
            QualityOfService = qualityOfService;

            byIdMap[Id] = this;
        }

        public static GONetChannel ById(GONetChannelId id)
        {
            return byIdMap[id];
        }

        public static implicit operator GONetChannelId(GONetChannel channel)
        {
            return channel.Id;
        }
    }
}
