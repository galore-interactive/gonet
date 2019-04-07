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

        public const uint GONetId_Unset = 0;

        /// <summary>
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// </summary>
        public const byte CodeGenerationId_Unset = 0;

        #endregion

        [SerializeField, HideInInspector]
        internal byte codeGenerationId;

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
        [GONetAutoMagicalSync(ProcessingPriority_GONetInternalOverride = int.MaxValue)]
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
    }
}
