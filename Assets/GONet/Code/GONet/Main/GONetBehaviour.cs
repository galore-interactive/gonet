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
using System.Threading.Tasks;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// Provides a base class with commonly used hooks into the GONet API that might be easier to use for beginners before they are familiar with GONet's event api (i.e., <see cref="GONetMain.EventBus"/>).
    /// </summary>
    public abstract class GONetBehaviour : MonoBehaviour
    {
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
                if (gonetParticipant != null && IsGONetFullyReady())
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
        private bool IsGONetFullyReady()
        {
            // Check basic participant initialization
            if (gonetParticipant == null || gonetParticipant.GONetId == 0)
            {
                return false;
            }

            // Check client/server status is known
            if (!GONetMain.IsClientVsServerStatusKnown)
            {
                return false;
            }

            // If we're a client, ensure client instance exists and is fully initialized
            if (GONetMain.IsClient)
            {
                if (GONetMain.GONetClient == null)
                {
                    return false; // Client but no client instance - not ready
                }

                if (!GONetMain.GONetClient.IsInitializedWithServer)
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

            return true;
        }

        protected override void Start()
        {
            base.Start();

            // If added at runtime to already-ready participant, call OnGONetReady() now
            if (WasAddedAtRuntime && IsGONetFullyReady())
            {
                try
                {
                    OnGONetReady(gonetParticipant);
                }
                catch (Exception ex)
                {
                    GONetLog.Error($"[GONetBehaviour] Exception in OnGONetReady() for runtime-added component '{GetType().Name}' on '{gameObject.name}': {ex.Message}\n{ex.StackTrace}");
                }
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

                // Call the unified OnGONetReady() hook for design-time added components
                // Runtime-added components get this called from Start() instead
                if (!WasAddedAtRuntime)
                {
                    try
                    {
                        OnGONetReady(gonetParticipant);
                    }
                    catch (Exception ex)
                    {
                        GONetLog.Error($"[GONetBehaviour] Exception in OnGONetReady() for design-time component '{GetType().Name}' on '{gameObject.name}': {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        public override void OnGONetReady(GONetParticipant gonetParticipant)
        {
            base.OnGONetReady(gonetParticipant);

            if (gonetParticipant == this.gonetParticipant)
            {
                OnGONetReady();
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
