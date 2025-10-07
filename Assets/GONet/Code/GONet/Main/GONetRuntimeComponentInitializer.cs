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
using UnityEngine;

namespace GONet
{
    /// <summary>
    /// <para><b>Purpose:</b> Dynamically adds a <see cref="GONetParticipantCompanionBehaviour"/> component to the persistent
    /// <see cref="GONetGlobal"/> singleton when this scene loads, and optionally removes it when the scene unloads.</para>
    ///
    /// <para><b>Why This Exists:</b> GONet uses a singleton pattern for <see cref="GONetGlobal"/> - the first instance persists
    /// across all scenes via DontDestroyOnLoad, and duplicate instances in other scenes are automatically destroyed.
    /// This means you cannot add scene-specific components directly to GONetGlobal in the scene hierarchy.</para>
    ///
    /// <para><b>Common Use Cases:</b></para>
    /// <list type="bullet">
    ///     <item><description>Adding RPC-based systems (like chat) that need <see cref="GONetParticipantCompanionBehaviour"/> for RPCs</description></item>
    ///     <item><description>Scene-specific GONet functionality that should be added/removed with scene lifecycle</description></item>
    ///     <item><description>Testing scenes standalone (each scene can have its own initializer without conflicts)</description></item>
    /// </list>
    ///
    /// <para><b>How To Use:</b></para>
    /// <list type="number">
    ///     <item><description>Create an empty GameObject in your scene (e.g., "ComponentInitializer")</description></item>
    ///     <item><description>Add this component to the GameObject</description></item>
    ///     <item><description>Use the dropdown in the Inspector to select which component type to add to GONetGlobal</description></item>
    ///     <item><description>Optionally check "Remove On Scene Unload" if the component should only exist while this scene is active</description></item>
    /// </list>
    ///
    /// <para><b>Example Scenario:</b> You want chat functionality in multiple scenes:</para>
    /// <list type="bullet">
    ///     <item><description>Scene A (GONetSample): Add initializer with GONetSampleChatSystem, removeOnSceneUnload = FALSE (chat persists)</description></item>
    ///     <item><description>Scene B (RPCPlayground): Add initializer with GONetSampleChatSystem, removeOnSceneUnload = FALSE (reuses existing)</description></item>
    ///     <item><description>Scene C (BattleArena): Add initializer with GONetSampleChatSystem, removeOnSceneUnload = TRUE (chat only in this scene)</description></item>
    /// </list>
    ///
    /// <para><b>Important Notes:</b></para>
    /// <list type="bullet">
    ///     <item><description>Only types extending <see cref="GONetParticipantCompanionBehaviour"/> are supported (required for RPC functionality)</description></item>
    ///     <item><description>If the component already exists on GONetGlobal, this initializer will NOT claim ownership for removal</description></item>
    ///     <item><description>Multiple initializers can safely target the same component type across different scenes</description></item>
    ///     <item><description>The selected component type must have a parameterless constructor (standard Unity requirement)</description></item>
    /// </list>
    ///
    /// <para><b>CRITICAL - This is NOT a Networked Operation:</b></para>
    /// <para>GONetRuntimeComponentInitializer adds components LOCALLY on each machine - it does NOT synchronize component addition across the network.
    /// Each client and server must have the same initializer in their scene to ensure the component exists on all machines.
    /// This is NOT auto-magical networking - you must design your scenes so all machines execute the same initialization.</para>
    ///
    /// <para><b>CRITICAL - Lifecycle Differences:</b></para>
    /// <para>Components added via GONetRuntimeComponentInitializer have a DIFFERENT lifecycle than design-time components:</para>
    /// <list type="bullet">
    ///     <item><description><b>Design-time components:</b> Receive OnGONetParticipantEnabled → OnGONetParticipantStarted → OnGONetParticipantDeserializeInitAllCompleted → OnGONetReady</description></item>
    ///     <item><description><b>Runtime-added components:</b> ONLY receive OnGONetReady (called from Unity's Start)</description></item>
    /// </list>
    ///
    /// <para><b>RECOMMENDED:</b> Components that may be added at runtime should use <see cref="GONetParticipantCompanionBehaviour.OnGONetReady()"/>
    /// for initialization instead of <see cref="GONetParticipantCompanionBehaviour.OnGONetParticipantStarted()"/>. OnGONetReady() provides the same
    /// guarantees (GONetId assigned, OwnerAuthorityId set, GONetLocal available, RPCs ready) and works correctly in BOTH scenarios.</para>
    ///
    /// <para><b>WARNING:</b> This is the ONLY officially supported way to add GONetParticipantCompanionBehaviour components at runtime.
    /// Using Unity's AddComponent() directly is NOT supported and will cause lifecycle issues.</para>
    /// </summary>
    public class GONetRuntimeComponentInitializer : MonoBehaviour
    {
        /// <summary>
        /// The fully qualified type name of the component to add to GONetGlobal.
        /// This is serialized as a string to maintain compatibility across assembly reloads.
        /// Use the custom inspector dropdown to select the type - do not edit this field directly.
        /// </summary>
        [SerializeField]
        [Tooltip("The GONetParticipantCompanionBehaviour type to add to GONetGlobal. Use the dropdown to select.")]
        private string componentTypeName;

        /// <summary>
        /// <para>Controls whether the component is removed from GONetGlobal when this scene unloads.</para>
        ///
        /// <para><b>When FALSE (default):</b> Component persists across all scenes once added.
        /// Multiple scenes can safely have initializers for the same component - only the first adds it, others skip.</para>
        ///
        /// <para><b>When TRUE:</b> Component is removed when this scene unloads.
        /// Use this for scene-specific functionality that shouldn't persist.</para>
        ///
        /// <para><b>Important:</b> Only the initializer that actually added the component will remove it,
        /// even if multiple initializers exist. This prevents scenes from interfering with each other.</para>
        /// </summary>
        [Tooltip("If true, removes the component from GONetGlobal when this scene unloads. Leave false for persistent components.")]
        public bool removeOnSceneUnload = false;

        /// <summary>
        /// Reference to the component instance that was added by THIS initializer.
        /// Used to track ownership for cleanup. If null, this initializer did not add the component.
        /// </summary>
        private Component addedComponent;

        /// <summary>
        /// Cached Type object resolved from <see cref="componentTypeName"/>.
        /// Resolved once during Start() to avoid repeated reflection lookups.
        /// </summary>
        private Type componentType;

        /// <summary>
        /// <para>Called when the scene containing this initializer loads and becomes active.</para>
        ///
        /// <para>Performs the following steps:</para>
        /// <list type="number">
        ///     <item><description>Resolves the component type from the serialized type name</description></item>
        ///     <item><description>Validates the type extends GONetParticipantCompanionBehaviour</description></item>
        ///     <item><description>Finds the persistent GONetGlobal singleton instance</description></item>
        ///     <item><description>Checks if the component already exists on GONetGlobal</description></item>
        ///     <item><description>Adds the component if not present, or skips if already exists</description></item>
        ///     <item><description>Claims ownership only if this initializer added the component (for later cleanup)</description></item>
        /// </list>
        /// </summary>
        void Start()
        {
            // STEP 1: Resolve the Type object from the serialized type name
            // Type names are stored as strings to survive Unity's serialization across domain reloads
            if (string.IsNullOrEmpty(componentTypeName))
            {
                GONetLog.Warning($"[GONetRuntimeComponentInitializer] No component type selected on GameObject '{gameObject.name}' in scene '{gameObject.scene.name}'. Please select a type in the Inspector.");
                return;
            }

            componentType = Type.GetType(componentTypeName);

            if (componentType == null)
            {
                GONetLog.Error($"[GONetRuntimeComponentInitializer] Failed to resolve type '{componentTypeName}' on GameObject '{gameObject.name}'. The type may have been renamed or deleted.");
                return;
            }

            // STEP 2: Validate that the resolved type is compatible with GONet's RPC system
            // Only GONetParticipantCompanionBehaviour types can be added, as they support RPCs
            if (!typeof(GONetParticipantCompanionBehaviour).IsAssignableFrom(componentType))
            {
                GONetLog.Error($"[GONetRuntimeComponentInitializer] Type '{componentType.Name}' does not extend GONetParticipantCompanionBehaviour. Only GONetParticipantCompanionBehaviour types can be initialized with this component.");
                return;
            }

            // STEP 3: Find the persistent GONetGlobal singleton
            // CRITICAL: Use GONetGlobal.Instance instead of FindObjectOfType to avoid race condition
            // where we might find a duplicate GONetGlobal that's about to be destroyed (OwnerAuthorityId = 0)
            GONetGlobal gonetGlobal = GONetGlobal.Instance;

            if (gonetGlobal == null)
            {
                GONetLog.Error($"[GONetRuntimeComponentInitializer] GONetGlobal singleton not initialized yet. Ensure GONetGlobal.Awake() runs before this initializer.");
                return;
            }

            // STEP 4: Check if the component already exists on GONetGlobal
            // This handles cases where:
            // - Another scene already added this component
            // - The component was manually added to the GONetGlobal prefab
            // - Multiple initializers in different scenes target the same component type
            addedComponent = gonetGlobal.GetComponent(componentType);

            if (addedComponent == null)
            {
                // Component doesn't exist yet - add it and claim ownership for potential cleanup
                try
                {
                    addedComponent = gonetGlobal.gameObject.AddComponent(componentType);
                    GONetLog.Info($"[GONetRuntimeComponentInitializer] Added component '{componentType.Name}' to GONetGlobal from scene '{gameObject.scene.name}'");
                }
                catch (Exception ex)
                {
                    GONetLog.Error($"[GONetRuntimeComponentInitializer] Failed to add component '{componentType.Name}' to GONetGlobal: {ex.Message}");
                    addedComponent = null;
                }
            }
            else
            {
                // Component already exists - do NOT claim ownership
                // This prevents this initializer from removing a component that another scene (or the prefab) added
                GONetLog.Debug($"[GONetRuntimeComponentInitializer] Component '{componentType.Name}' already exists on GONetGlobal. Skipping addition (will not claim ownership for removal).");
                addedComponent = null; // Clear reference to indicate we don't own it
            }
        }

        /// <summary>
        /// <para>Called when this GameObject is destroyed (typically when the scene unloads).</para>
        ///
        /// <para>Cleanup Logic:</para>
        /// <list type="bullet">
        ///     <item><description>Only removes the component if <see cref="removeOnSceneUnload"/> is TRUE</description></item>
        ///     <item><description>Only removes if THIS initializer added the component (ownership tracked via <see cref="addedComponent"/>)</description></item>
        ///     <item><description>Does nothing if the component was already present or added by another initializer</description></item>
        /// </list>
        ///
        /// <para><b>Safety Guarantees:</b></para>
        /// <list type="bullet">
        ///     <item><description>Scene A cannot remove a component that Scene B added</description></item>
        ///     <item><description>If removeOnSceneUnload is false, component persists even when this initializer is destroyed</description></item>
        ///     <item><description>Multiple scenes can safely use the same component type without conflicts</description></item>
        /// </list>
        /// </summary>
        void OnDestroy()
        {
            // Only proceed with cleanup if:
            // 1. removeOnSceneUnload flag is enabled
            // 2. We actually added this component (have ownership)
            if (removeOnSceneUnload && addedComponent != null)
            {
                try
                {
                    GONetLog.Info($"[GONetRuntimeComponentInitializer] Removing component '{componentType?.Name ?? "Unknown"}' from GONetGlobal as scene '{gameObject.scene.name}' unloads");
                    Destroy(addedComponent);
                }
                catch (Exception ex)
                {
                    GONetLog.Error($"[GONetRuntimeComponentInitializer] Failed to remove component '{componentType?.Name ?? "Unknown"}' from GONetGlobal: {ex.Message}");
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only helper method to get the currently selected component type name.
        /// Used by the custom inspector to display current selection.
        /// </summary>
        internal string GetComponentTypeName()
        {
            return componentTypeName;
        }

        /// <summary>
        /// Editor-only helper method to set the component type name.
        /// Used by the custom inspector when user selects a type from the dropdown.
        /// </summary>
        internal void SetComponentTypeName(string typeName)
        {
            componentTypeName = typeName;
        }
#endif
    }
}
