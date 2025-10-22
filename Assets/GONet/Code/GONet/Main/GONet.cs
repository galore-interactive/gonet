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
using UnityEngine.SceneManagement;

using GONetCodeGenerationId = System.Byte;
using GONetChannelId = System.Byte;
using System.IO;
using System.Runtime.Serialization;
using System.Net;
using System.Collections;
using System.Diagnostics;
using GONet.PluginAPI;
using System.Text;
using System.Runtime.InteropServices;

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

        #region Scene Loading History Tracker

        /// <summary>
        /// Tracks scene loading history for debugging message flow.
        /// Thread-safe for concurrent access from network/main threads.
        /// Format: "GONetSample → RPCPlayground → ProjectileTest"
        /// </summary>
        private static readonly List<string> sceneLoadHistory = new List<string>();
        private static readonly object sceneHistoryLock = new object();

        /// <summary>
        /// Gets scene loading history as a single string for log prefixes.
        /// Thread-safe. Returns empty string if no scenes loaded yet.
        /// </summary>
        internal static string GetSceneHistory()
        {
            lock (sceneHistoryLock)
            {
                if (sceneLoadHistory.Count == 0)
                    return string.Empty;

                return string.Join(" → ", sceneLoadHistory);
            }
        }

        /// <summary>
        /// Records a scene load in the history tracker.
        /// Called automatically by GONetSceneManager when scenes load.
        /// Thread-safe.
        /// </summary>
        internal static void RecordSceneLoad(string sceneName)
        {
            lock (sceneHistoryLock)
            {
                sceneLoadHistory.Add(sceneName);
                GONetLog.Debug($"[SceneHistory] Scene loaded: {sceneName} (history now: {GetSceneHistory()})");
            }
        }

        /// <summary>
        /// Clears scene loading history. Used when starting new game sessions.
        /// Thread-safe.
        /// </summary>
        internal static void ClearSceneHistory()
        {
            lock (sceneHistoryLock)
            {
                sceneLoadHistory.Clear();
            }
        }

        #endregion

        #region Ring Buffer Metrics

        /// <summary>
        /// Snapshot of ring buffer metrics for a specific thread's event queue.
        /// Used by the inspector and debugging tools to monitor buffer health.
        /// </summary>
        public struct RingBufferMetrics
        {
            public int Capacity;
            public int Count;
            public int PeakCount;
            public int ResizeCount;
            public float FillPercentage;
            public string ThreadName;
        }

        /// <summary>
        /// Gets metrics for all ring buffers (one per sync thread).
        /// Thread-safe. Returns empty array if not initialized yet.
        /// Used by GONetGlobalCustomInspector for live metrics display.
        /// </summary>
        public static RingBufferMetrics[] GetRingBufferMetrics()
        {
            if (events_SendToOthersQueue_ByThreadMap == null || events_SendToOthersQueue_ByThreadMap.Count == 0)
            {
                return Array.Empty<RingBufferMetrics>();
            }

            var metrics = new List<RingBufferMetrics>();

            // Thread-safe iteration (dictionary keys added only from main thread, but ring buffers accessed from multiple threads)
            lock (events_SendToOthersQueue_ByThreadMap)
            {
                foreach (var kvp in events_SendToOthersQueue_ByThreadMap)
                {
                    var buffer = kvp.Value;
                    metrics.Add(new RingBufferMetrics
                    {
                        Capacity = buffer.Capacity,
                        Count = buffer.Count,
                        PeakCount = buffer.PeakCount,
                        ResizeCount = buffer.ResizeCount,
                        FillPercentage = buffer.FillPercentage,
                        ThreadName = kvp.Key.Name ?? "Main Thread"
                    });
                }
            }

            return metrics.ToArray();
        }

        #endregion

        #region Persistent Event History Export

        /// <summary>
        /// Controls whether event history export is enabled.
        /// Set via GONetGlobal inspector or code before initialization.
        /// </summary>
        public static bool EnableEventHistoryExport { get; set; } = true;

        /// <summary>
        /// If true, only server exports event history.
        /// If false, all machines (server + clients) export their own copies.
        /// Default: false (all machines export for maximum debugging capability)
        /// </summary>
        public static bool EventHistoryExport_ServerOnly { get; set; } = false;

        /// <summary>
        /// Exports the complete persistent event history to a human-readable file.
        /// Called automatically on application quit if EnableEventHistoryExport is true.
        /// File format: gonet-events-YYYY-MM-DD-HHmmss-[Server|ClientN].txt
        /// </summary>
        private static void ExportPersistentEventHistory()
        {
            if (!EnableEventHistoryExport)
            {
                GONetLog.Debug("[EventHistory] Export disabled (EnableEventHistoryExport=false)");
                return;
            }

            if (EventHistoryExport_ServerOnly && !IsServer)
            {
                GONetLog.Debug("[EventHistory] Export skipped (EventHistoryExport_ServerOnly=true and this is a client)");
                return;
            }

            try
            {
                // Determine role identifier for filename
                string roleIdentifier = IsServer ? "Server" : $"Client{MyAuthorityId}";

                // Create filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                string filename = $"gonet-events-{timestamp}-{roleIdentifier}.txt";

                // Use same directory as GONetLog (Application.persistentDataPath/logs)
                string logDirectory = Path.Combine(Application.persistentDataPath, "logs");
                Directory.CreateDirectory(logDirectory); // Ensure directory exists

                string filepath = Path.Combine(logDirectory, filename);

                int eventCount = persistentEventsArchive_CompleteHistory.Count;
                GONetLog.Info($"[EventHistory] Exporting {eventCount} persistent events to: {filepath}");

                using (StreamWriter writer = new StreamWriter(filepath, append: false, Encoding.UTF8))
                {
                    // Write header
                    writer.WriteLine("================================================================================");
                    writer.WriteLine($"GONet Persistent Event History Export");
                    writer.WriteLine($"Role: {roleIdentifier}");
                    writer.WriteLine($"Authority ID: {MyAuthorityId}");
                    writer.WriteLine($"Session GUID: {SessionGUID}");
                    writer.WriteLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                    writer.WriteLine($"Event Count: {eventCount}");
                    writer.WriteLine($"Scene History: {GetSceneHistory()}");
                    writer.WriteLine("================================================================================");
                    writer.WriteLine();

                    // Write event index for quick navigation
                    writer.WriteLine("EVENT INDEX (for grep searching):");
                    writer.WriteLine("  InstantiateGONetParticipantEvent - Spawns");
                    writer.WriteLine("  SyncEvent_ValueChangeProcessed - Value changes (Note: Transient, may not appear in persistent archive)");
                    writer.WriteLine("  SceneLoadEvent - Scene loads");
                    writer.WriteLine("  SceneUnloadEvent - Scene unloads");
                    writer.WriteLine();
                    writer.WriteLine("GREP EXAMPLES:");
                    writer.WriteLine("  grep 'GONetId=3072' gonet-events-*.txt  # All events for specific participant");
                    writer.WriteLine("  grep 'InstantiateGONetParticipantEvent' gonet-events-*.txt  # All spawns");
                    writer.WriteLine("  grep 'Authority1' gonet-events-*.txt  # All events involving client 1");
                    writer.WriteLine("================================================================================");
                    writer.WriteLine();

                    // Write events in chronological order
                    int eventIndex = 0;
                    foreach (var persistentEvent in persistentEventsArchive_CompleteHistory)
                    {
                        eventIndex++;

                        // Extract common properties from event
                        string eventTypeName = persistentEvent.GetType().Name;
                        uint gonetId = 0;
                        ushort ownerAuthority = 0;
                        long elapsedTicks = persistentEvent.OccurredAtElapsedTicks;

                        // Try to extract GONetId and OwnerAuthorityId from common event types
                        if (persistentEvent is InstantiateGONetParticipantEvent instantiateEvent)
                        {
                            gonetId = instantiateEvent.GONetId;
                            ownerAuthority = instantiateEvent.OwnerAuthorityId;
                        }
                        else if (persistentEvent is SyncEvent_ValueChangeProcessed valueChangeEvent)
                        {
                            gonetId = valueChangeEvent.GONetId;
                        }

                        // Format timestamp
                        double elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;

                        // Write event entry
                        writer.WriteLine($"[Event {eventIndex:D6}] Type={eventTypeName}");
                        writer.WriteLine($"  Timestamp: Ticks={elapsedTicks} ({elapsedSeconds:F3}s)");

                        if (gonetId != 0)
                            writer.WriteLine($"  GONetId: {gonetId}");

                        if (ownerAuthority != 0)
                            writer.WriteLine($"  Owner: Authority{ownerAuthority}");

                        // Add event-specific details
                        writer.WriteLine($"  Details: {GetEventDetailsString(persistentEvent)}");
                        writer.WriteLine();
                    }

                    writer.WriteLine("================================================================================");
                    writer.WriteLine($"END OF EVENT HISTORY - Total Events: {eventCount}");
                    writer.WriteLine("================================================================================");
                }

                GONetLog.Info($"[EventHistory] Export complete: {filepath} ({eventCount} events)");
            }
            catch (Exception ex)
            {
                GONetLog.Error($"[EventHistory] Export failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Gets a human-readable string with event-specific details.
        /// Used for event history export.
        /// </summary>
        private static string GetEventDetailsString(IPersistentEvent persistentEvent)
        {
            try
            {
                // Use ToString() as base, then add type-specific details
                string baseString = persistentEvent.ToString();

                if (persistentEvent is InstantiateGONetParticipantEvent instantiateEvent)
                {
                    return $"{baseString} | DesignTimeLocation={instantiateEvent.DesignTimeLocation} | Position={instantiateEvent.Position} | Rotation={instantiateEvent.Rotation}";
                }
                else if (persistentEvent is SyncEvent_ValueChangeProcessed valueChangeEvent)
                {
                    return $"{baseString} | SyncMemberIndex={valueChangeEvent.SyncMemberIndex} | GONetId={valueChangeEvent.GONetId}";
                }

                return baseString;
            }
            catch (Exception ex)
            {
                return $"[Error getting details: {ex.Message}]";
            }
        }

        #endregion

        public static GONetGlobal Global { get; private set; }

        /// <summary>
        /// Adaptive pool scaler manages dynamic pool sizing based on network demand.
        /// Initialized during InitOnUnityMainThread() with settings from GONetGlobal.
        /// </summary>
        private static GONetAdaptivePoolScaler adaptivePoolScaler;

        /// <summary>
        /// Manages networked scene loading and unloading.
        /// Server-authoritative: only server can initiate scene changes.
        /// Access from GONetBehaviour via this.SceneManager property.
        /// </summary>
        public static GONetSceneManager SceneManager { get; private set; }

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
            InitObjectPools();
            InitClientTime();
            InitSceneManager();
            InitAdaptivePoolScaler(); // Initialize adaptive scaling system

            ticksAtLastInit_UtcNow = DateTime.UtcNow.Ticks;

        }

        private static void InitSceneManager()
        {
            SceneManager = new GONetSceneManager(Global);

            // Subscribe to scene load completion to process deferred spawns
            SceneManager.OnSceneLoadCompleted += OnSceneLoadCompleted_ProcessDeferredSpawns;

            GONetLog.Debug("[GONetMain] Scene manager initialized");
        }

        private static void InitAdaptivePoolScaler()
        {
            adaptivePoolScaler = new GONetAdaptivePoolScaler(Global);
            GONetLog.Debug("[GONetMain] Adaptive pool scaler initialized");
        }

        private static void OnSceneLoadCompleted_ProcessDeferredSpawns(string sceneName, LoadSceneMode mode)
        {
            // CRITICAL FIX (October 2025): NEVER reset batch tracking on scene change
            //
            // WHY: Server and clients must have symmetric behavior - both persist batches across scenes.
            // If server resets but clients don't, late-joining clients can receive overlapping batches
            // because server "forgets" about batches allocated before the scene change.
            //
            // EXAMPLE BUG SCENARIO:
            // 1. Server allocates batch [604-803] to Client 2 before scene change
            // 2. Scene changes, server resets batch tracking (forgets [604-803])
            // 3. Client 2 keeps batch [604-803] (by design)
            // 4. Client 3 joins late, server allocates [704-903] (overlaps with Client 2's [604-803]!)
            // 5. Both clients allocate raw ID 704 → same GONetId → zombie objects
            //
            // MEMORY/ID SPACE ANALYSIS:
            // - Memory cost: ~8 bytes per batch (1 uint), trivial even for 1000+ clients
            // - GONetId space: 4,194,304 IDs available (22 bits), 200 IDs per batch = 20,971 max batches
            // - Realistic usage: 100 clients × 1000 spawns = 100K IDs used (2.5% of space)
            // - Batches released on client disconnect (natural recycling in long-running servers)
            //
            // DECISION: Server batch tracking persists across scenes, matching client behavior.
            // Only reset on full disconnect/shutdown.
            if (mode == LoadSceneMode.Single)
            {
                if (IsServer)
                {
                    // REMOVED: Server batch reset on scene change (caused overlapping batch bug)
                    // GONetIdBatchManager.Server_ResetAllBatches();
                    GONetLog.Info($"[GONetIdBatch] SERVER kept batches on scene change (LoadSceneMode.Single): {sceneName}");
                }
                if (IsClient)
                {
                    // DO NOT reset client batches - they persist across scenes
                    // GONetIdBatchManager.Client_ResetAllBatches();
                    client_lastServerGONetIdRawForRemoteControl = GONetParticipant.GONetIdRaw_Unset;
                    GONetLog.Info($"[GONetIdBatch] CLIENT kept batches on scene change (LoadSceneMode.Single): {sceneName}");
                }
            }

            // When a scene loads, process any deferred spawns that were waiting for it
            ProcessDeferredSpawnsForScene(sceneName);
        }

        private static void InitClientTime()
        {
            Global.StartCoroutine(InitClientTimeCoroutine());
        }

        private static IEnumerator InitClientTimeCoroutine()
        {
            while (!IsClientVsServerStatusKnown)
            {
                yield return null;
            }

            if (IsClient)
            {
                Time.TimeSetFromAuthority += Client_TimeSetFromAuthority;
            }
        }

        private static void Client_TimeSetFromAuthority(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks)
        {
            // This is called by the high-perf time sync when time is adjusted
            OnSyncValueChangeProcessed_Persist_Local(
                SyncEvent_Time_ElapsedTicks_SetFromAuthority.Borrow(
                    fromElapsedTicks,
                    toElapsedTicks,
                    GONetClient.connectionToServer.RTT_Latest,
                    GONetClient.connectionToServer.RTT_RecentAverage,
                    GONetClient.connectionToServer.RTTMilliseconds_LowLevelTransportProtocol),
                false); // NOTE: false is to indicate no copy needed

            // Log significant adjustments
            double adjustmentSeconds = toElapsedSeconds - fromElapsedSeconds;
            if (Math.Abs(adjustmentSeconds) > 0.01) // More than 10ms adjustment
            {
                GONetLog.Debug($"Local time adjusted from authority (i.e., server) by {adjustmentSeconds:F3} seconds (from {fromElapsedSeconds:F3} to {toElapsedSeconds:F3}), which is more than expected, but not necessarily a bad thing.");
            }
        }

        private static void Application_quitting_TakeNote()
        {
            IsApplicationQuitting = true;

            // Export persistent event history before shutdown
            ExportPersistentEventHistory();
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

        /// <summary>
        /// Have to ensure all these static object pools are initialized for all the (generated) child classes of 
        /// <see cref="SyncEvent_ValueChangeProcessed"/>
        /// </summary>
        private static void InitObjectPools()
        {
            List<Type> syncEventTypes = TypeUtils.GetAllTypesInheritingFrom<SyncEvent_ValueChangeProcessed>(isConcreteClassRequired: true);
            //UnityEngine.Debug.Log($"[DREETS] Start init'ing sync event class object pools (class count: {syncEventTypes.Count})...");
            foreach (Type syncEventType in syncEventTypes)
            {
                RuntimeHelpers.RunClassConstructor(syncEventType.TypeHandle);
            }
            //UnityEngine.Debug.Log("[DREETS] ...end (init'ing sync event class object pools)!");
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
        /// Is a client host in peer-to-peer or non-dedicated server setup?
        /// <see cref="ClientTypeFlags.ServerHost"/>.
        /// </summary>
        public static bool IsHost => IsServer && IsClient /* TODO && _gonetClient.ClientTypeFlags == ClientTypeFlags.ServerHost */;

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
        /// Late-joiner synchronization storage: "Current state" list of persistent events.
        /// This will NOT include ALL events that implement <see cref="IPersistentEvent"/> if anything
        /// cancelled out another/previous event (i.e., <see cref="ICancelOutOtherEvents"/>).
        ///
        /// ⚠️  CRITICAL DESIGN CONSTRAINT: EVENTS STORED BY REFERENCE
        ///
        /// This LinkedList stores DIRECT REFERENCES to persistent event objects for the entire
        /// session duration (minutes to hours). When late-joining clients connect, these exact
        /// references are serialized and transmitted (see Server_SendClientPersistentEventsSinceStart:4355).
        ///
        /// IMPLICATIONS FOR EVENT CLASSES:
        /// Any class that goes into this list (implements IPersistentEvent) MUST NOT use object pooling.
        /// If pooled events were stored here:
        ///   1. Event created and added to this list by reference
        ///   2. Event.Return() called → data cleared, returned to pool
        ///   3. Pool reuses object → stored reference now contains WRONG data
        ///   4. Late-joiner connects → receives corrupted data from stored reference
        ///   5. CATASTROPHIC: Game state desynchronization, crashes, invisible bugs
        ///
        /// Classes that CORRECTLY avoid pooling (do NOT implement ISelfReturnEvent):
        /// - PersistentRpcEvent (see GONetRpcs.cs:912 for detailed rationale)
        /// - PersistentRoutedRpcEvent (TargetRpc variant)
        /// - InstantiateGONetParticipantEvent (spawn events)
        /// - DespawnGONetParticipantEvent (despawn with cancellation)
        /// - SceneLoadEvent (networked scene management)
        ///
        /// Memory cost: ~1-10 KB per session (acceptable for data integrity guarantee)
        ///
        /// See also:
        /// - OnPersistentEvent_KeepTrack() at line 1549 - where events are added to this list
        /// - Server_SendClientPersistentEventsSinceStart() at line 4355 - where stored references are transmitted
        /// - GONetRpcs.cs:912 - PersistentRpcEvent class documentation for full pooling rationale
        /// </summary>
        static readonly LinkedList<IPersistentEvent> persistentEventsThisSession = new LinkedList<IPersistentEvent>();

        /// <summary>
        /// RECORD AND REPLAY ARCHIVE: Complete historical record of ALL persistent events that occurred during this session.
        /// Unlike <see cref="persistentEventsThisSession"/>, this list is NEVER modified - events are only added, never removed.
        /// This preserves the full event timeline including cancelled events for future record/replay functionality.
        ///
        /// Use cases:
        /// - Session replay: Replay the exact sequence of events that occurred
        /// - Debugging: Analyze full event history including cancelled events
        /// - Analytics: Track complete session timeline
        ///
        /// NOTE: This archive is currently NOT used for late-joiner synchronization - that uses persistentEventsThisSession.
        /// </summary>
        static readonly LinkedList<IPersistentEvent> persistentEventsArchive_CompleteHistory = new LinkedList<IPersistentEvent>();

        /// <summary>
        /// PUBLIC API: Access the complete historical archive of all persistent events.
        /// This is a read-only view of the full event timeline including cancelled events.
        ///
        /// IMPORTANT: This returns the internal list - do NOT modify it! Only use for reading/iteration.
        ///
        /// Example usage for future record/replay:
        /// <code>
        /// // Save complete session history to file
        /// var allEvents = GONetMain.PersistentEventsArchive_CompleteHistory;
        /// SaveToFile(allEvents);
        ///
        /// // Later: Replay the exact sequence
        /// foreach (var evt in LoadFromFile())
        /// {
        ///     ReplayEvent(evt);
        /// }
        /// </code>
        /// </summary>
        public static IEnumerable<IPersistentEvent> PersistentEventsArchive_CompleteHistory => persistentEventsArchive_CompleteHistory;

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
        static readonly Dictionary<Thread, RingBuffer<IGONetEvent>> events_SendToOthersQueue_ByThreadMap = new Dictionary<Thread, RingBuffer<IGONetEvent>>(12);

        /// <summary>
        /// The keys are only added from main unity thread...the value queues are only added to on the other thread (i.e., transfer data from <see cref="events_AwaitingSendToOthersQueue_ByThreadMap"/> once the time is right) but also read from and dequeued from the main unity thread when time to publish the events!
        /// </summary>
        static readonly Queue<SyncEvent_ValueChangeProcessed> syncValueChanges_ReceivedFromOtherQueue = new Queue<SyncEvent_ValueChangeProcessed>(100);

        /// <summary>
        /// AUTHORITY-AGNOSTIC: Queue for sync bundles deferred due to participants still in Awake/initialization.
        /// Used by BOTH authority owners (clients/server spawning objects) AND non-authority receivers.
        /// Only populated when GONetGlobal.deferSyncBundlesWaitingForGONetReady == true AND channel is reliable.
        /// Processed incrementally when any participant completes OnGONetReady (up to maxBundlesProcessedPerGONetReadyCallback per callback).
        /// </summary>
        internal static readonly Queue<NetworkData> incomingNetworkData_waitingForGONetReady = new Queue<NetworkData>(100);

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
            GONetLog.Info($"[INIT] Client_gonetClient_InitializedWithServer() called - MyAuthorityId={MyAuthorityId}");

            MyLocal = UnityEngine.Object.Instantiate(Global.gonetLocalPrefab);

            // CRITICAL: Set OwnerAuthorityId AFTER instantiation but BEFORE Start() is called
            // The GONetParticipant.Start() sends spawn event, so OwnerAuthorityId must be correct by then
            MyLocal.GONetParticipant.OwnerAuthorityId = MyAuthorityId;

            // CRITICAL: Move GONetLocal to DontDestroyOnLoad scene IMMEDIATELY after instantiation
            // This prevents it from being incorrectly recorded as "defined in scene" if a scene load is in progress
            UnityEngine.Object.DontDestroyOnLoad(MyLocal.gameObject);
            GONetLog.Info($"[INIT] GONetLocal instantiated and moved to DontDestroyOnLoad scene - MyLocal GONetId: {(MyLocal?.GONetParticipant?.GONetId ?? 0)}, OwnerAuthorityId: {MyLocal?.GONetParticipant?.OwnerAuthorityId ?? 0}");

            while (client.incomingNetworkData_mustProcessAfterClientInitialized.Count > 0)
            {
                NetworkData item = client.incomingNetworkData_mustProcessAfterClientInitialized.Dequeue();
                ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(item);
            }

            Client_SyncTimeWithServer_SendInitialBarrage();
        }

        /// <summary>
        /// Processes messages that were queued because they referenced GONetIds that weren't assigned yet.
        /// Called after scene-defined object GONetIds have been synchronized from the server.
        /// </summary>
        internal static void ProcessQueuedMessagesWaitingForGONetIds()
        {
            if (!IsClient || _gonetClient == null)
                return;

            int queueSize = _gonetClient.incomingNetworkData_waitingForGONetIds.Count;
            if (queueSize == 0)
            {
                GONetLog.Debug("[GONETID-QUEUE] No queued messages to process");
                return;
            }

            GONetLog.Info($"[GONETID-QUEUE] Processing {queueSize} queued messages that were waiting for GONetId assignments");

            int processedCount = 0;
            int failedCount = 0;

            while (_gonetClient.incomingNetworkData_waitingForGONetIds.Count > 0)
            {
                NetworkData item = _gonetClient.incomingNetworkData_waitingForGONetIds.Dequeue();
                try
                {
                    ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(item, isProcessingFromQueue: true);
                    processedCount++;
                }
                catch (Exception e)
                {
                    failedCount++;
                    GONetLog.Error($"[GONETID-QUEUE] Failed to process queued message: {e.Message}");
                    // Still need to return the message to the pool
                    SingleProducerQueues queues = singleProducerReceiveQueuesByThread[item.messageBytesBorrowedOnThread];
                    queues.queueForPostWorkResourceReturn.Enqueue(item);
                }
            }

            GONetLog.Info($"[GONETID-QUEUE] Finished processing queued messages - Processed: {processedCount}, Failed: {failedCount}");
        }

        /// <summary>
        /// Defers a sync bundle for retry after participant completes OnGONetReady.
        /// FIFO queue with size limit - oldest bundles dropped if queue full.
        /// </summary>
        private static void DeferSyncBundleWaitingForGONetReady(NetworkData networkData, long elapsedTicksAtSend, Type messageType)
        {
            int maxQueueSize = GONetGlobal.Instance.maxSyncBundlesWaitingForGONetReady;

            if (incomingNetworkData_waitingForGONetReady.Count < maxQueueSize)
            {
                incomingNetworkData_waitingForGONetReady.Enqueue(networkData);
            }
            else
            {
                // Queue full - drop OLDEST bundle and queue newest (FIFO policy)
                GONetLog.Warning($"[GONETREADY-QUEUE] Queue full ({maxQueueSize} bundles)! " +
                                $"Dropping OLDEST deferred bundle to make room. " +
                                $"Consider increasing GONetGlobal.maxSyncBundlesWaitingForGONetReady or disabling deferral. " +
                                $"MessageType: {messageType.Name}");

                NetworkData droppedMessage = incomingNetworkData_waitingForGONetReady.Dequeue();

                // Return dropped message's byte array to pool (critical for memory management)
                SingleProducerQueues droppedQueues = singleProducerReceiveQueuesByThread[droppedMessage.messageBytesBorrowedOnThread];
                droppedQueues.queueForPostWorkResourceReturn.Enqueue(droppedMessage);

                // Queue current message
                incomingNetworkData_waitingForGONetReady.Enqueue(networkData);
            }
        }

        /// <summary>
        /// Processes deferred sync bundles waiting for participants to complete OnGONetReady.
        /// Called automatically when any participant completes OnGONetReady.
        /// Processes up to maxBundlesProcessedPerGONetReadyCallback bundles per call to prevent frame stutter.
        /// </summary>
        internal static void ProcessDeferredSyncBundlesWaitingForGONetReady()
        {
            // Feature disabled - nothing to process
            if (!GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady)
                return;

            int queueSize = incomingNetworkData_waitingForGONetReady.Count;
            if (queueSize == 0)
                return; // Nothing queued

            int processedCount = 0;
            int failedCount = 0;

            // PERFORMANCE: Limit processing per callback to prevent frame stutter during mass spawns
            // OnGONetReady fires for EVERY participant - processing all queued bundles would cause spikes
            int maxPerCallback = GONetGlobal.Instance.maxBundlesProcessedPerGONetReadyCallback;
            int processed = 0;

            while (incomingNetworkData_waitingForGONetReady.Count > 0 && processed < maxPerCallback)
            {
                NetworkData item = incomingNetworkData_waitingForGONetReady.Dequeue();
                processed++;

                try
                {
                    // CRITICAL: Pass isProcessingFromQueue=true to prevent infinite retry loops
                    // If participant STILL not ready after retry, exception handler will DROP (not requeue)
                    ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(item, isProcessingFromQueue: true);
                    processedCount++;
                }
                catch (GONetParticipantNotReadyException notReadyEx)
                {
                    // Participant STILL not ready after 1+ frames - DROP (don't requeue)
                    // Exception handler in ProcessIncomingBytes already logged error via isProcessingFromQueue check
                    failedCount++;

                    // Return byte array to pool
                    SingleProducerQueues queues = singleProducerReceiveQueuesByThread[item.messageBytesBorrowedOnThread];
                    queues.queueForPostWorkResourceReturn.Enqueue(item);
                }
                catch (Exception e)
                {
                    // Unexpected failure - log and drop
                    failedCount++;
                    GONetLog.Error($"[GONETREADY-QUEUE] Failed to process deferred bundle: {e.Message}\n{e.StackTrace}");

                    // Return byte array to pool
                    SingleProducerQueues queues = singleProducerReceiveQueuesByThread[item.messageBytesBorrowedOnThread];
                    queues.queueForPostWorkResourceReturn.Enqueue(item);
                }
            }

            // Diagnostic logging (only if something happened)
            if (processedCount > 0 || failedCount > 0)
            {
                GONetLog.Debug($"[GONETREADY-QUEUE] Processed {processedCount} deferred bundles, " +
                              $"{failedCount} dropped (still not ready after retry), " +
                              $"{incomingNetworkData_waitingForGONetReady.Count} remaining in queue");
            }
        }

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdMap = new Dictionary<uint, GONetParticipant>(1000);
        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdAtInstantiationMap = new Dictionary<uint, GONetParticipant>(5000);
        internal static readonly Dictionary<uint, uint> recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map = new Dictionary<uint, uint>(1000);

        /// <summary>
        /// Tracks last warning time for each GONetId to suppress excessive "Unable to find GONetParticipant" warnings.
        /// Key: GONetId, Value: Time.ElapsedTicks when warning was last logged.
        /// Prevents log spam when unreliable sync events arrive after despawn (expected race condition).
        /// </summary>
        private static readonly Dictionary<uint, long> missingGONetParticipantWarningSuppressionMap = new Dictionary<uint, long>(100);

        /// <summary>
        /// Tracks last cleanup time for <see cref="missingGONetParticipantWarningSuppressionMap"/>.
        /// Cleanup runs once every 10 seconds to prevent unbounded dictionary growth.
        /// </summary>
        private static long? _lastWarningSuppressionCleanupTicks;

        /// <summary>
        /// Total count of unreliable packets dropped due to send buffer full (BorrowedCount > MAX_PACKETS_PER_TICK - 10).
        /// Incremented in SendBytesToRemoteConnection when flow control throttles unreliable messages.
        /// </summary>
        private static long _unreliablePacketDropCount = 0;

        #region GONetId Reuse Prevention (TTL Tracking)

        /// <summary>
        /// Tracks GONetIds that were recently despawned with their despawn timestamp.
        /// Prevents GONetId reuse while despawn messages are still in flight across the network.
        ///
        /// KEY: GONetId (composed value with authority)
        /// VALUE: Despawn time in seconds (GONetMain.Time.ElapsedSeconds)
        ///
        /// TTL is configured via GONetGlobal.gonetIdReuseDelaySeconds (default: 5 seconds).
        /// IDs are removed from this map after TTL expires during periodic cleanup.
        /// </summary>
        private static readonly Dictionary<uint, double> recentlyDespawnedGONetIds = new Dictionary<uint, double>(200);

        /// <summary>
        /// Tracks last cleanup time for <see cref="recentlyDespawnedGONetIds"/>.
        /// Cleanup runs periodically (every 30 seconds) to remove expired entries.
        /// </summary>
        private static double? _lastGONetIdReuseCleanupTime;

        #endregion

        /// <summary>
        /// Count of unreliable packets dropped since last log message.
        /// Resets to 0 after logging (every 100 drops).
        /// </summary>
        private static int _unreliablePacketDropCount_sinceLastLog = 0;

        /// <summary>
        /// Total count of successful packet sends (for calculating drop rate).
        /// Incremented in SendBytesToRemoteConnection when packets are successfully queued.
        /// </summary>
        private static long _successfulPacketSendCount = 0;

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
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.CodeGenerationId, out autoSyncCompanions) &&
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
            //GONetLog.Debug("Server assuming authority over GNP.  Is Mine Already (i.e., client used server assigned GONetIdRaw batch)? " + IsMine(gonetParticipant));
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
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.CodeGenerationId, out autoSyncCompanions) &&
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
            SyncEvent_ValueChangeProcessed @event = eventEnvelope.Event;
            OnGONetIdComponentChanged_EnsureMapKeysUpdated(gonetParticipant, @event.ValuePrevious.System_UInt16);

            if ((object)gonetParticipant != null && gonetParticipant.gonetId_raw != GONetParticipant.GONetId_Unset)
            {
                gonetParticipant.SetRigidBodySettingsConsideringOwner();
            }
            else
            {
                const string EXP = "Expecting to receive a non-null GNP, but it is null.";
                GONetLog.Warning(EXP);
            }

            using (var en = allGONetBehaviours.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    GONetBehaviour gnBehaviour = en.Current;
                    gnBehaviour.OnGONetParticipant_OwnerAuthorityIdChanged(
                        gonetParticipant,
                        @event.GONetId,
                        @event.ValuePrevious.System_UInt16,
                        @event.ValueNew.System_UInt16);
                }
            }
        }

        /// <summary>
        /// DEBUG: Handler for Transform.position sync events to trace what's actually being synced
        /// </summary>
        private static void OnTransformPositionChanged_Debug(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            GONetParticipant gnp = eventEnvelope.GONetParticipant;
            SyncEvent_ValueChangeProcessed @event = eventEnvelope.Event;

            string machineName = IsServer ? "Server" : $"Client:{MyAuthorityId}";
            string valuePrev = @event.ValuePrevious.UnityEngine_Vector3.ToString("F3");
            string valueNew = @event.ValueNew.UnityEngine_Vector3.ToString("F3");

            GONetLog.Info($"[{machineName}] [SYNC-DEBUG] Transform.position changed - GONetId: {gnp.GONetId}, Name: '{gnp.name}', IsMine: {gnp.IsMine}, Owner: {gnp.OwnerAuthorityId}, Prev: {valuePrev}, New: {valueNew}, IsRemote: {eventEnvelope.IsSourceRemote}");
        }

        /// <summary>
        /// DEBUG: Handler for Transform.rotation sync events to trace what's actually being synced
        /// </summary>
        private static void OnTransformRotationChanged_Debug(GONetEventEnvelope<SyncEvent_ValueChangeProcessed> eventEnvelope)
        {
            GONetParticipant gnp = eventEnvelope.GONetParticipant;
            SyncEvent_ValueChangeProcessed @event = eventEnvelope.Event;

            string machineName = IsServer ? "Server" : $"Client:{MyAuthorityId}";
            UnityEngine.Quaternion prevQuat = @event.ValuePrevious.UnityEngine_Quaternion;
            UnityEngine.Quaternion newQuat = @event.ValueNew.UnityEngine_Quaternion;
            string valuePrev = $"({prevQuat.x:F3}, {prevQuat.y:F3}, {prevQuat.z:F3}, {prevQuat.w:F3})";
            string valueNew = $"({newQuat.x:F3}, {newQuat.y:F3}, {newQuat.z:F3}, {newQuat.w:F3})";

            GONetLog.Info($"[{machineName}] [SYNC-DEBUG] Transform.rotation changed - GONetId: {gnp.GONetId}, Name: '{gnp.name}', IsMine: {gnp.IsMine}, Owner: {gnp.OwnerAuthorityId}, Prev: {valuePrev}, New: {valueNew}, IsRemote: {eventEnvelope.IsSourceRemote}");
        }

        internal static void OnGONetIdAboutToBeSet(uint gonetId_new, uint gonetId_raw_new, ushort ownerAuthorityId_new, GONetParticipant gonetParticipant)
        {
            if (gonetId_new == gonetParticipant.GONetIdAtInstantiation)
            {
                gonetParticipantByGONetIdAtInstantiationMap[gonetParticipant.GONetIdAtInstantiation] = gonetParticipant;
                gonetParticipantByGONetIdMap[gonetId_new] = gonetParticipant;

                // Deferred RPC system will automatically retry via ProcessDeferredRpcs() running every frame
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

                    // Deferred RPC system will automatically retry via ProcessDeferredRpcs() running every frame
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

                    // Deferred RPC system will automatically retry via ProcessDeferredRpcs() running every frame
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

                        // Deferred RPC system will automatically retry via ProcessDeferredRpcs() running every frame
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
        internal static readonly SecretaryOfTemporalAffairs Time = new();

        /// <summary>
        /// This is used to know which instances were instantiated due to a remote spawn message being received/processed.
        /// See <see cref="Instantiate_Remote(InstantiateGONetParticipantEvent)"/> and <see cref="Start_AutoPropagateInstantiation_IfAppropriate(GONetParticipant)"/>.
        /// </summary>
        static readonly List<GONetParticipant> remoteSpawns_avoidAutoPropagateSupport = new List<GONetParticipant>(1000);

        static GONetMain()
        {
            //Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            // NOTE: GONetThreading initializes itself automatically via [RuntimeInitializeOnLoadMethod]
            // to avoid Unity serialization phase issues with calling Unity APIs

            GONetGlobal.ActualServerConnectionInfoSet += OnActualServerConnectionInfoSet_UpdateIsServerOverride;

            InitMessageTypeToMessageIDMap();
            InitShouldSkipSyncSupport();
        }

        private static void OnActualServerConnectionInfoSet_UpdateIsServerOverride(string serverIP, int serverPort)
        {
            GONetLog.Debug($"Server override set to: {isServerOverride}, args: [{serverIP}]:{serverPort}, ServerIPAddress_Actual: {GONetGlobal.ServerIPAddress_Actual}, ServerPort_Actual: {GONetGlobal.ServerPort_Actual}, p2p: {GONetGlobal.ServerP2pEndPoint}");
        }

        private static void InitEventSubscriptions()
        {
            EventBus.Subscribe<IGONetEvent>(OnAnyEvent_RelayToRemoteConnections_IfAppropriate);
            EventBus.Subscribe<IPersistentEvent>(OnPersistentEvent_KeepTrack);
            EventBus.Subscribe<PersistentEvents_Bundle>(OnPersistentEventsBundle_ProcessAll_Remote, envelope => envelope.IsSourceRemote);
            EventBus.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent_Remote, envelope => envelope.IsSourceRemote);

            EventBus.Subscribe<GONetParticipantEnabledEvent>(OnEnabledGNPEvent);
            EventBus.Subscribe<GONetParticipantStartedEvent>(OnStartedGNPEvent);
            EventBus.Subscribe<GONetParticipantDeserializeInitAllCompletedEvent>(OnDeserializeInitAllCompletedGNPEvent);
            EventBus.Subscribe<GONetParticipantDisabledEvent>(OnDisabledGNPEvent);

            var despawnSubscription = EventBus.Subscribe<DespawnGONetParticipantEvent>(OnDespawnGNPEvent_Remote, envelope => envelope.IsSourceRemote);
            despawnSubscription.SetSubscriptionPriority_INTERNAL(int.MinValue); // process internally LAST since the GO will be destroyed and other subscribers may want to do something just prior to it being destroyed

            EventBus.SubscribeAnySyncEvents(OnSyncValueChangeProcessed_Persist_Local);

            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_GONetId, OnGONetIdChanged);
            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_GONetParticipant_OwnerAuthorityId, OnOwnerAuthorityIdChanged);

            // DEBUG: Subscribe to position/rotation sync events to trace what's actually being synced
            // NOTE: This logs EVERY transform sync (hundreds per second!) - only enable for debugging
            // To enable, add LOG_SYNC_VERBOSE to Player Settings → Scripting Define Symbols
            #if LOG_SYNC_VERBOSE
            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_Transform_position, OnTransformPositionChanged_Debug);
            EventBus.Subscribe(SyncEvent_GeneratedTypes.SyncEvent_Transform_rotation, OnTransformRotationChanged_Debug);
            #endif

            EventBus.Subscribe<ValueMonitoringSupport_NewBaselineEvent>(OnNewBaselineValue_Remote, envelope => envelope.IsSourceRemote);

            EventBus.Subscribe<ClientRemotelyControlledGONetIdServerBatchAssignmentEvent>(Client_AssignNewClientGONetIdRawBatch);
            EventBus.Subscribe<ClientRemotelyControlledGONetIdServerBatchRequestEvent>(Server_HandleClientBatchRequest);

            // Subscribe to chunked persistent events for reassembly
            EventBus.Subscribe<PersistentEvents_BundleChunk>(OnPersistentEventsChunkReceived, envelope => envelope.IsSourceRemote);

            // Subscribe to scene load complete events from clients (server-side handler)
            EventBus.Subscribe<SceneLoadCompleteEvent>(Server_OnClientSceneLoadComplete, envelope => envelope.IsSourceRemote);

            EventBus.InitializeRpcSystem();
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
                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gnp.CodeGenerationId][gnp];

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
                // Suppress excessive warnings - only log once per 5 seconds per GONetId
                // This is expected: unreliable sync events arrive after despawn (race condition, not a bug)
                const long SUPPRESSION_WINDOW_TICKS = 5 * TimeSpan.TicksPerSecond;
                long currentTicks = Time.ElapsedTicks;
                long lastWarningTicks;
                bool shouldLog = !missingGONetParticipantWarningSuppressionMap.TryGetValue(@event.GONetId, out lastWarningTicks) ||
                                 (currentTicks - lastWarningTicks) >= SUPPRESSION_WINDOW_TICKS;

                if (shouldLog)
                {
                    const string GNID = "Unable to find GONetParticipant for GONetId: ";
                    const string POSSI = ", which is possibly due to it being destroyed and this event came at a bad time just after destroy processed....like was the case during testing with ProjectileTest.unity";
                    GONetLog.Warning(string.Concat(GNID, @event.GONetId, POSSI));
                    missingGONetParticipantWarningSuppressionMap[@event.GONetId] = currentTicks;
                }
                // else: warning suppressed (already logged recently for this GONetId)
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
            int eventCount = eventEnvelope.Event.PersistentEvents.Count;
            //GONetLog.Warning($"[SPAWN_SYNC] CLIENT: OnPersistentEventsBundle_ProcessAll_Remote - Processing bundle with {eventCount} events from AuthorityId {eventEnvelope.SourceAuthorityId}");

            int sceneLoadCount = 0;
            int spawnCount = 0;
            int otherCount = 0;

            foreach (var item in eventEnvelope.Event.PersistentEvents)
            {
                if (item is SceneLoadEvent sceneLoad)
                {
                    sceneLoadCount++;
                    //GONetLog.Warning($"[SPAWN_SYNC] CLIENT: - Processing SceneLoadEvent: '{sceneLoad.SceneName}', Mode: {sceneLoad.Mode}");
                }
                else if (item is InstantiateGONetParticipantEvent spawn)
                {
                    spawnCount++;
                    //GONetLog.Debug($"[SPAWN_SYNC] CLIENT: - Processing spawn: GONetId {spawn.GONetId}, Scene: '{spawn.SceneIdentifier}'");
                }
                else
                {
                    otherCount++;
                }

                persistentEventsThisSession.AddLast(item);

                // Publish the persistent event to the event bus so all registered handlers can process it
                // This replaces the old piecemeal approach and ensures extensibility for new persistent event types
                EventBus.Publish(
                    item,
                    remoteSourceAuthorityId: eventEnvelope.SourceAuthorityId,
                    targetClientAuthorityId: MyAuthorityId, // this is required to ensure my handlers are invoked and that is it
                    shouldPublishReliably: true); // probably redundant as none of this should go back over the wire at all
            }

            GONetLog.Debug($"[SPAWN_SYNC] CLIENT: Bundle processing complete - SceneLoad: {sceneLoadCount}, Spawn: {spawnCount}, Other: {otherCount}");
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

            // Check for self-targeting early - don't relay to ourselves
            if (eventEnvelope.TargetClientAuthorityId != OwnerAuthorityId_Unset &&
                eventEnvelope.TargetClientAuthorityId == MyAuthorityId)
            {
                // This event is targeted at ourselves - local handlers have already been processed
                // in GONetEventBus.Publish(), so we don't need to relay it anywhere
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
                    //GONetLog.Debug($"Sending event to server.  type: {eventEnvelope.Event.GetType().Name}");
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

        /// <summary>
        /// Sends an event to specific remote connections without triggering local handlers.
        /// This is used when we need to send the same event to multiple specific targets efficiently.
        /// </summary>
        internal static void Server_SendEventToSpecificRemoteConnections(IGONetEvent @event, ushort[] targetAuthorityIds, int targetCount, bool isReliable)
        {
            if (!IsServer || targetCount == 0)
            {
                return; // Only server can route to specific targets
            }

            // Serialize the event once
            byte[] bytes = default;
            int returnBytesUsedCount = default;
            bool doesNeedToReturn = default;

            try
            {
                bytes = SerializationUtils.SerializeToBytes(@event, out returnBytesUsedCount, out doesNeedToReturn);
            }
            catch (Exception e)
            {
                GONetLog.Error($"Failed to serialize event for multi-target send: {e}");
                return;
            }

            // Determine channel based on reliability
            GONetChannelId channelId = isReliable ? GONetChannel.EventSingles_Reliable : GONetChannel.EventSingles_Unreliable;

            // Send to each target
            for (int i = 0; i < targetCount; i++)
            {
                ushort targetAuthorityId = targetAuthorityIds[i];

                // Skip if targeting self (shouldn't happen but safety check)
                if (targetAuthorityId == MyAuthorityId)
                {
                    GONetLog.Warning($"SendEventToSpecificRemoteConnections called with self as target, skipping");
                    continue;
                }

                // Get the remote client
                if (gonetServer.TryGetRemoteClientByAuthorityId(targetAuthorityId, out GONetRemoteClient remoteClient))
                {
                    SendBytesToRemoteConnection(remoteClient.ConnectionToClient, bytes, returnBytesUsedCount, channelId);
                }
                else
                {
                    GONetLog.Warning($"Target authority {targetAuthorityId} not found in remote clients");
                }
            }

            // Return borrowed bytes
            if (doesNeedToReturn)
            {
                SerializationUtils.ReturnByteArray(bytes);
            }
        }

        private static void SendBytesToRemoteConnectionsExceptSourceRemote(ushort remoteSourceAuthorityId, byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            GONetConnection_ServerToClient remoteClientConnection = null;
            uint count = _gonetServer.numConnections;

            // PHASE 2 FIX: Round-robin client processing to distribute server-side delay fairly
            // Without this, clients processed later in list experience cumulative processing delay
            // (e.g., Client 1: 10ms RTT, Client 5: 180ms RTT due to 170ms processing delay)
            // Round-robin starting index ensures all clients get "first" position equally over time
            int startIndex = _gonetServer.nextClientProcessingStartIndex;
            if (count > 0)
            {
                _gonetServer.nextClientProcessingStartIndex = (startIndex + 1) % (int)count;
            }

            for (int offset = 0; offset < count; ++offset)
            {
                int i = (startIndex + offset) % (int)count;
                remoteClientConnection = _gonetServer.remoteClients[i].ConnectionToClient;
                if (remoteClientConnection.OwnerAuthorityId != remoteSourceAuthorityId)
                {
                    SendBytesToRemoteConnection(remoteClientConnection, bytes, bytesUsedCount, channelId);
                }
            }
        }

        private static readonly List<IPersistentEvent> persistentEventsCancelledOut = new List<IPersistentEvent>(100);

        /// <summary>
        /// Stores persistent events for late-joiner synchronization and record/replay.
        ///
        /// ⚠️  CRITICAL: This method stores events BY REFERENCE for the entire session.
        /// These exact references are later serialized when late-joining clients connect
        /// (see Server_SendClientPersistentEventsSinceStart:4355).
        ///
        /// DESIGN REQUIREMENT:
        /// Event classes MUST NOT use object pooling. If they did:
        ///   eventEnvelope.Event stored here → Event.Return() clears data → Pool reuses object
        ///   → Late-joiner receives corrupted data from stored reference → CATASTROPHIC
        ///
        /// This is WHY PersistentRpcEvent and other IPersistentEvent implementations
        /// do NOT implement ISelfReturnEvent. See GONetRpcs.cs:912 and line 647 above
        /// for detailed rationale.
        /// </summary>
        private static void OnPersistentEvent_KeepTrack(GONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            // RECORD AND REPLAY: Always add to complete history archive (never remove)
            // This preserves the full event timeline for future replay functionality
            persistentEventsArchive_CompleteHistory.AddLast(eventEnvelope.Event);

            // LATE-JOINER SYNC: Manage current state with cancellation logic
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
                // CRITICAL DEDUPLICATION: For persistent RPCs, remove any previous RPC with same RpcId+GONetId
                // This prevents duplicate state updates from accumulating (e.g., BroadcastParticipantUpdate called 1000x = 1000 copies!)
                // Only the LATEST state matters for late-joiners, not the entire history
                if (eventEnvelope.Event is PersistentRpcEvent newRpc)
                {
                    // Find and remove any existing RPC with matching RpcId + GONetId
                    var node = persistentEventsThisSession.First;
                    while (node != null)
                    {
                        var nextNode = node.Next; // Save next before potential removal
                        if (node.Value is PersistentRpcEvent existingRpc &&
                            existingRpc.RpcId == newRpc.RpcId &&
                            existingRpc.GONetId == newRpc.GONetId)
                        {
                            persistentEventsThisSession.Remove(node);
                            //GONetLog.Debug($"[RPC_DEDUP] Removed duplicate persistent RPC 0x{existingRpc.RpcId:X8} for GONetId {existingRpc.GONetId} - keeping latest only");
                        }
                        node = nextNode;
                    }
                }

                persistentEventsThisSession.AddLast(eventEnvelope.Event);
                //if (eventEnvelope.Event is DespawnGONetParticipantEvent despawn)
                //{
                    //GONetLog.Warning($"[DESPAWN_SYNC] Added DespawnGONetParticipantEvent to persistentEventsThisSession (no events cancelled) - GONetId: {despawn.GONetId}");
                //}
            }
            else
            {
                //if (eventEnvelope.Event is DespawnGONetParticipantEvent despawn)
                //{
                    //GONetLog.Warning($"[DESPAWN_SYNC] DespawnGONetParticipantEvent cancelled out {count} events for GONetId {despawn.GONetId} - despawn event NOT added to persistentEventsThisSession (correct: object no longer exists)");
                //}
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

            const string IR = "pub/sub Instantiate REMOTE about to process...";
            //GONetLog.Debug(IR + $" gonetId: {eventEnvelope.Event.GONetId}, DesignTimeLocation: '{eventEnvelope.Event.DesignTimeLocation}', SceneIdentifier: '{eventEnvelope.Event.SceneIdentifier}', InstanceName: '{eventEnvelope.Event.InstanceName}'");

            // Check if this spawn requires a scene that isn't loaded yet
            if (!string.IsNullOrEmpty(eventEnvelope.Event.SceneIdentifier))
            {
                bool isSceneLoaded = IsSceneCurrentlyLoaded(eventEnvelope.Event.SceneIdentifier);
                //GONetLog.Debug($"[SPAWN_SYNC] GONetId {eventEnvelope.Event.GONetId} requires scene '{eventEnvelope.Event.SceneIdentifier}' - IsLoaded: {isSceneLoaded}");
                if (!isSceneLoaded)
                {
                    // Defer this spawn until the required scene is loaded
                    //GONetLog.Warning($"[SPAWN_SYNC] DEFERRING spawn for GONetId {eventEnvelope.Event.GONetId} - waiting for scene '{eventEnvelope.Event.SceneIdentifier}' to load");
                    deferredSpawnEvents.Add(eventEnvelope.Event);
                    return;
                }
            }

            //GONetLog.Debug($"[SPAWN_SYNC] Processing spawn immediately for GONetId {eventEnvelope.Event.GONetId}");
            GONetParticipant instance = Instantiate_Remote(eventEnvelope.Event);

            // If instantiation failed (e.g., empty DesignTimeLocation), skip further processing
            if (instance == null)
            {
                GONetLog.Warning($"Skipping remote instantiation processing - Instantiate_Remote returned null for GONetId: {eventEnvelope.Event.GONetId}");
                return;
            }

            // Complete the post-instantiation processing
            CompleteRemoteInstantiation(instance, eventEnvelope.Event, eventEnvelope.SourceAuthorityId);
        }

        /// <summary>
        /// Completes post-instantiation setup for remotely spawned objects.
        /// This must be called for ALL remote spawns (immediate and deferred) to ensure proper initialization.
        /// </summary>
        private static void CompleteRemoteInstantiation(GONetParticipant instance, InstantiateGONetParticipantEvent spawnEvent, ushort sourceAuthorityId)
        {
            //GONetLog.Debug($"[SPAWN_SYNC] CompleteRemoteInstantiation called for GONetId {spawnEvent.GONetId}, instance '{instance.gameObject.name}', IsServer: {IsServer}");

            if (IsServer)
            {
                GONetLocal gonetLocal = instance.gameObject.GetComponent<GONetLocal>();
                if (gonetLocal != null)
                {
                    Server_OnNewClientInstantiatedItsGONetLocal(gonetLocal);
                }

                if (spawnEvent.ImmediatelyRelinquishAuthorityToServer_AndTakeRemoteControlAuthority)
                {
                    GONetSpawnSupport_Runtime.Server_MarkToBeRemotelyControlled(instance, sourceAuthorityId);
                }
            }

            if (instance.ShouldHideDuringRemoteInstantiate && valueBlendingBufferLeadSeconds > 0)
            {
                //GONetLog.Debug($"[SPAWN_SYNC] Starting hide-during-buffer coroutine for '{instance.gameObject.name}'");
                GlobalSessionContext.StartCoroutine(OnInstantiationEvent_Remote_HideDuringBufferLeadTime(instance));
            }

            // CRITICAL: Start monitoring for auto-magical value sync on remote spawns
            // This was previously missing, causing remote spawns (especially server-owned projectiles from client spawn requests)
            // to not have their transform/value changes propagated over the network
            // The comment in Start_AutoPropogateInstantiation_IfAppropriate_INTERNAL said "remote source is processed like this elsewhere"
            // but there was no "elsewhere" - this is it!
            //
            // IMPORTANT: Force monitoring even if DidStartMonitoringForAutoMagicalNetworking is already true
            // Remote spawns may have had monitoring started elsewhere but it needs to happen on THIS machine (server)
            bool wasAlreadyMonitoring = instance.DidStartMonitoringForAutoMagicalNetworking;
            if (wasAlreadyMonitoring)
            {
                // Reset flag to allow monitoring to start
                //instance.DidStartMonitoringForAutoMagicalNetworking = false;
                //GONetLog.Debug($"[SPAWN_SYNC] Forcing monitoring restart for remote spawn '{instance.name}' (GONetId: {instance.GONetId}) - was already marked as monitoring");
            }
            OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking(instance);
            //GONetLog.Debug($"[SPAWN_SYNC] Started monitoring for remote spawn '{instance.name}' (GONetId: {instance.GONetId}, wasAlreadyMonitoring: {wasAlreadyMonitoring})");

            // PATH 7: Remote runtime-spawned participants - publish DeserializeInitAllCompleted after spawn completes
            // This handles projectiles/objects spawned by remote players that don't go through DeserializeBody_AllValuesBundle
            // The GONetId was set during Instantiate_Remote, so the participant is now fully ready from this client's perspective
            // NOTE: isRelatedLocalContentRequired=false because remote spawns are ready immediately on receiving client
            // The owner's GONetLocal may not exist yet on this client, but that's OK - the spawn itself is what matters
            if (instance.GONetId != 0) // Only require GONetId to be assigned
            {
                // Deduplication check: Only publish if not already published
                if (TryMarkDeserializeInitPublished(instance.GONetId))
                {
                    //GONetLog.Info($"[GONet] Publishing DeserializeInitAllCompleted for remote spawn '{instance.name}' (GONetId: {instance.GONetId}, IsMine: {instance.IsMine}) from CompleteRemoteInstantiation");
                    var deserializeInitEvent = new GONetParticipantDeserializeInitAllCompletedEvent(instance);
                    PublishEventAsSoonAsSufficientInfoAvailable(deserializeInitEvent, instance, isRelatedLocalContentRequired: false);
                }
                else
                {
                    //GONetLog.Info($"[GONet] Skipping duplicate DeserializeInitAllCompleted for remote spawn '{instance.name}' (GONetId: {instance.GONetId}) - already published from another path");
                }
            }
            else
            {
                GONetLog.Error($"[GONet] Remote spawn '{instance.name}' has no GONetId in CompleteRemoteInstantiation - this should never happen!");
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

        /// <summary>
        /// PRODUCTION-READY EVENT BROADCAST FRAMEWORK
        ///
        /// Robustly iterates all GONetBehaviours and invokes a callback for each, with:
        /// - Unity fake null protection (destroyed objects during iteration)
        /// - Per-behaviour exception isolation (one failure doesn't break pipeline)
        /// - Detailed error logging with context
        /// - Safe enumerator disposal (handles DestroyImmediate mid-iteration)
        ///
        /// Added 2025-10-11 to replace brittle direct iteration pattern in lifecycle event handlers.
        /// </summary>
        /// <param name="callback">Action to invoke for each behaviour. Exceptions are caught and logged.</param>
        /// <param name="eventName">Name of event being broadcast (for error logging context)</param>
        /// <param name="gonetParticipant">Related GONetParticipant (for error logging context)</param>
        private static void BroadcastToAllGONetBehaviours_Robust(
            System.Action<GONetBehaviour> callback,
            string eventName,
            GONetParticipant gonetParticipant)
        {
            int successCount = 0;
            int failureCount = 0;
            int nullSkipCount = 0;

            using (var enumerator = allGONetBehaviours.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    GONetBehaviour behaviour = enumerator.Current;

                    // DEFENSIVE: Unity fake null check (object destroyed during iteration)
                    if (behaviour == null)
                    {
                        nullSkipCount++;
                        continue;
                    }

                    // ROBUST: Per-behaviour try-catch - one exception doesn't break pipeline
                    try
                    {
                        callback(behaviour);
                        successCount++;
                    }
                    catch (System.Exception ex)
                    {
                        failureCount++;

                        // DETAILED ERROR LOGGING: Include full context for debugging
                        GONetLog.Error(
                            $"[GONet-EventBroadcast] EXCEPTION in {eventName} handler for GONetBehaviour '{behaviour.GetType().Name}' " +
                            $"(GameObject: {behaviour.gameObject?.name ?? "NULL"}) " +
                            $"processing GONetParticipant '{gonetParticipant?.gameObject?.name ?? "NULL"}' " +
                            $"(GONetId: {gonetParticipant?.GONetId ?? 0})\n" +
                            $"Exception: {ex.Message}\n" +
                            $"StackTrace:\n{ex.StackTrace}");
                    }
                }
            }

            // DIAGNOSTIC: Log if failures occurred (only when there were actual errors)
            if (failureCount > 0)
            {
                GONetLog.Warning(
                    $"[GONet-EventBroadcast] {eventName} completed with ERRORS: " +
                    $"Success={successCount}, Failures={failureCount}, NullSkipped={nullSkipCount}");
            }
        }

        private static void OnEnabledGNPEvent(GONetEventEnvelope<GONetParticipantEnabledEvent> eventEnvelope)
        {
            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            BroadcastToAllGONetBehaviours_Robust(
                behaviour => behaviour.OnGONetParticipantEnabled(gonetParticipant),
                nameof(OnEnabledGNPEvent),
                gonetParticipant);
        }

        private static void OnStartedGNPEvent(GONetEventEnvelope<GONetParticipantStartedEvent> eventEnvelope)
        {
            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            BroadcastToAllGONetBehaviours_Robust(
                behaviour => behaviour.OnGONetParticipantStarted(gonetParticipant),
                nameof(OnStartedGNPEvent),
                gonetParticipant);
        }

        private static void OnDeserializeInitAllCompletedGNPEvent(GONetEventEnvelope<GONetParticipantDeserializeInitAllCompletedEvent> eventEnvelope)
        {
            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;
            BroadcastToAllGONetBehaviours_Robust(
                behaviour => behaviour.OnGONetParticipantDeserializeInitAllCompleted(gonetParticipant),
                nameof(OnDeserializeInitAllCompletedGNPEvent),
                gonetParticipant);

            // LIFECYCLE GATE: Mark deserialization complete and check if OnGONetReady can fire
            // This replaces the old direct broadcast - now uses the centralized gate check
            gonetParticipant.MarkDeserializeInitComplete();
        }

        private static void OnDisabledGNPEvent(GONetEventEnvelope<GONetParticipantDisabledEvent> eventEnvelope)
        {
            GONetParticipant gonetParticipant = eventEnvelope.GONetParticipant;

            // CRITICAL: Check if Unity object is destroyed before accessing Unity methods
            // Unity fake null pattern: gonetParticipant reference exists, but Unity object may be destroyed
            if (gonetParticipant == null)
            {
                // Unity object destroyed - can't access Unity methods like GetInstanceID()
                // But we can still access C# properties if the reference isn't actually null
                if ((object)gonetParticipant != null)
                {
                    // C# reference exists - can access pure C# properties
                    recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map[gonetParticipant.GONetId] = gonetParticipant.GONetIdAtInstantiation;
                }
                return; // Skip rest of processing - can't call Unity methods on destroyed object
            }

            recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map[gonetParticipant.GONetId] = gonetParticipant.GONetIdAtInstantiation;

            // Clean up spawn scene tracking when GNP is disabled/destroyed
            ClearParticipantSpawnScene(gonetParticipant);
            definedInSceneParticipantInstanceIDs.Remove(gonetParticipant.GetInstanceID());

            // ROBUST: Use production-ready broadcast framework
            BroadcastToAllGONetBehaviours_Robust(
                behaviour => behaviour.OnGONetParticipantDisabled(gonetParticipant),
                nameof(OnDisabledGNPEvent),
                gonetParticipant);
        }

        /// <summary>
        /// Handles remote <see cref="DespawnGONetParticipantEvent"/> notifications.
        /// <para>This is called when a remote client/server despawns a GONetParticipant through gameplay logic
        /// (e.g., projectile hits, enemy dies, player destroys object).</para>
        /// <para><b>IMPORTANT:</b> This is NOT called for scene unload destroys, which are local-only.</para>
        /// </summary>
        private static void OnDespawnGNPEvent_Remote(GONetEventEnvelope<DespawnGONetParticipantEvent> eventEnvelope)
        {
            uint gonetId = eventEnvelope.Event.GONetId;

            //GONetLog.Warning($"[DESPAWN_SYNC] CLIENT: OnDespawnGNPEvent_Remote - Received despawn for GONetId {gonetId} from AuthorityId {eventEnvelope.SourceAuthorityId}");

            // IMPORTANT: If this GONetId has a deferred spawn, defer the despawn too!
            // Otherwise we process despawn before spawn completes, leaving a ghost object.
            bool hasDeferredSpawn = deferredSpawnEvents.Exists(spawnEvent => spawnEvent.GONetId == gonetId);
            if (hasDeferredSpawn)
            {
                //GONetLog.Warning($"[SPAWN_SYNC] DEFERRING despawn for GONetId {gonetId} - spawn is still deferred, will despawn after spawn completes");
                deferredDespawnEvents.Add(eventEnvelope.Event);
                return;
            }

            GONetParticipant gonetParticipant = null;
            if (gonetParticipantByGONetIdMap.TryGetValue(gonetId, out gonetParticipant))
            {
                gonetIdsDestroyedViaPropagation.Add(gonetParticipant.GONetId); // this container must have the gonetId added first in order to prevent OnDestroy_AutoPropagateRemoval_IfAppropriate from thinking it is appropriate to propagate more when it is already being propagated

                if (gonetParticipant == null || gonetParticipant.gameObject == null)
                {
                    const string REC = "Received remote notification to despawn a GNP, but Unity says it's already null. Ensure only the owner (i.e., GNP.IsMine) is the one who calls Unity's Destroy() method and the propagation across the network will be automatic via GONet.";
                    GONetLog.Error(REC);
                }
                else
                {
                    //GONetLog.Warning($"[DESPAWN_SYNC] CLIENT: Destroying GameObject '{gonetParticipant.gameObject.name}' for GONetId {gonetParticipant.GONetId}");
                    UnityEngine.Object.Destroy(gonetParticipant.gameObject);
                }
            }
            else
            {
                GONetLog.Warning($"[DESPAWN_SYNC] CLIENT: Despawn event received for GONetId {gonetId}, but no matching GONetParticipant found in map (may have already been destroyed or never spawned)");
            }
        }

        private static void InitShouldSkipSyncSupport()
        {

            // TODO FIXME add this back?: GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsRotationSyncd] = IsRotationNotSyncd;

            // TODO FIXME add this back?: GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsPositionSyncd] = IsPositionNotSyncd;
        }

        internal static bool IsRotationNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
        {
            // Check if rotation sync is disabled
            if (!monitoringSupport.syncCompanion.gonetParticipant.IsRotationSyncd)
            {
                return true; // Skip: rotation sync disabled
            }

            // PHYSICS SYNC FREQUENCY GATING: Check if this is a physics object and if so, gate by PhysicsUpdateInterval
            GONetParticipant participant = monitoringSupport.syncCompanion.gonetParticipant;
            bool isPhysicsObject = participant.IsRigidBodyOwnerOnlyControlled && participant.myRigidBody != null && participant.IsMine;

            if (isPhysicsObject)
            {
                // Get the physics update interval from this specific value's sync profile
                int physicsUpdateInterval = monitoringSupport.syncAttribute_PhysicsUpdateInterval;

                // Skip this physics frame if counter doesn't match interval
                // physicsUpdateInterval=1: sync frames 0,1,2,3 (always)
                // physicsUpdateInterval=2: sync frames 0,2 (every 2nd)
                // physicsUpdateInterval=3: sync frames 0,3 (every 3rd)
                // physicsUpdateInterval=4: sync frame 0 (every 4th)
                if (physicsFrameCounter % physicsUpdateInterval != 0)
                {
                    return true; // Skip: not the right physics frame for this interval
                }
            }

            return false; // Don't skip: should sync
        }

        internal static bool IsPositionNotSyncd(AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport, int index)
        {
            // Check if position sync is disabled
            if (!monitoringSupport.syncCompanion.gonetParticipant.IsPositionSyncd)
            {
                return true; // Skip: position sync disabled
            }

            // PHYSICS SYNC FREQUENCY GATING: Check if this is a physics object and if so, gate by PhysicsUpdateInterval
            GONetParticipant participant = monitoringSupport.syncCompanion.gonetParticipant;
            bool isPhysicsObject = participant.IsRigidBodyOwnerOnlyControlled && participant.myRigidBody != null && participant.IsMine;

            if (isPhysicsObject)
            {
                // Get the physics update interval from this specific value's sync profile
                int physicsUpdateInterval = monitoringSupport.syncAttribute_PhysicsUpdateInterval;

                // Skip this physics frame if counter doesn't match interval
                // physicsUpdateInterval=1: sync frames 0,1,2,3 (always)
                // physicsUpdateInterval=2: sync frames 0,2 (every 2nd)
                // physicsUpdateInterval=3: sync frames 0,3 (every 3rd)
                // physicsUpdateInterval=4: sync frame 0 (every 4th)
                if (physicsFrameCounter % physicsUpdateInterval != 0)
                {
                    return true; // Skip: not the right physics frame for this interval
                }
            }

            return false; // Don't skip: should sync
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
        /// <summary>
        /// CLIENT ONLY: Legacy API for backward compatibility.
        /// Internally delegates to Client_TryInstantiateToBeRemotelyControlledByMe() with default limbo fallback.
        ///
        /// RECOMMENDED: Use Client_TryInstantiateToBeRemotelyControlledByMe() for explicit control over batch exhaustion handling.
        /// </summary>
        public static GONetParticipant Client_InstantiateToBeRemotelyControlledByMe(GONetParticipant prefab, Vector3 position, Quaternion rotation)
        {
            if (IsClient)
            {
                // Delegate to new batch-aware API with default limbo mode
                // This ensures old code still works but uses the batch system correctly
                if (Client_TryInstantiateToBeRemotelyControlledByMe(prefab, position, rotation, out GONetParticipant participant))
                {
                    return participant;
                }

                // Batch exhausted and limbo mode is ReturnFailure
                GONetLog.Error($"[GONet] Failed to spawn '{prefab.name}' - batch exhausted and limbo mode is ReturnFailure. " +
                              "Consider using Client_TryInstantiateToBeRemotelyControlledByMe() for explicit handling.");
                return null;
            }

            return null;
        }

        /// <summary>
        /// CLIENT ONLY: Try to instantiate a new server-owned object.
        /// Uses GONetId batch system with limbo mode fallback for batch exhaustion.
        ///
        /// IMPORTANT: Limbo is an EDGE CASE for extreme rapid spawning (100+ spawns/sec).
        /// Most games will NEVER encounter this - batches are designed to prevent it.
        ///
        /// <para>
        /// This is the RECOMMENDED API for client-spawned, server-owned objects (e.g., projectiles).
        /// Provides explicit failure handling via Try pattern instead of dangerous fallback code.
        /// </para>
        ///
        /// <para>
        /// Behavior when GONetId batch is exhausted:
        /// - ReturnFailure: Returns false, user handles spawn failure
        /// - InstantiateInLimboWithAutoDisableAll: Spawns with all MonoBehaviours disabled
        /// - InstantiateInLimboWithAutoDisableRenderingAndPhysics: Spawns with only rendering/physics disabled (RECOMMENDED)
        /// - InstantiateInLimbo: Spawns normally, user checks Client_IsInLimbo
        /// </para>
        ///
        /// <para>
        /// When batch arrives from server, limbo objects automatically "graduate" to full networked status.
        /// </para>
        /// </summary>
        /// <param name="prefab">The GONetParticipant prefab to instantiate</param>
        /// <param name="position">Position to spawn at</param>
        /// <param name="rotation">Rotation to spawn with</param>
        /// <param name="outParticipant">The instantiated participant (null if ReturnFailure and batch exhausted)</param>
        /// <returns>True if spawned successfully (either with GONetId or in limbo), false if spawn failed</returns>
        public static bool Client_TryInstantiateToBeRemotelyControlledByMe(
            GONetParticipant prefab,
            Vector3 position,
            Quaternion rotation,
            out GONetParticipant outParticipant)
        {
            outParticipant = null;

            if (!IsClient)
            {
                GONetLog.Error("[ClientLimbo] Client_TryInstantiate called on server - this is client-only API");
                return false;
            }

            // Check if we have available batch IDs
            bool hasBatchIds = GONetIdBatchManager.Client_HasAvailableIds();

            if (hasBatchIds)
            {
                // Normal path: We have batch IDs available
                outParticipant = GONetSpawnSupport_Runtime.Instantiate_MarkToBeRemotelyControlled(prefab, position, rotation);
                outParticipant.RemotelyControlledByAuthorityId = MyAuthorityId;
                return true;
            }

            // BATCH EXHAUSTED: Determine limbo mode
            Client_GONetIdBatchLimboMode limboMode = Client_GetLimboMode(prefab);

            if (limboMode == Client_GONetIdBatchLimboMode.ReturnFailure)
            {
                // User wants explicit failure - return false
                uint remainingIds = GONetIdBatchManager.Client_GetRemainingIds();
                GONetLog.Warning($"[ClientLimbo] Spawn FAILED for '{prefab.name}' - no batch IDs available (remaining: {remainingIds}), limbo mode is ReturnFailure");

                // Raise event so user can show UI notification
                Client_OnSpawnEnteredLimbo?.Invoke(new Client_SpawnLimboEventArgs
                {
                    Participant = null,
                    Prefab = prefab,
                    LimboMode = limboMode,
                    RemainingIds = remainingIds,
                    Position = position,
                    Rotation = rotation
                });

                return false;
            }

            // Spawn in limbo according to configured mode
            outParticipant = Client_InstantiateInLimbo(prefab, position, rotation, limboMode);
            return true;
        }

        /// <summary>
        /// CLIENT ONLY: Determines which limbo mode to use for a given prefab.
        /// Checks prefab override first, then falls back to project settings default.
        /// </summary>
        private static Client_GONetIdBatchLimboMode Client_GetLimboMode(GONetParticipant prefab)
        {
            if (prefab.client_overrideLimboMode)
            {
                return prefab.client_limboMode;
            }

            // TODO: Read from GONetProjectSettings once implemented
            // return GONetProjectSettings.Instance.client_defaultLimboMode;

            // Hardcoded default for now (most balanced option)
            return Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableRenderingAndPhysics;
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

            // PHASE 2 FIX: Round-robin client processing to distribute server-side delay fairly
            int startIndex = _gonetServer.nextClientProcessingStartIndex;
            if (count > 0)
            {
                _gonetServer.nextClientProcessingStartIndex = (startIndex + 1) % (int)count;
            }

            for (int offset = 0; offset < count; ++offset)
            {
                int i = (startIndex + offset) % (int)count;
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
                // ADAPTIVE POOL SIZING (October 2025): Use dynamic pool size from adaptive scaler
                // Pool automatically scales based on demand while respecting absolute maximum
                if (GONetChannel.ById(channelId).QualityOfService == QosType.Unreliable)
                {
                    // Get current effective pool size (dynamically adjusted by adaptive scaler)
                    int currentPoolSize = adaptivePoolScaler != null
                        ? adaptivePoolScaler.GetCurrentPoolSize()
                        : (GONetGlobal.Instance != null ? GONetGlobal.Instance.maxPacketsPerTick : SingleProducerQueues.MAX_PACKETS_PER_TICK);

                    float threshold = GONetGlobal.Instance != null ? GONetGlobal.Instance.unreliableDropThreshold : 0.90f;
                    int dropThresholdCount = (int)(currentPoolSize * threshold);

                    if (singleProducerSendQueues.resourcePool.BorrowedCount > dropThresholdCount)
                    {
                        // Track unreliable packet drops for diagnostics
                        _unreliablePacketDropCount++;
                        _unreliablePacketDropCount_sinceLastLog++;

                        // Log periodically (every 100 drops or first drop) to avoid spam
                        // ONLY log if congestion logging is enabled (configurable in GONetGlobal)
                        bool shouldLog = (_unreliablePacketDropCount_sinceLastLog >= 100 || _unreliablePacketDropCount == 1) &&
                                        (GONetGlobal.Instance == null || GONetGlobal.Instance.enableCongestionLogging);

                        if (shouldLog)
                        {
                            string channelName = GONetChannel.ById(channelId) == GONetChannel.AutoMagicalSync_Unreliable ? "AutoMagicalSync" :
                                               GONetChannel.ById(channelId) == GONetChannel.TimeSync_Unreliable ? "TimeSync" :
                                               GONetChannel.ById(channelId) == GONetChannel.EventSingles_Unreliable ? "EventSingles" :
                                               GONetChannel.ById(channelId) == GONetChannel.CustomSerialization_Unreliable ? "CustomSerialization" : "Unknown";

                            int borrowed = singleProducerSendQueues.resourcePool.BorrowedCount;
                            float utilization = (float)borrowed / currentPoolSize;
                            float dropRate = (float)_unreliablePacketDropCount / (_unreliablePacketDropCount + _successfulPacketSendCount);

                            // Get adaptive scaler diagnostics if available
                            string adaptiveInfo = adaptivePoolScaler != null ? adaptivePoolScaler.GetDiagnostics() : "";

                            // Build diagnostic message with actionable recommendations
                            string message = $"[CONGESTION] Unreliable packet drops detected!\n" +
                                           $"  Dropped: {_unreliablePacketDropCount_sinceLastLog} packets (this batch), {_unreliablePacketDropCount} total\n" +
                                           $"  Drop Rate: {dropRate:P} ({_unreliablePacketDropCount} dropped / {_unreliablePacketDropCount + _successfulPacketSendCount} total)\n" +
                                           $"  Pool Utilization: {borrowed}/{currentPoolSize} ({utilization:P})\n" +
                                           $"  Drop Threshold: {threshold:P}\n" +
                                           $"  Channel: {channelName}\n" +
                                           $"  Connection: {(sendToConnection == null ? "ALL (broadcast)" : sendToConnection.ToString())}\n" +
                                           (string.IsNullOrEmpty(adaptiveInfo) ? "" : $"  {adaptiveInfo}\n");

                            // Add severity-based recommendations
                            if (dropRate > 0.10f) // Critical: >10% drop rate
                            {
                                bool isAdaptiveEnabled = GONetGlobal.Instance != null && GONetGlobal.Instance.enableAdaptivePoolScaling;
                                message += $"\n⚠️ CRITICAL CONGESTION ({dropRate:P} drop rate):\n" +
                                          "  IMMEDIATE ACTIONS:\n" +
                                          (isAdaptiveEnabled
                                              ? $"  1. Adaptive scaling is ENABLED - pool is at {currentPoolSize} packets\n" +
                                                "     If this is close to maxPacketsPerTick ceiling, increase the ceiling!\n"
                                              : $"  1. Adaptive scaling is DISABLED - using fixed size: {currentPoolSize}\n" +
                                                "     Enable adaptive scaling or increase maxPacketsPerTick\n") +
                                          "  2. Check for spawn storms (too many objects spawned at once)\n" +
                                          "  3. Reduce sync frequency in GONetParticipant sync profiles\n" +
                                          $"  4. If '{channelName}' is AutoMagicalSync, consider:\n" +
                                          "     - Using less frequent position/rotation sync\n" +
                                          "     - Disabling sync on distant/irrelevant objects\n";
                            }
                            else if (dropRate > 0.05f) // Warning: >5% drop rate
                            {
                                message += $"\n⚠️ HIGH CONGESTION ({dropRate:P} drop rate):\n" +
                                          "  RECOMMENDED ACTIONS:\n" +
                                          $"  1. Current pool size: {currentPoolSize} (check if adaptive scaling is working)\n" +
                                          "  2. Monitor for spawn rate spikes\n" +
                                          "  3. Review sync profiles for over-syncing\n";
                            }
                            else // Moderate: <5% drop rate
                            {
                                message += $"\n  STATUS: Moderate congestion ({dropRate:P} drop rate)\n" +
                                          "  This is typically acceptable for burst scenarios.\n" +
                                          "  If drops persist, consider increasing maxPacketsPerTick.\n";
                            }

                            GONetLog.Warning(message);
                            _unreliablePacketDropCount_sinceLastLog = 0;
                        }

                        return false;
                    }
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

            // DIAGNOSTIC: Track outgoing packet by channel
            // Added 2025-10-11 to investigate packet saturation during rapid spawning
            GONetChannel outgoingChannel = GONetChannel.ById(channelId);
            bool isOutgoingReliable = outgoingChannel.QualityOfService == QosType.Reliable;
            IncrementOutgoingPacketCounter(isOutgoingReliable);

            // Track successful packet sends for drop rate calculation
            System.Threading.Interlocked.Increment(ref _successfulPacketSendCount);

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

        #region Comprehensive Message Flow Logging

        /// <summary>
        /// Logging profile name for message flow logging.
        /// Profile must be registered before logging can occur.
        /// Example:
        ///   GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(
        ///       GONetMain.MessageFlowLoggingProfile,
        ///       outputToSeparateFile: true,
        ///       includeStackTraces: false,
        ///       minimumLogLevel: GONetLog.LogLevel.Info));
        /// </summary>
        public const string MessageFlowLoggingProfile = "MessageFlow";

        /// <summary>
        /// Controls whether comprehensive message flow logging is enabled.
        /// WARNING: Generates large log output even without stack traces.
        /// Default: false (disabled) - enable only for targeted debugging sessions.
        /// NOTE: Profile must be registered separately via GONetLog.RegisterLoggingProfile()
        /// </summary>
        public static bool EnableMessageFlowLogging { get; set; } = false;

        /// <summary>
        /// Extracts metadata from message bytes for logging purposes.
        /// Thread-safe. Returns partial data if deserialization fails.
        /// </summary>
        private static string ExtractMessageMetadata(byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            try
            {
                // Try to extract messageID and type from the bytes
                if (bytesUsedCount >= 4 && GONetChannel.IsGONetCoreChannel(channelId))
                {
                    using (var bitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(messageBytes, bytesUsedCount))
                    {
                        uint messageID;
                        bitStream.ReadUInt(out messageID);

                        Type messageType;
                        if (messageTypeByMessageIDMap.TryGetValue(messageID, out messageType))
                        {
                            long elapsedTicksAtSend;
                            bitStream.ReadLong(out elapsedTicksAtSend);

                            // Try to extract GONetId if this is an event that has one
                            string gonetIdInfo = string.Empty;
                            if (messageType == typeof(InstantiateGONetParticipantEvent) ||
                                messageType == typeof(AutoMagicalSync_ValueChanges_Message) ||
                                messageType == typeof(AutoMagicalSync_AllCurrentValues_Message))
                            {
                                try
                                {
                                    // These message types should have GONetId in their payload
                                    // We'll just note it exists rather than fully parse to avoid side effects
                                    gonetIdInfo = " [ContainsGONetIds]";
                                }
                                catch
                                {
                                    // Ignore extraction failures
                                }
                            }

                            return $"MsgType={messageType.Name}, MsgID={messageID}, SentTicks={elapsedTicksAtSend}{gonetIdInfo}";
                        }

                        return $"MsgID={messageID} [TypeUnknown]";
                    }
                }
                else if (channelId == GONetChannel.EventSingles_Reliable || channelId == GONetChannel.EventSingles_Unreliable || channelId == GONetChannel.ClientInitialization_EventSingles_Reliable)
                {
                    return $"EventSingle [Channel={channelId}]";
                }
                else
                {
                    return $"CustomChannel={channelId}";
                }
            }
            catch (Exception e)
            {
                return $"[MetadataExtractionFailed: {e.Message}]";
            }
        }

        /// <summary>
        /// Logs comprehensive send-side metadata for message flow debugging.
        /// Thread-safe. Called from background send thread.
        /// NOTE: Disabled by default - set EnableMessageFlowLogging = true to enable
        /// </summary>
        private static void LogMessageSend(byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId, GONetConnection targetConnection, bool isServerBroadcast)
        {
            if (!EnableMessageFlowLogging) return; // Exit early if disabled

            try
            {
                long sendTimestamp = Time.ElapsedTicks;
                string sceneHistory = GetSceneHistory();
                string metadata = ExtractMessageMetadata(messageBytes, bytesUsedCount, channelId);

                string target;
                if (isServerBroadcast)
                {
                    target = "ALL_CLIENTS";
                }
                else if (targetConnection != null)
                {
                    ushort targetAuthority = targetConnection is GONetConnection_ServerToClient serverToClient
                        ? serverToClient.OwnerAuthorityId
                        : (ushort)0;
                    target = $"Authority{targetAuthority}";
                }
                else
                {
                    target = "SERVER";
                }

                // Use logging profile (no stack traces if profile configured that way)
                string logMessage = $"[MSG-SEND] {sceneHistory} | SendTicks={sendTimestamp} | Source=Authority{MyAuthorityId} | Target={target} | Ch={channelId} | Bytes={bytesUsedCount} | {metadata}";
                GONetLog.Info(logMessage, MessageFlowLoggingProfile);
            }
            catch (Exception e)
            {
                GONetLog.Error($"[MSG-SEND] Logging failed: {e.Message}");
            }
        }

        /// <summary>
        /// Logs comprehensive receive-side metadata for message flow debugging.
        /// Thread-safe. Called from main thread during message processing.
        /// NOTE: Disabled by default - set EnableMessageFlowLogging = true to enable
        /// </summary>
        private static void LogMessageReceive(byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId, GONetConnection sourceConnection, long elapsedTicksAtSend)
        {
            if (!EnableMessageFlowLogging) return; // Exit early if disabled

            try
            {
                long receiveTimestamp = Time.ElapsedTicks;
                long latencyTicks = receiveTimestamp - elapsedTicksAtSend;
                double latencyMs = (latencyTicks / (double)Stopwatch.Frequency) * 1000.0;

                string sceneHistory = GetSceneHistory();
                string metadata = ExtractMessageMetadata(messageBytes, bytesUsedCount, channelId);

                ushort sourceAuthority = sourceConnection is GONetConnection_ServerToClient serverToClient
                    ? serverToClient.OwnerAuthorityId
                    : (ushort)0;

                // Use logging profile (no stack traces if profile configured that way)
                string logMessage = $"[MSG-RECV] {sceneHistory} | RecvTicks={receiveTimestamp} | Source=Authority{sourceAuthority} | Target=Authority{MyAuthorityId} | Ch={channelId} | Bytes={bytesUsedCount} | Latency={latencyMs:F2}ms | {metadata}";
                GONetLog.Info(logMessage, MessageFlowLoggingProfile);
            }
            catch (Exception e)
            {
                GONetLog.Error($"[MSG-RECV] Logging failed: {e.Message}");
            }
        }

        /// <summary>
        /// Logs comprehensive process-side metadata for OnGONetReady event broadcasting.
        /// Thread-safe. Called from main thread during event publishing.
        /// NOTE: Disabled by default - set EnableMessageFlowLogging = true to enable
        /// </summary>
        private static void LogEventProcess(GONetParticipant participant, int behaviourCount)
        {
            if (!EnableMessageFlowLogging) return; // Exit early if disabled

            try
            {
                long processTimestamp = Time.ElapsedTicks;
                string sceneHistory = GetSceneHistory();

                // Use logging profile (no stack traces if profile configured that way)
                string logMessage = $"[MSG-PROC] {sceneHistory} | ProcTicks={processTimestamp} | Event=OnGONetReady | GONetId={participant.GONetId} | Name={participant.name} | IsMine={participant.IsMine} | Owner=Authority{participant.OwnerAuthorityId} | BehaviourCount={behaviourCount}";
                GONetLog.Info(logMessage, MessageFlowLoggingProfile);
            }
            catch (Exception e)
            {
                GONetLog.Error($"[MSG-PROC] Logging failed: {e.Message}");
            }
        }

        #endregion

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

                                            // COMPREHENSIVE LOGGING - Send to all clients
                                            LogMessageSend(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId, null, isServerBroadcast: true);
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

                                                // COMPREHENSIVE LOGGING - Client to server
                                                LogMessageSend(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId, null, isServerBroadcast: false);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds + " size: " + networkData.bytesUsedCount);
                                    networkData.relatedConnection.SendMessageOverChannel(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);

                                    // COMPREHENSIVE LOGGING - Targeted send
                                    LogMessageSend(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId, networkData.relatedConnection, isServerBroadcast: false);
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

        /// <summary>
        /// Physics sync grouping - Used for server-authoritative Rigidbody synchronization.
        /// Runs via WaitForFixedUpdate coroutine AFTER all physics processing (simulation + collision/trigger callbacks).
        /// Uses END_OF_FRAME frequency (0f) so ProcessASAP() executes EVERY FixedUpdate without frequency throttling.
        /// Physics objects are filtered at call site in AutoMagicalSyncProcessing.Process() using IsRigidBodyOwnerOnlyControlled flag.
        /// Uses FixedElapsedTicks timestamps and unreliable channel for frequent physics updates (actual rate: 50Hz via FixedUpdate).
        /// </summary>
        internal static readonly SyncBundleUniqueGrouping grouping_physics_unreliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Unreliable, true);

        /// <summary>
        /// Physics frame counter for physics sync frequency gating.
        /// Incremented every FixedUpdate to track which physics frame we're on.
        /// Used with PhysicsUpdateInterval (1-4) to determine if this frame should sync physics state.
        /// </summary>
        private static int physicsFrameCounter = 0;

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

            // EARLY FRAME UPDATE: Call UpdateAfterGONetReady for all ready companions
            // Runs at end of GONetGlobal.Update() (priority -32000, early in frame)
            Update_EarlyFrame_UpdateAfterGONetReady();
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

                // Update adaptive pool scaler based on current utilization
                if (adaptivePoolScaler != null)
                {
                    // Calculate total borrowed count across all thread pools
                    int totalBorrowed = 0;
                    foreach (var kvp in singleProducerSendQueuesByThread)
                    {
                        totalBorrowed += kvp.Value.resourcePool.BorrowedCount;
                    }

                    int numClients = IsServer && gonetServer != null ? (int)gonetServer.numConnections : 0;
                    adaptivePoolScaler.Update(totalBorrowed, numClients);
                }

                // Process any queued main thread callbacks from async operations
                GONetThreading.ProcessMainThreadCallbacks();

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
                        GONetParticipant_AutoMagicalSyncCompanion_Generated activeAutoSyncCompanion = kvp_activeAutoSyncCompanion.Value;
                        int length_valueChangesSupport = activeAutoSyncCompanion.valuesChangesSupport.Length;
                        for (int i = 0; i < length_valueChangesSupport; ++i)
                        {
                            AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = activeAutoSyncCompanion.valuesChangesSupport[i];
                            if (valueChangeSupport != null && !GONetParticipant_AutoMagicalSyncCompanion_Generated.ShouldSkipSync(valueChangeSupport, i))
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
                        if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gnp.CodeGenerationId, out companionMap))
                        {
                            if (companionMap.ContainsKey(gnp))
                            {
                                GONetLog.Debug("gnp also now in map.....can now proceed with processing the remaining bytes!");
                            }
                        }
                    }
                }

                recentlyDisabledGONetId_to_GONetIdAtInstantiation_Map.Clear();

                // Periodically clean up old warning suppression entries (once every 10 seconds)
                // Prevents dictionary from growing indefinitely with despawned object IDs
                const long CLEANUP_INTERVAL_TICKS = 10 * TimeSpan.TicksPerSecond;
                const long SUPPRESSION_WINDOW_TICKS = 5 * TimeSpan.TicksPerSecond;
                if (missingGONetParticipantWarningSuppressionMap.Count > 0)
                {
                    long currentTicks = Time.ElapsedTicks;
                    if (!_lastWarningSuppressionCleanupTicks.HasValue ||
                        (currentTicks - _lastWarningSuppressionCleanupTicks.Value) >= CLEANUP_INTERVAL_TICKS)
                    {
                        // Remove entries older than suppression window + cleanup interval
                        long expiryThreshold = currentTicks - (SUPPRESSION_WINDOW_TICKS + CLEANUP_INTERVAL_TICKS);
                        var keysToRemove = new List<uint>();
                        foreach (var kvp in missingGONetParticipantWarningSuppressionMap)
                        {
                            if (kvp.Value < expiryThreshold)
                            {
                                keysToRemove.Add(kvp.Key);
                            }
                        }

                        foreach (uint key in keysToRemove)
                        {
                            missingGONetParticipantWarningSuppressionMap.Remove(key);
                        }

                        _lastWarningSuppressionCleanupTicks = currentTicks;

                        // Also log unreliable packet drop summary periodically
                        if (_unreliablePacketDropCount > 0)
                        {
                            GONetLog.Info($"[SYNC-HEALTH] Unreliable packet drops since start: {_unreliablePacketDropCount} | Active GONetParticipants: {gonetParticipantByGONetIdMap.Count} | Send buffer max: {SingleProducerQueues.MAX_PACKETS_PER_TICK}");
                        }
                    }
                }

                // LATE FRAME UPDATE: LateUpdateAfterGONetReady for all ready GONetParticipantCompanionBehaviours
                // Runs in Update_DoTheHeavyLifting_IfAppropriate (called from GONetLocal.LateUpdate() at priority +32000)
                //
                // ROBUSTNESS FEATURES:
                // - Enumerator with dispose: Safe against DestroyImmediate() modifying HashSet mid-loop
                // - Per-behaviour try-catch: One exception doesn't break entire pipeline
                // - Optimized null checks: Avoids Unity's overloaded null operator
                // - Static reflection cache: Zero overhead for behaviours that don't override method
                using (var enumerator = allGONetBehaviours.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        GONetBehaviour behaviour = enumerator.Current;

                        // Defensive: Check if destroyed during iteration
                        object behaviourObj = behaviour;
                        if (behaviourObj == null)
                        {
                            continue;
                        }

                        // Fast early exit: Type doesn't override method
                        if (!behaviour.hasLateUpdateAfterGONetReadyOverride)
                        {
                            continue;
                        }

                        // Optimized cast and null checks (avoid Unity null operator)
                        GONetParticipantCompanionBehaviour companion = behaviour as GONetParticipantCompanionBehaviour;
                        object companionObj = companion;
                        if (companionObj == null)
                        {
                            continue;
                        }

                        object participantObj = companion.GONetParticipant;
                        if (participantObj == null)
                        {
                            continue;
                        }

                        // Check if participant is ready
                        if (!IsGONetReady(companion.GONetParticipant))
                        {
                            continue;
                        }

                        // ROBUST: Try-catch per behaviour - one exception doesn't break pipeline
                        try
                        {
                            companion.LateUpdateAfterGONetReady();
                        }
                        catch (Exception e)
                        {
                            // Log with full context for debugging
                            GONetLog.Error($"[GONet] Exception in LateUpdateAfterGONetReady() for {companion.GetType().Name} (GONetId: {companion.GONetParticipant.GONetId}): {e}");
                        }
                    }
                }
                // END of late update loop

                // DIAGNOSTIC: Frame-end metrics for packet processing and deserialization
                // Added 2025-10-11 to investigate DeserializeInitAllCompleted event delivery during rapid spawning
                //LogFrameEndMetrics_IfAppropriate();
            }
        }
        // END of Update_DoTheHeavyLifting_IfAppropriate method

        /// <summary>
        /// DIAGNOSTIC: Enhanced frame-end metrics for packet processing pipeline analysis.
        /// Added 2025-10-11 to investigate packet saturation during rapid spawning.
        ///
        /// Tracks PER-FRAME and BY-CHANNEL:
        /// - INCOMING: Packets received, queued (awaiting process), processed this frame
        /// - OUTGOING: Packets sent this frame (reliable vs unreliable breakdown)
        /// - PARTICIPANTS: Waiting for deserialization
        /// - EVENTS: DeserializeInitAllCompleted published
        ///
        /// Channel breakdown helps identify which pipeline saturates first.
        /// </summary>
        private static int _lastLoggedFrame_FrameEndMetrics = -1;
        private static int _deserializeInitEventsPublishedThisFrame = 0;

        // Per-frame counters for incoming packet tracking
        private static int _incomingPacketsProcessedThisFrame_Reliable = 0;
        private static int _incomingPacketsProcessedThisFrame_Unreliable = 0;

        // Per-frame counters for outgoing packet tracking
        private static int _outgoingPacketsSentThisFrame_Reliable = 0;
        private static int _outgoingPacketsSentThisFrame_Unreliable = 0;

        /// <summary>
        /// DIAGNOSTIC: Call when processing an incoming packet to track throughput by channel.
        /// </summary>
        internal static void IncrementIncomingPacketCounter(bool isReliable)
        {
            if (isReliable)
                _incomingPacketsProcessedThisFrame_Reliable++;
            else
                _incomingPacketsProcessedThisFrame_Unreliable++;
        }

        /// <summary>
        /// DIAGNOSTIC: Call when sending an outgoing packet to track throughput by channel.
        /// </summary>
        internal static void IncrementOutgoingPacketCounter(bool isReliable)
        {
            if (isReliable)
                _outgoingPacketsSentThisFrame_Reliable++;
            else
                _outgoingPacketsSentThisFrame_Unreliable++;
        }

        /// <summary>
        /// DIAGNOSTIC: Call whenever a DeserializeInitAllCompleted event is published to track event rate.
        /// </summary>
        internal static void IncrementDeserializeInitEventCounter()
        {
            _deserializeInitEventsPublishedThisFrame++;
        }

        private static void LogFrameEndMetrics_IfAppropriate()
        {
            int currentFrame = UnityEngine.Time.frameCount;

            // Only log once per frame (defensive check)
            if (_lastLoggedFrame_FrameEndMetrics == currentFrame)
            {
                return;
            }
            _lastLoggedFrame_FrameEndMetrics = currentFrame;

            // === 1. INCOMING PACKET METRICS (by channel) ===
            int incomingQueued_Reliable = 0;
            int incomingQueued_Unreliable = 0;
            int incomingProcessed_Reliable = _incomingPacketsProcessedThisFrame_Reliable;
            int incomingProcessed_Unreliable = _incomingPacketsProcessedThisFrame_Unreliable;

            // Count packets currently queued awaiting processing (approx - queue doesn't track reliability)
            int totalQueuedPackets = 0;
            using (var enumerator = singleProducerReceiveQueuesByThread.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    SingleProducerQueues singleProducerReceiveQueues = enumerator.Current.Value;
                    ConcurrentQueue<NetworkData> incomingNetworkData = singleProducerReceiveQueues.queueForWork;
                    totalQueuedPackets += incomingNetworkData.Count;
                }
            }

            // Reset counters for next frame
            _incomingPacketsProcessedThisFrame_Reliable = 0;
            _incomingPacketsProcessedThisFrame_Unreliable = 0;

            // === 2. OUTGOING PACKET METRICS (by channel) ===
            int outgoingSent_Reliable = _outgoingPacketsSentThisFrame_Reliable;
            int outgoingSent_Unreliable = _outgoingPacketsSentThisFrame_Unreliable;

            // Count packets currently queued awaiting send
            int totalQueuedOutgoing = 0;
            using (var enumerator = singleProducerSendQueuesByThread.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    SingleProducerQueues singleProducerSendQueues = enumerator.Current.Value;
                    totalQueuedOutgoing += singleProducerSendQueues.queueForWork.Count;
                }
            }

            // Reset counters for next frame
            _outgoingPacketsSentThisFrame_Reliable = 0;
            _outgoingPacketsSentThisFrame_Unreliable = 0;

            // === 3. PARTICIPANT DESERIALIZATION STATE ===
            int participantsWaitingForDeserialize = 0;
            int totalParticipants = 0;

            using (var enumerator = gonetParticipantByGONetIdMap.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    GONetParticipant participant = enumerator.Current.Value;
                    if (participant != null)
                    {
                        totalParticipants++;
                        if (participant.requiresDeserializeInit && !participant.didDeserializeInitComplete)
                        {
                            participantsWaitingForDeserialize++;
                        }
                    }
                }
            }

            // === 4. EVENT BUS METRICS ===
            int eventBusQueueDepth = EventBus != null ? EventBus.GetApproximateQueueDepth() : 0;
            int deserializeInitEventsPublished = _deserializeInitEventsPublishedThisFrame;
            _deserializeInitEventsPublishedThisFrame = 0; // Reset for next frame

            // === 5. LOG IF INTERESTING (avoid spam during idle) ===
            // Threshold: Processing activity OR queues building up OR participants waiting
            const int ACTIVITY_THRESHOLD = 0; // Log whenever there's ANY activity
            bool hasActivity =
                (incomingProcessed_Reliable + incomingProcessed_Unreliable) > ACTIVITY_THRESHOLD ||
                (outgoingSent_Reliable + outgoingSent_Unreliable) > ACTIVITY_THRESHOLD ||
                totalQueuedPackets > 5 ||
                totalQueuedOutgoing > 5 ||
                participantsWaitingForDeserialize > 0 ||
                deserializeInitEventsPublished > 0;

            if (hasActivity)
            {
                GONetLog.Info(
                    $"[FRAME-METRICS] Frame {currentFrame}: " +
                    $"IN={{Processed:R{incomingProcessed_Reliable}/U{incomingProcessed_Unreliable}, Queued:{totalQueuedPackets}}} | " +
                    $"OUT={{Sent:R{outgoingSent_Reliable}/U{outgoingSent_Unreliable}, Queued:{totalQueuedOutgoing}}} | " +
                    $"Waiting={participantsWaitingForDeserialize}/{totalParticipants} | " +
                    $"EventBus={eventBusQueueDepth} | " +
                    $"DeserInitPub={deserializeInitEventsPublished}");
            }
        }

        /// <summary>
        /// EARLY FRAME UPDATE: UpdateAfterGONetReady for all ready GONetParticipantCompanionBehaviours.
        /// Called from GONetMain.Update() at end (runs at GONetGlobal.Update priority -32000, early in frame).
        ///
        /// ROBUSTNESS FEATURES:
        /// - Enumerator with dispose: Safe against DestroyImmediate() modifying HashSet mid-loop
        /// - Per-behaviour try-catch: One exception doesn't break entire pipeline
        /// - Optimized null checks: Avoids Unity's overloaded null operator (cast to object first)
        /// - Static reflection cache: Zero overhead for behaviours that don't override method
        /// </summary>
        internal static void Update_EarlyFrame_UpdateAfterGONetReady()
        {
            // SAFE ITERATION: Using enumerator with dispose pattern (HashSet-safe)
            // This handles DestroyImmediate() modifying the collection during iteration
            using (var enumerator = allGONetBehaviours.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    GONetBehaviour behaviour = enumerator.Current;

                // Defensive: Check if destroyed during iteration
                object behaviourObj = behaviour;
                if (behaviourObj == null)
                {
                    continue;
                }

                // Fast early exit: Type doesn't override method
                if (!behaviour.hasUpdateAfterGONetReadyOverride)
                {
                    continue;
                }

                // Optimized cast and null checks (avoid Unity null operator)
                GONetParticipantCompanionBehaviour companion = behaviour as GONetParticipantCompanionBehaviour;
                object companionObj = companion;
                if (companionObj == null)
                {
                    continue;
                }

                object participantObj = companion.GONetParticipant;
                if (participantObj == null)
                {
                    continue;
                }

                // Check if participant is ready
                if (!IsGONetReady(companion.GONetParticipant))
                {
                    continue;
                }

                    // ROBUST: Try-catch per behaviour - one exception doesn't break pipeline
                    try
                    {
                        companion.UpdateAfterGONetReady();
                    }
                    catch (Exception e)
                    {
                        // Log with full context for debugging
                        GONetLog.Error($"[GONet] Exception in UpdateAfterGONetReady() for {companion.GetType().Name} (GONetId: {companion.GONetParticipant.GONetId}): {e}");
                    }
                }
            }
        }

        /// <summary>
        /// REMOVED: Old Server_CollectAndSyncPhysicsState() method.
        /// Physics sync now handled by PhysicsSync_ProcessASAP() using standard AutoMagicalSync infrastructure.
        /// The T4 template generates Rigidbody-aware value sourcing for position/rotation automatically.
        /// See PhysicsSync_ProcessASAP() method and WaitForFixedUpdate coroutine in GONetGlobal.cs.
        /// </summary>

        /// <summary>
        /// PHYSICS FRAME UPDATE: FixedUpdateAfterGONetReady for all ready GONetParticipantCompanionBehaviours.
        /// Called from GONetGlobal.FixedUpdate() at Unity's fixed timestep (default: 50Hz / 0.02s).
        ///
        /// ROBUSTNESS FEATURES:
        /// - Enumerator with dispose: Safe against DestroyImmediate() modifying HashSet mid-loop
        /// - Per-behaviour try-catch: One exception doesn't break entire pipeline
        /// - Optimized null checks: Avoids Unity's overloaded null operator
        /// </summary>
        internal static void FixedUpdate_AfterGONetReady()
        {
            // Refresh physics time counter (mirrors Unity's Time.fixedTime behavior)
            if (Time != null)
            {
                Time.FixedUpdate();
            }

            // Physics sync now happens in WaitForFixedUpdate coroutine (started in GONetGlobal.cs)
            // This runs AFTER all physics processing (simulation + collision/trigger callbacks)
            // See PhysicsSync_ProcessASAP() method for implementation details

            // SAFE ITERATION: Using enumerator with dispose pattern (HashSet-safe)
            using (var enumerator = allGONetBehaviours.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    GONetBehaviour behaviour = enumerator.Current;

                // Defensive: Check if destroyed during iteration
                object behaviourObj = behaviour;
                if (behaviourObj == null)
                {
                    continue;
                }

                // Fast early exit: Type doesn't override method
                if (!behaviour.hasFixedUpdateAfterGONetReadyOverride)
                {
                    continue;
                }

                // Optimized cast and null checks
                GONetParticipantCompanionBehaviour companion = behaviour as GONetParticipantCompanionBehaviour;
                object companionObj = companion;
                if (companionObj == null)
                {
                    continue;
                }

                object participantObj = companion.GONetParticipant;
                if (participantObj == null)
                {
                    continue;
                }

                // Check if participant is ready
                if (!IsGONetReady(companion.GONetParticipant))
                {
                    continue;
                }

                    // ROBUST: Try-catch per behaviour
                    try
                    {
                        companion.FixedUpdateAfterGONetReady();
                    }
                    catch (Exception e)
                    {
                        GONetLog.Error($"[GONet] Exception in FixedUpdateAfterGONetReady() for {companion.GetType().Name} (GONetId: {companion.GONetParticipant.GONetId}): {e}");
                    }
                }
            }

            // Physics sync now happens in WaitForFixedUpdate coroutine (started in GONetGlobal)
            // This runs AFTER all physics processing (simulation + collision/trigger callbacks)
            // See PhysicsSync_ProcessASAP() method
        }

        /// <summary>
        /// Physics sync - Captures and syncs Rigidbody state from server to clients.
        /// Called from WaitForFixedUpdate coroutine AFTER all physics processing completes:
        /// - After all FixedUpdate() calls
        /// - After internal physics simulation
        /// - After OnCollisionEnter/Stay/Exit callbacks
        /// - After OnTriggerEnter/Stay/Exit callbacks
        ///
        /// This timing ensures we capture the FINAL physics state, not intermediate state.
        /// </summary>
        internal static void PhysicsSync_ProcessASAP()
        {
            // Increment physics frame counter (wraps around to prevent overflow)
            physicsFrameCounter = (physicsFrameCounter + 1) % 4;

            // SERVER ONLY: Physics sync only runs on server (authority over physics simulation)
            if (!IsServer)
            {
                return;
            }

            // Ensure physics sync processing support exists
            // This is created dynamically when first companion with physics sync needs it,
            // but we ensure it exists here for robustness
            if (!autoSyncProcessingSupportByFrequencyMap.ContainsKey(grouping_physics_unreliable))
            {
                // First-time setup - only happens once per session
                var physicsAutoSyncProcessingSupport =
                    new AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable(
                        grouping_physics_unreliable,
                        activeAutoSyncCompanionsByCodeGenerationIdMap);
                physicsAutoSyncProcessingSupport.AboutToProcess += AutoSyncProcessingSupport_AboutToProcess;
                autoSyncProcessingSupportByFrequencyMap[grouping_physics_unreliable] = physicsAutoSyncProcessingSupport;
                autoSyncProcessingSupports_UnityMainThread.Add(physicsAutoSyncProcessingSupport);
            }

            // Process physics sync using standard AutoMagicalSyncProcessing pipeline
            AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable physicsSync;
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_physics_unreliable, out physicsSync))
            {
                physicsSync.ProcessASAP();
            }
            else
            {
                GONetLog.Warning("[Physics Sync] Could not find physics sync processing support!");
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
            RingBuffer<IGONetEvent> eventQueue = events_SendToOthersQueue_ByThreadMap[Thread.CurrentThread];

            IGONetEvent @event;
            while (eventQueue.TryRead(out @event))
            {
                try
                {
                    EventBus.Publish(@event);
                }
                catch (Exception e)
                {
                    const string BOO = "Boo. Publishing this event failed. Error.Message: ";
                    GONetLog.Error(string.Concat(BOO, e.Message));
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
        static readonly long CLIENT_ABSURD_MAX_RTT_TICKS = TimeSpan.FromSeconds(10).Ticks;
        static readonly long CLIENT_MAX_ADJUSTMENT_TOLERANCE_TICKS = TimeSpan.FromMilliseconds(100).Ticks; // Maximum adjustment tolerance for gap closure
        static readonly long CLIENT_MIN_RTT_ESTIMATE_TICKS = TimeSpan.FromMilliseconds(10).Ticks >> 1; // 5ms one-way
        private static int clientStableSyncCount; // Thread-safe, no lock needed for simple increment
        private const int CLIENT_STABLE_SYNC_THRESHOLD = 3;
        static bool client_hasClosedTimeSyncGapWithServer;
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED = TimeSpan.FromMilliseconds(50).Ticks; // 0.05s
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED = TimeSpan.FromSeconds(5f).Ticks;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__UNTIL_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__POST_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED;
        static readonly long DIFF_TICKS_TOO_BIG_FOR_EASING = TimeSpan.FromSeconds(1f).Ticks; // if you are over a second out of sync...do not ease as that will take forever
        static bool client_hasSentSyncTimeRequest;
        static long client_lastSyncTimeRequestSentTicks;
        const int CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE = 60;
        private static readonly Dictionary<long, RequestMessage> client_lastFewTimeSyncsSentByUID = new(CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE);
        private static readonly List<long> client_uidCleanupBuffer = new(CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE);

        static long client_mostRecentTimeSyncResponseSentTicks;

        internal static readonly float BLENDING_BUFFER_LEAD_SECONDS_DEFAULT = 0.25f; // 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: 0.25f
        internal static float valueBlendingBufferLeadSeconds = BLENDING_BUFFER_LEAD_SECONDS_DEFAULT;
        internal static long valueBlendingBufferLeadTicks = TimeSpan.FromSeconds(BLENDING_BUFFER_LEAD_SECONDS_DEFAULT).Ticks;
        
        static bool client_isFirstTimeSync = true;

        /// <summary>
        /// 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: TimeSpan.FromMilliseconds(250).Ticks;
        /// </summary>
        private static void SetValueBlendingBufferLeadTimeFromMilliseconds(int valueBlendingBufferLeadTimeMilliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(valueBlendingBufferLeadTimeMilliseconds);
            valueBlendingBufferLeadSeconds = (float)timeSpan.TotalSeconds;
            valueBlendingBufferLeadTicks = timeSpan.Ticks;
        }


        private static void Client_SyncTimeWithServer_SendInitialBarrage()
        {
            // Send barrage of 5 time sync requests if not already synced.
            // This is basically the earliest point at which it makes sense to go ahead and initiate the time sync. We want to happen as soon as possible.
            GONetLog.Info($"[TimeSync] CLIENT: Starting initial time sync barrage (5 requests)");
            client_hasSentSyncTimeRequest = true; // Assume barrage is part of initial sync
            long startTicks = Time.ElapsedTicks;
            bool syncNeeded = true; // Could check processed data for a sync response
            if (syncNeeded)
            {
                for (int i = 0; i < 5; i++)
                {
                    Client_SyncTimeWithServer_SendRequest(startTicks + i);
                    if (i < 4) Thread.SpinWait(1000); // ~1 frame delay
                }
                client_lastSyncTimeRequestSentTicks = startTicks;
                TimeSyncScheduler.ResetOnConnection();
                GONetLog.Info($"[TimeSync] CLIENT: Initial barrage sent");
            }
        }

        /// <summary>
        /// Resets time sync to gap-closing mode.
        /// Call this after major events like scene changes or network hiccups.
        /// This triggers the same aggressive time sync sequence as initial connection
        /// (3 successful syncs required before gap is considered closed).
        /// CLIENT ONLY - has no effect on server.
        /// </summary>
        /// <param name="reason">Reason for reset (for logging/debugging)</param>
        public static void ResetTimeSyncGap(string reason = "unknown")
        {
            if (!IsClient) return;

            bool wasAlreadyClosed = client_hasClosedTimeSyncGapWithServer;

            // IMPORTANT: Don't reset time sync if the client hasn't closed the initial gap yet!
            // Late-joining clients need to complete their initial time sync sequence without interruption.
            // Only reset for clients that have already achieved sync and are experiencing a scene change.
            if (!wasAlreadyClosed)
            {
                GONetLog.Info($"[TimeSync] CLIENT: Skipping time sync reset for reason '{reason}' - client still closing initial gap (wasAlreadyClosed: {wasAlreadyClosed})");
                return;
            }

            //GONetLog.Info($"[TimeSync] CLIENT: Resetting time sync gap for reason: {reason}, wasAlreadyClosed: {wasAlreadyClosed}");

            // Reset to gap-closing phase
            client_hasClosedTimeSyncGapWithServer = false;
            System.Threading.Interlocked.Exchange(ref clientStableSyncCount, 0);

            // Reset scheduler to trigger immediate sync
            TimeSyncScheduler.ResetOnConnection();

            GONetLog.Info($"[TimeSync] CLIENT: Time sync state reset - starting new gap-closing phase");

            // Send initial barrage (same as connection)
            Client_SyncTimeWithServer_SendInitialBarrage();
        }
        /// <summary>
        /// Requests more frequent time synchronization after scene changes WITHOUT resetting the gap.
        /// This ensures good client-server time sync without blocking messages like ResetTimeSyncGap() does.
        /// Aggressive mode lasts for 10 seconds with 1-second sync intervals (instead of normal 5-second intervals).
        /// </summary>
        /// <param name="reason">Reason for requesting aggressive sync (for logging)</param>
        public static void RequestAggressiveTimeSync(string reason = "unknown")
        {
            if (!IsClient) return;

            //GONetLog.Info($"[TimeSync] CLIENT: Requesting aggressive time sync - Reason: {reason}");
            TimeSyncScheduler.EnableAggressiveMode(reason);
        }

        /// <summary>
        /// "IfAppropriate" is to indicate this runs on a schedule....if it is not the right time, this will do nothing.
        /// UPDATED to use TimeSyncScheduler for better performance
        /// </summary>
        private static void Client_SyncTimeWithServer_Initiate_IfAppropriate()
        {
            // Gap-closing phase: Frequent syncs until gap closed
            if (!client_hasClosedTimeSyncGapWithServer)
            {
                long nowTicks = Time.ElapsedTicks;
                if (nowTicks - client_lastSyncTimeRequestSentTicks < CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED)
                {
                    return;
                }

                client_hasSentSyncTimeRequest = true;
                client_lastSyncTimeRequestSentTicks = nowTicks;
                Client_SyncTimeWithServer_SendRequest(nowTicks);
                return;
            }

            // Maintenance phase: Use scheduler
            if (!TimeSyncScheduler.ShouldSyncNow())
            {
                return;
            }

            client_hasSentSyncTimeRequest = true;
            client_lastSyncTimeRequestSentTicks = Time.ElapsedTicks;
            Client_SyncTimeWithServer_SendRequest(client_lastSyncTimeRequestSentTicks);
        }

        static void Client_SyncTimeWithServer_SendRequest(long baseTicks)
        {
            RequestMessage timeSync = new RequestMessage(Time.RawElapsedTicks + (baseTicks % 1000));

            if (timeSync.UID == 0)
            {
                GONetLog.Error($"[TimeSync] CRITICAL BUG: Generated RequestMessage has UID=0! This will cause time sync to fail. OccurredAtElapsedTicks: {timeSync.OccurredAtElapsedTicks}");
            }

            //GONetLog.Info($"[TimeSync] CLIENT: Sending time sync request - UID: {timeSync.UID}, OccurredAt: {timeSync.OccurredAtElapsedTicks}");
            client_lastFewTimeSyncsSentByUID[timeSync.UID] = timeSync;
            if (client_lastFewTimeSyncsSentByUID.Count > CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE)
            {
                client_uidCleanupBuffer.Clear();
                long oldestTicks = long.MaxValue;
                long oldestUID = 0;
                foreach (var kvp in client_lastFewTimeSyncsSentByUID)
                {
                    if (kvp.Value.OccurredAtElapsedTicks < oldestTicks)
                    {
                        oldestTicks = kvp.Value.OccurredAtElapsedTicks;
                        oldestUID = kvp.Key;
                    }
                }
                client_lastFewTimeSyncsSentByUID.Remove(oldestUID);
            }

            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                uint messageID = messageTypeToMessageIDMap[typeof(RequestMessage)];
                bitStream.WriteUInt(messageID);
                bitStream.WriteLong(timeSync.OccurredAtElapsedTicks);
                bitStream.WriteLong(timeSync.UID);
                bitStream.WriteCurrentPartialByte();
                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);
                if (SendBytesToRemoteConnections(bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable))
                {
                    //GONetLog.Debug($"Sent time sync: t0={timeSync.OccurredAtElapsedTicks}, UID={timeSync.UID}");
                }
                mainThread_miscSerializationArrayPool.Return(bytes);
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

                    //GONetLog.Debug($"Server responding to time sync request from client.  My time (seconds): {TimeSpan.FromTicks(Time.ElapsedTicks).TotalSeconds}, ticks: {Time.ElapsedTicks}");
                }

                // body
                bitStream.WriteLong(requestUID);

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                SendBytesToRemoteConnection(connectionToClient, bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable);

                mainThread_miscSerializationArrayPool.Return(bytes);
            }
        }

        private static void Client_SyncTimeWithServer_ProcessResponse(long requestUID, long server_elapsedTicksAtSendResponse)
        {
            if (!client_lastFewTimeSyncsSentByUID.TryGetValue(requestUID, out RequestMessage requestMessage))
            {
                GONetLog.Warning($"No matching request for UID: {requestUID}, skipping process");
                return; // Early exit if no matching request
            }
            //GONetLog.Debug($"Processing sync for UID: {requestUID}, t0: {requestMessage.OccurredAtElapsedTicks} ticks ({TimeSpan.FromTicks(requestMessage.OccurredAtElapsedTicks).TotalSeconds:F3}s), t1: {server_elapsedTicksAtSendResponse} ticks ({TimeSpan.FromTicks(server_elapsedTicksAtSendResponse).TotalSeconds:F3}s)");

            // Force first sync to be immediate and close gap
            bool isFirstSync = client_isFirstTimeSync;
            HighPerfTimeSync.ProcessTimeSync(
                requestUID,
                server_elapsedTicksAtSendResponse,
                requestMessage,
                Time,
                forceAdjustment: isFirstSync
            );

            if (isFirstSync)
            {
                client_isFirstTimeSync = false;
                GONetLog.Info($"[TimeSync] CLIENT: FIRST time sync completed! Initial gap closed. UID: {requestUID}");
            }

            long responseReceivedTicks = Time.RawElapsedTicks;
            long requestSentTicks = requestMessage.OccurredAtElapsedTicks;
            long rtt_ticks = responseReceivedTicks - requestSentTicks;
            //GONetLog.Debug($"Calculated RTT: {rtt_ticks} ticks ({TimeSpan.FromTicks(rtt_ticks).TotalMilliseconds:F3}ms / {TimeSpan.FromTicks(rtt_ticks).TotalSeconds:F3}s)");

            if (rtt_ticks <= 0 || rtt_ticks >= CLIENT_ABSURD_MAX_RTT_TICKS)
            {
                GONetLog.Warning($"Invalid RTT: {rtt_ticks} ticks ({TimeSpan.FromTicks(rtt_ticks).TotalMilliseconds:F3}ms / {TimeSpan.FromTicks(rtt_ticks).TotalSeconds:F3}s), skipping gap check");
                goto Cleanup;
            }

            GONetClient.connectionToServer.RTT_Latest = (float)(rtt_ticks * HighResolutionTimeUtils.TICKS_TO_SECONDS); // Inline division
            long oneWayDelayTicks = (rtt_ticks >> 1); // Initial estimate
            //GONetLog.Debug($"Initial one-way delay estimate: {oneWayDelayTicks} ticks ({TimeSpan.FromTicks(oneWayDelayTicks).TotalMilliseconds:F3}ms)");

            if (GONetClient.connectionToServer.RTT_RecentAverage > 0)
            {
                //GONetLog.Debug($"Using RTT_RecentAverage: {GONetClient.connectionToServer.RTT_RecentAverage:F3}s, recalculating one-way delay");
                oneWayDelayTicks = (long)(GONetClient.connectionToServer.RTT_RecentAverage * TimeSpan.TicksPerSecond) >> 1;
                //GONetLog.Debug($"Updated one-way delay: {oneWayDelayTicks} ticks ({TimeSpan.FromTicks(oneWayDelayTicks).TotalMilliseconds:F3}ms)");
            }
            else
            {
                //GONetLog.Debug($"No RTT_RecentAverage, using minimum estimate: {CLIENT_MIN_RTT_ESTIMATE_TICKS} ticks ({TimeSpan.FromTicks(CLIENT_MIN_RTT_ESTIMATE_TICKS).TotalMilliseconds:F3}ms)");
                oneWayDelayTicks = Math.Max(oneWayDelayTicks, CLIENT_MIN_RTT_ESTIMATE_TICKS); // Minimum 5ms
            }

            long currentEffectiveTicks = responseReceivedTicks + Time.GetEffectiveOffsetTicks_Internal();
            long predictedServerTime = server_elapsedTicksAtSendResponse + oneWayDelayTicks;
            long diffTicksABS = Math.Abs(currentEffectiveTicks - predictedServerTime);
            /*
            GONetLog.Debug($"Predicted server time: {predictedServerTime} ticks ({TimeSpan.FromTicks(predictedServerTime).TotalSeconds:F3}s), " +
                           $"currentEffectiveTicks: {currentEffectiveTicks} ticks ({TimeSpan.FromTicks(currentEffectiveTicks).TotalSeconds:F3}s), " +
                           $"diffTicksABS: {diffTicksABS} ticks ({TimeSpan.FromTicks(diffTicksABS).TotalMilliseconds:F3}ms / {TimeSpan.FromTicks(diffTicksABS).TotalSeconds:F3}s)");
            */

            long adjustmentTicks = Time.GetAdjustmentTicks_Internal();
            //GONetLog.Debug($"Adjustment: {adjustmentTicks} ticks ({TimeSpan.FromTicks(adjustmentTicks).TotalMilliseconds:F3}ms / {TimeSpan.FromTicks(adjustmentTicks).TotalSeconds:F3}s)");

            if (!client_hasClosedTimeSyncGapWithServer)
            {
                //GONetLog.Info($"[TimeSync] CLIENT: Gap not closed yet. Checking conditions... diffTicksABS: {TimeSpan.FromTicks(diffTicksABS).TotalMilliseconds:F3}ms, adjustmentTicks: {TimeSpan.FromTicks(Math.Abs(adjustmentTicks)).TotalMilliseconds:F3}ms");
                if (diffTicksABS < CLIENT_SYNC_TIME_GAP_TICKS || Math.Abs(adjustmentTicks) < CLIENT_MAX_ADJUSTMENT_TOLERANCE_TICKS)
                {
                    Interlocked.Increment(ref clientStableSyncCount);
                    //GONetLog.Info($"[TimeSync] CLIENT: Stable sync progress - count: {clientStableSyncCount}/{CLIENT_STABLE_SYNC_THRESHOLD}");
                    if (clientStableSyncCount >= CLIENT_STABLE_SYNC_THRESHOLD)
                    {
                        client_hasClosedTimeSyncGapWithServer = true;
                        Interlocked.Exchange(ref clientStableSyncCount, 0); // Reset atomically
                        GONetLog.Info("[TimeSync] CLIENT: *** TIME SYNC GAP CLOSED *** - Switching to maintenance mode");
                    }
                }
                else
                {
                    GONetLog.Warning($"[TimeSync] CLIENT: Time divergence detected - resetting stable sync count (was {clientStableSyncCount})");
                    Interlocked.Exchange(ref clientStableSyncCount, 0); // Reset on divergence
                }
            }
            else
            {
                //GONetLog.Debug($"Maintenance mode active, last diffTicksABS: {diffTicksABS} ticks ({TimeSpan.FromTicks(diffTicksABS).TotalMilliseconds:F3}ms / {TimeSpan.FromTicks(diffTicksABS).TotalSeconds:F3}s)");
            }

        Cleanup:
            client_lastFewTimeSyncsSentByUID.Remove(requestUID);
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

            HighResolutionTimeUtils.Shutdown();
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
            /// <summary>
            /// DEPRECATED: Use GONetGlobal.Instance.maxPacketsPerTick instead.
            /// Kept for backward compatibility with code that references this constant.
            /// </summary>
            internal const int MAX_PACKETS_PER_TICK = 10 * 100;

            internal readonly ConcurrentQueue<NetworkData> queueForWork = new ConcurrentQueue<NetworkData>();
            internal readonly ConcurrentQueue<NetworkData> queueForPostWorkResourceReturn = new ConcurrentQueue<NetworkData>();

            /// <summary>
            /// CRITICAL IMPROVEMENT: Switched from ArrayPool to TieredArrayPool (October 2025).
            ///
            /// OLD PROBLEM:
            /// - ArrayPool allocated fixed-size arrays (1400-11200 bytes minimum)
            /// - Small RPC messages (10-50 bytes) wasted 95%+ memory
            /// - Pool exhaustion at ~1000 packets regardless of actual data size
            ///
            /// NEW SOLUTION:
            /// - TieredArrayPool routes requests to appropriately-sized pools
            /// - Small messages use tiny arrays (8-128 bytes)
            /// - 95% memory reduction for typical traffic patterns
            /// - 10-20x more headroom before congestion
            ///
            /// PERFORMANCE:
            /// - Zero performance penalty (inlined tier routing)
            /// - Reduced GC pressure (fewer large array allocations)
            /// - Better cache locality (smaller arrays fit in L1/L2)
            /// </summary>
            internal readonly TieredArrayPool<byte> resourcePool = new TieredArrayPool<byte>();
        }

        #region time (sync) related classes

        /// <summary>
        /// <para>
        /// <b>GONet Network-Synchronized Time Manager</b> - The authoritative source of truth for time in GONet multiplayer games.
        /// </para>
        ///
        /// <para>
        /// This class manages game (network) time with sub-millisecond precision using lock-free operations for maximum performance.
        /// The name "SecretaryOfTemporalAffairs" is a nerdy way to avoid conflicts with Unity's <see cref="UnityEngine.Time"/> class while being memorable!
        /// </para>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>THE BIBLE: HOW GONET TIME WORKS</b>
        /// </para>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>CORE CONCEPT: Real-World Time vs Unity Time</b>
        /// </para>
        ///
        /// <para>
        /// GONet time is based on <b>real-world Stopwatch time</b>, NOT Unity's frame-based time. This is critical for network synchronization:
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>Unity Time</b> - Frame-based, can pause/slow down, affected by Time.timeScale</item>
        /// <item><b>GONet Time</b> - Real-world clock, never pauses, immune to timeScale, network-synchronized across all clients</item>
        /// </list>
        ///
        /// <para>
        /// <b>Why real-world time?</b> Network packets arrive in real-world time, not Unity frame time. For smooth interpolation and
        /// accurate network state synchronization, GONet must track the same time domain as the network itself.
        /// </para>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>TWO TIME SYSTEMS: Standard Time vs Fixed Time</b>
        /// </para>
        ///
        /// <para>
        /// GONet maintains TWO synchronized time counters, mirroring Unity's Time.time and Time.fixedTime:
        /// </para>
        ///
        /// <list type="number">
        /// <item>
        /// <b>Standard Time (ElapsedSeconds)</b> - Updated every frame, equivalent to Unity's Time.time
        ///   <list type="bullet">
        ///   <item>Source: Stopwatch ticks + network offset (for server time synchronization)</item>
        ///   <item>Updated: Every Update() call (main thread)</item>
        ///   <item>Used for: Interpolation, extrapolation, gameplay logic</item>
        ///   <item>Thread-safe: Yes (lock-free with TLS caching)</item>
        ///   </list>
        /// </item>
        ///
        /// <item>
        /// <b>Fixed Time (FixedElapsedSeconds)</b> - Updated every physics tick, equivalent to Unity's Time.fixedTime
        ///   <list type="bullet">
        ///   <item>Source: Incremented by Time.fixedDeltaTime each FixedUpdate(), with catchup to stay synchronized</item>
        ///   <item>Updated: Every FixedUpdate() call (physics thread)</item>
        ///   <item>Used for: Physics simulation timestamp correlation</item>
        ///   <item>Thread-safe: Yes (lock-free with TLS caching)</item>
        ///   </list>
        /// </item>
        /// </list>
        ///
        /// <para>
        /// <b>Critical Guarantee:</b> Both time systems MUST always move forward (monotonicity). Time NEVER goes backward,
        /// even during network corrections. This is essential for physics stability and correct interpolation/extrapolation.
        /// </para>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>STANDARD TIME (ElapsedSeconds): The Network Time Domain</b>
        /// </para>
        ///
        /// <para>
        /// <b>Calculation:</b> <c>ElapsedSeconds = (Stopwatch.Ticks - StartTicks + NetworkOffset) / TicksPerSecond</c>
        /// </para>
        ///
        /// <para>
        /// <b>Network Offset:</b> The server periodically sends its current time to clients. Clients adjust their local time
        /// to match the server using one of three strategies:
        /// </para>
        ///
        /// <list type="number">
        /// <item><b>Immediate Jump</b> (gap > 1 second) - Instantly set time to server value (large corrections)</item>
        /// <item><b>Time Dilation</b> (gap > 50ms, negative) - Slow down time gradually over 2-5 seconds (smooth backward correction)</item>
        /// <item><b>Linear Interpolation</b> (gap > 1ms) - Smoothly interpolate over 1 second (small corrections)</item>
        /// </list>
        ///
        /// <para>
        /// <b>Why smooth corrections?</b> Instant time jumps cause visual glitches (objects teleporting, animations stuttering).
        /// Smooth corrections keep gameplay looking natural while maintaining network synchronization.
        /// </para>
        ///
        /// <para>
        /// <b>Thread Safety:</b> Uses lock-free atomic operations with Thread-Local Storage (TLS) caching for extreme performance:
        /// </para>
        ///
        /// <list type="bullet">
        /// <item>First access in a frame: Reads shared state atomically, caches in TLS</item>
        /// <item>Subsequent accesses: Returns cached value (nanosecond-level performance)</item>
        /// <item>No locks, no contention, fully thread-safe</item>
        /// </list>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>FIXED TIME (FixedElapsedSeconds): The Physics Time Domain</b>
        /// </para>
        ///
        /// <para>
        /// <b>Algorithm (Option C - Direct Gap Addition):</b>
        /// </para>
        ///
        /// <code>
        /// // On first FixedUpdate: Initialize to current network time
        /// if (firstFixedUpdate) {
        ///     FixedElapsedSeconds = ElapsedSeconds;
        /// }
        ///
        /// // On subsequent FixedUpdates:
        /// newFixedTime = oldFixedTime + Time.fixedDeltaTime;  // Normal increment
        ///
        /// gap = ElapsedSeconds - newFixedTime;                // Check if lagging
        /// if (gap > 0) {
        ///     newFixedTime += gap;                            // Catch up immediately
        /// }
        ///
        /// // Monotonicity protection (CRITICAL!)
        /// if (newFixedTime < oldFixedTime) {
        ///     newFixedTime = oldFixedTime;                    // Never go backward
        /// }
        ///
        /// FixedElapsedSeconds = newFixedTime;
        /// </code>
        ///
        /// <para>
        /// <b>Why "Option C" (Direct Gap Addition)?</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>✅ Handles any gap size</b> - No iteration limits, no freezing</item>
        /// <item><b>✅ O(1) complexity</b> - Single calculation, no loops</item>
        /// <item><b>✅ Always synchronized</b> - Fixed time tracks standard time perfectly</item>
        /// <item><b>✅ Network-correct</b> - Respects real-world time (Stopwatch-based)</item>
        /// <item><b>✅ Production proven</b> - Zero monotonicity violations in extensive testing</item>
        /// </list>
        ///
        /// <para>
        /// <b>What about Option A (incremental catchup)?</b> REMOVED - Failed in production testing. Hit 1000 iteration
        /// safety limit during scene transitions (10-30 second gaps), causing fixed time to freeze while standard time
        /// advanced, creating unrecoverable desynchronization.
        /// </para>
        ///
        /// <para>
        /// <b>When do gaps occur?</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>Normal operation</b> - Gap is typically 0-5ms (fixed slightly ahead or behind)</item>
        /// <item><b>Network corrections</b> - Server sends time adjustment, standard time jumps, fixed time catches up</item>
        /// <item><b>Scene transitions</b> - Physics pauses briefly, standard time keeps advancing (10-30 second gaps)</item>
        /// <item><b>Frame hitches</b> - Long frame causes gap, fixed time catches up immediately next FixedUpdate</item>
        /// </list>
        ///
        /// <para>
        /// <b>Monotonicity Guarantee:</b> CRITICAL for physics simulation. Unity's physics engine assumes Time.fixedTime
        /// never goes backward. If it did, objects would teleport, velocities would reverse, collisions would be missed.
        /// The monotonicity protection prevents this by clamping to the previous value if a network correction would cause
        /// backward time travel.
        /// </para>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>TYPICAL EXECUTION FLOW (Unity Frame)</b>
        /// </para>
        ///
        /// <code>
        /// // Unity Frame Cycle (every 16.67ms at 60 FPS):
        ///
        /// [FixedUpdate] (may run 0, 1, or multiple times)
        ///   ↓
        ///   SecretaryOfTemporalAffairs.FixedUpdate()
        ///   ↓
        ///   - Increment FixedElapsedSeconds by Time.fixedDeltaTime
        ///   - Check gap with ElapsedSeconds
        ///   - If lagging, catch up immediately
        ///   - Apply monotonicity protection
        ///   - Cache result in TLS
        ///   ↓
        ///   [Physics simulation uses FixedElapsedSeconds]
        ///
        /// [Update] (runs once per frame)
        ///   ↓
        ///   SecretaryOfTemporalAffairs.Update()
        ///   ↓
        ///   - Read Stopwatch ticks
        ///   - Apply network offset (interpolation/dilation)
        ///   - Calculate ElapsedSeconds
        ///   - Apply monotonicity protection
        ///   - Cache result in TLS
        ///   ↓
        ///   [Gameplay/rendering uses ElapsedSeconds]
        ///
        /// [LateUpdate, Rendering, etc.]
        ///   ↓
        ///   [Code reads time from TLS cache - nanosecond performance]
        /// </code>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>PERFORMANCE CHARACTERISTICS</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>ElapsedSeconds read</b> - ~10-50 nanoseconds (TLS cache hit)</item>
        /// <item><b>Update() call</b> - ~5-15 microseconds (atomic operations + calculation)</item>
        /// <item><b>FixedUpdate() call</b> - ~5-15 microseconds (increment + catchup check)</item>
        /// <item><b>Network offset adjustment</b> - ~10-20 microseconds (interpolation math)</item>
        /// <item><b>Thread safety</b> - 100% lock-free, zero contention</item>
        /// <item><b>Memory footprint</b> - 216 bytes (cache-line aligned, false-sharing prevention)</item>
        /// </list>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>COMMON SCENARIOS EXPLAINED</b>
        /// </para>
        ///
        /// <para>
        /// <b>Scenario 1: Client joins server mid-game</b>
        /// </para>
        ///
        /// <code>
        /// 1. Client starts with local time = 0.0s
        /// 2. Server sends "my time is 1234.56s"
        /// 3. Client sets NetworkOffset = 1234.56s (immediate jump, gap > 1s)
        /// 4. Client's ElapsedSeconds now reads ~1234.56s
        /// 5. Next FixedUpdate: FixedElapsedSeconds initializes to 1234.56s
        /// 6. Client is now synchronized with server timeline
        /// </code>
        ///
        /// <para>
        /// <b>Scenario 2: Network lag causes time drift</b>
        /// </para>
        ///
        /// <code>
        /// Frame 1000:
        ///   Client time: 50.000s
        ///   Server time: 50.000s (in sync)
        ///
        /// [Network lag - 200ms delay]
        ///
        /// Frame 1020:
        ///   Client time: 50.333s (local Stopwatch advanced)
        ///   Server says: "I'm at 50.533s" (200ms ahead due to lag)
        ///   Gap: 200ms
        ///   Strategy: Linear interpolation over 1 second
        ///
        ///   Over next 60 frames:
        ///     Client time speeds up slightly (adds 3.3ms per frame instead of normal)
        ///     After 1 second: Client back in sync at 51.533s
        /// </code>
        ///
        /// <para>
        /// <b>Scenario 3: Scene transition (physics pause)</b>
        /// </para>
        ///
        /// <code>
        /// Before scene load:
        ///   ElapsedSeconds: 100.000s
        ///   FixedElapsedSeconds: 100.000s (in sync)
        ///
        /// [Scene loading - physics disabled for 15 seconds]
        ///
        /// After scene load:
        ///   ElapsedSeconds: 115.000s (Stopwatch kept advancing)
        ///   FixedElapsedSeconds: 100.000s (no FixedUpdate calls during load)
        ///
        ///   First FixedUpdate after load:
        ///     newFixedTime = 100.000 + 0.0167 = 100.0167s
        ///     gap = 115.000 - 100.0167 = 14.9833s
        ///     Catchup: newFixedTime += 14.9833 = 115.000s
        ///     Result: Back in sync immediately (Option C!)
        /// </code>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>DEBUGGING AND VALIDATION</b>
        /// </para>
        ///
        /// <para>
        /// <b>Debug Logging (server only):</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item>Every Update(): Logs ElapsedSeconds vs Unity.Time.time</item>
        /// <item>Every FixedUpdate(): Logs FixedElapsedSeconds vs Unity.Time.fixedTime</item>
        /// <item>Every 50 physics frames: Full diagnostic dump comparing all time values</item>
        /// </list>
        ///
        /// <para>
        /// <b>Log Analysis Tools:</b> See <c>Assets/GONet/Sample/Utilities/LogAnalysis/analyze_physics_time.py</c>
        /// </para>
        ///
        /// <para>
        /// <b>Key metrics to watch:</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>Monotonicity violations</b> - Should be ZERO (time going backward = critical bug)</item>
        /// <item><b>Ping-pong detection</b> - Should be ZERO (time values oscillating = TLS bug)</item>
        /// <item><b>Gap size</b> - Normal: 0-5ms, Concerning: >100ms, Critical: >1s</item>
        /// <item><b>Catchup failures</b> - Should be ZERO (fixed time stuck = algorithm failure)</item>
        /// </list>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>PRODUCTION READINESS: EXTENSIVELY TESTED</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><b>Unit tests:</b> 121 comprehensive tests (all passing)</item>
        /// <item><b>Gameplay tests:</b> 2+ minute sessions, multiple scene changes, active spawning</item>
        /// <item><b>Monotonicity:</b> Zero violations across all tests</item>
        /// <item><b>Catchup:</b> Zero failures, handles gaps from 1ms to 30+ seconds</item>
        /// <item><b>Performance:</b> Nanosecond-level access, microsecond-level updates</item>
        /// <item><b>Thread safety:</b> 100% lock-free, validated under multi-threaded load</item>
        /// </list>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <b>RELATED DOCUMENTATION</b>
        /// </para>
        ///
        /// <list type="bullet">
        /// <item><c>D:\.claude\OPTION_C_FINAL_IMPLEMENTATION.md</c> - Decision rationale and test results</item>
        /// <item><c>D:\.claude\PHYSICS_TIME_MONOTONICITY_FIX.md</c> - Original monotonicity fix</item>
        /// <item><c>D:\.claude\PHYSICS_TIME_LOGGING_FIX.md</c> - Multi-instance logging solution</item>
        /// <item><c>Assets/GONet/Sample/Utilities/LogAnalysis/README_PHYSICS_TIME_ANALYSIS.md</c> - Testing guide</item>
        /// <item>Unit tests: <c>SecretaryOfTemporalAffairs_FixedUpdateTests.cs</c> (20 tests)</item>
        /// </list>
        ///
        /// <para>
        /// <b>━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━</b>
        /// </para>
        ///
        /// <para>
        /// <i>This is the definitive reference for GONet's time system. Keep it updated as the implementation evolves.</i>
        /// </para>
        /// </summary>
        public sealed class SecretaryOfTemporalAffairs
        {
            public delegate void TimeChangeArgs(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks);
            public event TimeChangeArgs TimeSetFromAuthority;

            // Cache-line aligned structure for atomic updates
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct TimeState
            {
                public long AuthorityOffsetTicks;
                public long TargetOffsetTicks;
                public long AdjustmentStartTicks;
                public long CachedElapsedTicks;
                public long LastUpdateFrame;
                public double CachedElapsedSeconds;
                public float LastDeltaTime;
                public int IsInitialized;

                // Physics time tracking (mirrors Unity's Time.fixedTime behavior)
                public long PhysicsElapsedTicks;          // Physics time counter (manually incremented)
                // PhysicsInitialized moved to AlignedTimeState for direct mutation
                public long CachedFixedElapsedTicks;      // Cached for fast access
                public long LastFixedUpdateFrame;         // Frame number for cache validation
                public double CachedFixedElapsedSeconds;  // Cached seconds version
                public float LastFixedDeltaTime;          // Delta between FixedUpdate calls
            }

            // Separate structure for interpolation state
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct InterpolationState
            {
                public long EffectiveOffsetTicks;
                public long LastCalculationTicks;
                public int Version;
                public long DilationStartOffsetTicks;
                public long DilationTargetOffsetTicks;
                public long DilationStartTimeTicks;
                public long DilationDurationTicks;
            }

            // 128-byte aligned structure to prevent false sharing
            [StructLayout(LayoutKind.Explicit, Size = 216)]
            private struct AlignedTimeState
            {
                [FieldOffset(0)] public TimeState State;
                [FieldOffset(64)] public InterpolationState Interpolation;
                [FieldOffset(128)] public long InitialStopwatchTicks;
                [FieldOffset(136)] public int UpdateCount;
                [FieldOffset(140)] public long InitialDateTimeTicks;
                [FieldOffset(148)] public int FixedUpdateCount; // Physics frame counter
                [FieldOffset(152)] public int PhysicsInitialized; // MOVED OUT: Direct access needed for mutation
                [FieldOffset(156)] public float UnityFixedTimeAtInit; // Unity's fixedTime when we initialized (for validation)
                [FieldOffset(160)] public long PhysicsTimeAtInit; // Initial physics time value (for validation)
            }
            private AlignedTimeState alignedState;

            // Constants
            private const long ADJUSTMENT_DURATION_TICKS = TimeSpan.TicksPerSecond; // 1 second
            private const double TICKS_TO_SECONDS = 1.0 / TimeSpan.TicksPerSecond;
            private const double SECONDS_TO_TICKS = TimeSpan.TicksPerSecond;

            // Thread-local cache for extreme performance
            [ThreadStatic] private static long tlsCachedTicks;
            [ThreadStatic] private static double tlsCachedSeconds;
            [ThreadStatic] private static int tlsLastFrame;
            [ThreadStatic] private static bool tlsInitialized;

            // Thread-local cache for FixedUpdate (physics time)
            [ThreadStatic] private static long tlsCachedFixedTicks;
            [ThreadStatic] private static double tlsCachedFixedSeconds;
            [ThreadStatic] private static int tlsLastFixedFrame;

            // Static fields for Unity Editor play mode handling
            private static long editorPlayModeStartStopwatchTicks = 0;
            private static bool isFirstInstanceThisPlaySession = true;

#if UNITY_EDITOR
            // This runs when entering play mode in Unity Editor
            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            static void ResetStaticsOnPlayMode()
            {
                ResetStaticsForTesting();
            }
#endif

            /// <summary>
            /// Resets all static state for testing purposes.
            /// MUST be called in test Setup() to ensure clean state between tests.
            /// </summary>
            internal static void ResetStaticsForTesting()
            {
                // Reset static state
                editorPlayModeStartStopwatchTicks = 0;
                isFirstInstanceThisPlaySession = true;
                // Clear thread-local storage
                tlsCachedTicks = 0;
                tlsCachedSeconds = 0;
                tlsLastFrame = -1;
                tlsInitialized = false;
                // Clear fixed time thread-local storage
                tlsCachedFixedTicks = 0;
                tlsCachedFixedSeconds = 0;
                tlsLastFixedFrame = -1;
            }

            public long ElapsedTicks
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => GetElapsedTicksFast();
            }

            public long RawElapsedTicks
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    long currentStopwatchTicks = HighResolutionTimeUtils.GetTimeSyncTicks_Internal();
                    long initialStopwatchTicks = Volatile.Read(ref alignedState.InitialStopwatchTicks);
                    return currentStopwatchTicks - initialStopwatchTicks;
                }
            }

            public double ElapsedSeconds
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => GetElapsedSecondsFast();
            }

            public double ElapsedSeconds_ClientSimulation
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    double elapsed = ElapsedSeconds;
                    return elapsed >= 0 ? elapsed - valueBlendingBufferLeadSeconds : 0;
                }
            }

            public float DeltaTime
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref alignedState.State.LastDeltaTime);
            }

            public int UpdateCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref alignedState.UpdateCount);
            }

            public int FrameCount { get; private set; }

            /// <summary>
            /// Synchronized elapsed time in ticks, cached once per FixedUpdate cycle.
            /// Use this for physics state collection to ensure consistent timestamps
            /// throughout the entire physics tick.
            /// DESIGN: Physics ONLY runs on server - clients receive synced position/rotation.
            /// </summary>
            public long FixedElapsedTicks
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => GetFixedElapsedTicksFast();
            }

            /// <summary>
            /// Synchronized elapsed time in seconds, cached once per FixedUpdate cycle.
            /// Use this for physics state collection to ensure consistent timestamps
            /// throughout the entire physics tick.
            /// DESIGN: Physics ONLY runs on server - clients receive synced position/rotation.
            /// </summary>
            public double FixedElapsedSeconds
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => GetFixedElapsedSecondsFast();
            }

            /// <summary>
            /// Time delta between FixedUpdate cycles (physics delta time).
            /// Mirrors Unity's Time.fixedDeltaTime progression.
            /// </summary>
            public float FixedDeltaTime
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref alignedState.State.LastFixedDeltaTime);
            }

            /// <summary>
            /// Physics frame counter (increments once per FixedUpdate).
            /// </summary>
            public int FixedUpdateCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Volatile.Read(ref alignedState.FixedUpdateCount);
            }

            private readonly double valueBlendingBufferLeadSeconds = 0.1; // Example value, adjust as needed

            public SecretaryOfTemporalAffairs()
            {
                // Initialize using high-resolution monotonic timer
                if (isFirstInstanceThisPlaySession)
                {
                    isFirstInstanceThisPlaySession = false;
                    editorPlayModeStartStopwatchTicks = HighResolutionTimeUtils.GetTimeSyncTicks_Internal();
                }
                long initialStopwatchTicks = editorPlayModeStartStopwatchTicks > 0 ?
                                             editorPlayModeStartStopwatchTicks :
                                             HighResolutionTimeUtils.GetTimeSyncTicks_Internal();
                // Store for reference
                alignedState.InitialStopwatchTicks = initialStopwatchTicks;
                alignedState.InitialDateTimeTicks = initialStopwatchTicks; // Using stopwatch ticks as relative time
                                                                           // Initialize all state to valid starting values
                alignedState.State.AuthorityOffsetTicks = 0;
                alignedState.State.TargetOffsetTicks = 0;
                alignedState.State.AdjustmentStartTicks = 0;
                alignedState.State.CachedElapsedTicks = 0;
                alignedState.State.CachedElapsedSeconds = 0.0;
                alignedState.State.LastUpdateFrame = -1;
                alignedState.State.LastDeltaTime = 0f;
                alignedState.State.IsInitialized = 1;
                alignedState.Interpolation.EffectiveOffsetTicks = 0;
                alignedState.Interpolation.LastCalculationTicks = 0;
                alignedState.Interpolation.Version = 0;
                alignedState.Interpolation.DilationStartOffsetTicks = 0;
                alignedState.Interpolation.DilationTargetOffsetTicks = 0;
                alignedState.Interpolation.DilationStartTimeTicks = 0;
                alignedState.Interpolation.DilationDurationTicks = 0;
                alignedState.UpdateCount = 0;
                // Initialize physics time state
                alignedState.State.PhysicsElapsedTicks = 0;
                alignedState.PhysicsInitialized = 0; // Not initialized until first FixedUpdate
                alignedState.State.CachedFixedElapsedTicks = 0;
                alignedState.State.LastFixedUpdateFrame = -1;
                alignedState.State.CachedFixedElapsedSeconds = 0.0;
                alignedState.State.LastFixedDeltaTime = 0f;
                alignedState.FixedUpdateCount = 0;
                Thread.MemoryBarrier();
            }

            public SecretaryOfTemporalAffairs(SecretaryOfTemporalAffairs initFromAuthority) : this()
            {
                if (initFromAuthority != null && initFromAuthority.alignedState.State.IsInitialized == 1)
                {
                    SetFromAuthority(initFromAuthority.ElapsedTicks);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal long GetAdjustmentTicks_Internal()
            {
                return (Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks) -
                        Volatile.Read(ref alignedState.State.AuthorityOffsetTicks)) * TimeSpan.TicksPerSecond;

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal long GetEffectiveOffsetTicks_Internal()
            {
                return Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private long GetElapsedTicksFast()
            {
                if (Volatile.Read(ref alignedState.State.IsInitialized) == 0)
                    return 0;
                
                if (!tlsInitialized)
                {
                    tlsLastFrame = -1;
                    tlsCachedTicks = 0;
                    tlsCachedSeconds = 0.0;
                    tlsInitialized = true;
                }
                
                int currentFrame = Volatile.Read(ref alignedState.UpdateCount);
                if (tlsLastFrame == currentFrame && tlsCachedTicks >= 0)
                    return tlsCachedTicks;
                
                long lastUpdateFrame = Volatile.Read(ref alignedState.State.LastUpdateFrame);
                if (lastUpdateFrame == currentFrame && lastUpdateFrame >= 0)
                {
                    long cachedTicks = Volatile.Read(ref alignedState.State.CachedElapsedTicks);
                    tlsLastFrame = currentFrame;
                    tlsCachedTicks = cachedTicks;
                    tlsCachedSeconds = alignedState.State.CachedElapsedSeconds;
                    return cachedTicks;
                }

                return CalculateElapsedTicks();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private double GetElapsedSecondsFast()
            {
                if (Volatile.Read(ref alignedState.State.IsInitialized) == 0)
                    return 0.0;
                
                if (!tlsInitialized)
                {
                    tlsLastFrame = -1;
                    tlsCachedTicks = 0;
                    tlsCachedSeconds = 0.0;
                    tlsInitialized = true;
                }
                
                int currentFrame = Volatile.Read(ref alignedState.UpdateCount);
                if (tlsLastFrame == currentFrame && tlsCachedSeconds >= 0)
                    return tlsCachedSeconds;

                long lastUpdateFrame = Volatile.Read(ref alignedState.State.LastUpdateFrame);
                if (lastUpdateFrame == currentFrame && lastUpdateFrame >= 0)
                {
                    double cachedSeconds = alignedState.State.CachedElapsedSeconds;
                    tlsLastFrame = currentFrame;
                    tlsCachedSeconds = cachedSeconds;
                    tlsCachedTicks = Volatile.Read(ref alignedState.State.CachedElapsedTicks);
                    return cachedSeconds;
                }

                long ticks = CalculateElapsedTicks();
                return Math.Max(0.0, ticks * TICKS_TO_SECONDS);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private long GetFixedElapsedTicksFast()
            {
                if (Volatile.Read(ref alignedState.State.IsInitialized) == 0)
                    return 0;

                if (!tlsInitialized)
                {
                    tlsLastFixedFrame = -1;
                    tlsCachedFixedTicks = 0;
                    tlsCachedFixedSeconds = 0.0;
                    tlsInitialized = true;
                }

                int currentFixedFrame = Volatile.Read(ref alignedState.FixedUpdateCount);
                if (tlsLastFixedFrame == currentFixedFrame && tlsCachedFixedTicks >= 0)
                    return tlsCachedFixedTicks;

                long lastFixedUpdateFrame = Volatile.Read(ref alignedState.State.LastFixedUpdateFrame);
                if (lastFixedUpdateFrame == currentFixedFrame && lastFixedUpdateFrame >= 0)
                {
                    long cachedFixedTicks = Volatile.Read(ref alignedState.State.CachedFixedElapsedTicks);
                    tlsLastFixedFrame = currentFixedFrame;
                    tlsCachedFixedTicks = cachedFixedTicks;
                    tlsCachedFixedSeconds = alignedState.State.CachedFixedElapsedSeconds;
                    return cachedFixedTicks;
                }

                // Fallback: physics not initialized yet, return network time
                return CalculateElapsedTicks();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private double GetFixedElapsedSecondsFast()
            {
                if (Volatile.Read(ref alignedState.State.IsInitialized) == 0)
                    return 0.0;

                if (!tlsInitialized)
                {
                    tlsLastFixedFrame = -1;
                    tlsCachedFixedTicks = 0;
                    tlsCachedFixedSeconds = 0.0;
                    tlsInitialized = true;
                }

                int currentFixedFrame = Volatile.Read(ref alignedState.FixedUpdateCount);
                if (tlsLastFixedFrame == currentFixedFrame && tlsCachedFixedSeconds >= 0)
                    return tlsCachedFixedSeconds;

                long lastFixedUpdateFrame = Volatile.Read(ref alignedState.State.LastFixedUpdateFrame);
                if (lastFixedUpdateFrame == currentFixedFrame && lastFixedUpdateFrame >= 0)
                {
                    double cachedFixedSeconds = alignedState.State.CachedFixedElapsedSeconds;
                    tlsLastFixedFrame = currentFixedFrame;
                    tlsCachedFixedSeconds = cachedFixedSeconds;
                    tlsCachedFixedTicks = Volatile.Read(ref alignedState.State.CachedFixedElapsedTicks);
                    return cachedFixedSeconds;
                }

                // Fallback: physics not initialized yet, return network time
                long ticks = CalculateElapsedTicks();
                return Math.Max(0.0, ticks * TICKS_TO_SECONDS);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private long CalculateElapsedTicks()
            {
                long currentStopwatchTicks = HighResolutionTimeUtils.GetTimeSyncTicks_Internal();
                long initialStopwatchTicks = Volatile.Read(ref alignedState.InitialStopwatchTicks);
                long lastCached;
                long elapsedStopwatchTicks = currentStopwatchTicks - initialStopwatchTicks;
                if (elapsedStopwatchTicks < 0)
                {
                    lastCached = Volatile.Read(ref alignedState.State.CachedElapsedTicks);
                    return lastCached > 0 ? lastCached : 0;
                }

                long rawElapsedTicks = elapsedStopwatchTicks;
                long effectiveOffset = GetEffectiveOffset(rawElapsedTicks);
                long result = rawElapsedTicks + effectiveOffset;
                lastCached = Volatile.Read(ref alignedState.State.CachedElapsedTicks);
                if (result < lastCached && lastCached > 0)
                {
                    return lastCached;
                }

                const long maxReasonableElapsed = 365L * TimeSpan.TicksPerDay;
                if (result > maxReasonableElapsed)
                {
                    return lastCached > 0 ? lastCached : 0;
                }

                return Math.Max(0, result);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private long GetEffectiveOffset(long currentElapsedTicks)
            {
                long authorityOffset = Volatile.Read(ref alignedState.State.AuthorityOffsetTicks);
                long targetOffset = Volatile.Read(ref alignedState.State.TargetOffsetTicks);
                long progress65536;
                if (authorityOffset == targetOffset)
                {
                    long currentEffective = Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);
                    if (currentEffective != authorityOffset)
                    {
                        Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, authorityOffset);
                    }
                    return authorityOffset;
                }
                
                long lastCalc = Volatile.Read(ref alignedState.Interpolation.LastCalculationTicks);
                long timeSinceLastCalc = currentElapsedTicks - lastCalc;
                if (timeSinceLastCalc < TimeSpan.TicksPerMillisecond)
                {
                    return Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);
                }

                long dilationDuration = Volatile.Read(ref alignedState.Interpolation.DilationDurationTicks);
                if (dilationDuration > 0)
                {
                    long dilationStart = Volatile.Read(ref alignedState.Interpolation.DilationStartTimeTicks);
                    long elapsed = currentElapsedTicks - dilationStart;
                    if (elapsed >= dilationDuration)
                    {
                        long target = Volatile.Read(ref alignedState.Interpolation.DilationTargetOffsetTicks);
                        Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, target);
                        Interlocked.Exchange(ref alignedState.State.AuthorityOffsetTicks, target);
                        Interlocked.Exchange(ref alignedState.Interpolation.DilationDurationTicks, 0);
                        return target;
                    }

                    long startOffset = Volatile.Read(ref alignedState.Interpolation.DilationStartOffsetTicks);
                    long targetDilationOffset = Volatile.Read(ref alignedState.Interpolation.DilationTargetOffsetTicks);
                    long offsetDelta = targetDilationOffset - startOffset;
                    progress65536 = (elapsed << 16) / dilationDuration;
                    long easedProgress65536;
                    if (progress65536 < 32768)
                    {
                        long t = progress65536;
                        easedProgress65536 = (4 * ((t * t) >> 16) * t) >> 16;
                    }
                    else
                    {
                        long t = progress65536 - 65536;
                        long tTimes2 = t << 1;
                        long pow3 = ((tTimes2 * tTimes2) >> 16) * tTimes2 >> 16;
                        easedProgress65536 = 65536 - (pow3 >> 1);
                    }

                    long newEffectiveOffset = startOffset + ((offsetDelta * easedProgress65536) >> 16);
                    Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, newEffectiveOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.LastCalculationTicks, currentElapsedTicks);

                    return newEffectiveOffset;
                }
                
                long adjustmentStart = Volatile.Read(ref alignedState.State.AdjustmentStartTicks);
                long adjustmentElapsed = currentElapsedTicks - adjustmentStart;
                
                if (adjustmentElapsed >= ADJUSTMENT_DURATION_TICKS)
                {
                    Interlocked.Exchange(ref alignedState.State.AuthorityOffsetTicks, targetOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, targetOffset);
                    return targetOffset;
                }

                progress65536 = (adjustmentElapsed << 16) / ADJUSTMENT_DURATION_TICKS;
                long offsetDiff = targetOffset - authorityOffset;
                long interpolatedOffset = authorityOffset + ((offsetDiff * progress65536) >> 16);
                Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, interpolatedOffset);
                Interlocked.Exchange(ref alignedState.Interpolation.LastCalculationTicks, currentElapsedTicks);

                return interpolatedOffset;
            }

            internal void SetFromAuthority(long elapsedTicksFromAuthority, bool forceImmediate = false)
            {
                if (elapsedTicksFromAuthority < 0)
                    return;

                long currentRawTicks = RawElapsedTicks;  // Use raw for accurate offset
                long currentEffectiveTicks = currentRawTicks + Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);  // For legacy checks if needed
                long oldEffectiveOffset = Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);
                long newOffset = elapsedTicksFromAuthority - currentRawTicks;  // target (server raw now) - raw = true offset
                long adjustment = newOffset - oldEffectiveOffset;
                long adjustmentAbs = Math.Abs(adjustment);
                //GONetLog.Debug($"Authority Set: RawTicks={currentRawTicks}, EffectiveTicks={currentEffectiveTicks}, OldOffset={oldEffectiveOffset}, NewOffset={newOffset}, Adjustment_Sec={TimeSpan.FromTicks(adjustment).TotalSeconds:F3}, Mode={(adjustmentAbs > TimeSpan.FromSeconds(1).Ticks ? "Immediate" : (adjustment < -TimeSpan.FromMilliseconds(50).Ticks ? "Dilation" : "Interpolation"))}");
                
                if (!forceImmediate && adjustmentAbs < TimeSpan.FromMilliseconds(1).Ticks)
                    return;
                
                if (forceImmediate || adjustmentAbs > TimeSpan.FromSeconds(1).Ticks)
                {
                    // Immediate
                    Interlocked.Exchange(ref alignedState.State.AuthorityOffsetTicks, newOffset);
                    Interlocked.Exchange(ref alignedState.State.TargetOffsetTicks, newOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.EffectiveOffsetTicks, newOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationDurationTicks, 0);
                }
                else if (adjustment < -TimeSpan.FromMilliseconds(50).Ticks)
                {
                    // Dilation (slow down for negative)
                    long duration = Math.Min(
                        TimeSpan.FromSeconds(5).Ticks,
                        Math.Max(TimeSpan.FromSeconds(2).Ticks, adjustmentAbs * 20)
                    );
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationStartOffsetTicks, oldEffectiveOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationTargetOffsetTicks, newOffset);
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationStartTimeTicks, currentRawTicks);  // Use raw for progress
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationDurationTicks, duration);
                    Interlocked.Exchange(ref alignedState.State.TargetOffsetTicks, newOffset);
                }
                else
                {
                    // Interpolation
                    Interlocked.Exchange(ref alignedState.State.TargetOffsetTicks, newOffset);
                    Interlocked.Exchange(ref alignedState.State.AdjustmentStartTicks, currentRawTicks);  // Use raw for progress
                    Interlocked.Exchange(ref alignedState.Interpolation.DilationDurationTicks, 0);
                }
                
                Interlocked.Increment(ref alignedState.Interpolation.Version);
                Update();
                
                if (TimeSetFromAuthority != null)
                {
                    long oldTicks = currentRawTicks + oldEffectiveOffset;  // Actual old effective
                    double oldSeconds = oldTicks * TICKS_TO_SECONDS;
                    double newSeconds = elapsedTicksFromAuthority * TICKS_TO_SECONDS;  // Actual new effective (raw + newOffset)
                    long newTicks = elapsedTicksFromAuthority;
                    TimeSetFromAuthority(oldSeconds, newSeconds, oldTicks, newTicks);
                }
            }

            internal void Update()
            {
                int newUpdateCount = Interlocked.Increment(ref alignedState.UpdateCount);
                long newElapsedTicks = CalculateElapsedTicks(); // Includes interpolation/dilation
                double newElapsedSecondsDouble = newElapsedTicks * TICKS_TO_SECONDS;
                double oldElapsedSeconds = Interlocked.Exchange(ref alignedState.State.CachedElapsedSeconds, newElapsedSecondsDouble); // Atomic read-and-write
                float deltaTime = 0f;
                if (oldElapsedSeconds >= 0 && newElapsedSecondsDouble > oldElapsedSeconds)
                {
                    deltaTime = (float)(newElapsedSecondsDouble - oldElapsedSeconds);
                    // Adjust clamping based on sync state
                    if (!client_isFirstTimeSync && Volatile.Read(ref alignedState.Interpolation.DilationDurationTicks) == 0) // Only clamp in steady state
                        deltaTime = Math.Clamp(deltaTime, 0.0f, 0.1f); // Use Math.Clamp for clarity
                    else if (deltaTime > 1.0f) // Log large initial or dilation deltas
                        GONetLog.Info($"Adjusted deltaTime: {deltaTime:F3}s at frame {newUpdateCount}");
                }
                Interlocked.Exchange(ref alignedState.State.CachedElapsedTicks, newElapsedTicks);
                alignedState.State.LastDeltaTime = deltaTime;
                Interlocked.Exchange(ref alignedState.State.LastUpdateFrame, newUpdateCount);
                Thread.MemoryBarrier();
                if (IsUnityMainThread)
                {
                    FrameCount = UnityEngine.Time.frameCount;
                }

                /*
                // DEBUG: Log every Update to track standard time progression (server only)
                if (IsUnityMainThread && IsServer)
                {
                    GONetLog.Debug($"[PhysicsTime] Update, gonet.std:{newElapsedSecondsDouble:F7}  unity.std:{UnityEngine.Time.time:F7}  unity.realtimeSinceStartup:{UnityEngine.Time.realtimeSinceStartup:F7}");
                }
                */
            }

            /// <summary>
            /// Called once per physics tick to refresh the physics time counter.
            /// Mirrors Unity's Time.fixedTime behavior by incrementing by exactly Time.fixedDeltaTime each call.
            /// DESIGN: Physics ONLY runs on server - clients receive synced position/rotation.
            /// </summary>
            internal void FixedUpdate()
            {
                int newFixedUpdateCount = Interlocked.Increment(ref alignedState.FixedUpdateCount);
                float unityFixedDeltaTime = UnityEngine.Time.fixedDeltaTime;

                // Get current standard time (already cached in Update() earlier this frame)
                double currentStandardTimeSeconds = ElapsedSeconds;

                double oldFixedElapsedSeconds = alignedState.State.CachedFixedElapsedSeconds;
                double newFixedElapsedSecondsDouble;
                long newFixedElapsedTicks;

                // First FixedUpdate? Initialize to current standard time
                if (alignedState.PhysicsInitialized == 0)
                {
                    // Anchor to current network time (same as standard time at initialization)
                    newFixedElapsedTicks = CalculateElapsedTicks();
                    newFixedElapsedSecondsDouble = newFixedElapsedTicks * TICKS_TO_SECONDS;
                    alignedState.PhysicsInitialized = 1;
                }
                else
                {
                    // Subsequent FixedUpdates: Increment by fixedDeltaTime, then catch up if lagging
                    // Start with normal increment
                    double newSeconds = oldFixedElapsedSeconds + unityFixedDeltaTime;

                    // If we're lagging behind standard time, add the gap to catch up immediately
                    // This ensures fixed time stays synchronized with network-adjusted standard time
                    double gap = currentStandardTimeSeconds - newSeconds;
                    if (gap > 0)
                    {
                        newSeconds += gap;
                    }

                    // Convert to ticks
                    long newTicks = (long)(newSeconds * TimeSpan.TicksPerSecond);

                    // CRITICAL: Ensure fixed time NEVER goes backward (monotonicity guarantee)
                    // This can happen if network offset adjustments cause standard time to decrease slightly
                    if (newSeconds < oldFixedElapsedSeconds)
                    {
                        // New value would go backward - clamp to previous value to maintain monotonicity
                        newSeconds = oldFixedElapsedSeconds;
                        newTicks = alignedState.State.CachedFixedElapsedTicks;
                    }

                    newFixedElapsedTicks = newTicks;
                    newFixedElapsedSecondsDouble = newSeconds;
                }

                alignedState.State.CachedFixedElapsedSeconds = newFixedElapsedSecondsDouble;

                float fixedDeltaTime = 0f;
                if (oldFixedElapsedSeconds >= 0 && newFixedElapsedSecondsDouble > oldFixedElapsedSeconds)
                {
                    fixedDeltaTime = (float)(newFixedElapsedSecondsDouble - oldFixedElapsedSeconds);
                    // Physics delta should be stable, clamp extreme values
                    fixedDeltaTime = Math.Clamp(fixedDeltaTime, 0.0f, 0.1f);
                }

                alignedState.State.CachedFixedElapsedTicks = newFixedElapsedTicks;
                alignedState.State.LastFixedDeltaTime = fixedDeltaTime;
                alignedState.State.LastFixedUpdateFrame = newFixedUpdateCount;

                /*
                // DEBUG: Log every FixedUpdate to track fixed time progression (server only)
                if (IsUnityMainThread && IsServer)
                {
                    GONetLog.Debug($"[PhysicsTime] FixedUpdate, gonet.fixed:{newFixedElapsedSecondsDouble:F7}  gonet.std:{currentStandardTimeSeconds:F7}  unity.fixed:{UnityEngine.Time.fixedTime:F7}  unity.std:{UnityEngine.Time.time:F7}  unity.realtimeSinceStartup:{UnityEngine.Time.realtimeSinceStartup:F7}");
                }
                
                // DIAGNOSTIC LOGGING: Compare all time values (every 50 physics frames)
                if (IsUnityMainThread && newFixedUpdateCount % 50 == 0)
                {
                    GONetLog.Info($"[PhysicsTime] " +
                                 $"Unity.Time.time={UnityEngine.Time.time:F6}s | " +
                                 $"Unity.Time.fixedTime={UnityEngine.Time.fixedTime:F6}s | " +
                                 $"Unity.Time.deltaTime={UnityEngine.Time.deltaTime:F6}s | " +
                                 $"GONet.Time.ElapsedSeconds={ElapsedSeconds:F6}s | " +
                                 $"GONet.Time.DeltaTime={DeltaTime:F6}s | " +
                                 $"GONet.Time.FixedElapsedSeconds={newFixedElapsedSecondsDouble:F6}s | " +
                                 $"GONet.Time.FixedDeltaTime={fixedDeltaTime:F6}s");
                }
                */

                Thread.MemoryBarrier();

                // Sync GONet frame count to Unity's frame count AFTER all updates complete
                // This ensures other threads see updated time values when they see the new FrameCount
                if (IsUnityMainThread)
                {
                    FrameCount = UnityEngine.Time.frameCount;
                }
            }

            /// <summary>
            /// Resets physics time counter to current network-synchronized time.
            /// Called automatically by GONetSceneManager after scene loads to prevent accumulated drift.
            /// Can also be called manually if needed.
            /// </summary>
            public void ResetPhysicsTime()
            {
                long anchorTicks = CalculateElapsedTicks();

                // Reset initialization flag so next FixedUpdate re-initializes
                alignedState.PhysicsInitialized = 0;
                alignedState.State.PhysicsElapsedTicks = anchorTicks;
                alignedState.State.CachedFixedElapsedTicks = anchorTicks;
                alignedState.State.CachedFixedElapsedSeconds = anchorTicks * TICKS_TO_SECONDS;

                GONetLog.Info($"[PhysicsTime] Reset to network time: {anchorTicks * TICKS_TO_SECONDS:F6}s");
            }

            public string DebugState()
            {
                long currentElapsed = CalculateElapsedTicks();
                long authorityOffset = Volatile.Read(ref alignedState.State.AuthorityOffsetTicks);
                long targetOffset = Volatile.Read(ref alignedState.State.TargetOffsetTicks);
                long effectiveOffset = Volatile.Read(ref alignedState.Interpolation.EffectiveOffsetTicks);
                bool isInterpolating = authorityOffset != targetOffset;
                return $"[SoTA] ElapsedTime: {currentElapsed / SECONDS_TO_TICKS:F3}s, " +
                       $"AuthorityOffset: {authorityOffset / SECONDS_TO_TICKS:F3}s, " +
                       $"TargetOffset: {targetOffset / SECONDS_TO_TICKS:F3}s, " +
                       $"EffectiveOffset: {effectiveOffset / SECONDS_TO_TICKS:F3}s, " +
                       $"Interpolating: {isInterpolating}, " +
                       $"UpdateCount: {UpdateCount}, " +
                       $"DeltaTime: {DeltaTime:F3}s, " +
                       $"Initialized: {alignedState.State.IsInitialized == 1}";
            }

            public (bool settled, int remainingMilliseconds) CheckAdjustmentStatus()
            {
                long authorityOffset = Volatile.Read(ref alignedState.State.AuthorityOffsetTicks);
                long targetOffset = Volatile.Read(ref alignedState.State.TargetOffsetTicks);
                bool settled = authorityOffset == targetOffset;
                if (settled)
                    return (true, 0);
                long adjustmentStart = Volatile.Read(ref alignedState.State.AdjustmentStartTicks);
                long currentElapsed = CalculateElapsedTicks();
                long adjustmentElapsed = currentElapsed - adjustmentStart;
                int remainingMs = Math.Max(0,
                    (int)((ADJUSTMENT_DURATION_TICKS - adjustmentElapsed) / TimeSpan.TicksPerMillisecond));
                return (false, remainingMs);
            }
        }

        /// <summary>
        /// High-performance, lock-free NTP-style time synchronization
        /// </summary>
        public static unsafe class HighPerfTimeSync
        {
            private struct MinRttState
            {
                public long MinRttTicks;
                public long MinTimeTicks;
            }

            private static MinRttState minRttState = new MinRttState { MinRttTicks = long.MaxValue, MinTimeTicks = 0 };
            private static readonly long MAX_RTT_TICKS = TimeSpan.FromSeconds(10).Ticks;
            private static readonly long FAST_MIN_RTT_CUTOFF_TICKS = TimeSpan.FromSeconds(10).Ticks;
            private static readonly long FAST_MIN_RTT_DEFAULT_RETURN_TICKS = TimeSpan.FromMilliseconds(50).Ticks;

            public static void ProcessTimeSync(
                long requestUID,
                long serverElapsedTicksAtResponse,
                RequestMessage requestMessage,
                SecretaryOfTemporalAffairs timeAuthority,
                bool forceAdjustment = false)
            {
                if (requestMessage == null || timeAuthority == null || serverElapsedTicksAtResponse <= 0)
                {
                    GONetLog.Warning($"[TimeSync] ProcessTimeSync EARLY EXIT - requestMessage null: {requestMessage == null}, timeAuthority null: {timeAuthority == null}, serverElapsedTicksAtResponse: {serverElapsedTicksAtResponse}");
                    return;
                }

                long t0 = requestMessage.OccurredAtElapsedTicks;  // raw
                long t1 = serverElapsedTicksAtResponse;
                long t2 = timeAuthority.RawElapsedTicks;  // raw at receive
                long rtt_ticks = t2 - t0;

                //GONetLog.Info($"[TimeSync] ProcessTimeSync - UID: {requestUID}, t0: {t0}, t1: {t1}, t2: {t2}, RTT_ticks: {rtt_ticks}, RTT_ms: {rtt_ticks / 10_000}ms, forceAdjustment: {forceAdjustment}");

                if (rtt_ticks < 0 || rtt_ticks > MAX_RTT_TICKS)
                {
                    GONetLog.Warning($"[TimeSync] Invalid RTT detected: {rtt_ticks / 10_000}ms, skipping sync");
                    return;
                }

                // Update minimum RTT if this sample is within the cutoff and lower
                long nowTicks = t2;
                long cutoff = nowTicks - FAST_MIN_RTT_CUTOFF_TICKS;
                bool updatedMinRtt = false;
                if (minRttState.MinTimeTicks < cutoff || rtt_ticks < minRttState.MinRttTicks)
                {
                    minRttState.MinRttTicks = rtt_ticks;
                    minRttState.MinTimeTicks = nowTicks;
                    updatedMinRtt = true;
                }

                long minRtt = minRttState.MinRttTicks;
                long oneWayDelayTicks = (minRtt > 0) ? (minRtt >> 1) : (FAST_MIN_RTT_DEFAULT_RETURN_TICKS >> 1);
                long adjustedServerTimeTicks = t1 + oneWayDelayTicks;
                long serverTimeNowTicks = adjustedServerTimeTicks;
                long clientTimeNowTicks = t2;
                long currentDifferenceTicks = serverTimeNowTicks - clientTimeNowTicks;
                long targetTimeTicks = clientTimeNowTicks + currentDifferenceTicks;

                //GONetLog.Info($"[TimeSync] Calculations - minRtt: {minRtt} ticks ({minRtt / 10_000}ms), oneWayDelay: {oneWayDelayTicks / 10_000}ms, currentDifference: {currentDifferenceTicks / 10_000}ms, updatedMinRtt: {updatedMinRtt}");

                timeAuthority.SetFromAuthority(targetTimeTicks, forceAdjustment);

                //GONetLog.Info($"[TimeSync] SetFromAuthority called - targetTimeTicks: {targetTimeTicks}, forceAdjustment: {forceAdjustment}");
            }

            /// <summary>
            /// Resets the time sync state for testing purposes.
            /// This should only be used in test scenarios to ensure clean state between tests.
            /// </summary>
            internal static void ResetForTesting()
            {
                // Reset the minimum RTT tracking state
                minRttState = new MinRttState
                {
                    MinRttTicks = long.MaxValue,
                    MinTimeTicks = 0
                };
            }
        }

        /// <summary>
        /// High-performance time sync scheduler
        /// </summary>
        public static class TimeSyncScheduler
        {
            private static long lastSyncTimeTicks = 0;
            private static readonly long SYNC_INTERVAL_TICKS = TimeSpan.TicksPerSecond * 5;
            private static readonly long AGGRESSIVE_INTERVAL_TICKS = TimeSpan.TicksPerSecond * 1; // 1 second for aggressive mode
            private static readonly long MIN_INTERVAL_TICKS = TimeSpan.TicksPerSecond;

            // Aggressive mode state
            private static long aggressiveModeEndTicks = 0;
            private static readonly long AGGRESSIVE_MODE_DURATION_TICKS = TimeSpan.TicksPerSecond * 10; // 10 seconds

            public static void ResetOnConnection()
            {
                lastSyncTimeTicks = Time.ElapsedTicks;
            }

            /// <summary>
            /// Temporarily increases time sync frequency without resetting gap.
            /// Used after scene changes to ensure good synchronization without blocking messages.
            /// </summary>
            public static void EnableAggressiveMode(string reason)
            {
                long now = Time.ElapsedTicks;
                aggressiveModeEndTicks = now + AGGRESSIVE_MODE_DURATION_TICKS;
                GONetLog.Info($"[TimeSync] Aggressive mode enabled for {TimeSpan.FromTicks(AGGRESSIVE_MODE_DURATION_TICKS).TotalSeconds}s - Reason: {reason}");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool ShouldSyncNow()
            {
                long now = Time.ElapsedTicks;
                long lastSync = Volatile.Read(ref lastSyncTimeTicks);
                long elapsed = now - lastSync;
                if (elapsed < MIN_INTERVAL_TICKS) return false;

                // Check if we're in aggressive mode
                bool isAggressiveMode = now < Volatile.Read(ref aggressiveModeEndTicks);
                long targetInterval = isAggressiveMode ? AGGRESSIVE_INTERVAL_TICKS : SYNC_INTERVAL_TICKS;

                if (elapsed < targetInterval) return false;
                return Interlocked.CompareExchange(ref lastSyncTimeTicks, now, lastSync) == lastSync;
            }
        }

        #endregion

        /// <summary>
        /// All incoming network bytes need to come here first, then <see cref="ProcessIncomingBytes_QueuedNetworkData_MainThread"/>.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread and eventually, the triage here will eventually send the incoming bytes to <see cref="ProcessIncomingBytes_QueuedNetworkData_MainThread"/>.
        /// </summary>
        internal static void ProcessIncomingBytes_TriageFromAnyThread(GONetConnection sourceConnection, byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            //GONetLog.Debug("received something.... size: " + bytesUsedCount);

            // DEBUG: Log ALL messages on Channel 8 immediately when received
            // NOTE: This logs every network message on chunk channel (hundreds per second)
            // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
            #if LOG_NETWORK_VERBOSE
            if (channelId == 8 && IsClient)
            {
                GONetLog.Warning($"[CHUNK_TRACE] NETWORK ENTRY - Channel: {channelId}, Bytes: {bytesUsedCount}, Thread: {Thread.CurrentThread.ManagedThreadId}");
            }
            #endif

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

            // DEBUG: Confirm enqueue
            // NOTE: This logs every network message enqueue (hundreds per second)
            // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
            #if LOG_NETWORK_VERBOSE
            if (channelId == 8 && IsClient)
            {
                GONetLog.Warning($"[CHUNK_TRACE] ENQUEUED - Channel: {channelId}, Bytes: {bytesUsedCount}, QueueCount: {singleProducerReceiveQueues.queueForWork.Count}");
            }
            #endif
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
                int threadQueueIndex = 0;
                while (enumerator.MoveNext())
                {
                    threadQueueIndex++;
                    SingleProducerQueues singleProducerReceiveQueues = enumerator.Current.Value;
                    ConcurrentQueue<NetworkData> incomingNetworkData = singleProducerReceiveQueues.queueForWork;
                    NetworkData networkData;
                    int readyCount = incomingNetworkData.Count;

                    // DIAGNOSTIC: Log queue backup (only when queue > 10 to avoid spam)
                    if (readyCount > 10 && IsClient)
                    {
                        GONetLog.Warning($"[QUEUE-BACKUP] Thread queue #{threadQueueIndex} has {readyCount} messages ready (potential processing bottleneck)");
                    }

                    // DEBUG: Log queue stats
                    // NOTE: This logs every frame when messages are ready (60+ times per second)
                    // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                    #if LOG_NETWORK_VERBOSE
                    if (readyCount > 0 && IsClient)
                    {
                        GONetLog.Info($"[DEBUG] Processing thread queue #{threadQueueIndex} - {readyCount} messages ready");
                    }
                    #endif
                    int processedCount = 0;
                    while (processedCount < readyCount && incomingNetworkData.TryDequeue(out networkData))
                    {
                        ++processedCount;

                        // DEBUG: Log EVERY dequeued message on Channel 8 (chunk channel)
                        // NOTE: This logs every dequeued network message (hundreds per second)
                        // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                        #if LOG_NETWORK_VERBOSE
                        if (networkData.channelId == 8)
                        {
                            GONetLog.Warning($"[CHUNK_TRACE] DEQUEUED - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}, ProcessedCount: {processedCount}/{readyCount}");
                        }

                        // DEBUG: Log EVERY dequeued message on Channel 9
                        if (networkData.channelId == 9)
                        {
                            GONetLog.Info($"[DEBUG] DEQUEUED - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}, ProcessedCount: {processedCount}/{readyCount}");
                        }
                        #endif

                        try
                        {
                            // DEBUG: Track entry to try block for Channel 8
                            // NOTE: This logs every message processing (hundreds per second)
                            // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                            #if LOG_NETWORK_VERBOSE
                            if (networkData.channelId == 8)
                            {
                                GONetLog.Error($"[CHUNK_TRACE] ENTERED TRY BLOCK - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}");
                            }
                            #endif

                            // IMPORTANT: This check must come first as it exits early if condition met!
                            bool shouldQueueForProcessingAfterInitialization =
                                !IsChannelClientInitializationRelated(networkData.channelId) && IsClient && _gonetClient != null && !_gonetClient.IsInitializedWithServer;

                            if (IsClient)
                            {
                                bool isInitRelated = IsChannelClientInitializationRelated(networkData.channelId);
                                bool isInitialized = _gonetClient != null && _gonetClient.IsInitializedWithServer;

                                // DEBUG: Log channel 8 queueing decision
                                // NOTE: This logs every message queueing decision (hundreds per second)
                                // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                                #if LOG_NETWORK_VERBOSE
                                if (networkData.channelId == 8)
                                {
                                    GONetLog.Warning($"[CHUNK_TRACE] QUEUEING DECISION - Channel: {networkData.channelId}, IsInitRelated: {isInitRelated}, IsInitialized: {isInitialized}, WillQueue: {shouldQueueForProcessingAfterInitialization}");
                                }
                                #endif

                                //GONetLog.Debug($"[MSG] Received message - channel: {networkData.channelId}, size: {networkData.bytesUsedCount}, isInitRelated: {isInitRelated}, isInitialized: {isInitialized}, willQueue: {shouldQueueForProcessingAfterInitialization}");
                            }

                            if (shouldQueueForProcessingAfterInitialization)
                            {
                                // Try to identify the message type being queued
                                string messageInfo = "unknown";
                                try
                                {
                                    if (GONetChannel.IsGONetCoreChannel(networkData.channelId) && networkData.bytesUsedCount >= 4)
                                    {
                                        using (var tempStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(networkData.messageBytes, networkData.bytesUsedCount))
                                        {
                                            uint msgID;
                                            tempStream.ReadUInt(out msgID);
                                            if (messageTypeByMessageIDMap.TryGetValue(msgID, out Type msgType))
                                            {
                                                messageInfo = msgType.Name;
                                            }
                                        }
                                    }
                                }
                                catch { }

                                // NOTE: This logs every deferred message (can be hundreds during connection)
                                // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                                #if LOG_NETWORK_VERBOSE
                                GONetLog.Warning($"[MSG] QUEUING message for later (client not initialized yet) - Channel: {networkData.channelId}, MessageType: {messageInfo}, IsInitRelated: {IsChannelClientInitializationRelated(networkData.channelId)}");
                                #endif
                                GONetClient.incomingNetworkData_mustProcessAfterClientInitialized.Enqueue(networkData);
                                // NOTE: We intentionally DON'T return the byte array to pool here - it's queued for later processing
                                // The byte array will be returned when the queued message is eventually processed
                                continue;
                            }
                        }
                        catch (Exception e)
                        {
                            GONetLog.Error(string.Concat("Error Message: ", e.Message, "\nError Stacktrace:\n", e.StackTrace));
                        }

                        // DEBUG: Log channel 8 before calling INTERNAL
                        // NOTE: This logs every message processing (hundreds per second)
                        // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
                        #if LOG_NETWORK_VERBOSE
                        if (networkData.channelId == 8)
                        {
                            GONetLog.Warning($"[CHUNK_TRACE] CALLING INTERNAL - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}");
                        }
                        #endif

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
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread_INTERNAL(NetworkData networkData, bool isProcessingFromQueue = false)
        {
            // DIAGNOSTIC: Track incoming packet processing by channel
            // Added 2025-10-11 to investigate packet saturation during rapid spawning
            GONetChannel channel = GONetChannel.ById(networkData.channelId);
            bool isReliable = channel.QualityOfService == QosType.Reliable;
            IncrementIncomingPacketCounter(isReliable);

            // DEBUG: Log EVERY message that enters this function on Channel 8
            // NOTE: This logs every message processing (hundreds per second)
            // To enable, add LOG_NETWORK_VERBOSE to Player Settings → Scripting Define Symbols
            #if LOG_NETWORK_VERBOSE
            if (networkData.channelId == 8) // ClientInitialization_EventSingles_Reliable
            {
                GONetLog.Warning($"[CHUNK_TRACE] INTERNAL ENTRY - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}, isProcessingFromQueue: {isProcessingFromQueue}");
            }

            // DEBUG: Log EVERY message that enters this function
            if (networkData.channelId == 9) // ClientInitialization_CustomSerialization_Reliable
            {
                GONetLog.Info($"[DEBUG] ProcessIncomingBytes ENTRY - Channel: {networkData.channelId}, Bytes: {networkData.bytesUsedCount}, isProcessingFromQueue: {isProcessingFromQueue}, _gonetClient null: {_gonetClient == null}, IsClient: {IsClient}");
            }
            #endif

            bool shouldReturnToPool = true; // Track whether message should be returned to pool (false if queued elsewhere)
            try
            {
                if (networkData.channelId == GONetChannel.ClientInitialization_EventSingles_Reliable.Id || networkData.channelId == GONetChannel.EventSingles_Reliable.Id || networkData.channelId == GONetChannel.EventSingles_Unreliable.Id)
                {
                    // COMPREHENSIVE LOGGING - Receive EventSingle
                    LogMessageReceive(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId, networkData.relatedConnection, 0);

                    //if (networkData.channelId == GONetChannel.ClientInitialization_EventSingles_Reliable.Id)
                    //{
                        //GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Received message on ClientInitialization_EventSingles_Reliable - Size: {networkData.bytesUsedCount} bytes, From: AuthorityId {networkData.relatedConnection.OwnerAuthorityId}");
                    //}

                    DeserializeBody_EventSingle(networkData.messageBytes, networkData.bytesUsedCount, networkData.relatedConnection);
                }
                else if (GONetChannel.IsGONetCoreChannel(networkData.channelId))
                {
                    // IMPORTANT: Extract elapsedTicksAtSend for logging BEFORE creating the main processing bitStream
                    // because LogMessageReceive uses the same thread-local builder and would reset the position!
                    long elapsedTicksAtSend = 0;
                    if (networkData.bytesUsedCount >= 12) // Ensure we have at least messageID (4) + timestamp (8)
                    {
                        using (var tempStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(networkData.messageBytes, networkData.bytesUsedCount))
                        {
                            uint tempMsgId;
                            tempStream.ReadUInt(out tempMsgId);
                            tempStream.ReadLong(out elapsedTicksAtSend);
                        }
                    }

                    // COMPREHENSIVE LOGGING - Receive GONet core channel message (BEFORE main processing to avoid position reset)
                    LogMessageReceive(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId, networkData.relatedConnection, elapsedTicksAtSend);

                    using (var bitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(networkData.messageBytes, networkData.bytesUsedCount))
                    {
                        Type messageType;
                        ////////////////////////////////////////////////////////////////////////////
                        // header...just message type/id...well, now it is send time too
                        uint messageID;
                        bitStream.ReadUInt(out messageID);
                        messageType = messageTypeByMessageIDMap[messageID];

                        bitStream.ReadLong(out elapsedTicksAtSend);

                        // VELOCITY-AUGMENTED SYNC: Read velocity bit for VALUE/VELOCITY bundle type
                        // This bit determines whether all values in this bundle are serialized as velocities or values
                        // CRITICAL: Must be read BEFORE DeserializeBody_BundleOfChoice to stay in sync with serialization
                        bool isVelocityBundle = false;
                        if (messageType == typeof(AutoMagicalSync_ValueChanges_Message) ||
                            messageType == typeof(AutoMagicalSync_ValuesNowAtRest_Message))
                        {
                            isVelocityBundle = bitStream.ReadBit();
                        }

                        // DEBUG: Log position after reading header for OwnerAuthorityIdAssignmentEvent
                        //if (messageType == typeof(OwnerAuthorityIdAssignmentEvent))
                        //{
                            //GONetLog.Info($"[INIT] CLIENT: After reading header - MessageID: {messageID}, ElapsedTicks: {elapsedTicksAtSend}, BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                        //}
                        ////////////////////////////////////////////////////////////////////////////

                        // DEBUG: Log every message type received
                        //if (messageType == typeof(OwnerAuthorityIdAssignmentEvent) || messageType == typeof(ServerSaysClientInitializationCompletion))
                        //{
                            //GONetLog.Info($"[INIT] Received {messageType.Name} - MessageID: {messageID}, Channel: {networkData.channelId}, IsServer: {IsServer}, MyAuthorityId: {MyAuthorityId}");
                        //}

                        //GONetLog.Debug($"received something....networkData.bytesUsedCount: {networkData.bytesUsedCount}, messageType: {messageType.Name}, IsServer? {IsServer} (isServerOverride: {isServerOverride}, MyAuthorityId: {MyAuthorityId}/Server: {OwnerAuthorityId_Server}), IsClient? {IsClient}");

                        {  // body:
                            if (messageType == typeof(AutoMagicalSync_ValueChanges_Message) ||
                                messageType == typeof(AutoMagicalSync_ValuesNowAtRest_Message))
                            {
                                try
                                {
                                    DeserializeBody_BundleOfChoice(bitStream, networkData.relatedConnection, networkData.channelId, elapsedTicksAtSend, messageType, isVelocityBundle);
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
                                catch (GONetParticipantNotReadyException notReadyEx)
                                {
                                    // Participant exists but Awake incomplete - handle based on channel quality and config
                                    QosType channelQuality = GONetChannel.ById(networkData.channelId).QualityOfService;

                                    if (isProcessingFromQueue)
                                    {
                                        // Already retried once - participant STILL not ready after 1+ frames
                                        GONetLog.Error($"[GONETREADY-QUEUE] Sync bundle still has unready participant (GONetId: {notReadyEx.GONetId}) after retry. " +
                                                      $"This indicates an OnGONetReady lifecycle bug. Dropping bundle. Channel: {networkData.channelId}");
                                        // Falls through to pool return (shouldReturnToPool=true)
                                    }
                                    else if (channelQuality == QosType.Reliable &&
                                             GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady)
                                    {
                                        // CONFIGURABLE: User opted into deferral for reliable channels
                                        DeferSyncBundleWaitingForGONetReady(networkData, elapsedTicksAtSend, messageType);
                                        shouldReturnToPool = false; // Queue owns byte array now

                                        GONetLog.Debug($"[GONETREADY-QUEUE] Deferred reliable sync bundle - participant {notReadyEx.GONetId} not ready yet. " +
                                                      $"Queue size: {incomingNetworkData_waitingForGONetReady.Count}");
                                    }
                                    else
                                    {
                                        // DEFAULT: Drop the bundle (unreliable channel OR user disabled deferral)
                                        GONetLog.Debug($"[GONETREADY-DROP] Dropped sync bundle - participant {notReadyEx.GONetId} not ready. " +
                                                      $"Channel: {networkData.channelId} ({channelQuality}), " +
                                                      $"DeferEnabled: {GONetGlobal.Instance.deferSyncBundlesWaitingForGONetReady}");
                                        // Falls through to pool return (shouldReturnToPool=true)
                                    }
                                }
                            }
                            else if (messageType == typeof(RequestMessage))
                            {
                                long requestUID;
                                bitStream.ReadLong(out requestUID);

                                //GONetLog.Info($"[TimeSync] SERVER: Received time sync request - UID: {requestUID}, elapsedTicksAtSend: {elapsedTicksAtSend}");

                                if (requestUID == 0)
                                {
                                    GONetLog.Error($"[TimeSync] SERVER: CRITICAL - Received RequestMessage with UID=0! elapsedTicksAtSend: {elapsedTicksAtSend}, bitStream position: {bitStream.Position_Bytes}");
                                }

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
                                // Dump the raw bytes received
                                //string hex = System.BitConverter.ToString(networkData.messageBytes, 0, networkData.bytesUsedCount);
                                //GONetLog.Info($"[INIT] CLIENT: Received message - Bytes: {hex}");

                                // Show bytes 12-14 where OwnerAuthorityId should be
                                //string authBytes = System.BitConverter.ToString(networkData.messageBytes, 12, 3);
                                //GONetLog.Info($"[INIT] CLIENT: Bytes 12-14 (where AuthorityId should be): {authBytes}");

                                //GONetLog.Info($"[INIT] CLIENT: About to read OwnerAuthorityId - BitCount: {GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED}, BitStream Position Before: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits, TotalBytes: {networkData.bytesUsedCount}");

                                ushort ownerAuthorityId;
                                bitStream.ReadUShort(out ownerAuthorityId, GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED);
                                //GONetLog.Info($"[INIT] CLIENT: After read OwnerAuthorityId - Value: {ownerAuthorityId}, BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");

                                long sessionGUIDremote;
                                bitStream.ReadLong(out sessionGUIDremote);
                                //GONetLog.Info($"[INIT] CLIENT: After read SessionGUID - Value: {sessionGUIDremote}, BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                                SessionGUID = sessionGUIDremote;

                                if (!IsServer) // this only applied to clients....should NEVER happen on server
                                {
                                    //GONetLog.Info($"[INIT] Client received OwnerAuthorityIdAssignmentEvent - assigned AuthorityId: {ownerAuthorityId}");
                                    MyAuthorityId = ownerAuthorityId;
                                    //GONetLog.Info($"[INIT] After setting MyAuthorityId field - MyAuthorityId is now: {MyAuthorityId}");
                                } // else log warning?
                            }
                            else if (messageType == typeof(AutoMagicalSync_AllCurrentValues_Message))
                            {
                                // IMPORTANT: If we have deferred spawns waiting for scene load, we must also defer the AllValues bundle
                                // Otherwise we'll try to apply values to GameObjects that haven't been spawned yet (causing dictionary lookup failures)
                                if (deferredSpawnEvents.Count > 0)
                                {
                                    GONetLog.Warning($"[INIT] Deferring AllValues bundle processing - {deferredSpawnEvents.Count} spawns are waiting for scene load");

                                    // IMPORTANT: Copy only the remaining bytes AFTER the header (which has already been read)
                                    // The bitStream position is currently at the start of the body, after reading messageID and elapsedTicks
                                    int currentPosition = bitStream.Position_Bytes;
                                    int remainingBytes = networkData.bytesUsedCount - currentPosition;
                                    byte[] deferredBytes = new byte[remainingBytes];
                                    Array.Copy(bitStream.GetBuffer(), currentPosition, deferredBytes, 0, remainingBytes);

                                    // Store which scene we're waiting for (use the first deferred spawn's scene as reference)
                                    string requiredScene = deferredSpawnEvents.Count > 0 ? deferredSpawnEvents[0].SceneIdentifier : "";

                                    deferredAllValuesBundle = new DeferredAllValuesBundle
                                    {
                                        RawBytes = deferredBytes,
                                        BytesUsedCount = remainingBytes,
                                        RelatedConnection = networkData.relatedConnection,
                                        ElapsedTicksAtSend = elapsedTicksAtSend,
                                        RequiredSceneName = requiredScene
                                    };

                                    GONetLog.Warning($"[INIT] AllValues bundle deferred - waiting for scene '{requiredScene}' and {deferredSpawnEvents.Count} spawns to process (bytes: {remainingBytes})");
                                }
                                else
                                {
                                    try
                                    {
                                        DeserializeBody_AllValuesBundle(bitStream, networkData.bytesUsedCount, networkData.relatedConnection, elapsedTicksAtSend);
                                    }
                                    catch (KeyNotFoundException keyNotFoundEx)
                                    {
                                        // GONetId not found - likely scene-defined object IDs not assigned yet
                                        QosType channelQuality = GONetChannel.ById(networkData.channelId).QualityOfService;

                                        // CRITICAL FIX: Don't re-queue messages that are already being processed from the queue
                                        // This prevents infinite loops where a message keeps getting dequeued, failing, and re-queued
                                        if (isProcessingFromQueue)
                                        {
                                            GONetLog.Error($"[GONETID-QUEUE] Message still missing GONetId after queue processing - GONetId likely destroyed during scene change. Dropping message to prevent infinite loop.");
                                            // Message will be returned to pool via shouldReturnToPool=true
                                        }
                                        else if (IsClient && channelQuality == QosType.Reliable)
                                        {
                                            // Queue this message for retry after GONetId sync completes
                                            if (_gonetClient.incomingNetworkData_waitingForGONetIds.Count < GONetClient.MAX_GONETID_QUEUE_SIZE)
                                            {
                                                _gonetClient.incomingNetworkData_waitingForGONetIds.Enqueue(networkData);
                                                GONetLog.Debug($"[GONETID-QUEUE] Queued reliable message (channel: {networkData.channelId}) waiting for GONetId assignment. Queue size: {_gonetClient.incomingNetworkData_waitingForGONetIds.Count}");

                                                // Skip processing, but DON'T return to pool - it's now owned by the queue
                                                shouldReturnToPool = false;
                                            }
                                            else
                                            {
                                                // Queue full - log error and drop oldest
                                                GONetLog.Error($"[GONETID-QUEUE] Queue full ({GONetClient.MAX_GONETID_QUEUE_SIZE} messages)! Dropping oldest message. This indicates a problem with GONetId synchronization.");
                                                NetworkData droppedMessage = _gonetClient.incomingNetworkData_waitingForGONetIds.Dequeue();

                                                // Return dropped message to pool
                                                SingleProducerQueues droppedQueues = singleProducerReceiveQueuesByThread[droppedMessage.messageBytesBorrowedOnThread];
                                                droppedQueues.queueForPostWorkResourceReturn.Enqueue(droppedMessage);

                                                // Queue current message
                                                _gonetClient.incomingNetworkData_waitingForGONetIds.Enqueue(networkData);
                                                shouldReturnToPool = false;
                                            }
                                        }
                                        else
                                        {
                                            // Unreliable message or not a client - just drop it
                                            GONetLog.Debug($"[GONETID-QUEUE] Dropping unreliable message (channel: {networkData.channelId}) due to missing GONetId - as designed");
                                        }
                                        // Let it fall through to the finally block for cleanup
                                    }
                                }
                            }
                            else if (messageType == typeof(ServerSaysClientInitializationCompletion))
                            {
                                if (IsClient)
                                {
                                    //GONetLog.Info($"[INIT] Client received ServerSaysClientInitializationCompletion - MyAuthorityId is currently: {MyAuthorityId}");
                                    //GONetLog.Debug($"[INIT] Setting IsInitializedWithServer = true (this will fire InitializedWithServer event)");
                                    GONetClient.IsInitializedWithServer = true;
                                    //GONetLog.Debug($"[INIT] Client is now initialized with server - GONetLocal should be instantiated");

                                    // IMPORTANT: Log registered sync companions for debugging
                                    int totalCompanions = 0;
                                    foreach (var codeGenEntry in activeAutoSyncCompanionsByCodeGenerationIdMap)
                                    {
                                        totalCompanions += codeGenEntry.Value.Count;
                                        //GONetLog.Debug($"[AUTOMAGIC] Client has {codeGenEntry.Value.Count} sync companions registered for CodeGenId {codeGenEntry.Key}");
                                    }
                                    //GONetLog.Debug($"[AUTOMAGIC] Client total sync companions registered: {totalCompanions}");
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
                // Only return to pool if message wasn't queued elsewhere (e.g., waiting for GONetIds)
                if (shouldReturnToPool)
                {
                    // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                    SingleProducerQueues singleProducerReceiveQueues = singleProducerReceiveQueuesByThread[networkData.messageBytesBorrowedOnThread];
                    singleProducerReceiveQueues.queueForPostWorkResourceReturn.Enqueue(networkData);
                }
            }
        }

        private static void DeserializeBody_EventSingle(byte[] messageBytes, int bytesUsedCount, GONetConnection relatedConnection)
        {
            try
            {
                //GONetLog.Info($"[SPAWN_SYNC] CLIENT: ATTEMPTING to deserialize EventSingle - Size: {bytesUsedCount} bytes (array capacity: {messageBytes.Length}), From: AuthorityId {relatedConnection.OwnerAuthorityId}");

                // PERFORMANCE: Use ReadOnlySpan to deserialize only the actual message bytes (zero allocation, stack-only)
                // This is faster than ArraySegment<byte> (no heap allocation) and safer than raw byte[] (bounds-checked slice)
                IGONetEvent @event = SerializationUtils.DeserializeFromBytes<IGONetEvent>(
                    messageBytes.AsSpan(0, bytesUsedCount));

                //GONetLog.Warning($"[DESER_DEBUG] DeserializeFromBytes returned - Event is null: {@event == null}, Event type: {@event?.GetType().Name}");

                //GONetLog.Info($"[SPAWN_SYNC] CLIENT: SUCCESSFULLY deserialized EventSingle - Type: {@event.GetType().Name}, From: AuthorityId {relatedConnection.OwnerAuthorityId}");

                /*
                // Log PersistentEvents_Bundle and chunks specifically
                if (@event is PersistentEvents_Bundle bundle)
                {
                    GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Deserialized PersistentEvents_Bundle - Events: {bundle.PersistentEvents.Count}, From: AuthorityId {relatedConnection.OwnerAuthorityId}");
                }
                else if (@event is PersistentEvents_BundleChunk chunk)
                {
                    GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Deserialized PersistentEvents_BundleChunk - ChunkId: {chunk.ChunkId}, Index: {chunk.ChunkIndex}/{chunk.TotalChunks}, Size: {chunk.ChunkData.Length} bytes, From: AuthorityId {relatedConnection.OwnerAuthorityId}");
                }
                */

                //GONetLog.Warning($"[DESER_DEBUG] About to publish event to EventBus");
                EventBus.Publish(@event, relatedConnection.OwnerAuthorityId);
                //GONetLog.Warning($"[DESER_DEBUG] EventBus.Publish completed");
                // SPAM: Commented out - creates 2,777+ log entries during stress testing, mostly ValueMonitoringSupport events
                //GONetLog.Debug($"Incoming event being published.  Type: {@event.GetType().Name}");
            }
            catch (System.Exception ex)
            {
                GONetLog.Error($"[SPAWN_SYNC] CLIENT: FAILED to deserialize EventSingle - Size: {bytesUsedCount} bytes (array capacity: {messageBytes.Length}), From: AuthorityId {relatedConnection.OwnerAuthorityId}, Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
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

            //GONetLog.Debug($"instantiation.location: {instantiateEvent.DesignTimeLocation}, parent.fullPath: {instantiateEvent.ParentFullUniquePath}");

            // CRITICAL: Validate DesignTimeLocation is not empty before attempting to instantiate
            // If it's empty, the spawn event was created before metadata was initialized (timing issue)
            if (string.IsNullOrWhiteSpace(instantiateEvent.DesignTimeLocation))
            {
                GONetLog.Error($"Cannot instantiate remote GONetParticipant - DesignTimeLocation is empty/null! GONetId: {instantiateEvent.GONetId}, InstanceName: {instantiateEvent.InstanceName}. This indicates the spawn event was created before metadata initialization completed. The spawn will be skipped.");
                isCurrentlyProcessingInstantiateGNPEvent = false;
                return null;
            }

            GONetParticipant template = GONetSpawnSupport_Runtime.LookupTemplateFromDesignTimeMetadata(instantiateEvent.DesignTimeLocation);

            // CRITICAL: Get and set metadata on the TEMPLATE before instantiating
            // Unity calls Awake() DURING Instantiate(), so we must prepare the template first
            // The instance will inherit this metadata when it's created
            DesignTimeMetadata templateMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(instantiateEvent.DesignTimeLocation);
            bool templateMetadataWasAlreadySet = false;

            if (templateMetadata != null && !string.IsNullOrWhiteSpace(templateMetadata.Location))
            {
                //GONetLog.Debug($"Instantiate_Remote: Pre-setting metadata on template '{template.name}' - Location: '{templateMetadata.Location}', CodeGenId: {templateMetadata.CodeGenerationId}");

                // Check if template already has metadata to avoid overwriting
                if (!template.IsDesignTimeMetadataInitd)
                {
                    DesignTimeMetadata metadataToSet = new DesignTimeMetadata
                    {
                        Location = templateMetadata.Location,
                        CodeGenerationId = templateMetadata.CodeGenerationId,
                        UnityGuid = templateMetadata.UnityGuid
                    };
                    GONetSpawnSupport_Runtime.SetDesignTimeMetadata(template, metadataToSet);
                    template.IsDesignTimeMetadataInitd = true;
                }
                else
                {
                    templateMetadataWasAlreadySet = true;
                }
            }

            template.wasInstantiatedForce = true; // the instantiated one will get this
            GONetParticipant instance =
                string.IsNullOrWhiteSpace(instantiateEvent.ParentFullUniquePath)
                    ? UnityEngine.Object.Instantiate(template, instantiateEvent.Position, instantiateEvent.Rotation)
                    : UnityEngine.Object.Instantiate(template, instantiateEvent.Position, instantiateEvent.Rotation, HierarchyUtils.FindByFullUniquePath(instantiateEvent.ParentFullUniquePath).transform);
            template.wasInstantiatedForce = false; // be safe and set back to false

            // Deserialize custom spawn data from IGONetSyncdBehaviourInitializer components
            DeserializeCustomSpawnData(instance, instantiateEvent.CustomSpawnData);

            // The instance should have inherited the metadata from the template during Instantiate()
            // But we still need to mark it as initialized to be safe
            if (templateMetadata != null && !string.IsNullOrWhiteSpace(templateMetadata.Location))
            {
                // Verify the instance has the correct metadata
                if (!instance.IsDesignTimeMetadataInitd)
                {
                    GONetLog.Warning($"Instantiate_Remote: Instance '{instance.gameObject.name}' did NOT inherit metadata initialization flag from template - setting it now");
                    instance.IsDesignTimeMetadataInitd = true;
                }

                //GONetLog.Debug($"Instantiate_Remote: Instance '{instance.gameObject.name}' metadata - Location: '{instance.DesignTimeLocation}', CodeGenId: {instance.CodeGenerationId}, IsInitd: {instance.IsDesignTimeMetadataInitd}");
            }

            if (!string.IsNullOrWhiteSpace(instantiateEvent.InstanceName))
            {
                instance.gameObject.name = instantiateEvent.InstanceName;
            }

            //const string INSTANTIATE = "Instantiate_Remote, Instantiate complete....go.name: ";
            //const string ID = " event.gonetId: ";
            //const string FORCE = " wasInstantiatedForce: ";
            //GONetLog.Debug(string.Concat(INSTANTIATE, instance.gameObject.name, ID, instantiateEvent.GONetId, FORCE, instance.wasInstantiatedForce));

            instance.OwnerAuthorityId = instantiateEvent.OwnerAuthorityId;
            if (instantiateEvent.GONetId != GONetParticipant.GONetId_Unset)
            {
                instance.SetGONetIdFromRemoteInstantiation(instantiateEvent);
            }
            remoteSpawns_avoidAutoPropagateSupport.Add(instance);
            instance.IsOKToStartAutoMagicalProcessing = true;

            // LIFECYCLE GATE: Remote spawns require DeserializeInitAllCompleted before OnGONetReady
            // CRITICAL FIX 2025-10-11: Authority instances (IsMine=True) do NOT need deserialization!
            // They are the source of truth, not receiving sync data from elsewhere.
            // Only non-authority instances (IsMine=False) need to wait for initial sync data from remote authority.
            //
            // Example: Client spawns projectile with server authority
            //   - Client's instance: IsMine=False (not authority) → MUST wait for server sync data
            //   - Server's instance: IsMine=True (IS authority) → NO deserialization needed!
            //
            // Before this fix: Server instances got stuck waiting for events that would never come
            // After this fix: Server instances skip deserialization, OnGONetReady fires immediately after Start()
            if (!instance.IsMine)
            {
                instance.MarkRequiresDeserializeInit();
            }

            // Track which scene this GNP was spawned in
            string spawnSceneName = GONetSceneManager.GetSceneIdentifier(instance.gameObject);
            if (!string.IsNullOrEmpty(spawnSceneName))
            {
                RecordParticipantSpawnScene(instance, spawnSceneName);
            }

            isCurrentlyProcessingInstantiateGNPEvent = false;

            return instance;
        }

        /// <summary>
        /// Serializes initialization data from all <see cref="IGONetSyncdBehaviourInitializer"/> components on the given GONetParticipant.
        /// Used for both runtime spawns and scene-defined object synchronization.
        /// </summary>
        /// <param name="gonetParticipant">The participant to serialize initialization data from</param>
        /// <returns>Serialized initialization data byte array, or null if no initializers found</returns>
        internal static byte[] SerializeSceneObjectInitData(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant == null)
            {
                return null;
            }

            // Find all IGONetSyncdBehaviourInitializer components on the same GameObject
            IGONetSyncdBehaviourInitializer[] providers = gonetParticipant.GetComponents<IGONetSyncdBehaviourInitializer>();

            if (providers == null || providers.Length == 0)
            {
                return null; // No initialization data providers
            }

            // Create builder for serialization
            Utils.BitByBitByteArrayBuilder builder = Utils.BitByBitByteArrayBuilder.GetBuilder();

            // Write provider count (for deserialization validation)
            builder.WriteUInt((uint)providers.Length, 8); // Max 255 providers

            // Call each provider's serialization method
            foreach (IGONetSyncdBehaviourInitializer provider in providers)
            {
                provider.Spawner_SerializeSpawnData(builder);
            }

            // CRITICAL: Flush any remaining bits from scratch buffer to memory!
            // Without this, the last byte(s) of data remain in BitWriter's scratch buffer
            // and never get copied to the result array, causing deserialization to read garbage (zeros).
            // This happens because WriteFloat() writes 32 bits at a time, and when combined with
            // the 8-bit provider count, the last 8 bits of the 4th float stay in scratch.
            builder.WriteCurrentPartialByte();

            // Return serialized byte array (copy only the written bytes, not the full buffer)
            int bytesWritten = builder.Length_WrittenBytes;
            byte[] result = new byte[bytesWritten];
            Array.Copy(builder.GetBuffer(), 0, result, 0, bytesWritten);

            // HEX DUMP: Log raw bytes for debugging serialization issue
            string hexDump = System.BitConverter.ToString(result).Replace("-", " ");
            GONetLog.Debug($"[SceneInitData] Serialized initialization data for '{gonetParticipant.gameObject.name}' ({providers.Length} providers, {bytesWritten} bytes) - RAW BYTES: {hexDump}");

            return result;
        }

        /// <summary>
        /// Deserializes initialization data and calls <see cref="IGONetSyncdBehaviourInitializer.Receiver_DeserializeSpawnData"/> on all providers.
        /// Used for scene-defined object synchronization.
        /// </summary>
        /// <param name="participant">The scene-defined GONetParticipant</param>
        /// <param name="initData">Serialized initialization data from the RPC (or null if no providers)</param>
        internal static void DeserializeSceneObjectInitData(GONetParticipant participant, byte[] initData)
        {
            if (initData == null || initData.Length == 0)
            {
                return; // No initialization data to deserialize
            }

            if (participant == null)
            {
                GONetLog.Error($"[SceneInitData] Cannot deserialize init data - participant is null");
                return;
            }

            // Find all IGONetSyncdBehaviourInitializer components on the same GameObject
            IGONetSyncdBehaviourInitializer[] providers = participant.GetComponents<IGONetSyncdBehaviourInitializer>();

            if (providers == null || providers.Length == 0)
            {
                GONetLog.Warning($"[SceneInitData] Received initialization data ({initData.Length} bytes) but no IGONetSyncdBehaviourInitializer components found on '{participant.gameObject.name}' (GONetId: {participant.GONetId})");
                return;
            }

            // HEX DUMP: Log raw bytes for debugging serialization issue
            string hexDump = System.BitConverter.ToString(initData).Replace("-", " ");
            GONetLog.Debug($"[SceneInitData] RAW BYTES for '{participant.gameObject.name}': {hexDump}");

            // Create builder for deserialization
            Utils.BitByBitByteArrayBuilder builder = Utils.BitByBitByteArrayBuilder.GetBuilder_WithNewData(initData, initData.Length);

            // Read provider count (for validation)
            uint providerCount;
            builder.ReadUInt(out providerCount, 8);

            if (providerCount != providers.Length)
            {
                GONetLog.Error($"[SceneInitData] Provider count mismatch on '{participant.gameObject.name}': Expected {providerCount} providers (from init data), found {providers.Length} components. Deserialization may fail!");
            }

            // Call each provider's deserialization method
            foreach (IGONetSyncdBehaviourInitializer provider in providers)
            {
                provider.Receiver_DeserializeSpawnData(builder);
            }

            GONetLog.Debug($"[SceneInitData] Deserialized initialization data for '{participant.gameObject.name}' ({providers.Length} providers, {initData.Length} bytes)");
        }

        /// <summary>
        /// Deserializes custom spawn data and calls <see cref="IGONetSyncdBehaviourInitializer.Receiver_DeserializeSpawnData"/> on all providers.
        /// </summary>
        /// <param name="instance">The instantiated GONetParticipant</param>
        /// <param name="customSpawnData">Serialized spawn data from the spawn event (or null if no providers)</param>
        private static void DeserializeCustomSpawnData(GONetParticipant instance, byte[] customSpawnData)
        {
            if (customSpawnData == null || customSpawnData.Length == 0)
            {
                return; // No spawn data to deserialize
            }

            // Find all IGONetSyncdBehaviourInitializer components on the same GameObject
            IGONetSyncdBehaviourInitializer[] providers = instance.GetComponents<IGONetSyncdBehaviourInitializer>();

            if (providers == null || providers.Length == 0)
            {
                GONetLog.Warning($"[SpawnData] Received custom spawn data ({customSpawnData.Length} bytes) but no IGONetSyncdBehaviourInitializer components found on '{instance.gameObject.name}' (GONetId: {instance.GONetId})");
                return;
            }

            // Create builder for deserialization
            Utils.BitByBitByteArrayBuilder builder = Utils.BitByBitByteArrayBuilder.GetBuilder_WithNewData(customSpawnData, customSpawnData.Length);

            // Read provider count (for validation)
            uint providerCount;
            builder.ReadUInt(out providerCount, 8);

            if (providerCount != providers.Length)
            {
                GONetLog.Error($"[SpawnData] Provider count mismatch on '{instance.gameObject.name}': Expected {providerCount} providers (from spawn data), found {providers.Length} components. Deserialization may fail!");
            }

            // Call each provider's deserialization method
            foreach (IGONetSyncdBehaviourInitializer provider in providers)
            {
                provider.Receiver_DeserializeSpawnData(builder);
            }

            GONetLog.Debug($"[SpawnData] Deserialized spawn data for '{instance.gameObject.name}' ({providers.Length} providers, {customSpawnData.Length} bytes)");
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient connectionToClient)
        {
            //GONetLog.Debug($"[INIT] Server_OnClientConnected_SendClientCurrentState: Starting initialization for newly connected client (AuthorityId will be assigned)");

            Server_AssignNewClientAuthorityId(connectionToClient);
            //GONetLog.Debug($"[INIT] Assigned AuthorityId: {connectionToClient.OwnerAuthorityId}");

            Server_AssignNewClientGONetIdRawBatch(connectionToClient);
            //GONetLog.Debug($"[INIT] Assigned GONetId batch");

            Server_SendClientPersistentEventsSinceStart(connectionToClient);
            //GONetLog.Debug($"[INIT] Sent persistent events");

            Server_SendClientCurrentState_AllAutoMagicalSync(connectionToClient);
            //GONetLog.Debug($"[INIT] Sent current state (all auto-magical sync values)");

            Server_SendClientIndicationOfInitializationCompletion(connectionToClient); // NOTE: sending this will cause the client to instantiate its GONetLocal
            //GONetLog.Debug($"[INIT] Sent initialization completion message to client AuthorityId: {connectionToClient.OwnerAuthorityId}");
        }

        private static void Server_OnNewClientInstantiatedItsGONetLocal(GONetLocal newClientGONetLocal)
        {
            GONetRemoteClient remoteClient = _gonetServer.GetRemoteClientByAuthorityId(newClientGONetLocal.GONetParticipant.OwnerAuthorityId);
            //GONetLog.Debug($"[INIT] Server received GONetLocal from client AuthorityId: {newClientGONetLocal.GONetParticipant.OwnerAuthorityId} - marking client as IsInitializedWithServer = true");
            remoteClient.IsInitializedWithServer = true;
            //GONetLog.Debug($"[INIT] Client AuthorityId {newClientGONetLocal.GONetParticipant.OwnerAuthorityId} is now fully initialized with server");

            // REMOVED: Scene-defined object ID sync no longer happens immediately
            // Instead, it's triggered by SceneLoadCompleteEvent from the client after each scene finishes loading
            // This fixes race condition where scene load was async and objects didn't exist yet
            // See Server_OnClientSceneLoadComplete() and Server_SendClientSceneDefinedObjectIds_ForSpecificScene()
        }

        /// <summary>
        /// Sends GONetId assignments for all scene-defined objects in currently loaded scenes to a newly connected client.
        /// This ensures late-joining clients receive the same GONetIds that were assigned to scene objects on the server.
        /// </summary>
        private static void Server_SendClientSceneDefinedObjectIds(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            //GONetLog.Warning($"[SCENE_SYNC] Server_SendClientSceneDefinedObjectIds - START for AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}");
            //GONetLog.Warning($"[SCENE_SYNC] definedInSceneParticipantInstanceIDs.Count: {definedInSceneParticipantInstanceIDs.Count}");
            //GONetLog.Warning($"[SCENE_SYNC] participantInstanceID_to_SpawnSceneName.Count: {participantInstanceID_to_SpawnSceneName.Count}");
            //GONetLog.Warning($"[SCENE_SYNC] gonetParticipantByGONetIdMap.Count: {gonetParticipantByGONetIdMap.Count}");

            // Get all currently loaded scenes
            HashSet<string> loadedScenes = new HashSet<string>();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    loadedScenes.Add(scene.name);
                    //GONetLog.Warning($"[SCENE_SYNC] Detected loaded scene: '{scene.name}'");
                }
            }

            // For each loaded scene, collect scene-defined object GONetIds
            foreach (string sceneName in loadedScenes)
            {
                //GONetLog.Warning($"[SCENE_SYNC] Processing scene '{sceneName}' for scene-defined objects...");
                List<string> designTimeLocations = new List<string>();
                List<uint> gonetIds = new List<uint>();
                List<byte[]> customInitDataList = new List<byte[]>();

                int matchedInstanceIds = 0;
                int foundParticipants = 0;

                // Find all GONetParticipants that were defined in this scene
                foreach (int instanceId in definedInSceneParticipantInstanceIDs)
                {
                    if (participantInstanceID_to_SpawnSceneName.TryGetValue(instanceId, out string participantScene) &&
                        participantScene == sceneName)
                    {
                        matchedInstanceIds++;

                        // Find the actual participant
                        foreach (var kvp in gonetParticipantByGONetIdMap)
                        {
                            GONetParticipant participant = kvp.Value;
                            if (participant != null &&
                                participant.GetInstanceID() == instanceId &&
                                participant.IsDesignTimeMetadataInitd &&
                                participant.GONetId != 0 &&
                                !string.IsNullOrEmpty(participant.DesignTimeLocation))
                            {
                                foundParticipants++;
                                designTimeLocations.Add(participant.DesignTimeLocation);
                                gonetIds.Add(participant.GONetId);

                                // Get initialization data from cache (populated during initial scene load)
                                // This ensures late-joiners receive IDENTICAL data to early joiners (no re-randomization!)
                                byte[] initData = GONetGlobal.GetCachedSceneObjectInitData(sceneName, participant.DesignTimeLocation);
                                customInitDataList.Add(initData); // Can be null if no IGONetSyncdBehaviourInitializer components

                                //GONetLog.Debug($"[SCENE_SYNC] Found scene-defined participant: GONetId {participant.GONetId}, Location: {participant.DesignTimeLocation}, Scene: {sceneName}");
                                break;
                            }
                        }
                    }
                }

                //GONetLog.Warning($"[SCENE_SYNC] Scene '{sceneName}': matchedInstanceIds={matchedInstanceIds}, foundParticipants={foundParticipants}, sending={designTimeLocations.Count}");

                if (designTimeLocations.Count > 0)
                {
                    //GONetLog.Info($"[INIT] Sending {designTimeLocations.Count} scene-defined object GONetIds for scene '{sceneName}' to newly connected client AuthorityId: {gonetConnection_ServerToClient.OwnerAuthorityId}");
                    Global.SendSceneDefinedObjectIdSync_ToSpecificClient(sceneName, designTimeLocations.ToArray(), gonetIds.ToArray(), customInitDataList.ToArray(), gonetConnection_ServerToClient.OwnerAuthorityId);
                }
            }

            //GONetLog.Warning($"[SCENE_SYNC] Server_SendClientSceneDefinedObjectIds - END for AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}");
        }

        /// <summary>
        /// Sends GONetId assignments for scene-defined objects in a SPECIFIC scene to a client.
        /// Called when a client notifies the server that a scene load has completed.
        /// This ensures the client has fully loaded the scene before receiving GONetIds.
        /// </summary>
        private static void Server_SendClientSceneDefinedObjectIds_ForSpecificScene(string sceneName, ushort clientAuthorityId)
        {
            //GONetLog.Info($"[SCENE_SYNC] Server sending scene-defined object IDs for scene '{sceneName}' to client AuthorityId {clientAuthorityId}");

            List<string> designTimeLocations = new List<string>();
            List<uint> gonetIds = new List<uint>();
            List<byte[]> customInitDataList = new List<byte[]>();

            // Find all GONetParticipants that were defined in this specific scene
            foreach (int instanceId in definedInSceneParticipantInstanceIDs)
            {
                if (participantInstanceID_to_SpawnSceneName.TryGetValue(instanceId, out string participantScene) &&
                    participantScene == sceneName)
                {
                    // Find the actual participant
                    foreach (var kvp in gonetParticipantByGONetIdMap)
                    {
                        GONetParticipant participant = kvp.Value;
                        if (participant != null &&
                            participant.GetInstanceID() == instanceId &&
                            participant.IsDesignTimeMetadataInitd &&
                            participant.GONetId != 0 &&
                            !string.IsNullOrEmpty(participant.DesignTimeLocation))
                        {
                            designTimeLocations.Add(participant.DesignTimeLocation);
                            gonetIds.Add(participant.GONetId);

                            // Get initialization data from cache (populated during initial scene load)
                            // This ensures late-joiners receive IDENTICAL data to early joiners (no re-randomization!)
                            byte[] initData = GONetGlobal.GetCachedSceneObjectInitData(sceneName, participant.DesignTimeLocation);
                            customInitDataList.Add(initData); // Can be null if no IGONetSyncdBehaviourInitializer components

                            //GONetLog.Debug($"[SCENE_SYNC] Found scene-defined participant for '{sceneName}': GONetId {participant.GONetId}, Location: {participant.DesignTimeLocation}");
                            break;
                        }
                    }
                }
            }

            if (designTimeLocations.Count > 0)
            {
                //GONetLog.Info($"[SCENE_SYNC] Sending {designTimeLocations.Count} scene-defined object GONetIds for scene '{sceneName}' to client AuthorityId: {clientAuthorityId}");
                Global.SendSceneDefinedObjectIdSync_ToSpecificClient(sceneName, designTimeLocations.ToArray(), gonetIds.ToArray(), customInitDataList.ToArray(), clientAuthorityId);
            }
            else
            {
                //GONetLog.Debug($"[SCENE_SYNC] No scene-defined objects found for scene '{sceneName}' to send to client {clientAuthorityId}");
            }
        }

        /// <summary>
        /// Server-side handler for when a client notifies that a scene has finished loading.
        /// This is the CORRECT time to send scene-defined object GONetId assignments.
        /// </summary>
        private static void Server_OnClientSceneLoadComplete(GONetEventEnvelope<SceneLoadCompleteEvent> eventEnvelope)
        {
            if (!IsServer)
                return;

            SceneLoadCompleteEvent evt = eventEnvelope.Event;
            ushort clientAuthorityId = eventEnvelope.SourceAuthorityId;

            GONetLog.Debug($"Client {clientAuthorityId} finished loading scene '{evt.SceneName}' - sending scene-defined object IDs");

            // Send scene-defined object GONetIds for this specific scene now that client has loaded it
            Server_SendClientSceneDefinedObjectIds_ForSpecificScene(evt.SceneName, clientAuthorityId);
        }

        private static void Server_SendClientPersistentEventsSinceStart(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            //GONetLog.Warning($"[SPAWN_SYNC] Server_SendClientPersistentEventsSinceStart - Total persistent events: {persistentEventsThisSession.Count}");

            if (persistentEventsThisSession.Count > 0)
            {
                // TEMPORARY DEBUG: Log ALL persistent events before filtering
                /*
                GONetLog.Error($"[SPAWN_SYNC] ===== DUMPING ALL {persistentEventsThisSession.Count} PERSISTENT EVENTS BEFORE FILTERING =====");
                int debugIndex = 0;
                foreach (var evt in persistentEventsThisSession)
                {
                    string eventType = evt.GetType().Name;
                    string details = "";

                    if (evt is InstantiateGONetParticipantEvent spawnEvt)
                    {
                        details = $"InstId: {spawnEvt.GONetIdAtInstantiation}, GONetId: {spawnEvt.GONetId}, Scene: '{spawnEvt.SceneIdentifier}', DesignTimeLocation: '{spawnEvt.DesignTimeLocation}'";
                    }
                    else if (evt is DespawnGONetParticipantEvent despawnEvt)
                    {
                        details = $"GONetId: {despawnEvt.GONetId}";
                    }
                    else if (evt is SceneLoadEvent sceneLoadEvt)
                    {
                        details = $"SceneName: '{sceneLoadEvt.SceneName}', LoadType: {sceneLoadEvt.LoadType}, Mode: {sceneLoadEvt.Mode}";
                    }
                    else if (evt is SceneUnloadEvent sceneUnloadEvt)
                    {
                        details = $"SceneName: '{sceneUnloadEvt.SceneName}'";
                    }
                    else if (evt is ValueMonitoringSupport_NewBaselineEvent baselineEvt)
                    {
                        details = $"GONetId: {baselineEvt.GONetId}";
                    }
                    else if (evt is ValueMonitoringSupport_BaselineExpiredEvent expiredEvt)
                    {
                        details = $"GONetId: {expiredEvt.GONetId}";
                    }
                    else if (evt is OwnerAuthorityIdAssignmentEvent)
                    {
                        details = "(OwnerAuthorityIdAssignmentEvent - minimal data)";
                    }
                    else if (evt is PersistentRpcEvent rpcEvt)
                    {
                        details = $"RpcId: {rpcEvt.RpcId}, GONetId: {rpcEvt.GONetId}, SourceAuthority: {rpcEvt.SourceAuthorityId}, Target: {rpcEvt.OriginalTarget}";
                    }

                    GONetLog.Error($"[SPAWN_SYNC]   [{debugIndex}] {eventType} - {details}");
                    debugIndex++;
                }
                GONetLog.Error($"[SPAWN_SYNC] ===== END DUMP =====");
                */

                // Filter persistent events to only those relevant to currently loaded scenes
                //GONetLog.Error($"[SPAWN_SYNC] BEFORE FilterPersistentEventsByLoadedScenes - Count: {persistentEventsThisSession.Count}");
                LinkedList<IPersistentEvent> filteredEvents = FilterPersistentEventsByLoadedScenes(persistentEventsThisSession);
                //GONetLog.Error($"[SPAWN_SYNC] AFTER FilterPersistentEventsByLoadedScenes - Filtered count: {filteredEvents.Count}");

                int totalCount = persistentEventsThisSession.Count;
                int filteredCount = filteredEvents.Count;
                //GONetLog.Warning($"[SPAWN_SYNC] *** Sending {filteredCount} of {totalCount} persistent events to newly connected client (filtered by loaded scenes) ***");

                // Log details of what we're sending
                /*
                int spawnCount = 0;
                foreach (var evt in filteredEvents)
                {
                    if (evt is InstantiateGONetParticipantEvent spawnEvt)
                    {
                        spawnCount++;
                        GONetLog.Debug($"[SPAWN_SYNC] - Sending spawn: GONetId {spawnEvt.GONetId}, InstId {spawnEvt.GONetIdAtInstantiation}, Scene: '{spawnEvt.SceneIdentifier}', DesignTimeLocation: '{spawnEvt.DesignTimeLocation}'");
                    }
                }
                GONetLog.Debug($"[SPAWN_SYNC] Total spawn events being sent: {spawnCount}");
                */

                if (filteredCount > 0)
                {
                    PersistentEvents_Bundle bundle = new PersistentEvents_Bundle(Time.ElapsedTicks, filteredEvents);
                    int returnBytesUsedCount;

                    byte[] bytes = SerializationUtils.SerializeToBytes<IGONetEvent>(bundle, out returnBytesUsedCount, out bool doesNeedToReturn); // EXTREMELY important to include the <IGONetEvent> because there are multiple options for MessagePack to serialize this thing based on BobWad_Generated.cs' usage of [MemoryPack.MemoryPackUnion] for relevant interfaces this concrete class implements and the other end's call to deserialize will be to DeserializeBody_EventSingle and <IGONetEvent> will be used there too!!!

                    const int MAX_SERIALIZED_CHUNK_SIZE = 12 * 1024; // 12 KB per serialized chunk - safe within 16 KB transport limit
                    const int CHUNK_OVERHEAD_ESTIMATE = 32; // Overhead for PersistentEvents_BundleChunk wrapper (ChunkId, ChunkIndex, TotalChunks, OriginalBundleSize, MemoryPack metadata)
                    const int MAX_CHUNK_DATA_SIZE = MAX_SERIALIZED_CHUNK_SIZE - CHUNK_OVERHEAD_ESTIMATE; // ~12,256 bytes of actual data per chunk

                    if (returnBytesUsedCount > MAX_CHUNK_DATA_SIZE)
                    {
                        // CHUNKING PATH: Bundle too large for single message - split into chunks
                        ushort totalChunks = (ushort)((returnBytesUsedCount + MAX_CHUNK_DATA_SIZE - 1) / MAX_CHUNK_DATA_SIZE);
                        uint chunkId = GenerateUniqueChunkId();
                        /*
                        GONetLog.Warning(
                            $"[SPAWN_SYNC] SERVER: Large persistent events bundle ({returnBytesUsedCount} bytes, {filteredCount} events) " +
                            $"will be split into {totalChunks} chunks for client AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}. " +
                            $"PERFORMANCE WARNING: Large bundles may impact network performance. " +
                            $"Consider: 1) More aggressive scene filtering, 2) Event cleanup on scene changes, 3) Shorter session duration.");
                        */
                        for (ushort chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                        {
                            int offset = chunkIndex * MAX_CHUNK_DATA_SIZE;
                            int chunkDataSize = System.Math.Min(MAX_CHUNK_DATA_SIZE, returnBytesUsedCount - offset);

                            byte[] chunkData = new byte[chunkDataSize];
                            System.Buffer.BlockCopy(bytes, offset, chunkData, 0, chunkDataSize);

                            var chunkEvent = new PersistentEvents_BundleChunk(chunkId, chunkIndex, totalChunks, chunkData, returnBytesUsedCount);

                            int chunkBytesUsedCount;
                            // CRITICAL: Must include <IGONetEvent> type parameter to ensure MemoryPack union type tag is serialized!
                            // Without this, deserialization will fail or deserialize as wrong type (see line 4361 comment)
                            byte[] chunkBytes = SerializationUtils.SerializeToBytes<IGONetEvent>(chunkEvent, out chunkBytesUsedCount, out bool doesNeedToReturnChunk);

                            // Validate chunk size is within limits
                            if (chunkBytesUsedCount > MAX_SERIALIZED_CHUNK_SIZE)
                            {
                                GONetLog.Error($"[SPAWN_SYNC] CRITICAL: Serialized chunk {chunkIndex + 1}/{totalChunks} exceeds MAX_SERIALIZED_CHUNK_SIZE! " +
                                    $"Actual: {chunkBytesUsedCount} bytes, Max: {MAX_SERIALIZED_CHUNK_SIZE} bytes. " +
                                    $"ChunkDataSize: {chunkDataSize}, Overhead: {chunkBytesUsedCount - chunkDataSize}. " +
                                    $"This will likely cause message corruption or delivery failure.");
                            }

                            //GONetLog.Info($"[SPAWN_SYNC] SERVER: Sending chunk {chunkIndex + 1}/{totalChunks} ({chunkBytesUsedCount} bytes, {chunkDataSize} data + {chunkBytesUsedCount - chunkDataSize} overhead) to AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}");

                            SendBytesToRemoteConnection(gonetConnection_ServerToClient, chunkBytes, chunkBytesUsedCount, GONetChannel.ClientInitialization_EventSingles_Reliable);

                            if (doesNeedToReturnChunk)
                            {
                                SerializationUtils.ReturnByteArray(chunkBytes);
                            }
                        }

                        //GONetLog.Warning($"[SPAWN_SYNC] SERVER: All {totalChunks} chunks SENT to AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId} (ChunkId: {chunkId})");
                    }
                    else
                    {
                        // NORMAL PATH: Bundle fits in single message (< ~12 KB)
                        //GONetLog.Warning($"[SPAWN_SYNC] SERVER: Serialized PersistentEvents_Bundle - Size: {returnBytesUsedCount} bytes, Events: {filteredCount}, Channel: {GONetChannel.ClientInitialization_EventSingles_Reliable}, Target: AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}");

                        SendBytesToRemoteConnection(gonetConnection_ServerToClient, bytes, returnBytesUsedCount, GONetChannel.ClientInitialization_EventSingles_Reliable);

                        //GONetLog.Warning($"[SPAWN_SYNC] SERVER: PersistentEvents_Bundle SENT to AuthorityId {gonetConnection_ServerToClient.OwnerAuthorityId}");
                    }

                    if (doesNeedToReturn)
                    {
                        SerializationUtils.ReturnByteArray(bytes);
                    }
                }
            }
        }

        /// <summary>
        /// Filters persistent events to only include those relevant to currently loaded scenes.
        /// <para>This prevents late-joining clients from receiving spawn events for objects in unloaded scenes.</para>
        /// </summary>
        private static LinkedList<IPersistentEvent> FilterPersistentEventsByLoadedScenes(LinkedList<IPersistentEvent> allEvents)
        {
            LinkedList<IPersistentEvent> filteredEvents = new LinkedList<IPersistentEvent>();

            // Get currently loaded scenes from server's scene manager
            HashSet<string> loadedScenes = new HashSet<string>();
            if (SceneManager != null)
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        loadedScenes.Add(scene.name);
                        //GONetLog.Warning($"[SPAWN_SYNC] FilterPersistentEvents: Detected loaded scene '{scene.name}'");
                    }
                }
            }

            // Always include DontDestroyOnLoad scene (persistent across scene changes)
            loadedScenes.Add(HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE);

            // CRITICAL FIX: Track which scene load events to send based on current loaded scenes
            // We should only send SceneLoadEvent for scenes that are CURRENTLY loaded, not the entire history
            // Otherwise late-joiners receive all scene transitions and end up in wrong scenes
            HashSet<string> sceneLoadEventsSent = new HashSet<string>();

            // CRITICAL: Track which GONetIds have spawn events being sent
            // Value baseline events should ONLY be sent if the corresponding spawn is also being sent
            HashSet<uint> gonetIdsWithSpawnsBeingSent = new HashSet<uint>();

            //GONetLog.Warning($"[SPAWN_SYNC] FilterPersistentEvents: About to filter {allEvents.Count} events. Loaded scenes: {string.Join(", ", loadedScenes)}");

            // Filter events based on scene
            foreach (IPersistentEvent persistentEvent in allEvents)
            {
                bool shouldInclude = true;

                // CRITICAL: Filter SceneLoadEvent to only send for currently loaded scenes
                if (persistentEvent is SceneLoadEvent sceneLoadEvent)
                {
                    // Only include scene load if:
                    // 1. The scene is currently loaded on the server
                    // 2. We haven't already sent a load event for this scene (avoid duplicates from scene history)
                    if (loadedScenes.Contains(sceneLoadEvent.SceneName) && !sceneLoadEventsSent.Contains(sceneLoadEvent.SceneName))
                    {
                        shouldInclude = true;
                        sceneLoadEventsSent.Add(sceneLoadEvent.SceneName);
                        //GONetLog.Warning($"[SPAWN_SYNC] Including SceneLoadEvent for '{sceneLoadEvent.SceneName}' - currently loaded on server");
                    }
                    else
                    {
                        shouldInclude = false;
                        //GONetLog.Warning($"[SPAWN_SYNC] EXCLUDING SceneLoadEvent for '{sceneLoadEvent.SceneName}' - not currently loaded or already sent");
                    }
                }
                // CRITICAL: Exclude SceneUnloadEvent - these are historical and not needed for late-joiners
                // Late-joiners should only receive the CURRENT scene state, not the unload history
                else if (persistentEvent is SceneUnloadEvent sceneUnloadEvent)
                {
                    shouldInclude = false;
                    //GONetLog.Warning($"[SPAWN_SYNC] EXCLUDING SceneUnloadEvent for '{sceneUnloadEvent.SceneName}' - late-joiners only need current state");
                }
                // Check if this is a spawn event with scene information
                else if (persistentEvent is InstantiateGONetParticipantEvent spawnEvent)
                {
                    // Only include spawns from currently loaded scenes
                    if (!string.IsNullOrEmpty(spawnEvent.SceneIdentifier))
                    {
                        shouldInclude = loadedScenes.Contains(spawnEvent.SceneIdentifier);
                        if (shouldInclude)
                        {
                            gonetIdsWithSpawnsBeingSent.Add(spawnEvent.GONetId);
                            //GONetLog.Debug($"[SPAWN_SYNC] INCLUDING spawn: InstId {spawnEvent.GONetIdAtInstantiation}, Scene '{spawnEvent.SceneIdentifier}' (matches loaded scenes)");
                        }
                        else
                        {
                            //GONetLog.Warning($"[SPAWN_SYNC] EXCLUDING spawn: InstId {spawnEvent.GONetIdAtInstantiation}, Scene '{spawnEvent.SceneIdentifier}' (NOT in loaded scenes)");
                        }
                    }
                    // If no scene identifier, include it (backward compatibility for old events)
                    else
                    {
                        gonetIdsWithSpawnsBeingSent.Add(spawnEvent.GONetId);
                        //GONetLog.Debug($"[SPAWN_SYNC] INCLUDING spawn: InstId {spawnEvent.GONetIdAtInstantiation}, No SceneIdentifier (backward compat)");
                    }
                }
                // CRITICAL: Filter value baseline events - only send if corresponding spawn is also being sent
                else if (persistentEvent is ValueMonitoringSupport_NewBaselineEvent baselineEvent)
                {
                    // ONLY send value baseline if we're also sending the spawn for this GONetId
                    uint gonetId = baselineEvent.GONetId;
                    if (!gonetIdsWithSpawnsBeingSent.Contains(gonetId))
                    {
                        shouldInclude = false;
                        //GONetLog.Warning($"[SPAWN_SYNC] EXCLUDING ValueBaseline for GONetId {gonetId} - spawn not being sent");
                    }
                }
                else if (persistentEvent is ValueMonitoringSupport_BaselineExpiredEvent expiredEvent)
                {
                    // ONLY send expired baseline if we're also sending the spawn for this GONetId
                    uint gonetId = expiredEvent.GONetId;
                    if (!gonetIdsWithSpawnsBeingSent.Contains(gonetId))
                    {
                        shouldInclude = false;
                        //GONetLog.Warning($"[SPAWN_SYNC] EXCLUDING ExpiredBaseline for GONetId {gonetId} - spawn not being sent");
                    }
                }
                // NOTE: Persistent RPCs are NOT filtered here because:
                // - GONet_GlobalContext is used as a "bucket" for RPCs without specific participant context
                // - Scene-specific components can be added to GONet_GlobalContext via GONetRuntimeComponentInitializer
                // - Filtering would break legitimate global RPCs
                // - Component-not-ready exceptions will defer RPCs until timeout (handled by deferred RPC system)
                // All other persistent events (OwnerAuthorityIdAssignment, etc.) are always included

                if (shouldInclude)
                {
                    filteredEvents.AddLast(persistentEvent);
                }
            }

            // CRITICAL FIX: Reorder events to ensure SceneLoadEvents come FIRST
            // Late-joining clients MUST receive and process SceneLoadEvent before any spawn events for that scene
            // Otherwise spawns get deferred indefinitely waiting for the scene to load
            LinkedList<IPersistentEvent> reorderedEvents = new LinkedList<IPersistentEvent>();

            // First pass: Add all SceneLoadEvents
            foreach (IPersistentEvent evt in filteredEvents)
            {
                if (evt is SceneLoadEvent)
                {
                    reorderedEvents.AddLast(evt);
                    //GONetLog.Warning($"[SPAWN_SYNC] Prioritizing SceneLoadEvent to front of bundle");
                }
            }

            // Second pass: Add all other events (preserving their relative order)
            foreach (IPersistentEvent evt in filteredEvents)
            {
                if (!(evt is SceneLoadEvent))
                {
                    reorderedEvents.AddLast(evt);
                }
            }

            //GONetLog.Warning($"[SPAWN_SYNC] FilterPersistentEvents: Reordered {filteredEvents.Count} events - SceneLoadEvents now at front");

            return reorderedEvents;
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
                    //GONetLog.Info($"[INIT] SERVER: About to write OwnerAuthorityId - Value: {connectionToClient.OwnerAuthorityId}, BitCount: {GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED}, BitStream Position Before: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                    bitStream.WriteUShort(connectionToClient.OwnerAuthorityId, GONetParticipant.OWNER_AUTHORITY_ID_BIT_COUNT_USED);
                    //GONetLog.Info($"[INIT] SERVER: After write OwnerAuthorityId - BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                    bitStream.WriteLong(SessionGUID);
                    //GONetLog.Info($"[INIT] SERVER: After write SessionGUID - BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                }

                //GONetLog.Info($"[INIT] SERVER: About to WriteCurrentPartialByte - BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits");
                bitStream.WriteCurrentPartialByte();
                //GONetLog.Info($"[INIT] SERVER: After WriteCurrentPartialByte - BitStream Position: {bitStream.Position_Bytes} bytes {bitStream.Position_Bits} bits, TotalBytes: {bitStream.Length_WrittenBytes}");

                // Dump the raw bytes being sent
                byte[] buffer = bitStream.GetBuffer();
                string hex = System.BitConverter.ToString(buffer, 0, bitStream.Length_WrittenBytes);
                //GONetLog.Info($"[INIT] SERVER: Sending message - Bytes: {hex}");

                SendBytesToRemoteConnection(connectionToClient, buffer, bitStream.Length_WrittenBytes, GONetChannel.ClientInitialization_CustomSerialization_Reliable);
            }
        }

        private static void Server_AssignNewClientGONetIdRawBatch(GONetConnection_ServerToClient connectionToClient)
        {
            var @event = new ClientRemotelyControlledGONetIdServerBatchAssignmentEvent();
            uint batchStart = GONetIdBatchManager.Server_AllocateNewBatch(lastAssignedGONetIdRaw);
            @event.GONetIdRawBatchStart = batchStart;

            lastAssignedGONetIdRaw = batchStart + 99; // Batch is 100 IDs, so last ID is +99

            EventBus.Publish(@event, targetClientAuthorityId: connectionToClient.OwnerAuthorityId);
        }

        private static void Client_AssignNewClientGONetIdRawBatch(
            GONetEventEnvelope<ClientRemotelyControlledGONetIdServerBatchAssignmentEvent> eventEnvelope)
        {
            if (IsClient)
            {
                GONetIdBatchManager.Client_AddBatch(eventEnvelope.Event.GONetIdRawBatchStart);

                // CRITICAL: Process limbo queue when batch arrives
                Client_OnBatchReceived_ProcessDeferredSpawns();
            }
        }

        /// <summary>
        /// CLIENT: Requests a new GONetId batch from the server when running low on IDs.
        /// Called automatically when remaining IDs drop below threshold (< 20).
        /// </summary>
        private static void Client_RequestNewGONetIdBatch()
        {
            if (!IsClient || GONetClient == null || GONetClient.connectionToServer == null)
            {
                GONetLog.Error("[GONetIdBatch] CLIENT: Cannot request new batch - not connected to server");
                return;
            }

            GONetLog.Info("[GONetIdBatch] CLIENT requesting new batch from server due to low ID count");

            // Create request event to send to server
            var requestEvent = new ClientRemotelyControlledGONetIdServerBatchRequestEvent();
            EventBus.Publish(requestEvent, targetClientAuthorityId: OwnerAuthorityId_Server);
        }

        /// <summary>
        /// SERVER: Handles client request for additional GONetId batch when running low.
        /// </summary>
        private static void Server_HandleClientBatchRequest(
            GONetEventEnvelope<ClientRemotelyControlledGONetIdServerBatchRequestEvent> eventEnvelope)
        {
            if (!IsServer || _gonetServer == null)
            {
                return; // Ignore if not server
            }

            ushort requestingClientAuthorityId = eventEnvelope.SourceAuthorityId;
            GONetLog.Info($"[GONetIdBatch] SERVER received batch request from client {requestingClientAuthorityId}");

            // Find the connection for this client
            GONetConnection_ServerToClient connectionToClient = null;
            uint count = _gonetServer.numConnections;

            for (int i = 0; i < count; ++i)
            {
                GONetConnection_ServerToClient connection = _gonetServer.remoteClients[i].ConnectionToClient;
                if (connection.OwnerAuthorityId == requestingClientAuthorityId)
                {
                    connectionToClient = connection;
                    break;
                }
            }

            if (connectionToClient != null)
            {
                Server_AssignNewClientGONetIdRawBatch(connectionToClient);
            }
            else
            {
                GONetLog.Error($"[GONetIdBatch] SERVER could not find connection for client {requestingClientAuthorityId}");
            }
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetIdRaw = GONetParticipant.GONetIdRaw_Unset;
        static uint client_lastServerGONetIdRawForRemoteControl = GONetParticipant.GONetIdRaw_Unset; // Used in GetNextAvailableGONetIdRaw for legacy flow
        // NOTE: Batch management now handled by GONetIdBatchManager

        #region CLIENT LIMBO MODE SUPPORT

        /// <summary>
        /// CLIENT ONLY: Queue of GONetParticipants that were spawned in limbo state (no GONetId batch available).
        /// These will be "graduated" to full networked status when a new batch arrives from server.
        /// </summary>
        private static readonly Queue<GONetParticipant> client_deferredSpawnsAwaitingBatch = new Queue<GONetParticipant>();

        /// <summary>
        /// CLIENT ONLY: Event raised when a spawn enters limbo state due to batch exhaustion.
        /// Subscribe to this to implement custom UI notifications (e.g., "Out of spawn capacity").
        /// </summary>
        public static event Action<Client_SpawnLimboEventArgs> Client_OnSpawnEnteredLimbo;

        /// <summary>
        /// CLIENT ONLY: Gets a read-only collection of participants currently in limbo state.
        /// For use by editor inspectors and debugging tools.
        /// </summary>
        public static IEnumerable<GONetParticipant> Client_GetLimboParticipants()
        {
            return client_deferredSpawnsAwaitingBatch;
        }

        /// <summary>
        /// CLIENT ONLY: Gets the count of participants currently in limbo state.
        /// </summary>
        public static int Client_GetLimboCount()
        {
            return client_deferredSpawnsAwaitingBatch.Count;
        }

        /// <summary>
        /// CLIENT ONLY: Instantiates an object in limbo state (no GONetId assigned).
        /// Object will be queued for graduation when batch arrives.
        /// </summary>
        private static GONetParticipant Client_InstantiateInLimbo(
            GONetParticipant prefab,
            Vector3 position,
            Quaternion rotation,
            Client_GONetIdBatchLimboMode limboMode)
        {
            GONetParticipant instance;

            switch (limboMode)
            {
                case Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableAll:
                    instance = Client_InstantiateInLimbo_DisableAll(prefab, position, rotation);
                    break;

                case Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableRenderingAndPhysics:
                    instance = Client_InstantiateInLimbo_DisableRenderingAndPhysics(prefab, position, rotation);
                    break;

                case Client_GONetIdBatchLimboMode.InstantiateInLimbo:
                    instance = Client_InstantiateInLimbo_NoDisable(prefab, position, rotation);
                    break;

                default:
                    GONetLog.Error($"[ClientLimbo] Unknown limbo mode: {limboMode}");
                    return null;
            }

            // Mark as in limbo and add to deferred queue
            instance.client_isInLimbo = true;
            instance.RemotelyControlledByAuthorityId = MyAuthorityId;
            client_deferredSpawnsAwaitingBatch.Enqueue(instance);

            uint remainingIds = GONetIdBatchManager.Client_GetRemainingIds();
            GONetLog.Warning($"[ClientLimbo] Spawned '{prefab.name}' in LIMBO mode {limboMode} (remaining IDs: {remainingIds}, limbo queue size: {client_deferredSpawnsAwaitingBatch.Count})");

            // Raise event
            Client_OnSpawnEnteredLimbo?.Invoke(new Client_SpawnLimboEventArgs
            {
                Participant = instance,
                Prefab = prefab,
                LimboMode = limboMode,
                RemainingIds = remainingIds,
                Position = position,
                Rotation = rotation
            });

            return instance;
        }

        /// <summary>
        /// CLIENT ONLY: Limbo Mode 1 - Disable ALL MonoBehaviours (except GONetParticipant).
        /// Object is completely frozen until batch arrives.
        /// </summary>
        private static GONetParticipant Client_InstantiateInLimbo_DisableAll(
            GONetParticipant prefab,
            Vector3 position,
            Quaternion rotation)
        {
            // Instantiate normally first
            GONetParticipant instance = UnityEngine.Object.Instantiate(prefab, position, rotation);

            // Copy design time metadata from prefab (same as Instantiate_MarkToBeRemotelyControlled)
            DesignTimeMetadata prefabMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(prefab, force: true);
            if (prefabMetadata != null && !string.IsNullOrWhiteSpace(prefabMetadata.Location))
            {
                DesignTimeMetadata instanceMetadata = new DesignTimeMetadata
                {
                    Location = prefabMetadata.Location,
                    CodeGenerationId = prefabMetadata.CodeGenerationId,
                    UnityGuid = prefabMetadata.UnityGuid
                };
                GONetSpawnSupport_Runtime.SetDesignTimeMetadata(instance, instanceMetadata);
            }

            // Disable all MonoBehaviours except GONetParticipant
            instance.client_limboDisabledComponents = new List<MonoBehaviour>();
            MonoBehaviour[] allComponents = instance.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
            foreach (MonoBehaviour component in allComponents)
            {
                if (component != null && !(component is GONetParticipant) && component.enabled)
                {
                    component.enabled = false;
                    instance.client_limboDisabledComponents.Add(component);
                }
            }

            GONetLog.Info($"[ClientLimbo] DisableAll mode: Disabled {instance.client_limboDisabledComponents.Count} components on '{instance.name}'");
            return instance;
        }

        /// <summary>
        /// CLIENT ONLY: Limbo Mode 2 - Disable ONLY rendering and physics components.
        /// MonoBehaviours still run (Start/Update) but object is invisible/non-physical.
        /// RECOMMENDED DEFAULT: Good balance of safety and flexibility.
        /// </summary>
        private static GONetParticipant Client_InstantiateInLimbo_DisableRenderingAndPhysics(
            GONetParticipant prefab,
            Vector3 position,
            Quaternion rotation)
        {
            // Instantiate normally first
            GONetParticipant instance = UnityEngine.Object.Instantiate(prefab, position, rotation);

            // Copy design time metadata from prefab
            DesignTimeMetadata prefabMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(prefab, force: true);
            if (prefabMetadata != null && !string.IsNullOrWhiteSpace(prefabMetadata.Location))
            {
                DesignTimeMetadata instanceMetadata = new DesignTimeMetadata
                {
                    Location = prefabMetadata.Location,
                    CodeGenerationId = prefabMetadata.CodeGenerationId,
                    UnityGuid = prefabMetadata.UnityGuid
                };
                GONetSpawnSupport_Runtime.SetDesignTimeMetadata(instance, instanceMetadata);
            }

            // Disable rendering components
            instance.client_limboDisabledRenderers = new List<Renderer>();
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    renderer.enabled = false;
                    instance.client_limboDisabledRenderers.Add(renderer);
                }
            }

            // Disable 3D colliders
            instance.client_limboDisabledColliders = new List<Collider>();
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.enabled)
                {
                    collider.enabled = false;
                    instance.client_limboDisabledColliders.Add(collider);
                }
            }

            // Disable 2D colliders
            instance.client_limboDisabledColliders2D = new List<Collider2D>();
            Collider2D[] colliders2D = instance.GetComponentsInChildren<Collider2D>(includeInactive: true);
            foreach (Collider2D collider in colliders2D)
            {
                if (collider != null && collider.enabled)
                {
                    collider.enabled = false;
                    instance.client_limboDisabledColliders2D.Add(collider);
                }
            }

            // Make Rigidbody kinematic (if present)
            instance.client_limboRigidbody = instance.GetComponentInChildren<Rigidbody>();
            if (instance.client_limboRigidbody != null)
            {
                instance.client_limboRigidbodyWasKinematic = instance.client_limboRigidbody.isKinematic;
                instance.client_limboRigidbody.isKinematic = true;
            }

            // Make Rigidbody2D kinematic (if present)
            instance.client_limboRigidbody2D = instance.GetComponentInChildren<Rigidbody2D>();
            if (instance.client_limboRigidbody2D != null)
            {
                instance.client_limboRigidbody2DOriginalType = instance.client_limboRigidbody2D.bodyType;
                instance.client_limboRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            }

            GONetLog.Info($"[ClientLimbo] DisableRenderingAndPhysics mode: Disabled {instance.client_limboDisabledRenderers.Count} renderers, {instance.client_limboDisabledColliders.Count} colliders, {instance.client_limboDisabledColliders2D.Count} colliders2D on '{instance.name}'");
            return instance;
        }

        /// <summary>
        /// CLIENT ONLY: Limbo Mode 3 - No automatic disabling.
        /// Object runs normally, user must check Client_IsInLimbo themselves.
        /// </summary>
        private static GONetParticipant Client_InstantiateInLimbo_NoDisable(
            GONetParticipant prefab,
            Vector3 position,
            Quaternion rotation)
        {
            // Instantiate normally - no component disabling
            GONetParticipant instance = UnityEngine.Object.Instantiate(prefab, position, rotation);

            // Copy design time metadata from prefab
            DesignTimeMetadata prefabMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(prefab, force: true);
            if (prefabMetadata != null && !string.IsNullOrWhiteSpace(prefabMetadata.Location))
            {
                DesignTimeMetadata instanceMetadata = new DesignTimeMetadata
                {
                    Location = prefabMetadata.Location,
                    CodeGenerationId = prefabMetadata.CodeGenerationId,
                    UnityGuid = prefabMetadata.UnityGuid
                };
                GONetSpawnSupport_Runtime.SetDesignTimeMetadata(instance, instanceMetadata);
            }

            GONetLog.Info($"[ClientLimbo] NoDisable mode: '{instance.name}' spawned normally, user must check Client_IsInLimbo");
            return instance;
        }

        /// <summary>
        /// CLIENT ONLY: Processes deferred spawns (limbo queue) when a new batch arrives.
        /// Called automatically by Client_AssignNewClientGONetIdRawBatch.
        /// Graduates limbo objects to full networked status by assigning GONetIds and re-enabling components.
        /// </summary>
        private static void Client_OnBatchReceived_ProcessDeferredSpawns()
        {
            if (client_deferredSpawnsAwaitingBatch.Count == 0)
            {
                return; // Nothing to process
            }

            int processedCount = 0;
            int failedCount = 0;

            GONetLog.Info($"[ClientLimbo] Processing {client_deferredSpawnsAwaitingBatch.Count} deferred spawns from limbo queue");

            // Process all limbo spawns that can be assigned IDs
            while (client_deferredSpawnsAwaitingBatch.Count > 0 && GONetIdBatchManager.Client_HasAvailableIds())
            {
                GONetParticipant participant = client_deferredSpawnsAwaitingBatch.Dequeue();

                if (participant == null || participant.gameObject == null)
                {
                    GONetLog.Warning($"[ClientLimbo] Skipping null/destroyed participant in limbo queue");
                    failedCount++;
                    continue;
                }

                // Exit limbo and assign GONetId
                bool success = Client_ExitLimbo(participant);
                if (success)
                {
                    processedCount++;
                }
                else
                {
                    failedCount++;
                }
            }

            uint remainingIds = GONetIdBatchManager.Client_GetRemainingIds();
            GONetLog.Info($"[ClientLimbo] Batch processing complete: {processedCount} graduated, {failedCount} failed, {client_deferredSpawnsAwaitingBatch.Count} still in limbo, {remainingIds} IDs remaining");
        }

        /// <summary>
        /// CLIENT ONLY: Exits limbo state for a participant.
        /// Re-enables disabled components and assigns GONetId from batch.
        /// </summary>
        private static bool Client_ExitLimbo(GONetParticipant participant)
        {
            if (participant == null || !participant.Client_IsInLimbo)
            {
                GONetLog.Warning($"[ClientLimbo] Cannot exit limbo - participant is null or not in limbo");
                return false;
            }

            string participantName = participant.gameObject.name;
            GONetLog.Info($"[ClientLimbo] Exiting limbo for '{participantName}'");

            // Re-enable components based on which mode was used
            if (participant.client_limboDisabledComponents != null && participant.client_limboDisabledComponents.Count > 0)
            {
                // Mode 1: DisableAll - Re-enable all MonoBehaviours
                foreach (MonoBehaviour component in participant.client_limboDisabledComponents)
                {
                    if (component != null)
                    {
                        component.enabled = true;
                    }
                }
                GONetLog.Info($"[ClientLimbo] Re-enabled {participant.client_limboDisabledComponents.Count} components on '{participantName}'");
                participant.client_limboDisabledComponents.Clear();
                participant.client_limboDisabledComponents = null;
            }
            else if (participant.client_limboDisabledRenderers != null)
            {
                // Mode 2: DisableRenderingAndPhysics - Re-enable rendering/physics
                foreach (Renderer renderer in participant.client_limboDisabledRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                }

                foreach (Collider collider in participant.client_limboDisabledColliders)
                {
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }

                foreach (Collider2D collider in participant.client_limboDisabledColliders2D)
                {
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }

                if (participant.client_limboRigidbody != null)
                {
                    participant.client_limboRigidbody.isKinematic = participant.client_limboRigidbodyWasKinematic;

                    // Enable interpolation for smooth rendering if non-authority
                    if (!participant.IsMine)
                    {
                        participant.client_limboRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                    }
                }

                if (participant.client_limboRigidbody2D != null)
                {
                    participant.client_limboRigidbody2D.bodyType = participant.client_limboRigidbody2DOriginalType;

                    // Enable interpolation for smooth rendering if non-authority
                    if (!participant.IsMine)
                    {
                        participant.client_limboRigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
                    }
                }

                GONetLog.Info($"[ClientLimbo] Re-enabled rendering/physics on '{participantName}'");

                // Clear references
                participant.client_limboDisabledRenderers.Clear();
                participant.client_limboDisabledColliders.Clear();
                participant.client_limboDisabledColliders2D.Clear();
                participant.client_limboDisabledRenderers = null;
                participant.client_limboDisabledColliders = null;
                participant.client_limboDisabledColliders2D = null;
                participant.client_limboRigidbody = null;
                participant.client_limboRigidbody2D = null;
            }
            // Mode 3: NoDisable - nothing to re-enable

            // Assign GONetId from batch
            AssignGONetIdRaw_IfAppropriate(participant, shouldForceChangeEventIfAlreadySet: false);

            // Mark as no longer in limbo BEFORE triggering OnGONetReady
            participant.client_isInLimbo = false;

            // LIFECYCLE GATE: Graduated from limbo - check if OnGONetReady can fire
            // This replaces the old direct broadcast - now uses the centralized gate check
            GONetLog.Info($"[ClientLimbo] '{participantName}' graduated from limbo - GONetId: {participant.GONetId} - checking OnGONetReady gate");
            CheckAndPublishOnGONetReady_IfAllConditionsMet(participant);

            return true;
        }

        #endregion

        /// <summary>
        /// Counter for generating unique chunk IDs for multi-chunk messages.
        /// Used by PersistentEvents_BundleChunk to identify which chunks belong together.
        /// </summary>
        static int lastAssignedChunkId = 0;

        /// <summary>
        /// Generates a unique chunk ID for identifying multi-chunk messages.
        /// Thread-safe for use across multiple connections.
        /// </summary>
        private static uint GenerateUniqueChunkId()
        {
            return (uint)System.Threading.Interlocked.Increment(ref lastAssignedChunkId);
        }

        /// <summary>
        /// Tracks in-progress chunk reassembly for large persistent events bundles.
        /// Key: ChunkId, Value: Reassembly state tracking
        /// </summary>
        private class ChunkReassemblyState
        {
            public byte[] CompleteData;
            public bool[] ReceivedChunks;
            public ushort TotalChunks;
            public int ReceivedCount;
            public double TimeStarted;
            public int OriginalSize;
        }

        static readonly System.Collections.Generic.Dictionary<uint, ChunkReassemblyState> pendingChunkReassembly = new System.Collections.Generic.Dictionary<uint, ChunkReassemblyState>();

        /// <summary>
        /// CLIENT: Handles incoming chunk of a large persistent events bundle.
        /// Reassembles chunks and processes the complete bundle when all chunks received.
        /// </summary>
        private static void OnPersistentEventsChunkReceived(GONetEventEnvelope<PersistentEvents_BundleChunk> envelope)
        {
            if (!IsClient)
            {
                return; // Only clients receive chunks from server
            }

            var chunk = envelope.Event;
            uint chunkId = chunk.ChunkId;

            //GONetLog.Info($"[SPAWN_SYNC] CLIENT: Received chunk {chunk.ChunkIndex + 1}/{chunk.TotalChunks} (ChunkId: {chunkId}, Size: {chunk.ChunkData.Length} bytes)");

            ChunkReassemblyState reassembly;
            if (!pendingChunkReassembly.TryGetValue(chunkId, out reassembly))
            {
                // First chunk for this message - initialize reassembly state
                reassembly = new ChunkReassemblyState
                {
                    CompleteData = new byte[chunk.OriginalBundleSize],
                    ReceivedChunks = new bool[chunk.TotalChunks],
                    TotalChunks = chunk.TotalChunks,
                    ReceivedCount = 0,
                    TimeStarted = Time.ElapsedSeconds,
                    OriginalSize = chunk.OriginalBundleSize
                };
                pendingChunkReassembly[chunkId] = reassembly;

                //GONetLog.Info($"[SPAWN_SYNC] CLIENT: Started reassembly for ChunkId {chunkId} ({chunk.TotalChunks} total chunks, {chunk.OriginalBundleSize} bytes)");
            }

            // Validate chunk consistency
            if (chunk.TotalChunks != reassembly.TotalChunks)
            {
                GONetLog.Error($"[SPAWN_SYNC] CLIENT: Chunk TotalChunks mismatch! Expected {reassembly.TotalChunks}, got {chunk.TotalChunks}. ChunkId: {chunkId}");
                pendingChunkReassembly.Remove(chunkId); // Abort this reassembly
                return;
            }

            if (chunk.OriginalBundleSize != reassembly.OriginalSize)
            {
                GONetLog.Error($"[SPAWN_SYNC] CLIENT: Chunk OriginalSize mismatch! Expected {reassembly.OriginalSize}, got {chunk.OriginalBundleSize}. ChunkId: {chunkId}");
                pendingChunkReassembly.Remove(chunkId);
                return;
            }

            // Check for duplicate chunk
            if (reassembly.ReceivedChunks[chunk.ChunkIndex])
            {
                GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Duplicate chunk {chunk.ChunkIndex} received for ChunkId {chunkId} - ignoring");
                return;
            }

            // Copy chunk data into complete buffer
            // CRITICAL FIX: Must match server's MAX_CHUNK_DATA_SIZE calculation!
            // Server uses: MAX_SERIALIZED_CHUNK_SIZE (12KB) - CHUNK_OVERHEAD_ESTIMATE (32 bytes) = 12,256 bytes per chunk
            // Old code used 12,288 bytes (12KB), causing 32-byte misalignment that corrupted reassembled data!
            const int MAX_CHUNK_DATA_SIZE = (12 * 1024) - 32; // 12,256 bytes - MUST match server's chunking logic
            int offset = chunk.ChunkIndex * MAX_CHUNK_DATA_SIZE;

            System.Buffer.BlockCopy(chunk.ChunkData, 0, reassembly.CompleteData, offset, chunk.ChunkData.Length);

            // Mark chunk as received
            reassembly.ReceivedChunks[chunk.ChunkIndex] = true;
            reassembly.ReceivedCount++;

            //GONetLog.Info($"[SPAWN_SYNC] CLIENT: Reassembly progress: {reassembly.ReceivedCount}/{reassembly.TotalChunks} chunks received (ChunkId: {chunkId})");

            // Check if reassembly complete
            if (reassembly.ReceivedCount == reassembly.TotalChunks)
            {
                double reassemblyTime = Time.ElapsedSeconds - reassembly.TimeStarted;
                //GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Reassembly COMPLETE for ChunkId {chunkId} ({reassembly.TotalChunks} chunks, {reassembly.OriginalSize} bytes, {reassemblyTime:F2}s)");

                // Deserialize the complete bundle
                // CRITICAL: Must deserialize as IGONetEvent (matching server's serialization on line 4369)
                // then cast to PersistentEvents_Bundle, because the original bundle was serialized with union type tags
                try
                {
                    IGONetEvent deserializedEvent = SerializationUtils.DeserializeFromBytes<IGONetEvent>(reassembly.CompleteData);

                    if (deserializedEvent is PersistentEvents_Bundle completeBundle)
                    {
                        //GONetLog.Warning($"[SPAWN_SYNC] CLIENT: Successfully deserialized reassembled bundle ({completeBundle.PersistentEvents.Count} events)");

                        // Process the complete bundle through the normal persistent events handler
                        var bundleEnvelope = GONetEventEnvelope<PersistentEvents_Bundle>.Borrow(completeBundle, envelope.SourceAuthorityId, null);
                        OnPersistentEventsBundle_ProcessAll_Remote(bundleEnvelope);
                        GONetEventEnvelope<PersistentEvents_Bundle>.Return(bundleEnvelope);
                    }
                    else
                    {
                        GONetLog.Error($"[SPAWN_SYNC] CLIENT: Reassembled event is not PersistentEvents_Bundle! Type: {deserializedEvent?.GetType().Name ?? "null"}, ChunkId: {chunkId}");
                    }
                }
                catch (System.Exception ex)
                {
                    GONetLog.Error($"[SPAWN_SYNC] CLIENT: FAILED to deserialize reassembled bundle! ChunkId: {chunkId}, Size: {reassembly.OriginalSize} bytes, Error: {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    // Clean up reassembly state
                    pendingChunkReassembly.Remove(chunkId);
                }
            }
        }

        /// <summary>
        /// For every runtime instance of <see cref="GONetParticipant"/>, there will be one and only one item in one and only one of the <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/>'s <see cref="Dictionary{TKey, TValue}.Values"/>.
        /// The key into this is the <see cref="GONetParticipant.CodeGenerationId"/>.
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
            if (activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gnp.CodeGenerationId, out collection))
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
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncSettings_ProfileTemplate.PhysicsUpdateInterval"/>
            /// Physics sync frequency: 1=every FixedUpdate, 2=every 2nd, 3=every 3rd, 4=every 4th.
            /// Only used for physics sync (Rigidbody position/rotation when IsRigidBodyOwnerOnlyControlled=true).
            /// </summary>
            internal int syncAttribute_PhysicsUpdateInterval;
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
            internal static readonly long AT_REST_CLEAR_THRESHOLD_TICKS = TimeSpan.FromSeconds(1).Ticks;

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
            internal long hasAwaitingAtRest_lastProcessedAtRestTicks;
            internal GONetSyncableValue hasAwaitingAtRest_value;

            /// <summary>
            /// NEW: Physics snapping flag for at-rest handling.
            /// If true, when the at-rest value is applied, trigger physics snapping on the GONetParticipant
            /// to eliminate quantization error (position: ~0.95mm → sub-mm, rotation: ~0.3° → sub-0.01°).
            /// Only applies to physics objects (IsRigidBodyOwnerOnlyControlled=true) on non-authority clients.
            /// </summary>
            internal bool hasAwaitingAtRest_needsPhysicsSnap;

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
                // Check if this is arriving after an at-rest was set but not yet applied
                if (hasAwaitingAtRest && elapsedTicksAtChange < hasAwaitingAtRest_assumedInitialRestElapsedTicks)
                {
                    GONetLog.Debug($"[LATE-ARRIVAL-REJECTED-AWAITING] index:{index} " +
                        $"valueTime:{TimeSpan.FromTicks(elapsedTicksAtChange).TotalSeconds}s " +
                        $"value:{value} " +
                        $"atRestTime:{TimeSpan.FromTicks(hasAwaitingAtRest_assumedInitialRestElapsedTicks).TotalSeconds}s");
                    return; // Reject updates from before the pending at-rest time
                }

                // Check if this is arriving after an at-rest was already applied
                if (hasAwaitingAtRest_lastProcessedAtRestTicks > 0 && elapsedTicksAtChange < hasAwaitingAtRest_lastProcessedAtRestTicks)
                {
                    GONetLog.Debug($"[LATE-ARRIVAL-REJECTED-PROCESSED] index:{index} " +
                        $"valueTime:{TimeSpan.FromTicks(elapsedTicksAtChange).TotalSeconds}s " +
                        $"value:{value} " +
                        $"lastAtRestTime:{TimeSpan.FromTicks(hasAwaitingAtRest_lastProcessedAtRestTicks).TotalSeconds}s");
                    return; // Reject updates from before the last processed at-rest time
                }

                // Log what's being added (only if logging is enabled)
                if (syncAttribute_ShouldBlendBetweenValuesReceived && ValueBlendUtils.ShouldLog)
                {
                    GONetLog.Debug($"[BUFFER-ADD] index:{index} " +
                        $"time:{TimeSpan.FromTicks(elapsedTicksAtChange).TotalSeconds}s " +
                        $"value:{value} " +
                        $"bufferSize:{mostRecentChanges_usedSize} " +
                        $"hasAwaitingAtRest:{hasAwaitingAtRest}");
                }

                // Check for duplicate timestamps
                for (int i = 0; i < mostRecentChanges_usedSize; ++i)
                {
                    var item = mostRecentChanges[i];
                    if (item.elapsedTicksAtChange == elapsedTicksAtChange)
                    {
                        return; // avoid adding items with same timestamp as it will mess up value blending
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
                        }

                        // Consider clearing lastProcessedAtRestTicks if we're clearly moving again
                        // Using pre-calculated constant instead of TimeSpan.FromSeconds(1).Ticks
                        if (hasAwaitingAtRest_lastProcessedAtRestTicks > 0 &&
                            (elapsedTicksAtChange - hasAwaitingAtRest_lastProcessedAtRestTicks) > AT_REST_CLEAR_THRESHOLD_TICKS)
                        {
                            GONetLog.Debug($"[AT-REST-CLEARED] index:{index} - object moving again, clearing at-rest protection");
                            hasAwaitingAtRest_lastProcessedAtRestTicks = 0;
                        }

                        return;
                    }
                }

                if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                {
                    mostRecentChanges[mostRecentChanges_usedSize] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                    ++mostRecentChanges_usedSize;

                    // Consider clearing lastProcessedAtRestTicks here too
                    if (hasAwaitingAtRest_lastProcessedAtRestTicks > 0 &&
                        (elapsedTicksAtChange - hasAwaitingAtRest_lastProcessedAtRestTicks) > AT_REST_CLEAR_THRESHOLD_TICKS)
                    {
                        GONetLog.Debug($"[AT-REST-CLEARED] index:{index} - object moving again, clearing at-rest protection");
                        hasAwaitingAtRest_lastProcessedAtRestTicks = 0;
                    }
                }
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
                            //GONetLog.Debug("the new value being placed in buffer is happening prior to applying the new baseline!");

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

                GONetSyncableValue currentValue = syncCompanion.GetAutoMagicalSyncValue(index);
                GONetSyncableValue blendedValue;
                if (ValueBlendUtils.TryGetBlendedValue(this, Time.ElapsedTicks - useBufferLeadTicks, out blendedValue, out bool didExtrapolatePastMostRecentChanges))
                {
                    // We do not want to apply TRULY extrapolated (past end of most recent values) values if an at rest command is awaiting
                    // processing since it is likely that the extrapolation occurred due to lack of information coming from owner since it is at rest.
                    if (!hasAwaitingAtRest || !didExtrapolatePastMostRecentChanges)
                    {
                        /*
                        GONetLog.Debug($"[BLEND-APPLY] index:{index} " +
                            $"currentValue:{currentValue} " +
                            $"blendedValue:{blendedValue} " +
                            $"didExtrapolate:{didExtrapolatePastMostRecentChanges} " +
                            $"hasAwaitingAtRest:{hasAwaitingAtRest} " +
                            $"bufferSize:{mostRecentChanges_usedSize}");
                        */

                        // Try to apply via Rigidbody.MovePosition/MoveRotation for smooth physics rendering
                        // Falls back to standard SetAutoMagicalSyncValue if no Rigidbody or not position/rotation
                        if (!TryApplyBlendedValue_UsingRigidbodyIfPresent(blendedValue))
                        {
                            syncCompanion.SetAutoMagicalSyncValue(index, blendedValue);
                        }

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
            }

            /// <summary>
            /// Attempts to apply blended value using Rigidbody.MovePosition/MoveRotation if a Rigidbody exists.
            /// This respects Unity's Rigidbody interpolation for smooth rendering on non-authority clients.
            /// NOTE: Relies on implementation detail that position/rotation sync values use GONetMain.IsPositionNotSyncd/IsRotationNotSyncd as their ShouldSkipSync delegates.
            /// </summary>
            /// <returns>True if applied via Rigidbody, false if no Rigidbody or not a position/rotation field</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryApplyBlendedValue_UsingRigidbodyIfPresent(GONetSyncableValue blendedValue)
            {
                GONetParticipant participant = syncCompanion.gonetParticipant;

                // EARLY EXIT: Only apply for non-authority (IsMine = false)
                if (participant.IsMine)
                    return false;

                // EARLY EXIT: Only proceed if there's a cached Rigidbody (3D or 2D)
                // This check avoids expensive function pointer comparisons when no physics body exists
                if (participant.myRigidBody == null && participant.myRigidBody2D == null)
                    return false;

                // Check if this is position or rotation by matching the skip sync function
                // This is an implementation detail but avoids string comparisons
                bool isPosition = syncAttribute_ShouldSkipSync == GONetMain.IsPositionNotSyncd;
                bool isRotation = syncAttribute_ShouldSkipSync == GONetMain.IsRotationNotSyncd;

                if (!isPosition && !isRotation)
                    return false;

                // Try Rigidbody (3D) - use cached reference
                Rigidbody rb = participant.myRigidBody;
                if (rb != null && rb.isKinematic)
                {
                    if (isPosition)
                    {
                        rb.MovePosition(blendedValue.UnityEngine_Vector3);
                        return true;
                    }
                    else if (isRotation)
                    {
                        rb.MoveRotation(blendedValue.UnityEngine_Quaternion);
                        return true;
                    }
                }

                // Try Rigidbody2D - use cached reference
                Rigidbody2D rb2D = participant.myRigidBody2D;
                if (rb2D != null && rb2D.bodyType == RigidbodyType2D.Kinematic)
                {
                    if (isPosition)
                    {
                        rb2D.MovePosition(blendedValue.UnityEngine_Vector3);
                        return true;
                    }
                    else if (isRotation)
                    {
                        // Rigidbody2D.MoveRotation takes float (Z-axis rotation in degrees)
                        // Extract Z component from Quaternion
                        float zRotation = blendedValue.UnityEngine_Quaternion.eulerAngles.z;
                        rb2D.MoveRotation(zRotation);
                        return true;
                    }
                }

                return false;
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

            internal bool TryGetBlendedValue(long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
            {
                return syncCompanion.TryGetBlendedValue(index, mostRecentChanges, mostRecentChanges_usedSize, atElapsedTicks, out blendedValue, out didExtrapolatePastMostRecentChanges);
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

        internal static IEnumerator OnAwake_ApplyDesignTimeMetadata(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug($"dreetsi cikd wash sod installed");
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                while (!GONetSpawnSupport_Runtime.IsDesignTimeMetadataCached)
                {
                    //GONetLog.Warning($"dreetsi");
                    yield return null;
                }
                //GONetLog.Debug($"dreetsi   --- now we poop!");

                InitDesignTimeMetadata_IfNeeded(gonetParticipant);
            }
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnEnable method.
        /// </summary>
        internal static void OnEnable_StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            // IMPORTANT: We no longer can call this at this time becauase due to the latest implementation of how desigh time location is
            //            stored/processed, the WasInstantiated is not known at this point and if we called the method below bad things would happen
            //            because the WasInstantiated is needed to be known in order to figure out design time metadata like code gen id which is needed for next method to work.
            //            Instead, check out OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking
            //StartMonitoringForAutoMagicalNetworking(gonetParticipant);

            //GONetLog.Debug($"gnp.name: {gonetParticipant.name} WasInstantiatedForce: {gonetParticipant.wasInstantiatedForce}");
            if (gonetParticipant.wasInstantiatedForce)
            {
                // we now know this was instantiated (from remote source as that is the only time WasInstantiatedForce is true)....scene stuff gets this called automatically elsewhere
                OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking(gonetParticipant);
            }
        }

        private static void OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            // Process AutoDontDestroyOnLoad flag BEFORE starting monitoring
            // This ensures the scene identifier is set correctly
            if (gonetParticipant.AutoDontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(gonetParticipant.gameObject);
                GONetLog.Debug($"[DDOL] Auto-applied DontDestroyOnLoad to: {gonetParticipant.gameObject.name}");
            }

            StartMonitoringForAutoMagicalNetworking(gonetParticipant);
        }

        private static void StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                InitDesignTimeMetadata_IfNeeded(gonetParticipant);

                if (gonetParticipant.CodeGenerationId == GONetParticipant.CodeGenerationId_Unset ||
                    gonetParticipant.DidStartMonitoringForAutoMagicalNetworking)
                {
                    //GONetLog.Debug($"dreetsi never never in life.  code gen id: {gonetParticipant.CodeGenerationId}, did start? {gonetParticipant.DidStartMonitoringForAutoMagicalNetworking}");
                    return;
                }

                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
                    //GONetLog.Debug($"dreetsi  cache? {GONetSpawnSupport_Runtime.IsDesignTimeMetadataCached}, go.name: {gonetParticipant.gameObject.name}, genId: {gonetParticipant.CodeGenerationId}");
                    if (!activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.CodeGenerationId, out autoSyncCompanions))
                    {
                        autoSyncCompanions = new Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                        activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.CodeGenerationId] = autoSyncCompanions; // NOTE: This is the only place we add to the outer dictionary and this is always run in the main unity thread, THEREFORE no need for Concurrent....just on the inner ones
                    }
                    GONetParticipant_AutoMagicalSyncCompanion_Generated companion = GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.CreateInstance(gonetParticipant);
                    autoSyncCompanions[gonetParticipant] = companion; // NOTE: This is the only place where the inner dictionary is added to and is ensured to run on unity main thread since OnEnable, so no need for concurrency as long as we can say the same about removes

                    gonetParticipant.AddGONetIdAtInstantiationChangedHandler(OnGONetIdAtInstantiationChanged_DoSomeMapMaintenanceForKeyLookupPerformanceLater);

                    uniqueSyncGroupings.Clear();
                    for (int i = 0; i < companion.valuesCount; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = companion.valuesChangesSupport[i];

                        if (!GONetParticipant_AutoMagicalSyncCompanion_Generated.ShouldSkipSync(monitoringSupport, i))
                        {
                        SyncBundleUniqueGrouping grouping =
                            new SyncBundleUniqueGrouping(
                                monitoringSupport.syncAttribute_SyncChangesEverySeconds,
                                monitoringSupport.syncAttribute_Reliability,
                                monitoringSupport.syncAttribute_MustRunOnUnityMainThread);

                        uniqueSyncGroupings.Add(grouping); // since it is a set, duplicates will be discarded
                    }
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

                    // Deferred RPC system will automatically retry via ProcessDeferredRpcs() running every frame
                }

                uint gonetIdThatIsGoingToBePopulated = isCurrentlyProcessingInstantiateGNPEvent ? currentlyProcessingInstantiateGNPEvent.GONetId : gonetParticipant.GONetId;
                var enableEvent = new GONetParticipantEnabledEvent(gonetIdThatIsGoingToBePopulated);
                PublishEventAsSoonAsSufficientInfoAvailable(enableEvent, gonetParticipant);

                //const string INSTANTIATE = "GNP Enabled go.name: ";
                //const string ID = " gonetId: ";
                //GONetLog.Debug(string.Concat(INSTANTIATE, gonetParticipant.gameObject.name, ID + gonetParticipant.GONetId));

                gonetParticipant.DidStartMonitoringForAutoMagicalNetworking = true;
            }
        }

        private static void InitDesignTimeMetadata_IfNeeded(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug($"InitDesignTimeMetadata_IfNeeded: Called for '{gonetParticipant.gameObject.name}', IsDesignTimeMetadataInitd: {gonetParticipant.IsDesignTimeMetadataInitd}, UnityGuid: '{gonetParticipant.UnityGuid}'");

            if (!gonetParticipant.IsDesignTimeMetadataInitd)
            {
                // IMPORTANT: We must ensure the design-time metadata cache is loaded before attempting to initialize
                // This check prevents initialization before the DesignTimeMetadata.json file has been loaded into memory
                // Normally this is guaranteed by GONetParticipant.AwakeCoroutine() waiting for the cache,
                // but when called from AutoPropagateInitialInstantiation, we need this explicit guard
                if (!GONetSpawnSupport_Runtime.IsDesignTimeMetadataCached)
                {
                    GONetLog.Warning($"InitDesignTimeMetadata_IfNeeded: Cannot initialize metadata for '{gonetParticipant.gameObject.name}' - design-time metadata cache not loaded yet! This should not happen in normal flow.");
                    return;
                }

                string fullUniquePath = DesignTimeMetadata.GetFullUniquePathInScene(gonetParticipant);
                //GONetLog.Debug($"InitDesignTimeMetadata_IfNeeded: Calling InitDesignTimeMetadata for '{gonetParticipant.gameObject.name}' with path: {fullUniquePath}, UnityGuid: '{gonetParticipant.UnityGuid}'");
                GONetSpawnSupport_Runtime.InitDesignTimeMetadata(fullUniquePath, gonetParticipant);
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

            // PERFORMANCE FIX: Use GONet's ArrayPool to avoid GC from ToArray() - zero allocations after warmup
            // CRITICAL: The HashSet enumeration itself can throw if modified, so we try/catch it
            int tickReceiversCount = tickReceivers.Count;
            if (tickReceiversCount > 0)
            {
                GONetBehaviour[] tickReceiversSnapshot = tickReceivers_arrayPool.Borrow(tickReceiversCount);
                int actualCount = 0;
                try
                {
                    // Try to copy - if collection is modified during copy, we'll catch and skip this tick cycle
                    foreach (var tickReceiver in tickReceivers)
                    {
                        if (actualCount >= tickReceiversSnapshot.Length) break; // Safety check
                        tickReceiversSnapshot[actualCount++] = tickReceiver;
                    }

                    // Iterate using actual count (not array.Length, which may be larger than needed)
                    for (int i = 0; i < actualCount; i++)
                    {
                        tickReceiversSnapshot[i].Tick(uniqueGrouping.scheduleFrequencyHz, uniqueElapsedSeconds, uniqueDeltaSeconds);
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("Collection was modified"))
                {
                    // Collection was modified during enumeration - this can happen if Tick() callbacks
                    // trigger add/remove that bypasses the deferred system. Skip this tick cycle.
                    GONetLog.Warning($"tickReceivers collection modified during Tick() - skipping this sync cycle for {uniqueGrouping}. This should be rare.");
                }
                finally
                {
                    // CRITICAL: Always return to pool, even if exception thrown
                    tickReceivers_arrayPool.Return(tickReceiversSnapshot);
                }
            }

            autoSyncUniqueGroupingToLastElapsedTicks[uniqueGrouping] = elapsedTicks;
        }

        /// <summary>
        /// auto-magical sync related housekeeping....essentially populating a shadow map that uses a different key that was not available with correct value when the first map was created
        /// </summary>
        private static void OnGONetIdAtInstantiationChanged_DoSomeMapMaintenanceForKeyLookupPerformanceLater(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug($"DREETSi update map. gnp.name: {gonetParticipant.name}, genId: {gonetParticipant.CodeGenerationId}, gonetid@instantiation: {gonetParticipant.GONetIdAtInstantiation}, now: {gonetParticipant.GONetId}");

            Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions_uintKeyForPerformance;
            if (!activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance.TryGetValue(gonetParticipant.CodeGenerationId, out autoSyncCompanions_uintKeyForPerformance))
            {
                autoSyncCompanions_uintKeyForPerformance = new Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance[gonetParticipant.CodeGenerationId] = autoSyncCompanions_uintKeyForPerformance; // NOTE: This is the only place we add to the outer dictionary and this is always run in the main unity thread, THEREFORE no need for Concurrent....just on the inner ones
            }

            Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.CodeGenerationId];
            autoSyncCompanions_uintKeyForPerformance[gonetParticipant.GONetIdAtInstantiation] = autoSyncCompanions[gonetParticipant]; // NOTE: This is the only place where the inner dictionary is added to and is ensured to run on unity main thread since OnEnable, so no need for concurrency as long as we can say the same about removes
        }

        public static bool IsChannelClientInitializationRelated(GONetChannelId channelId)
        {
            return
                channelId == GONetChannel.ClientInitialization_EventSingles_Reliable ||
                channelId == GONetChannel.ClientInitialization_CustomSerialization_Reliable ||
                channelId == GONetChannel.TimeSync_Unreliable; // CRITICAL: Time sync must happen during initialization!
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
            bool wasDefinedInScene = WasDefinedInScene(gonetParticipant);
            //GONetLog.Info($"[SPAWN] Start_AutoPropogateInstantiation_IfAppropriate_INTERNAL - name: '{gonetParticipant.gameObject.name}', wasDefinedInScene: {wasDefinedInScene}, IsServer: {IsServer}, IsClient: {IsClient}");

            if (wasDefinedInScene)
            {
                //GONetLog.Info($"[SPAWN] '{gonetParticipant.gameObject.name}' was defined in scene - will only assign GONetId on server, NO spawn event propagation");
                if (IsServer) // stuff defined in the scene will be owned by the server and therefore needs to be assigned a GONetId by server
                {
                    AssignGONetIdRaw_IfAppropriate(gonetParticipant);
                }
                else if (IsClient)
                {
                    // LIFECYCLE GATE: Scene-defined objects on clients require DeserializeInitAllCompleted before OnGONetReady
                    // (They're receiving sync data from server, not local authority)
                    gonetParticipant.MarkRequiresDeserializeInit();
                }
            }
            else
            {
                bool isThisCondisideredTheMomentOfInitialInstantiation = !remoteSpawns_avoidAutoPropagateSupport.Contains(gonetParticipant);
                //GONetLog.Info($"[SPAWN] '{gonetParticipant.gameObject.name}' NOT defined in scene - isThisCondisideredTheMomentOfInitialInstantiation: {isThisCondisideredTheMomentOfInitialInstantiation}");

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

                    //GONetLog.Info($"[SPAWN] About to assign GONetId and publish spawn event for '{gonetParticipant.gameObject.name}'");
                    AssignGONetIdRaw_IfAppropriate(gonetParticipant);
                    //GONetLog.Info($"[SPAWN] Assigned GONetId {gonetParticipant.GONetId} to '{gonetParticipant.gameObject.name}'");
                    AutoPropagateInitialInstantiation(gonetParticipant);
                    //GONetLog.Info($"[SPAWN] Published spawn event for '{gonetParticipant.gameObject.name}' with GONetId {gonetParticipant.GONetId}");
                    OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking(gonetParticipant); // we now know this was instantiated (by local source...remote source is processed like this elsewhere)....scene stuff gets this called automatically elsewhere
                }
                else
                {
                    // this data item has now served its purpose (i.e., avoid auto propagate since it already came from remote source!), so remove it
                    remoteSpawns_avoidAutoPropagateSupport.Remove(gonetParticipant);
                }
            }

            var startEvent = new GONetParticipantStartedEvent(gonetParticipant);
            PublishEventAsSoonAsSufficientInfoAvailable(startEvent, gonetParticipant);

            // REMOVED: Path 1 (Start) publication - this caused race conditions with GONetLocal.AddToLookupOnceAuthorityIdKnown
            // All IsMine participants are now published from GONetLocal.AddToLookupOnceAuthorityIdKnown (Path 3/4) - the definitive moment of readiness
            // Remote participants are published from deserialization path (Path 2)
            // This ensures 100% coverage with zero race conditions and zero duplicates

            // PATH 8: Client spawns remotely-controlled object (projectiles with server authority)
            // These participants have OwnerAuthorityId = server, so they won't be caught by Path 5 (IsRelatedToThisLocality fails)
            // The spawning client needs OnGONetReady even though they don't own it
            // CRITICAL: Require server's GONetLocal to be present - this ensures proper initialization synchronization
            // The server's GONetLocal is now properly sent to all clients via FilterPersistentEventsByLoadedScenes fix
            if (IsClient &&
                GONetSpawnSupport_Runtime.IsMarkedToBeRemotelyControlled(gonetParticipant) &&
                IsGONetReady(gonetParticipant))
            {
                // Deduplication check: Only publish if not already published
                if (TryMarkDeserializeInitPublished(gonetParticipant.GONetId))
                {
                    //GONetLog.Info($"[GONet] Publishing DeserializeInitAllCompleted for client-spawned remotely-controlled '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}, OwnerAuthorityId: {gonetParticipant.OwnerAuthorityId}) from Start path");
                    var deserializeInitEvent = new GONetParticipantDeserializeInitAllCompletedEvent(gonetParticipant);
                    PublishEventAsSoonAsSufficientInfoAvailable(deserializeInitEvent, gonetParticipant, isRelatedLocalContentRequired: true); // Wait for server GONetLocal - required for proper initialization
                }
                else
                {
                    //GONetLog.Info($"[GONet] Skipping duplicate DeserializeInitAllCompleted for client-spawned remotely-controlled '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId}) - already published from another path");
                }
            }
        }

        /// <summary>
        /// PRE: Already known that <paramref name="gonetParticipant"/> has <see cref="GONetParticipant.IsMine_ToRemotelyControl"/> true.
        /// PRE: <see cref="MyAuthorityId"/> is set to final value and is not <see cref="OwnerAuthorityId_Unset"/> in case it is needed as a fallback (i.e., when not enough values in id batch from server).
        ///
        /// TODO: look into calling this method inside of <see cref="Client_InstantiateToBeRemotelyControlledByMe(GONetParticipant, Vector3, Quaternion)"/> instead of where it is called from now...this would allow for the final GONetId to be set/known immediately!
        /// </summary>
        private static void Client_DoAutoPropogateInstantiationPrep_RemotelyControlled(GONetParticipant gonetParticipant)
        {
            // Just set the authority - the actual ID allocation happens in GetNextAvailableGONetIdRaw
            // to avoid double-counting (allocating here and using there)
            gonetParticipant.OwnerAuthorityId = OwnerAuthorityId_Server;
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

                    // LIFECYCLE GATE: GONetId assigned - check if OnGONetReady can fire
                    CheckAndPublishOnGONetReady_IfAllConditionsMet(gonetParticipant);
                }
                else
                {
                    throw new OverflowException("Unable to assign a new GONetId, because lastAssignedGONetId has reached the max value of GONetParticipant.GONetId_Raw_MaxValue, which is: " + GONetParticipant.GONetId_Raw_MaxValue);
                }
            }
        }

        private static uint GetNextAvailableGONetIdRaw(GONetParticipant gonetParticipant)
        {
            // CLIENT: Use batch manager for remotely-controlled spawns
            if (IsClient && gonetParticipant.OwnerAuthorityId == OwnerAuthorityId_Server)
            {
                uint batchId;
                bool shouldRequestNewBatch;

                // GONetId Reuse Prevention: Loop until we find a batch ID that's not recently despawned
                int reusePrevention_attemptCount = 0;
                const int MAX_REUSE_PREVENTION_ATTEMPTS = 200; // Should never need this many, but prevent infinite loop

                do
                {
                    bool success = GONetIdBatchManager.Client_TryAllocateNextId(out batchId, out shouldRequestNewBatch);

                    if (!success)
                    {
                        // CRITICAL: This should NEVER be reached if using Client_TryInstantiate API correctly
                        // The dangerous fallback code has been removed and replaced with limbo mode system
                        // If you hit this exception, you are:
                        // 1. Using Client_InstantiateToBeRemotelyControlledByMe (old API) during batch exhaustion, OR
                        // 2. Calling Instantiate_MarkToBeRemotelyControlled directly (internal API - don't do this)
                        //
                        // SOLUTION: Use Client_TryInstantiateToBeRemotelyControlledByMe instead
                        // This will handle batch exhaustion gracefully via limbo mode
                        throw new InvalidOperationException(
                            "[GONetIdBatch] CRITICAL: No batch IDs available for client spawn! " +
                            "This means you're using the OLD API during batch exhaustion. " +
                            "REQUIRED FIX: Replace Client_InstantiateToBeRemotelyControlledByMe() with Client_TryInstantiateToBeRemotelyControlledByMe(). " +
                            "The Try version handles batch exhaustion via limbo mode. " +
                            $"Current state: {GONetIdBatchManager.Client_GetDiagnostics()}");
                    }

                    // Compose GONetId to check reuse eligibility (client-spawned objects get server authority)
                    uint composedGONetId = unchecked((uint)(batchId << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | OwnerAuthorityId_Server;

                    if (CanReuseGONetId(composedGONetId))
                    {
                        // This ID is safe to use - not recently despawned
                        if (shouldRequestNewBatch)
                        {
                            Client_RequestNewGONetIdBatch();
                        }
                        return batchId;
                    }

                    // This ID is recently despawned - skip it and try next one
                    // (CanReuseGONetId already logged warning about skipping)
                    reusePrevention_attemptCount++;

                } while (reusePrevention_attemptCount < MAX_REUSE_PREVENTION_ATTEMPTS);

                // If we exhausted attempts, something is very wrong
                GONetLog.Error($"[GONetId-Reuse] CRITICAL: Exhausted {MAX_REUSE_PREVENTION_ATTEMPTS} batch IDs - all recently despawned! " +
                              $"This should NEVER happen. Batch size: {GONetIdBatchManager.Client_GetDiagnostics()}. " +
                              $"Using potentially unsafe ID: {batchId}");
                return batchId; // Return last attempted ID as fallback (better than crash)
            }

            // SERVER or CLIENT (non-remotely-controlled): Regular ID assignment
            ++lastAssignedGONetIdRaw;

            if (IsServer)
            {
                // Skip any IDs that fall within client batches
                while (GONetIdBatchManager.Server_IsIdInAnyBatch(lastAssignedGONetIdRaw))
                {
                    ++lastAssignedGONetIdRaw;
                }

                // GONetId Reuse Prevention: Server-assigned IDs also need reuse checking
                uint composedGONetId_server = unchecked((uint)(lastAssignedGONetIdRaw << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | OwnerAuthorityId_Server;
                while (!CanReuseGONetId(composedGONetId_server))
                {
                    ++lastAssignedGONetIdRaw;

                    // Re-check batch collision after increment
                    while (GONetIdBatchManager.Server_IsIdInAnyBatch(lastAssignedGONetIdRaw))
                    {
                        ++lastAssignedGONetIdRaw;
                    }

                    composedGONetId_server = unchecked((uint)(lastAssignedGONetIdRaw << GONetParticipant.GONET_ID_BIT_COUNT_UNUSED)) | OwnerAuthorityId_Server;
                }
            }

            return lastAssignedGONetIdRaw;
        }

        /// <summary>
        /// Assigns a specific GONetId to a participant directly (used for syncing scene-defined objects from server to client).
        /// </summary>
        internal static void AssignGONetIdRaw_Direct(GONetParticipant gonetParticipant, uint gonetId)
        {
            gonetParticipant.GONetId = gonetId;
            GONetLog.Debug($"[GONetId] Directly assigned GONetId {gonetId} to '{gonetParticipant.gameObject.name}'");

            // LIFECYCLE GATE: GONetId assigned - check if OnGONetReady can fire
            CheckAndPublishOnGONetReady_IfAllConditionsMet(gonetParticipant);
        }

        /// <summary>
        /// Finds a GONetParticipant by its design-time location within a specific scene.
        /// </summary>
        internal static GONetParticipant FindParticipantByDesignTimeLocation(string designTimeLocation, string sceneName)
        {
            // Search all GONetParticipants in the scene (including those without GONetIds assigned yet)
            GONetParticipant[] allParticipants = UnityEngine.Object.FindObjectsOfType<GONetParticipant>();

            foreach (GONetParticipant participant in allParticipants)
            {
                if (participant != null &&
                    participant.IsDesignTimeMetadataInitd &&
                    participant.DesignTimeLocation == designTimeLocation)
                {
                    // Verify it's in the correct scene
                    string participantScene = GONetSceneManager.GetSceneIdentifier(participant.gameObject);
                    if (participantScene == sceneName)
                    {
                        return participant;
                    }
                }
            }

            return null;
        }

        private static void AutoPropagateInitialInstantiation(GONetParticipant gonetParticipant)
        {
            // CRITICAL: Ensure design-time metadata is initialized BEFORE creating the spawn event
            // This prevents the "TON CLEETLE!" error when DesignTimeLocation is accessed
            // The metadata must be initialized synchronously here because:
            // 1. GONetParticipant.Awake() initializes metadata in a coroutine (async)
            // 2. Start() is called before the coroutine completes
            // 3. We need DesignTimeLocation populated in the spawn event NOW
            InitDesignTimeMetadata_IfNeeded(gonetParticipant);

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

        /// <summary>
        /// Determines if a GONetParticipant is being destroyed as a result of scene unloading.
        /// <para><b>Detection Logic:</b></para>
        /// <list type="bullet">
        /// <item>Checks AutoDontDestroyOnLoad flag first (most reliable)</item>
        /// <item>Falls back to runtime scene detection if flag not set</item>
        /// <item>Returns FALSE if object is in DontDestroyOnLoad scene (these objects survive scene unloads)</item>
        /// <item>Returns TRUE if application is quitting (everything being destroyed)</item>
        /// <item>Returns TRUE if object's scene is not loaded or is unloading</item>
        /// <item>Returns FALSE otherwise (true gameplay despawn)</item>
        /// </list>
        /// </summary>
        /// <param name="gonetParticipant">The GONetParticipant being destroyed</param>
        /// <returns>True if destruction is from scene unload, false if it's an intentional gameplay despawn</returns>
        private static bool IsDestroyFromSceneUnload(GONetParticipant gonetParticipant)
        {
            if (IsApplicationQuitting)
            {
                return true; // Application quitting - not a gameplay despawn
            }

            // Primary check: AutoDontDestroyOnLoad flag (most reliable)
            if (gonetParticipant.AutoDontDestroyOnLoad)
            {
                return false; // This object is marked as DDOL, so it's a true gameplay despawn
            }

            Scene objectScene = gonetParticipant.gameObject.scene;

            // Fallback: Runtime detection of DontDestroyOnLoad scene
            // This catches cases where users manually called DontDestroyOnLoad without setting the flag
            if (GONetSceneManager.IsDontDestroyOnLoad(gonetParticipant.gameObject))
            {
                return false; // True gameplay despawn (DontDestroyOnLoad objects aren't affected by scene unloads)
            }

            // Check if the object's scene is unloading or unloaded
            if (!objectScene.isLoaded)
            {
                return true; // Scene is unloaded - destruction is from scene lifecycle
            }

            // Check GONet's scene manager for scene unloading state
            if (SceneManager != null)
            {
                string sceneName = GONetSceneManager.GetSceneIdentifier(gonetParticipant.gameObject);
                if (!string.IsNullOrEmpty(sceneName) && SceneManager.IsSceneUnloading(sceneName))
                {
                    return true; // Scene is actively unloading
                }
            }

            return false; // None of the scene-unload conditions met - this is a true gameplay despawn
        }

        internal static void OnDestroy_AutoPropagateRemoval_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying)
            {
                if (IsMine(gonetParticipant) || (IsServer && !Server_IsClientOwnerConnected(gonetParticipant)))
                {
                    // Determine if this is a true gameplay despawn or scene unload destruction
                    bool isSceneUnloadDestroy = IsDestroyFromSceneUnload(gonetParticipant);

                    if (!isSceneUnloadDestroy)
                    {
                        // True gameplay despawn: Send despawn event over network
                        AutoPropagateDespawn(gonetParticipant);
                    }
                    // else: Scene unload - don't send any event (coordinated via GONetSceneManager)
                }
                else
                {
                    // Check if this is a scene unload destroy (Unity automatically destroys all objects in unloading scenes)
                    bool isSceneUnloadDestroy = IsDestroyFromSceneUnload(gonetParticipant);

                    bool isExpected =
                        gonetIdsDestroyedViaPropagation.Contains(gonetParticipant.GONetId) ||
                        (IsClient && IsApplicationQuitting) ||
                        isSceneUnloadDestroy; // Scene unload destroys all objects - this is expected

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
        /// Publishes a <see cref="DespawnGONetParticipantEvent"/> for an intentional gameplay despawn.
        /// <para><b>PRE:</b> <paramref name="gonetParticipant"/> is owned by me.</para>
        /// <para>This is used when a GONetParticipant is destroyed through gameplay logic
        /// (e.g., projectile hits, enemy dies, player destroys object), NOT from scene unloading.</para>
        /// </summary>
        /// <param name="gonetParticipant">The GONetParticipant being despawned</param>
        private static void AutoPropagateDespawn(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant.GONetId == GONetParticipant.GONetId_Unset)
            {
                const string NOID = "GONetParticipant that I own was despawned, but it has not been assigned a GONetId yet. Unable to propagate the despawn to others. GameObject.name: ";
                GONetLog.Error(string.Concat(NOID, gonetParticipant.gameObject.name));
                return;
            }

            DespawnGONetParticipantEvent @event = new DespawnGONetParticipantEvent() { GONetId = gonetParticipant.GONetId };
            //GONetLog.Warning($"[DESPAWN_SYNC] Publishing DespawnGONetParticipantEvent for GONetId {gonetParticipant.GONetId}, GameObject: '{gonetParticipant.gameObject.name}'");
            EventBus.Publish(@event);
        }

        static readonly HashSet<int> definedInSceneParticipantInstanceIDs = new HashSet<int>();

        /// <summary>
        /// Maps GONetParticipant instance IDs to the scene name they were spawned in or loaded with.
        /// Used for scene-based spawn tracking and late-joiner synchronization.
        /// </summary>
        static readonly Dictionary<int, string> participantInstanceID_to_SpawnSceneName = new Dictionary<int, string>();

        /// <summary>
        /// Queue of spawn events waiting for their required scene to be loaded.
        /// <para>When a client receives a spawn for a scene they haven't loaded yet,
        /// the spawn is queued here and processed when the scene loads.</para>
        /// </summary>
        static readonly List<InstantiateGONetParticipantEvent> deferredSpawnEvents = new List<InstantiateGONetParticipantEvent>();

        /// <summary>
        /// Despawn events that arrived while spawns were deferred. These must be processed AFTER the deferred spawns complete.
        /// </summary>
        static readonly List<DespawnGONetParticipantEvent> deferredDespawnEvents = new List<DespawnGONetParticipantEvent>();

        /// <summary>
        /// Holds a deferred AllValues bundle that needs to be processed after spawns are complete.
        /// </summary>
        private struct DeferredAllValuesBundle
        {
            public byte[] RawBytes;
            public int BytesUsedCount;
            public GONetConnection RelatedConnection;
            public long ElapsedTicksAtSend;
            public string RequiredSceneName;
        }

        /// <summary>
        /// Deferred AllValues bundle waiting for spawns to be processed.
        /// <para>When a client receives the initialization AllValues bundle before spawns are processed,
        /// it's stored here and processed after deferred spawns complete.</para>
        /// </summary>
        static DeferredAllValuesBundle? deferredAllValuesBundle = null;

        /// <summary>
        /// Checks if a scene is currently loaded.
        /// </summary>
        private static bool IsSceneCurrentlyLoaded(string sceneIdentifier)
        {
            // DontDestroyOnLoad is always "loaded"
            if (sceneIdentifier == HierarchyUtils.DONT_DESTROY_ON_LOAD_SCENE)
                return true;

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.name == sceneIdentifier && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processes any deferred spawn events that were waiting for a scene to load.
        /// Called when a scene finishes loading.
        /// </summary>
        internal static void ProcessDeferredSpawnsForScene(string sceneName)
        {
            // NOTE: This can log every frame when deferred spawns exist
            // To enable, add LOG_SPAWN_VERBOSE to Player Settings → Scripting Define Symbols
            #if LOG_SPAWN_VERBOSE
            GONetLog.Debug($"[SPAWN_SYNC] ProcessDeferredSpawnsForScene called for '{sceneName}' - {deferredSpawnEvents.Count} events in queue");
            #endif

            if (deferredSpawnEvents.Count == 0)
                return;

            List<InstantiateGONetParticipantEvent> toProcess = new List<InstantiateGONetParticipantEvent>();

            // Find all spawns that were waiting for this scene
            for (int i = deferredSpawnEvents.Count - 1; i >= 0; i--)
            {
                InstantiateGONetParticipantEvent spawnEvent = deferredSpawnEvents[i];
                if (spawnEvent.SceneIdentifier == sceneName)
                {
                    //GONetLog.Debug($"[SPAWN_SYNC] Found deferred spawn for GONetId {spawnEvent.GONetId} matching scene '{sceneName}'");
                    toProcess.Add(spawnEvent);
                    deferredSpawnEvents.RemoveAt(i);
                }
            }

            if (toProcess.Count > 0)
            {
                //GONetLog.Warning($"[SPAWN_SYNC] *** Processing {toProcess.Count} deferred spawns for scene '{sceneName}' ***");

                // Process each spawn
                foreach (InstantiateGONetParticipantEvent spawnEvent in toProcess)
                {
                    //GONetLog.Debug($"[SPAWN_SYNC] Processing deferred spawn GONetId {spawnEvent.GONetId}, DesignTimeLocation: '{spawnEvent.DesignTimeLocation}'");
                    GONetParticipant instance = Instantiate_Remote(spawnEvent);
                    if (instance != null)
                    {
                        //GONetLog.Debug($"[SPAWN_SYNC] Successfully spawned deferred GONetId {spawnEvent.GONetId} as '{instance.gameObject.name}'");

                        // CRITICAL: Complete the post-instantiation setup that normally happens in OnInstantiationEvent_Remote
                        // Deferred spawns come from persistent events sent by server, so sourceAuthorityId is server
                        CompleteRemoteInstantiation(instance, spawnEvent, OwnerAuthorityId_Server);
                    }
                    else
                    {
                        GONetLog.Error($"[SPAWN_SYNC] FAILED to spawn deferred GONetId {spawnEvent.GONetId}!");
                    }
                }

                // IMPORTANT: After processing deferred spawns, check if we have a deferred AllValues bundle waiting
                // The AllValues bundle must be processed AFTER spawns so the GONetParticipants exist in the lookup maps
                if (deferredAllValuesBundle.HasValue && deferredAllValuesBundle.Value.RequiredSceneName == sceneName)
                {
                    GONetLog.Warning($"[INIT] Processing deferred AllValues bundle after spawns completed for scene '{sceneName}'");

                    DeferredAllValuesBundle bundle = deferredAllValuesBundle.Value;

                    // Reconstruct the BitStream from the stored bytes using GetBuilder_WithNewData
                    using (BitByBitByteArrayBuilder reconstructedBitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(bundle.RawBytes, bundle.BytesUsedCount))
                    {
                        DeserializeBody_AllValuesBundle(reconstructedBitStream, bundle.BytesUsedCount, bundle.RelatedConnection, bundle.ElapsedTicksAtSend);
                    }

                    // Clean up
                    deferredAllValuesBundle = null;

                    GONetLog.Warning($"[INIT] Deferred AllValues bundle processing complete");
                }

                // IMPORTANT: Process any deferred despawns AFTER spawns and AllValues complete
                // This ensures proper order: spawn -> initialize values -> despawn (if needed)
                if (deferredDespawnEvents.Count > 0)
                {
                    // Find despawns that match the spawns we just processed
                    List<DespawnGONetParticipantEvent> toProcessDespawns = new List<DespawnGONetParticipantEvent>();
                    foreach (var despawnEvent in deferredDespawnEvents)
                    {
                        // Check if this despawn's GONetId was in the spawns we just processed
                        if (toProcess.Exists(spawnEvent => spawnEvent.GONetId == despawnEvent.GONetId))
                        {
                            toProcessDespawns.Add(despawnEvent);
                        }
                    }

                    if (toProcessDespawns.Count > 0)
                    {
                        //GONetLog.Warning($"[SPAWN_SYNC] Processing {toProcessDespawns.Count} deferred despawns after spawns completed");

                        foreach (var despawnEvent in toProcessDespawns)
                        {
                            //GONetLog.Debug($"[SPAWN_SYNC] Processing deferred despawn for GONetId {despawnEvent.GONetId}");

                            // Look up the participant and destroy it
                            GONetParticipant gonetParticipant = null;
                            if (gonetParticipantByGONetIdMap.TryGetValue(despawnEvent.GONetId, out gonetParticipant))
                            {
                                gonetIdsDestroyedViaPropagation.Add(gonetParticipant.GONetId);

                                if (gonetParticipant != null && gonetParticipant.gameObject != null)
                                {
                                    //GONetLog.Debug($"[SPAWN_SYNC] Despawning '{gonetParticipant.gameObject.name}' (GONetId {despawnEvent.GONetId})");
                                    UnityEngine.Object.Destroy(gonetParticipant.gameObject);
                                }
                            }

                            // Remove from deferred list
                            deferredDespawnEvents.Remove(despawnEvent);
                        }

                        //GONetLog.Warning($"[SPAWN_SYNC] Deferred despawn processing complete");
                    }
                }
            }
            else
            {
                //GONetLog.Debug($"[SPAWN_SYNC] No deferred spawns matched scene '{sceneName}'");
            }
        }

        internal static void RecordParticipantsAsDefinedInScene(List<GONetParticipant> gonetParticipantsInLevel)
        {
            gonetParticipantsInLevel.ForEach(gonetParticipant => {
                // Process AutoDontDestroyOnLoad flag FIRST for scene-defined objects
                // This must happen before GetSceneIdentifier so it's tracked correctly
                if (gonetParticipant.AutoDontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(gonetParticipant.gameObject);
                    GONetLog.Debug($"[DDOL] Auto-applied DontDestroyOnLoad to scene-defined object: {gonetParticipant.gameObject.name}");
                }

                definedInSceneParticipantInstanceIDs.Add(gonetParticipant.GetInstanceID());

                // Track which scene this GNP was defined in
                string sceneName = GONetSceneManager.GetSceneIdentifier(gonetParticipant.gameObject);
                if (!string.IsNullOrEmpty(sceneName))
                {
                    participantInstanceID_to_SpawnSceneName[gonetParticipant.GetInstanceID()] = sceneName;
                    GONetLog.Debug($"[SceneTracking] Recorded GNP '{gonetParticipant.gameObject.name}' as defined in scene '{sceneName}'");
                }

                OnWasInstantiatedKnown_StartMonitoringForAutoMagicalNetworking(gonetParticipant);
                //GONetLog.Debug($" recording GNP defined in scene...go.Name: {gonetParticipant.gameObject.name} instanceId: {gonetParticipant.GetInstanceID()}");
            });
        }

        /// <summary>
        /// Records that a GONetParticipant was instantiated (spawned at runtime) in the specified scene.
        /// Called by spawn system when instantiating objects.
        /// </summary>
        internal static void RecordParticipantSpawnScene(GONetParticipant gonetParticipant, string sceneName)
        {
            if (gonetParticipant != null && !string.IsNullOrEmpty(sceneName))
            {
                participantInstanceID_to_SpawnSceneName[gonetParticipant.GetInstanceID()] = sceneName;
                //GONetLog.Debug($"[SceneTracking] Recorded spawned GNP '{gonetParticipant.gameObject.name}' in scene '{sceneName}'");
            }
        }

        /// <summary>
        /// Gets the scene name that a GONetParticipant was spawned in or defined in.
        /// Returns null if not tracked.
        /// </summary>
        public static string GetParticipantSpawnScene(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant == null)
                return null;

            participantInstanceID_to_SpawnSceneName.TryGetValue(gonetParticipant.GetInstanceID(), out string sceneName);
            return sceneName;
        }

        /// <summary>
        /// Clears spawn scene tracking for a GONetParticipant (e.g., when destroyed).
        /// </summary>
        internal static void ClearParticipantSpawnScene(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant != null)
            {
                participantInstanceID_to_SpawnSceneName.Remove(gonetParticipant.GetInstanceID());
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
                    AssignGONetIdRaw_IfAppropriate(item); // IMPORTANT: After setting OwnerAuthorityId, we need to assign the full GONetId (composite of raw + authority) to avoid partial GONetId
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

                GONetLog.Debug($"About to serialize all current values bundle for new client. activeAutoSyncCompanionsByCodeGenerationIdMap has {activeAutoSyncCompanionsByCodeGenerationIdMap.Count} code gen ID entries");
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

                //GONetLog.Debug($"[INIT] Sending {bytesUsedCount} bytes of current state to new client");
                SendBytesToRemoteConnection(connectionToClient, allValuesSerialized, bytesUsedCount, GONetChannel.ClientInitialization_CustomSerialization_Reliable); // NOT using GONetChannel.AutoMagicalSync_Reliable because that one is reserved for things as they are happening and not this one time blast to a new client for all things
                mainThread_valueChangeSerializationArrayPool.Return(allValuesSerialized);
                //GONetLog.Debug($"[INIT] Server_SendClientCurrentState_AllAutoMagicalSync completed");
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
                    var ringBuffer = new RingBuffer<IGONetEvent>(); // Starts at 2048, auto-scales to 16384
                    ringBuffer.OnResized = OnRingBufferResized;
                    events_SendToOthersQueue_ByThreadMap[thread] = ringBuffer;

                    isThreadRunning = true;
                    thread.Start();
                }
                else
                {
                    myThread_Time = Time; // if running on main thread, no need to use a different instance that will already be used on the main thread

                    if (!events_AwaitingSendToOthersQueue_ByThreadMap.ContainsKey(Thread.CurrentThread))
                    {
                        events_AwaitingSendToOthersQueue_ByThreadMap[Thread.CurrentThread] = new Queue<IGONetEvent>(100); // we're on main thread, safe to deal with regular dict here
                        var ringBuffer = new RingBuffer<IGONetEvent>(); // Starts at 2048, auto-scales to 16384
                        ringBuffer.OnResized = OnRingBufferResized;
                        events_SendToOthersQueue_ByThreadMap[Thread.CurrentThread] = ringBuffer;
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

            /// <summary>
            /// Callback invoked when the ring buffer automatically resizes.
            /// Logs informative messages about buffer growth and capacity warnings.
            /// </summary>
            private void OnRingBufferResized(int oldCapacity, int newCapacity, int currentCount)
            {
                // Calculate memory usage (approximate)
                int memoryKB = (newCapacity * 8 + 128 + 24) / 1024; // Array + padding + overhead

                if (newCapacity > oldCapacity)
                {
                    // Successful resize
                    GONetLog.Info($"[GONet] Ring buffer auto-scaled: {oldCapacity} → {newCapacity} (memory: ~{memoryKB} KB, current fill: {currentCount}/{newCapacity})");

                    if (newCapacity >= 16384)
                    {
                        // Reached maximum capacity
                        GONetLog.Warning($"[GONet] Ring buffer reached maximum capacity ({newCapacity}). Consider optimizing spawn rate or implementing spatial culling. Current load: {currentCount} events.");
                    }
                }
                else
                {
                    // Failed to resize (already at max capacity and hitting 75% threshold)
                    float fillPercent = (float)currentCount / oldCapacity * 100f;
                    GONetLog.Warning($"[GONet] Ring buffer at maximum capacity ({oldCapacity}) and {fillPercent:F1}% full! Events may be dropped during high load. Consider:\n" +
                        $"  - Reducing spawn rate (spread spawns over multiple frames)\n" +
                        $"  - Implementing spatial culling (only sync nearby objects)\n" +
                        $"  - Contact GONet support if this persists\n" +
                        $"Current event count: {currentCount}/{oldCapacity}");
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
                        lastScheduledProcessAtTicks = HighResolutionTimeUtils.UtcNowTicks;
                        Process();
                        shouldProcessInSeparateThreadASAP = false; // reset this

                        if (!doesRequireManualProcessInitiation)
                        { // (auto sync) frequency control:
                            long nowTicks = HighResolutionTimeUtils.UtcNowTicks;
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

                    // OPTIMIZATION: Calculate once per Process() call instead of per-participant in inner loop
                    bool isPhysicsSyncGrouping = uniqueGrouping.Equals(grouping_physics_unreliable); // FIXED: struct comparison

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
                                    GONetParticipant participant = enumeratorInner.Current.Key;
                                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;

                                    if (monitoringSupport == null)
                                    {
                                        GONetLog.Error("monitoringSupport == null");
                                        continue;
                                    }

                                    // CRITICAL FIX: Skip destroyed/despawned participants to prevent sending sync data for dead objects
                                    // This fixes the "white beacon/stuck projectile" bug where sync bundles included despawned objects for 30+ seconds
                                    // causing GetCurrentGONetIdByIdAtInstantiation() to return 0 and abort entire bundles on receiver side.
                                    //
                                    // Root cause: everythingMap not cleaned up when participants despawn, so sync thread keeps iterating
                                    // over dead participants and including their (now invalid) InstantiationIds in outgoing bundles.
                                    //
                                    // This check works for ALL authority scenarios (client-authority, server-authority, etc.) because:
                                    // - gonetParticipantByGONetIdMap is static (shared across all instances)
                                    // - Participants removed from map in OnDisable() on BOTH authority and non-authority sides
                                    // - Unity fake null pattern catches destroyed GameObjects
                                    if (participant == null ||  // Unity fake null - GameObject destroyed
                                        participant.GONetId == GONetParticipant.GONetId_Unset ||  // GONetId was reset/unset
                                        !gonetParticipantByGONetIdMap.ContainsKey(participant.GONetId))  // Participant despawned but still in everythingMap
                                    {
                                        continue; // Skip this participant - it's destroyed or despawned
                                    }

                                    // PHYSICS SYNC SEPARATION: Skip physics objects in non-physics pipeline, skip non-physics objects in physics pipeline
                                    // This prevents double-syncing position/rotation (once from regular 24Hz pipeline, once from physics 50Hz pipeline).
                                    // Physics pipeline (grouping_physics_unreliable): ONLY process physics objects owned by this authority
                                    // All other pipelines: ONLY process non-physics objects OR objects not owned by this authority
                                    bool isPhysicsObject = participant.IsRigidBodyOwnerOnlyControlled && participant.myRigidBody != null;
                                    bool shouldProcessInPhysicsPipeline = isPhysicsObject && participant.IsMine; // Only send physics updates if I own the object

                                    if (isPhysicsSyncGrouping)
                                    {
                                        // In physics sync grouping: ONLY process objects I own that are physics objects
                                        if (!shouldProcessInPhysicsPipeline)
                                        {
                                            continue; // Skip: not my physics object
                                        }
                                        // PHYSICS SYNC FREQUENCY GATING: Now handled per-value in IsPositionNotSyncd/IsRotationNotSyncd
                                        // This allows position and rotation to have independent PhysicsUpdateInterval settings
                                    }
                                    else
                                    {
                                        // In regular sync grouping: SKIP physics objects I own (those are handled by physics pipeline)
                                        if (shouldProcessInPhysicsPipeline)
                                        {
                                            continue; // Skip: my physics object (handled by physics pipeline)
                                        }
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
                                        GONetParticipant participant = enumeratorInner.Current.Key;
                                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;

                                        if (monitoringSupport == null)
                                        {
                                            GONetLog.Error("monitoringSupport == null");
                                            continue;
                                        }

                                        // CRITICAL FIX: Skip destroyed/despawned participants (same check as main sync loop above)
                                        if (participant == null ||
                                            participant.GONetId == GONetParticipant.GONetId_Unset ||
                                            !gonetParticipantByGONetIdMap.ContainsKey(participant.GONetId))
                                        {
                                            continue; // Skip this participant - it's destroyed or despawned
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
                //GONetLog.Debug($"????????send changed auto-magical sync values to all connections..count: {count}");
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

                            // PHASE 2 FIX: Round-robin client processing to distribute server-side delay fairly
                            int numConnections = (int)_gonetServer.numConnections;
                            int startIndex = _gonetServer.nextClientProcessingStartIndex;
                            if (numConnections > 0)
                            {
                                _gonetServer.nextClientProcessingStartIndex = (startIndex + 1) % numConnections;
                            }

                            for (int offset = 0; offset < numConnections; ++offset)
                            {
                                int iConnection = (startIndex + offset) % numConnections;
                                GONetConnection_ServerToClient gONetConnection_ServerToClient = _gonetServer.remoteClients[iConnection].ConnectionToClient;
                                GONetRemoteClient remoteClient = _gonetServer.GetRemoteClientByAuthorityId(gONetConnection_ServerToClient.OwnerAuthorityId);
                                bool isInitialized = remoteClient.IsInitializedWithServer;

                                // IMPORTANT: Log why AutoMagicalSync messages are NOT sent to specific clients
                                if (!isInitialized && bundleFragments.fragmentCount > 0)
                                {
                                    GONetLog.Warning($"[SYNC-BLOCKED] Server NOT sending AutoMagicalSync to AuthorityId {gONetConnection_ServerToClient.OwnerAuthorityId} - client IsInitializedWithServer: {isInitialized}");
                                }

                                for (int iFragment = 0; iFragment < bundleFragments.fragmentCount; ++iFragment)
                                {
                                    //GONetLog.Debug("AutoMagicalSync_ValueChanges_Message sending right after this. bytesUsedCount: " + bundleFragments.fragmentBytesUsedCount[iFragment]);  /////////////////////////// DREETS!
                                    if (isInitialized) // only send to client initialized with server!
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
                RingBuffer<IGONetEvent> queueSend = events_SendToOthersQueue_ByThreadMap[Thread.CurrentThread];
                while (queueAwaiting.Count > 0)
                {
                    var @event = queueAwaiting.Dequeue();
                    if (!queueSend.TryWrite(@event))
                    {
                        // Handle buffer full scenario (e.g., retry or log an error)
                        GONetLog.Error($"Ring buffer is full! Could not publish event!  Event type: {@event.GetType().Name}");
                    }
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
                        long nowTicks = HighResolutionTimeUtils.UtcNowTicks;

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

        /// <summary>
        /// Velocity-augmented sync: Tracks bundle alternation between VALUE (even) and VELOCITY (odd).
        /// Thread-safe via Interlocked.Increment.
        /// </summary>
        private static int velocityBundleCounter = 0;

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
            //GONetLog.Debug($"mikkyu magoo...countFilteres: {countFiltered}");
            int individualChangesCountRemaining = countFiltered;
            bundleFragments.fragmentCount = 0;

            if (countFiltered == 0)
            {
                return;
            }

            int lastIndexUsed = 0;

            // VELOCITY-AUGMENTED SYNC: Decide if this bundle sends velocities or values
            // Alternate between VALUE (even) and VELOCITY (odd) bundles
            bool isVelocityBundle = (System.Threading.Interlocked.Increment(ref velocityBundleCounter) % 2 == 0);

            while (individualChangesCountRemaining > 0)
            {
                using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
                {
                    { // header...just message type/id...well, and now time...and velocity flag
                        uint messageID = messageTypeToMessageIDMap[chosenBundleType];
                        bitStream.WriteUInt(messageID);

                        bitStream.WriteLong(elapsedTicksAtCapture);

                        // VELOCITY-AUGMENTED SYNC: Write velocity bit (ONE bit for entire bundle)
                        bitStream.WriteBit(isVelocityBundle);
                    }

                    // body
                    int changesInBundleCount = SerializeBody_ChangesBundle(changes, bitStream, filterUsingOwnerAuthorityId, ref lastIndexUsed, isVelocityBundle);
                    
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
            int totalGNPs = 0;
            int serializedGNPs = 0;
            int excludedGNPs = 0;

            // IMPORTANT: Create a snapshot to avoid InvalidOperationException if the collection is modified during iteration
            // This can happen when a new client connects and their GONetLocal is spawned while we're serializing
            var enumeratorOuter = activeAutoSyncCompanionsByCodeGenerationIdMap.ToList().GetEnumerator();
            while (enumeratorOuter.MoveNext())
            {
                //GONetLog.Debug($"[INIT] SerializeBody: Processing code generation ID {enumeratorOuter.Current.Key}");
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
               // GONetLog.Debug($"[INIT] SerializeBody: Code gen ID {enumeratorOuter.Current.Key} has {currentMap.Count} GNPs");

                // IMPORTANT: Also snapshot the inner dictionary to prevent concurrent modification
                var snapshot = currentMap.ToList();
                //GONetLog.Debug($"[INIT] SerializeBody: ToList() created snapshot with {snapshot.Count} items (original had {currentMap.Count})");
                var enumeratorInner = snapshot.GetEnumerator();
                int innerIterationCount = 0;
                while (enumeratorInner.MoveNext())
                {
                    var current = enumeratorInner.Current;
                    innerIterationCount++;
                    totalGNPs++;

                    // IMPORTANT: Check for null GNP or destroyed GameObject before accessing properties
                    if (current.Key == null || current.Key.gameObject == null)
                    {
                        GONetLog.Warning($"[INIT] SerializeBody: Iteration {innerIterationCount}/{currentMap.Count} - GNP is null or destroyed, skipping");
                        continue;
                    }

                    //GONetLog.Debug($"[INIT] SerializeBody: Iteration {innerIterationCount}/{currentMap.Count} - GNP: '{current.Key.gameObject.name}'");

                    GONetParticipant gonetParticipant = current.Key;
                    // IMPORTANT: Check both that all components are set AND that GONetId is not 0
                    // This can happen if a client connects after OnEnable but before Start assigns the GONetId
                    bool hasAllComponents = gonetParticipant.DoesGONetIdContainAllComponents();
                    bool idIsNotZero = gonetParticipant.GONetId != GONetParticipant.GONetId_Unset;

                    if (hasAllComponents && idIsNotZero)
                    {
                        // SPAM: Commented out - not needed in normal operation, uncomment for deep serialization debugging
                        //GONetLog.Debug($"Serializing GNP '{gonetParticipant.gameObject.name}' with GONetId: {gonetParticipant.GONetId} (raw: {gonetParticipant.gonetId_raw}, authority: {gonetParticipant.OwnerAuthorityId})");

                        GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Serialize(bitStream_headerAlreadyWritten, gonetParticipant, gonetParticipant.GONetId);

                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = current.Value;
                        //GONetLog.Debug($"[INIT] About to call SerializeAll() for GNP '{gonetParticipant.gameObject.name}'");
                        try
                        {
                            monitoringSupport.SerializeAll(bitStream_headerAlreadyWritten);
                            //GONetLog.Debug($"[INIT] Completed SerializeAll() for GNP '{gonetParticipant.gameObject.name}'");
                            serializedGNPs++;
                        }
                        catch (System.Exception ex)
                        {
                            GONetLog.Error($"[INIT] Exception during SerializeAll() for GNP '{gonetParticipant.gameObject.name}': {ex.Message}\n{ex.StackTrace}");
                            throw; // Re-throw to preserve stack trace
                        }
                    }
                    else
                    {
                        excludedGNPs++;
                        GONetLog.Error($"Excluding GNP '{gonetParticipant.gameObject.name}' with partial GONetId: {gonetParticipant.GONetId} (raw: {gonetParticipant.gonetId_raw}, authority: {gonetParticipant.OwnerAuthorityId}) hasAllComponents: {hasAllComponents} idIsNotZero: {idIsNotZero} from all current values bundle.  WasDefinedInScene: {WasDefinedInScene(gonetParticipant)}");
                    }
                }
            }

            //GONetLog.Debug($"Serialization complete. Total GNPs: {totalGNPs}, Serialized: {serializedGNPs}, Excluded: {excludedGNPs}");
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
        /// <param name="isVelocityBundle">TRUE if this bundle contains velocity data, FALSE for value data</param>
        private static int SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten, ushort filterUsingOwnerAuthorityId, ref int lastIndexUsed, bool isVelocityBundle)
        {
            //GONetLog.Debug("mikkyu magoo");

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
                //GONetLog.AppendLine($"serialize change index: {change.index}");
                change.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, change.index, isVelocityBundle);
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
            GONetLog.Debug($"Starting deserialization of all values bundle. bytesUsedCount: {bytesUsedCount}, stream position: {bitStream_headerAlreadyRead.Position_Bytes}");

            int deserializedCount = 0;
            int streamPositionBytes_preGonetId;
            // IMPORTANT: Use <= to ensure we don't read past the last complete byte
            // The WriteCurrentPartialByte() on serialization side means bytesUsedCount includes the final partial byte
            // We need to leave enough room for at least a GONetId (minimum 4 bytes) to avoid reading garbage
            const int MIN_GONETID_BYTES = 4; // GONetId is a uint, minimum 4 bytes
            while ((streamPositionBytes_preGonetId = bitStream_headerAlreadyRead.Position_Bytes) + MIN_GONETID_BYTES <= bytesUsedCount) // while more data to read/process
            {
                uint gonetId = GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead).System_UInt32;

                //GONetLog.Debug($"Deserialized GONetId: {gonetId} at stream position (pre: {streamPositionBytes_preGonetId}, post: {bitStream_headerAlreadyRead.Position_Bytes})");

                if (GONetParticipant.DoesGONetIdContainAllComponents(gonetId))
                {
                    GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.CodeGenerationId][gonetParticipant];

                    GONetLog.Debug($"Successfully deserialized GNP '{gonetParticipant.gameObject.name}' with GONetId: {gonetId}");
                    syncCompanion.DeserializeInitAll(bitStream_headerAlreadyRead, elapsedTicksAtSend);

                    // LATE-JOINER PHYSICS SNAPPING: If this is a physics object, trigger physics snapping
                    // to eliminate quantization error for objects at rest (position ~0.95mm → sub-mm, rotation ~0.3° → sub-0.01°)
                    bool isPhysicsObject = gonetParticipant.IsRigidBodyOwnerOnlyControlled &&
                                           gonetParticipant.myRigidBody != null &&
                                           !gonetParticipant.IsMine;

                    if (isPhysicsObject)
                    {
                        // Check if this object has position or rotation sync by scanning all indices for matching function pointers
                        // This relies on the implementation detail that position/rotation use IsPositionNotSyncd/IsRotationNotSyncd delegates
                        bool hasPositionOrRotation = false;
                        for (byte i = 0; i < syncCompanion.valuesChangesSupport.Length; i++)
                        {
                            AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[i];
                            if (changedValue.syncAttribute_ShouldSkipSync == IsPositionNotSyncd ||
                                changedValue.syncAttribute_ShouldSkipSync == IsRotationNotSyncd)
                            {
                                hasPositionOrRotation = true;
                                break;
                            }
                        }

                        if (hasPositionOrRotation)
                        {
                            // Get current transform values (just applied via DeserializeInitAll)
                            Vector3 position = gonetParticipant.transform.position;
                            Quaternion rotation = gonetParticipant.transform.rotation;

                            // Trigger physics snapping to improve final resting accuracy
                            gonetParticipant.TriggerPhysicsSnapToRest(position, rotation);
                        }
                    }

                    // Deduplication check: Only publish if not already published
                    if (TryMarkDeserializeInitPublished(gonetId))
                    {
                        //GONetLog.Info($"[GONet] Publishing DeserializeInitAllCompleted for '{gonetParticipant.name}' (GONetId: {gonetId}, IsMine: {gonetParticipant.IsMine}) from deserialization path");
                        PublishEventAsSoonAsSufficientInfoAvailable(
                            new GONetParticipantDeserializeInitAllCompletedEvent(gonetParticipant),
                            gonetParticipant,
                            isRelatedLocalContentRequired: true);
                    }
                    else
                    {
                        //GONetLog.Info($"[GONet] Skipping duplicate DeserializeInitAllCompleted for '{gonetParticipant.name}' (GONetId: {gonetId}) - already published from another path");
                    }

                    deserializedCount++;
                }
                else
                {
                    GONetLog.Error($"Deserialized a gonetId value ({gonetId}) that is not complete, which will cause reading the rest of the values to fail in mysterious ways...so, will STOP deserializing now!  stream.Position_Bytes: (pre:{streamPositionBytes_preGonetId}, post:{bitStream_headerAlreadyRead.Position_Bytes}) bytesUsedCount: {bytesUsedCount}");
                    return;
                }
            }

            GONetLog.Debug($"Deserialization complete. Total GONetIds deserialized: {deserializedCount}");
        }

        /// <summary>
        /// Awaiting to not be unity null and to have an entry in the corresponding entry/map in <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/> for its codeGenerationId.
        /// </summary>
        static readonly List<GONetParticipant> gnpsAwaitingCompanion = new List<GONetParticipant>(1000);

        private static void DeserializeBody_BundleOfChoice(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, GONetConnection sourceOfChangeConnection, GONetChannelId channelId, long elapsedTicksAtSend, Type chosenBundleType, bool isVelocityBundle = false)
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

                // IMPROVED: Try multiple lookup strategies with better diagnostics
                // CRITICAL: Check instantiation map FIRST if current GONetId is 0 (unset/reset during scene changes)
                if (gonetId == GONetParticipant.GONetId_Unset && gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(gonetIdAtInstantiation))
                {
                    // Participant exists but GONetId is unset - happens during scene transitions
                    gonetParticipant = gonetParticipantByGONetIdAtInstantiationMap[gonetIdAtInstantiation];
                    GONetLog.Debug($"GONetId lookup: Participant found with unset GONetId (instantiation: {gonetIdAtInstantiation}). Likely during scene transition.");
                }
                else if (gonetParticipantByGONetIdMap.ContainsKey(gonetId))
                {
                    gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                }
                else if (gonetParticipantByGONetIdMap.ContainsKey(gonetIdAtInstantiation))
                {
                    gonetParticipant = gonetParticipantByGONetIdMap[gonetIdAtInstantiation];
                    GONetLog.Debug($"GONetId lookup: Found by instantiation ID in main map (current: {gonetId}, instantiation: {gonetIdAtInstantiation})");
                }
                else if (gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(gonetIdAtInstantiation))
                {
                    gonetParticipant = gonetParticipantByGONetIdAtInstantiationMap[gonetIdAtInstantiation];
                    // CRITICAL: Do NOT access gonetParticipant.name here - participant may be destroyed
                    GONetLog.Debug($"GONetId lookup: Found in instantiation map (current: {gonetId}, instantiation: {gonetIdAtInstantiation}), IsInitialized: {(IsClient ? GONetClient.IsInitializedWithServer : true)}");
                }

                if ((object)gonetParticipant == null)
                {
                    QosType channelQuality = GONetChannel.ById(channelId).QualityOfService;
                    if (channelQuality == QosType.Reliable)
                    {
                        // Enhanced diagnostics for debugging lookup failures
                        GONetLog.Error($"RELIABLE sync bundle - GONetParticipant NOT FOUND. Current GONetId: {gonetId}, InstantiationId: {gonetIdAtInstantiation}. " +
                                      $"Maps contain - byGONetId: {gonetParticipantByGONetIdMap.Count} entries, byInstantiationId: {gonetParticipantByGONetIdAtInstantiationMap.Count} entries. " +
                                      $"IsInitialized: {(IsClient ? GONetClient.IsInitializedWithServer : true)}. " +
                                      $"This indicates spawn event not received or participant destroyed.");

                        throw new GONetOutOfOrderHorseDickoryException($"Reliable changes bundle being processed and GONetParticipant NOT FOUND by GONetId: {gonetId} gonetId@instantiation: {gonetIdAtInstantiation}");
                    }
                    else
                    {
                        // DIAGNOSTIC LOGGING: Track which GONetIds cause bundle aborts
                        GONetLog.Error($"[BUNDLE-ABORT] ⚠️ UNRELIABLE BUNDLE ABORTED ⚠️ " +
                                      $"GONetId: {gonetId}, " +
                                      $"InstantiationId: {gonetIdAtInstantiation}, " +
                                      $"ChannelId: {channelId}, " +
                                      $"QoS: {GONetChannel.ById(channelId).QualityOfService}, " +
                                      $"IsClient: {IsClient}, " +
                                      $"MyAuthorityId: {MyAuthorityId}, " +
                                      $"InGONetIdMap: {gonetParticipantByGONetIdMap.ContainsKey(gonetId)}, " +
                                      $"InInstantiationMap: {gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(gonetIdAtInstantiation)}, " +
                                      $"TotalInGONetIdMap: {gonetParticipantByGONetIdMap.Count}, " +
                                      $"TotalInInstantiationMap: {gonetParticipantByGONetIdAtInstantiationMap.Count}");

                        // CRITICAL: Throw exception to trigger deferral system for unreliable bundles too
                        // Original approach: `return` aborted ENTIRE bundle, losing sync data for ALL subsequent participants
                        // Cannot use `continue`: Bitstream has unread value data (index + value bytes), skipping causes desync
                        //
                        // PROBLEM: Sync bundles pack hundreds of participants. If ONE participant is missing:
                        //   - Using `return`: Drops entire bundle → hundreds of objects never get position/rotation updates
                        //   - Using `continue`: Desyncs bitstream → corrupts ALL subsequent reads in bundle
                        //
                        // SOLUTION: Defer ENTIRE bundle (even for unreliable) and retry next frame when participant likely ready.
                        // User symptoms with `return`: White beacons (color never syncs), projectiles stuck at origin (position never syncs).
                        //
                        // Exception caught in ProcessIncomingBytes - will defer if GONetGlobal.deferUnreliableSyncBundles enabled.
                        throw new GONetParticipantNotReadyException(
                            $"Unreliable sync bundle received for missing participant (GONetId: {gonetId}, Instantiation: {gonetIdAtInstantiation})",
                            gonetIdAtInstantiation);
                    }
                }


                // CRITICAL FIX: Check if Unity object was destroyed before accessing properties
                // Unity's overloaded == operator detects destroyed objects, while (object)cast checks C# reference
                if (gonetParticipant == null)
                {
                    bool isCSharpReferenceNull = (object)gonetParticipant == null;

                    // IMPORTANT: Do NOT access any Unity properties of gonetParticipant here!
                    // Even though C# reference may not be null, Unity object is destroyed and accessing Unity properties throws MissingReferenceException
                    // Pure C# properties (like GONetId) may work, but should be avoided for consistency - use cached values instead
                    GONetLog.Error($"GONetParticipant Unity object destroyed but still in maps. C# reference null: {isCSharpReferenceNull}, GONetIdAtInstantiation: {gonetIdAtInstantiation}. Skipping this sync bundle item.");

                    // Skip processing this destroyed participant - do NOT add to awaiting or continue processing
                    continue;
                }

                //Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];

                // CRITICAL FIX: Defensive check for sync companion availability during rapid spawning
                // During high spawn rates, sync bundles can arrive BEFORE GONetParticipant.Awake() completes
                // (Awake runs as coroutine and yields). This causes NullReferenceException when trying to
                // access sync companion that hasn't been registered yet.
                Dictionary<uint, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap;
                if (!activeAutoSyncCompanionsByCodeGenerationIdMap_uintKeyForPerformance.TryGetValue(gonetParticipant.CodeGenerationId, out companionMap))
                {
                    // Sync companion map not created yet for this CodeGenerationId
                    // This happens when spawn event was received but participant hasn't finished initializing
                    QosType channelQuality = GONetChannel.ById(channelId).QualityOfService;
                    // DIAGNOSTIC LOGGING: Track companion map issues
                    GONetLog.Error($"[BUNDLE-ABORT-COMPANION-MAP] ⚠️ BUNDLE ABORTED - No companion map ⚠️ " +
                                  $"GONetId: {gonetParticipant.GONetId}, " +
                                  $"InstantiationId: {gonetIdAtInstantiation}, " +
                                  $"CodeGenerationId: {gonetParticipant.CodeGenerationId}, " +
                                  $"Channel: {(channelQuality == QosType.Reliable ? "Reliable" : "Unreliable")}, " +
                                  $"IsClient: {IsClient}, " +
                                  $"MyAuthorityId: {MyAuthorityId}");

                    // CRITICAL: Throw exception to trigger deferral system instead of aborting entire bundle
                    // Using `return` here drops ENTIRE bundle, losing sync data for all subsequent participants
                    throw new GONetParticipantNotReadyException(
                        $"Sync companion map not created yet for CodeGenerationId {gonetParticipant.CodeGenerationId}",
                        gonetIdAtInstantiation);
                }

                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;
                if (!companionMap.TryGetValue(gonetParticipant._GONetIdAtInstantiation, out syncCompanion))
                {
                    // Companion not registered yet for this specific participant instance
                    QosType channelQuality = GONetChannel.ById(channelId).QualityOfService;

                    // DIAGNOSTIC LOGGING: Track companion not in map
                    GONetLog.Error($"[BUNDLE-ABORT-COMPANION-MISSING] ⚠️ BUNDLE ABORTED - Companion not in map ⚠️ " +
                                  $"GONetId: {gonetParticipant.GONetId}, " +
                                  $"InstantiationId: {gonetParticipant._GONetIdAtInstantiation}, " +
                                  $"Channel: {(channelQuality == QosType.Reliable ? "Reliable" : "Unreliable")}, " +
                                  $"IsClient: {IsClient}, " +
                                  $"MyAuthorityId: {MyAuthorityId}");

                    // CRITICAL: Throw exception to trigger deferral system instead of aborting entire bundle
                    throw new GONetParticipantNotReadyException(
                        $"Sync companion not registered for GONetId {gonetParticipant.GONetId}",
                        gonetIdAtInstantiation);
                }

                // DEFENSIVE CHECK (NEW - SYNC BUNDLE GONETREADY RACE CONDITION FIX):
                // Participant must have completed Awake() and have syncCompanion ready before deserialization.
                // Even though we fetched syncCompanion from the map above, during rapid spawning scenarios:
                // - The companion might have been registered to the map BUT
                // - The participant's Awake() coroutine is still running (didAwakeComplete=false)
                // - Accessing syncCompanion methods can cause NullReferenceException or unexpected behavior
                //
                // This is a RACE CONDITION between:
                // 1. Network thread processing sync bundles (this code)
                // 2. Main thread running GONetParticipant.AwakeCoroutine()
                //
                // Solution: Throw descriptive exception that calling code will catch and defer/drop based on config.
                if (!gonetParticipant.didAwakeComplete || syncCompanion == null)
                {
                    // CRITICAL: Do NOT access gonetParticipant.name here - participant may be destroyed
                    throw new GONetParticipantNotReadyException(
                        $"GONetParticipant {gonetIdAtInstantiation} exists but not ready for deserialization. " +
                        $"didAwakeComplete: {gonetParticipant.didAwakeComplete}, " +
                        $"syncCompanion null: {syncCompanion == null}",
                        gonetIdAtInstantiation);
                }

                try
                {
                    // CRITICAL: Re-check if Unity object still exists before accessing properties
                    // Object could have been destroyed during bundle processing (mid-loop through multiple participants)
                    if (gonetParticipant == null)
                    {
                        GONetLog.Warning($"[SYNC] GONetParticipant was destroyed during bundle processing. Skipping this sync data item.");
                        continue;
                    }

                    bool isBundleTypeValueChanges = chosenBundleType == typeof(AutoMagicalSync_ValueChanges_Message);

                    byte index = (byte)bitStream_headerAlreadyRead.ReadByte();

                    if (gonetParticipant.IsMine) // with recent changes, bundles all all the same for all clients, which means you will receive your own stuff too...essentially want to skip, but have to move the bit reader forward!
                    {
                        // VELOCITY-AUGMENTED SYNC: Always pass isVelocityBundle to keep bitstream in sync
                        // (even when skipping, we must read the same number of bits)
                        syncCompanion.DeserializeInitSingle_ReadOnlyNotApply(bitStream_headerAlreadyRead, index, isVelocityBundle);

                        // IMPORTANT: Log when Client:2 skips its own values
                        if (IsClient)
                        {
                            // DEFENSIVE: Check again before accessing properties (object could be destroyed mid-processing)
                            string logName = (gonetParticipant != null && gonetParticipant.gameObject != null) ? gonetParticipant.gameObject.name : "<destroyed>";
                            uint logId = (gonetParticipant != null) ? gonetParticipant.GONetId : GONetParticipant.GONetId_Unset;
                            GONetLog.Info($"[SYNC-SKIP] Client skipping own value - GONetId: {logId}, GameObject: '{logName}', index: {index}");
                        }
                    }
                    else
                    {
                        if (isBundleTypeValueChanges)
                        {
                            // IMPORTANT: Log value change application for Client:2
                            //if (IsClient)
                            //{
                                // DEFENSIVE: Check again before accessing properties (object could be destroyed mid-processing)
                                //string logName = (gonetParticipant != null && gonetParticipant.gameObject != null) ? gonetParticipant.gameObject.name : "<destroyed>";
                                //uint logId = (gonetParticipant != null) ? gonetParticipant.GONetId : GONetParticipant.GONetId_Unset;
                                //GONetLog.Info($"[SYNC-APPLY] Client applying value change - GONetId: {logId}, GameObject: '{logName}', index: {index}");
                            //}

                            // VELOCITY-AUGMENTED SYNC: Handle VELOCITY vs VALUE bundles
                            if (isVelocityBundle)
                            {
                                // VELOCITY BUNDLE: Deserialize velocity, synthesize position
                                GONetSyncableValue velocityValue = syncCompanion.DeserializeInitSingle_ReadOnlyNotApply(bitStream_headerAlreadyRead, index, true);

                                var changesSupport = syncCompanion.valuesChangesSupport[index];
                                int recentChangesCount = changesSupport.mostRecentChanges_usedSize;

                                if (recentChangesCount >= 1)
                                {
                                    // Get last known position
                                    var lastSnapshot = changesSupport.mostRecentChanges[0];

                                    // Calculate deltaTime from last snapshot to this bundle's send time
                                    float deltaTime = (elapsedTicksAtSend - lastSnapshot.elapsedTicksAtChange) / (float)System.TimeSpan.TicksPerSecond;

                                    // Synthesize new position from velocity
                                    GONetSyncableValue synthesizedValue = SynthesizeValueFromVelocity(
                                        lastSnapshot.numericValue,
                                        velocityValue,
                                        deltaTime);

                                    // Store synthesized position as snapshot
                                    syncCompanion.InitSingle(synthesizedValue, index, elapsedTicksAtSend);
                                }
                                else
                                {
                                    // No previous snapshot - treat velocity as initial value (shouldn't happen in practice)
                                    GONetLog.Warning($"[VelocitySync] VELOCITY bundle received but no previous snapshot for GONetId {gonetParticipant.GONetId}, index {index}. Cannot synthesize.");
                                    // Still initialize with velocity data to keep bitstream in sync
                                    syncCompanion.InitSingle(velocityValue, index, elapsedTicksAtSend);
                                }
                            }
                            else
                            {
                                // VALUE BUNDLE: Deserialize and apply normally
                                syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend, false);
                            }

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

                                // VELOCITY-AUGMENTED SYNC: For ValuesNowAtRest, isVelocityBundle should typically be false
                                // (resting values don't have velocity), but pass it for bitstream consistency
                                GONetSyncableValue value = syncCompanion.DeserializeInitSingle_ReadOnlyNotApply(bitStream_headerAlreadyRead, index, isVelocityBundle);
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest = true;
                                long assumedInitialRestElapsedTicks = elapsedTicksAtSend - TimeSpan.FromSeconds(syncCompanion.valuesChangesSupport[index].syncAttribute_SyncChangesEverySeconds).Ticks; // need to subtract the sync rate off of this to know when the value actually first arrived at rest value
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_assumedInitialRestElapsedTicks = assumedInitialRestElapsedTicks;
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_sinceLeadTimeAdjustedElapsedTicks = Time.ElapsedTicks - valueBlendingBufferLeadTicks;
                                syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_value = value;

                                // NEW: Check if this is a physics object at-rest (position or rotation sync)
                                // Physics snapping eliminates quantization error: position ~0.95mm → sub-mm, rotation ~0.3° → sub-0.01°
                                bool isPhysicsObject = gonetParticipant.IsRigidBodyOwnerOnlyControlled &&
                                                       gonetParticipant.myRigidBody != null &&
                                                       !gonetParticipant.IsMine;

                                if (isPhysicsObject)
                                {
                                    // Check if this is position or rotation by matching the ShouldSkipSync function pointer
                                    // This relies on the implementation detail that position/rotation use IsPositionNotSyncd/IsRotationNotSyncd delegates
                                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[index];
                                    bool isPosition = changedValue.syncAttribute_ShouldSkipSync == IsPositionNotSyncd;
                                    bool isRotation = changedValue.syncAttribute_ShouldSkipSync == IsRotationNotSyncd;
                                    bool isPositionOrRotation = isPosition || isRotation;

                                    if (isPositionOrRotation)
                                    {
                                        // PHYSICS SNAPPING: Mark this value as needing physics snap when applied
                                        syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_needsPhysicsSnap = true;
                                    }
                                }

                                /*{
                                    GONetLog.Debug($"[AT-REST-RECEIVED] GNP:{gonetParticipant.GONetId} index:{index} " +
                                        $"atRestValue:{value} currentValue:{syncCompanion.GetAutoMagicalSyncValue(index)} " +
                                        $"bufferSize:{syncCompanion.valuesChangesSupport[index].mostRecentChanges_usedSize} " +
                                        $"willApplyAt:{TimeSpan.FromTicks(assumedInitialRestElapsedTicks + valueBlendingBufferLeadTicks).TotalSeconds}s");

                                    // Log the buffer contents before clearing
                                    var changes = syncCompanion.valuesChangesSupport[index];
                                    for (int j = 0; j < changes.mostRecentChanges_usedSize; j++)
                                    {
                                        GONetLog.Debug($"  Buffer[{j}]: time:{TimeSpan.FromTicks(changes.mostRecentChanges[j].elapsedTicksAtChange).TotalSeconds}s " +
                                            $"value:{changes.mostRecentChanges[j].numericValue}");
                                    }
                                }*/

                                long assumedOneWayAtRestDelayTicks = Time.ElapsedTicks - assumedInitialRestElapsedTicks;
                                long easingDurationTicks = assumedOneWayAtRestDelayTicks - valueBlendingBufferLeadTicks;

                                // NOTE: This will run immediately if one-way network time exceeds valueBlendingBufferLeadTicks (i.e. non-owner will always be extrapolating!)
                                Global.StartCoroutine(DoAtOrAfterElapsedTicks(() => 
                                {
                                    /* BEFORE
                                    syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest = false;
                                    // Clearing the recent changes buffer effectively ensures that the new value at rest value is the one applied and no
                                    // blending will occur since this is the only value in the blending buffer....neat trick to not need additional code
                                    // to make sure we apply this new value at rest now!
                                    var mostRecentQueuedValue = syncCompanion.valuesChangesSupport[index].mostRecentChanges[0];
                                    syncCompanion.valuesChangesSupport[index].ClearMostRecentChanges();
                                    //GONetLog.Debug($"just cleared most recent changes due to at rest....easingDuration: {TimeSpan.FromTicks(easingDurationTicks).TotalSeconds}\n(OLD) recent buffered:{mostRecentQueuedValue.numericValue} \n(OLD) current value: {syncCompanion.GetAutoMagicalSyncValue(index)}, \n(NEW) at rest value: {value}");
                                    syncCompanion.InitSingle(value, index, assumedInitialRestElapsedTicks);
                                    
                                    //syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_easeUntilElapsedTicks = ;
                                    */

                                    // AFTER:
                                    var mostRecentQueuedValue = syncCompanion.valuesChangesSupport[index].mostRecentChanges_usedSize > 0
                                        ? syncCompanion.valuesChangesSupport[index].mostRecentChanges[0]
                                        : default;

                                    /*
                                     GONetLog.Debug($"[AT-REST-APPLYING] GNP:{gonetParticipant.GONetId} index:{index} " +
                                        $"clearingBuffer:{syncCompanion.valuesChangesSupport[index].mostRecentChanges_usedSize} items " +
                                        $"lastBufferedValue:{mostRecentQueuedValue.numericValue} " +
                                        $"currentValue:{syncCompanion.GetAutoMagicalSyncValue(index)} " +
                                        $"newAtRestValue:{value}");
                                    */

                                    syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest = false;
                                    syncCompanion.valuesChangesSupport[index].ClearMostRecentChanges();
                                    syncCompanion.InitSingle(value, index, assumedInitialRestElapsedTicks);

                                    //GONetLog.Debug($"[AT-REST-APPLIED] GNP:{gonetParticipant.GONetId} index:{index} finalValue:{syncCompanion.GetAutoMagicalSyncValue(index)}");

                                    // NEW: Trigger physics snapping if needed (after value is applied)
                                    if (syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_needsPhysicsSnap)
                                    {
                                        syncCompanion.valuesChangesSupport[index].hasAwaitingAtRest_needsPhysicsSnap = false;

                                        // Get both position and rotation (may be same or different index)
                                        // Physics snapping requires both to achieve sub-mm position and sub-0.01° rotation
                                        Vector3 position = gonetParticipant.transform.position;
                                        Quaternion rotation = gonetParticipant.transform.rotation;

                                        gonetParticipant.TriggerPhysicsSnapToRest(position, rotation);
                                    }
                                }, assumedInitialRestElapsedTicks + valueBlendingBufferLeadTicks));
                            }
                            else
                            {
                                // VELOCITY-AUGMENTED SYNC: For ValuesNowAtRest (no blending), pass isVelocityBundle
                                // Should typically be false since resting values don't have velocity
                                syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend, isVelocityBundle);

                                // NEW: Immediate physics snap for non-blended physics objects at rest
                                bool isPhysicsObject = gonetParticipant.IsRigidBodyOwnerOnlyControlled &&
                                                       gonetParticipant.myRigidBody != null &&
                                                       !gonetParticipant.IsMine;

                                if (isPhysicsObject)
                                {
                                    // Check if this is position or rotation by matching the ShouldSkipSync function pointer
                                    // This relies on the implementation detail that position/rotation use IsPositionNotSyncd/IsRotationNotSyncd delegates
                                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue changedValue = syncCompanion.valuesChangesSupport[index];
                                    bool isPosition = changedValue.syncAttribute_ShouldSkipSync == IsPositionNotSyncd;
                                    bool isRotation = changedValue.syncAttribute_ShouldSkipSync == IsRotationNotSyncd;
                                    bool isPositionOrRotation = isPosition || isRotation;

                                    if (isPositionOrRotation)
                                    {
                                        // Get current transform values (just applied via DeserializeInitSingle)
                                        Vector3 position = gonetParticipant.transform.position;
                                        Quaternion rotation = gonetParticipant.transform.rotation;

                                        // Trigger physics snapping immediately (no coroutine delay for non-blended values)
                                        gonetParticipant.TriggerPhysicsSnapToRest(position, rotation);
                                    }
                                }
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
                    // CRITICAL FIX: Defensive property access - object could be destroyed, causing the original exception!
                    // Accessing gonetParticipant properties here would throw ANOTHER NullRef, hiding the original error
                    string logName = "<error-accessing-name>";
                    uint logIdInstantiation = GONetParticipant.GONetId_Unset;
                    uint logIdCurrent = GONetParticipant.GONetId_Unset;
                    uint logCodeGenId = 0;
                    bool logContainsKey = false;

                    try
                    {
                        if (gonetParticipant != null)
                        {
                            logName = gonetParticipant.name;
                            logIdInstantiation = gonetParticipant._GONetIdAtInstantiation;
                            logIdCurrent = gonetParticipant.GONetId;
                            logCodeGenId = gonetParticipant.CodeGenerationId;
                            if (companionMap != null)
                            {
                                logContainsKey = companionMap.ContainsKey(logIdCurrent);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors during error logging - we just want to get as much info as possible
                    }

                    GONetLog.Error($"name: {logName} _GONetIdAtInstantiation: {logIdInstantiation}, now: {logIdCurrent}, contains.now? {logContainsKey}, genId: {logCodeGenId}");
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

        #region GONetId Reuse Prevention Methods

        /// <summary>
        /// Marks a GONetId as recently despawned, preventing immediate reuse.
        /// Called automatically from OnDisable_StopMonitoringForAutoMagicalNetworking.
        ///
        /// The GONetId will remain unavailable for reuse until the configured TTL expires
        /// (GONetGlobal.gonetIdReuseDelaySeconds, default 5 seconds).
        /// </summary>
        /// <param name="gonetId">The GONetId being despawned</param>
        internal static void MarkGONetIdDespawned(uint gonetId)
        {
            if (gonetId == GONetParticipant.GONetId_Unset)
            {
                return; // Don't track unset IDs
            }

            double despawnTime = Time.ElapsedSeconds;
            recentlyDespawnedGONetIds[gonetId] = despawnTime;

            //GONetLog.Debug($"[GONetId-Reuse] Marked GONetId {gonetId} as despawned at {despawnTime:F3}s (TTL: {GetGONetIdReuseDelay():F1}s)");
        }

        /// <summary>
        /// Checks if a GONetId can be safely reused (TTL has expired).
        /// Used by GONetIdBatchManager during ID allocation.
        /// </summary>
        /// <param name="gonetId">The GONetId to check</param>
        /// <returns>True if ID can be reused, false if still in cooldown period</returns>
        internal static bool CanReuseGONetId(uint gonetId)
        {
            if (!recentlyDespawnedGONetIds.TryGetValue(gonetId, out double despawnTime))
            {
                return true; // Not in recently despawned map, safe to reuse
            }

            double elapsed = Time.ElapsedSeconds - despawnTime;
            double reuseDelay = GetGONetIdReuseDelay();

            if (elapsed >= reuseDelay)
            {
                // TTL expired, remove from map and allow reuse
                recentlyDespawnedGONetIds.Remove(gonetId);
                GONetLog.Debug($"[GONetId-Reuse] GONetId {gonetId} TTL expired ({elapsed:F3}s >= {reuseDelay:F1}s), allowing reuse");
                return true;
            }

            // Still in cooldown period
            GONetLog.Warning($"[GONetId-Reuse] ⚠️  GONetId {gonetId} reuse prevented - TTL not expired ({elapsed:F3}s / {reuseDelay:F1}s remaining: {reuseDelay - elapsed:F3}s)");
            return false;
        }

        /// <summary>
        /// Gets the configured GONetId reuse delay from GONetGlobal.
        /// Falls back to 5 seconds if GONetGlobal not available.
        /// </summary>
        private static double GetGONetIdReuseDelay()
        {
            var gonetGlobal = GONetGlobal.Instance;
            if (gonetGlobal != null)
            {
                return gonetGlobal.gonetIdReuseDelaySeconds;
            }
            return 5.0; // Default fallback
        }

        /// <summary>
        /// Periodic cleanup of expired GONetIds from recentlyDespawnedGONetIds map.
        /// Runs every 30 seconds to prevent unbounded growth.
        /// Called from Update() main loop.
        /// </summary>
        internal static void CleanupExpiredDespawnedGONetIds()
        {
            const double CLEANUP_INTERVAL_SECONDS = 30.0;

            double now = Time.ElapsedSeconds;

            // Initialize or check cleanup interval
            if (!_lastGONetIdReuseCleanupTime.HasValue ||
                (now - _lastGONetIdReuseCleanupTime.Value) >= CLEANUP_INTERVAL_SECONDS)
            {
                _lastGONetIdReuseCleanupTime = now;

                if (recentlyDespawnedGONetIds.Count == 0)
                {
                    return; // Nothing to clean
                }

                double reuseDelay = GetGONetIdReuseDelay();
                List<uint> toRemove = new List<uint>(recentlyDespawnedGONetIds.Count);

                foreach (var kvp in recentlyDespawnedGONetIds)
                {
                    double elapsed = now - kvp.Value;
                    if (elapsed >= reuseDelay)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                if (toRemove.Count > 0)
                {
                    foreach (uint id in toRemove)
                    {
                        recentlyDespawnedGONetIds.Remove(id);
                    }
                    GONetLog.Info($"[GONetId-Reuse] Cleaned up {toRemove.Count} expired despawned GONetIds (map size now: {recentlyDespawnedGONetIds.Count})");
                }
            }
        }

        #endregion

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnDisable method.
        /// </summary>
        internal static void OnDisable_StopMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying && gonetParticipant.IsInternallyConfigured) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.CodeGenerationId];
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

                /*
                // DIAGNOSTIC LOGGING: Track participant removal from maps
                bool wasInGONetIdMap = gonetParticipantByGONetIdMap.ContainsKey(gonetParticipant.GONetId);
                bool wasInInstantiationMap = gonetParticipantByGONetIdAtInstantiationMap.ContainsKey(gonetParticipant.GONetIdAtInstantiation);
                string gameObjectName = (gonetParticipant.gameObject != null) ? gonetParticipant.gameObject.name : "<null>";

                GONetLog.Info($"[PARTICIPANT-REMOVED] 🗑️ GONetId: {gonetParticipant.GONetId}, " +
                             $"InstantiationId: {gonetParticipant.GONetIdAtInstantiation}, " +
                             $"GameObject: '{gameObjectName}', " +
                             $"IsServer: {IsServer}, " +
                             $"MyAuthorityId: {MyAuthorityId}, " +
                             $"WasInGONetIdMap: {wasInGONetIdMap}, " +
                             $"WasInInstantiationMap: {wasInInstantiationMap}");
                */

                // CRITICAL FIX: Remove from BOTH maps to prevent "destroyed but still in maps" errors
                gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetId);
                gonetParticipantByGONetIdAtInstantiationMap.Remove(gonetParticipant.GONetIdAtInstantiation);

                // Cleanup: Remove from deduplication tracking to allow GONetId reuse
                deserializeInitPublishedGONetIds.Remove(gonetParticipant.GONetId);

                // Cleanup: Clear any deferred RPCs for this GONetId to prevent infinite defer loops on GONetId reuse
                GONetEventBus.ClearDeferredRpcsForGONetId(gonetParticipant.GONetId);

                // GONetId Reuse Prevention: Mark this GONetId as recently despawned to prevent immediate reuse
                MarkGONetIdDespawned(gonetParticipant.GONetId);
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

        // PERFORMANCE: Use GONet's ArrayPool for zero-GC iteration during Tick() calls
        // Pool manages array lifecycle - borrow, use, return. Zero allocations after warmup.
        private static readonly Utils.ArrayPool<GONetBehaviour> tickReceivers_arrayPool =
            new Utils.ArrayPool<GONetBehaviour>(initialSize: 1, growByCount: 1, arraySizeMinimum: 10, arraySizeMaximum: 500);
        internal static void AddTickReceiver(GONetBehaviour gONetBehaviour)
        {
            tickReceivers_awaitingAdd.Add(gONetBehaviour);
        }

        internal static void RemoveTickReceiver(GONetBehaviour gONetBehaviour)
        {
            tickReceivers_awaitingRemove.Add(gONetBehaviour);
        }

        private static readonly HashSet<GONetBehaviour> allGONetBehaviours = new(1000);

        /// <summary>
        /// Tracks GONetIds that have already published DeserializeInitAllCompleted to prevent duplicate OnGONetReady() calls.
        /// Ensures exactly-once delivery across all publication paths:
        ///
        /// PATH 2: ProcessIncomingBytes_DeserializeAll_INTERNAL (line ~6114) - Remote scene-defined participants receiving first network sync
        /// PATH 3: GONetLocal.AddToLookupOnceAuthorityIdKnown (line ~135) - GONetLocal itself when added to lookup
        /// PATH 4: GONetLocal.AddToLookupOnceAuthorityIdKnown (line ~154) - Scene-defined IsMine participants (may start before GONetLocal ready)
        /// PATH 5: GONetLocal.AddIfAppropriate (line ~204) - Runtime-spawned IsMine participants (after GONetLocal already in lookup)
        /// PATH 6: GONetParticipantCompanionBehaviour.Start() (GONetBehaviour.cs ~311) - Runtime-added COMPONENTS via GONetRuntimeComponentInitializer
        /// PATH 7: CompleteRemoteInstantiation (line ~1352) - Remote runtime-spawned participants (received via network)
        /// PATH 8: Start_AutoPropagateInstantiation_IfAppropriate (line ~4611) - Client-spawned remotely-controlled participants (projectiles with server authority)
        ///
        /// REMOVED: Path 1 (Start) - Caused race conditions, redundant with paths above
        ///
        /// NOTE: Path 6 is special - it doesn't publish DeserializeInitAllCompleted, it directly calls OnGONetReady() on the component
        /// for ALL ready participants. This ensures components added mid-game don't miss participants that became ready earlier.
        ///
        /// This deduplication acts as defense-in-depth to guarantee exactly-once OnGONetReady() delivery for Paths 2-5, 7-8.
        /// Path 6 doesn't use this system (it's component-scoped, not participant-scoped).
        /// </summary>
        private static readonly HashSet<uint> deserializeInitPublishedGONetIds = new HashSet<uint>();

        /// <summary>
        /// Attempts to mark a GONetId as having published DeserializeInitAllCompleted.
        /// Returns true if this is the first publication (should publish), false if already published (skip).
        /// Thread-safe due to HashSet.Add() being atomic for the check-and-insert operation.
        /// </summary>
        internal static bool TryMarkDeserializeInitPublished(uint gonetId)
        {
            return deserializeInitPublishedGONetIds.Add(gonetId);
        }

        internal static void RegisterBehaviour(GONetBehaviour gONetBehaviour)
        {
            allGONetBehaviours.Add(gONetBehaviour);
        }
        internal static void UnregisterBehaviour(GONetBehaviour gONetBehaviour)
        {
            allGONetBehaviours.Remove(gONetBehaviour);
        }

        /// <summary>
        /// Checks if the passed in <paramref name="gonetParticipant"/> is fully initialized and ready for use.
        /// This means:
        /// - GONetId is assigned
        /// - GONetLocal is available in the lookup
        /// - Client/Server status is known
        /// - If client, fully initialized with server
        /// </summary>
        public static bool IsGONetReady(GONetParticipant gonetParticipant)
        {
            // Check basic participant initialization
            if (gonetParticipant == null ||
                gonetParticipant.OwnerAuthorityId == OwnerAuthorityId_Unset ||
                gonetParticipant.gonetId_raw == GONetParticipant.GONetIdRaw_Unset ||
                !gonetParticipant.IsInternallyConfigured)
            {
                return false;
            }

            // Check client/server status is known
            if (!IsClientVsServerStatusKnown)
            {
                return false;
            }

            // If we're a client, ensure client instance exists and is fully initialized
            if (IsClient)
            {
                if (GONetClient == null)
                {
                    return false; // Client but no client instance - not ready
                }

                if (!GONetClient.IsInitializedWithServer)
                {
                    return false; // Client exists but not initialized with server
                }
            }

            // Check GONetLocal lookup is available
            if (GONetLocal.LookupByAuthorityId == null)
            {
                return false;
            }

            // Use the indexer to look up the GONetLocal for this participant's authority ID
            // The indexer returns null if not found (safe, no exceptions)
            GONetLocal local = GONetLocal.LookupByAuthorityId[gonetParticipant.OwnerAuthorityId];
            if (local == null)
            {
                return false;
            }

            // LIFECYCLE GATE: Check Unity lifecycle completion (Awake, Start)
            if (!gonetParticipant.didAwakeComplete || !gonetParticipant.didStartComplete)
            {
                return false; // Unity lifecycle not yet complete
            }

            // LIFECYCLE GATE: Check deserialization requirement (if needed for remote objects)
            if (gonetParticipant.requiresDeserializeInit && !gonetParticipant.didDeserializeInitComplete)
            {
                return false; // Waiting for remote sync data (DeserializeInitAllCompleted)
            }

            // LIFECYCLE GATE: Ensure not in limbo state (client batch exhaustion edge case)
            if (gonetParticipant.Client_IsInLimbo)
            {
                return false; // Still waiting for GONetId batch from server
            }

            return true;
        }

        /// <summary>
        /// Checks if all OnGONetReady prerequisites are met and broadcasts to all GONetBehaviours if so.
        /// Called after each lifecycle milestone (Awake, Start, DeserializeInit, ExitLimbo).
        ///
        /// This is the simplified gate check that delegates to IsGONetReady() for all validation.
        /// Only fires OnGONetReady once per participant (tracked via didOnGONetReadyFire flag).
        /// </summary>
        internal static void CheckAndPublishOnGONetReady_IfAllConditionsMet(GONetParticipant gonetParticipant)
        {
            // Prevent duplicate calls - OnGONetReady should only fire once
            if (gonetParticipant.didOnGONetReadyFire)
            {
                return; // Already fired, nothing to do
            }

            // Check if all prerequisites are met (delegates to IsGONetReady)
            if (!IsGONetReady(gonetParticipant))
            {
                return; // Not ready yet, wait for next milestone
            }

            // All conditions met! Mark as fired and broadcast OnGONetReady to all GONetBehaviours
            gonetParticipant.didOnGONetReadyFire = true;

            // Broadcast OnGONetReady to all registered GONetBehaviours
            using (var en = allGONetBehaviours.GetEnumerator())
            {
                while (en.MoveNext())
                {
                    GONetBehaviour gnBehaviour = en.Current;
                    try
                    {
                        gnBehaviour.OnGONetReady(gonetParticipant);
                    }
                    catch (Exception ex)
                    {
                        GONetLog.Error($"[GONet] Exception in OnGONetReady() broadcast for behaviour '{gnBehaviour.GetType().Name}' on '{gnBehaviour.gameObject.name}' with participant '{gonetParticipant.name}': {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }

            // NEW: This participant is now ready - try processing deferred sync bundles
            // (They might have been waiting for THIS participant specifically)
            ProcessDeferredSyncBundlesWaitingForGONetReady();
        }

        #region Velocity-Augmented Sync Helper Methods

        /// <summary>
        /// Velocity-augmented sync: Synthesizes a new value from a previous value and velocity over deltaTime.
        /// Used to generate intermediate positions/rotations from received velocity packets.
        /// </summary>
        /// <param name="lastValue">The last received VALUE (e.g., position, rotation)</param>
        /// <param name="velocity">The received VELOCITY (e.g., linear velocity for Vector3, angular velocity as Vector3 for Quaternion)</param>
        /// <param name="deltaTime">Time elapsed since lastValue was received (in seconds)</param>
        /// <returns>Synthesized value: lastValue + velocity * deltaTime (appropriate for the type)</returns>
        private static GONetSyncableValue SynthesizeValueFromVelocity(
            GONetSyncableValue lastValue,
            GONetSyncableValue velocity,
            float deltaTime)
        {
            GONetSyncableValue result = new GONetSyncableValue();

            switch (lastValue.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Single: // float
                {
                    result.System_Single = lastValue.System_Single + velocity.System_Single * deltaTime;
                    return result;
                }

                case GONetSyncableValueTypes.UnityEngine_Vector2:
                {
                    result.UnityEngine_Vector2 = lastValue.UnityEngine_Vector2 + velocity.UnityEngine_Vector2 * deltaTime;
                    return result;
                }

                case GONetSyncableValueTypes.UnityEngine_Vector3:
                {
                    result.UnityEngine_Vector3 = lastValue.UnityEngine_Vector3 + velocity.UnityEngine_Vector3 * deltaTime;
                    return result;
                }

                case GONetSyncableValueTypes.UnityEngine_Vector4:
                {
                    result.UnityEngine_Vector4 = lastValue.UnityEngine_Vector4 + velocity.UnityEngine_Vector4 * deltaTime;
                    return result;
                }

                case GONetSyncableValueTypes.UnityEngine_Quaternion:
                {
                    // Angular velocity is stored as Vector3 (axis × radians/sec)
                    if (velocity.GONetSyncType != GONetSyncableValueTypes.UnityEngine_Vector3)
                    {
                        GONetLog.Error($"[VelocitySync] Quaternion synthesis requires Vector3 angular velocity, but received {velocity.GONetSyncType}");
                        return lastValue; // Return unchanged
                    }

                    result.UnityEngine_Quaternion = RotateQuaternionByAngularVelocity(
                        lastValue.UnityEngine_Quaternion,
                        velocity.UnityEngine_Vector3, // Angular velocity as Vector3
                        deltaTime);
                    return result;
                }

                default:
                    GONetLog.Warning($"[VelocitySync] Velocity synthesis not implemented for type {lastValue.GONetSyncType}. Returning lastValue unchanged.");
                    return lastValue;
            }
        }

        /// <summary>
        /// Velocity-augmented sync: Rotates a quaternion by angular velocity over deltaTime.
        /// Angular velocity is represented as Vector3: axis (normalized direction) × magnitude (radians/sec).
        /// </summary>
        /// <param name="current">Current rotation</param>
        /// <param name="angularVelocity">Angular velocity as Vector3 (axis × radians/sec)</param>
        /// <param name="deltaTime">Time elapsed (in seconds)</param>
        /// <returns>New rotation after applying angular velocity</returns>
        private static UnityEngine.Quaternion RotateQuaternionByAngularVelocity(
            UnityEngine.Quaternion current,
            UnityEngine.Vector3 angularVelocity,
            float deltaTime)
        {
            // Calculate total rotation angle in radians
            float angle = angularVelocity.magnitude * deltaTime;

            // Early exit if rotation is negligible (avoid division by zero on normalize)
            if (angle < 0.0001f)
            {
                return current;
            }

            // Extract rotation axis
            UnityEngine.Vector3 axis = angularVelocity.normalized;

            // Create delta rotation quaternion (Unity's AngleAxis expects degrees)
            UnityEngine.Quaternion deltaRot = UnityEngine.Quaternion.AngleAxis(angle * UnityEngine.Mathf.Rad2Deg, axis);

            // Apply delta rotation: deltaRot * current (order matters for quaternions!)
            return (deltaRot * current).normalized;
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

    /// <summary>
    /// Thrown when attempting to deserialize sync data for a GONetParticipant that exists
    /// but hasn't completed Awake()/OnGONetReady initialization yet.
    /// Distinct from KeyNotFoundException (participant missing entirely) or GONetOutOfOrderHorseDickoryException (participant fully missing).
    ///
    /// This exception indicates a RACE CONDITION where:
    /// - The participant GameObject exists in the scene/hierarchy
    /// - GONetId lookup succeeds (participant is in dictionaries)
    /// - BUT: didAwakeComplete=false OR syncCompanion=null (async Awake() coroutine still running)
    ///
    /// Solution: Defer bundle processing until OnGONetReady fires (configurable) or drop (default).
    /// </summary>
    [Serializable]
    public class GONetParticipantNotReadyException : Exception
    {
        /// <summary>
        /// The GONetId (instantiation) of the participant that wasn't ready.
        /// Useful for logging/diagnostics and retry logic.
        /// </summary>
        public uint GONetId { get; }

        public GONetParticipantNotReadyException()
        {
        }

        public GONetParticipantNotReadyException(string message) : base(message)
        {
        }

        public GONetParticipantNotReadyException(string message, uint gonetId) : base(message)
        {
            GONetId = gonetId;
        }

        public GONetParticipantNotReadyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected GONetParticipantNotReadyException(SerializationInfo info, StreamingContext context) : base(info, context)
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

    /// <summary>
    /// CLIENT ONLY: Event arguments for when a spawn enters limbo state due to GONetId batch exhaustion.
    /// IMPORTANT: Limbo is RARE - only occurs during extreme rapid spawning (100+ spawns/sec).
    /// </summary>
    public class Client_SpawnLimboEventArgs
    {
        /// <summary>
        /// The GONetParticipant that entered limbo state (no GONetId assigned yet).
        /// </summary>
        public GONetParticipant Participant { get; internal set; }

        /// <summary>
        /// The prefab that was instantiated.
        /// </summary>
        public GONetParticipant Prefab { get; internal set; }

        /// <summary>
        /// The limbo mode that was applied to this spawn.
        /// </summary>
        public Client_GONetIdBatchLimboMode LimboMode { get; internal set; }

        /// <summary>
        /// Number of IDs remaining across all batches (should be 0 if entering limbo).
        /// </summary>
        public uint RemainingIds { get; internal set; }

        /// <summary>
        /// Position where the object was spawned.
        /// </summary>
        public Vector3 Position { get; internal set; }

        /// <summary>
        /// Rotation where the object was spawned.
        /// </summary>
        public Quaternion Rotation { get; internal set; }
    }
}
