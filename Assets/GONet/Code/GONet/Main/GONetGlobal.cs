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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet
{
    /// <summary>
    /// Very important, in fact required, that this get added to one and only one <see cref="GameObject"/> in the first scene loaded in your game.
    /// This is where all the links into Unity life cycle stuffs start for GONet at large.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    [RequireComponent(typeof(GONetParticipant))]
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public sealed class GONetGlobal : GONetParticipantCompanionBehaviour
    {
        /// <summary>
        /// Singleton instance to prevent duplicate GONetGlobal instances across scenes.
        /// </summary>
        private static GONetGlobal instance;

        /// <summary>
        /// Public accessor for the singleton instance.
        /// Use this instead of FindObjectOfType to ensure you get the persistent instance, not a duplicate that's about to be destroyed.
        /// </summary>
        public static GONetGlobal Instance => instance;

        #region TODO this should be configurable/set elsewhere potentially AFTER loading up and depending on other factors like match making etc...

        //public string serverIP;

        //public int serverPort;

        [Tooltip("***IMPORTANT: When Awake() is called, this value will be locked in place, whereas any adjustments at runtime will yield nothing.\nWhen a Sync Settings Profile or [GONetAutoMagicalSync] setting for " + nameof(GONetAutoMagicalSyncSettings_ProfileTemplate.ShouldBlendBetweenValuesReceived) + " is set to true, this value is used throughout GONet for the length of time in milliseconds to buffer up received sync values from other machines in the network before applying the data locally.\n*When 0, everything will have to be predicted client-side (e.g., extrapolation) since the data received is always old.\n*Non-zero positive values will yield much more accurate (although out of date) data assuming the buffer lead time is large enough to account for lag (network/processing).")]
        [Range(0, 1000)]
        public int valueBlendingBufferLeadTimeMilliseconds = (int)TimeSpan.FromSeconds(GONetMain.BLENDING_BUFFER_LEAD_SECONDS_DEFAULT).TotalMilliseconds;


        #endregion

        [Tooltip("GONet requires GONetGlobal to have a prefab for GONetLocal set here.  Each machine in the network game will instantiate one instance of this prefab.")]
        [SerializeField]
        internal GONetLocal gonetLocalPrefab;

        [Tooltip("Enable automatic client/server role detection based on port availability.\n\n" +
                "When enabled:\n" +
                "• First instance (port free) → Starts as SERVER\n" +
                "• Additional instances (port occupied) → Start as CLIENTS\n" +
                "• Command line args (-server/-client) always override auto-detection\n\n" +
                "This is ideal for local development and testing, eliminating manual role selection.\n\n" +
                "When disabled:\n" +
                "• You must explicitly specify -server or -client via command line\n" +
                "• Or use keyboard shortcuts (Ctrl+Alt+S for server, Ctrl+Alt+C for client)\n\n" +
                "Default: Enabled (recommended for development)")]
        public bool enableAutoRoleDetection = true;

        [Tooltip("Number of GONetIds allocated per batch for client-spawned objects.\n\n" +
                "IMPORTANT: Limbo mode only triggers when client exhausts ALL batch IDs (RARE edge case).\n\n" +
                "• Higher values (500-1000): Better for rapid spawning scenarios (100+ spawns/sec)\n" +
                "• Lower values (100-200): Better for typical gameplay (reduces server memory overhead)\n\n" +
                "Default: 200 IDs per batch (suitable for most games)\n" +
                "Range: 100-1000 IDs per batch\n\n" +
                "Client automatically requests new batch when 50% remaining.")]
        [Range(100, 1000)]
        public int client_GONetIdBatchSize = 200;

        [Header("Congestion Management - Adaptive Scaling")]
        [Tooltip("⭐ ADAPTIVE POOL SIZING (Recommended)\n\n" +
                "When TRUE (default): Pool size automatically scales based on network demand.\n" +
                "• Scales UP when utilization exceeds 75% (prevents drops)\n" +
                "• Scales DOWN when utilization stays below 25% (conserves memory)\n" +
                "• Warns aggressively when memory/bandwidth limits approached\n" +
                "• Respects maxPacketsPerTick as absolute ceiling (safety cap)\n\n" +
                "When FALSE: Manual control - pool fixed at maxPacketsPerTick.\n" +
                "• For experts who need precise bandwidth control\n" +
                "• For bandwidth-constrained scenarios (mobile, low-end servers)\n\n" +
                "GONet Philosophy: \"Do what the user wants, warn when risky\"\n" +
                "Default: TRUE (auto-scale for best user experience)")]
        public bool enableAdaptivePoolScaling = true;

        [Tooltip("Starting pool size for adaptive scaling (when enableAdaptivePoolScaling=true).\n\n" +
                "Adaptive scaling will grow/shrink from this baseline:\n" +
                "• Grows when utilization >75% (up to maxPacketsPerTick ceiling)\n" +
                "• Shrinks when utilization <25% (down to this minimum)\n\n" +
                "CONFIGURATION GUIDELINES:\n" +
                "• Small Co-op (2-8 players): 500\n" +
                "• Battle Royale (50-100 players): 1500\n" +
                "• MMO (100+ players): 3000\n\n" +
                "NOTE: When enableAdaptivePoolScaling=false, this value is ignored and\n" +
                "maxPacketsPerTick is used as a fixed pool size.\n\n" +
                "Default: 1000 (suitable for most games)\n" +
                "Range: 100-10000")]
        [Range(100, 10000)]
        public int adaptivePoolBaselineSize = 1000;

        [Tooltip("ABSOLUTE MAXIMUM pool size (safety ceiling for adaptive scaling).\n\n" +
                "When enableAdaptivePoolScaling=TRUE:\n" +
                "• Pool can never grow beyond this limit (prevents runaway memory)\n" +
                "• Aggressive warnings logged when approaching this ceiling\n" +
                "• Recommended: 10x your baseline (e.g., baseline=1000 → max=10000)\n\n" +
                "When enableAdaptivePoolScaling=FALSE:\n" +
                "• This IS the fixed pool size (no scaling occurs)\n\n" +
                "EXPERT OVERRIDE: Lower this to cap bandwidth in constrained scenarios:\n" +
                "• Mobile clients with limited bandwidth\n" +
                "• Low-end servers with strict memory budgets\n" +
                "• Development/testing with artificial constraints\n\n" +
                "⚠️ WARNING: Setting this TOO LOW will cause packet drops!\n" +
                "SYMPTOMS: Objects stuck at spawn, high drop rates, 'Pool exhausted' errors\n\n" +
                "Default: 20000 (generous ceiling for auto-scaling)\n" +
                "Range: 100-100000")]
        [Range(100, 100000)]
        public int maxPacketsPerTick = 20000;

        [Tooltip("Start dropping unreliable packets when pool utilization exceeds this percentage.\n\n" +
                "Flow control threshold to prevent packet pool exhaustion.\n" +
                "When borrowed packet count exceeds (maxPacketsPerTick × unreliableDropThreshold),\n" +
                "new unreliable packets are dropped to preserve pool capacity for reliable packets.\n\n" +
                "TUNING GUIDANCE:\n" +
                "• Higher (0.95-0.99): More aggressive - allows pool to fill nearly completely\n" +
                "  Use when: Reliable packets are rare, mostly unreliable traffic\n" +
                "• Lower (0.80-0.90): More conservative - drops unreliable earlier\n" +
                "  Use when: Mix of reliable/unreliable, want buffer for reliable messages\n\n" +
                "TRADE-OFFS:\n" +
                "• Too high: Reliable packets may fail if pool suddenly fills\n" +
                "• Too low: Unnecessary unreliable packet drops under normal load\n\n" +
                "Default: 0.90 (drop unreliable when 90% pool utilization)\n" +
                "Range: 0.50-0.99 (50%-99%)")]
        [Range(0.5f, 0.99f)]
        public float unreliableDropThreshold = 0.90f;

        [Tooltip("Enable detailed congestion logging for debugging network bottlenecks.\n\n" +
                "When enabled, logs packet drop events with actionable diagnostics:\n" +
                "• Drop rate (packets dropped / total packets)\n" +
                "• Pool utilization percentage\n" +
                "• Channel causing drops (AutoMagicalSync, TimeSync, etc.)\n" +
                "• Recommended solutions (increase pool size, reduce sync frequency, etc.)\n\n" +
                "WHEN TO ENABLE:\n" +
                "• Investigating objects stuck at spawn position\n" +
                "• Tuning maxPacketsPerTick for your game\n" +
                "• Debugging high packet drop rates\n\n" +
                "PERFORMANCE IMPACT:\n" +
                "• Minimal - only logs when drops occur\n" +
                "• Throttled logging (batches drops to avoid spam)\n\n" +
                "Default: Enabled (helps identify congestion issues)")]
        public bool enableCongestionLogging = true;

        [Header("Sync Bundle Handling - OnGONetReady Race Condition")]
        [Tooltip("DEFAULT: false (DROP bundles if participant not ready - industry standard).\n\n" +
                "When enabled, reliable sync bundles will be queued and retried once if participant hasn't completed OnGONetReady.\n\n" +
                "WHEN TO ENABLE:\n" +
                "• Turn-based games where every state change must be received\n" +
                "• Zero data loss is critical (e.g., ownership changes, inventory updates)\n\n" +
                "WHEN TO LEAVE DISABLED (DEFAULT):\n" +
                "• Action games with high-frequency updates (positions, rotations)\n" +
                "• Unreliable bundles are ALWAYS dropped regardless of this setting\n" +
                "• Authority re-sends updated state 30-60 times/sec (auto-recovery)\n" +
                "• Value blending smooths over 1-2 dropped frames\n\n" +
                "AUTHORITY-AGNOSTIC:\n" +
                "• Works on clients AND servers receiving sync data\n" +
                "• Handles ALL network topologies (client→server, server→client, peer-to-peer)\n\n" +
                "Default: false (matches industry standards - FishNet, Mirror)")]
        public bool deferSyncBundlesWaitingForGONetReady = false;

        [Tooltip("Maximum sync bundles to queue per receiver while waiting for participants to complete OnGONetReady.\n\n" +
                "TYPICAL VALUES:\n" +
                "• Awake() completes in 1-2 frames typically\n" +
                "• At 200 spawns/sec, only 6-12 bundles queued\n" +
                "• Queue size of 100 handles extreme burst scenarios\n\n" +
                "FIFO DROP POLICY:\n" +
                "• When queue fills, oldest bundles are dropped to make room\n" +
                "• Warning logged prompting you to increase limit or disable deferral\n\n" +
                "Only used when deferSyncBundlesWaitingForGONetReady=true.\n\n" +
                "Default: 100 bundles\n" +
                "Range: 10-500")]
        [Range(10, 500)]
        public int maxSyncBundlesWaitingForGONetReady = 100;

        [Tooltip("Maximum bundles to process per OnGONetReady callback (prevents frame stutter during burst processing).\n\n" +
                "PERFORMANCE RATIONALE:\n" +
                "• OnGONetReady fires for EVERY participant that becomes ready\n" +
                "• Processing all queued bundles at once would cause frame stutter during mass spawns\n" +
                "• Remaining bundles will be processed in subsequent OnGONetReady callbacks\n\n" +
                "TUNING:\n" +
                "• Higher (20-50): Faster queue drainage, but potential frame spikes\n" +
                "• Lower (5-10): Smoother frame times, but slower queue drainage\n\n" +
                "Only used when deferSyncBundlesWaitingForGONetReady=true.\n\n" +
                "Default: 10 bundles/callback\n" +
                "Range: 1-50")]
        [Range(1, 50)]
        public int maxBundlesProcessedPerGONetReadyCallback = 10;

        [Header("GONetId Reuse Protection")]
        [Tooltip("Time in seconds to wait after an object despawns before allowing its GONetId to be reused.\n\n" +
                "PURPOSE:\n" +
                "Prevents GONetId reuse while despawn messages are still in flight across the network.\n" +
                "If a GONetId is reused too quickly, despawn messages for the old object may arrive\n" +
                "after a new object has already claimed that ID, causing the wrong object to despawn.\n\n" +
                "RECOMMENDED VALUES:\n" +
                "• LAN (low latency): 2-3 seconds\n" +
                "• Internet (normal): 5 seconds (default)\n" +
                "• High latency/packet loss: 10-15 seconds\n\n" +
                "HOW IT WORKS:\n" +
                "• When an object despawns, its GONetId is marked with a timestamp\n" +
                "• The ID cannot be reused until this delay has elapsed\n" +
                "• Ensures all despawn messages have been delivered and processed\n" +
                "• Based on network RTT + safety margin for packet reordering\n\n" +
                "SYMPTOMS OF TOO-LOW VALUE:\n" +
                "• 'Despawn event received but no matching GONetParticipant found' warnings\n" +
                "• Objects stuck on client after server despawns them\n" +
                "• Wrong objects getting despawned (premature destroys)\n\n" +
                "Default: 5 seconds (handles typical internet latency)\n" +
                "Range: 1-30 seconds")]
        [Range(1f, 30f)]
        public float gonetIdReuseDelaySeconds = 5f;

        [Header("Reliable Message Queue")]
        [Tooltip("Maximum reliable message queue size before messages are dropped (lower-level transport setting).\n\n" +
                "PURPOSE:\n" +
                "When reliable messages are sent faster than they can be transmitted and acknowledged,\n" +
                "they queue up waiting for sendBuffer space. This setting prevents unbounded memory growth.\n\n" +
                "WHEN EXHAUSTION OCCURS:\n" +
                "• [RELIABLE-QUEUE-EXHAUSTION] error will be logged\n" +
                "• Message will be DROPPED (spawn events, RPCs, etc. will fail silently)\n" +
                "• This is EXTREMELY RARE - requires sustained burst + high packet loss + slow ACKs\n\n" +
                "COMMON CAUSES:\n" +
                "• Sustained 100+ messages/sec + high packet loss (>10%)\n" +
                "• Very high RTT (>250ms) with slow ACKs\n" +
                "• SendBuffer full (1024 capacity) AND continued high message rate\n\n" +
                "RECOMMENDED VALUES:\n" +
                "• LAN/Low latency: 1000-2000 (default: 2000)\n" +
                "• Internet/Normal latency: 2000-5000\n" +
                "• High latency/packet loss: 5000-10000\n\n" +
                "SYMPTOMS OF EXHAUSTION:\n" +
                "• Spawn events never propagate (objects appear only on one client)\n" +
                "• RPCs fail to deliver\n" +
                "• [RELIABLE-QUEUE-EXHAUSTION] errors in logs\n\n" +
                "Default: 2000 messages\n" +
                "Range: 1000-10000")]
        [Range(1000, 10000)]
        public int maxReliableMessageQueueSize = 2000;

        [Tooltip("GONet needs to know immediately on start of the program whether or not this game instance is a client or the server in order to initialize properly.  When using the provided Start_CLIENT.bat and Start_SERVER.bat files with builds, that will be taken care of for you.  However, when using the editor as a client (connecting to a server build), setting this flag to true is the only way for GONet to know immediately this game instance is a client.  If you run in the editor and see errors in the log on start up (e.g., \"[Log:Error] (Thread:1) (29 Dec 2019 20:24:06.970) (frame:-1s) (GONetEventBus handler error) Event Type: GONet.GONetParticipantStartedEvent\"), then it is likely because you are running as a client and this flag is not set to true.")]
        public bool shouldAttemptAutoStartAsClient = true;

        /// <summary>
        /// NOTE: GONetGlobal contains RUNTIME settings that affect gameplay behavior.
        /// For EDITOR-ONLY settings (code generation, asset processing, etc.), see GONetProjectSettings.
        /// </summary>
        [Header("Runtime Debug Settings")]
        [Tooltip("Enable comprehensive message flow logging for debugging network issues.\n\n" +
                "When enabled, logs every send/receive/process event to: gonet-MessageFlow-YYYY-MM-DD.log\n\n" +
                "⚠️ WARNING: Generates large log files. Only enable for targeted debugging sessions.\n\n" +
                "Logs include:\n" +
                "• [MSG-SEND] - When messages are sent (timestamp, target, channel, bytes)\n" +
                "• [MSG-RECV] - When messages arrive (timestamp, source, latency)\n" +
                "• [MSG-PROC] - When OnGONetReady events are broadcast\n\n" +
                "The MessageFlow logging profile is automatically registered with:\n" +
                "• Separate file output (gonet-MessageFlow-YYYY-MM-DD.log)\n" +
                "• No stack traces (clean, readable output)\n" +
                "• Info level and above\n\n" +
                "Default: Disabled")]
        public bool enableMessageFlowLogging = false;

        private readonly List<GONetParticipant> enabledGONetParticipants = new List<GONetParticipant>(1000);
        /// <summary>
        /// <para>A convenient collection of all the <see cref="GONetParticipant"/> instances that are currently enabled no matter what the value of <see cref="GONetParticipant.OwnerAuthorityId"/> value is.</para>
        /// <para>Elements are added here once Start() was called on the <see cref="GONetParticipant"/> and removed once OnDisable() is called.</para>
        /// <para>Do NOT attempt to modify this collection as to avoid creating issues for yourself/others.</para>
        /// </summary>
        public IEnumerable<GONetParticipant> EnabledGONetParticipants => enabledGONetParticipants;

        public static readonly string ServerIPAddress_Default = GONetMain.isServerOverride ? "0.0.0.0" : "127.0.0.1";
        public const int ServerPort_Default = 40000;

        public delegate void ServerConnectionInfoChanged(string serverIP, int serverPort);
        public static event ServerConnectionInfoChanged ActualServerConnectionInfoSet;

        public static bool AreAllServerConnectionInfoActualsSet => !string.IsNullOrWhiteSpace(serverIPAddress_Actual) && serverPort_Actual != -1;

        [SerializeField]
        [Tooltip("Server connection ip or hostname.  If not provided, GONetGlobal.ServerIPAddress_Default is used instead.")]
        private string server;
        [SerializeField]
        [Tooltip("Server connection port.  If not provided, GONetGlobal.ServerPort_Default is used instead.")]
        private int serverPort;

        /// <summary>
        /// DO NOT SET THIS OUTSIDE GONET INTERNAL CODE!
        /// </summary>
        internal static string serverIPAddress_Actual;
        /// <summary>
        /// IMPORTANT: This will be NULL/empty when the actual serer ip address is not known!
        /// </summary>
        public static string ServerIPAddress_Actual { get => serverIPAddress_Actual; internal set { serverIPAddress_Actual = value; FireEventIfBothActualsSet(); } }

        /// <summary>
        /// DO NOT SET THIS OUTSIDE GONET INTERNAL CODE!
        /// </summary>
        internal static int serverPort_Actual = -1;
        /// <summary>
        /// IMPORTANT: This will be -1 when the actual server ip address is not known!
        /// </summary>
        public static int ServerPort_Actual { get => serverPort_Actual; internal set { serverPort_Actual = value; FireEventIfBothActualsSet(); } }

        public static IPEndPoint ServerP2pEndPoint { get; internal set; }

        private static void FireEventIfBothActualsSet()
        {
            if (AreAllServerConnectionInfoActualsSet)
            {
                ActualServerConnectionInfoSet?.Invoke(serverIPAddress_Actual, serverPort_Actual);
            }
        }

        protected override void Awake()
        {
            // PHASE 2 FIX: Force Application.runInBackground = true for multiplayer servers/clients
            // This ensures servers keep processing network traffic even when Unity window loses focus
            // Critical for dedicated servers and multi-instance local testing (editor + builds)
            // Without this, unfocused instances pause and cause timeouts/disconnects
            Application.runInBackground = true;

            // Self-destroying singleton pattern: Prevent duplicate GONetGlobal instances
            if (instance != null && instance != this)
            {
                GONetLog.Warning($"[GONetGlobal] Duplicate GONetGlobal detected in scene '{gameObject.scene.name}'. Destroying duplicate immediately to prevent any processing.");

                // CRITICAL: Use DestroyImmediate (not Destroy) to prevent any further processing on this duplicate
                // This ensures GONetParticipant and other components don't try to initialize or process on a duplicate that shouldn't exist
                DestroyImmediate(gameObject);
                return;
            }
            instance = this;

            // Register the MessageFlow logging profile for comprehensive message flow debugging
            // This profile writes to a separate file (gonet-MessageFlow-YYYY-MM-DD.log) with no stack traces
            GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(
                GONetMain.MessageFlowLoggingProfile,
                outputToSeparateFile: true,
                includeStackTraces: false,  // CRITICAL: Prevents stack trace spam
                minimumLogLevel: GONetLog.LogLevel.Info));

            // Enable message flow logging if inspector checkbox is set
            GONetMain.EnableMessageFlowLogging = enableMessageFlowLogging;
            if (enableMessageFlowLogging)
            {
                GONetLog.Info($"[GONetGlobal] Message flow logging ENABLED - output to: gonet-MessageFlow-{System.DateTime.Now:yyyy-MM-dd}.log");
            }

            if (gonetLocalPrefab == null)
            {
                Debug.LogError("Sorry.  We have to exit the application.  GONet requires GONetGlobal to have a prefab for GONetLocal set in the field named " + nameof(gonetLocalPrefab));
#if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
            }

            if (!string.IsNullOrWhiteSpace(server))
            {
                serverIPAddress_Actual = server;
            }
            if (serverPort != default && serverPort > 0)
            {
                serverPort_Actual = serverPort;
            }
            ServerIPAddress_Actual = string.IsNullOrWhiteSpace(serverIPAddress_Actual) ? ServerIPAddress_Default : serverIPAddress_Actual;
            ServerPort_Actual = (serverPort_Actual == default || serverPort_Actual < 0) ? ServerPort_Default : serverPort_Actual;

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;

            GONetMain.InitOnUnityMainThread(this, gameObject.GetComponent<GONetSessionContext>(), valueBlendingBufferLeadTimeMilliseconds);

            base.Awake(); // YUK: code smell...having to break OO protocol here and call base here as it needs to come AFTER the init stuff is done in GONetMain.InitOnUnityMainThread() and unity main thread identified or exceptions will be thrown in base.Awake() when subscribing

            // IMPORTANT: Cache design time metadata BEFORE any other initialization
            // This ensures metadata is available when GONetParticipants start their Awake() calls
            StartCoroutine(CacheDesignTimeMetadata_ThenContinueInit());

            enabledGONetParticipants.Clear();

            // Create persistent status UI
            CreateStatusUI();

            if (shouldAttemptAutoStartAsClient)
            {
                Editor_AttemptStartAsClientIfAppropriate(ServerIPAddress_Actual, ServerPort_Actual);
            }
        }

        private void CreateStatusUI()
        {
            // Add GONetStatusUI component if it doesn't already exist
            if (GetComponent<GONet.Sample.GONetStatusUI>() == null)
            {
                gameObject.AddComponent<GONet.Sample.GONetStatusUI>();
            }
        }

        protected override void OnDestroy()
        {
            // CRITICAL: Clear singleton reference when this instance is destroyed
            // This is essential for Unity Editor play mode to prevent stale references
            // that cause non-deterministic behavior between play sessions
            if (instance == this)
            {
                instance = null;
                GONetLog.Debug("[GONetGlobal] Cleared singleton instance reference on destroy");
            }

            // Unsubscribe from scene events to prevent memory leaks
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

            // NOTE: We do NOT clear design-time metadata caches here as they contain
            // critical build-time information needed to detect changes between builds.
            // The 98 vs 51 log difference is acceptable - it's not a bug, just different
            // code paths on first load vs cached load.

            base.OnDestroy();
        }

        public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
        {
            base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

            if (isServer)
            {
                GONetMain.gonetServer.ClientDisconnected += Server_ClientDisconnected;
            }
        }

        private void Server_ClientDisconnected(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            Server_MakeDoublySureAllClientOwnedGNPsDestroyed(gonetConnection_ServerToClient.OwnerAuthorityId);
        }

        private void Server_MakeDoublySureAllClientOwnedGNPsDestroyed(ushort ownerAuthorityId)
        {
            for (int i = enabledGONetParticipants.Count - 1; i >= 0; --i)
            {
                GONetParticipant enabledGNP = enabledGONetParticipants[i];
                if (enabledGNP.OwnerAuthorityId == ownerAuthorityId && enabledGNP && enabledGNP.gameObject)
                {
                    Destroy(enabledGNP.gameObject);
                }
            }
        }

        public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantEnabled(gonetParticipant);

            AddIfAppropriate(gonetParticipant);
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            AddIfAppropriate(gonetParticipant);

            ushort toBeRemotelyControlledByAuthorityId;
            if (GONetMain.IsServer && GONetSpawnSupport_Runtime.Server_TryGetMarkToBeRemotelyControlledBy(gonetParticipant, out toBeRemotelyControlledByAuthorityId))
            {
                GONetMain.Server_AssumeAuthorityOver(gonetParticipant);

                // IMPORTANT: only now, after assuming authority, will the following change actually get propogated to the non-owners (i.e., since only the owner can make a auto-propogated change)
                gonetParticipant.RemotelyControlledByAuthorityId = toBeRemotelyControlledByAuthorityId;

                GONetSpawnSupport_Runtime.Server_UnmarkToBeRemotelyControlled_ProcessingComplete(gonetParticipant);
            }
        }

        private void AddIfAppropriate(GONetParticipant gonetParticipant)
        {
            if (!enabledGONetParticipants.Contains(gonetParticipant)) // may have already been added elsewhere
            {
                enabledGONetParticipants.Add(gonetParticipant);
            }
        }

        public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
        {
            enabledGONetParticipants.Remove(gonetParticipant); // regardless of whether or not it was present before this call, it will not be present afterward
        }

        private void Editor_AttemptStartAsClientIfAppropriate(string serverIP, int serverPort)
        {
            if (!Application.isEditor) return;

            if (!AreAllServerConnectionInfoActualsSet)
            {
                ActualServerConnectionInfoSet += Editor_AttemptStartAsClientIfAppropriate;
                return;
            }

            ActualServerConnectionInfoSet -= Editor_AttemptStartAsClientIfAppropriate;

            // Check if auto-detection is enabled
            if (!enableAutoRoleDetection)
            {
                GONetLog.Info("[GONetGlobal] Auto role detection is disabled. Use command line args (-server/-client) or keyboard shortcuts (Ctrl+Alt+S/C) to start manually.");
                return;
            }

            // Auto-detect whether to start as server or client based on port availability
            // Only do this if we're not already a client or server
            if (!GONetMain.IsClient && !GONetMain.IsServer)
            {
                var sampleSpawner = GetComponent<GONetSampleSpawner>();
                if (sampleSpawner)
                {
                    bool isServerRemote = !NetworkUtils.IsIPAddressOnLocalMachine(serverIP);
                    bool isPortOccupied = NetworkUtils.IsLocalPortListening(serverPort);

                    if (isServerRemote || isPortOccupied)
                    {
                        // Server is running remotely or port is occupied locally → start as client
                        sampleSpawner.InstantiateClientIfNotAlready();
                        GONetLog.Info($"[GONetGlobal] Editor auto-detection: Starting as CLIENT (server at {serverIP}:{serverPort})");
                    }
                    else
                    {
                        // Port is free and server would be local → start as server
                        sampleSpawner.InstantiateServerIfNotAlready();
                        GONetLog.Info($"[GONetGlobal] Editor auto-detection: Port {serverPort} is free, starting as SERVER");
                    }
                }
                else
                {
                    const string UNABLE = "Unable to honor your setting of true on ";
                    const string BECAUSE = " because we could not find ";
                    const string ATTACHED = " attached to this GameObject, which is required to automatically start in this manner.";
                    GONetLog.Error(string.Concat(UNABLE, nameof(shouldAttemptAutoStartAsClient), BECAUSE, nameof(GONetSampleSpawner), ATTACHED));
                }
            }
        }

        private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            { // do auto-assign authority id stuffs for all gonet stuff in scene
                List<GONetParticipant> gonetParticipantsInLevel = new List<GONetParticipant>();
                GameObject[] sceneObjects = sceneLoaded.GetRootGameObjects();

                GONetLog.Debug($"OnSceneLoaded: '{sceneLoaded.name}' with {sceneObjects.Length} root objects");

                FindAndAppend(sceneObjects, gonetParticipantsInLevel, (gnp) => !WasInstantiated(gnp)); // IMPORTANT: or else!

                GONetLog.Debug($"Found {gonetParticipantsInLevel.Count} GONetParticipants in scene. Names: {string.Join(", ", gonetParticipantsInLevel.ConvertAll(gnp => gnp.name))}");

                GONetMain.RecordParticipantsAsDefinedInScene(gonetParticipantsInLevel);

                if (GONetMain.IsClientVsServerStatusKnown)
                {
                    GONetMain.AssignOwnerAuthorityIds_IfAppropriate(gonetParticipantsInLevel);

                    // IMPORTANT: If this is the server and there are connected clients, sync scene-defined object GONetIds
                    if (GONetMain.IsServer && GONetMain.gonetServer != null && GONetMain.gonetServer.numConnections > 0)
                    {
                        StartCoroutine(SyncSceneDefinedObjectIds_WhenReady(sceneLoaded.name, gonetParticipantsInLevel));
                    }
                }
                else
                {
                    StartCoroutine(AssignOwnerAuthorityIds_WhenAppropriate(gonetParticipantsInLevel));
                }
            }

            bool WasInstantiated(GONetParticipant gONetParticipant)
            {
                // OLD WAY that no longer applies since this info is not stored on GNP
                //(gnp) => gnp.DesignTimeLocation.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX)); // IMPORTANT: or else!

                //string fullUniquePath = DesignTimeMetadata.GetFullUniquePathInScene(gonetParticipant);
                //return !GONetSpawnSupport_Runtime.AnyDesignTimeMetadata(fullUniquePath);

                // TODO FIXME figure out what case exists where all the GNPs in a newly loaded scene do not ALL get considered NOT spawned/instantiated!!!!

                return false;
            }
        }

        private IEnumerator AssignOwnerAuthorityIds_WhenAppropriate(List<GONetParticipant> gonetParticipantsInLevel)
        {
            while (!GONetMain.IsClientVsServerStatusKnown)
            {
                yield return null;
            }

            GONetMain.AssignOwnerAuthorityIds_IfAppropriate(gonetParticipantsInLevel);
        }

        private IEnumerator SyncSceneDefinedObjectIds_WhenReady(string sceneName, List<GONetParticipant> sceneParticipants)
        {
            // Wait a frame to ensure all GONetIds have been assigned
            yield return null;

            // Collect design-time locations and GONetIds for all scene-defined objects
            List<string> designTimeLocations = new List<string>();
            List<uint> gonetIds = new List<uint>();

            foreach (var participant in sceneParticipants)
            {
                if (participant != null &&
                    participant.IsDesignTimeMetadataInitd &&
                    participant.GONetId != 0 &&
                    !string.IsNullOrEmpty(participant.DesignTimeLocation))
                {
                    designTimeLocations.Add(participant.DesignTimeLocation);
                    gonetIds.Add(participant.GONetId);
                }
            }

            if (designTimeLocations.Count > 0)
            {
                GONetLog.Info($"[GONetGlobal] Syncing {designTimeLocations.Count} scene-defined object GONetIds for scene '{sceneName}' to all clients");
                SendSceneDefinedObjectIdSync(sceneName, designTimeLocations.ToArray(), gonetIds.ToArray());
            }
        }

        private static void FindAndAppend<T>(GameObject[] gameObjects, /* IN/OUT */ List<T> listToAppend, Func<T, bool> filter)
        {
            int count = gameObjects != null ? gameObjects.Length : 0;
            for (int i = 0; i < count; ++i)
            {
                T t = gameObjects[i].GetComponent<T>();
                if (t != null && filter(t))
                {
                    listToAppend.Add(t);
                }
                foreach (Transform childTransform in gameObjects[i].transform)
                {
                    FindAndAppend(new[] { childTransform.gameObject }, listToAppend, filter);
                }
            }
        }

        private void Update()
        {
            GONetMain.Update(this);

            // Process deferred RPCs - handle cases where GONetParticipants weren't available during initial processing
            GONetEventBus.ProcessDeferredRpcs();

            // GONetId Reuse Prevention: Periodic cleanup of expired despawned GONetIds
            GONetMain.CleanupExpiredDespawnedGONetIds();
        }

        /// <summary>
        /// Unity's FixedUpdate() hook - Calls GONetMain.FixedUpdate_AfterGONetReady() for physics frame updates.
        /// Runs at Unity's fixed timestep (default: 50Hz / 0.02 seconds).
        /// </summary>
        private void FixedUpdate()
        {
            GONetMain.FixedUpdate_AfterGONetReady();
        }

        private void OnApplicationQuit()
        {
            // DIAGNOSTIC DUMP: Log lifecycle state of ALL GONetParticipants before shutdown
            // This helps us understand what prevented OnGONetReady from firing
            //DumpLifecycleStateDiagnostics();

            GONetMain.Shutdown();
        }

        /// <summary>
        /// Diagnostic dump of all GONetParticipants showing which lifecycle gates prevented OnGONetReady.
        /// Called on application quit to capture final state for analysis.
        /// </summary>
        private void DumpLifecycleStateDiagnostics()
        {
            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder(4096);
                sb.AppendLine("========================================");
                sb.AppendLine("[QUIT-DIAGNOSTIC] Application quitting - dumping GONetParticipant lifecycle states");
                sb.AppendLine("========================================");

                // Find ALL GONetParticipants (even destroyed ones might still exist)
                GONetParticipant[] allParticipants = UnityEngine.Object.FindObjectsOfType<GONetParticipant>(includeInactive: true);

                sb.AppendLine($"[QUIT-DIAGNOSTIC] Found {allParticipants.Length} total GONetParticipants");
                sb.AppendLine();

                int neverFiredOnGONetReady = 0;
                int awakeIncomplete = 0;
                int startIncomplete = 0;
                int deserializeIncomplete = 0;
                int missingGONetId = 0;
                int missingAuthority = 0;

                foreach (var participant in allParticipants)
                {
                    if (participant == null) continue; // Unity fake null check

                    bool firedReady = participant.didOnGONetReadyFire;
                    if (!firedReady)
                    {
                        neverFiredOnGONetReady++;

                        // Log detailed state for participants that never fired OnGONetReady
                        sb.AppendLine($"[QUIT-DIAGNOSTIC] NEVER FIRED OnGONetReady:");
                        sb.AppendLine($"  InstanceID: {participant.GetInstanceID()}");
                        sb.AppendLine($"  GameObject: {participant.gameObject.name}");
                        sb.AppendLine($"  GONetId: {participant.GONetId} (Unset={participant.GONetId == GONetParticipant.GONetId_Unset})");
                        sb.AppendLine($"  OwnerAuthorityId: {participant.OwnerAuthorityId} (Unset={participant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset})");
                        sb.AppendLine($"  IsMine: {participant.IsMine}");
                        sb.AppendLine($"  WasInstantiated: {participant.WasInstantiated}");
                        sb.AppendLine($"  IsInternallyConfigured: {participant.IsInternallyConfigured}");
                        sb.AppendLine($"  LIFECYCLE GATES:");
                        sb.AppendLine($"    didAwakeComplete: {participant.didAwakeComplete}");
                        sb.AppendLine($"    didStartComplete: {participant.didStartComplete}");
                        sb.AppendLine($"    requiresDeserializeInit: {participant.requiresDeserializeInit}");
                        sb.AppendLine($"    didDeserializeInitComplete: {participant.didDeserializeInitComplete}");
                        sb.AppendLine($"    didOnGONetReadyFire: {participant.didOnGONetReadyFire}");
                        sb.AppendLine($"  CLIENT LIMBO STATE:");
                        sb.AppendLine($"    client_isInLimbo: {participant.client_isInLimbo}");
                        sb.AppendLine();

                        // Count failure reasons
                        if (!participant.didAwakeComplete) awakeIncomplete++;
                        if (!participant.didStartComplete) startIncomplete++;
                        if (participant.requiresDeserializeInit && !participant.didDeserializeInitComplete) deserializeIncomplete++;
                        if (participant.GONetId == GONetParticipant.GONetId_Unset) missingGONetId++;
                        if (participant.OwnerAuthorityId == GONetMain.OwnerAuthorityId_Unset) missingAuthority++;
                    }
                }

                // Summary statistics
                sb.AppendLine("========================================");
                sb.AppendLine("[QUIT-DIAGNOSTIC] SUMMARY:");
                sb.AppendLine($"  Total participants: {allParticipants.Length}");
                sb.AppendLine($"  OnGONetReady fired: {allParticipants.Length - neverFiredOnGONetReady}");
                sb.AppendLine($"  OnGONetReady NEVER fired: {neverFiredOnGONetReady}");
                sb.AppendLine();
                sb.AppendLine("  Failure breakdown (participants may have multiple issues):");
                sb.AppendLine($"    didAwakeComplete = false: {awakeIncomplete}");
                sb.AppendLine($"    didStartComplete = false: {startIncomplete}");
                sb.AppendLine($"    Deserialization incomplete: {deserializeIncomplete}");
                sb.AppendLine($"    GONetId unset: {missingGONetId}");
                sb.AppendLine($"    OwnerAuthorityId unset: {missingAuthority}");
                sb.AppendLine("========================================");

                // Output entire diagnostic as ONE log statement
                GONetLog.Info(sb.ToString());
            }
            catch (System.Exception ex)
            {
                GONetLog.Error($"[QUIT-DIAGNOSTIC] Exception during lifecycle dump: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private System.Collections.IEnumerator CacheDesignTimeMetadata_ThenContinueInit()
        {
            // Start the caching process and wait for it to complete
            bool cachingComplete = false;
            GONetSpawnSupport_Runtime.CacheAllProjectDesignTimeMetadata(this, () => cachingComplete = true);

            // Wait until caching is actually complete
            while (!cachingComplete)
            {
                yield return null;
            }

            GONetLog.Debug("GONetGlobal: Design time metadata caching completed - ready for scene processing");
        }

        // ========================================
        // SCENE MANAGEMENT RPCs (Phase 5)
        // ========================================
        //
        // IMPORTANT NOTES FOR ADDING RPCs TO CLASSES IN NAMESPACES:
        //
        // 1. RPC methods can be internal or public (both work with code generator)
        // 2. Classes with RPCs MUST derive from GONetParticipantCompanionBehaviour
        // 3. Classes CAN be in namespaces - generator handles this correctly
        // 4. For TargetRpc with validation:
        //    - Use property-based targeting: [TargetRpc(nameof(PropertyName), validationMethod: nameof(ValidateMethod))]
        //    - First constructor parameter must be a property/field name (string), NOT RpcTarget enum
        //    - Validation methods must return RpcValidationResult (not bool)
        //    - Validation methods must use ref parameters matching the RPC signature
        //    - Example: private RpcValidationResult ValidateMyRpc(ref string param1, ref int param2)
        // 5. Get validation context via GONetMain.EventBus.GetValidationContext()
        // 6. Use context.GetValidationResult() to get the result object
        // 7. Call result.AllowAll() or result.DenyAll() to control RPC execution
        //
        // See RPC_RequestLoadScene and Validate_RequestLoadScene below for complete examples.
        // ========================================

        /// <summary>
        /// Property that returns the server's authority ID for TargetRpc targeting.
        /// </summary>
        internal ushort ServerAuthorityId => GONetMain.OwnerAuthorityId_Server;

        /// <summary>
        /// TARGET RPC: Request server to load a scene (usable by both clients and server).
        /// Uses TargetRpc for built-in validation support.
        /// </summary>
        [TargetRpc(nameof(ServerAuthorityId), validationMethod: nameof(Validate_RequestLoadScene))]
        internal async Task<RpcDeliveryReport> RPC_RequestLoadScene(string sceneName, byte modeRaw, byte loadTypeRaw)
        {
            // IMPORTANT: This RPC should only execute on the server
            // When called from client, it sends to server but also executes locally
            // We need to check if we're the server before processing
            if (!GONetMain.IsServer)
            {
                // This is the client-side call that triggers the RPC send
                // Don't execute the logic here, just let it send to server
                return default;
            }

            LoadSceneMode mode = (LoadSceneMode)modeRaw;
            SceneLoadType loadType = (SceneLoadType)loadTypeRaw;

            GONetLog.Info($"[GONetGlobal] Scene load request received: '{sceneName}' (Mode: {mode}, Type: {loadType})");

            // IMPORTANT: If async approval is required, validation hook will show UI
            // and the actual scene load happens when user approves (in SceneSelectionUI.OnApproveClicked)
            // so we must NOT load the scene here - just let validation handle it
            bool requiresAsyncApproval = GONetMain.SceneManager.RequiresAsyncApproval;
            GONetLog.Info($"[GONetGlobal] Checking RequiresAsyncApproval: {requiresAsyncApproval}");
            if (requiresAsyncApproval)
            {
                GONetLog.Info($"[GONetGlobal] Scene load requires async approval - validation will handle UI, scene will load after approval");
                return default;
            }
            else
            {
                GONetLog.Info($"[GONetGlobal] RequiresAsyncApproval is FALSE - proceeding with immediate scene load");
            }

            // Forward to scene manager (only when async approval NOT required)
            if (loadType == SceneLoadType.BuildSettings)
            {
                GONetMain.SceneManager.LoadSceneFromBuildSettings(sceneName, mode);
            }
#if ADDRESSABLES_AVAILABLE
            else if (loadType == SceneLoadType.Addressables)
            {
                GONetMain.SceneManager.LoadSceneFromAddressables(sceneName, mode);
            }
#endif
            else
            {
                GONetLog.Error($"[GONetGlobal] Unsupported scene load type: {loadType}");
            }

            return default;
        }

        /// <summary>
        /// Validation method for scene load requests.
        /// Called by TargetRpc system before executing RPC_RequestLoadScene.
        /// </summary>
        internal RpcValidationResult Validate_RequestLoadScene(ref string sceneName, ref byte modeRaw, ref byte loadTypeRaw)
        {
            LoadSceneMode mode = (LoadSceneMode)modeRaw;

            // Get validation context and result
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                GONetLog.Error("[GONetGlobal] No validation context available for scene load request");
                var errorResult = RpcValidationResult.CreatePreAllocated(0);
                errorResult.DenyAll("No validation context");
                return errorResult;
            }

            var validationContext = context.Value;
            var result = validationContext.GetValidationResult();
            ushort callerAuthorityId = validationContext.SourceAuthorityId;

            // Use scene manager's validation hook
            var sceneManager = GONetMain.SceneManager;
            bool allowed = sceneManager.InvokeValidation(sceneName, mode, callerAuthorityId);
            if (!allowed)
            {
                GONetLog.Warning($"[GONetGlobal] Scene load request denied by validation: '{sceneName}' from client {callerAuthorityId}");
                result.DenyAll($"Scene load denied for '{sceneName}'");
                return result;
            }

            // Allow all targets (in this case, should only be the server)
            result.AllowAll();

            // If validation requires async approval (e.g., server UI), set ExpectFollowOnResponse
            // This signals to the caller that a follow-on RPC will be sent with the final decision
            result.ExpectFollowOnResponse = sceneManager.RequiresAsyncApproval;

            return result;
        }

        /// <summary>
        /// TARGET RPC: Server sends scene load request response to client.
        /// Uses first ushort parameter to specify target client authority ID.
        /// </summary>
        /// <param name="targetClientId">Authority ID of the client to receive the response</param>
        /// <param name="approved">True if request was approved, false if denied</param>
        /// <param name="sceneName">Name of the scene that was requested</param>
        /// <param name="denialReason">If denied, the reason for denial (optional)</param>
        [TargetRpc]
        internal void RPC_SceneRequestResponse(ushort targetClientId, bool approved, string sceneName, string denialReason = "")
        {
            if (approved)
            {
                GONetLog.Info($"[GONetGlobal] Scene request approved: '{sceneName}'");
            }
            else
            {
                string reason = string.IsNullOrEmpty(denialReason) ? "Request denied" : denialReason;
                GONetLog.Warning($"[GONetGlobal] Scene request denied: '{sceneName}' - {reason}");
            }

            // Notify scene manager of response
            GONetMain.SceneManager.InvokeSceneRequestResponse(approved, sceneName, denialReason);
        }

        /// <summary>
        /// INTERNAL: Sends scene load request RPC to server.
        /// <para><b>USER NOTE:</b> This method is internal infrastructure and should NOT be called by user code.</para>
        /// <para>Due to GONet being in the same assembly as your game code, internal methods are technically accessible,
        /// but calling them directly bypasses the intended public API.</para>
        /// <para><b>Instead, use:</b> <c>GONetMain.SceneManager.RequestLoadBuildSettingsScene(...)</c> or <c>RequestLoadAddressablesScene(...)</c></para>
        /// </summary>
        internal void SendSceneLoadRequest(string sceneName, byte modeRaw, byte loadTypeRaw)
        {
            CallRpc(nameof(RPC_RequestLoadScene), sceneName, modeRaw, loadTypeRaw);
        }

        /// <summary>
        /// INTERNAL: Sends scene unload request RPC to server.
        /// <para><b>USER NOTE:</b> This method is internal infrastructure and should NOT be called by user code.</para>
        /// <para>Due to GONet being in the same assembly as your game code, internal methods are technically accessible,
        /// but calling them directly bypasses the intended public API.</para>
        /// <para><b>Instead, use:</b> <c>GONetMain.SceneManager.RequestUnloadScene(...)</c></para>
        /// </summary>
        internal void SendSceneUnloadRequest(string sceneName)
        {
            CallRpc(nameof(RPC_RequestUnloadScene), sceneName);
        }

        /// <summary>
        /// INTERNAL: Sends scene request response RPC to client.
        /// <para><b>USER NOTE:</b> This method is internal infrastructure and should NOT be called by user code.</para>
        /// <para>Due to GONet being in the same assembly as your game code, internal methods are technically accessible,
        /// but calling them directly bypasses the intended public API.</para>
        /// <para><b>Instead, use:</b> <c>GONetMain.SceneManager.SendSceneRequestResponse(...)</c></para>
        /// </summary>
        internal void SendSceneRequestResponse(ushort clientId, bool approved, string sceneName, string reason = "")
        {
            CallRpc(nameof(RPC_SceneRequestResponse), clientId, approved, sceneName, reason);
        }

        /// <summary>
        /// TARGET RPC: Request server to unload a scene (usable by both clients and server).
        /// Uses TargetRpc for built-in validation support.
        /// </summary>
        [TargetRpc(nameof(ServerAuthorityId), validationMethod: nameof(Validate_RequestUnloadScene))]
        internal void RPC_RequestUnloadScene(string sceneName)
        {
            GONetLog.Info($"[GONetGlobal] Scene unload request received: '{sceneName}'");
            GONetMain.SceneManager.UnloadScene(sceneName);
        }

        /// <summary>
        /// Validation method for scene unload requests.
        /// Called by TargetRpc system before executing RPC_RequestUnloadScene.
        /// </summary>
        internal RpcValidationResult Validate_RequestUnloadScene(ref string sceneName)
        {
            // Get validation context and result
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                GONetLog.Error("[GONetGlobal] No validation context available for scene unload request");
                var errorResult = RpcValidationResult.CreatePreAllocated(0);
                errorResult.DenyAll("No validation context");
                return errorResult;
            }

            var result = context.Value.GetValidationResult();

            // Can add validation hook for unload if needed in future
            // For now, allow all unload requests (scene manager will validate if scene is loaded)
            result.AllowAll();
            return result;
        }

        /// <summary>
        /// TARGET RPC: Server sends scene-defined object GONetId assignments to client(s).
        /// First parameter specifies target: use OwnerAuthorityId_Unset for all clients, or specific authority ID for single client.
        /// Called after client initialization is complete, so all scene objects should be ready.
        /// </summary>
        [TargetRpc]
        internal void RPC_SyncSceneDefinedObjectIds(ushort targetClientId, string sceneName, string[] designTimeLocations, uint[] gonetIds)
        {
            // Only process on clients
            if (GONetMain.IsServer)
                return;

            GONetLog.Info($"[GONetGlobal] Received scene GONetId sync for '{sceneName}' - {designTimeLocations.Length} objects");

            int assignedCount = 0;
            int notFoundCount = 0;

            // Match each design-time location to a GONetParticipant and assign its GONetId
            for (int i = 0; i < designTimeLocations.Length; i++)
            {
                string location = designTimeLocations[i];
                uint gonetId = gonetIds[i];

                // Find the GONetParticipant with this design-time location
                GONetParticipant participant = GONetMain.FindParticipantByDesignTimeLocation(location, sceneName);
                if (participant != null)
                {
                    // Assign the GONetId to match the server's assignment
                    GONetMain.AssignGONetIdRaw_Direct(participant, gonetId);
                    GONetLog.Debug($"[GONetGlobal] Assigned GONetId {gonetId} to scene object '{participant.gameObject.name}' at location '{location}'");
                    assignedCount++;
                }
                else
                {
                    GONetLog.Warning($"[GONetGlobal] Could not find scene object at location '{location}' to assign GONetId {gonetId}");
                    notFoundCount++;
                }
            }

            if (notFoundCount > 0)
            {
                GONetLog.Warning($"[GONetGlobal] Assigned {assignedCount} of {designTimeLocations.Length} scene-defined object GONetIds for scene '{sceneName}' ({notFoundCount} not found)");
            }
            else
            {
                GONetLog.Info($"[GONetGlobal] Successfully assigned all {assignedCount} scene-defined object GONetIds for scene '{sceneName}'");
            }

            // IMPORTANT: Mark that scene-defined object IDs are ready and process any queued messages
            if (GONetMain.IsClient && GONetMain.GONetClient != null)
            {
                GONetMain.GONetClient.areSceneDefinedObjectIdsReady = true;
                GONetMain.ProcessQueuedMessagesWaitingForGONetIds();
            }
        }

        /// <summary>
        /// INTERNAL: Sends scene-defined object GONetId assignments to all clients.
        /// Called by server after loading a scene with scene-defined objects.
        /// </summary>
        internal void SendSceneDefinedObjectIdSync(string sceneName, string[] designTimeLocations, uint[] gonetIds)
        {
            CallRpc(nameof(RPC_SyncSceneDefinedObjectIds), GONetMain.OwnerAuthorityId_Unset, sceneName, designTimeLocations, gonetIds);
        }

        /// <summary>
        /// INTERNAL: Sends scene-defined object GONetId assignments to a specific client.
        /// Called by server when a late-joining client connects.
        /// </summary>
        internal void SendSceneDefinedObjectIdSync_ToSpecificClient(string sceneName, string[] designTimeLocations, uint[] gonetIds, ushort targetClientAuthorityId)
        {
            CallRpc(nameof(RPC_SyncSceneDefinedObjectIds), targetClientAuthorityId, sceneName, designTimeLocations, gonetIds);
        }

        /// <summary>
        /// UNIVERSAL LOGGING: Captures OnGONetReady for ALL GONet participants (beacons, projectiles, physics cubes, etc.).
        /// This is the central coordinator that sees every participant's lifecycle, providing consistent logging
        /// regardless of companion script type or GameObject.
        ///
        /// Used for comprehensive log analysis to track OnGONetReady timing, frame delays, and reliability metrics.
        /// InstanceID enables correlation with Awake() events for complete lifecycle tracking.
        /// </summary>
        public override void OnGONetReady(GONetParticipant gonetParticipant)
        {
            base.OnGONetReady(gonetParticipant);

            // Log with InstanceID for Awake correlation and GONetId for other analysis
            //GONetLog.Info($"[GONetGlobal] ✅ OnGONetReady FIRED - InstanceID: {gonetParticipant.GetInstanceID()}, GONetId: {gonetParticipant.GONetId}, GameObject: {gonetParticipant.name}, IsMine: {gonetParticipant.IsMine}, Owner: {gonetParticipant.OwnerAuthorityId}");
        }
    }
}
