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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// </summary>
    public abstract class GONetBehaviour : MonoBehaviour
    {
        #region UpdateAfterGONetReady Support - Static Per-Type Caching

        /// <summary>
        /// Static helper class for caching method override detection across all three update variants.
        /// Populated once per type on first instance construction, avoiding per-instance reflection overhead.
        /// </summary>
        private static class GONetBehaviour_UpdateAfterGONetReady_Cache
        {
            private static readonly Dictionary<Type, Dictionary<string, bool>> overrideCache = new Dictionary<Type, Dictionary<string, bool>>();

            internal static bool HasOverride(Type type, string methodName)
            {
                if (!overrideCache.TryGetValue(type, out Dictionary<string, bool> methodCache))
                {
                    methodCache = new Dictionary<string, bool>();
                    overrideCache[type] = methodCache;
                }

                if (!methodCache.TryGetValue(methodName, out bool hasOverride))
                {
                    // First instance of this type checking this method - use reflection ONCE
                    MethodInfo method = type.GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    // Check if the declaring type is THIS type or a subclass (not GONetBehaviour/GONetParticipantCompanionBehaviour base)
                    hasOverride = method != null &&
                                  method.DeclaringType != typeof(GONetBehaviour) &&
                                  method.DeclaringType != typeof(GONetParticipantCompanionBehaviour);

                    // Cache the result for all future instances of this type
                    methodCache[methodName] = hasOverride;
                }

                return hasOverride;
            }
        }

        /// <summary>
        /// Instance-level flags indicating whether THIS specific type overrides update methods.
        /// Set once during Awake() by looking up the static cache.
        /// Used by GONetMain update loops to avoid calling empty base implementations.
        /// </summary>
        internal bool hasUpdateAfterGONetReadyOverride;
        internal bool hasLateUpdateAfterGONetReadyOverride;
        internal bool hasFixedUpdateAfterGONetReadyOverride;

        #endregion
        private const float SEMI_ARBITRAY_WAIT_BEFORE_INFORMING_CLINET_VS_SERVER_STATUS_KNOWN_SECONDS = 0.1f;

        [Tooltip("GONet will send a 'tick' at each of the unique synchronization schedules as defined in various profiles (i.e., GONet => GONet Editor Support => Create New Sync Settings Profile) used when syncing values.")]
        [SerializeField] private bool isTickReceiver;
        public bool IsTickReceiver
        {
            get => isTickReceiver;
            set
            {
                isTickReceiver = value;
                if (isTickReceiver)
                {
                    GONetMain.AddTickReceiver(this);
                }
            }
        }

        /// <summary>
        /// IMPORTANT: Keep in mind this is not going to be a good/final value until <see cref="OnGONetClientVsServerStatusKnown(bool, bool, ushort)"/> is called, which is also when <see cref="GONetMain.IsClientVsServerStatusKnown"/> turns true.
        /// </summary>
        public bool IsServer => GONetMain.IsServer;

        /// <summary>
        /// IMPORTANT: Keep in mind this is not going to be a good/final value until <see cref="OnGONetClientVsServerStatusKnown(bool, bool, ushort)"/> is called, which is also when <see cref="GONetMain.IsClientVsServerStatusKnown"/> turns true.
        /// </summary>
        public bool IsClient => GONetMain.IsClient;

        /// <summary>
        /// Since this is a vital feature of GONet, it is conveniently placed here to avoid having to type "GONetMain." each time when in a child class.
        /// </summary>
        public GONetEventBus EventBus => GONetMain.EventBus;

        /// <summary>
        /// Convenient access to GONet's scene manager for networked scene loading/unloading.
        /// Server-authoritative: only server can initiate scene changes.
        /// </summary>
        public GONetSceneManager SceneManager => GONetMain.SceneManager;

        protected virtual void Awake()
        {
            GONetMain.RegisterBehaviour(this);

            // PERFORMANCE OPTIMIZATION: Static per-type caching for update method override detection
            // Uses reflection ONCE per type (not per instance) to detect which update methods are overridden
            Type myType = GetType();
            hasUpdateAfterGONetReadyOverride = GONetBehaviour_UpdateAfterGONetReady_Cache.HasOverride(myType, "UpdateAfterGONetReady");
            hasLateUpdateAfterGONetReadyOverride = GONetBehaviour_UpdateAfterGONetReady_Cache.HasOverride(myType, "LateUpdateAfterGONetReady");
            hasFixedUpdateAfterGONetReadyOverride = GONetBehaviour_UpdateAfterGONetReady_Cache.HasOverride(myType, "FixedUpdateAfterGONetReady");
        }

        protected virtual void OnEnable()
        {
            if (IsTickReceiver)
            {
                GONetMain.AddTickReceiver(this);
            }
        }

        protected virtual void OnDisable()
        {
            GONetMain.RemoveTickReceiver(this);
        }

        internal void OnGONetParticipant_OwnerAuthorityIdChanged(GONetParticipant gonetParticipant, uint gonetId, ushort valuePrevious, ushort valueNew)
        {
            if ((object)gonetParticipant == null)
            {
                gonetParticipant = GONetMain.DeriveGNPFromCurrentAndPreviousValues(gonetId, valuePrevious, valueNew);
            }

            bool isSetToValidValue = gonetParticipant.gonetId_raw != GONetParticipant.GONetId_Unset && valueNew != GONetMain.OwnerAuthorityId_Unset;
            if (isSetToValidValue)
            {
                OnGONetParticipant_OwnerAuthorityIdSet(gonetParticipant);
            }
        }

        protected virtual void Start()
        {
            StartCoroutine(WaitThenTriggerClientVsServerStatusKnown());
        }

        private IEnumerator WaitThenTriggerClientVsServerStatusKnown()
        {
            while (!GONetMain.IsClientVsServerStatusKnown || GONetMain.MyAuthorityId == GONetMain.OwnerAuthorityId_Unset)
            {
                yield return null;
            }

            if (GONetMain.IsServer)
            {
                while (GONetMain.gonetServer == null)
                {
                    yield return null;
                }
            }

            if (GONetMain.IsClient)
            {
                while (GONetMain.GONetClient == null)
                {
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(SEMI_ARBITRAY_WAIT_BEFORE_INFORMING_CLINET_VS_SERVER_STATUS_KNOWN_SECONDS); // TODO this magic number to wait is bogus and not sure fire....we need to wait only the exact amount of "time" required and no more/no less
            // Option B: still not sure fire...need to dig in more to know the exact moment thsi is safe and exact thing that happens if not wait!! Share in docs to users!!
            //yield return null;
            //yield return new WaitForEndOfFrame();
            
            OnGONetClientVsServerStatusKnown(GONetMain.IsClient, GONetMain.IsServer, GONetMain.MyAuthorityId);
        }

        protected virtual void OnDestroy()
        {
            GONetMain.UnregisterBehaviour(this);
        }

        /// <summary>
        /// <para>
        /// IMPORTANT: When this gets called, the <see cref="GONet.GONetLocal"/> for this machine and the <see cref="GONet.GONetGlobal"/> 
        ///            for all machines has already been initialized, and the following methods have been called prior to this one
        ///            on those GoNet participants' instances of this behavior on them:
        ///            ---<see cref="OnGONetParticipantStarted(GONetParticipant)"/>
        ///            ---<see cref="OnGONetParticipantEnabled(GONetParticipant)"/>
        ///            
        ///            It is guaranteed to execute at least <see cref="SEMI_ARBITRAY_WAIT_BEFORE_INFORMING_CLINET_VS_SERVER_STATUS_KNOWN_SECONDS"/> 
        ///            seconds after those above mentioned methods!
        /// </para>     
        /// <para>When this is called, GONet knows whether or not this machine is going to be a GONet client or server.  So any action that must know that first is now OK to execute.</para>
        /// <para>Futhermore, GONet has also assigned an authority id for this machine (i.e., <see cref="GONetMain.MyAuthorityId"/> is set) and that is important as well before doing certain things like instantiating/spawning prefabs with <see cref="GONetParticipant"/> attached.</para>
        /// <para>So, please after this is called, feel free to instantiate/spawn networked GameObjects (i.e., with <see cref="GONetParticipant"/>) into the scene.</para>
        /// </summary>
        /// <param name="isClient">The value of <see cref="GONetMain.IsClient"/> at the time of calling this method. If true, it is also true that <see cref="GONetMain.GONetClient"/> is not null.</param>
        /// <param name="isServer">The value of <see cref="GONetMain.IsServer"/> at the time of calling this method. If true, it is also true that <see cref="GONetMain.gonetServer"/> is not null.</param>
        /// <param name="myAuthorityId">The value of <see cref="GONetMain.MyAuthorityId"/> at the time of calling this method, which is guaranteed to be valid and not <see cref="GONetMain.OwnerAuthorityId_Unset"/></param>
        public virtual void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId) { }

        public virtual void OnGONetParticipantEnabled(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// IMPORTANT: When this is called, this is the first time it is certain that the 
        ///            <see cref="GONetParticipant.GONetId"/> value is fully assigned!
        /// </summary>
        public virtual void OnGONetParticipantStarted(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <paramref name="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public virtual void OnGONetParticipantDeserializeInitAllCompleted(GONetParticipant gonetParticipant) { }

        public virtual void OnGONetParticipantDisabled(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// Since there is some order of operations differences between machines who instantiate a new <see cref="GONetParticipant"/> and others in regards to
        /// at what point the <see cref="GONetParticipant.OwnerAuthorityId"/> is set AND one of those differences is the value not being set at the point of
        /// the call to <see cref="OnGONetParticipantStarted(GONetParticipant)"/>, this method exists to have a callback.
        /// </summary>
        /// <param name="gonetParticipant"></param>
        public virtual void OnGONetParticipant_OwnerAuthorityIdSet(GONetParticipant gonetParticipant) { }

        /// <summary>
        /// Called when GONet is fully initialized and ready for use. This is the recommended hook for initializing components.
        /// See <see cref="GONetParticipantCompanionBehaviour.OnGONetReady()"/> for full documentation of guarantees.
        /// </summary>
        public virtual void OnGONetReady(GONetParticipant gonetParticipant) { }

        /// <param name="uniqueTickHz">how many times a second this unique frequency is called at...there are many possibilities since each GONet sync settings profile can have its frequency set to a different value</param>
        /// <param name="elapsedSeconds"></param>
        /// <param name="deltaTime">seconds passed since last call to this</param>
        internal virtual void Tick(short uniqueTickHz, double elapsedSeconds, double deltaTime) { }
    }

    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// NOTE: This is a convenience class named for Photon PUN users as they might be used to using MonoBehaviourPunCallbacks, but this is the same as <see cref="GONetBehaviour"/>.
    /// </summary>
    public abstract class MonoBehaviourGONetCallbacks : GONetBehaviour { }

    /// <summary>
    /// NOTE: This is a convenience class named with the "MonoBehaviour" prefix in case it helps identifying this class as a possible one to use.
    ///       This is the same as <see cref="GONetParticipantCompanionBehaviour"/> and you can read the class documentation there to know how to use.
    /// </summary>
    public abstract class MonoBehaviourGONetParticipantCompanion : GONetParticipantCompanionBehaviour { }

    /// <summary>
    /// <para>
    /// For <see cref="GameObject"/>s that have a <see cref="GONet.GONetParticipant"/> "installed" on them, the other <see cref="MonoBehaviour"/>s also "installed" can
    /// optionally extend this class to automatically have a reference to the <see cref="GONetParticipant"/> instance to reference it when making decisions
    /// on what to execute.  The most common example is to use <see cref="GONetParticipant.IsMine"/> to know whether or not to execute some game logic or not so that
    /// the logic is only executed on the owner's machine and the networking will handle the rest so the other machines will see the results of the game logic being
    /// executed by "the owner."
    /// </para>
    /// <para>
    /// It is also important to know that a <see cref="MonoBehaviour"/> that extends this class can be "installed" on a child <see cref="GameObject"/> of where the
    /// <see cref="GONetParticipant"/> is "installed" and this class will look up to the parent to find the nearest "installed" <see cref="GONetParticipant"/> and
    /// reference that.  This is helpful when some game logic is present in children that is relevant to networking stuffs and you want to keep it that way.
    /// </para>
    ///
    /// <para><b>CRITICAL: Runtime Component Addition</b></para>
    /// <para>GONetParticipantCompanionBehaviour components are designed to be present on GameObjects from scene load (design-time).
    /// If you need to add these components at runtime, you MUST use <see cref="GONetRuntimeComponentInitializer"/> - this is the ONLY
    /// officially supported method. Using Unity's AddComponent() directly is NOT supported and will cause lifecycle issues.</para>
    ///
    /// <para><b>Lifecycle Differences:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Design-time:</b> Full lifecycle - OnGONetParticipantEnabled → OnGONetParticipantStarted → OnGONetParticipantDeserializeInitAllCompleted → OnGONetReady</description></item>
    ///   <item><description><b>Runtime (via GONetRuntimeComponentInitializer):</b> Simplified lifecycle - OnGONetReady only (called from Start)</description></item>
    /// </list>
    ///
    /// <para><b>RECOMMENDED:</b> Use <see cref="OnGONetReady()"/> for initialization. It works correctly in both design-time and runtime scenarios
    /// with the same guarantees (GONetId assigned, OwnerAuthorityId set, GONetLocal available, RPCs ready).</para>
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public abstract class GONetParticipantCompanionBehaviour : GONetBehaviour
    {
        public bool IsMine => gonetParticipant.IsMine;
        public GONetParticipant GONetParticipant => gonetParticipant;

        protected GONetParticipant gonetParticipant;

        /// <summary>
        /// <para>Indicates whether this component was added at runtime (after the GONetParticipant was already fully initialized).</para>
        /// <para>This is set during Awake() and used to determine the proper lifecycle callback sequence.</para>
        ///
        /// <para><b>IMPORTANT:</b> The ONLY officially supported way to add GONetParticipantCompanionBehaviour components at runtime
        /// is via <see cref="GONetRuntimeComponentInitializer"/>. Using Unity's AddComponent() directly is NOT recommended
        /// and may result in unexpected behavior.</para>
        ///
        /// <para><b>Design-time (False):</b> Component was present in the scene when it loaded.</para>
        /// <para><b>Runtime (True):</b> Component was added via <see cref="GONetRuntimeComponentInitializer"/> after scene load.</para>
        /// </summary>
        public bool WasAddedAtRuntime { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            gonetParticipant = GetComponent<GONetParticipant>();
            Transform xform = transform;
            while ((object)gonetParticipant == null && (object)xform != null)
            {
                gonetParticipant = xform.gameObject.GetComponent<GONetParticipant>();
                xform = xform.parent;
            }

            // Check if we're being added to an already-ready GONetParticipant
            // Wrap in try-catch to handle any edge cases during initialization
            try
            {
                if (gonetParticipant != null && IsGONetReady())
                {
                    // Participant is already fully ready - we're being added at runtime
                    WasAddedAtRuntime = true;
                }
            }
            catch (Exception ex)
            {
                // If we can't determine runtime status, default to false (design-time)
                // This ensures we don't break initialization even in unexpected scenarios
                GONetLog.Warning($"[GONetBehaviour] Could not determine WasAddedAtRuntime status for '{GetType().Name}' on '{gameObject?.name ?? "unknown"}': {ex.Message}");
                WasAddedAtRuntime = false;
            }
        }

        /// <summary>
        /// Checks if the associated GONetParticipant is fully initialized and ready for use.
        /// This means:
        /// - GONetId is assigned
        /// - GONetLocal is available in the lookup
        /// - Client/Server status is known
        /// - If client, fully initialized with server
        /// </summary>
        public bool IsGONetReady()
        {
            return GONetMain.IsGONetReady(gonetParticipant);
        }

        protected override void Start()
        {
            base.Start();

            // PATH 6: Catch-up mechanism for behaviours that start AFTER participants are already ready
            // This handles:
            // 1. Runtime-added components (via GONetRuntimeComponentInitializer)
            // 2. Scene-defined components when client returns to a scene (scene reload after LoadSceneMode.Single)
            // 3. Late-joiners loading into a scene with existing participants
            // Without this, behaviours miss OnGONetReady events for participants that became ready before the behaviour existed
            bool shouldCatchUp = WasAddedAtRuntime || GONetMain.gonetParticipantByGONetIdMap.Count > 0;

            if (shouldCatchUp)
            {
                int caughtUpCount = 0;
                // Call OnGONetReady for ALL ready participants, not just this component's participant
                // This matches the behavior of OnDeserializeInitAllCompletedGNPEvent which broadcasts to all behaviours
                foreach (var kvp in GONetMain.gonetParticipantByGONetIdMap)
                {
                    GONetParticipant participant = kvp.Value;
                    if (GONetMain.IsGONetReady(participant))
                    {
                        try
                        {
                            OnGONetReady(participant);
                            caughtUpCount++;
                        }
                        catch (Exception ex)
                        {
                            GONetLog.Error($"[GONetBehaviour] Exception in OnGONetReady() catch-up for component '{GetType().Name}' on '{gameObject.name}' with participant '{participant.name}': {ex.Message}\n{ex.StackTrace}");
                        }
                    }
                }

                //if (caughtUpCount > 0)
                //{
                    //GONetLog.Info($"[GONetBehaviour] Component '{GetType().Name}' on '{gameObject.name}' caught up on {caughtUpCount} existing participants (WasAddedAtRuntime: {WasAddedAtRuntime})");
                //}
            }
        }

        public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantEnabled(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantEnabled();
            }
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantStarted();
            }
        }

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <paramref name="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public override void OnGONetParticipantDeserializeInitAllCompleted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantDeserializeInitAllCompleted(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantDeserializeInitAllCompleted();

                // NOTE: OnGONetReady() is now broadcast from GONet.OnDeserializeInitAllCompletedGNPEvent
                // to ALL behaviours for ALL participants (not just own participant).
                // This ensures every behaviour gets notified when any participant becomes ready.
                // Runtime-added components still get OnGONetReady() called from Start() if already ready.
            }
        }

        public override void OnGONetReady(GONetParticipant gonetParticipant)
        {
            base.OnGONetReady(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetReady();

                // CRITICAL: Notify the RPC system that this component is ready to receive RPCs
                // This triggers processing of any deferred RPCs that arrived before this component was added
                GONetEventBus.OnComponentReadyToReceiveRpcs(gonetParticipant);
            }
        }

        public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantDisabled(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipantDisabled();
            }
        }

        public override void OnGONetParticipant_OwnerAuthorityIdSet(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipant_OwnerAuthorityIdSet(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetParticipant_OwnerAuthorityIdSet();
            }
        }

        public virtual void OnGONetParticipantEnabled() { }

        /// <summary>
        /// IMPORTANT: When this is called, this is the first time it is certain that the 
        ///            <see cref="GONetParticipant.GONetId"/> value is fully assigned!
        /// </summary>
        public virtual void OnGONetParticipantStarted() { }

        /// <summary>
        /// NOTE: This is guaranteed to be called after the <see cref="GONetLocal"/> associated with the <see cref="gonetParticipant"/> is available
        ///       in <see cref="GONetLocal.LookupByAuthorityId"/>.
        /// </summary>
        public virtual void OnGONetParticipantDeserializeInitAllCompleted() { }

        public virtual void OnGONetParticipantDisabled() { }

        public virtual void OnGONetParticipant_OwnerAuthorityIdSet() { }

        /// <summary>
        /// <para><b>THE UNIFIED INITIALIZATION HOOK</b> - Called when GONet is fully initialized and ready for use.</para>
        /// <para>This is the RECOMMENDED hook for initializing GONetParticipantCompanionBehaviour components.</para>
        ///
        /// <para><b>GUARANTEED when this is called:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="GONetParticipant.GONetId"/> is assigned (non-zero)</description></item>
        ///   <item><description><see cref="GONetParticipant.OwnerAuthorityId"/> is assigned</description></item>
        ///   <item><description><see cref="GONetMain.IsServer"/> and <see cref="GONetMain.MyAuthorityId"/> are valid</description></item>
        ///   <item><description><see cref="GONetMain.IsClientVsServerStatusKnown"/> is true (client/server role is determined)</description></item>
        ///   <item><description>If client: <see cref="GONetClient.IsInitializedWithServer"/> is true (fully connected and initialized)</description></item>
        ///   <item><description><see cref="GONetLocal"/> instances are available in <see cref="GONetLocal.LookupByAuthorityId"/></description></item>
        ///   <item><description>This participant's <see cref="GONetLocal"/> is registered in the lookup dictionary</description></item>
        ///   <item><description>All auto-magical syncs are initialized</description></item>
        ///   <item><description>RPCs can be called safely</description></item>
        /// </list>
        ///
        /// <para><b>Works in ALL scenarios:</b></para>
        /// <list type="bullet">
        ///   <item><description><b>Design-time:</b> Component present in scene - called after <see cref="OnGONetParticipantDeserializeInitAllCompleted()"/></description></item>
        ///   <item><description><b>Runtime:</b> Component added via <see cref="GONetRuntimeComponentInitializer"/> - called from <see cref="Start()"/> (RECOMMENDED method)</description></item>
        /// </list>
        ///
        /// <para><b>CRITICAL:</b> If adding components at runtime, you MUST use <see cref="GONetRuntimeComponentInitializer"/>.
        /// Using Unity's AddComponent() directly is NOT officially supported and may cause lifecycle issues.</para>
        ///
        /// <para><b>TIP:</b> Use <see cref="WasAddedAtRuntime"/> to determine if this component was added at runtime vs design-time if you need
        /// different initialization logic for each case.</para>
        /// </summary>
        public virtual void OnGONetReady() { }

        /// <summary>
        /// <para><b>EARLY FRAME UPDATE - Called after this companion's GONetParticipant is fully ready.</b></para>
        /// <para>This is a framework-provided update loop that runs EARLY in the frame (before most Update() methods).</para>
        ///
        /// <para><b>⭐ PERFORMANCE: HIGHLY PREFERRED over Unity's Update()</b></para>
        /// <list type="bullet">
        ///   <item><description><b>Unity Update():</b> N objects = N Update() calls registered with Unity (linear overhead, native→managed bridge per call)</description></item>
        ///   <item><description><b>GONet pattern:</b> N objects = 1 Update() call (constant overhead, centralized C# iteration)</description></item>
        ///   <item><description><b>Zero overhead if not overridden:</b> Static per-type reflection cache ensures empty implementations are never called</description></item>
        ///   <item><description><b>One-time reflection cost:</b> Only first instance of each type uses reflection to detect override</description></item>
        /// </list>
        ///
        /// <para><b>⏱️ TIMING: Runs at END of GONetMain.Update() (called from GONetGlobal.Update() at priority -32000)</b></para>
        /// <list type="bullet">
        ///   <item><description>Executes BEFORE most Update() methods (early frame)</description></item>
        ///   <item><description>Good for: Game logic, movement, input processing</description></item>
        ///   <item><description>Other scripts can see your changes this frame</description></item>
        ///   <item><description>⚠️ Bypasses Unity's Script Execution Order settings - ALL UpdateAfterGONetReady() calls run at same priority (-32000)</description></item>
        /// </list>
        ///
        /// <para><b>✅ GUARANTEED when this is called:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="OnGONetReady()"/> has fired for this companion's participant</description></item>
        ///   <item><description><see cref="GONetParticipant.GONetId"/> is assigned (non-zero)</description></item>
        ///   <item><description><see cref="GONetParticipant.OwnerAuthorityId"/> is assigned</description></item>
        ///   <item><description>All initialization values set in OnGONetReady are populated</description></item>
        ///   <item><description>NO defensive checks needed (framework guarantees safety)</description></item>
        /// </list>
        ///
        /// <para><b>🔄 ALTERNATIVES:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="FixedUpdateAfterGONetReady()"/> - For physics-based movement (runs at physics timestep)</description></item>
        ///   <item><description><see cref="LateUpdateAfterGONetReady()"/> - For camera follow, UI, finalization (runs late in frame)</description></item>
        ///   <item><description>Defensive Update() - When you need precise Script Execution Order control (see SpawnTestBeacon.cs example)</description></item>
        /// </list>
        ///
        /// <para><b>EXAMPLE USAGE:</b></para>
        /// <code>
        /// public class Projectile : GONetParticipantCompanionBehaviour
        /// {
        ///     private Vector3 movementDirection;
        ///     public float speed = 10f;
        ///
        ///     public override void OnGONetReady()
        ///     {
        ///         base.OnGONetReady();
        ///         movementDirection = transform.forward; // Initialize state
        ///     }
        ///
        ///     internal override void UpdateAfterGONetReady()
        ///     {
        ///         // NO defensive checks needed - guaranteed movementDirection is initialized
        ///         if (IsMine)
        ///         {
        ///             transform.position += movementDirection * Time.deltaTime * speed;
        ///         }
        ///     }
        /// }
        /// </code>
        ///
        /// <para>See ONGONETREADY_LIFECYCLE_DESIGN.md for detailed pattern comparison and frame timeline visualization.</para>
        /// </summary>
        internal virtual void UpdateAfterGONetReady() { }

        /// <summary>
        /// <para><b>PHYSICS FRAME UPDATE - Called after this companion's GONetParticipant is fully ready.</b></para>
        /// <para>This is a framework-provided update loop that runs at Unity's FIXED TIMESTEP (physics rate).</para>
        ///
        /// <para><b>⭐ PERFORMANCE: HIGHLY PREFERRED over Unity's FixedUpdate()</b></para>
        /// <list type="bullet">
        ///   <item><description><b>Unity FixedUpdate():</b> N objects = N FixedUpdate() calls registered with Unity (linear overhead)</description></item>
        ///   <item><description><b>GONet pattern:</b> N objects = 1 FixedUpdate() call (constant overhead, centralized C# iteration)</description></item>
        ///   <item><description><b>Zero overhead if not overridden:</b> Static per-type reflection cache ensures empty implementations are never called</description></item>
        /// </list>
        ///
        /// <para><b>⏱️ TIMING: Runs in GONetMain.FixedUpdate_AfterGONetReady() (called from GONetGlobal.FixedUpdate())</b></para>
        /// <list type="bullet">
        ///   <item><description>Runs at Unity's fixed timestep (default: 50Hz / 0.02 seconds)</description></item>
        ///   <item><description>Can run multiple times per frame or zero times per frame (depends on frame time)</description></item>
        ///   <item><description>Good for: Physics-based movement, forces, rigid body manipulation, deterministic simulation</description></item>
        ///   <item><description>Runs independently of frame rate for consistent physics behavior</description></item>
        /// </list>
        ///
        /// <para><b>✅ GUARANTEED when this is called:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="OnGONetReady()"/> has fired for this companion's participant</description></item>
        ///   <item><description>All GONet initialization is complete</description></item>
        ///   <item><description>NO defensive checks needed</description></item>
        /// </list>
        ///
        /// <para><b>🔄 ALTERNATIVES:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="UpdateAfterGONetReady()"/> - For frame-based movement (runs early in frame)</description></item>
        ///   <item><description><see cref="LateUpdateAfterGONetReady()"/> - For late-frame logic (runs after all updates)</description></item>
        /// </list>
        ///
        /// <para><b>EXAMPLE USAGE:</b></para>
        /// <code>
        /// public class PhysicsCharacter : GONetParticipantCompanionBehaviour
        /// {
        ///     private Rigidbody rb;
        ///     public float moveForce = 10f;
        ///
        ///     public override void OnGONetReady()
        ///     {
        ///         base.OnGONetReady();
        ///         rb = GetComponent&lt;Rigidbody&gt;();
        ///     }
        ///
        ///     internal override void FixedUpdateAfterGONetReady()
        ///     {
        ///         if (IsMine)
        ///         {
        ///             Vector3 moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        ///             rb.AddForce(moveInput * moveForce, ForceMode.Force);
        ///         }
        ///     }
        /// }
        /// </code>
        ///
        /// <para>See ONGONETREADY_LIFECYCLE_DESIGN.md for detailed pattern comparison.</para>
        /// </summary>
        internal virtual void FixedUpdateAfterGONetReady() { }

        /// <summary>
        /// <para><b>LATE FRAME UPDATE - Called after this companion's GONetParticipant is fully ready.</b></para>
        /// <para>This is a framework-provided update loop that runs LATE in the frame (after all Update() methods).</para>
        ///
        /// <para><b>⭐ PERFORMANCE: HIGHLY PREFERRED over Unity's LateUpdate()</b></para>
        /// <list type="bullet">
        ///   <item><description><b>Unity LateUpdate():</b> N objects = N LateUpdate() calls registered with Unity (linear overhead)</description></item>
        ///   <item><description><b>GONet pattern:</b> N objects = 1 LateUpdate() call (constant overhead, centralized C# iteration)</description></item>
        ///   <item><description><b>Zero overhead if not overridden:</b> Static per-type reflection cache ensures empty implementations are never called</description></item>
        /// </list>
        ///
        /// <para><b>⏱️ TIMING: Runs in GONetMain.Update_DoTheHeavyLifting_IfAppropriate() (called from GONetLocal.LateUpdate() at priority +32000)</b></para>
        /// <list type="bullet">
        ///   <item><description>Executes AFTER all Update() methods and most LateUpdate() methods (late frame)</description></item>
        ///   <item><description>Good for: Camera follow, UI updates, finalization logic, network sync collection</description></item>
        ///   <item><description>Changes won't be visible to other scripts until next frame</description></item>
        ///   <item><description>Ideal for "observing" final state after all movement/logic has completed</description></item>
        /// </list>
        ///
        /// <para><b>✅ GUARANTEED when this is called:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="OnGONetReady()"/> has fired for this companion's participant</description></item>
        ///   <item><description>All GONet initialization is complete</description></item>
        ///   <item><description>NO defensive checks needed</description></item>
        /// </list>
        ///
        /// <para><b>🔄 ALTERNATIVES:</b></para>
        /// <list type="bullet">
        ///   <item><description><see cref="UpdateAfterGONetReady()"/> - For early-frame game logic (runs before most Update() methods)</description></item>
        ///   <item><description><see cref="FixedUpdateAfterGONetReady()"/> - For physics-based movement (runs at physics timestep)</description></item>
        /// </list>
        ///
        /// <para><b>EXAMPLE USAGE:</b></para>
        /// <code>
        /// public class CameraFollow : GONetParticipantCompanionBehaviour
        /// {
        ///     private Transform target;
        ///     public Vector3 offset = new Vector3(0, 5, -10);
        ///
        ///     public override void OnGONetReady()
        ///     {
        ///         base.OnGONetReady();
        ///         target = transform; // Follow this object
        ///     }
        ///
        ///     internal override void LateUpdateAfterGONetReady()
        ///     {
        ///         // Runs AFTER all movement is complete - smooth camera follow
        ///         Camera.main.transform.position = target.position + offset;
        ///     }
        /// }
        /// </code>
        ///
        /// <para>See ONGONETREADY_LIFECYCLE_DESIGN.md for detailed pattern comparison and frame timeline visualization.</para>
        /// </summary>
        internal virtual void LateUpdateAfterGONetReady() { }

        #region RPC Support
        // 0 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc(string methodName)
        {
            EventBus.CallRpcInternal(this, methodName);
        }

        // 1 parameter
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1>(string methodName, T1 arg1)
        {
            EventBus.CallRpcInternal(this, methodName, arg1);
        }

        // 2 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1, T2>(string methodName, T1 arg1, T2 arg2)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2);
        }

        // 3 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1, T2, T3>(string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3);
        }

        // 4 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1, T2, T3, T4>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3, arg4);
        }

        // 5 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1, T2, T3, T4, T5>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3, arg4, arg5);
        }

        // 6 parameters
        /// <summary>
        /// NOTE: This method does NOT USE reflection, even though you're passing in a string <paramref name="methodName"/>. 
        ///       Instead, generated code and dictionary lookups are used.
        /// </summary>
        protected void CallRpc<T1, T2, T3, T4, T5, T6>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        // 7 parameters
        protected void CallRpc<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        // 8 parameters
        /// <summary>
        /// Invokes a fire-and-forget Remote Procedure Call (RPC) on this <see cref="GONetParticipant"/> across the network.
        /// This method is for RPCs that return <c>void</c> or <see cref="Task"/> without a result value.
        /// <remarks>
        /// <para><b>Performance Note:</b> This method does NOT use reflection. Instead, generated code and dictionary lookups are used for optimal performance.</para>
        /// <para>
        /// <b>methodName</b>: The name of the RPC method to invoke. Use <c>nameof(YourMethodName)</c> for compile-time safety, better refactoring support and to avoid string literal garbage/typos.
        /// </para>
        /// 
        /// <para><b>Required Attributes:</b></para>
        /// <para>The target method MUST be decorated with ONE of these attributes:</para>
        /// <list type="bullet">
        ///   <item><see cref="ServerRpcAttribute"/> - Client → Server (optionally relayed to other clients)</item>
        ///   <item><see cref="ClientRpcAttribute"/> - Server → All clients</item>
        ///   <item><see cref="TargetRpcAttribute"/> - Server → Specific client(s)</item>
        /// </list>
        /// 
        /// <para><b>Routing Behavior by RPC Type:</b></para>
        /// <list type="table">
        ///   <listheader>
        ///     <term>RPC Type</term>
        ///     <description>Routing Behavior</description>
        ///   </listheader>
        ///   <item>
        ///     <term><see cref="ServerRpcAttribute"/></term>
        ///     <description>
        ///       Sent from client to server. Set <see cref="GONetRpcAttribute.IsMineRequired"/>=<c>false</c> to allow any client to call.
        ///       Can relay to other clients using <see cref="ServerRpcAttribute.Relay"/> property (<see cref="RelayMode.None"/>/<see cref="RelayMode.Others"/>/<see cref="RelayMode.All"/>/<see cref="RelayMode.Owner"/>).
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="ClientRpcAttribute"/></term>
        ///     <description>Can only be called by server. Broadcasts to all connected clients.</description>
        ///   </item>
        ///   <item>
        ///     <term><see cref="TargetRpcAttribute"/></term>
        ///     <description>
        ///       Can only be called by server. Routes based on <see cref="TargetRpcAttribute.Target"/> property:
        ///       <see cref="RpcTarget.Owner"/>, <see cref="RpcTarget.Others"/>, <see cref="RpcTarget.All"/>, or <see cref="RpcTarget.SpecificAuthority"/>.
        ///     </description>
        ///   </item>
        /// </list>
        /// 
        /// <para><b>Targeting Specific Authorities</b> (<see cref="TargetRpcAttribute"/> with <see cref="RpcTarget.SpecificAuthority"/>):</para>
        /// <para>Option 1 - First parameter is authority ID:</para>
        /// <code>
        /// [TargetRpc(RpcTarget.SpecificAuthority)]
        /// void NotifyPlayer(ushort targetAuthorityId, string message) { }
        /// // Usage: CallRpc(nameof(NotifyPlayer), playerAuthId, "Hello!")
        /// </code>
        /// <para>Option 2 - Use property/field name:</para>
        /// <code>
        /// public ushort CurrentOwnerId { get; set; }
        /// 
        /// [TargetRpc(nameof(CurrentOwnerId))]
        /// void NotifyOwner(string message) { }
        /// // Usage: CallRpc(nameof(NotifyOwner), "Item claimed!")
        /// </code>
        /// 
        /// <para><b>Common Usage Patterns:</b></para>
        /// <code>
        /// // ServerRpc - Client requests action
        /// [ServerRpc(IsMineRequired = false)]
        /// void RequestPickup(int itemId) { /* validate and apply */ }
        /// CallRpc(nameof(RequestPickup), 42);
        /// 
        /// // ClientRpc - Server broadcasts event
        /// [ClientRpc]
        /// void OnPlayerScored(ushort playerId, int points) { }
        /// if (GONetMain.IsServer) 
        ///     CallRpc(nameof(OnPlayerScored), playerId, 10);
        /// 
        /// // TargetRpc - Server notifies specific client
        /// [TargetRpc(RpcTarget.SpecificAuthority)]
        /// void ShowDamage(ushort targetAuth, float damage) { }
        /// if (GONetMain.IsServer) 
        ///     CallRpc(nameof(ShowDamage), victimId, 25f);
        /// </code>
        /// </remarks>
        /// </summary>
        /// <seealso cref="CallRpcAsync{TResult}(string)"/>
        /// <seealso cref="ServerRpcAttribute"/>
        /// <seealso cref="ClientRpcAttribute"/>
        /// <seealso cref="TargetRpcAttribute"/>
        protected void CallRpc<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            EventBus.CallRpcInternal(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        // Async versions with return values - generic pushed to EventBus
        protected async Task<TResult> CallRpcAsync<TResult>(string methodName)
        {
            return await EventBus.CallRpcInternalAsync<TResult>(this, methodName);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1>(string methodName, T1 arg1)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1>(this, methodName, arg1);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2>(string methodName, T1 arg1, T2 arg2)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2>(this, methodName, arg1, arg2);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3>(string methodName, T1 arg1, T2 arg2, T3 arg3)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3>(this, methodName, arg1, arg2, arg3);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3, T4>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3, T4>(this, methodName, arg1, arg2, arg3, arg4);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3, T4, T5>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5>(this, methodName, arg1, arg2, arg3, arg4, arg5);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3, T4, T5, T6>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6>(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7>(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        protected async Task<TResult> CallRpcAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            return await EventBus.CallRpcInternalAsync<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(this, methodName, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }

        /// <summary>
        /// Retrieves the complete validation report for a previously validated TargetRpc call.
        /// </summary>
        /// <param name="reportId">The unique identifier of the validation report, obtained from RpcDeliveryReport.ValidationReportId</param>
        /// <returns>
        /// The full RpcValidationResult containing detailed validation information including:
        /// - AllowedTargets: Array of authority IDs that were allowed to receive the RPC
        /// - DeniedTargets: Array of authority IDs that were denied
        /// - DenialReason: Detailed explanation of why targets were denied
        /// - ModifiedData: The modified message data if the validator transformed it
        /// Returns null if the report ID is invalid, expired, or if called from a client.
        /// </returns>
        /// <remarks>
        /// This method can is called from the client and run on the server. Validation reports are stored temporarily
        /// and expire after approximately 60 seconds or when the storage limit (1000 reports) is exceeded.
        /// Use this for debugging validation issues or retrieving detailed information about why
        /// certain targets were denied or how messages were modified during validation.
        /// </remarks>
        /// <example>
        /// <code>
        /// var deliveryReport = await SendTeamMessageConfirmed("Hello team!");
        /// if (deliveryReport.ValidationReportId != 0)
        /// {
        ///     var fullReport = await GetFullRpcValidationReport(deliveryReport.ValidationReportId);
        ///     if (fullReport.HasValue)
        ///     {
        ///         Debug.Log($"Denied to {fullReport.Value.DeniedCount} targets: {fullReport.Value.DenialReason}");
        ///     }
        /// }
        /// </code>
        /// </example>
        [ServerRpc]
        public async Task<RpcValidationResult?> GetFullRpcValidationReport(ulong reportId)
        {
            if (GONetMain.IsServer && EventBus.TryGetStoredRpcValidationReport(reportId, out RpcValidationResult report))
            {
                // Could add permission checks here - maybe only the original caller can retrieve
                return report;
            }
            return null;
        }
        #endregion
    }
}
