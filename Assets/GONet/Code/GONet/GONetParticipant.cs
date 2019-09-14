/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GONet.Serializables;
using GONet.Utils;
using UnityEngine;

using GONetCodeGenerationId = System.Byte;

namespace GONet
{
    /// <summary>
    /// This is required to be present on any <see cref="GameObject"/> you want to have participate in GONet activities.
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class GONetParticipant : MonoBehaviour
    {
        #region constants

        /// <summary>
        /// This represents the index inside <see cref="GONet.Generation.GONetParticipant_ComponentsWithAutoSyncMembers.ComponentMemberNames_By_ComponentTypeFullName"/>
        /// </summary>
        internal const byte ASSumed_GONetId_INDEX = 0;

        public const uint GONetId_Unset = 0;
        public const uint GONetId_Raw_MaxValue = (uint.MaxValue << GONET_ID_BIT_COUNT_UNUSED) >> GONET_ID_BIT_COUNT_UNUSED;

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public const GONetCodeGenerationId CodeGenerationId_Unset = 0;

        #endregion

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        [SerializeField, HideInInspector]
        public GONetCodeGenerationId codeGenerationId = CodeGenerationId_Unset;

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
            SyncChangesEverySeconds = AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, // important that this gets immediately communicated when it changes to avoid other changes related to this participant possibly getting processed before this required prerequisite assignment is made (i.e., other end will not be able to correlate the other changes to this participant if this has not been processed yet)
            ProcessingPriority_GONetInternalOverride = int.MaxValue - 1,
            MustRunOnUnityMainThread = true)]
        public ushort OwnerAuthorityId
        {
            get => ownerAuthorityId;
            internal set
            {
                ownerAuthorityId = value;
                OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(true);
            }
        }

        /// <summary>
        /// <para>IMPORTANT: Up until some time during <see cref="Start"/>, the value of <see cref="OwnerAuthorityId"/> will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown, which means this method will return false for everyone (even the actual owner).  Once the owner is known, <see cref="GONetParticipant.OwnerAuthorityId"/> value will change and the <see cref="SyncEvent_GONetParticipant_OwnerAuthorityId"/> event will fire (i.e., you should call <see cref="GONetEventBus.Subscribe{T}(GONetEventBus.HandleEventDelegate{T}, GONetEventBus.EventFilterDelegate{T})"/> on <see cref="EventBus"/>)</para>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// </summary>
        public bool IsMine => GONetMain.IsMine(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(bool isOwnerAuthorityIdKnownToBeGoodValueNow)
        {
            if (!isOwnerAuthorityIdKnownToBeGoodValueNow)
            {
                ushort ownerAuthorityId_asRepresentedInside_gonetId = (ushort)((gonetId << GONET_ID_BIT_COUNT_USED) >> GONET_ID_BIT_COUNT_USED);

                if (ownerAuthorityId_asRepresentedInside_gonetId != GONetMain.OwnerAuthorityId_Unset && ownerAuthorityId != ownerAuthorityId_asRepresentedInside_gonetId)
                {
                    if (ownerAuthorityId != GONetMain.OwnerAuthorityId_Unset)
                    {
                        const string CHG = "OwnerAuthorityId changing from a non-unset value to a different non-unset value.  If this is not happening due to a call to GONetMain.Server_AssumeAuthorityOver(GNP), then all is well; however, if not.....EXPLAIN yourself!  previous OwnerAuthorityId: ";
                        const string NEW = " new OwnerAuthorityId: ";
                        const string GNS = "<GONet server>";
                        GONetLog.Info(string.Concat(CHG, ownerAuthorityId, NEW, ownerAuthorityId_asRepresentedInside_gonetId == GONetMain.OwnerAuthorityId_Server ? GNS : ownerAuthorityId_asRepresentedInside_gonetId.ToString()));
                    }

                    // big ASSumption here, that if the gonetId contains a non-zero value for authority id and we have both (1) not represented that value inside ownerAuthorityId component and (2) ownerAuthorityId is unset....we are ASSuming gonetId composite contains the real/new value for ownerAuthorityId and we should use it!
                    ownerAuthorityId = ownerAuthorityId_asRepresentedInside_gonetId;
                }
            }

            gonetId_raw = (gonetId >> GONET_ID_BIT_COUNT_UNUSED);
            gonetId = unchecked((uint)(gonetId_raw << GONET_ID_BIT_COUNT_UNUSED)) | ownerAuthorityId;
        }

        public uint gonetId_raw { get; private set; } = 0;
        /// <summary>
        /// This is the composite value of <see cref="gonetId_raw"/> and <see cref="ownerAuthorityId"/> smashed together into a single uint value
        /// </summary>
        private uint gonetId = GONetId_Unset;
        /// <summary>
        /// Every instance of <see cref="GONetParticipant"/> will be assigned a unique value to this variable.
        /// IMPORTANT: This is the most important message to process first as data management in GONet relies on it.
        /// </summary>
        [GONetAutoMagicalSync(
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
                OnGONetIdComponentChanged_UpdateAllComponents_IfAppropriate(false);

                GONetMain.gonetParticipantByGONetIdMap.Remove(gonetId_previous);
                GONetMain.gonetParticipantByGONetIdMap[gonetId] = this; // TODO first check for collision/overwrite and throw exception....or warning at least!
            }
        }

        [GONetAutoMagicalSync(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT)]
        public bool IsPositionSyncd = false; // TODO Maybe change to PositionSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        [GONetAutoMagicalSync(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT)]
        public bool IsRotationSyncd = false; // TODO Maybe change to RotationSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        /// <summary>
        /// public: Do NOT use this.  It is internal to GONet!
        /// This is ONLY accurate information during design time.  The location can easily change during runtime.
        /// This is used for referential purposes only and mainly for auto-propagate spawn support.
        /// </summary>
        [SerializeField, HideInInspector]
        public string designTimeLocation;
        public string DesignTimeLocation => designTimeLocation;

        /// <summary>
        /// <para>If false, the <see cref="GameObject"/> on which this is "installed" was defined in a scene.</para>
        /// <para>If true, the <see cref="GameObject"/> on which this is "installed" was added to the game via a call to some flavor of <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.</para>
        /// <para>IMPORTANT: This will have a value of true for EVERYTHING up until GONet knows for sure if it was defined in a scene or not!  If you need to be informed the moment this value is known to be false instead, register to the event <see cref="TODO FIXME add it here once available"/>.</para>
        /// <para>REASONING: Returning true for things defined in scene.  Well, it will actually change to a value of false by the time the MonoBehaviour lifecycle method Start() is called.  This is due to the timing of Unity's SceneManager.sceneLoaded callback (see https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html).  It is called between OnEnable() and Start().  This callback is what GONet uses to keep track of what was defined in a scene (i.e., WasInstantiated = false) and what is in the game due to Object.Instantiate() having been called programmatically by code (i.e., WasInstantiated = true)</para>
        /// </summary>
        public bool WasInstantiated => !GONetMain.WasDefinedInScene(this);

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

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event GNPDelegate AwakeCalled;
        private void Awake()
        {
            AwakeCalled?.Invoke(this);
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
        }

        private void Start()
        {
            //const string GNPS = "GNP.Start() name: ";
            //const string WAS = " WasInstantiated: ";
            //GONetLog.Info(string.Concat(GNPS, gameObject.name, WAS, WasInstantiated));

            if (!WasInstantiated) // NOTE: here in Start is the first point where we know the real/final value of WasInstantiated!
            {
                IsOKToStartAutoMagicalProcessing = true;
            }

            GONetMain.Start_AutoPropagateInstantiation_IfAppropriate(this);
        }

        private void OnDisable()
        {
            GONetMain.OnDisable_StopMonitoringForAutoMagicalNetworking(this);
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
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
                string fullUniquePath;
                bitStream_readFrom.ReadString(out fullUniquePath);

                uint GONetId = GONetId_Unset;
                bitStream_readFrom.ReadUInt(out GONetId); // should we order change list by this id ascending and just put diff from last value?

                GameObject gonetParticipantGO = HierarchyUtils.FindByFullUniquePath(fullUniquePath);
                GONetParticipant gonetParticipant = null;
                if ((object)gonetParticipantGO == null)
                {
                    GONetLog.Warning("If this is a client " + (GONetMain.IsClient ? "(and it is)" : "(and...I'll be, but its not and this is the SERVER and you have some worrying to do)") + ", it is possible that the server sent over the GONetId assignment prior to sending over the InstantiateGONetParticipantEvent; HOWEVER, things will work themselves out just fine momentarily when that event arrives here and is processed, because it will contain the GONetId and it will be set at that point.");
                }
                else
                {
                    gonetParticipant = gonetParticipantGO.GetComponent<GONetParticipant>();
                }

                if ((object)gonetParticipant != null)
                {
                    gonetParticipant.GONetId = GONetId;
                }

                return GONetId;
            }

            public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
            {
                string fullUniquePath = HierarchyUtils.GetFullUniquePath(gonetParticipant.gameObject);
                bitStream_appendTo.WriteString(fullUniquePath);

                uint gonetId = value.System_UInt32;
                bitStream_appendTo.WriteUInt(gonetId);
            }
        }
    }
}
