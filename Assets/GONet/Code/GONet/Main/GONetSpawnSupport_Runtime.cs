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

using GONet.Generation;
using GONet.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if ADDRESSABLES_AVAILABLE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace GONet
{
    public static class GONetSpawnSupport_Runtime
    {
        public static bool IsNewDtmForced { get; set; }

        public const string GONET_STREAMING_ASSETS_FOLDER = "GONet";
        public static readonly string DESIGN_TIME_METADATA_FILE_POST_STREAMING_ASSETS = Path.Combine(GONET_STREAMING_ASSETS_FOLDER, "DesignTimeMetadata.json");

        public const string SCENE_HIERARCHY_PREFIX = "scene://";
        public const string PROJECT_HIERARCHY_PREFIX = "project://";
        public const string RESOURCES_HIERARCHY_PREFIX = "resources://";
        public const string ADDRESSABLES_HIERARCHY_PREFIX = "addressables://";
        public const string RESOURCES = "Resources/";

        private static readonly string[] ALL_END_OF_LINE_OPTIONS = new[] { "\r\n", "\r", "\n" };

        private static readonly Dictionary<DesignTimeMetadata, GONetParticipant> designTimeMetadataToProjectTemplate = new(100, new DesignTimeMetadataLocationComparer());
        private static readonly DesignTimeMetadataDictionary designTimeMetadataLookup = new();
        private static bool isDesignTimeMetadataCachingComplete = false;
        private static readonly List<GONetParticipant> deferredLookupQueue = new List<GONetParticipant>();

#if ADDRESSABLES_AVAILABLE
        private static readonly Dictionary<string, GONetParticipant> addressablePrefabCache = new Dictionary<string, GONetParticipant>(100);
        private static readonly HashSet<string> loadingAddressableKeys = new HashSet<string>();
#endif
        /// <summary>
        /// For the dictionary <see cref="designTimeMetadataToProjectTemplate"/>, this special comparer that only considers location is preferred over the <see cref="DesignTimeMetadata"/> 
        /// equals and gethashcode implementation details.
        /// </summary>
        class DesignTimeMetadataLocationComparer : IEqualityComparer<DesignTimeMetadata>
        {
            public bool Equals(DesignTimeMetadata x, DesignTimeMetadata y)
            {
                if (x == null || y == null || x.Location == null || y.Location == null) return false;
                return x.Location == y.Location;
            }

            public int GetHashCode(DesignTimeMetadata obj)
            {
                return obj.Location.GetHashCode();
            }
        }

        static GONetSpawnSupport_Runtime()
        {
            //GONetLog.Debug($"---------------------------------NEW GONetSpawnSupport_Runtime(), so, all lookup cleared out!!!!");
        }

        public static IEnumerable<DesignTimeMetadata> LoadDesignTimeMetadataFromPersistence()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_METADATA_FILE_POST_STREAMING_ASSETS);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WEBGL)
            Debug.Log($"About to check out design time file at URI {fullPath}.");
            // As per, https://docs.unity3d.com/Manual/StreamingAssets.html :
            // "On Android and WebGL platforms, it’s not possible to access the streaming asset files directly via file system APIs and
            // streamingAssets path because these platforms return a URL. Use the UnityWebRequest class to access the content instead."
            Task<string> fileContentsTask = LoadFileFromWeb_Task(fullPath);
            if (fileContentsTask.Wait(5000))
            {
                string fileContentsJson = fileContentsTask.Result;
                DesignTimeMetadataLibrary library = JsonUtility.FromJson<DesignTimeMetadataLibrary>(fileContentsJson);
                return library.Entries;
            }
#else
            //Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                string fileContentsJson = File.ReadAllText(fullPath);
                DesignTimeMetadataLibrary library = JsonUtility.FromJson<DesignTimeMetadataLibrary>(fileContentsJson);
                return library.Entries;
            }
#endif

            throw new FileLoadException(fullPath);
        }

        private static async Task<string> LoadFileFromWeb_Task(string uri)
        {
            using UnityWebRequest fileRequest = UnityWebRequest.Get(uri);

            //fileRequest.SetRequestHeader("Content-Type", "text/plain");
            //var inFlight = fileRequest.SendWebRequest();

            var inFlight = fileRequest.Send();
            while (!inFlight.isDone)
            {
                await Task.Yield();
            }
            switch (fileRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    return fileRequest.downloadHandler.text;

                default:
                    throw new FileLoadException($"uri: {uri}, responseCode: {fileRequest.responseCode}, result: {fileRequest.result}");
            }
        }

        private static IEnumerator LoadFileFromWeb_Coroutine(string uri, Action<string> processResult)
        {
            using UnityWebRequest fileRequest = UnityWebRequest.Get(uri);

            //fileRequest.SetRequestHeader("Content-Type", "text/plain");
            //var inFlight = fileRequest.SendWebRequest();

            var inFlight = fileRequest.Send();
            while (!inFlight.isDone)
            {
                yield return null;
            }

            switch (fileRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    processResult(fileRequest.downloadHandler.text);
                    break;

                default:
                    throw new FileLoadException($"uri: {uri}, responseCode: {fileRequest.responseCode}, result: {fileRequest.result}");
            }
        }

        public static bool IsDesignTimeMetadataCached { get; private set; }

        public static void CacheAllProjectDesignTimeMetadata(MonoBehaviour coroutineOwner, System.Action onComplete = null)
        {
            //GONetLog.Debug("dreetsi cache begin");
            IsDesignTimeMetadataCached = false;
            coroutineOwner.StartCoroutine(
                CacheAllProjectDesignTimeMetadata_Coroutine(CacheAllDesignTimeMetadata, onComplete)
            );
        }

        private static IEnumerator CacheAllProjectDesignTimeMetadata_Coroutine(Action<IEnumerable<DesignTimeMetadata>> processResults, System.Action onComplete = null)
        {
            //GONetLog.Debug("dreetsi cache rotten");

            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_METADATA_FILE_POST_STREAMING_ASSETS);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WEBGL)
            Debug.Log($"About to check out design time file at URI {fullPath}.");
            // As per, https://docs.unity3d.com/Manual/StreamingAssets.html :
            // "On Android and WebGL platforms, it’s not possible to access the streaming asset files directly via file system APIs and
            // streamingAssets path because these platforms return a URL. Use the UnityWebRequest class to access the content instead."
            yield return LoadFileFromWeb_Coroutine(fullPath, (fileContentsJson) =>
                {
                    DesignTimeMetadataLibrary library = JsonUtility.FromJson<DesignTimeMetadataLibrary>(fileContentsJson);
                    processResults(library.Entries);
                });
            IsDesignTimeMetadataCached = true; // IMPORTANT: TODO FIXME this being in a coroutine is problematic because of what is waiting for this to be true...ought to block the main thread actually.....we should do this on another thread and block main thread until it this is true
            onComplete?.Invoke();

#else
            //Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                string fileContentsJson = File.ReadAllText(fullPath);
                DesignTimeMetadataLibrary library = JsonUtility.FromJson<DesignTimeMetadataLibrary>(fileContentsJson);
                processResults(library.Entries);
            }
            IsDesignTimeMetadataCached = true;
            onComplete?.Invoke();

            yield return null;
#endif
        }

        private static void CacheAllDesignTimeMetadata(IEnumerable<DesignTimeMetadata> allProjectDesignTimeMetadata)
        {
            isDesignTimeMetadataCachingComplete = false;

            foreach (DesignTimeMetadata designTimeMetadata in allProjectDesignTimeMetadata)
            {
                // GONetLog.Debug($"CacheAllDesignTimeMetadata: Processing metadata - Location: {designTimeMetadata?.Location ?? "NULL"}, IsNull: {designTimeMetadata == null}");

                if (designTimeMetadata.Location.StartsWith(PROJECT_HIERARCHY_PREFIX) || designTimeMetadata.Location.StartsWith(RESOURCES_HIERARCHY_PREFIX))
                {
                    // Handle both project:// and resources:// prefixes for backwards compatibility
                    string assetPath = designTimeMetadata.Location.StartsWith(PROJECT_HIERARCHY_PREFIX)
                        ? designTimeMetadata.Location.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty)
                        : designTimeMetadata.Location.Replace(RESOURCES_HIERARCHY_PREFIX, string.Empty);

                    GONetParticipant template = LookupResourceTemplateFromProjectLocation(assetPath);
                    if ((object)template != null)
                    {
                        GONetLog.Debug($"CacheAllDesignTimeMetadata: Found RESOURCES template for '{template.name}' at location: {designTimeMetadata.Location}");

                        // IMPORTANT: Ensure the template gets proper CodeGenerationId from its companion, not from metadata circular dependency
                        if (designTimeMetadata.CodeGenerationId == 0)
                        {
                            // Try to get CodeGenerationId directly from the template's companion if it exists
                            try
                            {
                                var companion = Generation.GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.CreateInstance(template);
                                if (companion != null)
                                {
                                    designTimeMetadata.CodeGenerationId = companion.CodeGenerationId;
                                    GONetLog.Debug($"CacheAllDesignTimeMetadata: Set CodeGenerationId={companion.CodeGenerationId} for template '{template.name}'");
                                    companion.Dispose(); // Clean up since we only needed the CodeGenerationId
                                }
                                else
                                {
                                    GONetLog.Warning($"CacheAllDesignTimeMetadata: Template '{template.name}' has no companion, CodeGenerationId remains 0");
                                }
                            }
                            catch (System.Exception ex)
                            {
                                GONetLog.Warning($"CacheAllDesignTimeMetadata: Could not create companion for template '{template.name}': {ex.Message}");
                            }
                        }

                        designTimeMetadataToProjectTemplate[designTimeMetadata] = template;

                        // Cache the metadata for the template so it has proper CodeGenerationId
                        designTimeMetadataLookup.Set(designTimeMetadata.Location, designTimeMetadata);
                    }
                    else
                    {
                        GONetLog.Warning($"CacheAllDesignTimeMetadata: Could not find RESOURCES template for location: {designTimeMetadata.Location}");
                    }
                }
                else if (designTimeMetadata.Location.StartsWith(ADDRESSABLES_HIERARCHY_PREFIX))
                {
                    // For addressables, we only cache the metadata - actual asset loading happens on-demand
                    GONetLog.Debug($"CacheAllDesignTimeMetadata: Caching ADDRESSABLES metadata for location: {designTimeMetadata.Location}");
                    designTimeMetadataLookup.Set(designTimeMetadata.Location, designTimeMetadata);
                }
                else if (designTimeMetadata.Location.StartsWith(SCENE_HIERARCHY_PREFIX))
                {
                    //GONetLog.Debug($"associating SCENE design time location: {designTimeMetadata.Location}");
                    designTimeMetadataLookup.Set(designTimeMetadata.Location, designTimeMetadata);
                }
            }

            isDesignTimeMetadataCachingComplete = true;
            GONetLog.Debug($"CacheAllDesignTimeMetadata: Caching complete - {designTimeMetadataLookup.Count} entries cached");

            // Process any deferred lookups that were queued while caching was in progress
            ProcessDeferredLookups();
        }

        private static void ProcessDeferredLookups()
        {
            if (deferredLookupQueue.Count > 0)
            {
                GONetLog.Debug($"ProcessDeferredLookups: Processing {deferredLookupQueue.Count} deferred metadata lookups");

                // Process each deferred participant
                for (int i = deferredLookupQueue.Count - 1; i >= 0; i--)
                {
                    GONetParticipant participant = deferredLookupQueue[i];
                    if (participant) // Check if still valid
                    {
                        // Remove the temporary empty metadata and do proper lookup
                        GetDesignTimeMetadata(participant, force: true);
                    }
                }

                deferredLookupQueue.Clear();
                GONetLog.Debug($"ProcessDeferredLookups: Completed processing deferred lookups");
            }
        }

        private static GONetParticipant LookupResourceTemplateFromProjectLocation(string projectLocation)
        {
            if (projectLocation != null && projectLocation.Contains(RESOURCES))
            {
                const string PREFAB_EXTENSION = ".prefab";
                string resourceLocation = projectLocation.Substring(projectLocation.IndexOf(RESOURCES) + RESOURCES.Length).Replace(PREFAB_EXTENSION, string.Empty);
                GONetParticipant gonetParticipant = Resources.Load<GONetParticipant>(resourceLocation);
                return gonetParticipant;
            }
            else
            {
                GONetLog.Warning("magoo....cannot find non-Resources assets at runtime.  project location: " + projectLocation);
                return null;
            }
        }

#if ADDRESSABLES_AVAILABLE
        /// <summary>
        /// Attempts to load a GONetParticipant prefab from Addressables cache first, then from Resources as fallback.
        /// </summary>
        private static GONetParticipant LookupTemplateFromAddressableOrResources(DesignTimeMetadata designTimeMetadata)
        {
            // Try Addressables first if key is available
            if (designTimeMetadata.LoadType == ResourceLoadType.Addressables && !string.IsNullOrEmpty(designTimeMetadata.AddressableKey))
            {
                if (addressablePrefabCache.TryGetValue(designTimeMetadata.AddressableKey, out GONetParticipant cachedPrefab))
                {
                    return cachedPrefab;
                }

                // If not cached, try to load synchronously (this should only happen if prefab wasn't pre-warmed)
                GONetLog.Warning($"Addressable prefab '{designTimeMetadata.AddressableKey}' not found in cache. Consider pre-warming cache for better performance.");
                GONetLog.Debug($"Attempting to load addressable with key: '{designTimeMetadata.AddressableKey}' from location: '{designTimeMetadata.Location}'");
                try
                {
                    var handle = Addressables.LoadAssetAsync<GameObject>(designTimeMetadata.AddressableKey);
                    GameObject gameObject = handle.WaitForCompletion();
                    GONetParticipant result = gameObject?.GetComponent<GONetParticipant>();
                    if (result != null)
                    {
                        addressablePrefabCache[designTimeMetadata.AddressableKey] = result;
                        return result;
                    }
                }
                catch (UnityEngine.AddressableAssets.InvalidKeyException ex)
                {
                    GONetLog.Error($"Invalid addressable key '{designTimeMetadata.AddressableKey}'. This usually means the asset is not properly configured in an Addressable group or has an invalid address. Full error: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    GONetLog.Error($"Failed to load addressable prefab '{designTimeMetadata.AddressableKey}': {ex.Message}");
                }
            }

            // Fallback to Resources if Addressables failed or not configured
            return LookupResourceTemplateFromProjectLocation(designTimeMetadata.Location);
        }

        /// <summary>
        /// Pre-loads addressable prefabs into cache for immediate access during network spawning.
        /// Call this during game initialization to avoid runtime delays.
        /// </summary>
        public static async Task WarmupAddressablePrefabCache(IEnumerable<string> addressableKeys)
        {
            var loadTasks = new List<Task>();

            foreach (string key in addressableKeys)
            {
                if (!addressablePrefabCache.ContainsKey(key) && !loadingAddressableKeys.Contains(key))
                {
                    loadingAddressableKeys.Add(key);
                    loadTasks.Add(LoadAndCacheAddressablePrefab(key));
                }
            }

            if (loadTasks.Count > 0)
            {
                await Task.WhenAll(loadTasks);
            }
        }

        /// <summary>
        /// Pre-loads all addressable prefabs found in design time metadata.
        /// </summary>
        public static async Task WarmupAddressablePrefabCache()
        {
            var addressableKeys = designTimeMetadataLookup
                .Where(metadata => metadata.LoadType == ResourceLoadType.Addressables && !string.IsNullOrEmpty(metadata.AddressableKey))
                .Select(metadata => metadata.AddressableKey)
                .Distinct();

            await WarmupAddressablePrefabCache(addressableKeys);
        }

        private static async Task LoadAndCacheAddressablePrefab(string addressableKey)
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
                GameObject gameObject = await handle.Task;
                GONetParticipant prefab = gameObject?.GetComponent<GONetParticipant>();

                if (prefab != null)
                {
                    addressablePrefabCache[addressableKey] = prefab;
                    GONetLog.Debug($"Cached addressable prefab: {addressableKey}");
                }
                else
                {
                    GONetLog.Warning($"Failed to load addressable prefab: {addressableKey} (result was null)");
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Error($"Error loading addressable prefab '{addressableKey}': {ex.Message}");
            }
            finally
            {
                loadingAddressableKeys.Remove(addressableKey);
            }
        }
#endif

        public static GONetParticipant LookupTemplateFromDesignTimeMetadata(DesignTimeMetadata designTimeMetadata)
        {
            if (designTimeMetadata != null)
            {
                if (designTimeMetadata.Location.StartsWith(SCENE_HIERARCHY_PREFIX))
                {
                    string fullUniquePath = designTimeMetadata.Location.Replace(SCENE_HIERARCHY_PREFIX, string.Empty);
                    return HierarchyUtils.FindByFullUniquePath(fullUniquePath).GetComponent<GONetParticipant>();
                }
                else if (designTimeMetadata.Location.StartsWith(ADDRESSABLES_HIERARCHY_PREFIX))
                {
#if ADDRESSABLES_AVAILABLE
                    // Pure addressables lookup using the addressable key
                    return LookupTemplateFromAddressableOrResources(designTimeMetadata);
#else
                    throw new InvalidOperationException($"Addressables support not available. Cannot load prefab from: {designTimeMetadata.Location}");
#endif
                }
                else if (designTimeMetadata.Location.StartsWith(PROJECT_HIERARCHY_PREFIX) || designTimeMetadata.Location.StartsWith(RESOURCES_HIERARCHY_PREFIX))
                {
#if ADDRESSABLES_AVAILABLE
                    // Try addressables-aware lookup first
                    if (designTimeMetadata.LoadType == ResourceLoadType.Addressables)
                    {
                        return LookupTemplateFromAddressableOrResources(designTimeMetadata);
                    }
#endif
                    // Use cached project template for Resources-based prefabs
                    return designTimeMetadataToProjectTemplate[designTimeMetadata];
                }
            }

            throw new ArgumentException(string.Concat("Must include supported prefix defined as const herein. value received: ", designTimeMetadata), nameof(designTimeMetadata));
        }

        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.DesignTimeLocation;
            return authorityInstance;
        }


        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant, Vector3, Quaternion)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal, Vector3 position, Quaternion rotation)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal, position, rotation);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.DesignTimeLocation;
            return authorityInstance;
        }

        static readonly Dictionary<GONetParticipant, string> nonAuthorityDesignTimeLocationByAuthorityInstanceMap = new Dictionary<GONetParticipant, string>(100);

        internal static bool TryGetNonAuthorityDesignTimeLocation(GONetParticipant authorityInstance, out string nonAuthorityDesignTimeLocation)
        {
            // TODO consider removing authorityInstance as it will only serve its purpose for one call here (at least at time of writing)...cost of keeping around even after it is dead?
            return nonAuthorityDesignTimeLocationByAuthorityInstanceMap.TryGetValue(authorityInstance, out nonAuthorityDesignTimeLocation);
        }

        static readonly HashSet<GONetParticipant> markedToBeRemotelyControlled = new HashSet<GONetParticipant>();
        static readonly Dictionary<GONetParticipant, ushort> server_markedToBeRemotelyControlledToIdMap = new Dictionary<GONetParticipant, ushort>();

        internal static bool IsMarkedToBeRemotelyControlled(GONetParticipant gonetParticipant)
        {
            return markedToBeRemotelyControlled.Contains(gonetParticipant) || server_markedToBeRemotelyControlledToIdMap.ContainsKey(gonetParticipant);
        }

        internal static bool Server_TryGetMarkToBeRemotelyControlledBy(GONetParticipant gonetParticipant, out ushort toBeRemotelyControlledByAuthorityId)
        {
            return server_markedToBeRemotelyControlledToIdMap.TryGetValue(gonetParticipant, out toBeRemotelyControlledByAuthorityId);
        }

        internal static void Server_MarkToBeRemotelyControlled(GONetParticipant gonetParticipant, ushort toBeRemotelyControlledByAuthorityId)
        {
            if (GONetMain.IsServer)
            {
                markedToBeRemotelyControlled.Add(gonetParticipant);
                server_markedToBeRemotelyControlledToIdMap[gonetParticipant] = toBeRemotelyControlledByAuthorityId;
            }
        }

        internal static bool Server_UnmarkToBeRemotelyControlled_ProcessingComplete(GONetParticipant gonetParticipant)
        {
            if (GONetMain.IsServer)
            {
                markedToBeRemotelyControlled.Remove(gonetParticipant);
                server_markedToBeRemotelyControlledToIdMap.Remove(gonetParticipant);
                return true;
            }

            return false;
        }

        internal static GONetParticipant Instantiate_MarkToBeRemotelyControlled(GONetParticipant prefab, Vector3 position, Quaternion rotation)
        {
            GONetParticipant instanceSoonToBeOwnedByServerAndRemotelyControlledByMe = UnityEngine.Object.Instantiate(prefab, position, rotation);

            // Copy design time metadata from prefab to spawned instance (for addressables support)
            // This is optional - if prefab doesn't have metadata, the system will work as before
            DesignTimeMetadata prefabMetadata = GetDesignTimeMetadata(prefab);
            if (prefabMetadata != null && !string.IsNullOrWhiteSpace(prefabMetadata.Location))
            {
                GONetLog.Debug($"Instantiate_MarkToBeRemotelyControlled: Copying metadata from prefab '{prefab.name}' - Location: {prefabMetadata.Location}");
                DesignTimeMetadata instanceMetadata = new DesignTimeMetadata
                {
                    Location = prefabMetadata.Location,
                    CodeGenerationId = prefabMetadata.CodeGenerationId,
                    UnityGuid = prefabMetadata.UnityGuid
                };
                SetDesignTimeMetadata(instanceSoonToBeOwnedByServerAndRemotelyControlledByMe, instanceMetadata);
            }
            else
            {
                // This is normal for prefabs that aren't addressable
                // The system will work as before - metadata will be initialized normally during Awake()
                GONetLog.Debug($"Instantiate_MarkToBeRemotelyControlled: Prefab '{prefab.name}' has no pre-cached metadata - will be initialized normally");
            }

            markedToBeRemotelyControlled.Add(instanceSoonToBeOwnedByServerAndRemotelyControlledByMe);
            return instanceSoonToBeOwnedByServerAndRemotelyControlledByMe;
        }

        /* TODO look back over the time in VCS when this came in....as using the same reference for all seems very wrong...it is to save memory and processing for auto-creating an insteance each time the implicit operator is going to/from string<->DTM 
        */
        private static readonly DesignTimeMetadata defaultDTM_EditorNotPlayMode = new DesignTimeMetadata()
        {
            CodeGenerationId = GONetParticipant.CodeGenerationId_Unset,
        };

        public static IEnumerable<DesignTimeMetadata> GetAllDesignTimeMetadata() => designTimeMetadataLookup;

        internal static void SetDesignTimeMetadata(GONetParticipant gONetParticipant, DesignTimeMetadata metadata)
        {
            designTimeMetadataLookup.Set(gONetParticipant, metadata);
        }
        public static void ClearAllDesignTimeMetadata()
        {
            designTimeMetadataLookup.Clear();
        }

        private static int callDepth = 0;
        public static DesignTimeMetadata GetDesignTimeMetadata(GONetParticipant gONetParticipant, bool force = false)
        {
            try
            {
                ++callDepth;
                if (callDepth > 1) return default;

                //GONetLog.Debug($"[DREETSleeps] designTimeMetadataLookup.Count: {designTimeMetadataLookup.Count}");

                if (!designTimeMetadataLookup.TryGetValue(gONetParticipant, out DesignTimeMetadata value) || force)
                {
                    // Safely get name and guid, handling destroyed objects during build process
                    string participantName = gONetParticipant ? gONetParticipant.name : "[destroyed]";
                    string participantGuid = gONetParticipant ? gONetParticipant.UnityGuid : "[unknown]";

                    // IMPORTANT: If caching is not complete and not forced, defer the lookup
                    if (!isDesignTimeMetadataCachingComplete && !force)
                    {
                        GONetLog.Debug($"GetDesignTimeMetadata: Deferring metadata lookup for '{participantName}' until caching completes");

                        // Add to deferred queue if not already there
                        if (!deferredLookupQueue.Contains(gONetParticipant))
                        {
                            deferredLookupQueue.Add(gONetParticipant);
                        }

                        // Return a temporary empty metadata to avoid crashes
                        bool shouldCreateNewDtm = Application.isPlaying || IsNewDtmForced;
                        DesignTimeMetadata tempMetadata = shouldCreateNewDtm
                            ? new DesignTimeMetadata() { CodeGenerationId = GONetParticipant.CodeGenerationId_Unset }
                            : defaultDTM_EditorNotPlayMode;
                        designTimeMetadataLookup.Set(gONetParticipant, tempMetadata);
                        return tempMetadata;
                    }

                    GONetLog.Debug($"GetDesignTimeMetadata: No cached metadata found for GONetParticipant '{participantName}', UnityGuid: {participantGuid}");

                    // DEBUG: Show what's actually in the cache (only when caching is complete)
                    GONetLog.Debug($"GetDesignTimeMetadata: Current cache has {designTimeMetadataLookup.Count} entries");
                    int logCount = 0;
                    foreach (var metadata in designTimeMetadataLookup)
                    {
                        if (logCount < 5) // Reduced to prevent spam
                        {
                            GONetLog.Debug($"  Cache metadata: Location='{metadata.Location}', CodeGenId={metadata.CodeGenerationId}");
                            logCount++;
                        }
                    }

                    // IMPORTANT: Before creating empty metadata, try to find it by location
                    // This handles cases where templates are loaded and should have their cached metadata
                    DesignTimeMetadata foundMetadata = null;

                    // First try resources:// prefix for prefabs (new format)
                    string expectedResourcesLocation = $"{RESOURCES_HIERARCHY_PREFIX}Assets/GONet/Resources/{participantName.Replace("(Clone)", "")}.prefab";
                    GONetLog.Debug($"GetDesignTimeMetadata: Trying resources location lookup: {expectedResourcesLocation}");
                    if (designTimeMetadataLookup.TryGetValue(expectedResourcesLocation, out foundMetadata))
                    {
                        GONetLog.Debug($"GetDesignTimeMetadata: Found metadata by resources location for '{participantName}'");
                        // Cache this found metadata for the participant
                        designTimeMetadataLookup.Set(gONetParticipant, foundMetadata);
                        value = foundMetadata;
                    }
                    // Fallback to project:// prefix for backwards compatibility
                    else if (designTimeMetadataLookup.TryGetValue($"{PROJECT_HIERARCHY_PREFIX}Assets/GONet/Resources/{participantName.Replace("(Clone)", "")}.prefab", out foundMetadata))
                    {
                        GONetLog.Debug($"GetDesignTimeMetadata: Found metadata by legacy project location for '{participantName}'");
                        // Cache this found metadata for the participant
                        designTimeMetadataLookup.Set(gONetParticipant, foundMetadata);
                        value = foundMetadata;
                    }
                    else
                    {
                        // Try other common locations with resources:// first, then project:// for backwards compatibility
                        string resourcesLocation = $"{RESOURCES_HIERARCHY_PREFIX}Assets/GONet/Resources/GONet/{participantName.Replace("(Clone)", "")}.prefab";
                        string legacyResourcesLocation = $"{PROJECT_HIERARCHY_PREFIX}Assets/GONet/Resources/GONet/{participantName.Replace("(Clone)", "")}.prefab";
                        GONetLog.Debug($"GetDesignTimeMetadata: Trying resources location lookup: {resourcesLocation}");
                        if (designTimeMetadataLookup.TryGetValue(resourcesLocation, out foundMetadata))
                        {
                            GONetLog.Debug($"GetDesignTimeMetadata: Found metadata by resources location for '{participantName}'");
                            designTimeMetadataLookup.Set(gONetParticipant, foundMetadata);
                            value = foundMetadata;
                        }
                        else if (designTimeMetadataLookup.TryGetValue(legacyResourcesLocation, out foundMetadata))
                        {
                            GONetLog.Debug($"GetDesignTimeMetadata: Found metadata by resources location for '{participantName}'");
                            designTimeMetadataLookup.Set(gONetParticipant, foundMetadata);
                            value = foundMetadata;
                        }
                        else
                        {
                            // Last resort: create empty metadata
                            bool shouldCreateNewDtm = Application.isPlaying || IsNewDtmForced;
                            DesignTimeMetadata metadata = shouldCreateNewDtm
                                ? new DesignTimeMetadata()
                                {
                                    CodeGenerationId = GONetParticipant.CodeGenerationId_Unset,
                                }
                                : defaultDTM_EditorNotPlayMode;
                            GONetLog.Warning($"GetDesignTimeMetadata: Creating empty metadata for '{participantName}' - no location found");
                            designTimeMetadataLookup.Set(gONetParticipant, metadata);
                            value = metadata;
                        }
                    }
                }
                else
                {
                    // Safely get name, handling destroyed objects during build process
                    string participantName = gONetParticipant ? gONetParticipant.name : "[destroyed]";
                    // Commented out to reduce log spam - this is called frequently during sync processing
                    // GONetLog.Debug($"GetDesignTimeMetadata: Found cached metadata for GONetParticipant '{participantName}', Location: {value.Location}");
                }
                return value;
            }
            finally
            {
                --callDepth;
            }
        }

        /// <summary>
        /// PRE: <see cref="GetDesignTimeMetadata(string)"/> already called passing in <paramref name="fullPathInScene"/> so <see cref="designTimeMetadataLookup"/> will already have
        /// POST: If it worked, <paramref name="gONetParticipant"/>.<see cref="GONetParticipant.IsDesignTimeMetadataInitd"/> set to true.
        /// </summary>
        internal static void InitDesignTimeMetadata(string fullPathInScene, GONetParticipant gONetParticipant)
        {
            try
            {
                //GONetLog.Debug($"dreetsi init. depth: {callDepth}, fullPathInScene: {fullPathInScene}, gnp.guid: {gONetParticipant.UnityGuid}, gnp.spawned? {gONetParticipant.WasInstantiated} is cached? {IsDesignTimeMetadataCached}");

                //IsDesignTimeMetadataCached = true; string fullUniquePath = DesignTimeMetadata.GetFullUniquePathInScene(gonetParticipant);

                ++callDepth;
                if (callDepth > 1) return;

                DesignTimeMetadata metadata = default;

                // First try to get metadata by scene path (works for scene objects)
                metadata = GetDesignTimeMetadata(fullPathInScene, canBypassDepthCheck: true);
                GONetLog.Debug($"InitDesignTimeMetadata: Scene path lookup for '{gONetParticipant.gameObject.name}' at '{fullPathInScene}' returned: {(metadata != default ? metadata.Location : "NULL")}");

                // If not found by scene path (empty location means it wasn't found) and object has UnityGuid, try prefab metadata lookup (for instantiated objects)
                if (string.IsNullOrEmpty(metadata.Location) && !string.IsNullOrWhiteSpace(gONetParticipant.UnityGuid))
                {
                    GONetLog.Debug($"InitDesignTimeMetadata: Scene lookup failed for '{gONetParticipant.gameObject.name}', trying UnityGuid lookup: {gONetParticipant.UnityGuid}");

                    GONetLog.Debug($"InitDesignTimeMetadata: Searching for UnityGuid '{gONetParticipant.UnityGuid}' in designTimeMetadataLookup...");
                    if (!designTimeMetadataLookup.TryGetValueByUnityGuid(gONetParticipant.UnityGuid, out metadata))
                    {
                        GONetLog.Debug($"InitDesignTimeMetadata: TryGetValueByUnityGuid failed, trying designTimeMetadataToProjectTemplate...");
                        metadata = designTimeMetadataToProjectTemplate.Keys.FirstOrDefault(x => x.UnityGuid == gONetParticipant.UnityGuid);
                        if (metadata == default)
                        {
                            // Let's debug what UnityGuids are actually available
                            var availableGuids = designTimeMetadataToProjectTemplate.Keys.Where(x => !string.IsNullOrEmpty(x.UnityGuid)).Select(x => x.UnityGuid).ToArray();
                            GONetLog.Error($"Could not find design time metadata for GONetParticipant with UnityGuid: {gONetParticipant.UnityGuid}, GameObject: {gONetParticipant.gameObject.name}, Scene path: {fullPathInScene}. Available UnityGuids: [{string.Join(", ", availableGuids)}]");
                        }
                        else
                        {
                            GONetLog.Debug($"InitDesignTimeMetadata: Found metadata via designTimeMetadataToProjectTemplate for '{gONetParticipant.gameObject.name}', Location: {metadata.Location}");
                        }
                    }
                    else
                    {
                        GONetLog.Debug($"InitDesignTimeMetadata: Found metadata via TryGetValueByUnityGuid for '{gONetParticipant.gameObject.name}', Location: {metadata.Location}");
                    }
                }

                GONetLog.Debug($"InitDesignTimeMetadata: Final metadata for '{gONetParticipant.gameObject.name}': {(metadata != default ? $"Location={metadata.Location}, CodeGenId={metadata.CodeGenerationId}" : "NULL")}");

                designTimeMetadataLookup.Set(gONetParticipant, metadata);

                gONetParticipant.IsDesignTimeMetadataInitd = true;
                //GONetLog.Debug($"dreetsi init dTM.. loc: {metadata.Location}, getId: {metadata.CodeGenerationId}");
            }
            finally
            {
                --callDepth;
            }
        }

        public static bool AnyDesignTimeMetadata(string designTimeLocation)
        {
            return designTimeMetadataLookup.TryGetValue(designTimeLocation, out DesignTimeMetadata value);
        }

        public static DesignTimeMetadata GetDesignTimeMetadata(string designTimeLocation, bool canBypassDepthCheck = false)
        {
            try
            {
                ++callDepth;
                if (!canBypassDepthCheck && callDepth > 1) return default;

                // IMPORTANT: Prevent caching metadata with empty/null location strings
                // This prevents the "TON CLEETLE!" error that occurs when Set() is called with an invalid location
                // If designTimeLocation is empty, it means the GONetParticipant's metadata wasn't initialized yet
                if (string.IsNullOrWhiteSpace(designTimeLocation))
                {
                    GONetLog.Warning($"GetDesignTimeMetadata called with empty/null location. This likely means metadata wasn't initialized before creating a spawn event. Returning default metadata.");
                    return default;
                }

                if (!designTimeMetadataLookup.TryGetValue(designTimeLocation, out DesignTimeMetadata value))
                {
                    bool shouldCreateNewDtm = Application.isPlaying || IsNewDtmForced;
                    DesignTimeMetadata metadata = shouldCreateNewDtm
                        ? new DesignTimeMetadata()
                        {
                            CodeGenerationId = GONetParticipant.CodeGenerationId_Unset,
                        }
                        : defaultDTM_EditorNotPlayMode;

                    designTimeMetadataLookup.Set(designTimeLocation, metadata);
                    value = metadata;
                }
                return value;
            }
            finally
            {
                --callDepth;
            }
        }

        public static void ChangeLocation(string previousLocation, string newLocation, DesignTimeMetadata value)
        {
            designTimeMetadataLookup.ChangeLocation(previousLocation, newLocation, value);
        }

        internal static string GetDesignTimeMetadata_Location(GONetParticipant gONetParticipant)
        {
            DesignTimeMetadata metadata = GetDesignTimeMetadata(gONetParticipant);
            if ((object)metadata == null)
            {
                if (callDepth == 0)
                {
                    Debug.LogError($"Unexpected situation.  callDepth should be > 0 to cause the inability to get the {nameof(DesignTimeMetadata)} from the {nameof(gONetParticipant)}, but callDepth is 0.");
                }
                return string.Empty;
            }

            return metadata.Location;
        }
    }

    public class DesignTimeMetadataDictionary : IEnumerable<DesignTimeMetadata>
    {
        private static readonly Dictionary<GONetParticipant, DesignTimeMetadata> designTimeMetadataByGNP = new(256);
        private static readonly Dictionary<string, DesignTimeMetadata> designTimeMetadataByLocation = new(256);

        public int Count => designTimeMetadataByGNP.Count + designTimeMetadataByLocation.Count;

        public void Clear()
        {
            designTimeMetadataByGNP.Clear();
            designTimeMetadataByLocation.Clear();
        }

        public void Set(GONetParticipant keyGNP, DesignTimeMetadata value)
        {
            if ((object)keyGNP == null || (object)value == default)
            {
                string gnpInfo = keyGNP != null ? $"GameObject: {keyGNP.gameObject.name}, UnityGuid: {keyGNP.UnityGuid}" : "null";
                throw new ArgumentException($"BLASTPHEAMOUSE! GONetParticipant: {gnpInfo}, metadata is null: {(object)value == default}");
            }

            designTimeMetadataByGNP[keyGNP] = value;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            if (string.IsNullOrWhiteSpace(keyGNP.DesignTimeLocation))
            {
                return;
            }
            designTimeMetadataByLocation[keyGNP.DesignTimeLocation] = value;
        }

        public void Set(string keyLocation, DesignTimeMetadata value)
        {
            if (string.IsNullOrWhiteSpace(keyLocation) || (object)value == default)
            {
                string locationInfo = string.IsNullOrWhiteSpace(keyLocation) ? "empty/null" : keyLocation;
                throw new ArgumentException($"TON CLEETLE! Location: '{locationInfo}', metadata is null: {(object)value == default}");
            }

            designTimeMetadataByLocation[keyLocation] = value;
            // GONetLog.Debug($"DesignTimeMetadataLookup.Set: Cached metadata for location: {keyLocation}");
        }

        public void ChangeLocation(string previousLocation, string newLocation, DesignTimeMetadata value)
        {
            if (string.IsNullOrWhiteSpace(newLocation) || (object)value == default)
            {
                throw new ArgumentException("BOMME BEETLE");
            }

            if (!string.IsNullOrWhiteSpace(previousLocation) && designTimeMetadataByLocation.TryGetValue(previousLocation, out var previousValue))
            {
                if (previousValue != value)
                {
                    throw new InvalidOperationException();
                }

                designTimeMetadataByLocation.Remove(previousLocation);
            }

            designTimeMetadataByLocation[newLocation] = value;
        }

        public bool TryGetValue(GONetParticipant keyGNP, out DesignTimeMetadata value)
        {
            if ((object)keyGNP == null)
            {
                throw new ArgumentException("SLAN KEATULL");
            }

            if (designTimeMetadataByGNP.TryGetValue(keyGNP, out value))
            {
                return true;
            }

            /////////////////////////////////////////////////////////////////////////////////////////////////

            if (string.IsNullOrWhiteSpace(keyGNP.DesignTimeLocation))
            {
                return false;
            }

            return designTimeMetadataByLocation.TryGetValue(keyGNP.DesignTimeLocation, out value);
        }

        public bool TryGetValue(string keyLocation, out DesignTimeMetadata value)
        {
            if (string.IsNullOrWhiteSpace(keyLocation))
            {
                value = default;
                return false;
            }

            return designTimeMetadataByLocation.TryGetValue(keyLocation, out value);
        }

        public bool TryGetValueByUnityGuid(string unityGuid, out DesignTimeMetadata value)
        {
            if (string.IsNullOrWhiteSpace(unityGuid))
            {
                value = default;
                return false;
            }

            KeyValuePair<GONetParticipant, DesignTimeMetadata> matchKVP_byGNP = 
                designTimeMetadataByGNP.FirstOrDefault(x => x.Key.UnityGuid == unityGuid || x.Value.UnityGuid == unityGuid);
            if (matchKVP_byGNP.Equals(default(KeyValuePair<GONetParticipant, DesignTimeMetadata>)))
            {
                KeyValuePair<string, DesignTimeMetadata> matchKVP_byLocation = 
                    designTimeMetadataByLocation.FirstOrDefault(x => x.Value.UnityGuid == unityGuid);
                if (matchKVP_byLocation.Equals(default(KeyValuePair<string, DesignTimeMetadata>)))
                {
                    value = default;
                    return false;
                }

                value = matchKVP_byLocation.Value;
                return true;
            }

            value = matchKVP_byGNP.Value;
            return true;
        }


        IEnumerator<DesignTimeMetadata> IEnumerable<DesignTimeMetadata>.GetEnumerator()
        {
            HashSet<DesignTimeMetadata> all = new HashSet<DesignTimeMetadata>(); // TODO memory mgt improvement needed!

            foreach (var dtm in designTimeMetadataByGNP.Values) all.Add(dtm);
            foreach (var dtm in designTimeMetadataByLocation.Values) all.Add(dtm);

            return all.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            HashSet<DesignTimeMetadata> all = new HashSet<DesignTimeMetadata>(); // TODO memory mgt improvement needed!

            foreach (var dtm in designTimeMetadataByGNP.Values) all.Add(dtm);
            foreach (var dtm in designTimeMetadataByLocation.Values) all.Add(dtm);

            return all.GetEnumerator();
        }
    }
}
