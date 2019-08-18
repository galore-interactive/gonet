/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System;
using System.Collections.Generic;
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

        public delegate void OwnerAuthorityIdChangedDelegate(GONetParticipant gonetParticipant, uint valueOld, uint valueNew);
        public event OwnerAuthorityIdChangedDelegate OwnerAuthorityIdChanged;

        

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

        uint ownerAuthorityId = GONetMain.OwnerAuthorityId_Unset;
        /// <summary>
        /// <para>This is set to a value that represents which machine in the game spawned this instance.</para>
        /// <para>IMPORTANT: Up until some time during <see cref="Start"/>, this value will be <see cref="GONetMain.OwnerAuthorityId_Unset"/> and the owner is essentially unknown.  Once the owner is known, this value will change and the <see cref="OwnerAuthorityIdChanged"/> event will fire.</para>
        /// <para>
        /// If the corresponding <see cref="GameObject"/> is included in the/a Unity scene, the owner will be considered the server
        /// and a value of <see cref="OwnerAuthorityId_Server"/> will be used.
        /// </para>
        /// </summary>
        [GONetAutoMagicalSync(ProcessingPriority_GONetInternalOverride = int.MaxValue - 1, MustRunOnUnityMainThread = true)]
        public uint OwnerAuthorityId
        {
            get { return ownerAuthorityId; }
            internal set
            {
                uint previous = ownerAuthorityId;
                ownerAuthorityId = value;
                if (previous != ownerAuthorityId)
                {
                    OwnerAuthorityIdChanged?.Invoke(this, previous, ownerAuthorityId);
                }
            }
        }

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
            get { return gonetId; }
            internal set
            {
                gonetId = value;
                GONetMain.gonetParticipantByGONetIdMap[value] = this; // TODO first check for collision/overwrite and throw exception....or warning at least!
                //GONetLog.Info("slamile...gonetId: " + gonetId);
            }
        }

        [GONetAutoMagicalSync(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT)]
        public bool IsPositionSyncd = false; // TODO Maybe change to PositionSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        [GONetAutoMagicalSync(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT)]
        public bool IsRotationSyncd = false; // TODO Maybe change to RotationSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        /// <summary>
        /// public: Do NOT use this.  It is internal to GONet!
        /// This is ONLY accurate information during design time.  The location can easily change during runtime.
        /// This is used for referential purposes only and mainly for auto-propogate spawn support.
        /// </summary>
        [SerializeField, HideInInspector]
        public string designTimeLocation;
        public string DesignTimeLocation => designTimeLocation;
        
        public bool WasInstantiated => !GONetMain.WasDefinedInScene(this);

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public delegate void EditorOnlyDelegate(GONetParticipant gonetParticipant);

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event EditorOnlyDelegate EditorOnlyDefaultContructor;
        public GONetParticipant()
        {
            EditorOnlyDefaultContructor?.Invoke(this);
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event EditorOnlyDelegate EditorOnlyAwake;
        private void Awake()
        {
            //GONetLog.Debug("Awake....instanceID: " + GetInstanceID());
            EditorOnlyAwake?.Invoke(this);
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event EditorOnlyDelegate EditorOnlyReset;
        private void Reset()
        {
            EditorOnlyReset?.Invoke(this);
        }

        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public static event EditorOnlyDelegate EditorOnlyOnDestroy;
        private void OnDestroy()
        {
            EditorOnlyOnDestroy?.Invoke(this);
        }

        private void OnEnable()
        {
            //GONetLog.Debug("OnEnable....instanceID: " + GetInstanceID());
            GONetMain.OnEnable_StartMonitoringForAutoMagicalNetworking(this);
        }

        private void Start()
        {
            //GONetLog.Debug("Start....instanceID: " + GetInstanceID());
            GONetMain.Start_AutoPropogateInstantiation_IfAppropriate(this);
        }

        private void OnDisable()
        {
            GONetMain.OnDisable_StopMonitoringForAutoMagicalNetworking(this);
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
                GameObject gonetParticipantGO = HierarchyUtils.FindByFullUniquePath(fullUniquePath);
                GONetParticipant gonetParticipant = gonetParticipantGO.GetComponent<GONetParticipant>();

                uint GONetId = default;
                bitStream_readFrom.ReadUInt(out GONetId); // should we order change list by this id ascending and just put diff from last value?

                gonetParticipant.GONetId = GONetId;

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
