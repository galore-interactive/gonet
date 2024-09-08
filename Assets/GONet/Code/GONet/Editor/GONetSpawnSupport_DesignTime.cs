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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEditor.FilePathAttribute;

namespace GONet.Editor
{
    /// <summary>
    /// sister class of <see cref="GONetSpawnSupport_Runtime"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class GONetSpawnSupport_DesignTime
    {
        static GONetSpawnSupport_DesignTime()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif
        }

        private static void OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly()
        {
            EnsureDesignTimeLocationsCurrent_ProjectOnly();
        }

        internal static void EnsureDesignTimeLocationsCurrent_ProjectOnly()
        {
            // clear it now as it will be built back up below
            RemoveFromPersistence_WherePrefixMatches(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX);

            // IMPORTANT: have to load them all up for else the following call will not "find" them all and only the ones that happened to be loaded already would be found/processed
            Resources.LoadAll<GONetParticipant>(string.Empty);
            foreach (var gonetParticipant in Resources.FindObjectsOfTypeAll<GONetParticipant>())
            {
                OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly_Single(gonetParticipant);
            }

            // IMPORTANT: have to do this because the above call to Resources.FindObjectsOfTypeAll<GONetParticipant>() does NOT identify a prefab that just had GNP added to it this frame!!!
            foreach (GONetParticipant gonetParticipant in GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GetGNPsAddedToPrefabThisFrame())
            {
                OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly_Single(gonetParticipant);
            }
        }

        internal static void OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly_Single(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant != null)
            {
                string projectPath = AssetDatabase.GetAssetPath(gonetParticipant);
                bool isProjectAsset = !string.IsNullOrWhiteSpace(projectPath);
                if (isProjectAsset)
                {
                    string currentLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);

                    // this seems unnecessary and problematic for project assets: 
                    EnsureDesignTimeLocationCurrent(gonetParticipant, currentLocation); // have to do proper unity serialization stuff for this to stick!

                    //gonetParticipant.DesignTimeLocation = currentLocation; // so, set it  directly and it seems to stick/save/persist just fine
                }
            }
            else if ((object)gonetParticipant != null && !string.IsNullOrWhiteSpace(gonetParticipant.DesignTimeLocation))
            {
                EnsureExistsInPersistence_WithTheseValues(gonetParticipant.DesignTimeLocation);
            }
        }

        private static void OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly()
        {
            GONetLog.Debug($"FRAME: {Time.frameCount} .... OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly");

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // SKIP code gen on first/oth frame due to this method being called when coming out of other GONet generation stuff (e.g., editor support: "Fix GONet Generated Code")
            if (Time.frameCount == 0) return;
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            bool isHierarchyChangingDueToExitingPlayModeInEditor = 
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange.HasValue && 
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.EnteredEditMode &&
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount == Time.frameCount; // IMPORTANT: this is how we know it "just" changed from play to edit mode...otherwise we could never run the logic we want after exiting the play mode and we start messing around with the hierarchy

            if (!Application.isPlaying && !isHierarchyChangingDueToExitingPlayModeInEditor) // it would not be design time if we are playing (in editor) now would it?
            {
                bool somethingChanged = false;
                int count = SceneManager.loadedSceneCount;
                for (int i = 0; i < count; ++i)
                {
                    Scene loadedScene = EditorSceneManager.GetSceneAt(i);

                    const string SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES = "/";
                    string scenePrefix = string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, loadedScene.name, SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES);
                    RemoveFromPersistence_WherePrefixMatches(scenePrefix); // clear anything already present from these scene now as it will be built back up below

                    foreach (var rootGO in loadedScene.GetRootGameObjects())
                    {
                        foreach (var gonetParticipant in rootGO.GetComponentsInChildren<GONetParticipant>())
                        {
                            string fullUniquePath = DesignTimeMetadata.GetFullUniquePathInScene(gonetParticipant);
                            if (fullUniquePath != gonetParticipant.DesignTimeLocation)
                            {
                                somethingChanged = true;
                                EnsureDesignTimeLocationCurrent(gonetParticipant, fullUniquePath); // have to do proper unity serialization stuff for this to stick!
                            }
                            else
                            {
                                EnsureExistsInPersistence_WithTheseValues(fullUniquePath); // although this is also called inside EnsureDesignTimeLocationCurrent, we need to call it here too in case the generated file this information goes into is manually deleted on the filesystem and the information was lost...this is a failsafe method to ensure it is populated!
                            }
                        }
                    }
                }

                if (somethingChanged)
                {
                    // NOTE: there is no longer anything else to do since we save the data outside the GNP itself in the DesignTimeLocations.json
                    //EditorSceneManager.MarkAllScenesDirty();
                    //EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // this may be too much....they will save when they want to...normally
                }
            }
        }

        internal static void EnsureExistsInPersistence_WithTheseValues(DesignTimeMetadata ensureExistsDtm)
        {
            IEnumerable<DesignTimeMetadata> persistedDtms = GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();

            bool doesAlreadyExist = persistedDtms.Any(x => x.Location == ensureExistsDtm.Location);
            if (doesAlreadyExist)
            {
                if (ensureExistsDtm.CodeGenerationId != GONetParticipant.CodeGenerationId_Unset || 
                    ensureExistsDtm.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX)) // IMPORTANT: allow persisting "project://" when code gen 0 (in hopes this gets corrected later, but need it in there now!)
                {
                    int iMatch = 0;
                    foreach (DesignTimeMetadata persistedDtm in persistedDtms.Where(x => x.Location == ensureExistsDtm.Location))
                    {
                        // update other info for those matching location

                        if (ensureExistsDtm.CodeGenerationId != GONetParticipant.CodeGenerationId_Unset)
                        {
                            persistedDtm.CodeGenerationId = ensureExistsDtm.CodeGenerationId;
                        }

                        persistedDtm.UnityGuid = ensureExistsDtm.UnityGuid;
                        
                        if (++iMatch > 1)
                        {
                            Debug.LogWarning($"More than 1 match of location: {ensureExistsDtm.Location}, match# {iMatch}");
                        }
                    }
                    OverwritePersistenceWith(persistedDtms);
                }
            }
            else
            {
                var updatedListDtms = new List<DesignTimeMetadata>(persistedDtms);
                updatedListDtms.Add(ensureExistsDtm);
                OverwritePersistenceWith(updatedListDtms);
            }
        }

        static void RemoveFromPersistence_WherePrefixMatches(string prefixToMatch)
        {
            IEnumerable<DesignTimeMetadata> all = GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();

            all = all.Where(x => !x.Location.StartsWith(prefixToMatch));

            OverwritePersistenceWith(all);
        }

        /// <summary>
        /// Do all proper unity serialization stuff or else a change will NOT stick/save/persist.
        /// </summary>
        private static void EnsureDesignTimeLocationCurrent(GONetParticipant gonetParticipant, string currentLocation)
        {
            string goName = gonetParticipant.gameObject.name; // IMPORTANT: after a call to serializedObject.ApplyModifiedProperties(), gonetParticipant is unity "null" and this line MUst come before that!

            /*
            SerializedObject serializedObject = new SerializedObject(gonetParticipant); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
            SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.DesignTimeLocation));
            serializedObject.Update();
            serializedProperty.stringValue = currentLocation; // set it this way or else it will NOT work with prefabs!
            gonetParticipant.DesignTimeLocation = currentLocation; // doubly sure
            serializedObject.ApplyModifiedProperties();
            */

            GONetLog.Debug("set design time location for name: " + goName + " to NEW value: " + currentLocation);

            DesignTimeMetadata designTimeMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(gonetParticipant);
            designTimeMetadata.Location = currentLocation;
            
            string unityGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gonetParticipant));
            designTimeMetadata.UnityGuid = unityGuid;

            {
                SerializedObject serializedObject = new SerializedObject(gonetParticipant); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
                SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.UnityGuid));
                serializedObject.Update();
                serializedProperty.stringValue = unityGuid; // set it this way or else it will NOT work with prefabs!
                gonetParticipant.UnityGuid = unityGuid;
                serializedObject.ApplyModifiedProperties();
            }

            EnsureExistsInPersistence_WithTheseValues(designTimeMetadata);
        }

        /// <summary>
        /// POST: contents of <see cref="allDesignTimeLocationsEncountered"/> persisted.
        /// </summary>
        private static void OverwritePersistenceWith(IEnumerable<DesignTimeMetadata> newCompleteDesignTimeLocations)
        {
            string directory = Path.Combine(Application.streamingAssetsPath, GONetSpawnSupport_Runtime.GONET_STREAMING_ASSETS_FOLDER);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var invalidMofosWillNotPersist = newCompleteDesignTimeLocations
                .Where(x => string.IsNullOrWhiteSpace(x.Location) || 
                    (x.Location.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX) 
                        && x.CodeGenerationId == GONetParticipant.CodeGenerationId_Unset));// IMPORTANT: allow persisting "project://" when code gen 0 (in hopes this gets corrected later, but need it in there now!)
            foreach (var invalid in invalidMofosWillNotPersist)
            {
                GONetLog.Warning($"This little piggy is not going to the market!  He has some missing data that is not cool to persist!  Most likely, this is OK to overlook based on latest implementation preference and reliance on project over scene centricity.  As json: {JsonUtility.ToJson(invalid)}");
            }

            DesignTimeMetadataLibrary designTimeMetadataLibrary = new DesignTimeMetadataLibrary()
            {
                Entries = newCompleteDesignTimeLocations
                    .Where(x => !invalidMofosWillNotPersist.Contains(x)).OrderBy(x => x.Location).ToArray(),
            };

            string fullPath = Path.Combine(Application.streamingAssetsPath, GONetSpawnSupport_Runtime.DESIGN_TIME_METADATA_FILE_POST_STREAMING_ASSETS);
            GONetLog.Debug($"writing all text to: {fullPath}");
            File.WriteAllText(fullPath, JsonUtility.ToJson(designTimeMetadataLibrary, prettyPrint: true));
        }
    }
}
