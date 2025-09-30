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
using UnityEngine;
using UnityEngine.SceneManagement;

#if ADDRESSABLES_AVAILABLE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

namespace GONet
{
    /// <summary>
    /// Manages networked scene loading and unloading for GONet.
    /// Server-authoritative: only server can initiate scene changes.
    /// Clients receive scene change events and load accordingly.
    /// Access via GONetMain.SceneManager or from GONetBehaviour.SceneManager.
    /// </summary>
    public class GONetSceneManager
    {
        private readonly GONetGlobal global;
        private readonly HashSet<string> loadedAdditiveScenes = new HashSet<string>();

#if ADDRESSABLES_AVAILABLE
        private readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> addressableSceneHandles =
            new Dictionary<string, AsyncOperationHandle<SceneInstance>>();
#endif

        // ========================================
        // EXTENSIBILITY HOOKS
        // ========================================

        /// <summary>
        /// Validation hook - return false to deny scene load request.
        /// Parameters: (sceneName, mode, requestingAuthority) → bool allowed
        /// </summary>
        public delegate bool SceneLoadValidationDelegate(string sceneName, LoadSceneMode mode, ushort requestingAuthority);

        /// <summary>
        /// Server-side validation for scene load requests.
        /// Subscribe to this event to add custom validation logic.
        /// </summary>
        public event SceneLoadValidationDelegate OnValidateSceneLoad;

        /// <summary>
        /// Called when scene load begins (after validation, before actual load)
        /// </summary>
        public delegate void SceneLoadDelegate(string sceneName, LoadSceneMode mode);

        /// <summary>
        /// Invoked when scene loading starts on this machine
        /// </summary>
        public event SceneLoadDelegate OnSceneLoadStarted;

        /// <summary>
        /// Invoked when scene loading completes on this machine
        /// </summary>
        public event SceneLoadDelegate OnSceneLoadCompleted;

        /// <summary>
        /// Called when scene unload begins
        /// </summary>
        public event SceneLoadDelegate OnSceneUnloadStarted;

        // ========================================
        // INITIALIZATION
        // ========================================

        internal GONetSceneManager(GONetGlobal global)
        {
            this.global = global;

            // Subscribe to scene events
            GONetMain.EventBus.Subscribe<SceneLoadEvent>(OnSceneLoadEvent);
            GONetMain.EventBus.Subscribe<SceneUnloadEvent>(OnSceneUnloadEvent);

            GONetLog.Debug("[GONetSceneManager] Initialized");
        }

        // ========================================
        // PUBLIC API - Scene Loading
        // ========================================

        /// <summary>
        /// Loads a scene from Build Settings.
        /// SERVER ONLY - clients should not call this directly.
        /// </summary>
        /// <param name="sceneName">Name of the scene in Build Settings</param>
        /// <param name="mode">Single or Additive loading mode</param>
        public void LoadSceneFromBuildSettings(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!GONetMain.IsServer)
            {
                GONetLog.Warning("[GONetSceneManager] Only server can load scenes. Use RequestLoadScene() from clients.");
                return;
            }

            LoadScene_Internal(sceneName, -1, SceneLoadType.BuildSettings, mode, true, 100);
        }

        /// <summary>
        /// Loads a scene from Build Settings by build index.
        /// SERVER ONLY - clients should not call this directly.
        /// </summary>
        /// <param name="buildIndex">Build index of the scene</param>
        /// <param name="mode">Single or Additive loading mode</param>
        public void LoadSceneFromBuildSettings(int buildIndex, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!GONetMain.IsServer)
            {
                GONetLog.Warning("[GONetSceneManager] Only server can load scenes. Use RequestLoadScene() from clients.");
                return;
            }

            string sceneName = GetSceneNameFromBuildIndex(buildIndex);
            if (string.IsNullOrEmpty(sceneName))
            {
                GONetLog.Error($"[GONetSceneManager] Invalid build index: {buildIndex}");
                return;
            }

            LoadScene_Internal(sceneName, buildIndex, SceneLoadType.BuildSettings, mode, true, 100);
        }

        /// <summary>
        /// Unloads an additively loaded scene.
        /// SERVER ONLY - clients should not call this directly.
        /// </summary>
        /// <param name="sceneName">Name of the scene to unload</param>
        public void UnloadScene(string sceneName)
        {
            if (!GONetMain.IsServer)
            {
                GONetLog.Warning("[GONetSceneManager] Only server can unload scenes.");
                return;
            }

            if (!loadedAdditiveScenes.Contains(sceneName))
            {
                GONetLog.Warning($"[GONetSceneManager] Scene '{sceneName}' is not loaded additively, cannot unload");
                return;
            }

            UnloadScene_Internal(sceneName, -1, SceneLoadType.BuildSettings);
        }

        // ========================================
        // INTERNAL IMPLEMENTATION
        // ========================================

        private void LoadScene_Internal(
            string sceneName,
            int buildIndex,
            SceneLoadType loadType,
            LoadSceneMode mode,
            bool activateOnLoad,
            int priority)
        {
            // Publish persistent event for all clients (including late-joiners)
            var evt = new SceneLoadEvent
            {
                SceneName = sceneName,
                SceneBuildIndex = buildIndex,
                LoadType = loadType,
                Mode = mode,
                ActivateOnLoad = activateOnLoad,
                Priority = priority,
                OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks
            };

            GONetMain.EventBus.Publish(evt);

            // Notify subscribers
            OnSceneLoadStarted?.Invoke(sceneName, mode);

            // Load locally on server
            LoadSceneLocally(evt);

            // Track additive scenes
            if (mode == LoadSceneMode.Additive)
            {
                loadedAdditiveScenes.Add(sceneName);
            }

            GONetLog.Info($"[GONetSceneManager] Server loading scene: {sceneName} ({loadType}, {mode})");
        }

        private void UnloadScene_Internal(string sceneName, int buildIndex, SceneLoadType loadType)
        {
            // Publish persistent event
            var evt = new SceneUnloadEvent
            {
                SceneName = sceneName,
                SceneBuildIndex = buildIndex,
                LoadType = loadType,
                OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks
            };

            GONetMain.EventBus.Publish(evt);

            // Notify subscribers
            OnSceneUnloadStarted?.Invoke(sceneName, LoadSceneMode.Additive);

            // Unload locally on server
            UnloadSceneLocally(evt);

            // Remove from tracking
            loadedAdditiveScenes.Remove(sceneName);

            GONetLog.Info($"[GONetSceneManager] Server unloading scene: {sceneName}");
        }

        // ========================================
        // EVENT HANDLERS
        // ========================================

        private void OnSceneLoadEvent(GONetEventEnvelope<SceneLoadEvent> eventEnvelope)
        {
            SceneLoadEvent evt = eventEnvelope.Event;

            // Server already loaded it when publishing the event
            if (GONetMain.IsServer)
                return;

            GONetLog.Info($"[GONetSceneManager] Client received scene load: {evt.SceneName} ({evt.LoadType}, {evt.Mode})");

            // Reset time sync to gap-closing mode for scene changes
            GONetMain.ResetTimeSyncGap($"scene_load_{evt.SceneName}");

            // Notify subscribers
            OnSceneLoadStarted?.Invoke(evt.SceneName, evt.Mode);

            // Load scene locally on client
            LoadSceneLocally(evt);

            // Track additive scenes
            if (evt.Mode == LoadSceneMode.Additive)
            {
                loadedAdditiveScenes.Add(evt.SceneName);
            }
        }

        private void OnSceneUnloadEvent(GONetEventEnvelope<SceneUnloadEvent> eventEnvelope)
        {
            SceneUnloadEvent evt = eventEnvelope.Event;

            // Server already unloaded it when publishing the event
            if (GONetMain.IsServer)
                return;

            GONetLog.Info($"[GONetSceneManager] Client received scene unload: {evt.SceneName}");

            // Notify subscribers
            OnSceneUnloadStarted?.Invoke(evt.SceneName, LoadSceneMode.Additive);

            // Unload scene locally on client
            UnloadSceneLocally(evt);

            // Remove from tracking
            loadedAdditiveScenes.Remove(evt.SceneName);
        }

        // ========================================
        // SCENE LOADING/UNLOADING IMPLEMENTATION
        // ========================================

        private void LoadSceneLocally(SceneLoadEvent evt)
        {
            if (evt.LoadType == SceneLoadType.BuildSettings)
            {
                // Use build index if available, otherwise use name
                if (evt.SceneBuildIndex >= 0)
                {
                    SceneManager.LoadSceneAsync(evt.SceneBuildIndex, evt.Mode);
                }
                else
                {
                    SceneManager.LoadSceneAsync(evt.SceneName, evt.Mode);
                }
            }
#if ADDRESSABLES_AVAILABLE
            else if (evt.LoadType == SceneLoadType.Addressables)
            {
                LoadSceneFromAddressablesAsync(evt);
            }
#else
            else
            {
                GONetLog.Error("[GONetSceneManager] Addressables not available but scene requested Addressables loading!");
            }
#endif
        }

        private void UnloadSceneLocally(SceneUnloadEvent evt)
        {
            if (evt.LoadType == SceneLoadType.BuildSettings)
            {
                Scene scene = SceneManager.GetSceneByName(evt.SceneName);
                if (scene.IsValid() && scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
#if ADDRESSABLES_AVAILABLE
            else if (evt.LoadType == SceneLoadType.Addressables)
            {
                UnloadSceneFromAddressablesAsync(evt.SceneName);
            }
#endif
        }

        // ========================================
        // HELPER METHODS
        // ========================================

        private string GetSceneNameFromBuildIndex(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
                return null;

            string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            return System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }

        /// <summary>
        /// Checks if a scene is currently loaded (additively or as main scene)
        /// </summary>
        public bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        // ========================================
        // ADDRESSABLES SUPPORT (Phase 3)
        // ========================================

#if ADDRESSABLES_AVAILABLE
        private async void LoadSceneFromAddressablesAsync(SceneLoadEvent evt)
        {
            var handle = Addressables.LoadSceneAsync(
                evt.SceneName,
                evt.Mode,
                evt.ActivateOnLoad,
                evt.Priority
            );

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                addressableSceneHandles[evt.SceneName] = handle;
                OnSceneLoadCompleted?.Invoke(evt.SceneName, evt.Mode);
                GONetLog.Info($"[GONetSceneManager] Addressable scene loaded: {evt.SceneName}");
            }
            else
            {
                GONetLog.Error($"[GONetSceneManager] Failed to load addressable scene '{evt.SceneName}': {handle.OperationException}");
            }
        }

        private async void UnloadSceneFromAddressablesAsync(string sceneName)
        {
            if (!addressableSceneHandles.TryGetValue(sceneName, out var handle))
            {
                GONetLog.Warning($"[GONetSceneManager] Cannot unload addressable scene '{sceneName}' - not loaded or not tracked");
                return;
            }

            var unloadHandle = Addressables.UnloadSceneAsync(handle);
            await unloadHandle.Task;

            addressableSceneHandles.Remove(sceneName);
            GONetLog.Info($"[GONetSceneManager] Addressable scene unloaded: {sceneName}");
        }
#endif
    }
}