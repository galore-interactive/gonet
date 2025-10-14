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

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// Cleans up runtime-loaded scenes that Unity leaves in the hierarchy after exiting play mode or after builds.
    ///
    /// **Problem:**
    /// 1. When scenes are loaded at runtime via GONetSceneManager during play mode, Unity doesn't track them
    ///    as part of the editor's scene state. When exiting play mode, Unity restores the editor to its pre-play
    ///    state, but runtime-loaded scenes end up in a corrupted "not loaded" state in the hierarchy.
    ///
    /// 2. During build process, GONet opens scenes temporarily to scan for GONetParticipants. Sometimes these
    ///    scenes don't get closed properly, leaving them in the hierarchy in an unloaded state.
    ///
    /// **Solution:**
    /// This script tracks scenes that exist before play mode/builds and removes any scenes from the hierarchy
    /// after exiting play mode/builds that weren't present beforehand.
    /// </summary>
    [InitializeOnLoad]
    public static class GONetPlayModeSceneCleanup
    {
        private static HashSet<string> scenesBeforePlayMode = new HashSet<string>();
        private static bool wasPlayingLastFrame = false;

        static GONetPlayModeSceneCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // About to enter play mode - remember which scenes are open
                    scenesBeforePlayMode.Clear();
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.IsValid())
                        {
                            scenesBeforePlayMode.Add(scene.path);
                        }
                    }
                    GONetLog.Debug($"[PlayModeSceneCleanup] Recorded {scenesBeforePlayMode.Count} scenes before entering play mode");
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Just exited play mode - clean up runtime-loaded scenes
                    CleanupRuntimeLoadedScenes();
                    break;
            }
        }

        private static void OnEditorUpdate()
        {
            // Track play mode state changes that might be missed by playModeStateChanged
            bool isPlayingNow = EditorApplication.isPlaying;

            if (wasPlayingLastFrame && !isPlayingNow)
            {
                // Just exited play mode - ensure cleanup happens
                // This is a backup in case playModeStateChanged doesn't fire reliably
                EditorApplication.delayCall += CleanupRuntimeLoadedScenes;
            }

            wasPlayingLastFrame = isPlayingNow;
        }

        private static void CleanupRuntimeLoadedScenes()
        {
            if (Application.isPlaying)
            {
                // Don't run during play mode
                return;
            }

            List<Scene> scenesToRemove = new List<Scene>();

            // Find scenes in hierarchy that weren't present before play mode
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.IsValid())
                    continue;

                // Check if this scene was present before entering play mode
                if (!scenesBeforePlayMode.Contains(scene.path))
                {
                    // This scene was loaded at runtime - it shouldn't be here after exiting play mode
                    scenesToRemove.Add(scene);
                }
            }

            // Remove runtime-loaded scenes from hierarchy
            foreach (Scene scene in scenesToRemove)
            {
                // Unity doesn't allow closing the last scene - check before attempting removal
                if (SceneManager.sceneCount <= 1)
                {
                    GONetLog.Warning($"[PlayModeSceneCleanup] Cannot close '{scene.name}' - it's the only scene in hierarchy. Unity requires at least one scene to be loaded.");
                    continue;
                }

                GONetLog.Info($"[PlayModeSceneCleanup] Removing runtime-loaded scene '{scene.name}' (path: '{scene.path}') from hierarchy after exiting play mode");

                try
                {
                    // Try to close the scene
                    if (scene.isLoaded)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                    else
                    {
                        // Scene is in "not loaded" state - Unity bug workaround
                        // We need to remove it from the hierarchy manually
                        // Unfortunately, there's no direct API to remove an unloaded scene from the hierarchy
                        // EditorSceneManager.CloseScene should handle this, but log a warning if it doesn't work
                        bool closed = EditorSceneManager.CloseScene(scene, true);
                        if (!closed)
                        {
                            GONetLog.Warning($"[PlayModeSceneCleanup] Failed to close unloaded scene '{scene.name}'. You may need to manually close it from the Hierarchy.");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    // Catch Unity's "cannot close last scene" error gracefully
                    if (ex.Message.Contains("Unloading the last loaded scene"))
                    {
                        GONetLog.Warning($"[PlayModeSceneCleanup] Unity prevented closing '{scene.name}' because it's the last scene. This is expected behavior.");
                    }
                    else
                    {
                        GONetLog.Warning($"[PlayModeSceneCleanup] Exception while closing scene '{scene.name}': {ex.Message}");
                    }
                }
            }

            if (scenesToRemove.Count > 0)
            {
                GONetLog.Info($"[PlayModeSceneCleanup] Cleaned up {scenesToRemove.Count} runtime-loaded scene(s) after exiting play mode");
            }

            // Clear tracked scenes
            scenesBeforePlayMode.Clear();
        }

        internal static void CleanupBuildScenes()
        {
            if (Application.isPlaying)
            {
                // Don't run during play mode
                return;
            }

            List<Scene> scenesToRemove = new List<Scene>();

            // Find all scenes that are in an invalid/unloaded state
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.IsValid())
                    continue;

                // Check if scene is in "not loaded" state (corrupted state from build process)
                if (!scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                {
                    // This scene is in an unloaded state - shouldn't be in hierarchy
                    scenesToRemove.Add(scene);
                }
            }

            // Remove unloaded scenes from hierarchy
            foreach (Scene scene in scenesToRemove)
            {
                GONetLog.Info($"[PlayModeSceneCleanup] Removing unloaded scene '{scene.name}' (path: '{scene.path}') from hierarchy after build");

                try
                {
                    // Try to close the scene
                    bool closed = EditorSceneManager.CloseScene(scene, true);
                    if (!closed)
                    {
                        GONetLog.Warning($"[PlayModeSceneCleanup] Failed to close unloaded scene '{scene.name}'. You may need to manually close it from the Hierarchy.");
                    }
                }
                catch (System.Exception ex)
                {
                    GONetLog.Warning($"[PlayModeSceneCleanup] Exception while closing scene '{scene.name}': {ex.Message}");
                }
            }

            if (scenesToRemove.Count > 0)
            {
                GONetLog.Info($"[PlayModeSceneCleanup] Cleaned up {scenesToRemove.Count} unloaded scene(s) after build");
            }
        }
    }

    /// <summary>
    /// Build callback to clean up scenes after build process completes.
    /// </summary>
    public class GONetBuildSceneCleanup : IPostprocessBuildWithReport
    {
        public int callbackOrder => 1000; // Run after GONet's build processors

        public void OnPostprocessBuild(BuildReport report)
        {
            // Clean up any scenes that were opened during build process
            EditorApplication.delayCall += GONetPlayModeSceneCleanup.CleanupBuildScenes;
        }
    }
}
