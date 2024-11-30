using UnityEditor;
using UnityEngine;

namespace GONet.Editor
{
    [CustomEditor(typeof(GONetAutoMagicalSyncSettings_ProfileTemplate))]
    public class GONetSyncSettingsProfileCustomInspector : UnityEditor.Editor
    {
        private GONetAutoMagicalSyncSettings_ProfileTemplate targetSyncSettingsProfile;

        private void OnEnable()
        {
            targetSyncSettingsProfile = (GONetAutoMagicalSyncSettings_ProfileTemplate)target;
        }

        public override void OnInspectorGUI()
        {
            bool guiEnabledPrevious = GUI.enabled;
            GUI.enabled = !Application.isPlaying;

            // Update the serialized object to reflect current state
            serializedObject.Update();

            // Begin tracking changes
            EditorGUI.BeginChangeCheck();

            // Draw the default inspector (handles all fields)
            DrawDefaultInspector();

            // Apply changes if detected
            if (EditorGUI.EndChangeCheck())
            {
                // Apply changes to serialized object and mark it dirty
                serializedObject.ApplyModifiedProperties();

                if (!Application.isPlaying)
                {
                    string assetPath = AssetDatabase.GetAssetPath(targetSyncSettingsProfile);
                    string trimmedPath = assetPath.StartsWith("Assets") ? assetPath : assetPath.Substring(assetPath.IndexOf("/Assets") + 1);

                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                        $"{nameof(GONetAutoMagicalSyncSettings_ProfileTemplate)} at '{trimmedPath}' has changed member values in editor.");

                    EditorUtility.SetDirty(targetSyncSettingsProfile);
                }
            }

            // Restore previous GUI state
            GUI.enabled = guiEnabledPrevious;
        }
    }
}