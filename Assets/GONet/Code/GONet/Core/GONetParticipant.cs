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
using System.Runtime.CompilerServices;
using GONet.Generation;
using GONet.Serializables;
using GONet.Utils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using GONetCodeGenerationId = System.Byte;

namespace GONet
{
    /// <summary>
    /// This is required to be present on any <see cref="GameObject"/> you want to have participate in GONet activities.
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode, DefaultExecutionOrder(-199)]
    public sealed class GONetParticipant : MonoBehaviour
    {
        #region constants

        /// <summary>
        /// This represents the index inside <see cref="GONet.Generation.GONetParticipant_ComponentsWithAutoSyncMembers.ComponentMemberNames_By_ComponentTypeFullName"/>
        /// </summary>
        internal const byte ASSumed_GONetId_INDEX = 0;

        public const uint GONetIdRaw_Unset = 0;
        public const uint GONetId_Unset = 0;
        public const uint GONetId_Raw_MaxValue = (uint.MaxValue << GONET_ID_BIT_COUNT_UNUSED) >> GONET_ID_BIT_COUNT_UNUSED;

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public const GONetCodeGenerationId CodeGenerationId_Unset = 0;

        #endregion

        [SerializeField]
        internal string UnityGuid;

        private GONetCodeGenerationId? cachedCodeGenerationId;

        public GONetCodeGenerationId CodeGenerationId
        {
            get
            {
                if (!cachedCodeGenerationId.HasValue)
                {
                    cachedCodeGenerationId = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(this).CodeGenerationId;
                }
                return cachedCodeGenerationId.Value;
            }
            internal set
            {
                GONetSpawnSupport_Runtime.GetDesignTimeMetadata(this).CodeGenerationId = value;
                cachedCodeGenerationId = value; // Update cache when value is set
            }
        }

        [Serializable]
        public class AnimatorControllerParameter
        {
            [SerializeField, HideInInspector]
            public AnimatorControllerParameterType valueType;

            [SerializeField]
            public bool isSyncd;
        }

        /// <summary>
        /// the key is the parameter name, the value is the other available info for that parameter
        /// </summary>
        [Serializable]
        public class AnimatorControllerParameterMap : SerializableDictionary<string, AnimatorControllerParameter> { }

        /// <summary>
        /// IMPORTANT: Do NOT touch this.  It is GONet internal.
        /// </summary>
        [SerializeField, HideInInspector]
        public AnimatorControllerParameterMap animatorSyncSupport;

        public const int OWNER_AUTHORITY_ID_BIT_COUNT_USED = 10;
        public const int OWNER_AUTHORITY_ID_BIT_COUNT_UNUSED = 16 - OWNER_AUTHORITY_ID_BIT_COUNT_USED;

        public const int GONET_ID_BIT_COUNT_UNUSED = OWNER_AUTHORITY_ID_BIT_COUNT_USED;
        public const int GONET_ID_BIT_COUNT_USED = 32 - GONET_ID_BIT_COUNT_UNUSED;

        ushort ownerAuthorityId = GONetMain.OwnerAuthorityId_Unset;
        /// <summary>
        /// <para>This is set to a value that represents which machine in the game spawned this instance.</para>
        /// <para>IMPORTANT: Up until some time during <see cref="Start"/>, this value will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown.  Once the owner is known, this value will change and the <see cref="SyncEvent_GONetParticipant_OwnerAuthorityId"/> event will fire (i.e., you should call <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/> on <see cref="GONetMain.EventBus"/>).</para>
        /// <para>
        /// If the corresponding <see cref="GameObject"/> is included in the/a Unity scene, the owner will be considered the server
        /// and a value of <see cref="OwnerAuthorityId_Server"/> will be used.
        /// </para>
        /// </summary>
        [GONetAutoMagicalSync(
            GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY,
            SyncChangesEverySeconds = AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, // important that this gets immediately communicated when it changes to avoid other changes related to this participant possibly getting processed before this required prerequisite assignment is made (i.e., other end will not be able to correlate the other changes to this participant if this has not been processed yet)
            ProcessingPriority_GONetInternalOverride = int.MaxValue - 1,
            MustRunOnUnityMainThread = true)]
        public ushort OwnerAuthorityId
        {
            get => ownerAuthorityId;
            internal set
            {
                ushort previous = ownerAuthorityId;
                ownerAuthorityId = value;
                OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(true, gonetId);

                if (ownerAuthorityId == GONetMain.MyAuthorityId)
                {
                    WasMineAtAnyPoint = true;
                }

                if (previous != GONetMain.OwnerAuthorityId_Unset && ownerAuthorityId != GONetMain.OwnerAuthorityId_Unset && previous != ownerAuthorityId)
                {
                    OwnerAuthorityId_LastChangedElapsedSeconds = GONetMain.Time.ElapsedSeconds;
                }
            }
        }

        const double OwnerAuthorityId_LastChangedElapsedSeconds_Unset = double.MinValue;

        /// <summary>
        /// This only gets set when it changed from non-<see cref="GONetMain.OwnerAuthorityId_Unset"/> value to another.
        /// </summary>
        public double OwnerAuthorityId_LastChangedElapsedSeconds { get; private set; } = OwnerAuthorityId_LastChangedElapsedSeconds_Unset;

        /// <summary>
        /// <para>Let's you know if the <see cref="OwnerAuthorityId"/> has changed from non-<see cref="GONetMain.OwnerAuthorityId_Unset"/> value to another at some point, perhaps multiple times.</para>
        /// <para>See <see cref="OwnerAuthorityId_LastChangedElapsedSeconds"/> to know when the last change like this occurred.</para>
        /// <para>This would be true if <see cref="GONetMain.Server_AssumeAuthorityOver(GONetParticipant)"/> was called in this.</para>
        /// </summary>
        public bool HasChangedAuthorityAtSomePoint => OwnerAuthorityId_LastChangedElapsedSeconds != OwnerAuthorityId_LastChangedElapsedSeconds_Unset;

        /// <summary>
        /// <para>IMPORTANT: Up until some time during <see cref="Start"/>, the value of <see cref="OwnerAuthorityId"/> will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown, which means this method will return false for everyone (even the actual owner).  Once the owner is known, <see cref="GONetParticipant.OwnerAuthorityId"/> value will change and the <see cref="SyncEvent_GONetParticipant_OwnerAuthorityId"/> event will fire (i.e., you should call <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/> on <see cref="EventBus"/>)</para>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// </summary>
        public bool IsMine => GONetMain.IsMine(this);

        /// <summary>
        /// <para>This might be valuable to know for client side GNPs that have since been transferred over to server authority (via <see cref="GONetMain.Server_AssumeAuthorityOver(GONetParticipant)"/>).</para>
        /// <para>In that case (which is not necessary the case even when this is true), <see cref="IsMine"/> will return false, and this will return true - NOTE: for that exact semantic, use <see cref="IsNoLongerMine"/> instead of this to be more clear.</para>
        /// <para>Although, it is important to realize both <see cref="IsMine"/> can be true and this be true at same time (i.e., authority was never transferred to server).</para>
        /// <para>See <see cref="IsNoLongerMine"/> for another semantic look at authority transferring.</para>
        /// </summary>
        public bool WasMineAtAnyPoint { get; private set; }

        /// <summary>
        /// <para>This might be valuable to know for client side GNPs that have since been transferred over to server authority (via <see cref="GONetMain.Server_AssumeAuthorityOver(GONetParticipant)"/>).</para>
        /// <para>In that case, <see cref="IsMine"/> will return false, and this will return true.</para>
        /// <para><see cref="IsMine"/> will return false, and this will return true.</para>
        /// </summary>
        public bool IsNoLongerMine => WasMineAtAnyPoint && !IsMine;

        /// <summary>
        /// This is mainly here to support player controlled <see cref="GONetParticipant"/>s (GNPs) in a strict server authoritative setup where a client/player only submits inputs to have
        /// the server process remotely and hopefully manipulate this GNP.
        /// <see cref="IsMine_ToRemotelyControl"/>
        /// </summary>
        [GONetAutoMagicalSync(
            GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY,
            SyncChangesEverySeconds = AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS,
            MustRunOnUnityMainThread = true)]
        public ushort RemotelyControlledByAuthorityId = GONetMain.OwnerAuthorityId_Unset;

        /// <summary>
        /// This is mainly here for player controlled <see cref="GONetParticipant"/>s (GNPs) in a strict server authoritative setup where a client/player only submits inputs to have
        /// the server process remotely and hopefully manipulate this GNP.
        /// Unless people use <see cref="RemotelyControlledByAuthorityId"/> in a strange way, when this is true, <see cref="IsMine"/> will be false.
        /// </summary>
        public bool IsMine_ToRemotelyControl => RemotelyControlledByAuthorityId == GONetMain.MyAuthorityId && GONetMain.MyAuthorityId != GONetMain.OwnerAuthorityId_Unset;

        /// <summary>
        /// <para>
        /// The expectation on setting this to true is the values for <see cref="IsPositionSyncd"/> and <see cref="IsRotationSyncd"/> are true
        /// and the associated <see cref="GameObject"/> has a <see cref="Rigidbody"/> installed on it as well 
        /// and <see cref="Rigidbody.isKinematic"/> is false and if using gravity, <see cref="Rigidbody.useGravity"/> is true.
        /// </para>
        /// <para>
        /// For 2D, GONet looks for the presence of <see cref="Rigidbody2D"/> installed and <see cref="Rigidbody2D.isKinematic"/>.
        /// </para>
        /// <para>
        /// If all that applies, then non-owners (i.e., <see cref="IsMine"/> is false) will have <see cref="Rigidbody.isKinematic"/> set to true and <see cref="Rigidbody.useGravity"/> set to false
        /// so the auto magically sync'd values for position and rotation come from owner controlled actions only.
        /// </para>
        /// <para>IMPORTANT: This is not going have an effect if/when changed during a running game.  This needs to be set during design time.  Maybe a future release will decorate it with <see cref="GONetAutoMagicalSyncAttribute"/>, if people need it.</para>
        /// </summary>
        public bool IsRigidBodyOwnerOnlyControlled;

        /// <summary>
        /// <para>
        /// This is an option (good for projectiles) to deal with there being an inherent delay of <see cref="GONetMain.valueBlendingBufferLeadSeconds"/> from the time a
        /// remote instantiation of this <see cref="GONetParticipant"/> (and <see cref="IsMine"/> is false) occurs and the time auto-magical sync data starts processing for value blending 
        /// (i.e., <see cref="GONetAutoMagicalSyncSettings_ProfileTemplate.ShouldBlendBetweenValuesReceived"/> and <see cref="GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived"/>).
        /// </para>
        /// <para>
        /// When this option is set to true, all <see cref="Renderer"/> components on this (including children) are turned off during the buffer lead time delay and then turned back on.
        /// </para>
        /// <para>
        /// If this option does not exactly suit your needs and you want something similar, then just subscribe using <see cref="GONetMain.EventBus"/> to the <see cref="GONetParticipantStartedEvent"/>
        /// and check if that event's envelope has <see cref="GONetEventEnvelope.IsSourceRemote"/> set to true and you can implement your own option to deal with this situation.
        /// </para>
        /// </summary>
        public bool ShouldHideDuringRemoteInstantiate;

        /// <summary>
        /// <para>
        /// If true, automatically calls <see cref="UnityEngine.Object.DontDestroyOnLoad(UnityEngine.Object)"/> on this GameObject when instantiated.
        /// This ensures the object persists across scene changes.
        /// </para>
        /// <para>
        /// <b>IMPORTANT:</b> This flag must be set BEFORE the GONetParticipant is enabled/started.
        /// For runtime-spawned objects, set this in the prefab or immediately after instantiation before GONet processes it.
        /// For scene-defined objects, set this in the inspector.
        /// </para>
        /// <para>
        /// <b>NOTE:</b> GONet uses this flag to properly track which scene spawns belong to.
        /// If you manually call DontDestroyOnLoad() without setting this flag, GONet may incorrectly
        /// associate the object with a scene during late-joiner synchronization.
        /// </para>
        /// </summary>
        [Tooltip("If true, automatically calls DontDestroyOnLoad on this GameObject when instantiated. Must be set before GONetParticipant is enabled.")]
        public bool AutoDontDestroyOnLoad;

        #region CLIENT LIMBO MODE CONFIGURATION (Advanced - Rarely Needed)

        /// <summary>
        /// CLIENT ONLY: Override project-wide limbo behavior for THIS prefab when GONetId batch is exhausted.
        ///
        /// IMPORTANT: Limbo is RARE - only occurs during extreme rapid spawning (100+ spawns/sec).
        /// Most games will NEVER encounter this. Only configure if spawning at massive rates.
        ///
        /// Leave unchecked to use GONet Project Settings default.
        /// Check to override with custom behavior for this specific prefab.
        /// </summary>
        [Header("Client Batch Limbo Override (Advanced - Rarely Needed)")]
        [Tooltip("Override project-wide limbo behavior for THIS prefab when batch IDs exhausted.\n\n" +
                 "IMPORTANT: Limbo is RARE - only during extreme rapid spawning (100+ spawns/sec).\n\n" +
                 "Unchecked = Use GONet Project Settings default\n" +
                 "Checked = Use custom mode below for this prefab")]
        public bool client_overrideLimboMode = false;

        /// <summary>
        /// CLIENT ONLY: Custom limbo mode for this prefab (only used if client_overrideLimboMode is true).
        /// See <see cref="Client_GONetIdBatchLimboMode"/> for mode descriptions.
        /// </summary>
        [Tooltip("Custom limbo mode for this prefab (only if Override checkbox is checked).\n\n" +
                 "See CLIENT_LIMBO_MODE_IMPLEMENTATION_PLAN.md for details on each mode.")]
        public Client_GONetIdBatchLimboMode client_limboMode = Client_GONetIdBatchLimboMode.InstantiateInLimboWithAutoDisableRenderingAndPhysics;

        #endregion

        #region CLIENT LIMBO STATE TRACKING (Internal - Do Not Modify)

        /// <summary>
        /// CLIENT ONLY: Is this object in "limbo" state?
        /// Limbo = exists locally but has no GONetId (waiting for batch from server).
        /// Object is NOT networked, cannot sync, cannot receive RPCs.
        /// Will "graduate" to networked when batch arrives.
        /// </summary>
        public bool Client_IsInLimbo => client_isInLimbo;

        // INTERNAL TRACKING - DO NOT MODIFY THESE FIELDS
        [NonSerialized] internal bool client_isInLimbo = false;

        // Option 1 tracking (DisableAll):
        [NonSerialized] internal List<MonoBehaviour> client_limboDisabledComponents;

        // Option 2 tracking (DisableRenderingAndPhysics):
        [NonSerialized] internal List<Renderer> client_limboDisabledRenderers;
        [NonSerialized] internal List<Collider> client_limboDisabledColliders;
        [NonSerialized] internal List<Collider2D> client_limboDisabledColliders2D;
        [NonSerialized] internal Rigidbody client_limboRigidbody;
        [NonSerialized] internal Rigidbody2D client_limboRigidbody2D;
        [NonSerialized] internal bool client_limboRigidbodyWasKinematic;
        [NonSerialized] internal RigidbodyType2D client_limboRigidbody2DOriginalType;

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(bool isOwnerAuthorityIdKnownToBeGoodValueNow, uint gonetId_priorToChanges)
        {
            ushort ownerAuthorityId_new = ownerAuthorityId;

            if (!isOwnerAuthorityIdKnownToBeGoodValueNow)
            {
                ushort ownerAuthorityId_asRepresentedInside_gonetId = (ushort)((gonetId << GONET_ID_BIT_COUNT_USED) >> GONET_ID_BIT_COUNT_USED);

                if (ownerAuthorityId_asRepresentedInside_gonetId != GONetMain.OwnerAuthorityId_Unset && ownerAuthorityId_new != ownerAuthorityId_asRepresentedInside_gonetId)
                {
                    /* In games where this happens a lot, it appears in the log a lot and seems unhelpful/spammy:
                    if (ownerAuthorityId_new != GONetMain.OwnerAuthorityId_Unset)
                    {
                        const string CHG = "OwnerAuthorityId changing from a non-unset value to a different non-unset value.  If this is happening due to a call to GONetMain.Server_AssumeAuthorityOver(GNP), then all is well; however, if not.....EXPLAIN yourself!  previous OwnerAuthorityId: ";
                        const string NEW = " new OwnerAuthorityId: ";
                        const string GNS = "<GONet server>";
                        GONetLog.Info(string.Concat(CHG, ownerAuthorityId_new, NEW, ownerAuthorityId_asRepresentedInside_gonetId == GONetMain.OwnerAuthorityId_Server ? GNS : ownerAuthorityId_asRepresentedInside_gonetId.ToString()));
                    }
                    */

                    // big ASSumption here, that if the gonetId contains a non-zero value for authority id and we have both (1) not represented that value inside ownerAuthorityId component and (2) ownerAuthorityId is unset....we are ASSuming gonetId composite contains the real/new value for ownerAuthorityId and we should use it!
                    ownerAuthorityId_new = ownerAuthorityId_asRepresentedInside_gonetId;
                }
            }

            uint gonetId_raw_priorToChanges = (gonetId_priorToChanges >> GONET_ID_BIT_COUNT_UNUSED);
            uint gonetId_raw_new = (gonetId >> GONET_ID_BIT_COUNT_UNUSED);
            uint gonetId_new = unchecked((uint)(gonetId_raw_new << GONET_ID_BIT_COUNT_UNUSED)) | ownerAuthorityId_new;

            /* In games where this happens a lot, it appears in the log a lot and seems unhelpful/spammy:
            if (gonetId_raw_priorToChanges != GONetIdRaw_Unset && gonetId_raw_new != gonetId_raw_priorToChanges)
            {
                const string CHG = "gonetId_raw changing from a non-unset value to a different non-unset value.  If this is happening due to a call to GONetMain.Server_AssumeAuthorityOver(GNP), then all is well; however, if not.....EXPLAIN yourself!  previous gonetId_raw: ";
                const string NEW = " new gonetId_raw: ";
                GONetLog.Info(string.Concat(CHG, gonetId_raw_priorToChanges, NEW, gonetId_raw_new));
            }
            */

            if (GONetIdAtInstantiation == GONetId_Unset && gonetId_raw_new != GONetIdRaw_Unset && ownerAuthorityId_new != GONetMain.OwnerAuthorityId_Unset)
            {
                //GONetLog.Debug("GONetIdAtInstantiation = " + gonetId_new);

                GONetIdAtInstantiation = gonetId_new;
            }

            GONetMain.OnGONetIdAboutToBeSet(gonetId_new, gonetId_raw_new, ownerAuthorityId_new, this);

            ownerAuthorityId = ownerAuthorityId_new;
            gonetId_raw = gonetId_raw_new;
            gonetId = gonetId_new;
        }

        public uint gonetId_raw { get; private set; } = GONetIdRaw_Unset;
        /// <summary>
        /// This is the composite value of <see cref="gonetId_raw"/> and <see cref="ownerAuthorityId"/> smashed together into a single uint value
        /// </summary>
        private uint gonetId = GONetId_Unset;
        /// <summary>
        /// Every instance of <see cref="GONetParticipant"/> will be assigned a unique value to this variable.
        /// IMPORTANT: This is the most important message to process first as data management in GONet relies on it.
        /// </summary>
        [GONetAutoMagicalSync(
            GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY,
            SyncChangesEverySeconds = AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, // important that this gets immediately communicated when it changes to avoid other changes related to this participant possibly getting processed before this required prerequisite assignment is made (i.e., other end will not be able to correlate the other changes to this participant if this has not been processed yet)
            ProcessingPriority_GONetInternalOverride = int.MaxValue,
            CustomSerialize_Type = typeof(GONetId_InitialAssignment_CustomSerializer),
            MustRunOnUnityMainThread = true)]
        public uint GONetId
        {
            get => gonetId;
            internal set
            {
                uint gonetId_previous = gonetId;
                gonetId = value;
                OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(false, gonetId_previous);
            }
        }

        internal delegate void GNP_uint_Changed(GONetParticipant gonetParticipant);
        private event GNP_uint_Changed gonetIdAtInstantiationChanged;

        private bool anyUnhandledChanges_GONetIdAtInstantiationChanged;
        internal void AddGONetIdAtInstantiationChangedHandler(GNP_uint_Changed handler)
        {
            gonetIdAtInstantiationChanged += handler;
            if (anyUnhandledChanges_GONetIdAtInstantiationChanged)
            {
                anyUnhandledChanges_GONetIdAtInstantiationChanged = false;
                handler(this);
            }
        }

        /// <summary>
        /// IMPORTANT: This is INTERNAL bud, so leave it alone!
        /// </summary>
        internal uint _GONetIdAtInstantiation;
        public uint GONetIdAtInstantiation
        { 
            get => _GONetIdAtInstantiation; 
            private set
            { 
                _GONetIdAtInstantiation = value; 
                
                if (gonetIdAtInstantiationChanged == null)
                {
                    anyUnhandledChanges_GONetIdAtInstantiationChanged = true;
                }
                else
                {
                    gonetIdAtInstantiationChanged.Invoke(this);
                }
            }
        }

        internal void SetGONetIdFromRemoteInstantiation(InstantiateGONetParticipantEvent instantiateEvent)
        {
            GONetId = instantiateEvent.GONetIdAtInstantiation;
            GONetId = instantiateEvent.GONetId; // TODO when/if replay support is added, this might overwrite what will automatically be done in OnEnable_AssignGONetId_IfAppropriate...maybe that one should be prevented..going to comment there now too
        }

        [GONetAutoMagicalSync]
        public bool IsPositionSyncd = false; // TODO Maybe change to PositionSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        [GONetAutoMagicalSync]
        public bool IsRotationSyncd = false; // TODO Maybe change to RotationSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        public string DesignTimeLocation => GONetSpawnSupport_Runtime.GetDesignTimeMetadata_Location(this);

        /// <summary>
        /// Does this GNP have all the values set from design time operations in order to support this being allowed to be included in the game at runtime?
        /// If not, an error will be logged in Awake() to alert you as to what needs to be done to resolve this.
        /// </summary>
        public bool IsInternallyConfigured => !string.IsNullOrWhiteSpace(DesignTimeLocation) && CodeGenerationId != CodeGenerationId_Unset;

        internal bool IsDesignTimeMetadataInitd { get; set; }

        /// <summary>
        /// <para>If false, the <see cref="GameObject"/> on which this is "installed" was defined in a scene.</para>
        /// <para>If true, the <see cref="GameObject"/> on which this is "installed" was added to the game via a call to some flavor of <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.</para>
        /// <para>IMPORTANT: This will have a value of true for EVERYTHING up until GONet knows for sure if it was defined in a scene or not!  If you need to be informed the moment this value is known to be false instead, register to the event <see cref="TODO FIXME add it here once available"/>.</para>
        /// <para>REASONING: Returning true for things defined in scene.  Well, it will actually change to a value of false by the time the MonoBehaviour lifecycle method Start() is called.  This is due to the timing of Unity's SceneManager.sceneLoaded callback (see https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html).  It is called between OnEnable() and Start().  This callback is what GONet uses to keep track of what was defined in a scene (i.e., WasInstantiated = false) and what is in the game due to Object.Instantiate() having been called programmatically by code (i.e., WasInstantiated = true)</para>
        /// </summary>
        public bool WasInstantiated => wasInstantiatedForce || !GONetMain.WasDefinedInScene(this);
        [SerializeField] internal bool wasInstantiatedForce;

        ulong endOfLineSentTickCountWhenSet_isOKToStartAutoMagicalProcessing = ulong.MaxValue;
        volatile bool isOKToStartAutoMagicalProcessing = false;
        /// <summary>
        /// <para>Before this is set to true, GONet does not know enough about this instance to allow processing of auto magical sync values.
        /// The main reason behind this is how GONet uses core <see cref="MonoBehaviour"/> magic methods and other Unity methods/flows
        /// (e.g. <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>) to manage lifecycle here and GONet will not immediately
        /// (i.e., upon instantiation or call to Awake) know if we need to or even can safely process/send any automagical sync.  GONet
        /// auto propagate instantiation (across the network) stuff is really what necessitates this safety check.</para>
        /// <para>WARNING: Setting this value is delayed by one frame to ensure the other threads reading from this are not executed too quickly and any bytes handed over to the reliable transport are processed first!</para>
        /// </summary>
        internal bool IsOKToStartAutoMagicalProcessing
        {
            get => isOKToStartAutoMagicalProcessing &&
                (endOfLineSentTickCountWhenSet_isOKToStartAutoMagicalProcessing < GONetMain.tickCount_endOfTheLineSendAndSave_Thread);

            set
            {
                isOKToStartAutoMagicalProcessing = value;

                endOfLineSentTickCountWhenSet_isOKToStartAutoMagicalProcessing = value ? GONetMain.tickCount_endOfTheLineSendAndSave_Thread : uint.MaxValue;
            }
        }

        internal bool DidStartMonitoringForAutoMagicalNetworking { get; set; }

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public delegate void GNPDelegate(GONetParticipant gonetParticipant);

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event GNPDelegate DefaultConstructorCalled;
        public GONetParticipant()
        {
            DefaultConstructorCalled?.Invoke(this);
        }

#if UNITY_EDITOR
        private static bool isExitingPlayMode = false;
        private static float timeSinceExitPlayMode = 0f;
        private static float exitPlayModeDelay = 0.5f; // half-second delay after exiting play mode
        public static bool isGenerating;

        static GONetParticipant()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += UpdateExitPlayModeState;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                isExitingPlayMode = true;
            }
            else if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                timeSinceExitPlayMode = 0f; // reset the time counter
            }
        }

        private static void UpdateExitPlayModeState()
        {
            if (isExitingPlayMode)
            {
                timeSinceExitPlayMode += Time.deltaTime;
                if (timeSinceExitPlayMode > exitPlayModeDelay)
                {
                    isExitingPlayMode = false; // delay has passed, safe to reset the flag
                }
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DoesGONetIdContainAllComponents()
        {
            return gonetId_raw != GONetId_Unset && OwnerAuthorityId != GONetMain.OwnerAuthorityId_Unset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DoesGONetIdContainAllComponents(uint gonetId)
        {
            uint gonetId_raw = (gonetId >> GONET_ID_BIT_COUNT_UNUSED);
            ushort ownerAuthorityId = (ushort)((gonetId << GONET_ID_BIT_COUNT_USED) >> GONET_ID_BIT_COUNT_USED);

            return gonetId_raw != GONetId_Unset && ownerAuthorityId != GONetMain.OwnerAuthorityId_Unset;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Conservative logging to understand OnValidate context
            // Removed verbose debug logging that was spamming console

            if (!EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                 !isExitingPlayMode &&
                 !isGenerating)
            {
                // Validation proceeding after guard conditions

                // Check prefab context with detailed logging
                bool isInPrefabPreview = IsInPrefabPreviewMode();
                // Checked prefab preview mode

                // Log scene information
                var scene = gameObject?.scene;
                if (scene.HasValue)
                {
                    // Scene context checked
                }

                // Log prefab information
                if (gameObject != null)
                {
                    bool isPartOfAnyPrefab = UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject);
                    var assetType = UnityEditor.PrefabUtility.GetPrefabAssetType(gameObject);
                    string assetPath = UnityEditor.AssetDatabase.GetAssetPath(gameObject);

                    // Prefab context checked
                }

                // Additional filtering: Only trigger OnValidateEditor if this appears to be a genuine user interaction
                // Skip if we're currently in asset database operations or if the inspector isn't visible
                bool isLikelyUserInteraction = IsLikelyUserInitiatedValidation();
                // User interaction detection completed

                if (isInPrefabPreview || isLikelyUserInteraction)
                {
                    // Triggering OnValidateEditor event
                    // Trigger OnValidateEditor event for property change detection
                    OnValidateEditor?.Invoke(this);
                }
                else
                {
                    // Skipping OnValidateEditor event (not user-initiated)
                }
            }
            else
            {
                // OnValidate skipped due to guard conditions
            }

            /* option used in editor namespace code only....here for reference
                         bool isHappeningDueToExitingPlayModeInEditor =
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange.HasValue &&
                (GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.EnteredEditMode ||
                    GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.ExitingPlayMode) &&
                (GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount == Time.frameCount || // IMPORTANT: this is how we know it "just" changed from play to edit mode...otherwise we could never run the logic we want after exiting the play mode and we start messing around with the hierarchy
                    Time.frameCount == 0);

            if (!EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !isHappeningDueToExitingPlayModeInEditor &&
                !isGenerating)
            {
                //GONetLog.Debug($"GONetParticipant was added or changed on GameObject: {gameObject.name} (Design-time only).");
            }
            */
        }

        /// <summary>
        /// Attempts to determine if the current OnValidate call is likely due to user interaction
        /// rather than internal Unity asset loading/scanning operations.
        /// </summary>
        private bool IsLikelyUserInitiatedValidation()
        {
            // Check if we're currently refreshing or importing assets
            if (EditorApplication.isUpdating)
            {
                return false; // Asset database is updating
            }

            // Check if AssetDatabase is currently refreshing
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
            {
                return false; // We're in an asset import worker process
            }

            // Check if we're in a prefab stage (double-click editing)
            var currentPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currentPrefabStage != null)
            {
                // If we're in prefab stage mode, only count it as user interaction if this GameObject
                // is actually part of the prefab being edited, not just loaded during asset scanning
                try
                {
                    if (currentPrefabStage.IsPartOfPrefabContents(gameObject))
                    {
                        return true; // This GameObject is part of the prefab being edited
                    }
                    else
                    {
                        // This GameObject is NOT part of the prefab being edited - likely Unity's internal loading
                        // during asset scanning while another prefab is open
                        return false;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // Can't check during Awake/OnEnable - be conservative
                    return false;
                }
            }

            // Use a simple heuristic: if the object is selected in the project or hierarchy,
            // and we're not in the middle of a batch operation, it's likely user interaction
            if (UnityEditor.Selection.activeGameObject == gameObject ||
                UnityEditor.Selection.Contains(gameObject))
            {
                return true; // Object is currently selected - likely user interaction
            }

            // Check if this object's asset is selected in the project window
            if (gameObject != null)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(gameObject);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var assetObject = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (UnityEditor.Selection.Contains(assetObject))
                    {
                        return true; // Asset is selected in project window
                    }
                }
            }

            // If none of the above conditions are met, it's likely an internal Unity operation
            return false;
        }

        private bool IsInPrefabPreviewMode()
        {
            // IMPORTANT: Use the SAME logic as IsLikelyUserInitiatedValidation for consistency
            // This prevents the regression loop where these two methods give conflicting results

            // Check if we're in a prefab stage (double-click editing)
            var currentPrefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (currentPrefabStage != null)
            {
                // If we're in prefab stage mode, only count it as preview mode if this GameObject
                // is actually part of the prefab being edited, not just loaded during asset scanning
                try
                {
                    if (currentPrefabStage.IsPartOfPrefabContents(gameObject))
                    {
                        return true; // This GameObject is part of the prefab being edited
                    }
                    else
                    {
                        // This GameObject is NOT part of the prefab being edited - not preview mode
                        return false;
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // Can't check during Awake/OnEnable - be conservative
                    return false;
                }
            }

            // Check if this is single-click inspector editing (no prefab stage active)
            if (UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                // Check if the object is in an unloaded or temporary scene (scene path is null or empty)
                if (!gameObject.scene.isLoaded && string.IsNullOrEmpty(gameObject.scene.path))
                {
                    // Additional check: confirm that there's a valid asset path for the nearest instance root
                    string assetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        return true; // Confirmed to be in Prefab Preview Mode (single-click)
                    }
                }
            }

            return false; // Not in Prefab Preview Mode
        }
#endif

        /// <summary>
        /// NOTE: This will NOT be called when this was added to a GO on a prefab when in Prefab Preview (i.e., in-context editing) mode!
        /// </summary>
        public static event GNPDelegate OnAwakeEditor;

        /// <summary>
        /// Called when OnEnable is invoked in edit mode (design time).
        /// Allows for detection of GONetParticipant enable/disable state changes during development.
        /// </summary>
        public static event GNPDelegate OnEnableEditor;

        /// <summary>
        /// Called when OnDisable is invoked in edit mode (design time).
        /// Allows for detection of GONetParticipant enable/disable state changes during development.
        /// </summary>
        public static event GNPDelegate OnDisableEditor;

        /// <summary>
        /// Called when OnValidate is invoked in edit mode (design time).
        /// Allows for detection of prefab asset property changes during Inspector editing.
        /// </summary>
        public static event GNPDelegate OnValidateEditor;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                StartCoroutine(AwakeCoroutine());
            }
#if UNITY_EDITOR
            else
            {
                OnAwakeEditor?.Invoke(this);
            }
#endif
        }

        private IEnumerator AwakeCoroutine()
        {
            //GONetLog.Debug($"dreetsi cikd wash");
            yield return GONetMain.OnAwake_ApplyDesignTimeMetadata(this);

            if (!IsInternallyConfigured)
            {
                GONetLog.Error($"{nameof(GONetParticipant)} on {nameof(GameObject)} with name:'{name}' is required to have {nameof(DesignTimeLocation)} and {nameof(CodeGenerationId)} set to a valid value.  One/both are not.  Therefore, this will be disabled.  GONet will automatically set these values.  Please ensure the scene has been saved and a game build is created so all server/clients have the new/same information.  If for some reason, this message appears even after creating a new game build, please go to the GONet => GONet Editor Support menu/window and click on 'Refresh GONet code generation' and/or 'Fix GONet Generated Code', then once that completes re-run the game build and try again.  **DEBUG**: DesignTimeLocation='{DesignTimeLocation}', CodeGenerationId={CodeGenerationId}, IsDesignTimeMetadataInitd={IsDesignTimeMetadataInitd}");
                enabled = false;
            }
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event GNPDelegate ResetCalled;
        private void Reset()
        {
            ResetCalled?.Invoke(this);
        }

        private void OnEnable()
        {
            GONetMain.OnEnable_StartMonitoringForAutoMagicalNetworking(this);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                OnEnableEditor?.Invoke(this);
            }
#endif
        }

        struct RigidBodySettings
        {
            public bool isKinematic;
            public bool useGravity;
        }
        Rigidbody myRigidBody;
        RigidBodySettings myRigidbodySettingsAtStart;

        struct RigidBody2DSettings
        {
            public bool isKinematic;
            public bool simulated;
            public RigidbodyType2D bodyType;
        }
        Rigidbody2D myRigidBody2D;
        RigidBody2DSettings myRigidbody2DSettingsAtStart;

        private void Start()
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                //const string GNPS = "GNP.Start() name: ";
                //const string WAS = " WasInstantiated: ";
                //GONetLog.Info(string.Concat(GNPS, gameObject.name, WAS, WasInstantiated));

                if (!WasInstantiated) // NOTE: here in Start is the first point where we know the real/final value of WasInstantiated!
                {
                    IsOKToStartAutoMagicalProcessing = true;
                }

                GONetMain.Start_AutoPropagateInstantiation_IfAppropriate(this);

                if ((myRigidBody = GetComponent<Rigidbody>()) != null)
                {
                    myRigidbodySettingsAtStart.isKinematic = myRigidBody.isKinematic;
                    myRigidbodySettingsAtStart.useGravity = myRigidBody.useGravity;

                    SetRigidBodySettingsConsideringOwner();
                }

                if ((myRigidBody2D = GetComponent<Rigidbody2D>()) != null)
                {
                    myRigidbody2DSettingsAtStart.isKinematic = myRigidBody2D.isKinematic;
                    myRigidbody2DSettingsAtStart.simulated = myRigidBody2D.simulated;
                    myRigidbody2DSettingsAtStart.bodyType = myRigidBody2D.bodyType;

                    SetRigidBodySettingsConsideringOwner();
                }
            }
        }

        /// <summary>
        /// PRE: <see cref="IsRigidBodyOwnerOnlyControlled"/> is known to be true and <see cref="myRigidBody"/> is not null; otherwise this method call will have NO effect.
        /// Call this in Start() and any time <see cref="OwnerAuthorityId"/> changes.
        /// </summary>
        internal void SetRigidBodySettingsConsideringOwner()
        {
            if (IsRigidBodyOwnerOnlyControlled)
            {
                if (myRigidBody != null)
            {
                if (IsMine)
                {
                    myRigidBody.isKinematic = myRigidbodySettingsAtStart.isKinematic;
                    myRigidBody.useGravity = myRigidbodySettingsAtStart.useGravity;
                }
                else
                {
                    myRigidBody.isKinematic = true;
                    myRigidBody.useGravity = false;
                    }
                }

                if (myRigidBody2D != null)
                {
                    if (IsMine)
                    {
                        myRigidBody2D.bodyType = myRigidbody2DSettingsAtStart.bodyType;
                        myRigidBody2D.isKinematic = myRigidbody2DSettingsAtStart.isKinematic;
                        myRigidBody2D.simulated = myRigidbody2DSettingsAtStart.simulated;
                    }
                    else
                    {
                        myRigidBody2D.bodyType = RigidbodyType2D.Kinematic;
                        myRigidBody2D.isKinematic = true;
                        myRigidBody2D.simulated = false;
                    }
                }
            }
        }

        private void OnDisable()
        {
            GONetMain.OnDisable_StopMonitoringForAutoMagicalNetworking(this);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                OnDisableEditor?.Invoke(this);
            }
#endif
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// NOTE: This will NOT be called when this was added to a GO on a prefab when in Prefab Preview (i.e., in-context editing) mode!
        /// </summary>
        public static event GNPDelegate OnDestroyCalled;
        private void OnDestroy()
        {
            GONetMain.OnDestroy_AutoPropagateRemoval_IfAppropriate(this);
            OnDestroyCalled?.Invoke(this);
        }

        public class GONetId_InitialAssignment_CustomSerializer : IGONetAutoMagicalSync_CustomSerializer
        {
            internal static GONetId_InitialAssignment_CustomSerializer Instance => GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONetId_InitialAssignment_CustomSerializer>();

            /// <summary>
            /// This is needed for <see cref="Activator.CreateInstance(Type)"/> in <see cref="GONetAutoMagicalSyncAttribute"/> method to be able to instantiate this.
            /// </summary>
            public GONetId_InitialAssignment_CustomSerializer() { }

            public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
            {
                GONetParticipant gonetParticipant = null;

                bool isOwnershipChange;
                bitStream_readFrom.ReadBit(out isOwnershipChange);
                if (isOwnershipChange)
                {
                    uint gonetIdAtInstantiation = GONetId_Unset;
                    bitStream_readFrom.ReadUInt(out gonetIdAtInstantiation);

                    gonetParticipant = GONetMain.GetGONetParticipantById(gonetIdAtInstantiation);
                }
                else
                {
                    bool wasDefinedInScene;
                    bitStream_readFrom.ReadBit(out wasDefinedInScene);
                    if (wasDefinedInScene)
                    {
                        string fullUniquePath;
                        bitStream_readFrom.ReadString(out fullUniquePath);
                        GameObject gnpGO = HierarchyUtils.FindByFullUniquePath(fullUniquePath); // with current implementation where all spawns/instantiations at runtime get immediately assigned a gonetId, the only time remaining where a full unique path is required to uniquely identify things is stuff defined in scenes TODO FIXME just auto assign things in the scene a "unique scene UID" and use that here instead....less info to send and fool proof unlike this hierarchy util thing!!!
                        if (gnpGO != null)
                        {
                            gonetParticipant = gnpGO.GetComponent<GONetParticipant>();
                        }
                    }
                    // else gonetParticipant will be null and the gonetId will be read below and then nothing else done as there is nothing else to do here
                }

                uint gonetId = GONetId_Unset;
                bitStream_readFrom.ReadUInt(out gonetId); // should we order change list by this id ascending and just put diff from last value?

                if ((object)gonetParticipant != null)
                {
                    //GONetLog.Debug("************ initial assignment of id......what is the value prior to assignment?  authority_id: " + gonetParticipant.OwnerAuthorityId + " raw: " + gonetParticipant.gonetId_raw + " full: " + gonetParticipant.GONetId + "..........newly assigned: " + gonetId);

                    gonetParticipant.GONetId = gonetId;
                }

                return gonetId;
            }

            public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
            {
                bool isOwnershipChange = gonetParticipant.GONetIdAtInstantiation != value.System_UInt32; // IMPORTANT: this is only good logic when the server assumes ownership over client....and no other ownership changes after that will register here
                bitStream_appendTo.WriteBit(isOwnershipChange);

                if (isOwnershipChange)
                {
                    bitStream_appendTo.WriteUInt(gonetParticipant.GONetIdAtInstantiation);
                }
                else
                {
                    bool wasDefinedInScene = GONetMain.WasDefinedInScene(gonetParticipant);
                    bitStream_appendTo.WriteBit(wasDefinedInScene);
                    if (wasDefinedInScene)
                    {
                        string fullUniquePath = HierarchyUtils.GetFullUniquePath(gonetParticipant.gameObject);
                        bitStream_appendTo.WriteString(fullUniquePath);
                    }
                }

                uint gonetId = value.System_UInt32;
                bitStream_appendTo.WriteUInt(gonetId);
            }

            public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
            {
                // do nothing!  TODO consider supporting quantizing even this, but not making sense right now and still want to keep this interface/API
            }

            public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
            {
                return valueA.System_UInt32 == valueB.System_UInt32;
            }
        }
    }
}
