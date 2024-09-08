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

namespace GONet
{
    public static class GONetSpawnSupport_Runtime
    {
        public static bool IsNewDtmForced { get;set; }

        public const string GONET_STREAMING_ASSETS_FOLDER = "GONet";
        public static readonly string DESIGN_TIME_METADATA_FILE_POST_STREAMING_ASSETS = Path.Combine(GONET_STREAMING_ASSETS_FOLDER, "DesignTimeMetadata.json");

        public const string SCENE_HIERARCHY_PREFIX = "scene://";
        public const string PROJECT_HIERARCHY_PREFIX = "project://";
        public const string RESOURCES = "Resources/";

        private static readonly string[] ALL_END_OF_LINE_OPTIONS = new[] { "\r\n", "\r", "\n" };

        private static readonly Dictionary<DesignTimeMetadata, GONetParticipant> designTimeMetadataToProjectTemplate = new (100);
        private static readonly DesignTimeMetadataDictionary designTimeMetadataLookup = new();

        static GONetSpawnSupport_Runtime()
        {
            GONetLog.Debug($"---------------------------------NEW GONetSpawnSupport_Runtime(), so, all lookup cleared out!!!!");
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
            Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
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

        public static void CacheAllProjectDesignTimeMetadata(MonoBehaviour coroutineOwner)
        {
            //GONetLog.Debug("dreetsi cache begin");
            IsDesignTimeMetadataCached = false;
            coroutineOwner.StartCoroutine(
                CacheAllProjectDesignTimeMetadata_Coroutine(CacheAllDesignTimeMetadata)
            );
        }

        private static IEnumerator CacheAllProjectDesignTimeMetadata_Coroutine(Action<IEnumerable<DesignTimeMetadata>> processResults)
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
                
#else
            //Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                string fileContentsJson = File.ReadAllText(fullPath);
                DesignTimeMetadataLibrary library = JsonUtility.FromJson<DesignTimeMetadataLibrary>(fileContentsJson);
                processResults(library.Entries);
            }
            IsDesignTimeMetadataCached = true;

            yield return null;
#endif
        }

        private static void CacheAllDesignTimeMetadata(IEnumerable<DesignTimeMetadata> allProjectDesignTimeMetadata)
        {
            foreach (DesignTimeMetadata designTimeMetadata in allProjectDesignTimeMetadata)
            {
                if (designTimeMetadata.Location.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
                    GONetParticipant template = LookupResourceTemplateFromProjectLocation(designTimeMetadata.Location.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty));
                    if ((object)template != null)
                    {
                        //GONetLog.Debug("found TEMPLATE for design time location: " + designTimeMetadata.Location);
                        designTimeMetadataToProjectTemplate[designTimeMetadata] = template;
                    }
                }
                else if (designTimeMetadata.Location.StartsWith(SCENE_HIERARCHY_PREFIX))
                {
                    //GONetLog.Debug($"associating SCENE design time location: {designTimeMetadata.Location}");
                    designTimeMetadataLookup.Set(designTimeMetadata.Location, designTimeMetadata);
                }
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

        public static GONetParticipant LookupTemplateFromDesignTimeMetadata(DesignTimeMetadata designTimeMetadata)
        {
            if (designTimeMetadata != null)
            {
                if (designTimeMetadata.Location.StartsWith(SCENE_HIERARCHY_PREFIX))
                {
                    string fullUniquePath = designTimeMetadata.Location.Replace(SCENE_HIERARCHY_PREFIX, string.Empty);
                    return HierarchyUtils.FindByFullUniquePath(fullUniquePath).GetComponent<GONetParticipant>();
                }
                else if (designTimeMetadata.Location.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
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

        private static int callDepth = 0;
        public static DesignTimeMetadata GetDesignTimeMetadata(GONetParticipant gONetParticipant)
        {
            try
            {
                ++callDepth;
                if (callDepth > 1) return default;

                //GONetLog.Debug($"[DREETSleeps] designTimeMetadataLookup.Count: {designTimeMetadataLookup.Count}");

                if (!designTimeMetadataLookup.TryGetValue(gONetParticipant, out DesignTimeMetadata value))
                {
                    bool shouldCreateNewDtm = Application.isPlaying || IsNewDtmForced;
                    DesignTimeMetadata metadata = shouldCreateNewDtm
                        ? new DesignTimeMetadata()
                        {
                            CodeGenerationId = GONetParticipant.CodeGenerationId_Unset,
                        }
                        : defaultDTM_EditorNotPlayMode;
                    GONetLog.Debug($"[DREETS] NEW NEW NEW NEW NEW NEW? {shouldCreateNewDtm}"); // monitor how often new is created!!! do we need a pool ???
                    designTimeMetadataLookup.Set(gONetParticipant, metadata);
                    value = metadata;
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
                if (!string.IsNullOrWhiteSpace(gONetParticipant.UnityGuid)) //(gONetParticipant.WasInstantiated)
                {
                    // NOTE: inside here this GNP could either appear in the scene or have been spawned, but since it has a UnityGuid, we know it is a prefab

                    if (!designTimeMetadataLookup.TryGetValueByUnityGuid(gONetParticipant.UnityGuid, out metadata))
                    {
                        metadata = designTimeMetadataToProjectTemplate.Keys.FirstOrDefault(x => x.UnityGuid == gONetParticipant.UnityGuid);
                        if (metadata == default)
                        {
                            GONetLog.Error($"dreetsi  snafooery");
                        }
                    }
                }
                else
                {
                    metadata = GetDesignTimeMetadata(fullPathInScene, canBypassDepthCheck: true);
                }
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

                if (!designTimeMetadataLookup.TryGetValue(designTimeLocation, out DesignTimeMetadata value))
                {
                    bool shouldCreateNewDtm = Application.isPlaying || IsNewDtmForced;
                    DesignTimeMetadata metadata = shouldCreateNewDtm
                        ? new DesignTimeMetadata()
                        {
                            CodeGenerationId = GONetParticipant.CodeGenerationId_Unset,
                        }
                        : defaultDTM_EditorNotPlayMode;
                    GONetLog.Debug($"[DREETS] NEW NEW NEW NEW NEW NEW? {shouldCreateNewDtm}"); // monitor how often new is created!!! do we need a pool ???
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

        public int Count => designTimeMetadataByGNP.Count;

        public void Set(GONetParticipant keyGNP, DesignTimeMetadata value)
        {
            if ((object)keyGNP == null || (object)value == default)
            {
                throw new ArgumentException($"BLASTPHEAMOUSE!  1: {((object)keyGNP == null)}, 2: {((object)value == default)}");
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
                throw new ArgumentException("TON CLEETLE");
            }

            designTimeMetadataByLocation[keyLocation] = value;
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
