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

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace GONet.Editor
{
    /// <summary>
    /// <para><b>Custom Inspector for <see cref="GONetRuntimeComponentInitializer"/>.</b></para>
    ///
    /// <para><b>Purpose:</b> Provides a user-friendly dropdown interface for selecting which
    /// <see cref="GONetParticipantCompanionBehaviour"/> component to add to GONetGlobal at runtime.</para>
    ///
    /// <para><b>Features:</b></para>
    /// <list type="bullet">
    ///     <item><description>Automatically discovers all available GONetParticipantCompanionBehaviour types in the project</description></item>
    ///     <item><description>Presents types in a searchable dropdown (Unity's built-in Popup control)</description></item>
    ///     <item><description>Displays helpful information about the selected type (name and namespace)</description></item>
    ///     <item><description>Shows warnings if no compatible types are found in the project</description></item>
    ///     <item><description>Stores the selection as a fully qualified type name for reliability across assembly reloads</description></item>
    /// </list>
    ///
    /// <para><b>How It Works:</b></para>
    /// <list type="number">
    ///     <item><description>On enable, scans all loaded assemblies for types extending GONetParticipantCompanionBehaviour</description></item>
    ///     <item><description>Filters out abstract classes and the base GONetParticipantCompanionBehaviour class itself</description></item>
    ///     <item><description>Sorts types alphabetically by name for easy browsing</description></item>
    ///     <item><description>Displays the list in a dropdown with full type names (including namespace)</description></item>
    ///     <item><description>Saves the selection as an AssemblyQualifiedName for maximum reliability</description></item>
    /// </list>
    ///
    /// <para><b>Technical Notes:</b></para>
    /// <list type="bullet">
    ///     <item><description>Uses reflection to discover types at edit-time (no runtime performance impact)</description></item>
    ///     <item><description>AssemblyQualifiedName ensures types can be resolved even after code changes</description></item>
    ///     <item><description>Automatically refreshes type list when inspector is opened/enabled</description></item>
    /// </list>
    /// </summary>
    [CustomEditor(typeof(GONetRuntimeComponentInitializer))]
    public class GONetRuntimeComponentInitializerEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Serialized property for the component type name field.
        /// This is the underlying string that stores the selected type's AssemblyQualifiedName.
        /// </summary>
        private SerializedProperty componentTypeNameProp;

        /// <summary>
        /// Serialized property for the removeOnSceneUnload flag.
        /// Controls whether the component should be removed when the scene unloads.
        /// </summary>
        private SerializedProperty removeOnSceneUnloadProp;

        /// <summary>
        /// Cached list of all discovered types that extend GONetParticipantCompanionBehaviour.
        /// Built once during OnEnable() to avoid repeated reflection queries.
        /// </summary>
        private List<Type> availableTypes;

        /// <summary>
        /// Display names for the dropdown, showing the full type name (namespace + class name).
        /// Parallel array to <see cref="availableTypes"/> - same index corresponds to same type.
        /// </summary>
        private string[] typeDisplayNames;

        /// <summary>
        /// Currently selected index in the dropdown.
        /// Corresponds to indices in <see cref="availableTypes"/> and <see cref="typeDisplayNames"/>.
        /// </summary>
        private int selectedIndex = 0;

        /// <summary>
        /// <para>Called when the inspector is opened or enabled.</para>
        ///
        /// <para>Performs initialization:</para>
        /// <list type="number">
        ///     <item><description>Binds to the serialized properties for data access</description></item>
        ///     <item><description>Discovers all available GONetParticipantCompanionBehaviour types via reflection</description></item>
        ///     <item><description>Builds display names for the dropdown</description></item>
        ///     <item><description>Restores the previously selected type (if any)</description></item>
        /// </list>
        /// </summary>
        void OnEnable()
        {
            // Bind to serialized properties so we can read/write the component's fields
            componentTypeNameProp = serializedObject.FindProperty("componentTypeName");
            removeOnSceneUnloadProp = serializedObject.FindProperty("removeOnSceneUnload");

            // DISCOVER ALL COMPATIBLE TYPES
            // Use reflection to find every type in every loaded assembly that:
            // 1. Extends GONetParticipantCompanionBehaviour (required for RPC support)
            // 2. Is not abstract (must be instantiable)
            // 3. Is not the base GONetParticipantCompanionBehaviour class itself
            availableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (System.Reflection.ReflectionTypeLoadException)
                    {
                        // Some assemblies may fail to load all types (e.g., optional dependencies missing)
                        // Gracefully skip these assemblies rather than breaking the entire discovery process
                        return new Type[0];
                    }
                })
                .Where(type =>
                    type != null &&
                    typeof(GONetParticipantCompanionBehaviour).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type != typeof(GONetParticipantCompanionBehaviour))
                .OrderBy(type => type.Name) // Sort alphabetically for easy browsing
                .ToList();

            // Build display names showing full type path for disambiguation
            // Format: "Namespace.ClassName" (e.g., "GONet.Sample.GONetSampleChatSystem")
            typeDisplayNames = availableTypes
                .Select(t => string.IsNullOrEmpty(t.Namespace) ? t.Name : $"{t.Namespace}.{t.Name}")
                .ToArray();

            // RESTORE PREVIOUS SELECTION
            // If a type was previously selected, find its index in our discovered types list
            string currentTypeName = componentTypeNameProp.stringValue;
            if (!string.IsNullOrEmpty(currentTypeName))
            {
                // Match by AssemblyQualifiedName for maximum reliability
                selectedIndex = availableTypes.FindIndex(type => type.AssemblyQualifiedName == currentTypeName);

                // If exact match failed, try matching by FullName (handles minor assembly version changes)
                if (selectedIndex < 0)
                {
                    Type fallbackType = Type.GetType(currentTypeName);
                    if (fallbackType != null)
                    {
                        selectedIndex = availableTypes.FindIndex(type => type.FullName == fallbackType.FullName);
                    }
                }

                // If still not found, default to first item (or 0 if list is empty)
                if (selectedIndex < 0)
                {
                    selectedIndex = availableTypes.Count > 0 ? 0 : -1;
                }
            }
        }

        /// <summary>
        /// <para>Draws the custom inspector GUI for GONetComponentInitializer.</para>
        ///
        /// <para>Inspector Layout:</para>
        /// <list type="number">
        ///     <item><description><b>Component Type Section:</b> Dropdown for selecting the type to initialize</description></item>
        ///     <item><description><b>Type Information Box:</b> Shows selected type's name and namespace</description></item>
        ///     <item><description><b>Remove On Scene Unload:</b> Toggle for cleanup behavior</description></item>
        /// </list>
        ///
        /// <para><b>User Experience Features:</b></para>
        /// <list type="bullet">
        ///     <item><description>Clear section headers with bold labels</description></item>
        ///     <item><description>Helpful info box showing details about the selected type</description></item>
        ///     <item><description>Warning message if no compatible types are found</description></item>
        ///     <item><description>Automatic saving of changes (no manual "Apply" needed)</description></item>
        /// </list>
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Update serializedObject to get latest values from the actual component
            serializedObject.Update();

            // HEADER: Component Type Selection
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("GONet Component Type", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select which GONetParticipantCompanionBehaviour component to add to GONetGlobal when this scene loads.\n\n" +
                "Only types extending GONetParticipantCompanionBehaviour are shown (required for RPC support).",
                MessageType.Info);

            if (availableTypes.Count > 0)
            {
                // DROPDOWN: Type Selection
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUILayout.Popup("Component Type", selectedIndex, typeDisplayNames);

                // If user changed selection, update the stored type name
                if (EditorGUI.EndChangeCheck() && newIndex != selectedIndex && newIndex >= 0)
                {
                    selectedIndex = newIndex;

                    // Store the AssemblyQualifiedName for maximum reliability
                    // This includes version, culture, and public key token, ensuring correct resolution
                    componentTypeNameProp.stringValue = availableTypes[selectedIndex].AssemblyQualifiedName;
                }

                // INFO BOX: Show details about currently selected type
                if (selectedIndex >= 0 && selectedIndex < availableTypes.Count)
                {
                    Type selectedType = availableTypes[selectedIndex];
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        $"Selected Type: {selectedType.Name}\n" +
                        $"Namespace: {selectedType.Namespace ?? "(Global)"}\n" +
                        $"Assembly: {selectedType.Assembly.GetName().Name}",
                        MessageType.None);
                }
            }
            else
            {
                // WARNING: No compatible types found
                EditorGUILayout.HelpBox(
                    "No types found that extend GONetParticipantCompanionBehaviour.\n\n" +
                    "Ensure you have created custom component classes that extend GONetParticipantCompanionBehaviour.",
                    MessageType.Warning);
            }

            // SECTION: Cleanup Behavior
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lifecycle Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(removeOnSceneUnloadProp, new GUIContent(
                "Remove On Scene Unload",
                "If enabled, the component will be removed from GONetGlobal when this scene unloads. " +
                "Leave disabled for components that should persist across all scenes."));

            // USAGE TIPS
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Usage Tips:\n" +
                "• For persistent features (like chat): Leave 'Remove On Scene Unload' UNCHECKED\n" +
                "• For scene-specific features: CHECK 'Remove On Scene Unload'\n" +
                "• Multiple scenes can safely initialize the same component type\n" +
                "• The first scene to load will add it, others will skip (unless removed between scenes)",
                MessageType.Info);

            // Apply changes back to the actual component
            serializedObject.ApplyModifiedProperties();
        }
    }
}
