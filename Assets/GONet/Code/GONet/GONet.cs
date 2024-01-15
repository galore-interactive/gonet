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

using GONet.Generation;
using GONet.Utils;
using ReliableNetcode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

using GONetCodeGenerationId = System.Byte;
using GONetChannelId = System.Byte;
using System.IO;
using System.Runtime.Serialization;
using System.Net;
using System.Collections;
using System.Diagnostics;
using GONet.PluginAPI;
using System.Text;

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

        /// <summary>
        /// When a <see cref="GONetParticipant"/> could not be looked up with <paramref name="currentGONetId"/>, then we will try another way here with all info passed in.
        /// </summary>
        internal static GONetParticipant DeriveGNPFromCurrentAndPreviousValues(uint currentGONetId, ushort previousOwnerAuthorityId, ushort currentOwnerAuthorityId)
        {
            uint presumedGONetIdThatWillBeFound = (currentGONetId ^ currentOwnerAuthorityId) | previousOwnerAuthorityId;
            if (gonetParticipantByGONetIdMap.ContainsKey(presumedGONetIdThatWillBeFound))
            {
                return gonetParticipantByGONetIdMap[presumedGONetIdThatWillBeFound];
            }
            else if (gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(presumedGONetIdThatWillBeFound))
            {
                return gonetParticipantByGONetIdAtInstantiationMap[presumedGONetIdThatWillBeFound];
            }
            return null;
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

            //IsUnityApplicationEditor = Application.isEditor;
            mainUnityThread = Thread.CurrentThread;
            Application.quitting += Application_quitting_TakeNote;

            Global = gONetGlobal;
            GlobalSessionContext = gONetSessionContext;
            SetValueBlendingBufferLeadTimeFromMilliseconds(valueBlendingBufferLeadTimeMilliseconds);
            InitEventSubscriptions();
            InitPersistence();
            InitQuantizers();

            ticksAtLastInit_UtcNow = DateTime.UtcNow.Ticks;
        }

        private static void Application_quitting_TakeNote()
        {
            IsApplicationQuitting = true;
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
        const string DATABASE_PATH_RELATIVE = "database";
        private static void InitPersistence()
        {
            persistenceFilePath = Path.Combine(Application.persistentDataPath, DATABASE_PATH_RELATIVE, string.Concat(Math.Abs(Application.productName.GetHashCode()), TRIPU, DateTime.Now.ToString(DATE_FORMAT), TRIPU, SGUID, TRIPU, MOAId, DB_EXT));
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, DATABASE_PATH_RELATIVE));
            if (File.Exists(persistenceFilePath))
            {
                persistenceFilePath = persistenceFilePath.Replace(DB_EXT, string.Concat(GUID.Generate().AsInt64(), DB_EXT)); // Appending a guid to ensure the file is unique....this should only be a problem when running multiple instances on a single machine during development/testing
            }
            persistenceFileStream = new FileStream(persistenceFilePath, FileMode.Append);

            IEnumerable<Type> syncEventTypes = GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.GetAllUniqueSyncEventTypes();
            foreach (Type syncEventType in syncEventTypes)
            {
                syncEventsToSaveQueueByEventType[syncEventType] = new SyncEventsSaveSupport();
            }
        }

        public static GONetParticipant MySessionContext_Participant { get; private set; } // TODO FIXME need to spawn this for everyone and set it here!
        public static ushort MyAuthorityId { get; private set; }


        private static readonly bool isServer_asIndicatedByCommandLineArgs = Environment.GetCommandLineArgs().Contains("-server");
        private static readonly bool isClient_asIndicatedByCommandLineArgs = Environment.GetCommandLineArgs().Contains("-client");

        internal static bool isServerOverride = isServer_asIndicatedByCommandLineArgs;

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
        public static bool IsClient => _gonetClient == null ? isClient_asIndicatedByCommandLineArgs : _gonetClient.ClientTypeFlags != ClientTypeFlags.None;

        /// <summary>
        /// Since the value of <see cref="GONetParticipant.GONetId"/> can change (i.e., <see cref="Server_AssumeAuthorityOver(GONetParticipant)"/> called),
        /// this is the mechanism to find the original value at time of initial instantiation.  Not sure how this helps others, but internally to GONet it is useful.
        /// </summary>
        public static uint GetGONetIdAtInstantiation(uint currentGONetId)
        {
            GONetParticipant gonetParticipant;
            uint gonetIdAtInstantiation;
            if (gonetParticipantByGONetIdMap.TryGetValue(currentGONetId, out gonetParticipant))
            {
                return gonetParticipant.GONetIdAtInstantiation;
            }
            else if (recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map.TryGetValue(currentGONetId, out gonetIdAtInstantiation))
            {
                return gonetIdAtInstantiation;
            }
            else
            {
                return GONetParticipant.GONetId_Unset;
            }
        }

        public static uint GetCurrentGONetIdByIdAtInstantiation(uint gonetIdAtInstantiation)
        {
            GONetParticipant gonetParticipant = null;
            if (gonetParticipantByGONetIdAtInstantiationMap.TryGetValue(gonetIdAtInstantiation, out gonetParticipant))
            {
                return gonetParticipant.GONetId;
            }
            return GONetParticipant.GONetId_Unset;
        }

        /// <summary>
        /// IMPORTANT: Prior to things being initialized with network connection(s), we may not know if we are a client or a server...in which case, this will return false!
        /// </summary>
        public static bool IsClientVsServerStatusKnown => IsServer || IsClient;

        private static GONetServer _gonetServer;
        /// <summary>
        /// This will be set internally only on the server side.  Do NOT set yourself!
        /// </summary>
        public static GONetServer gonetServer
        {
            get { return _gonetServer; }
            internal set
            {
                if (value != null)
                {
                    SessionGUID = GUID.Generate().AsInt64();
                }

                MyAuthorityId = OwnerAuthorityId_Server;
                _gonetServer = value;
                _gonetServer.ClientConnected += Server_OnClientConnected_SendClientCurrentState;

                MyLocal = UnityEngine.Object.Instantiate(Global.gonetLocalPrefab);

                //const string INSTANTIATE = "Just called Instantiate server local context and now it has gonetId: ";
                //GONetLog.Debug(string.Concat(INSTANTIATE, MyLocal.GONetParticipant.GONetId));
            }
        }

        public static GONetEventBus EventBus => GONetEventBus.Instance;

        public const string REQUIRED_CALL_UNITY_MAIN_THREAD = "Not allowed to call this from any other thread than the main Unity thread.";
        private static Thread mainUnityThread;
        public static bool IsUnityMainThread => mainUnityThread == Thread.CurrentThread;

        public static bool IsApplicationQuitting { get; private set; }

        /// <summary>
        /// Throws an exception if not called from main Unity thread (see <see cref="IsUnityMainThread"/>).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureMainThread_IfPlaying()
        {
            if (Application.isPlaying && !IsUnityMainThread)
            {
                throw new InvalidOperationException(REQUIRED_CALL_UNITY_MAIN_THREAD);
            }
        }

        public static bool IsUnityApplicationEditor => Application.isEditor;

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
                        var randomlySelectedType = new SyncEvent_Time_ElapsedTicks_SetFromAuthority();
                        queue_needsReturnToPool.Enqueue(randomlySelectedType);
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
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread.
        /// At last time of updating this declaration, this will be used to store <see cref="SyncEvent_ValueChangeProcessed"/>, <see cref="ValueMonitoringSupport_BaselineExpiredEvent"/> and <see cref="ValueMonitoringSupport_NewBaselineEvent"/> child classes.
        /// </summary>
        static readonly Dictionary<Thread, Queue<IGONetEvent>> events_AwaitingSendToOthersQueue_ByThreadMap = new Dictionary<Thread, Queue<IGONetEvent>>(12);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread (i.e., transfer data from <see cref="events_AwaitingSendToOthersQueue_ByThreadMap"/> once the time is right) but also read from and dequeued from the main unity thread when time to publish the events!
        /// </summary>
        static readonly Dictionary<Thread, ConcurrentQueue<IGONetEvent>> events_SendToOthersQueue_ByThreadMap = new Dictionary<Thread, ConcurrentQueue<IGONetEvent>>(12);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread (i.e., transfer data from <see cref="events_AwaitingSendToOthersQueue_ByThreadMap"/> once the time is right) but also read from and dequeued from the main unity thread when time to publish the events!
        /// </summary>
        static readonly Queue<SyncEvent_ValueChangeProcessed> syncValueChanges_ReceivedFromOtherQueue = new Queue<SyncEvent_ValueChangeProcessed>(100);

        internal static GONetClient _gonetClient;
        /// <summary>
        /// This will be set internally only on the client side.  Do NOT set yourself!
        /// </summary>
        public static GONetClient GONetClient
        {
            get => _gonetClient;

            internal set
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
        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdAtInstantiationMap = new Dictionary<uint, GONetParticipant>(5000);
        internal static readonly Dictionary<uint, uint> recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map = new Dictionary<uint, uint>(1000);

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
        /// 
        /// This method will result in the extrapolation of synced GONetAutoMagicalSync values from <paramref name="gonetParticipant"/> that employ value blending techniques on the client side.
        /// This implies that no lead time buffer will be utilized within the value blending technique causing the best effort to match the value on the server/owner machine at the current time.
        /// </para>
        /// <para>POST: *if* this method returns true, the value of <paramref name="gonetParticipant"/>'s <see cref="GONetParticipant.OwnerAuthorityId"/> will be changed to <see cref="MyAuthorityId"/> AND a new value for <see cref="GONetParticipant.gonetId_raw"/> will be assigned.</para>
        /// </summary>
        public static bool Server_AssumeAuthorityOver(GONetParticipant gonetParticipant)
        {
            GONetLog.Debug("Server assuming authority over GNP.  Is Mine Already (i.e., client used server assigned GONetIdRaw batch)? " + IsMine(gonetParticipant));
            // TODO need to implement the logic for automatically getting the initiating client a new batch if it is running low

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

        private static void OnOwnerAuthorityIdChanged(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            OnGONetIdComponentChanged_EnsureMapKeysUpdated(gonetParticipant, eventEnvelope.Event.ValuePrevious.System_UInt16);

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

        internal static void OnGONetIdAboutToBeSet(uint gonetId_new, uint gonetId_raw_new, ushort ownerAuthorityId_new, GONetParticipant gonetParticipant)
        {
            if (gonetId_new == gonetParticipant.GONetIdAtInstantiation)
            {
                gonetParticipantByGONetIdAtInstantiationMap[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;
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
                    gonetParticipantByGONetIdAtInstantiationMap[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;

                    gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetIdAtInstantiation);
                    gonetParticipantByGONetIdMap[gonetId_new] = gonetParticipant; // TODO first check for collision/overwrite and throw exception....or warning at least!
                }
            }
        }

        private static void OnGONetIdChanged(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            OnGONetIdComponentChanged_EnsureMapKeysUpdated(eventEnvelope.GONetParticipant, eventEnvelope.Event.ValuePrevious.System_UInt32);

            OnGONetIdChanged_UpdatePersistentInstantiationEvents(eventEnvelope);
        }

        private unsafe static void OnGONetIdChanged_UpdatePersistentInstantiationEvents(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            LinkedListNode<IPersistentEvent> current = persistentEventsThisSession.First;

            while (current != null)
            {
                var persistentEvent = current.Value;
                if (persistentEvent is InstantiateGONetParticipantEvent)
                {
                    InstantiateGONetParticipantEvent instantiationEvent = (InstantiateGONetParticipantEvent)persistentEvent;
                    SyncEvent_ValueChangeProcessed newGONetIdEvent = eventEnvelope.Event;
                    if (instantiationEvent.GONetId == newGONetIdEvent.ValuePrevious.System_UInt32)
                    {
                        instantiationEvent.GONetId = newGONetIdEvent.ValueNew.System_UInt32;

                        // this is a struct and the copy over of the value is not going to stick inside the persistentEventsThisSession...so we do linked list stuffities to replace old

                        persistentEventsThisSession.AddBefore(current, instantiationEvent);
                        persistentEventsThisSession.Remove(current);

                        break;
                    }
                }

                current = current.Next;
            }
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

                    // there is a bug if we put this back in for projectile/gnp being assumed ownership by server in that it never gets placed into gonetParticipantByGONetIdMap with the new gonetId if we keep this: if (areAllComponentsChanging)
                    {
                        gonetParticipantByGONetIdAtInstantiationMap[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;

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

            GONetGlobal.ActualServerConnectionInfoSet += OnActualServerConnectionInfoSet_UpdateIsServerOverride;

            InitMessageTypeToMessageIDMap();
            InitShouldSkipSyncSupport();
        }

        private static void OnActualServerConnectionInfoSet_UpdateIsServerOverride(string serverIP, int serverPort)
        {
            isServerOverride |=
                NetworkUtils.IsIPAddressOnLocalMachine(GONetGlobal.ServerIPAddress_Actual) &&
                !NetworkUtils.IsLocalPortListening(GONetGlobal.ServerPort_Actual);
        }

        private static void InitEventSubscriptions()
        {
            EventBus.Subscribe<IGONetEvent>(OnAnyEvent_RelayToRemoteConnections_IfAppropriate);
            EventBus.Subscribe<IPersistentEvent>(OnPersistentEvent_KeepTrack);
            EventBus.Subscribe<PersistentEvents_Bundle>(OnPersistentEventsBundle_ProcessAll_Remote, envelope => envelope.IsSourceRemote);
            EventBus.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent_Remote, envelope => envelope.IsSourceRemote);

            EventBus.Subscribe<GONetParticipantDisabledEvent>(OnDisabledGNPEvent);

            var subscription = EventBus.Subscribe<DestroyGONetParticipantEvent>(OnDestroyGNPEvent_Remote, envelope => envelope.IsSourceRemote);
            subscription.SetSubscriptionPriority_INTERNAL(int.MinValue); // process internally LAST since the GO will be destroyed and other subscribers may want to do something just prior to it being destroyed

            EventBus.Subscribe<SyncEvent_ValueChangeProcessed>(OnSyncValueChangeProcessed_Persist_Local);

            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_GONetId, OnGONetIdChanged);
            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_OwnerAuthorityId, OnOwnerAuthorityIdChanged);

            EventBus.Subscribe<ValueMonitoringSupport_NewBaselineEvent>(OnNewBaselineValue_Remote, envelope => envelope.IsSourceRemote);
            
            EventBus.Subscribe<ClientRemotelyControlledGONetIdServerBatchAssignmentEvent>(Client_AssignNewClientGONetIdRawBatch);
        }

        private static void OnNewBaselineValue_Remote(GONetEventEnvelope<ValueMonitoringSupport_NewBaselineEvent> eventEnvelope)
        {
            ValueMonitoringSupport_NewBaselineEvent @event = eventEnvelope.Event;
            ApplyNewBaselineValue_Remote(@event);
        }

        private static void ApplyNewBaselineValue_Remote(ValueMonitoringSupport_NewBaselineEvent @event)
        {
            GONetParticipant gnp;
            if (gonetParticipantByGONetIdMap.TryGetValue(@event.GONetId, out gnp)
                || gonetParticipantByGONetIdMap.TryGetValue(GetGONetIdAtInstantiation(@event.GONetId), out gnp)) // IMPORTANT: this is here because a newly connecting client will process this new baseline with all the other persistent events, but it is done before the new gonetId assignment is made for gnps that were assumed authority by server and have a new gonetId
            {
                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gnp.codeGenerationId][gnp];

                AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = syncCompanion.valuesChangesSupport[@event.ValueIndex];
                //GONetSyncableValue baselineValue_previous = valueChangeSupport.baselineValue_current;

                if (@event is ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3) // most common first
                {
                    valueChangeSupport.baselineValue_current = ((ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3)@event).NewBaselineValue;
                }
                else if (@event is ValueMonitoringSupport_NewBaselineEvent_System_Single)
                {
                    valueChangeSupport.baselineValue_current = ((ValueMonitoringSupport_NewBaselineEvent_System_Single)@event).NewBaselineValue;
                }
                else if (@event is ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2)
                {
                    valueChangeSupport.baselineValue_current = ((ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2)@event).NewBaselineValue;
                }
                else if (@event is ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4)
                {
                    valueChangeSupport.baselineValue_current = ((ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4)@event).NewBaselineValue;
                }
                else if (@event is ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion)
                {
                    valueChangeSupport.baselineValue_current = ((ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion)@event).NewBaselineValue;
                }
                else
                {
                    const string NEW = "New baseline value is of type not yet accounted for.  type: ";
                    GONetLog.Warning(string.Concat(NEW, @event.GetType().FullName));
                }

                const string APPLIED = "New baseline value applied for type: ";
                const string INDEX = " valueIndex: ";
                //GONetLog.Debug(string.Concat(APPLIED, @event.GetType().FullName, INDEX, @event.ValueIndex));
            }
            else
            {
                const string GNID = "Unable to find GONetParticipant for GONetId: ";
                const string POSSI = ", which is possibly due to it being destroyed and this event came at a bad time just after destroy processed....like was the case during testing with ProjectileTest.unity";
                GONetLog.Warning(string.Concat(GNID, @event.GONetId, POSSI));
            }
        }

        private static void OnSyncValueChangeProcessed_Persist_Local(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            OnSyncValueChangeProcessed_Persist_Local(eventEnvelope.Event);
        }

        private static void OnSyncValueChangeProcessed_Persist_Local(SyncEvent_ValueChangeProcessed @event, bool doesRequireCopy = true)
        {
#if !PERF_NO_PROCESS_SYNC_EVENTS
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
#endif
        }

        private static void OnPersistentEventsBundle_ProcessAll_Remote(GONetEventEnvelope<PersistentEvents_Bundle> eventEnvelope)
        {
            GONetLog.Debug("******received persistent events bundle from remote source");

            { // TODO why not just publish each one of these so all the handlers kick in and process??????   otherwise, we are left with this piecemeal processing below....error prone and problematic for sure!
                foreach (var item in eventEnvelope.Event.PersistentEvents)
                {
                    persistentEventsThisSession.AddLast(item);

                    if (item is InstantiateGONetParticipantEvent)
                    {
                        Instantiate_Remote((InstantiateGONetParticipantEvent)item);
                    }
                    else if (item is ValueMonitoringSupport_NewBaselineEvent)
                    {
                        ApplyNewBaselineValue_Remote((ValueMonitoringSupport_NewBaselineEvent)item);
                    }
                }
            }
        }

        /// <summary>
        /// Definition of "if appropriate":
        ///     -The server will always send to remote connections....clients only send to remote connections (i.e., just to server) when locally sourced!
        /// </summary>
        private static void OnAnyEvent_RelayToRemoteConnections_IfAppropriate(GONetEventEnvelope<IGONetEvent> eventEnvelope)
        {
            if (eventEnvelope.Event is ILocalOnlyPublish ||                                            //If this event implements ILocalOnlyPublish means that it will only be published locally and it will not be remotely transmitted.
                (eventEnvelope.IsSingularRecipientOnly && IsServer && eventEnvelope.IsSourceRemote) || //If this event has arrived to the server from a remote source and it does not need to be relayed then do not keep executing this method.
                (IsClient && eventEnvelope.IsSourceRemote))                                            //If an event from a remote source (the server) has arrived to the client, it does not need to relay it to any other connection. That is a server side feature only.
            {
                return;
            }

            byte[] bytes = default;
            int returnBytesUsedCount = default;
            bool doesNeedToReturn = default;
            try
            {
                //Get bytes from memory pool
                bytes = SerializationUtils.SerializeToBytes(eventEnvelope.Event, out returnBytesUsedCount, out doesNeedToReturn); // TODO FIXME if the envelope is processed from a remote source, then we SHOULD attach the bytes to it and reuse them!

            }
            catch (Exception e)
            {
                GONetLog.Error(e.ToString());
                return;
            }

            //Decide the Reliability of the event transmission based on the envelope
            GONetChannelId channelId = eventEnvelope.IsReliable ? GONetChannel.EventSingles_Reliable : GONetChannel.EventSingles_Unreliable;

            //If the event was not generated by server and we are the server, we relay it to our connections except the event's remote originator.
            if (IsServer && eventEnvelope.IsSourceRemote)
            {
                SendBytesToRemoteConnectionsExceptSourceRemote(eventEnvelope.SourceAuthorityId, bytes, returnBytesUsedCount, channelId);
            }
            else if (IsServer || !eventEnvelope.IsSourceRemote)
            {
                //If we are a client we broadcast it to our connections (which are only the server)
                if (IsClient)
                {
                    GONetLog.Debug($"Sending event to server.  type: {eventEnvelope.Event.GetType().Name}");
                    SendBytesToRemoteConnections(bytes, returnBytesUsedCount, channelId);
                }
                else
                {
                    bool shouldBroadcast = eventEnvelope.TargetClientAuthorityId == OwnerAuthorityId_Unset;
                    if (shouldBroadcast)
                    {
                        SendBytesToRemoteConnections(bytes, returnBytesUsedCount, channelId);
                    }
                    else
                    {
                        GONetRemoteClient remoteClient = gonetServer.GetRemoteClientByAuthorityId(eventEnvelope.TargetClientAuthorityId);
                        SendBytesToRemoteConnection(remoteClient.ConnectionToClient, bytes, returnBytesUsedCount, channelId);
                    }
                }
            }

            if (doesNeedToReturn)
            {
                //Return borrowed bytes to memory pool
                SerializationUtils.ReturnByteArray(bytes);
            }
        }

        private static void SendBytesToRemoteConnectionsExceptSourceRemote(ushort remoteSourceAuthorityId, byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            GONetConnection_ServerToClient remoteClientConnection = null;
            uint count = _gonetServer.numConnections;

            for (int i = 0; i < count; ++i)
            {
                remoteClientConnection = _gonetServer.remoteClients[i].ConnectionToClient;
                if (remoteClientConnection.OwnerAuthorityId != remoteSourceAuthorityId)
                {
                    SendBytesToRemoteConnection(remoteClientConnection, bytes, bytesUsedCount, channelId);
                }
            }
        }

        private static readonly List<IPersistentEvent> persistentEventsCancelledOut = new List<IPersistentEvent>(100);

        private static void OnPersistentEvent_KeepTrack(GONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            ICancelOutOtherEvents iCancelOthers = eventEnvelope.Event as ICancelOutOtherEvents;
            persistentEventsCancelledOut.Clear();
            if (iCancelOthers != null)
            {
                var enumerator = persistentEventsThisSession.GetEnumerator(); // TODO consider narrowing down to just the items in there that match iCancelOthers.OtherEventTypeCancelledOut, but Linq is not desired...not sure how best to do this
                while (enumerator.MoveNext())
                {
                    IPersistentEvent consideredEvent = enumerator.Current;
                    if (TypeUtils.IsTypeAInstanceOfAnyTypesB(consideredEvent.GetType(), iCancelOthers.OtherEventTypesCancelledOut) && iCancelOthers.DoesCancelOutOtherEvent(consideredEvent))
                    {
                        persistentEventsCancelledOut.Add(consideredEvent);
                    }
                }
            }

            int count = persistentEventsCancelledOut.Count;
            if (count == 0)
            {
                persistentEventsThisSession.AddLast(eventEnvelope.Event);
            }
            else
            {
                for (int i = 0; i < count; ++i)
                {
                    IPersistentEvent cancelledEvent = persistentEventsCancelledOut[i];
                    persistentEventsThisSession.Remove(cancelledEvent); // remove the cancelled out earlier event
                }
            }

            //GONetLog.Debug($"Persistent event of type: {eventEnvelope.Event.GetType().Name} just passed through....updated overall count: {persistentEventsThisSession.Count}");
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

                if (eventEnvelope.Event.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority)
                {
                    GONetSpawnSupport_Runtime.Server_MarkToBeRemotelyControlled(instance, eventEnvelope.SourceAuthorityId);
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

        private static void OnDisabledGNPEvent(GONetEventEnvelope<GONetParticipantDisabledEvent> eventEnvelope)
        {
            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map[gonetParticipant.GONetId] = gonetParticipant.GONetIdAtInstantiation;
        }

        private static void OnDestroyGNPEvent_Remote(GONetEventEnvelope<DestroyGONetParticipantEvent> eventEnvelope)
        {
            ////GONetLog.Debug("DREETS pork");

            GONetParticipant gonetParticipant = null;
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

            // TODO FIXME add this back?: GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsRotationSyncd] = IsRotationNotSyncd;

            // TODO FIXME add this back?: GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsPositionSyncd] = IsPositionNotSyncd;
        }

        internal static bool IsRotationNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
        {
            return !monitoringSupport.syncCompanion.gonetParticipant.IsRotationSyncd;
        }

        internal static bool IsPositionNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
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

        /// <summary>
        /// <para>
        /// This is mainly here to support player controlled <see cref="GONetParticipant"/>s (GNPs) in a strict server authoritative setup where a client/player only submits inputs to have
        /// the server process remotely and hopefully manipulate this GNP.
        /// See <see cref="GONetParticipant.RemotelyControlledByAuthorityId"/> and <see cref="GONetParticipant.IsMine_ToRemotelyControl"/>.
        /// IMPORTANT: send to server to immediately assume ownership over, which will yield this being always at 0 latency (i.e., all values will be extrapolated to match server)!!!
        /// </para>
        /// <para>
        /// IMPORTANT: This could be used for projectiles too even if the client instantiating it will not control it after the initial "birth" of it.
        /// </para>
        /// </summary>
        public static GONetParticipant Client_InstantiateToBeRemotelyControlledByMe(GONetParticipant prefab, Vector3 position, Quaternion rotation)
        {
            if (IsClient)
            {
                GONetParticipant gonetParticipant = 
                    GONetSpawnSupport_Runtime.Instantiate_MarkToBeRemotelyControlled(prefab, position, rotation);

                // In order for the caller to immediately see that this is remotely controlled, set this here locally and server will do the same after processing
                // TODO make sure this local change does not mess up the one that occurs on server to propogate to all others
                gonetParticipant.RemotelyControlledByAuthorityId = MyAuthorityId;

                return gonetParticipant;
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Searches for <see cref="GONetParticipant"/> by <paramref name="gonetId"/> and checks against <see cref="GONetParticipant.GONetId"/> and <see cref="GONetParticipant.GONetIdAtInstantiation"/>.
        /// </summary>
        /// <returns>null if not found</returns>
        public static GONetParticipant GetGONetParticipantById(uint gonetId)
        {
            GONetParticipant gonetParticipant = null;
            if (!gonetParticipantByGONetIdMap.TryGetValue(gonetId, out gonetParticipant))
            {
                gonetParticipantByGONetIdAtInstantiationMap.TryGetValue(gonetId, out gonetParticipant);
            }
            return gonetParticipant;
        }

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread"/>
        /// IMPORTANT: *IF* this method is called and <paramref name="channelId"/> is associated with <see cref="QosType.Unreliable"/> *AND* the 
        ///            outbound buffer is full, it will NOT be queued up nor sent in which case false is returned.
        /// </summary>
        public static bool SendBytesToRemoteConnections(byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            return SendBytesToRemoteConnection(null, bytes, bytesUsedCount, channelId); // passing null will result in sending to all remote connections
        }

        /// <summary>
        /// As the server, send <paramref name="messageBytes"/> over <paramref name="channelId"/> to all connected clients except the one represented by <paramref name="sourceClientConnection"/>.
        /// </summary>
        private static void Server_SendBytesToNonSourceClients(byte[] messageBytes, int bytesUsedCount, GONetConnection sourceClientConnection, byte channelId)
        {
            uint count = _gonetServer.numConnections;
            for (int i = 0; i < count; ++i)
            {
                GONetConnection_ServerToClient remoteClientConnection = _gonetServer.remoteClients[i].ConnectionToClient;
                if (remoteClientConnection.OwnerAuthorityId != sourceClientConnection.OwnerAuthorityId)
                {
                    SendBytesToRemoteConnection(remoteClientConnection, messageBytes, bytesUsedCount, channelId);
                }
            }
        }

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread"/>
        /// IMPORTANT: *IF* this method is called and <paramref name="channelId"/> is associated with <see cref="QosType.Unreliable"/> *AND* the 
        ///            outbound buffer is full, it will NOT be queued up nor sent in which case false is returned.
        /// </summary>
        private static bool SendBytesToRemoteConnection(GONetConnection sendToConnection, byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            SingleProducerQueues singleProducerSendQueues = ReturnSingleProducerResources_IfAppropriate(singleProducerSendQueuesByThread, Thread.CurrentThread);

            { // flow control:
                if (GONetChannel.ById(channelId).QualityOfService == QosType.Unreliable && singleProducerSendQueues.resourcePool.BorrowedCount > SingleProducerQueues.MAX_PACKETS_PER_TICK - 10) // TODO better config of this
                {
                    //const string SURPASSED = "Surpassed limit...we will now essentially throw away this unreliable data you were trying to send.";
                    //GONetLog.Info(SURPASSED);
                    return false;
                }
            }

            byte[] bytesCopy = singleProducerSendQueues.resourcePool.Borrow(bytesUsedCount);
            Buffer.BlockCopy(bytes, 0, bytesCopy, 0, bytesUsedCount);

            NetworkData networkData = new NetworkData()
            {
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                messageBytes = bytesCopy,
                bytesUsedCount = bytesUsedCount,
                relatedConnection = sendToConnection,
                channelId = channelId
            };

            singleProducerSendQueues.queueForWork.Enqueue(networkData);

            return true;
        }

        /// <summary>
        /// POST: if there is no entry for <paramref name="producerThread"/> in <paramref name="singleProducerQueuesByThread"/>, a new one is instantiated and added.
        /// </summary>
        private static SingleProducerQueues ReturnSingleProducerResources_IfAppropriate(ConcurrentDictionary<Thread, SingleProducerQueues> singleProducerQueuesByThread, Thread producerThread)
        {
            SingleProducerQueues singleProducerQueues;
            if (singleProducerQueuesByThread.TryGetValue(producerThread, out singleProducerQueues))
            {
                int processedCount = 0;
                int readyCount = singleProducerQueues.queueForPostWorkResourceReturn.Count;
                NetworkData readyToReturn;
                while (processedCount < readyCount && singleProducerQueues.queueForPostWorkResourceReturn.TryDequeue(out readyToReturn))
                {
                    singleProducerQueues.resourcePool.Return(readyToReturn.messageBytes); // since we now know we are on the correct thread (i.e.., same as borrowed on) we can return it to pool
                    ++processedCount;
                }

                if (processedCount < readyCount)
                {
                    GONetLog.Warning($"Not sure why, but there were {readyCount} items ready to be returned to resource pool, but we only returned {processedCount}.");
                }
            }
            else
            {
                singleProducerQueuesByThread[producerThread] = singleProducerQueues = new SingleProducerQueues();
            }

            return singleProducerQueues;
        }

        internal static ulong tickCount_endOfTheLineSendAndSave_Thread;
        private static volatile bool isRunning_endOfTheLineSendAndSave_Thread;
        private static void SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread()
        {
            tickCount_endOfTheLineSendAndSave_Thread = 0;

            while (isRunning_endOfTheLineSendAndSave_Thread)
            {
                try
                {
                    { // Do send stuffs
                        var sendThreads = singleProducerSendQueuesByThread.Keys;
                        foreach (var sendThread in sendThreads)
                        {
                            SingleProducerQueues singleProducerSendQueues = singleProducerSendQueuesByThread[sendThread];
                            ConcurrentQueue<NetworkData> endOfTheLineSendQueue = singleProducerSendQueues.queueForWork;
                            int processedCount = 0;
                            int readyCount = endOfTheLineSendQueue.Count;
                            NetworkData networkData;
                            while (processedCount < readyCount && endOfTheLineSendQueue.TryDequeue(out networkData))
                            {
                                if (networkData.relatedConnection == null)
                                {
                                    if (IsServer)
                                    {
                                        if (_gonetServer != null)
                                        {
                                           //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds + " size: " + networkData.bytesUsedCount);
                                            _gonetServer.SendBytesToAllClients(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                                        }
                                    }
                                    else
                                    {
                                        if (GONetClient != null)
                                        {
                                            while (isRunning_endOfTheLineSendAndSave_Thread && !GONetClient.IsConnectedToServer)
                                            {
                                                const string SLEEP = "SLEEP!  I am not connected right now.  I have data to send, but need to wait to be connected in order to send it.";
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
                                    //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds + " size: " + networkData.bytesUsedCount);
                                    networkData.relatedConnection.SendMessageOverChannel(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                                }

                                { // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                                    singleProducerSendQueues.queueForPostWorkResourceReturn.Enqueue(networkData);
                                }

                                ++processedCount;
                            }

                            if (processedCount < readyCount)
                            {
                                GONetLog.Warning($"Not sure why, but there were {readyCount} items ready to be processed, but we only processed {processedCount}.");
                            }
                        }
                    }

#if !PERF_NO_PROCESS_SYNC_EVENTS
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
#endif
                }
                catch (Exception e)
                {
                    GONetLog.Error(string.Concat("Unexpected error attempting to process sends in separate thread.  Exception.Type: ", e.GetType().Name, " Exception.Message: ", e.Message, " \nException.StackTrace: ", e.StackTrace));
                }
                finally
                {
                    ++tickCount_endOfTheLineSendAndSave_Thread;
                }
            }
        }

        #endregion

        #region internal methods

        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_reliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Reliable, false);
        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_unreliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Unreliable, false);

        static Thread endOfLineSendAndSaveThread;

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/> once per Unity <see cref="MonoBehaviour"/> Update cycle.
        /// </summary>
        internal static void Update(GONetBehaviour coroutineManager)
        {
            Time.Update(); // This is the important thing to execute as early in a frame as possible (hence the -32000 setting in Script Execution Order) to get more accurate network timing to match Unity's frame time as it relates to values changing

            EventBus.PublishQueuedEventsForMainThread();

            if (myLocal == null) // NOTE: This check is important since it will eventually call Update_DoTheHeavyLifting_IfAppropriate, which is also called regularly from MyLocal.LateUpdate and we only want to process this during the time MyLocal is not present (i.e., since it is instantiated after start-up)
            {
                coroutineManager.StartCoroutine(Update_EndOfFrame());
            }
        }

        private static IEnumerator Update_EndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            Update_DoTheHeavyLifting_IfAppropriate(null, false);
        }

        static int lastCalledFrame_Update_DoTheHeavyLifting = -1;

        internal static void Update_DoTheHeavyLifting_IfAppropriate(GONetLocal gonetLocalCaller, bool shouldCheckGONetLocalArgument)
        {
            bool isAppropriate = (!shouldCheckGONetLocalArgument || gonetLocalCaller == myLocal)
                && lastCalledFrame_Update_DoTheHeavyLifting < UnityEngine.Time.frameCount; // avoid accidentally calling this multiple times a frame since it is called from two possible places

            if (isAppropriate)
            {
                lastCalledFrame_Update_DoTheHeavyLifting = UnityEngine.Time.frameCount;

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

                PublishEvents_SentToOthers();
#if !PERF_NO_PROCESS_SYNC_EVENTS
                PublishEvents_SyncValueChanges_ReceivedFromOthers();
#endif
                SaveEventsInQueueASAP_IfAppropriate();

                if (endOfLineSendAndSaveThread == null)
                {
                    isRunning_endOfTheLineSendAndSave_Thread = true;
                    endOfLineSendAndSaveThread = new Thread(SendBytes_EndOfTheLine_AllSendsAndSavesMUSTComeHere_SeparateThread);
                    endOfLineSendAndSaveThread.Name = "GONet End-of-the-Line Send & Save";
                    endOfLineSendAndSaveThread.Priority = System.Threading.ThreadPriority.AboveNormal;
                    endOfLineSendAndSaveThread.IsBackground = true; // do not prevent process from exiting when foreground thread(s) end
                    endOfLineSendAndSaveThread.Start();
                }

                if (IsServer)
                {
                    _gonetServer?.Update();
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

                recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map.Clear();
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
            byte[] bytes = SerializationUtils.SerializeToBytes(SyncEvent_PersistenceBundle.Instance, out returnBytesUsedCount, out bool doesNeedToReturn);

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

            if (doesNeedToReturn)
            {
                SerializationUtils.ReturnByteArray(bytes);
            }
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

        private static void PublishEvents_SentToOthers()
        {
            var eventDictionaryEnumerator = events_SendToOthersQueue_ByThreadMap.GetEnumerator();
            while (eventDictionaryEnumerator.MoveNext())
            {
                ConcurrentQueue<IGONetEvent> eventQueue = eventDictionaryEnumerator.Current.Value;
                int count = eventQueue.Count;
                IGONetEvent @event;
                while (count > 0 && eventQueue.TryDequeue(out @event))
                {
                    try
                    {
                        EventBus.Publish(@event);
                    }
                    catch (Exception e)
                    {
                        const string BOO = "Boo.  Publishing this event failed.  Error.Message: ";
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

                        if (SendBytesToRemoteConnections(bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable))
                        {
                            //GONetLog.Debug("just sent time sync to server....my time (seconds): " + TimeSpan.FromTicks(timeSync.OccurredAtElapsedTicks).TotalSeconds);
                        }

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
                    //GONetLog.Debug("RTT_Latest (s): " + _gonetClient.connectionToServer.RTT_Latest + " ****RTT_RecentAverage (s): " + _gonetClient.connectionToServer.RTT_RecentAverage + " low_level.rtt (ms): " + _gonetClient.connectionToServer.RTTMilliseconds_LowLevelTransportProtocol);

                    long oneWayDelayTicks = TimeSpan.FromSeconds(GONetClient.connectionToServer.RTT_RecentAverage).Ticks >> 1; // divide by 2
                    long newClientTimeTicks = server_elapsedTicksAtSendResponse + oneWayDelayTicks;

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
                else { GONetLog.Warning("The time sync response from server is somehow (i.e., UDP out of order packets) older than the another response already process.  It makes no sense to use this out of date information."); }
            }
        }

        #endregion

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>.
        /// Calling this cleans up things from the game session.
        /// </summary>
        internal static void Shutdown()
        {
            LogMinsAndMaxsEncountered();

            isRunning_endOfTheLineSendAndSave_Thread = false;

            if (IsServer)
            {
                _gonetServer?.Stop();
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

        [Conditional("GONET_MEASURE_VALUES_MIN_MAX")]
        private static void LogMinsAndMaxsEncountered()
        {
            foreach (var syncCompanionsForCodeGenerationId in activeAutoSyncCompanionsByCodeGenerationIdMap)
            {
                GONetCodeGenerationId codeGenerationId = syncCompanionsForCodeGenerationId.Key;
                int valueCount = 0;
                List<GONetSyncableValue> mins_forCodeGenerationId = new List<GONetSyncableValue>();
                List<GONetSyncableValue> maxs_forCodeGenerationId = new List<GONetSyncableValue>();

                foreach (var gnpAndSyncCompanion in syncCompanionsForCodeGenerationId.Value)
                {
                    valueCount = gnpAndSyncCompanion.Value.valuesCount;
                    for (int i = 0; i < valueCount; ++i)
                    {
                        var val = gnpAndSyncCompanion.Value.valuesChangesSupport[i];

                        if (mins_forCodeGenerationId.Count <= i)
                        {
                            mins_forCodeGenerationId.Add(val.valueLimitEncountered_min);
                        }
                        else
                        {
                            var currentMin = mins_forCodeGenerationId[i];
                            GONetSyncableValue.UpdateMinimumEncountered_IfApppropriate(ref currentMin, val.valueLimitEncountered_min);
                            mins_forCodeGenerationId[i] = currentMin;
                        }

                        if (maxs_forCodeGenerationId.Count <= i)
                        {
                            maxs_forCodeGenerationId.Add(val.valueLimitEncountered_max);
                        }
                        else
                        {
                            var currentMax = maxs_forCodeGenerationId[i];
                            GONetSyncableValue.UpdateMaximumEncountered_IfApppropriate(ref currentMax, val.valueLimitEncountered_max);
                            maxs_forCodeGenerationId[i] = currentMax;
                        }
                    }
                }

                for (int i = 0; i < valueCount; ++i)
                {
                    GONetLog.Debug(string.Concat("codeGenerationId: ", codeGenerationId, " index: ", i, " min: ", mins_forCodeGenerationId[i].ToString(), " max: ", maxs_forCodeGenerationId[i].ToString()));
                }
            }
        }

        private static void RemitEula_IfAppropriate(string eulaFilePath)
        {
            if (File.Exists(eulaFilePath))
            {
                bool isEulaRequirementMetOtherMeans = (DateTime.UtcNow.Ticks - ticksAtLastInit_UtcNow) < 3007410000 || (IsServer && server_lastAssignedAuthorityId == OwnerAuthorityId_Unset) ||
                    System.BitConverter.IsLittleEndian && System.BitConverter.GetBytes(double.NaN)[7] == (Math.Pow(2, 8) - 1) && "😊".Length == 2 && Convert.ToBoolean(Convert.ToInt32("101", 2)) && Enumerable.Range(1, 10).Sum() == Enumerable.Range(1, 10).Aggregate((a, b) => a + b);
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
            public Thread messageBytesBorrowedOnThread;
            public byte[] messageBytes;
            public int bytesUsedCount;
            public GONetChannelId channelId;
        }

        static readonly ConcurrentDictionary<Thread, SingleProducerQueues> singleProducerReceiveQueuesByThread = new ConcurrentDictionary<Thread, SingleProducerQueues>();
        static readonly ConcurrentDictionary<Thread, SingleProducerQueues> singleProducerSendQueuesByThread = new ConcurrentDictionary<Thread, SingleProducerQueues>();

        /// <summary>
        /// Each <see cref="Thread"/> that ends up calling either 
        /// 
        /// A) For Sends:
        /// <see cref="SendBytesToRemoteConnections(GONetCodeGenerationId[], int, GONetCodeGenerationId)"/> or 
        /// <see cref="SendBytesToRemoteConnection(GONetConnection, GONetCodeGenerationId[], int, GONetCodeGenerationId)"/> (i.e., the producer)
        /// 
        /// B) For Receives:
        /// <see cref="ProcessIncomingBytes_TriageFromAnyThread(GONetConnection, GONetCodeGenerationId[], int, GONetCodeGenerationId)"/> (i.e., the producer)
        /// 
        /// will have an instance of this to keep track of related stuffs as it moves between the producer and consumer thread (i.e., "end of the line" for sends).
        /// </summary>
        internal sealed class SingleProducerQueues
        {
            internal const int MAX_PACKETS_PER_TICK = 10 * 100;

            internal readonly ConcurrentQueue<NetworkData> queueForWork = new ConcurrentQueue<NetworkData>();
            internal readonly ConcurrentQueue<NetworkData> queueForPostWorkResourceReturn = new ConcurrentQueue<NetworkData>();
            internal readonly ArrayPool<byte> resourcePool = new ArrayPool<byte>(MAX_PACKETS_PER_TICK, 1, SerializationUtils.MTU, SerializationUtils.MTU_x8);
        }

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

            /// <summary>
            /// <para>This value is as close to the "same" across ALL machines in the game session (server and clients alike).</para>
            /// 
            /// <para>
            /// If you want to know how far back in time the client is simulating to account for the buffered values in 
            /// order to have smoothed/real data to display (i.e., <see cref="GONetGlobal.valueBlendingBufferLeadTimeMilliseconds"/>
            /// as set in the Inspector), then use <see cref="ElapsedSeconds_ClientSimulation"/> instead of this.
            /// </para>
            /// </summary>
            public double ElapsedSeconds => elapsedSeconds;

            /// <summary>
            /// Use this if you want to know how far back in time from the actual time of <see cref="ElapsedSeconds"/> that the 
            /// client is simulating to account for the buffered values in order to have smoothed/real data to display 
            /// (i.e., <see cref="GONetGlobal.valueBlendingBufferLeadTimeMilliseconds"/> as set in the Inspector).
            /// </summary>
            public double ElapsedSeconds_ClientSimulation => elapsedSeconds - valueBlendingBufferLeadSeconds;

            float lastUpdateSeconds;
            /// <summary>
            /// <para>Duration of seconds between the most two recent calls to <see cref="Update"/>.</para>
            /// <para>This is the GONet ~equivalent to <see cref="UnityEngine.Time.deltaTime"/>, but does NOT account for being called from within FixedUpdate and will NEVER represent the deltaTime for between calls to FixedUpdate is local to this instance.</para>
            /// </summary>
            public float DeltaTime => lastUpdateSeconds;

            internal volatile int updateCount = 0;

            public SecretaryOfTemporalAffairs() { }

            public SecretaryOfTemporalAffairs(SecretaryOfTemporalAffairs initFromAuthority)
            {
                SetFromAuthority(initFromAuthority.ElapsedTicks);
            }

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
                double elapsedSecondsPrevious = elapsedSeconds;

                ++UpdateCount;

                if (elapsedSeconds == ElapsedSecondsUnset)
                {
                    baselineTicks = HighResolutionTimeUtils.Now.Ticks;
                }

                long elapsedTicks_withoutEasement = HighResolutionTimeUtils.Now.Ticks - baselineTicks;
                ElapsedTicks = elapsedTicks_withoutEasement - GetTicksToSubtractForSetFromAuthorityEasing();

                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                lastUpdateSeconds = (float)(elapsedSeconds - elapsedSecondsPrevious);

                if (IsUnityMainThread)
                {
                    //GONetLog.Debug(string.Concat("gonet.seconds: ", ElapsedSeconds, " unity.seconds: ", UnityEngine.Time.time, " diff: ", (UnityEngine.Time.time - ElapsedSeconds), " gonet.hash: ", GetHashCode()));
                }
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

            SingleProducerQueues singleProducerReceiveQueues = ReturnSingleProducerResources_IfAppropriate(singleProducerReceiveQueuesByThread, Thread.CurrentThread);

            NetworkData networkData = new NetworkData()
            {
                relatedConnection = sourceConnection,
                messageBytes = singleProducerReceiveQueues.resourcePool.Borrow(bytesUsedCount),
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                bytesUsedCount = bytesUsedCount,
                channelId = channelId
            };

            Buffer.BlockCopy(messageBytes, 0, networkData.messageBytes, 0, bytesUsedCount);

            singleProducerReceiveQueues.queueForWork.Enqueue(networkData);
        }

        #endregion

        #region private methods

        /// <summary>
        /// This is where ***all*** incoming message are run through the handling/processing logic.
        /// Call this from the main Unity thread!
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread()
        {
            using (var enumerator = singleProducerReceiveQueuesByThread.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    SingleProducerQueues singleProducerReceiveQueues = enumerator.Current.Value;
                    ConcurrentQueue<NetworkData> incomingNetworkData = singleProducerReceiveQueues.queueForWork;
                    NetworkData networkData;
                    int readyCount = incomingNetworkData.Count;
                    int processedCount = 0;
                    while (processedCount < readyCount && incomingNetworkData.TryDequeue(out networkData))
                    {
                        ++processedCount;
                        try
                        {
                            // IMPORTANT: This check must come first as it exits early if condition met!
                            if (!IsChannelClientInitializationRelated(networkData.channelId) && IsClient && !_gonetClient.IsInitializedWithServer)
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
                }
            }
        }

        public delegate void CustomChannelPayloadHandler(GONetChannelId channelId, GONetConnection relatedConnection, byte[] messageBytes, int bytesUsedCount);
        public static event CustomChannelPayloadHandler OnCustomChannelPayloadReceived;

        /// <summary>
        /// POST: <paramref name="networkData"/> is returned to the associated/proper queue in <see cref="singleProducerSendQueuesByThread"/>
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(NetworkData networkData)
        {
            try
            {
                if (networkData.channelId == GONetChannel.ClientInitialization_EventSingles_Reliable || networkData.channelId == GONetChannel.EventSingles_Reliable || networkData.channelId == GONetChannel.EventSingles_Unreliable)
                {
                    DeserializeBody_EventSingle(networkData.messageBytes, networkData.relatedConnection);
                }
                else if (GONetChannel.IsGONetCoreChannel(networkData.channelId))
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
                            if (messageType == typeof(AutoMagicalSync_ValueChanges_Message) ||
                                messageType == typeof(AutoMagicalSync_ValuesNowAtRest_Message))
                            {
                                DeserializeBody_BundleOfChoice(bitStream, networkData.relatedConnection, networkData.channelId, elapsedTicksAtSend, messageType);
                                if (IsServer)
                                {
                                    /*
                                     * When dealing with client -> server -> client experience, which is to say the server needs to re broadcast this "values now at rest bundle" 
                                     * since we piggy backed this "at rest" impl off of the value change impl where the re broadcast pretty much happens automatically through the 
                                     * changed value, but things are a little different for "at rest" seeing as how the server receiving the initiating client's "at rest" message 
                                     * could already have that same "at rest" value as its latest in the buffer prior to receiving the "at rest" message when it clears out the buffer 
                                     * except for the at rest value and that means the server would not realize or have a mechanism to turn around and send "at rest" to other clients, 
                                     * which is the remaining issue in long drawn out Shaun speak.
                                     */
                                    Server_SendBytesToNonSourceClients(networkData.messageBytes, networkData.bytesUsedCount, networkData.relatedConnection, networkData.channelId);
                                }
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
                else
                {
                    OnCustomChannelPayloadReceived?.Invoke(networkData.channelId, networkData.relatedConnection, networkData.messageBytes, networkData.bytesUsedCount);
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
                    SingleProducerQueues singleProducerReceiveQueues = singleProducerReceiveQueuesByThread[networkData.messageBytesBorrowedOnThread];
                    singleProducerReceiveQueues.queueForPostWorkResourceReturn.Enqueue(networkData);
                }
            }
        }

        private static void DeserializeBody_EventSingle(byte[] messageBytes, GONetConnection relatedConnection)
        {
            IGONetEvent @event = SerializationUtils.DeserializeFromBytes<IGONetEvent>(messageBytes);
            EventBus.Publish(@event, relatedConnection.OwnerAuthorityId);
            //GONetLog.Debug($"Incoming event being published.  Type: {@event.GetType().Name}");
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
            GONetParticipant instance =
                string.IsNullOrWhiteSpace(instantiateEvent.ParentFullUniquePath)
                    ? UnityEngine.Object.Instantiate(template, instantiateEvent.Position, instantiateEvent.Rotation)
                    : UnityEngine.Object.Instantiate(template, instantiateEvent.Position, instantiateEvent.Rotation, HierarchyUtils.FindByFullUniquePath(instantiateEvent.ParentFullUniquePath).transform);

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
                instance.SetGONetIdFromRemoteInstantiation(instantiateEvent);
            }
            remoteSpawns_avoidAutoPropagateSupport.Add(instance);
            instance.IsOKToStartAutoMagicalProcessing = true;

            isCurrentlyProcessingInstantiateGNPEvent = false;

            return instance;
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient connectionToClient)
        {
            Server_AssignNewClientAuthorityId(connectionToClient);
            Server_AssignNewClientGONetIdRawBatch(connectionToClient);
            Server_SendClientPersistentEventsSinceStart(connectionToClient);
            Server_SendClientCurrentState_AllAutoMagicalSync(connectionToClient);
            Server_SendClientIndicationOfInitializationCompletion(connectionToClient); // NOTE: sending this will cause the client to instantiate its GONetLocal
        }

        private static void Server_OnNewClientInstantiatedItsGONetLocal(GONetLocal newClientGONetLocal)
        {
            GONetRemoteClient remoteClient = _gonetServer.GetRemoteClientByAuthorityId(newClientGONetLocal.GONetParticipant.OwnerAuthorityId);
            remoteClient.IsInitializedWithServer = true;
        }

        private static void Server_SendClientPersistentEventsSinceStart(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            if (persistentEventsThisSession.Count > 0)
            {
                //GONetLog.Debug($"About to send this many persistent events to newly connected client: {persistentEventsThisSession.Count}");
                PersistentEvents_Bundle bundle = new PersistentEvents_Bundle(Time.ElapsedTicks, persistentEventsThisSession);
                int returnBytesUsedCount;

                byte[] bytes = SerializationUtils.SerializeToBytes<IGONetEvent>(bundle, out returnBytesUsedCount, out bool doesNeedToReturn); // EXTREMELY important to include the <IGONetEvent> because there are multiple options for MessagePack to serialize this thing based on BobWad_Generated.cs' usage of [MemoryPack.MemoryPackUnion] for relevant interfaces this concrete class implements and the other end's call to deserialize will be to DeserializeBody_EventSingle and <IGONetEvent> will be used there too!!!
                SendBytesToRemoteConnection(gonetConnection_ServerToClient, bytes, returnBytesUsedCount, GONetChannel.ClientInitialization_EventSingles_Reliable);
                if (doesNeedToReturn)
                {
                    SerializationUtils.ReturnByteArray(bytes);
                }
            }
        }

        private static void Server_AssignNewClientAuthorityId(GONetConnection_ServerToClient connectionToClient)
        {
            // first assign locally
            connectionToClient.OwnerAuthorityId = ++server_lastAssignedAuthorityId;
            _gonetServer.OnConnectionToClientAuthorityIdAssigned(connectionToClient, connectionToClient.OwnerAuthorityId); // TODO this should automatically happen via event...i.e., update the setter above to do event stuff on change!

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

        private static void Server_AssignNewClientGONetIdRawBatch(GONetConnection_ServerToClient connectionToClient)
        {
            var @event = new ClientRemotelyControlledGONetIdServerBatchAssignmentEvent();
            uint batchStart = lastAssignedGONetIdRaw + 1;
            @event.GONetIdRawBatchStart = batchStart;
            
            client_finalServerGONetIdForRemoteControl_batchStartValues.Add(batchStart);
            lastAssignedGONetIdRaw += CLIENT_FINAL_SERVER_GONETID_BATCH_SIZE;
            
            EventBus.Publish(@event, targetClientAuthorityId: connectionToClient.OwnerAuthorityId);
        }

        private static void Client_AssignNewClientGONetIdRawBatch(
            GONetEventEnvelope<ClientRemotelyControlledGONetIdServerBatchAssignmentEvent> eventEnvelope)
        {
            if (IsClient)
            {
                client_finalServerGONetIdForRemoteControl_batchStartValues.Add(eventEnvelope.Event.GONetIdRawBatchStart);
            }
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetIdRaw = GONetParticipant.GONetIdRaw_Unset;
        static uint client_lastServerGONetIdRawForRemoteControl = GONetParticipant.GONetIdRaw_Unset;
        static readonly List<uint> client_finalServerGONetIdForRemoteControl_batchStartValues = new List<uint>();
        static readonly Stack<int> client_finalServerGONetIdForRemoteControl_batchStartValues_removeIndexStack = new Stack<int>();
        const int CLIENT_FINAL_SERVER_GONETID_BATCH_SIZE = 100;

        /// <summary>
        /// For every runtime instance of <see cref="GONetParticipant"/>, there will be one and only one item in one and only one of the <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/>'s <see cref="Dictionary{TKey, TValue}.Values"/>.
        /// The key into this is the <see cref="GONetParticipant.codeGenerationId"/>.
        /// </summary>
        static readonly Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> activeAutoSyncCompanionsByCodeGenerationIdMap =
            new Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>>(byte.MaxValue);
        static readonly Dictionary<GONetCodeGenerationId, Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated>> activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance =
            new Dictionary<GONetCodeGenerationId, Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated>>(byte.MaxValue);

        static readonly Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupportByFrequencyMap =
            new Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        static readonly List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupports_UnityMainThread =
            new List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        // TODO FIXME make internal just for editor!
        public static GONetParticipant_AutoMagicalSyncCompanion_Generated GetSyncCompanionByGNP(GONetParticipant gnp)
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = null;

            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> collection;
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gnp.codeGenerationId, out collection))
            {
                collection.TryGetValue(gnp, out syncCompanion);
            }

            return syncCompanion;
        }

        internal class AutoMagicalSync_ValueMonitoringSupport_ChangedValue
        {
            internal byte index;
            internal GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;
            internal string memberName;

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

            /// <summary>
            /// similar to keeping track of initial value, but it could change over time due to some new rules to support increased quantization/compression
            /// </summary>
            internal GONetSyncableValue baselineValue_current;

            internal GONetSyncableValue lastKnownValue;
            internal GONetSyncableValue lastKnownValue_previous;

            /// <summary>
            /// Throughout a session, this will represent the minimum value of <see cref="lastKnownValue"/> encountered since the start of the session.
            /// IMPORTANT: This is only updated ***on the owner's machine*** if the following precompiler definition exists: GONET_MEASURE_VALUES_MIN_MAX (see <see cref="GONetSyncableValue.UpdateMinimumEncountered_IfApppropriate"/>)
            /// </summary>
            internal GONetSyncableValue valueLimitEncountered_min;
            /// <summary>
            /// Throughout a session, this will represent the maximum value of <see cref="lastKnownValue"/> encountered since the start of the session.
            /// IMPORTANT: This is only updated ***on the owner's machine*** if the following precompiler definition exists: GONET_MEASURE_VALUES_MIN_MAX (see <see cref="GONetSyncableValue.UpdateMinimumEncountered_IfApppropriate"/>)
            /// </summary>
            internal GONetSyncableValue valueLimitEncountered_max;

            internal const int MOST_RECENT_CHANGEs_SIZE_MINIMUM = 10;
            internal const int MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED = 100;
            internal static readonly ArrayPool<NumericValueChangeSnapshot> mostRecentChangesPool = new ArrayPool<NumericValueChangeSnapshot>(1000, 50, MOST_RECENT_CHANGEs_SIZE_MINIMUM, MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED);
            internal static readonly long AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS = TimeSpan.FromSeconds(1).Ticks;

            /// <summary>
            /// This will be null when <see cref="syncAttribute_ShouldBlendBetweenValuesReceived"/> is false AND/OR if the value type is NOT numeric (although, the latter will be identified early on in either generation or runtime and cause an exception to essentially disallow that!).
            /// IMPORTANT: This is always sorted in most recent with lowest index to oldest with highest index order.
            /// </summary>
            internal NumericValueChangeSnapshot[] mostRecentChanges;
            internal int mostRecentChanges_capacitySize;
            internal int mostRecentChanges_usedSize = 0;
            private ushort mostRecentChanges_UpdatedByAuthorityId;

            /// <summary>
            /// If true, a message from owner came in indicating this is at rest, but is awaiting processing of that while the 
            /// value blending buffer lead time transpires first.
            /// Also, if true, <see cref="hasAwaitingAtRest_assumedInitialRestElapsedTicks"/> will indicate when the source indicating as much (i.e., elapsedTicksAtSend).
            /// </summary>
            internal bool hasAwaitingAtRest;
            internal long hasAwaitingAtRest_assumedInitialRestElapsedTicks;
            internal long hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks;
            internal GONetSyncableValue hasAwaitingAtRest_value;

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
                    if (item.elapsedTicksAtChange == elapsedTicksAtChange)
                    {
                        return; // avoid adding in new items with same timestamp as an existing item as it will mess up value blending, NOTE: This probably only happens just after an 'at rest'
                    }

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

                        bool isPreviousValuePresent = (i + 1) < mostRecentChanges_usedSize;
                        if (isPreviousValuePresent)
                        {
                            AdjustValueOnExpectedUpcomingNewBaseline_IfAppropriate(ref value, mostRecentChanges[i + 1].numericValue);
                        }

                        mostRecentChanges[i] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                        if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                        {
                            ++mostRecentChanges_usedSize;
                            //GONetLog.Debug("added new recent change...gonetId: " + syncCompanion.gonetParticipant.GONetId + " index: " + index);
                        }
                        //LogBufferContentsIfAppropriate();
                        return;
                    }
                }

                if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                {
                    mostRecentChanges[mostRecentChanges_usedSize] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                    ++mostRecentChanges_usedSize;
                    //GONetLog.Debug("added new recent change...gonetId: " + syncCompanion.gonetParticipant.GONetId + " index: " + index);
                }

                //LogBufferContentsIfAppropriate();
            }

            private void AdjustValueOnExpectedUpcomingNewBaseline_IfAppropriate(ref GONetSyncableValue valueNew, GONetSyncableValue valuePrevious)
            {
                switch (valueNew.GONetSyncType)
                {
                    case GONetSyncableValueTypes.UnityEngine_Vector3: // see IsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange to consolidate impls like below since they are very similar?
                        UnityEngine.Vector3 diff = valueNew.UnityEngine_Vector3 - valuePrevious.UnityEngine_Vector3;
                        System.Single componentLimitLower = syncAttribute_QuantizerSettingsGroup.lowerBound;// * 0.8f; // TODO cache this value
                        System.Single componentLimitUpper = syncAttribute_QuantizerSettingsGroup.upperBound;// * 0.8f; // TODO cache this value
                        
                        bool isLikelyBeingProcessedPriorToExpectedUpcomingNewBaseline =
                            diff.x < componentLimitLower || diff.x > componentLimitUpper ||
                            diff.y < componentLimitLower || diff.y > componentLimitUpper ||
                            diff.z < componentLimitLower || diff.z > componentLimitUpper;

                        if (isLikelyBeingProcessedPriorToExpectedUpcomingNewBaseline)
                        {
                            GONetLog.Debug("the new value being placed in buffer is happening prior to applying the new baseline!");

                            Vector3 replacementValue = valueNew.UnityEngine_Vector3;

                            if (diff.x < componentLimitLower) replacementValue.x += componentLimitLower;
                            if (diff.x > componentLimitUpper) replacementValue.x -= componentLimitUpper;
                            if (diff.y < componentLimitLower) replacementValue.y += componentLimitLower;
                            if (diff.y > componentLimitUpper) replacementValue.y -= componentLimitUpper;
                            if (diff.z < componentLimitLower) replacementValue.z += componentLimitLower;
                            if (diff.z > componentLimitUpper) replacementValue.z -= componentLimitUpper;

                            valueNew = replacementValue;
                        }
                        break;
                }
            }

            internal bool TryGetMostRecentChangeAtTime(long elapsedTicksAtChange, out GONetSyncableValue value)
            {
                for (int i = 0; i < mostRecentChanges_usedSize; ++i)
                {
                    var item = mostRecentChanges[i];
                    if (item.elapsedTicksAtChange == elapsedTicksAtChange)
                    {
                        value = item.numericValue;
                        return true;
                    }
                }

                value = default;
                return false;
            }

            long lastLogBufferContentsTicks;

            private void LogBufferContentsIfAppropriate(float onlyEverySeconds = 0.01f, bool isFullRequired = false)
            {
                if ((!isFullRequired || mostRecentChanges_usedSize == mostRecentChanges_capacitySize) && 
                    (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastLogBufferContentsTicks).TotalSeconds > onlyEverySeconds))
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
            /// <para>Loop through the recent changes to interpolate or extrapolate if possible.</para>
            /// <para>POST: The related/associated value is updated to what is believed to be the current value based on recent changes accumulated from owner/source.</para>
            /// </summary>
            internal void ApplyValueBlending_IfAppropriate(long useBufferLeadTicks)
            {
                if (syncCompanion.gonetParticipant.IsMine)
                {
                    return;
                }

                //TODO FIX ME Revisit
                /*Since an IsMine_ToRemotelyControl entity is going to be controlled by the server based on the client inputs we don't want to interpolate this entity but extrapolate it.
                  If we interpolate it, not only will we be adding at least a visual lag equal to RTT ms but also an additional useBufferLeadTicks ms from the interpolation buffer.
                  This can make the entity feel really unresponsive. However, if the user only trust extrapolation, although the visual lag is not going to be that much, the behaviour
                  could feel glitchy based on the issues that extrapolation techniques bring to the table.*/
                if (syncCompanion.gonetParticipant.IsMine_ToRemotelyControl)
                {
                    useBufferLeadTicks = 0;
                }

                //GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter.ShouldLog = syncCompanion.gonetParticipant.GetComponent<PlayerController>();
                GONetSyncableValue blendedValue;
                if (ValueBlendUtils.TryGetBlendedValue(this, Time.ElapsedTicks - useBufferLeadTicks, out blendedValue, out bool didExtrapolate))
                {
                    // we do not want to apply extrapolated values if an at rest command is awaiting processing since it is likely that 
                    // the extrapolation occurred due to lack of information coming from owner since it is at rest.
                    if (!hasAwaitingAtRest || !didExtrapolate)
                    {
                        syncCompanion.SetAutoMagicalSyncValue(index, blendedValue);

                        //if (hasAwaitingAtRest)
                        //{
                            //float lerp = (Time.ElapsedTicks - useBufferLeadTicks - hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks) / (float)(hasAwaitingAtRest_assumedInitialRestElapsedTicks - hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks);
                            //if (lerp < 0) lerp = 0;
                            //if (lerp > 1) lerp = 1;
                            //GONetLog.Debug($"sync[{index}] recv({TimeSpan.FromTicks(hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks).TotalSeconds}) lerp:{lerp} until({TimeSpan.FromTicks(hasAwaitingAtRest_assumedInitialRestElapsedTicks).TotalSeconds})  was blending at time: {TimeSpan.FromTicks(Time.ElapsedTicks - useBufferLeadTicks).TotalSeconds}");
                            //GONetLog.Debug($"sync[{index}] current:({syncCompanion.GetAutoMagicalSyncValue(index)}) rest:({hasAwaitingAtRest_value})");
                        //}
                    }
                    //else GONetLog.Debug("hasAwaitingAtRest && didExtrapolate -- skipping auto magical value set.  index: " + index);
                    //else //if (hasAwaitingAtRest)
                    //{
                        //float lerp = (Time.ElapsedTicks - useBufferLeadTicks - hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks) / (float)(hasAwaitingAtRest_assumedInitialRestElapsedTicks - hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks);
                        //if (lerp < 0) lerp = 0;
                        //if (lerp > 1) lerp = 1;
                        //GONetLog.Debug($"will not change! sync[{index}] didExtrapolate:{didExtrapolate} recv({TimeSpan.FromTicks(hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks).TotalSeconds}) lerp:{lerp} until({TimeSpan.FromTicks(hasAwaitingAtRest_assumedInitialRestElapsedTicks).TotalSeconds})  was blending at time: {TimeSpan.FromTicks(Time.ElapsedTicks - useBufferLeadTicks).TotalSeconds}");
                        //GONetLog.Debug($"sync[{index}] current:({syncCompanion.GetAutoMagicalSyncValue(index)}) rest:({hasAwaitingAtRest_value})");
                    //}
                }
                //if (Input.GetKeyDown(KeyCode.L)) GONetLog.Append_FlushDebug("**************************************************   something strange happened \n");
                //GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter.ShouldLog = false;
            }

            /// <summary>
            /// At time of writing, the only case for this is when transferring ownership of client owned thing over to server ownership and on server there will no longer be value blending as it will be the owner/source for others
            /// </summary>
            internal void ClearMostRecentChanges()
            {
                //GONetLog.Debug("Clearing most recent changes...gonetId: " + syncCompanion.gonetParticipant.GONetId + " index: " + index + "\nbuffer:\n" + GetMostRecentChangesString());
                mostRecentChanges_usedSize = 0; // TODO there really may need to be some more housekeeping to do here, but this is functional.
            }

            private string GetMostRecentChangesString()
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < mostRecentChanges_usedSize; ++i)
                {
                    sb.Append("[").Append(i).Append("], timeAtChange: ").Append(TimeSpan.FromTicks(mostRecentChanges[i].elapsedTicksAtChange).TotalSeconds);
                    sb.Append(" value: ").Append( mostRecentChanges[i].numericValue).AppendLine();
                }
                return sb.ToString();
            }

            internal bool TryGetBlendedValue(long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
            {
                return syncCompanion.TryGetBlendedValue(index, mostRecentChanges, mostRecentChanges_usedSize, atElapsedTicks, out blendedValue, out didExtrapolate);
            }
        }

        /// <summary>
        /// Only (re)used in <see cref="OnEnable_StartMonitoringForAutoMagicalNetworking"/>.
        /// </summary>
        static readonly HashSet<SyncBundleUniqueGrouping> uniqueSyncGroupings = new HashSet<SyncBundleUniqueGrouping>();

        internal struct SyncBundleUniqueGrouping : IEquatable<SyncBundleUniqueGrouping>
        {
            /// <summary>
            /// How many seconds between each scheduled call?
            /// </summary>
            internal readonly float scheduleFrequency;
            /// <summary>
            /// How many times a second is the scheduled frequency?
            /// </summary>
            internal readonly short scheduleFrequencyHz;
            internal readonly AutoMagicalSyncReliability reliability;
            internal readonly bool mustRunOnUnityMainThread;

            internal SyncBundleUniqueGrouping(float scheduleFrequency, AutoMagicalSyncReliability reliability, bool mustRunOnUnityMainThread)
            {
                this.scheduleFrequency = scheduleFrequency;

                float v = 1.0f / scheduleFrequency;
                scheduleFrequencyHz = (short)(v + 0.5f);

                this.reliability = reliability;
                this.mustRunOnUnityMainThread = mustRunOnUnityMainThread;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SyncBundleUniqueGrouping))
                {
                    return false;
                }

                var other = (SyncBundleUniqueGrouping)obj;
                return scheduleFrequency == other.scheduleFrequency &&
                       reliability == other.reliability &&
                       mustRunOnUnityMainThread == other.mustRunOnUnityMainThread;
            }

            public bool Equals(SyncBundleUniqueGrouping other)
            {
                return scheduleFrequency == other.scheduleFrequency &&
                       reliability == other.reliability &&
                       mustRunOnUnityMainThread == other.mustRunOnUnityMainThread;
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

                    gonetParticipant.GONetIdAtInstantiationChanged += OnGONetIdAtInstantiationChanged_DoSomeMapMaintenanceForKeyLookupPerformanceLater;

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
                            autoSyncProcessingSupport.AboutToProcess += AutoSyncProcessingSupport_AboutToProcess;
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
                PublishEventAsSoonAsSufficientInfoAvailable(enableEvent, gonetParticipant);

                //const string INSTANTIATE = "GNP Enabled go.name: ";
                //const string ID = " gonetId: ";
                //GONetLog.Debug(string.Concat(INSTANTIATE, gonetParticipant.gameObject.name, ID + gonetParticipant.GONetId));
            }
        }

        private static readonly Dictionary<SyncBundleUniqueGrouping, long> autoSyncUniqueGroupingToLastElapsedTicks =
            new Dictionary<SyncBundleUniqueGrouping, long>();

        private static void AutoSyncProcessingSupport_AboutToProcess(in SyncBundleUniqueGrouping uniqueGrouping, long elapsedTicks)
        {
            if (!autoSyncUniqueGroupingToLastElapsedTicks.TryGetValue(uniqueGrouping, out long uniqueElapsedTicks_previous))
            {
                uniqueElapsedTicks_previous = elapsedTicks;
            }

            double uniqueElapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
            double uniqueDeltaSeconds = TimeSpan.FromTicks(elapsedTicks - uniqueElapsedTicks_previous).TotalSeconds;

            { // account for some tick receivers adding or removing during a call to tick, which must avoid updating collection while enumerating it
                foreach (var tickReceiver in tickReceivers_awaitingAdd)
                {
                    tickReceivers.Add(tickReceiver);
                }
                tickReceivers_awaitingAdd.Clear();
                foreach (var tickReceiver in tickReceivers_awaitingRemove)
                {
                    tickReceivers.Remove(tickReceiver);
                }
                tickReceivers_awaitingRemove.Clear();
            }

            foreach (var tickReceiver in tickReceivers)
            {
                tickReceiver.Tick(uniqueGrouping.scheduleFrequencyHz, uniqueElapsedSeconds, uniqueDeltaSeconds);
            }

            autoSyncUniqueGroupingToLastElapsedTicks[uniqueGrouping] = elapsedTicks;
        }

        /// <summary>
        /// auto-magical sync related housekeeping....essentially populating a shadow map that uses a different key that was not available with correct value when the first map was created
        /// </summary>
        private static void OnGONetIdAtInstantiationChanged_DoSomeMapMaintenanceForKeyLookupPerformanceLater(GONetParticipant gonetParticipant)
        {
            Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions_uintKeyForPerformance;
            if (!activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions_uintKeyForPerformance))
            {
                autoSyncCompanions_uintKeyForPerformance = new Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance[gonetParticipant.codeGenerationId] = autoSyncCompanions_uintKeyForPerformance; // NOTE: This is the only place we add to the outer dictionary and this is always run in the main unity thread, THEREFORE no need for Concurrent....just on the inner ones
            }

            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
            autoSyncCompanions_uintKeyForPerformance[gonetParticipant.GONetIdAtInstantiation] = autoSyncCompanions[gonetParticipant]; // NOTE: This is the only place where the inner dictionary is added to and is ensured to run on unity main thread since OnEnable, so no need for concurrency as long as we can say the same about removes
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
                if (IsClientVsServerStatusKnown)
                {
                    Start_AutoPropogateInstantiation_IfAppropriate_INTERNAL(gonetParticipant);
                }
                else
                {
                    GlobalSessionContext_Participant.StartCoroutine(AutoPropogateInstantiation_WhenAppropriate(gonetParticipant));
                }
            }
        }

        private static IEnumerator AutoPropogateInstantiation_WhenAppropriate(GONetParticipant gonetParticipant)
        {
            while (!IsClientVsServerStatusKnown)
            {
                yield return null;
            }

            Start_AutoPropogateInstantiation_IfAppropriate_INTERNAL(gonetParticipant);
        }

        private static void Start_AutoPropogateInstantiation_IfAppropriate_INTERNAL(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug($"Start GNP...was defined in scene? {WasDefinedInScene(gonetParticipant)}...name: {gonetParticipant.gameObject.name} instanceId: {gonetParticipant.GetInstanceID()}");

            if (WasDefinedInScene(gonetParticipant))
            {
                if (IsServer) // stuff defined in the scene will be owned by the server and therefore needs to be assigned a GONetId by server
                {
                    AssignGONetIdRaw_IfAppropriate(gonetParticipant);
                }
            }
            else
            {
                bool isThisCondisideredTheMomentOfInitialInstantiation = !remoteSpawns_avoidAutoPropagateSupport.Contains(gonetParticipant);
                if (isThisCondisideredTheMomentOfInitialInstantiation)
                {
                    if (IsClient && GONetSpawnSupport_Runtime.IsMarkedToBeRemotelyControlled(gonetParticipant))
                    {
                        Client_DoAutoPropogateInstantiationPrep_RemotelyControlled(gonetParticipant);
                    }
                    else
                    {
                        gonetParticipant.OwnerAuthorityId = MyAuthorityId; // With the flow of methods and such, this looks like the first point in time we know to set this to my authority id
                    }

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
            PublishEventAsSoonAsSufficientInfoAvailable(startEvent, gonetParticipant);
        }

        /// <summary>
        /// PRE: Already known that <paramref name="gonetParticipant"/> has <see cref="GONetParticipant.IsMine_ToRemotelyControl"/> true.
        /// PRE: <see cref="MyAuthorityId"/> is set to final value and is not <see cref="OwnerAuthorityId_Unset"/> in case it is needed as a fallback (i.e., when not enough values in id batch from server).
        /// 
        /// TODO: look into calling this method inside of <see cref="Client_InstantiateToBeRemotelyControlledByMe(GONetParticipant, Vector3, Quaternion)"/> instead of where it is called from now...this would allow for the final GONetId to be set/known immediately!
        /// </summary>
        private static void Client_DoAutoPropogateInstantiationPrep_RemotelyControlled(GONetParticipant gonetParticipant)
        {
            uint nextRemoteControlIdFromServer = client_lastServerGONetIdRawForRemoteControl == GONetParticipant.GONetIdRaw_Unset ? GONetParticipant.GONetIdRaw_Unset : client_lastServerGONetIdRawForRemoteControl + 1;
            int count = client_finalServerGONetIdForRemoteControl_batchStartValues.Count;
            for (int i = 0; i < count; ++i)
            {
                uint min_inclusive = client_finalServerGONetIdForRemoteControl_batchStartValues[i];
                if (nextRemoteControlIdFromServer == GONetParticipant.GONetIdRaw_Unset)
                {
                    nextRemoteControlIdFromServer = min_inclusive;
                }
                else
                {
                    uint max_exclusive = min_inclusive + CLIENT_FINAL_SERVER_GONETID_BATCH_SIZE;
                    if (nextRemoteControlIdFromServer >= min_inclusive && nextRemoteControlIdFromServer < max_exclusive)
                    {
                        // good to go within the limits/range of this batch, nothing left to do/look for here
                        break;
                    }
                    else if (nextRemoteControlIdFromServer == max_exclusive) // if just/already assigned end of range of this batch
                    {
                        // reset next and mark this batch for removal since we used all the items inside it at this point
                        client_finalServerGONetIdForRemoteControl_batchStartValues_removeIndexStack.Push(i);
                        nextRemoteControlIdFromServer = GONetParticipant.GONetIdRaw_Unset;
                    }
                    else
                    {
                        // this condition should not be possible
                        GONetLog.Error("Not possible....guess it was.  oops....figure this one out!");
                    }
                }
            }

            count = client_finalServerGONetIdForRemoteControl_batchStartValues_removeIndexStack.Count;
            for (int i = 0; i < count; ++i)
            {
                int index = client_finalServerGONetIdForRemoteControl_batchStartValues_removeIndexStack.Pop();
                client_finalServerGONetIdForRemoteControl_batchStartValues.RemoveAt(index);
            }

            if (nextRemoteControlIdFromServer != GONetParticipant.GONetIdRaw_Unset)
            {
                gonetParticipant.OwnerAuthorityId = OwnerAuthorityId_Server;
                client_lastServerGONetIdRawForRemoteControl = nextRemoteControlIdFromServer;
            }
            else
            {
                //GONetLog.Warning($"Client instantiating something to remotely control, but no enough server assigned GONetIdRaw values to use.  Will default to client owned initially and auto-switch to server once server side.");
                gonetParticipant.OwnerAuthorityId = MyAuthorityId; // With the flow of methods and such, this looks like the first point in time we know to set this to my authority id
                client_lastServerGONetIdRawForRemoteControl = GONetParticipant.GONetIdRaw_Unset;
            }
        }

        /// <summary>
        /// PRE: <paramref name="event"/> must also implement <see cref="IHaveRelatedGONetId"/>.
        /// Sufficient Info: 
        /// -GONetId has all components (i.e., <see cref="GONetParticipant.DoesGONetIdContainAllComponents()"/>
        /// -if <paramref name="isRelatedLocalContentRequired"/> true, then <see cref="GONetLocal.LookupByAuthorityId"/> for <paramref name="gonetParticipant"/>'s <see cref="GONetParticipant.OwnerAuthorityId"/> is not default
        /// </summary>
        private static void PublishEventAsSoonAsSufficientInfoAvailable(IGONetEvent @event, GONetParticipant gonetParticipant, bool isRelatedLocalContentRequired = false)
        {
            if (!((object)@event is IHaveRelatedGONetId))
            {
                throw new ArgumentException("Argument must an event that implements IHaveRelatedGONetId for this to make any sense and work....the way the event classes/interfaces was implemented causes this unsightly inability to just use IHaveRelatedGONetId as the param type, but do it!", nameof(@event));
            }

            if (gonetParticipant.DoesGONetIdContainAllComponents() && gonetParticipantByGONetIdMap[gonetParticipant.GONetId] == gonetParticipant
                && (!isRelatedLocalContentRequired || GONetLocal.LookupByAuthorityId[gonetParticipant.OwnerAuthorityId] != default))
            {
                //GONetLog.Debug($"publishing event of type: {@event.GetType().Name}");
                EventBus.Publish<IGONetEvent>(@event);
            }
            else
            {
                //GONetLog.Debug($"MAYBE publish later once all info avail...event of type: {@event.GetType().Name}");
                GlobalSessionContext_Participant.StartCoroutine(PublishEventAsSoonAsGONetIdAssigned_Coroutine(@event, gonetParticipant, isRelatedLocalContentRequired));
            }
        }

        /// <summary>
        /// PRE: <paramref name="event"/> must also implement <see cref="IHaveRelatedGONetId"/>.
        /// This method should only ever be called on a client and as a result of having an event ready to go (e.g., <see cref="GONetParticipantStartedEvent"/> or <see cref="GONetParticipantEnabledEvent"/>)
        /// but since the associated <see cref="GONetParticipant"/> was defined in a unity scene and since the server will assign its <see cref="GONetParticipant.GONetId"/> and this client
        /// will get it momentarily after this initialization causing this event to be raised is processed...we need a mechanism to postpone the event publish until gonetid assigned so the
        /// event publish process of placing into an envelope with a reference to the actual GNP will find the GNP since the proper gonetid is known.
        /// </summary>
        private static IEnumerator PublishEventAsSoonAsGONetIdAssigned_Coroutine(IGONetEvent @event, GONetParticipant gonetParticipant, bool isRelatedLocalContentRequired = true)
        {
            // TODO [PERF] don't create a coroutine per event like this...just throw in a collection and check/process on a frequency elsewhere
            GONetParticipant mappedGNP;
            while (
                !gonetParticipant.DoesGONetIdContainAllComponents() || 
                !gonetParticipantByGONetIdMap.TryGetValue(gonetParticipant.GONetId, out mappedGNP) || 
                mappedGNP != gonetParticipant ||
                (isRelatedLocalContentRequired && GONetLocal.LookupByAuthorityId[gonetParticipant.OwnerAuthorityId] == default))
            {
                //GONetLog.Debug($"still waiting for all info to publish event of type: {@event.GetType().Name}.  gnp.idAll? {gonetParticipant.DoesGONetIdContainAllComponents()} key? {gonetParticipantByGONetIdMap.ContainsKey(gonetParticipant.GONetId)} req? {isRelatedLocalContentRequired}");
                yield return null;
            }

            //GONetLog.Debug($"done waiting for all info to publish event of type: {@event.GetType().Name} gonetId: {gonetParticipant.GONetId}");
            ((IHaveRelatedGONetId)@event).GONetId = gonetParticipant.GONetId;
            EventBus.Publish<IGONetEvent>(@event);
        }

        private static void AssignGONetIdRaw_IfAppropriate(GONetParticipant gonetParticipant, bool shouldForceChangeEventIfAlreadySet = false)
        {
            if (shouldForceChangeEventIfAlreadySet || gonetParticipant.gonetId_raw == GONetParticipant.GONetId_Unset) // TODO need to avoid this when this guy is coming from replay too! gonetParticipant.WasInstantiated true is all we have now...will have WasFromReplay later
            {
                if (lastAssignedGONetIdRaw < GONetParticipant.GONetId_Raw_MaxValue)
                {
                    uint gonetId_raw = GetNextAvailableGONetIdRaw(gonetParticipant);
                    gonetParticipant.GONetId = (gonetId_raw << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED) | gonetParticipant.OwnerAuthorityId;
                }
                else
                {
                    throw new OverflowException("Unable to assign a new GONetId, because lastAssignedGONetId has reached the max value of GONetParticipant.GONetId_Raw_MaxValue, which is: " + GONetParticipant.GONetId_Raw_MaxValue);
                }
            }
        }

        private static uint GetNextAvailableGONetIdRaw(GONetParticipant gonetParticipant)
        {
            ++lastAssignedGONetIdRaw;
            
            if (IsServer)
            {
                int count = client_finalServerGONetIdForRemoteControl_batchStartValues.Count;
                for (int i = 0; i < count; ++i)
                {
                    uint min_inclusive = client_finalServerGONetIdForRemoteControl_batchStartValues[i];
                    uint max_exclusive = min_inclusive + CLIENT_FINAL_SERVER_GONETID_BATCH_SIZE;
                    bool isNewValueInBatch = lastAssignedGONetIdRaw >= min_inclusive && lastAssignedGONetIdRaw < max_exclusive;
                    if (isNewValueInBatch)
                    {
                        // if it was in a batch set it to the value just after the current batch
                        lastAssignedGONetIdRaw = max_exclusive;
                    }
                }
            }
            else
            {
                bool isForRemotelyControlledOnClient = IsClient && gonetParticipant.OwnerAuthorityId == OwnerAuthorityId_Server;
                if (isForRemotelyControlledOnClient && client_lastServerGONetIdRawForRemoteControl != GONetParticipant.GONetIdRaw_Unset)
                {
                    --lastAssignedGONetIdRaw; // undo the now unwanted action we did above in method
                    return client_lastServerGONetIdRawForRemoteControl;
                }
            }

            return lastAssignedGONetIdRaw;
        }

        private static void AutoPropagateInitialInstantiation(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event;

            string nonAuthorityDesignTimeLocation;
            if (GONetSpawnSupport_Runtime.TryGetNonAuthorityDesignTimeLocation(gonetParticipant, out nonAuthorityDesignTimeLocation))
            {
                @event = InstantiateGONetParticipantEvent.Create_WithNonAuthorityInfo(gonetParticipant, nonAuthorityDesignTimeLocation);
            }
            else if (GONetSpawnSupport_Runtime.IsMarkedToBeRemotelyControlled(gonetParticipant))
            {
                @event = InstantiateGONetParticipantEvent.Create_WithRemotelyControlledByInfo(gonetParticipant);
            }
            else
            {
                @event = InstantiateGONetParticipantEvent.Create(gonetParticipant);
            }

            //GONetLog.Debug($"Publish InstantiateGONetParticipantEvent now. gonetId: {gonetParticipant.GONetId}"); /////////////////////////// DREETS!
            EventBus.Publish(@event); // this causes the auto propagation via local handler to send to all remotes (i.e., all clients if server, server if client)

            gonetParticipant.IsOKToStartAutoMagicalProcessing = true; // VERY IMPORTANT that this comes AFTER publishing the event so the flood gates to start syncing data come AFTER other parties are made aware of the GNP in the above event!
        }

        internal static void OnDestroy_AutoPropagateRemoval_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying)
            {
                if (IsMine(gonetParticipant) || (IsServer && !Server_IsClientOwnerConnected(gonetParticipant)))
                {
                    AutoPropagateInitialDestroy(gonetParticipant);
                }
                else
                {
                    bool isExpected =
                        gonetIdsDestroyedViaPropagation.Contains(gonetParticipant.GONetId) ||
                        (IsClient && IsApplicationQuitting);

                    if (!isExpected)
                    {
                        const string NOD = "GONetParticipant being destroyed and IsMine is false, which means the only other GONet-approved reason this should be destroyed is through automatic propagation over the network as a response to the owner destroying it OR a client just closed out; HOWEVER, that is not the case right now and the ASSumption is that you inadvertantly called UnityEngine.Object.Destroy() on something not owned by you.  GONetId: ";
                        GONetLog.Warning(string.Concat(NOD, gonetParticipant.GONetId));
                    }
                }
            }
        }

        public static bool Server_IsClientOwnerConnected(GONetParticipant gonetParticipant)
        {
            return gonetServer.TryGetRemoteClientByAuthorityId(gonetParticipant.OwnerAuthorityId, out var gonetRemoteClient);
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
            gonetParticipantsInLevel.ForEach(gonetParticipant => {
                definedInSceneParticipantInstanceIDs.Add(gonetParticipant.GetInstanceID());
                //GONetLog.Debug($" recording GNP defined in scene...go.Name: {gonetParticipant.gameObject.name} instanceId: {gonetParticipant.GetInstanceID()}");
            });
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
                if (bytesUsedCount > SerializationUtils.MTU)
                {
                    GONetLog.Warning(string.Concat("Late joiner, here's how many bytes of automagical sync data I'm sending your way: ", bytesUsedCount));
                    // TODO break into smaller packets!!!
                }
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
        /// For every unique combination encountered of the following values: 
        ///     <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>, 
        ///     <see cref="GONetAutoMagicalSyncAttribute.MustRunOnUnityMainThread"/> and 
        ///     <see cref="GONetAutoMagicalSyncAttribute.Reliability"/> (i.e., as encapsulated in <see cref="SyncBundleUniqueGrouping"/>), 
        /// an instance of this class will be created and used to process only those fields/properties set to be sync'd on that frequency.
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
            long lastScheduledProcessAtTicks;

            static readonly long END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS = TimeSpan.FromSeconds(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS).Ticks;

            internal delegate void ProcessContext(in SyncBundleUniqueGrouping uniqueGrouping, long elapsedTicks);
            internal event ProcessContext AboutToProcess;

            SyncBundleUniqueGrouping uniqueGrouping;
            long scheduleFrequencyTicks;
            Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency;
            QosType uniqueGrouping_qualityOfService;
            GONetChannelId uniqueGrouping_valueChanges_channelId;
            GONetChannelId uniqueGrouping_valuesNowAtRest_channelId;

            /// <summary>
            /// Indicates whether or not <see cref="ProcessASAP"/> must be called (manually) from an outside part in order for sync processing to occur.
            /// </summary>
            internal bool DoesRequireManualProcessInitiation => scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS || !isSetupToRunInSeparateThread;

            /// <summary>
            /// Just a helper data structure just for use in <see cref="ProcessAutoMagicalSyncStuffs(bool, ReliableEndpoint)"/>
            /// </summary>
            readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);

            readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> valuesNowAtRestToBroadcast = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);
            bool signalNeedToResetAtRest_untilBetterWayToDealWithThisSituation;

            readonly ArrayPool<byte> myThread_valueChangeSerializationArrayPool;

            readonly SecretaryOfTemporalAffairs myThread_Time;

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
                uniqueGrouping_valueChanges_channelId = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? GONetChannel.AutoMagicalSync_Reliable : GONetChannel.AutoMagicalSync_Unreliable;
                uniqueGrouping_valuesNowAtRest_channelId = GONetChannel.AutoMagicalSync_ValuesNowAtRest_Reliable;

                this.everythingMap_evenStuffNotOnThisScheduleFrequency = everythingMap_evenStuffNotOnThisScheduleFrequency;

                Time.TimeSetFromAuthority += Time_TimeSetFromAuthority;

                isSetupToRunInSeparateThread = !uniqueGrouping.mustRunOnUnityMainThread;
                if (isSetupToRunInSeparateThread)
                {
                    myThread_Time = new SecretaryOfTemporalAffairs(GONetMain.Time); // since not running on main thread, we need to use a new/separate instance to avoid cross thread access conflicts

                    thread = new Thread(ContinuallyProcess_NotMainThread);
                    thread.Name = string.Concat("GONet Auto-magical Sync - ", Enum.GetName(typeof(AutoMagicalSyncReliability), uniqueGrouping.reliability), " Freq: ", uniqueGrouping.scheduleFrequency);
                    thread.Priority = System.Threading.ThreadPriority.AboveNormal;
                    thread.IsBackground = true; // do not prevent process from exiting when foreground thread(s) end

                    events_AwaitingSendToOthersQueue_ByThreadMap[thread] = new Queue<IGONetEvent>(100); // we're on main thread, safe to deal with regular dict here
                    events_SendToOthersQueue_ByThreadMap[thread] = new ConcurrentQueue<IGONetEvent>(); // we're on main thread, safe to deal with regular dict here

                    isThreadRunning = true;
                    thread.Start();
                }
                else
                {
                    myThread_Time = Time; // if running on main thread, no need to use a different instance that will already be used on the main thread

                    if (!events_AwaitingSendToOthersQueue_ByThreadMap.ContainsKey(Thread.CurrentThread))
                    {
                        events_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread] = new Queue<IGONetEvent>(100); // we're on main thread, safe to deal with regular dict here
                        events_SendToOthersQueue_ByThreadMap[Thread.CurrentThread] = new ConcurrentQueue<IGONetEvent>(); // we're on main thread, safe to deal with regular dict here
                    }
                }

            }

            private void Time_TimeSetFromAuthority(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks)
            {
                if (myThread_Time != Time) // avoid SetFromAuthority if the local time instance is the same as GONetMain instance since it will be already handled/set
                {
                    myThread_Time.SetFromAuthority(toElapsedTicks);
                }
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
                        lastScheduledProcessAtTicks = HighResolutionTimeUtils.UtcNow.Ticks;
                        Process();
                        shouldProcessInSeparateThreadASAP = false; // reset this

                        if (!doesRequireManualProcessInitiation)
                        { // (auto sync) frequency control:
                            long nowTicks = HighResolutionTimeUtils.UtcNow.Ticks;
                            long ticksToSleep = scheduleFrequencyTicks - (nowTicks - lastScheduledProcessAtTicks);
                            if (ticksToSleep > 0)
                            {
                                Thread.Sleep(TimeSpan.FromTicks(ticksToSleep));
                                //GONetLog.Debug("sleep ticks: " + ticksToSleep);
                            }
                            else
                            {
                                //GONetLog.Debug("scheduleFrequencyTicks: " + scheduleFrequencyTicks + ", sleep ticks: " + ticksToSleep);
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
                //long startTicks = HighResolutionTimeUtils.UtcNow.Ticks;
                int bundleFragmentsMadeCount = 0;

                try
                {
                    if (myThread_Time != Time) // avoid updating time if the local time instance is the same as GONetMain instance since it will be updated already
                    {
                        myThread_Time.Update();
                    }
                    long myTicks = myThread_Time.ElapsedTicks;

                    AboutToProcess?.Invoke(uniqueGrouping, myTicks);

                    // loop over everythingMap_evenStuffNotOnThisScheduleFrequency only processing the items inside that match scheduleFrequency
                    syncValuesToSend.Clear();
                    valuesNowAtRestToBroadcast.Clear();

                    using (var enumeratorOuter = everythingMap_evenStuffNotOnThisScheduleFrequency.GetEnumerator())
                    {
                        while (enumeratorOuter.MoveNext())
                        {
                            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                            if (currentMap == null)
                            {
                                GONetLog.Error("currentMap == null");
                            }
                            using (var enumeratorInner = currentMap.GetEnumerator())
                            {
                                while (enumeratorInner.MoveNext())
                                {
                                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;
                                    if (monitoringSupport == null)
                                    {
                                        GONetLog.Error("monitoringSupport == null");
                                    }

                                    if (signalNeedToResetAtRest_untilBetterWayToDealWithThisSituation)
                                    {
                                        monitoringSupport.ResetAtRestValues(uniqueGrouping);
                                    }

                                    // need to call this for every single one to keep track of changes, BUT we only want to consider/process ones that match the current frequency:
                                    monitoringSupport.UpdateLastKnownValues(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    if (monitoringSupport.HaveAnyValuesChangedSinceLastCheck_AppendNewlyAtRest(uniqueGrouping, myTicks, valuesNowAtRestToBroadcast)) // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    {
                                        monitoringSupport.AnnotateMyBaselineValuesNeedingAdjustment();
                                        monitoringSupport.AppendListWithChangesSinceLastCheck(syncValuesToSend, uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                        monitoringSupport.OnValueChangeCheck_Reset(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                                    }
                                }
                            }
                        }
                    }

                    bundleFragmentsMadeCount += SendSyncValueBundlesToRelevantParties_IfAppropriate(syncValuesToSend, myTicks, typeof(AutoMagicalSync_ValueChanges_Message));
                    bundleFragmentsMadeCount += SendSyncValueBundlesToRelevantParties_IfAppropriate(valuesNowAtRestToBroadcast, myTicks, typeof(AutoMagicalSync_ValuesNowAtRest_Message));

                    { // all this to call ApplyAnnotatedBaselineValueAdjustments()
                        Queue<IGONetEvent> baselineAdjustmentsEventQueue = events_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread];
                        using (var enumeratorOuter = everythingMap_evenStuffNotOnThisScheduleFrequency.GetEnumerator())
                        {
                            while (enumeratorOuter.MoveNext())
                            {
                                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                                if (currentMap == null)
                                {
                                    GONetLog.Error("currentMap == null");
                                }

                                using (var enumeratorInner = currentMap.GetEnumerator())
                                {
                                    while (enumeratorInner.MoveNext())
                                    {
                                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;
                                        if (monitoringSupport == null)
                                        {
                                            GONetLog.Error("monitoringSupport == null");
                                        }

                                        monitoringSupport.ApplyAnnotatedBaselineValueAdjustments(baselineAdjustmentsEventQueue); // we figured out when this needs to get called....and it is now, AFTER the send of the changes accumulated herein to avoid using new baseline incorrectly 
                                    }
                                }
                            }
                        }
                    }

                    PublishEvents_SyncValueChangesSentToOthers_ASAP();
                    signalNeedToResetAtRest_untilBetterWayToDealWithThisSituation = false;
                }
                catch (InvalidOperationException ioe)
                {
                    const string ENUMERATION_WHILE_MODOFYING = "Collection was modified; enumeration operation may not execute.";
                    bool willWeASSumeThisIsExpected = !uniqueGrouping.mustRunOnUnityMainThread && ioe.Message == ENUMERATION_WHILE_MODOFYING; // TODO need to add in a clause for only happening early on during a new GNP cycle this could happen when running in separate thread (ie., not unity main thread)
                    if (willWeASSumeThisIsExpected)
                    {
                        const string SEMI = "Semi-expected error attempting to process auto-magical syncs on separate thread (i.e., not unity main thread).  It is only expected when a new GNP is being processed and some internal Dictionary is updated in main thread while we are processing here in this separate thread, in fact a bit prematurely.";
                        GONetLog.Warning(string.Concat(SEMI));

                        signalNeedToResetAtRest_untilBetterWayToDealWithThisSituation = true; // we want to reset any stuff marked as at rest above so it does not get stuck in bad state, but since we are already in a bad enumeration state not going to attempt an enumeration over what is likely the same data here...try it later
                    }
                    else
                    {
                        GONetLog.Error(string.Concat("Unexpected error attempting to process auto-magical syncs.  Exception.Type: ", ioe.GetType().Name, " Exception.Message: ", ioe.Message, " \nException.StackTrace: ", ioe.StackTrace));
                    }
                }
                catch (Exception e)
                {
                    GONetLog.Error(string.Concat("Unexpected error attempting to process auto-magical syncs.  Exception.Type: ", e.GetType().Name, " Exception.Message: ", e.Message, " \nException.StackTrace: ", e.StackTrace));
                }

                /*
                if (bundleFragmentsMadeCount > 0)
                {
                    long endTicks = HighResolutionTimeUtils.UtcNow.Ticks;
                    GONetLog.Debug("[DREETS] bundleFragmentsMadeCount: " + bundleFragmentsMadeCount + " duration(ms): " + TimeSpan.FromTicks(endTicks - startTicks).TotalMilliseconds);
                }
                */
            }

            /// <returns>The number of bundle fragments made</returns>
            private int SendSyncValueBundlesToRelevantParties_IfAppropriate(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesForBundles, long relatedElapsedTicks, Type chosenBundleType)
            {
                int bundleFragmentsMadeCount = 0;
                int count = syncValuesForBundles.Count;
                if (count > 0)
                {
                    GONetChannelId useThisChannelId = chosenBundleType == typeof(AutoMagicalSync_ValueChanges_Message) ? uniqueGrouping_valueChanges_channelId : uniqueGrouping_valuesNowAtRest_channelId;  // TODO this is fairly hardcoded and limited in terms of options, but right now this is all...and need to just move on to test how it will work before making this more configurable

                    //GONetLog.Debug("sending changed auto-magical sync values to all connections");
                    if (IsServer)
                    {
                        // if its the server, we have to consider who we are sending to and ensure we do not send then changes that initially came from them!
                        if (_gonetServer != null)
                        {
                            WholeBundleOfChoiceFragments bundleFragments;

                            // Only send out changes I own as server (i.e., passing in MyAuthorityId for filter).
                            // Clients will get other clients' changes they own from server auto-forward elsewhere
                            //  (see ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL-Server_SendBytesToNonSourceClients).
                            SerializeWhole_BundleOfChoice(syncValuesForBundles, myThread_valueChangeSerializationArrayPool, MyAuthorityId, relatedElapsedTicks, chosenBundleType, out bundleFragments);

                            for (int iConnection = 0; iConnection < _gonetServer.numConnections; ++iConnection)
                            {
                                GONetConnection_ServerToClient gONetConnection_ServerToClient = _gonetServer.remoteClients[iConnection].ConnectionToClient;
                                for (int iFragment = 0; iFragment < bundleFragments.fragmentCount; ++iFragment)
                                {
                                    //GONetLog.Debug("AutoMagicalSync_ValueChanges_Message sending right after this. bytesUsedCount: " + bundleFragments.fragmentBytesUsedCount[iFragment]);  /////////////////////////// DREETS!
                                    if (_gonetServer.GetRemoteClientByAuthorityId(gONetConnection_ServerToClient.OwnerAuthorityId).IsInitializedWithServer) // only send to client initialized with server!
                                    {
                                        SendBytesToRemoteConnection(gONetConnection_ServerToClient, bundleFragments.fragmentBytes[iFragment], bundleFragments.fragmentBytesUsedCount[iFragment], useThisChannelId);
                                    }
                                }
                            }

                            for (int iFragment = 0; iFragment < bundleFragments.fragmentCount; ++iFragment)
                            {
                                myThread_valueChangeSerializationArrayPool.Return(bundleFragments.fragmentBytes[iFragment]);
                            }

                            bundleFragmentsMadeCount += bundleFragments.fragmentCount;
                        }
                    }
                    else
                    {
                        WholeBundleOfChoiceFragments bundleFragments;
                        if (chosenBundleType == typeof(AutoMagicalSync_ValuesNowAtRest_Message))
                        {
                            SerializeWhole_NowAtRestBundle(syncValuesForBundles, myThread_valueChangeSerializationArrayPool, MyAuthorityId, relatedElapsedTicks, out bundleFragments);
                        }
                        else
                        {
                            SerializeWhole_ChangesBundle(syncValuesForBundles, myThread_valueChangeSerializationArrayPool, MyAuthorityId, relatedElapsedTicks, out bundleFragments);
                        }

                        for (int iFragment = 0; iFragment < bundleFragments.fragmentCount; ++iFragment)
                        {
                            byte[] changesSerialized = bundleFragments.fragmentBytes[iFragment];
                            SendBytesToRemoteConnections(changesSerialized, bundleFragments.fragmentBytesUsedCount[iFragment], useThisChannelId);
                            myThread_valueChangeSerializationArrayPool.Return(changesSerialized);
                        }

                        bundleFragmentsMadeCount += bundleFragments.fragmentCount;
                    }

                    if (chosenBundleType == typeof(AutoMagicalSync_ValuesNowAtRest_Message))
                    {
                        for (int i = 0; i < count; ++i)
                        {
                            AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport = syncValuesForBundles[i];
                            valueMonitoringSupport.syncCompanion.IndicateAtRestBroadcasted(valueMonitoringSupport.index);
                        }
                    }
                }

                return bundleFragmentsMadeCount;
            }

            /// <summary>
            /// Promote Local Thread Events To Main Thread For Publishing since calling <see cref="GONetEventBus.Publish{T}(T, uint?)"/> is not to be called from multiple threads!
            /// </summary>
            private void PublishEvents_SyncValueChangesSentToOthers_ASAP()
            {
                Queue<IGONetEvent> queueAwaiting = events_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread];
                ConcurrentQueue<IGONetEvent> queueSend = events_SendToOthersQueue_ByThreadMap[Thread.CurrentThread];
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
                        long nowTicks = HighResolutionTimeUtils.UtcNow.Ticks;

                        bool isFirstTimeThrough = lastScheduledProcessAtTicks == 0;
                        if (isFirstTimeThrough)
                        {
                            lastScheduledProcessAtTicks = nowTicks; // This value needs an initialization or else Process_ASAP will always processs EVERY frame unintentionally
                        }

                        bool isASAPNow = (nowTicks - lastScheduledProcessAtTicks) > scheduleFrequencyTicks;
                        if (isASAPNow)
                        {
                            Process();
                            lastScheduledProcessAtTicks += scheduleFrequencyTicks;
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
                        .Select(a => a.GetLoadableTypes().Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(IGONetEvent)) && !t.IsAbstract).OrderBy(t2 => t2.FullName)))
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
                UnityEngine.Debug.LogError(ex); // since our log stuffs does not work in static context within unity editor, use unity logging for this one
            }
        }

        /// <summary>
        /// <para>PRE: <paramref name="changes"/> size is greater than 0</para>
        /// <para>PRE: <paramref name="filterUsingOwnerAuthorityId"/> is not <see cref="OwnerAuthorityId_Unset"/> otherwise an exception is thrown</para>
        /// <para>POST: return a serialized packet with only the stuff that excludes <paramref name="filterUsingOwnerAuthorityId"/> as to not send to them (i.e., likely because they are the one who owns this data in the first place and already know this change occurred!)</para>
        /// <para>IMPORTANT: The caller is responsible for returning the returned byte[] to <paramref name="byteArrayPool"/></para>
        /// </summary>
        private static void SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, ArrayPool<byte> byteArrayPool, ushort filterUsingOwnerAuthorityId, long elapsedTicksAtCapture, out WholeBundleOfChoiceFragments bundleFragments)
        {
            SerializeWhole_BundleOfChoice(changes, byteArrayPool, filterUsingOwnerAuthorityId, elapsedTicksAtCapture, typeof(AutoMagicalSync_ValueChanges_Message), out bundleFragments);
        }

        private static void SerializeWhole_NowAtRestBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, ArrayPool<byte> byteArrayPool, ushort filterUsingOwnerAuthorityId, long elapsedTicksAtCapture, out WholeBundleOfChoiceFragments bundleFragments)
        {
            SerializeWhole_BundleOfChoice(changes, byteArrayPool, filterUsingOwnerAuthorityId, elapsedTicksAtCapture, typeof(AutoMagicalSync_ValuesNowAtRest_Message), out bundleFragments);
        }


        internal class WholeBundleOfChoiceFragments
        {
            public const int FRAGMENT_MAX_COUNT = 256;

            internal int fragmentCount;
            internal readonly byte[][] fragmentBytes;
            internal readonly int[] fragmentBytesUsedCount;

            internal WholeBundleOfChoiceFragments()
            {
                fragmentCount = 0;
                fragmentBytes = new byte[FRAGMENT_MAX_COUNT][];
                fragmentBytesUsedCount = new int[FRAGMENT_MAX_COUNT];
            }
        }

        static readonly ConcurrentDictionary<Thread, WholeBundleOfChoiceFragments> wholeBundleOfChoiceBuffersByThread = new ConcurrentDictionary<Thread, WholeBundleOfChoiceFragments>(4, 4);

        /// <param name="filterUsingOwnerAuthorityId">NOTE: pass in <see cref="OwnerAuthorityId_Unset"/> to NOT filter</param>
        private static void SerializeWhole_BundleOfChoice(
            List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, 
            ArrayPool<byte> byteArrayPool, 
            ushort filterUsingOwnerAuthorityId, 
            long elapsedTicksAtCapture, 
            Type chosenBundleType, 
            out WholeBundleOfChoiceFragments bundleFragments)
        {
            if (!wholeBundleOfChoiceBuffersByThread.TryGetValue(Thread.CurrentThread, out bundleFragments))
            {
                wholeBundleOfChoiceBuffersByThread[Thread.CurrentThread] = bundleFragments = new WholeBundleOfChoiceFragments();
            }

            int countTotal = changes.Count;
            int countFiltered = SerializeBody_ChangesBundle_PRE_OrderAndCountFiltered(changes, filterUsingOwnerAuthorityId);
            int individualChangesCountRemaining = countFiltered;
            bundleFragments.fragmentCount = 0;

            if (countFiltered == 0)
            {
                return;
            }

            int lastIndexUsed = 0;

            while (individualChangesCountRemaining > 0)
            {
                using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
                {
                    { // header...just message type/id...well, and now time 
                        uint messageID = messageTypeToMessageIDMap[chosenBundleType];
                        bitStream.WriteUInt(messageID);

                        bitStream.WriteLong(elapsedTicksAtCapture);
                    }

                    // body
                    int changesInBundleCount = SerializeBody_ChangesBundle(changes, bitStream, filterUsingOwnerAuthorityId, ref lastIndexUsed);
                    
                    if (changesInBundleCount > 0)
                    {
                        bitStream.WriteCurrentPartialByte();

                        var byteCount = bitStream.Length_WrittenBytes;
                        bundleFragments.fragmentBytesUsedCount[bundleFragments.fragmentCount] = byteCount;
                        byte[] bytes = byteArrayPool.Borrow(byteCount);
                        Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, byteCount);
                        bundleFragments.fragmentBytes[bundleFragments.fragmentCount] = bytes;

                        individualChangesCountRemaining -= changesInBundleCount;
                        bundleFragments.fragmentCount++;
                    }
                    else
                    {
                        if (individualChangesCountRemaining > 0)
                        {
                            GONetLog.Warning("Why mismatch in remaining expected versus actual.  This could be serious!");
                        }
                        break;
                    }
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
                    if (gonetParticipant.DoesGONetIdContainAllComponents())
                    {
                        GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Serialize(bitStream_headerAlreadyWritten, gonetParticipant, gonetParticipant.GONetId);

                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = current.Value;
                        monitoringSupport.SerializeAll(bitStream_headerAlreadyWritten);
                    }
                    else
                    {
                        GONetLog.Error($"Excluding GNP with partial GONetId: {gonetParticipant.GONetId} from all current values bundle to avoid deserialization/processing issues on the other side.  But WHY!?!?!?!?");
                    }
                }
            }
        }

        /// <summary>
        /// This is to be called only ONCE prior to possible multiple calls to <see cref="SerializeBody_ChangesBundle(List{AutoMagicalSync_ValueMonitoringSupport_ChangedValue}, BitByBitByteArrayBuilder, ushort)"/>
        /// since ordering and filtering count only needs to be done once.
        /// POST: <paramref name="changes"/> are ordered in place
        /// </summary>
        /// <returns>the filtered count of items in <paramref name="changes"/> using <paramref name="filterUsingOwnerAuthorityId"/> as the filtering out criteria</returns>
        /// <param name="filterUsingOwnerAuthorityId">NOTE: pass in <see cref="OwnerAuthorityId_Unset"/> to NOT filter</param>
        private static int SerializeBody_ChangesBundle_PRE_OrderAndCountFiltered(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, ushort filterUsingOwnerAuthorityId)
        {
            int countMinus1 = changes.Count - 1;
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

            return countFiltered;
        }

        /// <summary>
        /// Returns the number of changes actually included in/added to the <paramref name="bitStream_headerAlreadyWritten"/> AFTER any filtering this method does (e.g., checking <paramref name="filterUsingOwnerAuthorityId"/>).
        /// </summary>
        /// <param name="filterUsingOwnerAuthorityId">NOTE: pass in <see cref="OwnerAuthorityId_Unset"/> to NOT filter</param>
        private static int SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten, ushort filterUsingOwnerAuthorityId, ref int lastIndexUsed)
        {
            int countTotal = changes.Count;
            int changesInBundle = 0;

            uint gonetId_previous = 0;
            Queue<IGONetEvent> syncEventQueue = events_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread];
            const int CUTOFF = SerializationUtils.MTU; // TODO we can go higher when compression used
            for (int i = lastIndexUsed; i < countTotal && bitStream_headerAlreadyWritten.Length_WrittenBytes < CUTOFF; ++i) // only keep going on this if under MTU so the bundle does not get turned into fragments inside the low level networking layer...we handle at higher level in gonet now
            {
                AutoMagicalSync_ValueMonitoringSupport_ChangedValue change = changes[i];
                if (!ShouldSendChange(change, filterUsingOwnerAuthorityId))
                {
                    continue; // skip this guy (i.e., apply the "filter")
                }

                lastIndexUsed = i;
                ++changesInBundle;

#if !PERF_NO_PROCESS_SYNC_EVENTS
                syncEventQueue.Enqueue(GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateInstance(SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers, Time.ElapsedTicks, filterUsingOwnerAuthorityId, change.syncCompanion, change.index));
#endif

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

                { // have to write the gonetid first before each changed value
                    //GONetLog.Append(change.syncCompanion.gonetParticipant.GONetIdAtInstantiation + ", ");
                    uint gonetId = change.syncCompanion.gonetParticipant.GONetIdAtInstantiation;

                    long diffFromPrevious = gonetId - gonetId_previous;
                    bool isSameAsPrevious = diffFromPrevious == 0;
                    bitStream_headerAlreadyWritten.WriteBit(isSameAsPrevious);
                    if (isSameAsPrevious)
                    {
                        bool isDiffNegative_mustBeFalseToIndicateNormalFlowToContinueProcessingAsRealData = false;
                        bitStream_headerAlreadyWritten.WriteBit(isDiffNegative_mustBeFalseToIndicateNormalFlowToContinueProcessingAsRealData);
                    }
                    else
                    {
                        bool isDiffNegative = diffFromPrevious < 0;
                        uint gonetId_diff_unsigned = isDiffNegative ? (uint)(-diffFromPrevious) : (uint)diffFromPrevious;

                        uint gonetIdByteCount;
                        if (gonetId_diff_unsigned < 0x1_00_00)
                        {
                            if (gonetId_diff_unsigned < 0x1_00) gonetIdByteCount = 1;
                            else gonetIdByteCount = 2;
                        }
                        else if (gonetId_diff_unsigned < 0x1_00_00_00) gonetIdByteCount = 3;
                        else gonetIdByteCount = 4;

                        bitStream_headerAlreadyWritten.WriteBit(isDiffNegative);
                        bitStream_headerAlreadyWritten.WriteUInt(gonetIdByteCount - 1, 2); // since gonetId usually will take a smaller number of bytes than all 4 allotted, we can save some space like this
                        bitStream_headerAlreadyWritten.WriteUInt(gonetId_diff_unsigned, gonetIdByteCount << 3);
                    }

                    gonetId_previous = gonetId;
                }

                bitStream_headerAlreadyWritten.WriteByte(change.index); // then have to write the index, otherwise other end does not know which index to deserialize
                change.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, change.index);
            }
            //GONetLog.Append_FlushDebug();

            { // indicates end of bundle!  we write regardless of if changes added up top or not...no real harm
                bitStream_headerAlreadyWritten.WriteBit(true);
                bitStream_headerAlreadyWritten.WriteBit(true); // true here for isDiffNegative coming right after true for isSameAsPrevious is all it takes to indicate an impossible normal state, which is the end of the content!
            }

            return changesInBundle;
        }

        /// <param name="filterUsingOwnerAuthorityId">NOTE: pass in <see cref="OwnerAuthorityId_Unset"/> to NOT filter</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldSendChange(AutoMagicalSync_ValueMonitoringSupport_ChangedValue change, ushort filterUsingOwnerAuthorityId)
        {
            return
                change.syncCompanion.gonetParticipant.GONetId != GONetParticipant.GONetId_Unset &&
                (IsServer
                    ? (filterUsingOwnerAuthorityId == OwnerAuthorityId_Unset || // the unset value is now possible to send here to indicate no filtering!
                       (filterUsingOwnerAuthorityId == OwnerAuthorityId_Server && change.syncCompanion.gonetParticipant.OwnerAuthorityId == filterUsingOwnerAuthorityId) || // if it comes from the server itself
                        (filterUsingOwnerAuthorityId != OwnerAuthorityId_Server && _gonetServer.GetRemoteClientByAuthorityId(filterUsingOwnerAuthorityId).IsInitializedWithServer && // only send to a client if that client is considered initialized with the server
                        (change.syncCompanion.gonetParticipant.OwnerAuthorityId != filterUsingOwnerAuthorityId // In most circumstances, the server should send every change except for changes back to the owner itself
                                                                                                               // TODO try to make this work as an option: || IsThisChangeTheMomentOfInception(change)
                            || change.index == GONetParticipant.ASSumed_GONetId_INDEX))) // this is the one exception, if the server is assigning the instantiator/owner its GONetId for the first time, it DOES need to get sent back to itself
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
            int streamPositionBytes_preGonetId;
            while ((streamPositionBytes_preGonetId = bitStream_headerAlreadyRead.Position_Bytes) < bytesUsedCount) // while more data to read/process
            {
                uint gonetId = GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead).System_UInt32;

                if (GONetParticipant.DoesGONetIdContainAllComponents(gonetId))
                {
                    GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId][gonetParticipant];

                    //UnityEngine.Debug.Log($"[DREETS] Deserialize init all for GONetId: {gonetId}");
                    syncCompanion.DeserializeInitAll(bitStream_headerAlreadyRead, elapsedTicksAtSend);
                    
                    PublishEventAsSoonAsSufficientInfoAvailable(
                        new GONetParticipantDeserializeInitAllCompletedEvent(gonetParticipant), 
                        gonetParticipant, 
                        isRelatedLocalContentRequired: true);
                }
                else
                {
                    GONetLog.Error($"Deserialized a gonetId value ({gonetId}) that is not complete, which will cause reading the rest of the values to fail in mysterious ways...so, will STOP deserializing now!  stream.Position_Bytes: (pre:{streamPositionBytes_preGonetId}, post:{bitStream_headerAlreadyRead.Position_Bytes}) bytesUsedCount: {bytesUsedCount}");
                    return;
                }
            }
        }

        /// <summary>
        /// Awaiting to not be unity null and to have an entry in the corresponding entry/map in <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/> for its codeGenerationId.
        /// </summary>
        static readonly List<GONetParticipant> gnpsAwaitingCompanion = new List<GONetParticipant>(1000);

        private static void DeserializeBody_BundleOfChoice(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, GONetConnection sourceOfChangeConnection, GONetChannelId channelId, long elapsedTicksAtSend, Type chosenBundleType)
        {
            //if (chosenBundleType == typeof(AutoMagicalSync_ValuesNowAtRest_Message)) GONetLog.Debug($"remote source sent us at rest bundle.");
            uint gonetId_previous = 0;

            while (true)
            {
                bool isSameAsPrevious;
                bitStream_headerAlreadyRead.ReadBit(out isSameAsPrevious);

                bool isDiffNegative;
                bitStream_headerAlreadyRead.ReadBit(out isDiffNegative);

                uint gonetIdAtInstantiation;

                if (isSameAsPrevious)
                {
                    if (isDiffNegative) // this essentially impossible combination is the signal of end of content!
                    {
                        break;
                    }

                    gonetIdAtInstantiation = gonetId_previous;
                }
                else
                {
                    uint gonetIdByteCount;
                    bitStream_headerAlreadyRead.ReadUInt(out gonetIdByteCount, 2);

                    uint gonetId_diff_unsigned;
                    bitStream_headerAlreadyRead.ReadUInt(out gonetId_diff_unsigned, (gonetIdByteCount + 1) << 3);

                    long gonetId_diff = isDiffNegative ? -gonetId_diff_unsigned : gonetId_diff_unsigned;
                    gonetIdAtInstantiation = (uint)(gonetId_previous + gonetId_diff);
                    gonetId_previous = gonetIdAtInstantiation;

                    //GONetLog.Append(gonetIdAtInstantiation + ", ");
                }

                GONetParticipant gonetParticipant = null;
                uint gonetId = GetCurrentGONetIdByIdAtInstantiation(gonetIdAtInstantiation);

                if (gonetParticipantByGONetIdMap.ContainsKey(gonetId))
                {
                    gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                }
                else
                {
                    if (gonetParticipantByGONetIdMap.ContainsKey(gonetIdAtInstantiation))
                    {
                        gonetParticipant = gonetParticipantByGONetIdMap[gonetIdAtInstantiation];
                    }
                    else if (gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(gonetIdAtInstantiation))
                    {
                        gonetParticipant = gonetParticipantByGONetIdAtInstantiationMap[gonetIdAtInstantiation];
                    }

                    if ((object)gonetParticipant == null)
                    {
                        //GONetLog.Append_FlushDebug();

                        QosType channelQuality = GONetChannel.ById(channelId).QualityOfService;
                        if (channelQuality == QosType.Reliable)
                        {
                            const string GLAD = "Reliable changes bundle being processed and GONetParticipant NOT FOUND by GONetId: ";
                            const string INST = " gonetId@instantiation(as found in serialized body): ";
                            const string COUNT = "  This will cause us not to be able to process this and the rest of the bundle, which means we will not process count: ";
                            //throw new GONetOutOfOrderHorseDickoryException(string.Concat(GLAD, gonetId, INST, gonetIdAtInstantiation, COUNT, (count - i)));
                            throw new GONetOutOfOrderHorseDickoryException(string.Concat(GLAD, gonetId, INST, gonetIdAtInstantiation, COUNT));
                        }
                        else
                        {
                            const string NTS = "Received some unreliable GONetAutoMagicalSync data prior to some necessary prerequisite reliable data and we are unable to process this message.  Since it was sent unreliably, just pretend it did not arrive at all.  If this message streams in the log, perhaps you should be worried; however, it may appear from time to time around initialization and spawning under what is considered \"normal circumstances.\"  gonetId(from message, which is expected to be at instantiation): ";
                            const string LOCAL = " gonetId (from lookup, supposed to be current): ";
                            GONetLog.Warning(string.Concat(NTS, gonetIdAtInstantiation, LOCAL, gonetId));
                            return;
                        }
                    }
                }


                //Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance[gonetParticipant.codeGenerationId];

                if (gonetParticipant == null)
                {
                    GONetLog.Error("dude's Unity null...the rest will fail.  reference null too? " + ((object)gonetParticipant == null) + " gonetId: " + ((object)gonetParticipant == null ? GONetParticipant.GONetId_Unset : gonetParticipant.GONetId));
                    gnpsAwaitingCompanion.Add(gonetParticipant);
                }

                try
                {
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = companionMap[gonetParticipant._GONetIdAtInstantiation];

                    bool isBundleTypeValueChanges = chosenBundleType == typeof(AutoMagicalSync_ValueChanges_Message);

                    byte index = (byte)bitStream_headerAlreadyRead.ReadByte();

                    if (gonetParticipant.IsMine) // with recent changes, bundles all all the same for all clients, which means you will receive your own stuff too...essentially want to skip, but have to move the bit reader forward!
                    {
                        syncCompanion.DeserializeInitSingle_ReadOnlyNotApply(bitStream_headerAlreadyRead, index);
                    }
                    else
                    {
                        if (isBundleTypeValueChanges)
                        {
                            syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend);

                            AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[index];

#if !PERF_NO_PROCESS_SYNC_EVENTS
                            syncValueChanges_ReceivedFromOtherQueue.Enqueue(GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateInstance(SyncEvent_ValueChangeProcessedExplanation.InboundFromOther, elapsedTicksAtSend, sourceOfChangeConnection.OwnerAuthorityId, changedValue.syncCompanion, changedValue.index));
#endif
                        }
                        else // ASSume values now at rest bundle
                        {
                            //GONetLog.Debug($"remote source says now at rest.  index: {index}. elapsedMsAtSend: {TimeSpan.FromTicks(elapsedTicksAtSend).TotalMilliseconds}  time Now-Source: {TimeSpan.FromTicks(Time.ElapsedTicks - elapsedTicksAtSend).TotalMilliseconds}ms, time remaining before buffer lead time elapsed: {TimeSpan.FromTicks(elapsedTicksAtSend + valueBlendingBufferLeadTicks - Time.ElapsedTicks).TotalMilliseconds}");

                            // clear out the value blending buffer if appropriate and also ensure the value gets set instead of only added to blending buffer!
                            if (syncCompanion.valuesChangesSupport[index].syncAttribute_ShouldBlendBetweenValuesReceived)
                            {
                                // Deserializing from the bit stream has to happen now before waiting in coroutine becuase the rest of the bit stream processing happens immediately hereafter!
                                // We don't want it applied immediately, so we have to setup a coroutine to
                                // apply the value AFTER we ensure the value blending buffer time has elapsed to avoid applying too soon!
                                GONetSyncableValue value = syncCompanion.DeserializeInitSingle_ReadOnlyNotApply(bitStream_headerAlreadyRead, index);
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest = true;
                                long assumedInitialRestElapsedTicks = elapsedTicksAtSend - TimeSpan.FromSeconds(syncCompanion.valuesChangesSupport[index].syncAttribute_SyncChangesEverySeconds).Ticks; // need to subtract the sync rate off of this to know when the value actually first arrived at rest value
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_assumedInitialRestElapsedTicks = assumedInitialRestElapsedTicks;
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks = Time.ElapsedTicks - valueBlendingBufferLeadTicks;
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_value = value;
                                long assumedOneWayAtRestDelayTicks = Time.ElapsedTicks - assumedInitialRestElapsedTicks;
                                long easingDurationTicks = assumedOneWayAtRestDelayTicks - valueBlendingBufferLeadTicks;

                                // NOTE: This will run immediately if one-way network time exceeds valueBlendingBufferLeadTicks (i.e. non-owner will always be extrapolating!)
                                Global.StartCoroutine(DoAtOrAfterElapsedTicks(() => 
                                {
                                    syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest = false;
                                    // Clearing the recent changes buffer effectively ensures that the new value at rest value is the one applied and no
                                    // blending will occur since this is the only value in the blending buffer....neat trick to not need additional code
                                    // to make sure we apply this new value at rest now!
                                    var mostRecentQueuedValue = syncCompanion.valuesChangesSupport[index].mostRecentChanges[0];
                                    syncCompanion.valuesChangesSupport[index].ClearMostRecentChanges();
                                    //GONetLog.Debug($"just cleared most recent changes due to at rest....easingDuration: {TimeSpan.FromTicks(easingDurationTicks).TotalSeconds}\n(OLD) recent buffered:{mostRecentQueuedValue.numericValue} \n(OLD) current value: {syncCompanion.GetAutoMagicalSyncValue(index)}, \n(NEW) at rest value: {value}");
                                    syncCompanion.InitSingle(value, index, assumedInitialRestElapsedTicks);
                                    
                                    //syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_easeUntilElapsedTicks = ;

                                }, assumedInitialRestElapsedTicks + valueBlendingBufferLeadTicks));
                            }
                            else
                            {
                                syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend);
                            }

                            /* TODO change this to an at rest message?  probably not needed...leave commented out for now until deemed useful
                            AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[index];

                            syncValueChanges_ReceivedFromOtherQueue.Enqueue(GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.CreateInstance(SyncEvent_ValueChangeProcessedExplanation.InboundFromOther, elapsedTicksAtSend, sourceOfChangeConnection.OwnerAuthorityId, changedValue.syncCompanion, changedValue.index));
                            */
                        }
                    }
                }
                catch (Exception e)
                {
                    GONetLog.Error("BOOM! bitStream_headerAlreadyRead  " + e.StackTrace + "  position_bytes: " + bitStream_headerAlreadyRead.Position_Bytes + " Length_WrittenBytes: " + bitStream_headerAlreadyRead.Length_WrittenBytes);

                    throw e;
                }
            }
            //GONetLog.Append_FlushDebug("\n************done reading changes bundle");
        }

        static IEnumerator DoAtOrAfterElapsedTicks(Action doAction, long atOrAfterElapsedTicks)
        {
            while (Time.ElapsedTicks < atOrAfterElapsedTicks)
            {
                yield return null;
            }
            doAction();
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
        /// <summary>
        /// A breadth first search for child transforms that goes deep into children, which is not the case with <see cref="Transform.Find(string)"/>
        /// </summary>
        public static Transform FindDeep(this Transform transform, string childName)
        {
            Transform result = transform.Find(childName);
            if (result == null)
            {
                int count = transform.childCount;
                for (int i = 0; i < count; ++i)
                {
                    result = transform.GetChild(i).FindDeep(childName);
                    if (result != null)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        private static readonly HashSet<GONetBehaviour> tickReceivers = new HashSet<GONetBehaviour>();
        private static readonly HashSet<GONetBehaviour> tickReceivers_awaitingAdd = new HashSet<GONetBehaviour>();
        private static readonly HashSet<GONetBehaviour> tickReceivers_awaitingRemove = new HashSet<GONetBehaviour>();
        internal static void AddTickReceiver(GONetBehaviour gONetBehaviour)
        {
            tickReceivers_awaitingAdd.Add(gONetBehaviour);
        }

        internal static void RemoveTickReceiver(GONetBehaviour gONetBehaviour)
        {
            tickReceivers_awaitingRemove.Add(gONetBehaviour);
        }
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
        public static readonly GONetChannel AutoMagicalSync_ValuesNowAtRest_Reliable;
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
            AutoMagicalSync_ValuesNowAtRest_Reliable = new GONetChannel(QosType.Reliable);
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

        public static bool IsGONetCoreChannel(GONetChannelId channelId)
        {
            return
                channelId == TimeSync_Unreliable ||
                channelId == AutoMagicalSync_Reliable ||
                channelId == AutoMagicalSync_Unreliable ||
                channelId == AutoMagicalSync_ValuesNowAtRest_Reliable ||
                channelId == CustomSerialization_Reliable ||
                channelId == CustomSerialization_Unreliable ||
                channelId == EventSingles_Reliable ||
                channelId == EventSingles_Unreliable ||
                channelId == ClientInitialization_EventSingles_Reliable ||
                channelId == ClientInitialization_CustomSerialization_Reliable;
        }
    }
}
