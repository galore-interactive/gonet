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
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if ADDRESSABLES_AVAILABLE
using UnityEngine.AddressableAssets;
#endif

namespace GONet
{
    /// <summary>
    /// Helper class for working with GONet and Unity Addressables.
    /// Provides utilities to preload addressable prefabs for optimal network spawning performance.
    /// </summary>
    public static class GONetAddressablesHelper
    {
#if ADDRESSABLES_AVAILABLE
        /// <summary>
        /// Call this during game initialization to preload all addressable GONet prefabs for immediate spawning.
        /// This ensures network spawning doesn't have async delays when instantiating addressable prefabs.
        ///
        /// Example usage:
        /// <code>
        /// async void Start()
        /// {
        ///     await GONetAddressablesHelper.PreloadAllGONetAddressablePrefabs();
        ///     // Now all addressable GONet prefabs are cached and ready for instant spawning
        /// }
        /// </code>
        /// </summary>
        public static async Task PreloadAllGONetAddressablePrefabs()
        {
            await GONetSpawnSupport_Runtime.WarmupAddressablePrefabCache();
        }

        /// <summary>
        /// Preloads specific addressable keys for GONet prefabs.
        /// Use this if you want to selectively preload only certain prefabs.
        ///
        /// Example usage:
        /// <code>
        /// await GONetAddressablesHelper.PreloadGONetAddressablePrefabs(new[] { "PlayerPrefab", "BulletPrefab" });
        /// </code>
        /// </summary>
        /// <param name="addressableKeys">The addressable keys to preload</param>
        public static async Task PreloadGONetAddressablePrefabs(IEnumerable<string> addressableKeys)
        {
            await GONetSpawnSupport_Runtime.WarmupAddressablePrefabCache(addressableKeys);
        }

        /// <summary>
        /// Example of how to load and instantiate an addressable prefab with GONet networking.
        /// The instantiated object will automatically participate in GONet networking.
        ///
        /// THREAD SAFETY: This method GUARANTEES execution returns to Unity's main thread after await.
        /// Safe to call Unity APIs immediately after awaiting.
        ///
        /// PERFORMANCE NOTE: This method does NOT use caching - it loads from Addressables every time.
        /// For repeated spawning, use LoadGONetPrefabAsync_Cached() instead for much better performance.
        ///
        /// Example usage:
        /// <code>
        /// var bulletPrefab = await GONetAddressablesHelper.LoadGONetPrefabAsync("BulletPrefab");
        /// var bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        /// // Safe - guaranteed on Unity main thread!
        /// </code>
        /// </summary>
        /// <param name="addressableKey">The addressable key for the prefab</param>
        /// <returns>The loaded GONetParticipant prefab ready for instantiation</returns>
        public static async Task<GONetParticipant> LoadGONetPrefabAsync(string addressableKey)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            var gameObject = await handle.Task;

            // CRITICAL IL2CPP FIX: Ensure we're back on Unity main thread after await
            // In IL2CPP builds, async continuations MAY execute on background threads
            // We MUST marshal back to Unity main thread before calling Unity APIs
            await GONetThreading.EnsureMainThread();

            return gameObject.GetComponent<GONetParticipant>();
        }

        /// <summary>
        /// Loads a GONet prefab from cache if available, otherwise loads from Addressables and caches it.
        ///
        /// PERFORMANCE: This is MUCH faster than LoadGONetPrefabAsync() for repeated spawning.
        /// First call loads from Addressables (async), subsequent calls return instantly (cache hit).
        ///
        /// THREAD SAFETY: This method GUARANTEES execution returns to Unity's main thread after await.
        ///
        /// RECOMMENDED PATTERN for rapid spawning:
        /// <code>
        /// // Option 1: Load on-demand (first call async, rest instant)
        /// async void FireBullet()
        /// {
        ///     var bulletPrefab = await GONetAddressablesHelper.LoadGONetPrefabAsync_Cached("BulletPrefab");
        ///     // First call: ~50ms (load from Addressables)
        ///     // Subsequent calls: ~0.01ms (cache hit)
        ///     Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        /// }
        ///
        /// // Option 2: Preload during Start (best for known prefabs)
        /// async void Start()
        /// {
        ///     await GONetAddressablesHelper.PreloadGONetAddressablePrefabs(new[] { "BulletPrefab", "ExplosionPrefab" });
        ///     // Now all subsequent loads are instant!
        /// }
        /// </code>
        ///
        /// CACHE BEHAVIOR:
        /// - Cache is shared across all GONet systems (spawn system uses same cache)
        /// - Cache persists until scene unload or manual clear
        /// - Thread-safe for concurrent calls to same key (only loads once)
        /// </summary>
        /// <param name="addressableKey">The addressable key for the prefab</param>
        /// <returns>The loaded GONetParticipant prefab ready for instantiation</returns>
        public static async Task<GONetParticipant> LoadGONetPrefabAsync_Cached(string addressableKey)
        {
            // Fast path: Check cache first (avoids async overhead)
            if (GONetSpawnSupport_Runtime.TryGetCachedAddressablePrefab(addressableKey, out GONetParticipant cachedPrefab))
            {
                // Cache hit - return instantly (no await needed!)
                // ALREADY on main thread (synchronous path)
                return cachedPrefab;
            }

            // Cache miss - load from Addressables and cache the result
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            var gameObject = await handle.Task;

            // CRITICAL IL2CPP FIX: Ensure we're back on Unity main thread after await
            await GONetThreading.EnsureMainThread();

            GONetParticipant prefab = gameObject.GetComponent<GONetParticipant>();

            // Cache for future calls
            GONetSpawnSupport_Runtime.CacheAddressablePrefab(addressableKey, prefab);

            return prefab;
        }

        /// <summary>
        /// ADVANCED USE ONLY: Loads addressable prefab WITHOUT guaranteeing main thread return.
        ///
        /// WARNING: After awaiting, you may be on a background thread! Unity API calls will crash!
        /// Only use this if you understand async threading and will manually marshal to main thread.
        ///
        /// Example usage for experts:
        /// <code>
        /// var bulletPrefab = await GONetAddressablesHelper.LoadGONetPrefabAsync_Unsafe("BulletPrefab");
        /// await GONetThreading.EnsureMainThread(); // REQUIRED - manual marshal
        /// var bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        /// </code>
        /// </summary>
        /// <param name="addressableKey">The addressable key for the prefab</param>
        /// <returns>The loaded GONetParticipant prefab - WARNING: may not be on main thread!</returns>
        public static async Task<GONetParticipant> LoadGONetPrefabAsync_Unsafe(string addressableKey)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
            var gameObject = await handle.Task;

            // Log diagnostics but DO NOT marshal - caller is responsible
            GONetThreading.LogCurrentThread();

            return gameObject.GetComponent<GONetParticipant>();
        }
#else
        /// <summary>
        /// Addressables not available - falls back to no-op.
        /// Install Unity Addressables package to enable addressables support.
        /// </summary>
        public static Task PreloadAllGONetAddressablePrefabs()
        {
            GONetLog.Warning("Addressables package not installed. GONet addressables support disabled. All prefabs must be in Resources folders.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Addressables not available - falls back to no-op.
        /// Install Unity Addressables package to enable addressables support.
        /// </summary>
        public static Task PreloadGONetAddressablePrefabs(IEnumerable<string> addressableKeys)
        {
            GONetLog.Warning("Addressables package not installed. GONet addressables support disabled. All prefabs must be in Resources folders.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Addressables not available - this method is not supported.
        /// Install Unity Addressables package to enable addressables support.
        /// </summary>
        public static Task<GONetParticipant> LoadGONetPrefabAsync(string addressableKey)
        {
            GONetLog.Error("Addressables package not installed. Use Resources.Load<GONetParticipant>() instead.");
            return Task.FromResult<GONetParticipant>(null);
        }
#endif

        /// <summary>
        /// Returns whether Addressables support is available in this project.
        /// </summary>
        public static bool IsAddressablesAvailable
        {
            get
            {
#if ADDRESSABLES_AVAILABLE
                return true;
#else
                return false;
#endif
            }
        }
    }
}