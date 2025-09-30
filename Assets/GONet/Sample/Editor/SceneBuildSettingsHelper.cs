using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GONet.Sample.Editor
{
    public static class SceneBuildSettingsHelper
    {
        [MenuItem("GONet/Sample/Add GONetSample to Build Settings (First)")]
        public static void AddGONetSampleToBuildSettings()
        {
            string scenePath = "Assets/GONet/Sample/GONetSample.unity";

            // Get current build settings scenes
            var scenes = EditorBuildSettings.scenes;
            var scenesList = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);

            // Check if scene already exists
            int existingIndex = scenesList.FindIndex(s => s.path == scenePath);

            if (existingIndex >= 0)
            {
                // Remove from current position
                scenesList.RemoveAt(existingIndex);
                Debug.Log($"[SceneBuildSettingsHelper] Removed existing GONetSample from position {existingIndex}");
            }

            // Add at the beginning
            scenesList.Insert(0, new EditorBuildSettingsScene(scenePath, true));

            // Update build settings
            EditorBuildSettings.scenes = scenesList.ToArray();

            Debug.Log($"[SceneBuildSettingsHelper] Added GONetSample.unity as first scene in Build Settings");

            // Log all scenes for verification
            Debug.Log("[SceneBuildSettingsHelper] Current Build Settings scenes:");
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var scene = EditorBuildSettings.scenes[i];
                Debug.Log($"  [{i}] {scene.path} (enabled: {scene.enabled})");
            }
        }
    }
}
