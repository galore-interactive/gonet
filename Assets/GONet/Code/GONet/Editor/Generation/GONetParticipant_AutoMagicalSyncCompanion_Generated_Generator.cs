/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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

using Assets.GONet.Code.GONet.Editor.Generation;
using GONet.Editor;
using GONet.Utils;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Generation
{
    [InitializeOnLoad] // ensure class initializer is called whenever scripts recompile
    public static class GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator
    {
        /// <summary>
        /// IMPORTANT: This is duplicate thought as <see cref="GONetParticipant_ComponentsWithAutoSyncMembers"/>...TODO: need to merge these!
        /// 
        /// Each item in the outer list maps to a single <see cref="GameObject"/> found in a Unity scene and/or prefab that has a <see cref="GONetParticipant"/> installed on it AND also has
        /// one or more <see cref="MonoBehaviour"/> instances installed on it that have one or more fields/properties decorated with <see cref="GONetAutoMagicalSyncAttribute"/>.
        /// 
        /// The index of the outer item matches to the generated class name suffix (e.g., GONetParticipant_AutoMagicalSyncCompanion_Generated_15).
        /// The index of the inner item matches to the place in order of that attribute being encountered during discovery/enumeration (has to be deterministic).
        /// </summary>
        static List<List<AutoMagicalSyncAttribute_GenerationSupport>> gonetParticipantCombosEncountered = new List<List<AutoMagicalSyncAttribute_GenerationSupport>>();

        internal const string GENERATION_FILE_PATH = "Assets/GONet/Code/GONet/Generation/";
        internal const string GENERATED_FILE_PATH = GENERATION_FILE_PATH + "Generated/";
        const string FILE_SUFFIX = ".cs";

        static GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;

            GONetParticipant.DefaultConstructorCalled += GONetParticipant_EditorOnlyDefaultContructor;
            GONetParticipant.ResetCalled += GONetParticipant_EditorOnlyReset;
            GONetParticipant.AwakeCalled += GONetParticipant_EditorOnlyAwake;
            GONetParticipant.OnDestroyCalled += GONetParticipant_EditorOnlyOnDestroy;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif

            EditorSceneManager.sceneSaved += EditorSceneManager_sceneSaved;

            EditorApplication.quitting += OnEditorApplicationQuitting;
        }

        static readonly Dictionary<int, List<GONetParticipant>> OnPostprocessAllAssets_By_frameCountMap = new Dictionary<int, List<GONetParticipant>>();
        static GONetParticipant[] OnPostprocessAllAssets_By_frameCountMap_empty = new GONetParticipant[0];
        internal static IEnumerable<GONetParticipant> GetGNPsAddedToPrefabThisFrame()
        {
            List<GONetParticipant> gnpsAddedToPrefabsThisFrame;
            if (!OnPostprocessAllAssets_By_frameCountMap.TryGetValue(Time.frameCount, out gnpsAddedToPrefabsThisFrame))
            {
                return OnPostprocessAllAssets_By_frameCountMap_empty;
            }
            return gnpsAddedToPrefabsThisFrame;
        }

        internal static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            HashSet<string> existingGONetParticipantAssetPaths = new HashSet<string>();
            IEnumerable<string> designTimeLocations_gonetParticipants = GONetSpawnSupport_Runtime.LoadDesignTimeLocationsFromPersistence();
            
            foreach (string importedAsset in importedAssets)
            {
                //Debug.Log("Reimported Asset: " + importedAsset);

                string possibleDesignTimeAssetLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, importedAsset);
                if (designTimeLocations_gonetParticipants.Contains(possibleDesignTimeAssetLocation))
                {
                    existingGONetParticipantAssetPaths.Add(importedAsset);
                }
                else
                {
                    // let's see if it is a new GNP and act accordingly as this is the only place it gets picked up automatically when GNP first added to existing prefab

                    const string PREFAB_EXTENSION = ".prefab";
                    string resourceLocation = importedAsset.Substring(importedAsset.IndexOf(GONetSpawnSupport_Runtime.RESOURCES) + GONetSpawnSupport_Runtime.RESOURCES.Length).Replace(PREFAB_EXTENSION, string.Empty);
                    UnityEngine.Object resource = Resources.Load(resourceLocation);
                    if (resource is GameObject)
                    {
                        GameObject gameObject = resource as GameObject;
                        GONetParticipant gonetParticipant = gameObject.GetComponent<GONetParticipant>();
                        if (gonetParticipant != null)
                        {
                            GONetSpawnSupport_DesignTime.OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly_Single(gonetParticipant);
                            existingGONetParticipantAssetPaths.Add(importedAsset);

                            List<GONetParticipant> gnpsAddedToPrefabsThisFrame;
                            if (!OnPostprocessAllAssets_By_frameCountMap.TryGetValue(Time.frameCount, out gnpsAddedToPrefabsThisFrame))
                            {
                                OnPostprocessAllAssets_By_frameCountMap[Time.frameCount] = gnpsAddedToPrefabsThisFrame = new List<GONetParticipant>();
                            }
                            gnpsAddedToPrefabsThisFrame.Add(gonetParticipant);
                        }
                    }
                }
            }

            foreach (string deletedAsset in deletedAssets)
            {
                //Debug.Log("Deleted Asset: " + deletedAsset);

                existingGONetParticipantAssetPaths.Remove(deletedAsset); // no matter what, if this was deleted....get this out of here
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                //Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);

                existingGONetParticipantAssetPaths.Remove(movedFromAssetPaths[i]); // no matter what, if this was moved....get this old one out of here

                string movedAsset = movedAssets[i];
                string possibleDesignTimeAssetLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, movedAsset);
                if (designTimeLocations_gonetParticipants.Contains(possibleDesignTimeAssetLocation))
                {
                    existingGONetParticipantAssetPaths.Add(movedAsset);
                }
            }
            
            { // after that, we need to see about do things to ensure ALL prefabs get generated when stuff (e.g., C# files) change that possibly have some changes to sync stuffs
                IEnumerable<string> gnpPrefabAssetPaths = designTimeLocations_gonetParticipants.Where(x => x.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX)).Select(x => x.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length));
                foreach (string gnpPrefabAssetPath in gnpPrefabAssetPaths)
                {
                    existingGONetParticipantAssetPaths.Add(gnpPrefabAssetPath);
                }
            }

            for (int i = existingGONetParticipantAssetPaths.Count - 1; i >= 0; --i)
            {
                string item = existingGONetParticipantAssetPaths.ElementAt(i);
                int lastFrameCountProcessed;
                if (gonetParticipantAssetsPaths_to_lastFrameCountProcessed.TryGetValue(item, out lastFrameCountProcessed) && lastFrameCountProcessed == Time.frameCount)
                {
                    existingGONetParticipantAssetPaths.Remove(item);
                }
            }

            if (existingGONetParticipantAssetPaths.Count > 0)
            {
                DoAllTheGenerationStuffs(existingGONetParticipantAssetPaths); // IMPORTANT: calling this will end up calling AssetDatabase.SaveAssets() and seemingly this exact method again with those assets....so we need to avoid infinite loop!
            }
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            bool canASSume_GoingFrom_Editer_To_Play = !EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode; // see http://wiki.unity3d.com/index.php/SaveOnPlay for the idea here!
            if (canASSume_GoingFrom_Editer_To_Play)
            {
                //GONetLog.Debug("[DREETS] entering play mode...not there yet, but about to be....about to generate as that needs to happen before playing!");
                DoAllTheGenerationStuffs();
            }
        }

        private static void EditorSceneManager_sceneSaved(Scene scene)
        {
            //GONetLog.Debug("[DREETS] saved scene: " + scene.name);
            DoAllTheGenerationStuffs();
        }

        private static void OnProjectChanged()
        {
            if (IsInstantiationOfPrefab(Selection.activeGameObject))
            {
                GONetParticipant gonetParticipant_onPrefab = Selection.activeGameObject.GetComponent<GONetParticipant>();
                if (gonetParticipant_onPrefab != null)
                {
                    OnGONetParticipantPrefabCreated_Editor(gonetParticipant_onPrefab);
                }
            }

            // TODO need to figure out what to check in order to call OnGONetParticipantPrefabDeleted_Editor
        }

        static readonly List<GONetParticipant> gonetParticipants_prefabsCreatedSinceLastGeneratorRun = new List<GONetParticipant>();

        private static void OnGONetParticipantPrefabCreated_Editor(GONetParticipant gonetParticipant_onPrefab)
        {
            GONetLog.Debug("[DREETS] *********GONetParticipant PREFAB*********** created.");
            gonetParticipants_prefabsCreatedSinceLastGeneratorRun.Add(gonetParticipant_onPrefab);
        }

        private static void OnGONetParticipantPrefabDeleted_Editor(GONetParticipant gonetParticipant_onPrefab)
        {
            //GONetLog.Debug("[DREETS] *********GONetParticipant PREFAB*********** deleted.");
        }

        private static void GONetParticipant_EditorOnlyDefaultContructor(GONetParticipant gonetParticipant)
        {

        }

        internal static bool IsInstantiationOfPrefab(UnityEngine.Object @object)
        {
            bool isPrefab = @object != null &&
#if UNITY_2018_3_OR_NEWER
                PrefabUtility.GetCorrespondingObjectFromSource(@object) != null;
#else
                PrefabUtility.GetPrefabParent(@object.gameObject) == null && PrefabUtility.GetPrefabObject(@object.gameObject) != null;
#endif
            return isPrefab;
        }

        private static void GONetParticipant_EditorOnlyAwake(GONetParticipant gonetParticipant)
        {
            if (IsInstantiationOfPrefab(gonetParticipant))
            {
                //GONetLog.Debug("[DREETS] *********PREFAB*********** GONetParticipant straight been woke!");
            }
            else
            {
                //GONetLog.Debug("[DREETS] woke!");
            }
        }

        private static void GONetParticipant_EditorOnlyReset(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug("[DREETS] added GONetParticipant");
        }

        private static void GONetParticipant_EditorOnlyOnDestroy(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug("[DREETS] ***DESTROYED*** GONetParticipant....");
        }

        [DidReloadScripts]
        public static void OnScriptsReloaded()
        {

        }

        internal static void Reset_OnAddedToGameObjectForFirstTime_EditorOnly(GONetParticipant gonetParticipant)
        {

        }

        /// <summary>
        /// This little beast face is here to prevent the continual processing of assets in an infinite loop!
        /// </summary>
        static readonly FileBackedMap<string, int> gonetParticipantAssetsPaths_to_lastFrameCountProcessed = new FileBackedMap<string, int>(GENERATION_FILE_PATH + "gonetParticipantAssetsPaths_to_lastFrameCountProcessed.bin");

        static void OnEditorApplicationQuitting()
        {
            gonetParticipantAssetsPaths_to_lastFrameCountProcessed.Clear(); // don't want the frameCount from this session carrying over to next session
        }

        static int howDeepIsYourSaveStack = 0;

        /// <summary>
        /// PRE: <paramref name="ensureToIncludeTheseGONetParticipantAssets_paths"/> is either null OR populated with asset path KNOWN to be <see cref="GONetParticipant"/> prefabs in the project!
        /// </summary>
        /// <param name="ensureToIncludeTheseGONetParticipantAssets_paths"></param>
        internal static void DoAllTheGenerationStuffs(IEnumerable<string> ensureToIncludeTheseGONetParticipantAssets_paths = null)
        {
            if (ensureToIncludeTheseGONetParticipantAssets_paths != null)
            {
                foreach (var path in ensureToIncludeTheseGONetParticipantAssets_paths)
                {
                    gonetParticipantAssetsPaths_to_lastFrameCountProcessed[path] = Time.frameCount;
                }
            }

            try
            {
                howDeepIsYourSaveStack++;

                /* auto-sync code gen psuedo:

                // NOTE: This method is thought to be called when editor save scene called
                // 		 TODO: need to account for not saving scene and running from editor......that run needs the latest code gen and id mappings etc...TODO: look for callback for when going into play mode in editor

                -in static initializer that runs before any GONet stuff, attempt load hold all unique encapsulated data (snaps) from persistence
                -if snaps not found, create empty list/set to hold all unique encapsulated data
                -find all GONetParticipants (gos) // MUST include in all scene and all prefabs ==AND== MUST be deterministically ordered!
                -for each GONetParticipant in gos (go)
                    -find all [AutoMagicalSync]s on go (amss) // MUST be deterministically ordered!
                    -ask utility for unique snap // pass all info available, for current go and amss combo
                        -utility => if snap not found in snaps (i.e., no match any other snap layout/content 1-to-1), add snap to snaps and return snap
                    -annotate go with snap.id (likely matches the index inside snaps), replacing existing value if present (in private [SerializeField] i suppose)
                        -NOTE: see GONetParticipant.codeGenerationId
                -for each snap in snaps
                    -generate a C# class for auto-magical sync support (class name suffix is "_<snap.id>")
                -compile and save etc...
                -persist latest unique list/set of snaps (overwriting whatever was there previously)
                */

                List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence = LoadAllSnapsFromPersistence();
                //allUniqueSnapsForPersistence.ForEach(x => GONetLog.Debug("from persistence x.codeGenerationId: " + x.codeGenerationId + " x.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

                List<GONetParticipant> gonetParticipantsInOpenScenes = new List<GONetParticipant>();

                for (int iOpenScene = 0; iOpenScene < SceneManager.sceneCount; ++iOpenScene)
                {
                    Scene scene = SceneManager.GetSceneAt(iOpenScene);
                    GameObject[] rootGOs = scene.GetRootGameObjects();
                    for (int iRootGO = 0; iRootGO < rootGOs.Length; ++iRootGO)
                    {
                        GONetParticipant[] gonetParticipantsInOpenScene = rootGOs[iRootGO].GetComponentsInChildren<GONetParticipant>();
                        gonetParticipantsInOpenScenes.AddRange(gonetParticipantsInOpenScene);
                    }

                }

                List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleNewUniqueSnaps = new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
                gonetParticipantsInOpenScenes.ForEach(gonetParticipant => possibleNewUniqueSnaps.Add(new GONetParticipant_ComponentsWithAutoSyncMembers(gonetParticipant)));
                // TODO add in stuff to possibleNewUniqueSnaps from gonetParticipants_prefabsCreatedSinceLastGeneratorRun before clearing it out below

                if (ensureToIncludeTheseGONetParticipantAssets_paths != null)
                {
                    foreach (string gonetParticipantAssetPath in ensureToIncludeTheseGONetParticipantAssets_paths)
                    {
                        GONetParticipant gonetParticipantPrefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(gonetParticipantAssetPath);
                        if (gonetParticipantPrefab != null) // it would be null during some GONet generation flows herein when moving locations...but eventually it will be A-OK...avoid/skip for now
                        {
                            possibleNewUniqueSnaps.Add(new GONetParticipant_ComponentsWithAutoSyncMembers(gonetParticipantPrefab));
                        }
                    }
                }

                byte max_codeGenerationId = allUniqueSnapsForPersistence.Count > 0 ? allUniqueSnapsForPersistence.Max(x => x.codeGenerationId) : GONetParticipant.CodeGenerationId_Unset;
                //GONetLog.Debug("max_codeGenerationId: " + max_codeGenerationId);
                //GONetLog.Debug("gonetParticipantsInOpenScenes.count: " + gonetParticipantsInOpenScenes.Count);
                //GONetLog.Debug("before allUniqueSnapsForPersistence.count: " + allUniqueSnapsForPersistence.Count);
                possibleNewUniqueSnaps.ForEach(possibleNew =>
                {
                    if (!allUniqueSnapsForPersistence.Contains(possibleNew, SnapComparer.Instance))
                    {
                        possibleNew.codeGenerationId = ++max_codeGenerationId; // TODO need to account for any gaps in this list if some are removed at any point
                        //GONetLog.Debug("just assigned codeGenerationId: " + possibleNew.codeGenerationId);
                        allUniqueSnapsForPersistence.Add(possibleNew);
                    }
                });
                //GONetLog.Debug("after allUniqueSnapsForPersistence.count: " + allUniqueSnapsForPersistence.Count);

                bool shouldSaveScene_weChangedCodeGenerationIds = false;
                { // make sure all GONetParticipants have assigned codeGenerationId, which is vitally important for game play runtime
                    possibleNewUniqueSnaps.ForEach(possibleNew =>
                    {
                        var matchFromPersistence = allUniqueSnapsForPersistence.First(unique => SnapComparer.Instance.Equals(possibleNew, unique));
                        if (possibleNew.codeGenerationId != matchFromPersistence.codeGenerationId ||
                            possibleNew.gonetParticipant.codeGenerationId != matchFromPersistence.codeGenerationId)
                        {
                            //GONetLog.Debug("match found from persistence... BEFORE possibleNew.codeGenerationId: " + possibleNew.codeGenerationId);

                            possibleNew.codeGenerationId = matchFromPersistence.codeGenerationId;
                            { // cannot simply do the following: possibleNew.gonetParticipant.codeGenerationId = matchFromPersistence.codeGenerationId;
                                SerializedObject so = new SerializedObject(possibleNew.gonetParticipant);
                                so.Update();
                                so.FindProperty(nameof(GONetParticipant.codeGenerationId)).intValue = matchFromPersistence.codeGenerationId;
                                so.ApplyModifiedProperties();
                                //EditorSceneManager.MarkAllScenesDirty(); this causes endless loop
                            }

                            //GONetLog.Debug("match found from persistence... AFTER possibleNew.codeGenerationId: " + possibleNew.codeGenerationId);

                            shouldSaveScene_weChangedCodeGenerationIds = true;
                        }
                        else
                        {
                            //GONetLog.Debug("already matched??? rere");
                        }
                    });
                }

                if (shouldSaveScene_weChangedCodeGenerationIds
                    && howDeepIsYourSaveStack <= 1) // gotta be careful we do not get into an endless cycle as the method we are in now is called when scene saved.
                {
                    if (ensureToIncludeTheseGONetParticipantAssets_paths != null)
                    {
                        foreach (string gonetParticipantAssetPath in ensureToIncludeTheseGONetParticipantAssets_paths)
                        {
                            GONetParticipant gonetParticipantPrefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(gonetParticipantAssetPath);
                            if (gonetParticipantPrefab != null) // it would be null during some GONet generation flows herein when moving locations...but eventually it will be A-OK...avoid/skip for now
                            {
                                PrefabUtility.SavePrefabAsset(gonetParticipantPrefab.gameObject);// this will save any changes like codeGenerationId change from above logic
                            }
                        }
                    }

                    //GONetLog.Debug("magoo...save time....loop endlessly!");
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    // TODO call this for stuff in gonetParticipants_prefabsCreatedSinceLastGeneratorRun: PrefabUtility.SavePrefabAsset()
                    
                }

                { // Do generation stuffs? .... based on what has changed in scene/project (i.e., any GNP/AutoSync changes) since last save
                    int count = allUniqueSnapsForPersistence.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        GONetParticipant_ComponentsWithAutoSyncMembers one = allUniqueSnapsForPersistence[i];
                        one.ApplyProfileToAttributes_IfAppropriate(); // this needs to be done for everyone prior to generation!

                        try
                        {
                            GenerateClass(one);
                        }
                        catch (NullReferenceException)
                        {
                            // This is expected to happen when removing [GONetAutoMagicalSync] from members  and no more exist on any members in that MB/class and then running generation is trying to access information to generate class for that old data 
                            // IMPORTANT: we do not delete/re-use codeGenerationIds, which leads to this......and old generated classes will sit around in projects as a result if the devs remove stuff alot!!!
                        }
                    }

                    byte ASSumedMaxCodeGenerationId = (byte)count;
                    BobWad_Generated_Generator.GenerateClass(ASSumedMaxCodeGenerationId, allUniqueSnapsForPersistence);

                    AssetDatabase.SaveAssets(); // since we are generating the class that is the real thing of value here, ensure we also save the asset to match current state
                    AssetDatabase.Refresh(); // get the Unity editor to recognize any new code just added and recompile it
                }

                { // clean up
                    gonetParticipants_prefabsCreatedSinceLastGeneratorRun.Clear();

                    DeleteAllSnapsFromPersistence();
                    SaveAllSnapsToPersistence(allUniqueSnapsForPersistence);
                }
            }
            finally
            {
                howDeepIsYourSaveStack--;
            }
        }

        #region persistence

        internal const string SNAPS_FILE = GENERATION_FILE_PATH + "Unique_GONetParticipant_ComponentsWithAutoSyncMembers.bin";

        /// <summary>
        /// returns empty list if nothing persisted.
        /// </summary>
        private static List<GONetParticipant_ComponentsWithAutoSyncMembers> LoadAllSnapsFromPersistence()
        {
            if (File.Exists(SNAPS_FILE))
            {
                byte[] snapsFileBytes = File.ReadAllBytes(SNAPS_FILE);
                var l = SerializationUtils.DeserializeFromBytes<List<GONetParticipant_ComponentsWithAutoSyncMembers>>(snapsFileBytes);
                foreach (var ll in l)
                {
                    foreach (var lll in ll.ComponentMemberNames_By_ComponentTypeFullName)
                    {
                        foreach (var llll in lll.autoSyncMembers)
                        {
                            llll.PostDeserialize_InitAttribute(lll.componentTypeAssemblyQualifiedName);
                        }
                    }
                }
                return l;
            }
            else
            {
                return new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
            }
        }

        private static void SaveAllSnapsToPersistence(List<GONetParticipant_ComponentsWithAutoSyncMembers> allSnaps)
        {
            if (!Directory.Exists(GENERATION_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATION_FILE_PATH);
            }

            allSnaps.ForEach(x => GONetLog.Debug("SAVING to persistence x.codeGenerationId: " + x.codeGenerationId + " x.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

            int returnBytesUsedCount;
            byte[] snapsFileBytes = SerializationUtils.SerializeToBytes(allSnaps, out returnBytesUsedCount);
            FileUtils.WriteBytesToFile(SNAPS_FILE, snapsFileBytes, returnBytesUsedCount, FileMode.Truncate);
            SerializationUtils.ReturnByteArray(snapsFileBytes);
        }

        private static void DeleteAllSnapsFromPersistence()
        {
            // TODO FIXME delete all (rows?) from Snap persistence
        }

        #endregion

        private static void GenerateClass(GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry)
        {
            var t4Template = new GONetParticipant_AutoMagicalSyncCompanion_GeneratedTemplate(uniqueEntry);
            string generatedClassText = t4Template.TransformText();

            if (!Directory.Exists(GENERATED_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATED_FILE_PATH);
            }
            string writeToPath = string.Concat(GENERATED_FILE_PATH, t4Template.ClassName, FILE_SUFFIX);
            File.WriteAllText(writeToPath, generatedClassText);
        }
    }

    /// <summary>
    /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember
    {
        /// <summary>
        /// If false, the member is a property.
        /// </summary>
        [Key(0)]
        public bool isField;
        [Key(1)]
        public string memberTypeFullName;
        [Key(2)]
        public string memberName;

        /// <summary>
        /// A value of 0 indicates this single member does NOT represent an animator controller parameter id
        /// </summary>
        [Key(3)]
        public int animatorControllerParameterId = 0;
        [Key(4)]
        public string animatorControllerParameterMethodSuffix;
        [Key(5)]
        public string animatorControllerParameterTypeFullName;
        [Key(6)]
        public string animatorControllerName;
        [Key(7)]
        public string animatorControllerParameterName;

        [IgnoreMember]
        public GONetAutoMagicalSyncAttribute attribute;
        private int nameHash;
        private string methodSuffix;
        private string typeFullName;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember() { }

        internal GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(MemberInfo syncMember)
            : this(syncMember, (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true))
        {
        }

        internal GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(MemberInfo syncMember, GONetAutoMagicalSyncAttribute attribute)
        {
            isField = syncMember.MemberType == MemberTypes.Field;
            memberTypeFullName = syncMember.MemberType == MemberTypes.Property
                                    ? ((PropertyInfo)syncMember).PropertyType.FullName
                                    : ((FieldInfo)syncMember).FieldType.FullName;
            memberName = syncMember.Name;

            this.attribute = attribute;
        }

        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(MemberInfo syncMember, GONetAutoMagicalSyncAttribute attribute, int animatorControllerParameterId, string animatorControllerParameterMethodSuffix, string animatorControllerParameterTypeFullName, string animatorControllerName, string animatorControllerParameterName) : this(syncMember, attribute)
        {
            this.animatorControllerParameterId = animatorControllerParameterId;
            this.animatorControllerParameterMethodSuffix = animatorControllerParameterMethodSuffix;
            this.animatorControllerParameterTypeFullName = animatorControllerParameterTypeFullName;
            this.animatorControllerName = animatorControllerName;
            this.animatorControllerParameterName = animatorControllerParameterName;
        }

        internal void PostDeserialize_InitAttribute(string memberOwner_componentTypeAssemblyQualifiedName)
        {
            Type memberOwnerType = Type.GetType(memberOwner_componentTypeAssemblyQualifiedName);
            MemberInfo syncMember = memberOwnerType.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance)[0];
            Type syncMemberType = syncMember.MemberType == MemberTypes.Property
                                ? ((PropertyInfo)syncMember).PropertyType
                                : ((FieldInfo)syncMember).FieldType;

            { // get the attribute, either off the field itself or our intrinsic stuff we do
                attribute = (GONetAutoMagicalSyncAttribute)syncMember.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true);

                bool isSpecialCaseThatRequiresManualAttributeConstruction = attribute == null;
                if (isSpecialCaseThatRequiresManualAttributeConstruction)
                {
                    if (!GONetParticipant_ComponentsWithAutoSyncMembers.intrinsicAttributeByMemberTypeMap.TryGetValue(ValueTuple.Create(memberOwnerType, syncMemberType), out attribute))
                    {
                        const string TURD = "This is some bogus turdmeal.  Should be able to either deserialize the GONetAutoMagicalSyncAttribute or lookup one from intrinsic type, but nope!  memberOwnerType.FullName: ";
                        const string MTFN = " memberType.FullName: ";
                        const string EXPECT = "\nThis is expected to happen when removing [GONetAutoMagicalSync] from members  and no more exist on any members in that MB/class and then running generation is trying to access information to generate class for that old data ";
                        string message = string.Concat(TURD, memberOwnerType.FullName, MTFN, syncMemberType.FullName, EXPECT);
                        Debug.LogWarning(message);
                        GONetLog.Warning(message);
                    }
                }
            }
        }

        /// <summary>
        /// Update the attribute based on the settings in the referenced profile/template (if applicable).
        /// IMPORTANT: This is required to be called prior to code generation (i.e., <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GenerateClass(GONetParticipant_ComponentsWithAutoSyncMembers)"/>)!
        /// </summary>
        /// <param name="syncMemberType"></param>
        internal void ApplyProfileToAttribute_IfAppropriate(Type syncMemberType, string memberName)
        {
            if (attribute == null)
            {
                const string NOATTR = "No attribute to which to apply profile.  This is only expected when you just removed the [GONetAutoMagicalSync] off the member of name: ";
                string message = string.Concat(NOATTR, memberName);
                GONetLog.Warning(message);
                Debug.LogWarning(message);
                return;
            }

            if (!string.IsNullOrWhiteSpace(attribute.SettingsProfileTemplateName))
            {
                GONetAutoMagicalSyncSettings_ProfileTemplate profile = Resources.Load<GONetAutoMagicalSyncSettings_ProfileTemplate>(string.Format(SYNC_PROFILE_RESOURCES_FOLDER_FORMATSKI, attribute.SettingsProfileTemplateName).Replace(".asset", string.Empty));

                if (profile == null)
                {
                    const string UNABLE = "Unable to locate requested profile/template asset with name: ";
                    const string DEFAULT = ".  We will use the default profile/template instead (creating it if we have to).";
                    GONetLog.Warning(string.Concat(UNABLE, attribute.SettingsProfileTemplateName, DEFAULT));

                    var defaultProfile = Resources.Load<GONetAutoMagicalSyncSettings_ProfileTemplate>(string.Format(SYNC_PROFILE_RESOURCES_FOLDER_FORMATSKI, GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT).Replace(".asset", string.Empty));

                    if (defaultProfile == null)
                    {
                        defaultProfile = GONetEditorWindow.CreateSyncSettingsProfileAsset<GONetAutoMagicalSyncSettings_ProfileTemplate>(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___DEFAULT);

                        defaultProfile.MustRunOnUnityMainThread = false;
                        defaultProfile.ProcessingPriority = 0;
                        defaultProfile.QuantizeDownToBitCount = 0;
                        defaultProfile.SendViaReliability = AutoMagicalSyncReliability.Unreliable;
                        defaultProfile.ShouldBlendBetweenValuesReceived = true;
                        defaultProfile.SyncChangesASAP = false;
                        defaultProfile.SyncChangesFrequencyOccurrences = 24;
                        defaultProfile.SyncChangesFrequencyUnitOfTime = SyncChangesTimeUOM.TimesPerSecond;
                        defaultProfile.SyncValueTypeSerializerOverrides = new SyncType_CustomSerializer_Pair[] {
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Vector3, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(Vector3Serializer)) },
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Quaternion, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(QuaternionSerializer)) },
                            };
                    }

                    profile = defaultProfile;
                }

                attribute.MustRunOnUnityMainThread = profile.MustRunOnUnityMainThread;
                attribute.ProcessingPriority = profile.ProcessingPriority;
                attribute.QuantizeDownToBitCount = profile.QuantizeDownToBitCount;
                attribute.QuantizeLowerBound = profile.QuantizeLowerBound;
                attribute.QuantizeUpperBound = profile.QuantizeUpperBound;
                attribute.Reliability = profile.SendViaReliability;
                attribute.ShouldBlendBetweenValuesReceived = profile.ShouldBlendBetweenValuesReceived;
                attribute.ShouldSkipSync_RegistrationId = (int)profile.ShouldSkipSyncRegistrationId;

                float syncEverySeconds = 0;
                if (profile.SyncChangesFrequencyOccurrences > 0)
                {
                    switch (profile.SyncChangesFrequencyUnitOfTime)
                    {
                        case SyncChangesTimeUOM.TimesPerSecond:
                            syncEverySeconds = 1f / profile.SyncChangesFrequencyOccurrences;
                            break;

                        case SyncChangesTimeUOM.TimesPerMinute:
                            syncEverySeconds = (1f / profile.SyncChangesFrequencyOccurrences) * 60f;
                            break;

                        case SyncChangesTimeUOM.TimesPerHour:
                            syncEverySeconds = (1f / profile.SyncChangesFrequencyOccurrences) * 60f * 60f;
                            break;

                        case SyncChangesTimeUOM.TimesPerDay:
                            syncEverySeconds = (1f / profile.SyncChangesFrequencyOccurrences) * 60f * 60f * 24f;
                            break;
                    }
                }
                attribute.SyncChangesEverySeconds = profile.SyncChangesASAP ? AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS : syncEverySeconds;

                if (profile.SyncValueTypeSerializerOverrides != null && profile.SyncValueTypeSerializerOverrides.Length > 0)
                {
                    GONetSyncableValueTypes gonetSyncType;
                    if (gonetSyncTypeByRealTypeMap.TryGetValue(syncMemberType, out gonetSyncType))
                    {
                        if (profile.SyncValueTypeSerializerOverrides.Any(x => x.ValueType == gonetSyncType))
                        {
                            SyncType_CustomSerializer_Pair first = profile.SyncValueTypeSerializerOverrides.First(x => x.ValueType == gonetSyncType);
                            attribute.CustomSerialize_Type = first.CustomSerializerType.Type;

                            if (attribute.CustomSerialize_Type != null)
                            {
                                GONetLog.Debug("GONet will use the custom serializer type: " + attribute.CustomSerialize_Type.FullName);
                            }
                        }
                    }
                    else
                    {
                        GONetLog.Warning("Could not match up the actual C# Type of the data (" + syncMemberType.FullName + ") with a valid/supported value on " + typeof(GONetSyncableValueTypes).FullName);
                    }
                }
            }
        }

        static readonly Dictionary<Type, GONetSyncableValueTypes> gonetSyncTypeByRealTypeMap = new Dictionary<Type, GONetSyncableValueTypes>()
        {
            { typeof(byte), GONetSyncableValueTypes.System_Byte },
            { typeof(float), GONetSyncableValueTypes.System_Single },
            { typeof(int), GONetSyncableValueTypes.System_Int32 },
            { typeof(Quaternion), GONetSyncableValueTypes.UnityEngine_Quaternion },
            // ?? TODO reference type support: { typeof(string), GONetSyncableValueTypes.System_String },
            { typeof(ulong), GONetSyncableValueTypes.System_UInt64 },
            { typeof(Vector3), GONetSyncableValueTypes.UnityEngine_Vector3 },
            { typeof(bool), GONetSyncableValueTypes.System_Boolean },
            { typeof(short), GONetSyncableValueTypes.System_Int16 },
            { typeof(ushort), GONetSyncableValueTypes.System_UInt16 },
            { typeof(uint), GONetSyncableValueTypes.System_UInt32 },
            { typeof(long), GONetSyncableValueTypes.System_Int64 },
            { typeof(double), GONetSyncableValueTypes.System_Double },
            { typeof(sbyte), GONetSyncableValueTypes.System_SByte },
            { typeof(Vector2), GONetSyncableValueTypes.UnityEngine_Vector2 },
            { typeof(Vector4), GONetSyncableValueTypes.UnityEngine_Vector4 },
        };

        public const string SYNC_PROFILE_RESOURCES_FOLDER_FORMATSKI = "GONet/SyncSettingsProfiles/{0}.asset";
    }

    /// <summary>
    /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers_Single
    {
        [Key(0)]
        public string componentTypeName;
        [Key(1)]
        public string componentTypeFullName;
        [Key(2)]
        public string componentTypeAssemblyQualifiedName;

        /// <summary>
        /// in deterministic order!
        /// </summary>
        [Key(3)]
        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] autoSyncMembers;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single() { }

        /// <param name="component_autoSyncMembers">in deterministic order!</param>
        internal GONetParticipant_ComponentsWithAutoSyncMembers_Single(Component component, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] component_autoSyncMembers)
        {
            Type componentType = component.GetType();
            componentTypeName = componentType.Name;
            componentTypeFullName = componentType.FullName;
            componentTypeAssemblyQualifiedName = componentType.AssemblyQualifiedName;

            autoSyncMembers = component_autoSyncMembers;
        }
    }

    /// <summary>
    /// This is what represents a unique combination of <see cref="GONetAutoMagicalSyncAttribute"/>s on a <see cref="GameObject"/> with <see cref="GONetParticipant"/> "installed."
    /// A set of these gets persisted into a file at <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.SNAPS_FILE"/>.
    /// 
    /// NOTE: once called this Snap...renamed to this.
    /// 
    /// </summary>
    [MessagePackObject]
    public class GONetParticipant_ComponentsWithAutoSyncMembers
    {
        /// <summary>
        /// Assigned once know to be a unique combo.
        /// Relates directly to <see cref="GONetParticipant.codeGenerationId"/>.
        /// IMPORTANT: MessagePack for C# is not supporting internal field.....so, this is now public...boo!
        /// </summary>
        [Key(0)]
        public byte codeGenerationId;

        /// <summary>
        /// In deterministic order....with the one for <see cref="GONetParticipant.GONetId"/> first due to special case processing....also <see cref="GONetParticipant.ASSumed_GONetId_INDEX"/>
        /// </summary>
        [Key(1)]
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single[] ComponentMemberNames_By_ComponentTypeFullName;

        /// <summary>
        /// This is ONLY populated when <see cref="GONetParticipant_ComponentsWithAutoSyncMembers.GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant)"/> is used during 
        /// the time leading up to doing a generation call (i.e., <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DoAllTheGenerationStuffs"/>).
        /// </summary>
        internal GONetParticipant gonetParticipant;

        static readonly GONetAutoMagicalSyncAttribute attribute_transform_rotation = new GONetAutoMagicalSyncAttribute(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_ROTATION);
        static readonly GONetAutoMagicalSyncAttribute attribute_transform_position = new GONetAutoMagicalSyncAttribute(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___TRANSFORM_POSITION);
        static readonly GONetAutoMagicalSyncAttribute attribute_animator_parameters = new GONetAutoMagicalSyncAttribute(GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___ANIMATOR_CONTROLLER_PARAMETERS);

        internal static readonly Dictionary<ValueTuple<Type, Type>, GONetAutoMagicalSyncAttribute> intrinsicAttributeByMemberTypeMap = new Dictionary<ValueTuple<Type, Type>, GONetAutoMagicalSyncAttribute>(2)
        {
            { ValueTuple.Create(typeof(Transform), typeof(Vector3)), attribute_transform_position },
            { ValueTuple.Create(typeof(Transform), typeof(Quaternion)), attribute_transform_rotation },
            { ValueTuple.Create(typeof(Animator), typeof(AnimatorControllerParameter[])), attribute_animator_parameters },
        };

        /// <summary>
        /// IMPORTANT: Do NOT use this.  Being public is required to work with deserialize/load from persistence:
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers() { }

        internal GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;

            int iSinglesAdded = -1;
            int gonetIdOriginalIndex_single = -1;
            int gonetIdOriginalIndex_singleMember = -1;

            var componentMemberNames_By_ComponentTypeFullName = new LinkedList<GONetParticipant_ComponentsWithAutoSyncMembers_Single>();
            foreach (MonoBehaviour component in gonetParticipant.GetComponents<MonoBehaviour>().OrderBy(c => c.GetType().FullName))
            {
                var componentAutoSyncMembers = new LinkedList<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember>();

                IEnumerable<MemberInfo> syncMembers = component
                    .GetType()
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(member => (member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                                    && member.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true) != null)
                    .OrderBy(member => member.Name);

                int syncMemberCount = syncMembers.Count();
                bool willBeAddingBelow = syncMemberCount > 0;
                if (willBeAddingBelow)
                {
                    ++iSinglesAdded;
                    bool isGONetParticipant = component.GetType() == typeof(GONetParticipant);
                    if (isGONetParticipant)
                    {
                        gonetIdOriginalIndex_single = iSinglesAdded;
                    }

                    for (int iSyncMember = 0; iSyncMember < syncMemberCount; ++iSyncMember)
                    {
                        MemberInfo syncMember = syncMembers.ElementAt(iSyncMember);
                        var singleMember = new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(syncMember);
                        componentAutoSyncMembers.AddLast(singleMember);

                        if (isGONetParticipant && syncMember.Name == nameof(GONetParticipant.GONetId))
                        {
                            gonetIdOriginalIndex_singleMember = iSyncMember;
                        }
                    }

                    var newSingle = new GONetParticipant_ComponentsWithAutoSyncMembers_Single(component, componentAutoSyncMembers.ToArray());
                    componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle);
                }
            }

            Type gonetParticipant_transformType = gonetParticipant.transform.GetType();
            if (gonetParticipant_transformType == typeof(Transform) && gonetParticipant_transformType != typeof(RectTransform)) // since GameObject instances can have either Transform or RectTransform, we have to check.....as GONet is currently only working with regular Transform
            { // intrinsic Transform properties that cannot manually have the [GONetAutoMagicalSync] added.... (e.g., transform rotation and position)
                var component_autoSyncMembers_transform = new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[2];

                MemberInfo transform_rotation = typeof(Transform).GetMember(nameof(Transform.rotation), BindingFlags.Public | BindingFlags.Instance)[0];
                component_autoSyncMembers_transform[0] = new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(transform_rotation, attribute_transform_rotation);

                MemberInfo transform_position = typeof(Transform).GetMember(nameof(Transform.position), BindingFlags.Public | BindingFlags.Instance)[0];
                component_autoSyncMembers_transform[1] = new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(transform_position, attribute_transform_position);

                var newSingle_transform = new GONetParticipant_ComponentsWithAutoSyncMembers_Single(gonetParticipant.transform, component_autoSyncMembers_transform);
                componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle_transform);
            }

            Animator animator = gonetParticipant.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null) // IMPORTANT: in editor, looks like animator.parameterCount is [sometimes!...figured out when...it is only when the Animator window is open and its controller is selected...editor tries to do tricky stuff that whacks this all out for some reason] 0 even when shit is there....hence the usage of animator.runtimeAnimatorController.parameters instead of animator.parameters
            { // intrinsic Animator properties that cannot manually have the [GONetAutoMagicalSync] added.... (e.g., transform rotation and position)
                var parameters = (AnimatorControllerParameter[])animator.runtimeAnimatorController.GetType().GetProperty(nameof(Animator.parameters), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(animator.runtimeAnimatorController);
                if (parameters != null && parameters.Length > 0)
                {
                    var component_autoSyncMembers_animator_parameter = new List<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember>();

                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        MemberInfo animator_parameters = typeof(Animator).GetMember(nameof(Animator.parameters), BindingFlags.Public | BindingFlags.Instance)[0];
                        AnimatorControllerParameter animatorControllerParameter = parameters[i];

                        if (gonetParticipant.animatorSyncSupport[animatorControllerParameter.name].isSyncd) // NOTE: doing this check right here is why isSyncd cannot be changed at runtime like IsPositionSyncd and IsRotationSyncd
                        {
                            string methodSuffix;
                            string typeFullName;
                            switch (animatorControllerParameter.type)
                            {
                                case AnimatorControllerParameterType.Bool:
                                    methodSuffix = "Bool";
                                    typeFullName = typeof(bool).FullName;
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    methodSuffix = "Float";
                                    typeFullName = typeof(float).FullName;
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    methodSuffix = "Integer";
                                    typeFullName = typeof(int).FullName;
                                    break;

                                case AnimatorControllerParameterType.Trigger:
                                default:
                                    methodSuffix = "Trigger";
                                    typeFullName = typeof(bool).FullName;
                                    break;
                            }

                            component_autoSyncMembers_animator_parameter.Add(new GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember(animator_parameters, attribute_animator_parameters, animatorControllerParameter.nameHash, methodSuffix, typeFullName, animator.runtimeAnimatorController.name, animatorControllerParameter.name));
                        }
                    }

                    var newSingle_animator = new GONetParticipant_ComponentsWithAutoSyncMembers_Single(gonetParticipant.GetComponent<Animator>(), component_autoSyncMembers_animator_parameter.ToArray());
                    componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle_animator);
                }
            }

            ComponentMemberNames_By_ComponentTypeFullName = componentMemberNames_By_ComponentTypeFullName.ToArray();
            //GONetLog.Debug("new ComponentMemberNames_By_ComponentTypeFullName.length: " + ComponentMemberNames_By_ComponentTypeFullName.Length);

            { // now that we have arrays, we can swap some stuff to ensure GONetId field is index 0!
                if (gonetIdOriginalIndex_single > 0) // if it is already 0, nothing to do
                {
                    var tmp = ComponentMemberNames_By_ComponentTypeFullName[0];
                    ComponentMemberNames_By_ComponentTypeFullName[0] = ComponentMemberNames_By_ComponentTypeFullName[gonetIdOriginalIndex_single];
                    ComponentMemberNames_By_ComponentTypeFullName[gonetIdOriginalIndex_single] = tmp;
                }

                // at this point, we know ComponentMemberNames_By_ComponentTypeFullName[0] represents the GONetParticipant component
                if (gonetIdOriginalIndex_singleMember > 0) // if it is already 0, nothing to do...yet again
                {
                    var gonetParticipantCompnent = ComponentMemberNames_By_ComponentTypeFullName[0];
                    var tmp = gonetParticipantCompnent.autoSyncMembers[0];
                    gonetParticipantCompnent.autoSyncMembers[0] = gonetParticipantCompnent.autoSyncMembers[gonetIdOriginalIndex_singleMember];
                    gonetParticipantCompnent.autoSyncMembers[gonetIdOriginalIndex_singleMember] = tmp;
                }
            }
        }

        internal void ApplyProfileToAttributes_IfAppropriate()
        {
            foreach (GONetParticipant_ComponentsWithAutoSyncMembers_Single single in ComponentMemberNames_By_ComponentTypeFullName)
            {
                Type memberOwnerType = Type.GetType(single.componentTypeAssemblyQualifiedName);
                foreach (var singleMember in single.autoSyncMembers)
                {
                    MemberInfo syncMember = memberOwnerType.GetMember(singleMember.memberName, BindingFlags.Public | BindingFlags.Instance)[0];
                    Type syncMemberType = syncMember.MemberType == MemberTypes.Property
                                        ? ((PropertyInfo)syncMember).PropertyType
                                        : ((FieldInfo)syncMember).FieldType;

                    singleMember.ApplyProfileToAttribute_IfAppropriate(syncMemberType, singleMember.memberName);
                }
            }
        }
    }

    /// <summary>
    /// NOTE: Does NOT consider <see cref="GONetParticipant_ComponentsWithAutoSyncMembers.codeGenerationId"/>.
    /// </summary>
    class SnapComparer : IEqualityComparer<GONetParticipant_ComponentsWithAutoSyncMembers>
    {
        internal static SnapComparer Instance => new SnapComparer();

        public bool Equals(GONetParticipant_ComponentsWithAutoSyncMembers x, GONetParticipant_ComponentsWithAutoSyncMembers y)
        {
            bool areInitiallyEqual =
                x != null && y != null &&
                x.ComponentMemberNames_By_ComponentTypeFullName != null && y.ComponentMemberNames_By_ComponentTypeFullName != null &&
                x.ComponentMemberNames_By_ComponentTypeFullName.Length == y.ComponentMemberNames_By_ComponentTypeFullName.Length;

            //GONetLog.Debug("areInitiallyEqual: " + areInitiallyEqual);

            if (areInitiallyEqual)
            {
                int componentCount = x.ComponentMemberNames_By_ComponentTypeFullName.Length;
                for (int iComponent = 0; iComponent < componentCount; ++iComponent)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_Single xSingle = x.ComponentMemberNames_By_ComponentTypeFullName[iComponent];
                    int xCount = xSingle.autoSyncMembers.Length;

                    GONetParticipant_ComponentsWithAutoSyncMembers_Single ySingle = y.ComponentMemberNames_By_ComponentTypeFullName[iComponent];
                    int yCount = ySingle.autoSyncMembers.Length;

                    if (xSingle.componentTypeAssemblyQualifiedName == ySingle.componentTypeAssemblyQualifiedName && xCount == yCount)
                    {
                        for (int iMember = 0; iMember < xCount; ++iMember)
                        {
                            string xMemberName = xSingle.autoSyncMembers[iMember].memberName;
                            string yMemberName = ySingle.autoSyncMembers[iMember].memberName;
                            if (xMemberName != yMemberName)
                            {
                                GONetLog.Debug("member names differ, iMember: " + iMember + " xMemberName: " + xMemberName + " yMemberName: " + yMemberName);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        GONetLog.Debug("qualified names not same or different count");
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public int GetHashCode(GONetParticipant_ComponentsWithAutoSyncMembers obj)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// TODO: Hmm...how is this different than <see cref="GONetMain.AutoMagicalSync_ValueMonitoringSupport"/>
    /// </summary>
    struct AutoMagicalSyncAttribute_GenerationSupport
    {
        Type monoBehaviourType;
        MemberInfo decoratedMember;
        GONetAutoMagicalSyncAttribute syncAttribute;
    }

    /// <summary>
    /// <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator"/> is static and in order to extend 
    /// <see cref="AssetPostprocessor"/>, you obviously cannot be static....that is why I exist and then delegate on to it.
    /// </summary>
    class Magoo : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }
    }
}
