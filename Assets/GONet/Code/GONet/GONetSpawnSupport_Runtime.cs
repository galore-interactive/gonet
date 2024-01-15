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

using GONet.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GONet
{
    public static class GONetSpawnSupport_Runtime
    {
        public const string GONET_STREAMING_ASSETS_FOLDER = "GONet";
        public static readonly string DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS = Path.Combine(GONET_STREAMING_ASSETS_FOLDER, "DesignTimeLocations.txt");

        public const string SCENE_HIERARCHY_PREFIX = "scene://";
        public const string PROJECT_HIERARCHY_PREFIX = "project://";
        public const string RESOURCES = "Resources/";

        private static readonly string[] ALL_END_OF_LINE_OPTIONS = new[] { "\r\n", "\r", "\n" };

        private static readonly Dictionary<string, GONetParticipant> designTimeLocationToProjectTemplate = new Dictionary<string, GONetParticipant>(100);

        public static IEnumerable<string> LoadDesignTimeLocationsFromPersistence()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WEBGL)
            Debug.Log($"About to check out design time file at URI {fullPath}.");
            // As per, https://docs.unity3d.com/Manual/StreamingAssets.html :
            // "On Android and WebGL platforms, it’s not possible to access the streaming asset files directly via file system APIs and
            // streamingAssets path because these platforms return a URL. Use the UnityWebRequest class to access the content instead."
            Task<string> fileContentsTask = LoadFileFromWeb_Task(fullPath);
            if (fileContentsTask.Wait(5000))
            {
                string fileContents = fileContentsTask.Result;
                return fileContents.Split(ALL_END_OF_LINE_OPTIONS, StringSplitOptions.None);
            }
#else
            Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                string fileContents = File.ReadAllText(fullPath);
                return fileContents.Split(ALL_END_OF_LINE_OPTIONS, StringSplitOptions.None);
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

        public static void CacheAllProjectDesignTimeLocations(MonoBehaviour coroutineOwner)
        {
            coroutineOwner.StartCoroutine(
                CacheAllProjectDesignTimeLocations_Coroutine(CacheAllProjectDesignTimeLocations)
            );
        }

        private static IEnumerator CacheAllProjectDesignTimeLocations_Coroutine(Action<IEnumerable<string>> processResults)
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS);
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_WEBGL)
            Debug.Log($"About to check out design time file at URI {fullPath}.");
            // As per, https://docs.unity3d.com/Manual/StreamingAssets.html :
            // "On Android and WebGL platforms, it’s not possible to access the streaming asset files directly via file system APIs and
            // streamingAssets path because these platforms return a URL. Use the UnityWebRequest class to access the content instead."
            yield return LoadFileFromWeb_Coroutine(fullPath, (fileContents) =>
                {
                    IEnumerable<string> lines = fileContents.Split(ALL_END_OF_LINE_OPTIONS, StringSplitOptions.None);
                    processResults(lines);
                });
                
#else
            Debug.Log($"About to check out design time file at {fullPath}.  Does it exist? {File.Exists(fullPath)}");
            if (File.Exists(fullPath))
            {
                string fileContents = File.ReadAllText(fullPath);
                IEnumerable<string> lines = fileContents.Split(ALL_END_OF_LINE_OPTIONS, StringSplitOptions.None);
                processResults(lines);
            }
            yield return null;
#endif
        }

        private static void CacheAllProjectDesignTimeLocations(IEnumerable<string> allProjectDesignTimeLocations)
        {
            foreach (string designTimeLocation in allProjectDesignTimeLocations)
            {
                if (designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
                    GONetParticipant template = LookupResourceTemplateFromProjectLocation(designTimeLocation.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty));
                    if ((object)template != null)
                    {
                        GONetLog.Debug("found template for design time location: " + designTimeLocation);
                        designTimeLocationToProjectTemplate[designTimeLocation] = template;
                    }
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

        public static GONetParticipant LookupTemplateFromDesignTimeLocation(string designTimeLocation)
        {
            if (designTimeLocation != null)
            {
                if (designTimeLocation.StartsWith(SCENE_HIERARCHY_PREFIX))
                {
                    string fullUniquePath = designTimeLocation.Replace(SCENE_HIERARCHY_PREFIX, string.Empty);
                    return HierarchyUtils.FindByFullUniquePath(fullUniquePath).GetComponent<GONetParticipant>();
                }
                else if (designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
                    return designTimeLocationToProjectTemplate[designTimeLocation];
                }
            }

            throw new ArgumentException(string.Concat("Must include supported prefix defined as const herein. value received: ", designTimeLocation), nameof(designTimeLocation));
        }

        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.designTimeLocation;
            return authorityInstance;
        }


        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant, Vector3, Quaternion)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal, Vector3 position, Quaternion rotation)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal, position, rotation);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.designTimeLocation;
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
    }
}
