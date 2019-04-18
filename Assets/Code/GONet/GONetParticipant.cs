using System;
using GONet.Utils;
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// This is required to be present on any <see cref="GameObject"/> you want to have participate in GONet activities.
    /// </summary>
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class GONetParticipant : MonoBehaviour
    {
        #region constants

        public const uint OwnerAuthorityId_Unset = 0;
        public const uint OwnerAuthorityId_Server = uint.MaxValue;

        /// <summary>
        /// This represents the index inside <see cref="GONet.Generation.GONetParticipant_ComponentsWithAutoSyncMembers.ComponentMemberNames_By_ComponentTypeFullName"/>
        /// </summary>
        internal const byte ASSumed_GONetId_INDEX = 0;

        public const uint GONetId_Unset = 0;

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public const byte CodeGenerationId_Unset = 0;

        #endregion

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        [SerializeField, HideInInspector]
        public byte codeGenerationId = CodeGenerationId_Unset;

        /// <summary>
        /// This is set to a value that represents which machine in the game spawned this instance.
        /// If the corresponding <see cref="GameObject"/> is included in the/a Unity scene, the owner will be considered the server
        /// and a value of <see cref="OwnerAuthorityId_Server"/> will be used.
        /// </summary>
        [GONetAutoMagicalSync(ProcessingPriority_GONetInternalOverride = int.MaxValue - 1)]
        public uint OwnerAuthorityId { get; internal set; } = OwnerAuthorityId_Unset;

        private uint gonetId = GONetId_Unset;
        /// <summary>
        /// Every instance of <see cref="GONetParticipant"/> will be assigned a unique value to this variable.
        /// IMPORTANT: This is the most important message to process first as data management in GONet relies on it.
        /// </summary>
        [GONetAutoMagicalSync(ProcessingPriority_GONetInternalOverride = int.MaxValue, CustomSerialize_Type = typeof(GONetId_InitialAssignment_CustomSerializer))]
        public uint GONetId
        {
            get { return gonetId; }
            internal set
            {
                gonetId = value;
                GONetMain.gonetParticipantByGONetIdMap[value] = this; // TODO first check for collision/overwrite and throw exception....or warning at least!
            }
        }

        [GONetAutoMagicalSync]
        public bool IsPositionSyncd = false; // TODO Maybe change to PositionSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted

        [GONetAutoMagicalSync]
        public bool IsRotationSyncd = false; // TODO Maybe change to RotationSyncStrategy, defaulting to 'Excluded' if more than 2 options required/wanted


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
            GONetMain.OnEnable_StartMonitoringForAutoMagicalNetworking(this);
        }

        private void OnDisable()
        {
            GONetMain.OnDisable_StopMonitoringForAutoMagicalNetworking(this);
        }

        internal class GONetId_InitialAssignment_CustomSerializer : IGONetAutoMagicalSync_CustomSerializer
        {
            internal static GONetId_InitialAssignment_CustomSerializer Instance => GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONetId_InitialAssignment_CustomSerializer>();

            /// <summary>
            /// This is needed for <see cref="Activator.CreateInstance(Type)"/> in <see cref="GONetAutoMagicalSyncAttribute"/> method to be able to instantiate this.
            /// </summary>
            public GONetId_InitialAssignment_CustomSerializer() { }

            public object Deserialize(Utils.BitStream bitStream_readFrom)
            {
                string fullUniquePath;
                bitStream_readFrom.ReadString(out fullUniquePath);
                GameObject gonetParticipantGO = HierarchyUtils.FindByFullUniquePath(fullUniquePath);
                GONetParticipant gonetParticipant = gonetParticipantGO.GetComponent<GONetParticipant>();

                uint GONetId = default(uint);
                bitStream_readFrom.ReadUInt(out GONetId); // should we order change list by this id ascending and just put diff from last value?

                gonetParticipant.GONetId = GONetId;

                return GONetId;
            }

            public void Serialize(Utils.BitStream bitStream_appendTo, GONetParticipant gonetParticipant, object value)
            {
                string fullUniquePath = HierarchyUtils.GetFullUniquePath(gonetParticipant.gameObject);
                bitStream_appendTo.WriteString(fullUniquePath);

                uint gonetId = (uint)value;
                bitStream_appendTo.WriteUInt(gonetId);
            }
        }
    }
}
