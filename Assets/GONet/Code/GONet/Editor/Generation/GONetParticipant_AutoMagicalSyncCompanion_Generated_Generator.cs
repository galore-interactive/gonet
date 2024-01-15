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

using Assets.GONet.Code.GONet.Editor.Generation;
using GONet.Editor;
using GONet.PluginAPI;
using GONet.Utils;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Generation
{
    public class ProcessBuildHelper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static object _compilationContext;
        private static List<CompilerMessage> _compilationErrorMessages = new List<CompilerMessage>();
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            CompilationPipeline.compilationStarted += CompilationPipelineOnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipelineOnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += CompilationPipelineOnCompilationFinished;

            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.UpdateAllUniqueSnaps();
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GenerateFiles();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DeleteGeneratedFiles();
        }

        private static void CompilationPipelineOnCompilationStarted(object compilationContext)
        {
            _compilationContext = compilationContext;
            _compilationErrorMessages.Clear();
        }

        private static void CompilationPipelineOnAssemblyCompilationFinished(string path, CompilerMessage[] messages)
        {
            for (int i = messages?.Length ?? 0; --i >= 0;)
            {
                CompilerMessage compilerMessage = messages[i];
                if (compilerMessage.type == CompilerMessageType.Error)
                {
                    _compilationErrorMessages.Add(compilerMessage);
                }
            }
        }

        private void CompilationPipelineOnCompilationFinished(object compilationContext)
        {
            if (compilationContext != _compilationContext) return;

            _compilationContext = null; // reset for next time

            CompilationPipeline.compilationStarted -= CompilationPipelineOnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished -= CompilationPipelineOnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished -= CompilationPipelineOnCompilationFinished;

            if (_compilationErrorMessages.Count > 0)
            {
                Debug.LogError($"Compilation (with runtime only generated files included) finished with errors ({_compilationErrorMessages.Count}).  Going to delete runtime only generated files to avoid additional issues.");
                foreach (CompilerMessage message in _compilationErrorMessages)
                {
                    Debug.LogError($"\tERROR.  Message: {message.message}\n\t\tFile: {message.file}, Line: {message.line}, Column: {message.column}");
                }    

                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DeleteGeneratedFiles();
            }
        }
    }

    /// <summary>
    /// This class is responsible for executing the code generation pipeline. This pipeline contains three main tasks:
    /// 1. Delete generated scripts
    /// 2. Create generated scripts
    /// 3. Update all the generation information based on the new project state.
    /// Each task is documented within its corresponding method, check them out for further information.
    /// </summary>
    [InitializeOnLoad] // ensure class initializer is called whenever scripts recompile
    public static class GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator
    {
        //ASK SHAUN POSSIBLE REFACTOR: This list is not being used
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

        private const string GENERATION_FILE_PATH = "Assets/GONet/Code/GONet/Generation/";
        internal const string GENERATED_FILE_PATH = GENERATION_FILE_PATH + "Generated/";
        internal const string GENERATED_SUFFIX = "_Generated";
        private const string C_SHARP_FILE_SUFFIX = ".cs";
        internal const string BINARY_FILE_SUFFIX = "_MemoryPack.bin"; // NOTE: _MemoryPack suffix added to allow working well on older project upgraded from before GONet used MemoryPack (and used MessagePack instead...which formats do not match/jive)
        internal const string GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH = GENERATION_FILE_PATH + "All_Unique_Snaps" + BINARY_FILE_SUFFIX;
        internal const string GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH = GENERATION_FILE_PATH + "In_Scene_Unique_Snaps" + BINARY_FILE_SUFFIX;
        internal const string ASSET_FOLDER_SNAPS_FILE = GENERATION_FILE_PATH + "Assets_Folder_Unique_Snaps" + BINARY_FILE_SUFFIX;
        private const string TEST_FILENAME_TO_FORCE_GEN = "Test" + BINARY_FILE_SUFFIX;

        public static int LastPlayModeStateChange_frameCount { get; private set; } = -1;
        static PlayModeStateChange? lastPlayModeStateChange;
        public static PlayModeStateChange? LastPlayModeStateChange
        {
            get => lastPlayModeStateChange;
            private set
            {
                lastPlayModeStateChange = value;
                LastPlayModeStateChange_frameCount = Time.frameCount;
            }
        }

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

        internal static void DeleteGeneratedFiles()
        {
            /*This method is one of the key tasks (out of three) of the GONet's code generation pipeline. The goal of this code generation task is to delete all the generated scripts except of the
            * SyncEvent_GeneratedTypes.cs. It is executed in the following cases:
            * 1. Inmediately before Unity changes from PlayMode to EditorMode. UnityEditor.EditorApplication.playModeStateChanged.OnEditorPlayModeStateChanged(PlayModeStateChange state)
            * 2. Inmediately after Unity ends the build process. UnityEditor.Builds.IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
            *
            * This is how this method works:
            * 1. We get the GUIDs from all the files within the GENERATED_FILE_PATH that contains GENERATED_SUFFIX.
            * 2. If that file is not the SyncEvent_GeneratedTypes.cs we delete it.
            * 3. We refresh the assets data base since we have deleted scripts within the Assets folder.
            */

            string[] guids = AssetDatabase.FindAssets(GENERATED_SUFFIX, new string[] { GENERATED_FILE_PATH });
            for (int i = 0; i < guids.Length; ++i)
            {
                if (AssetDatabase.GUIDToAssetPath(guids[i]).Contains(nameof(SyncEvent_GeneratedTypes)))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
                UpdateAssetsSnaps(existingGONetParticipantAssetPaths); // IMPORTANT: calling this will end up calling AssetDatabase.SaveAssets() and seemingly this exact method again with those assets....so we need to avoid infinite loop!
            }
        }

        private static void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            LastPlayModeStateChange = state;

            //TODO add comment about why are we doing this.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                GenerateFiles();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                DeleteGeneratedFiles();
            }
        }

        private static void EditorSceneManager_sceneSaved(Scene scene)
        {
            //GONetLog.Debug("[DREETS] saved scene: " + scene.name);
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

        //ASK SHAUN POSSIBLE REFACTOR: This field is not being used. Just to add items and clear them but it is not being consulted anywhere. Delete?
        static readonly List<GONetParticipant> gonetParticipants_prefabsCreatedSinceLastGeneratorRun = new List<GONetParticipant>();

        //ASK SHAUN POSSIBLE REFACTOR: These 3 methods do nothing. Delete?
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
            //Debug.Log("Schmicks EDITOR beast!  go:" + gonetParticipant.gameObject);
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
            Animator animator = gonetParticipant.GetComponent<Animator>();
            if (AnimationEditorUtils.TryGetAnimatorControllerParameters(animator, out var parameters))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    if (gonetParticipant.animatorSyncSupport == null)
                    {
                        gonetParticipant.animatorSyncSupport = new GONetParticipant.AnimatorControllerParameterMap();
                    }

                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (!StringUtils.IsStringValidForCSharpNamingConventions(parameters[i].name))
                        {
                            GONetLog.Error($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                            Debug.LogError($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                            continue;
                        }

                        AnimatorControllerParameter animatorControllerParameter = parameters[i];
                        bool isAnimParamTypeSupportedInGONet = animatorControllerParameter.type != AnimatorControllerParameterType.Trigger;
                        string parameterSyncMap_key = animatorControllerParameter.name;
                        if (!gonetParticipant.animatorSyncSupport.ContainsKey(parameterSyncMap_key))
                        {
                            gonetParticipant.animatorSyncSupport[parameterSyncMap_key] = new GONetParticipant.AnimatorControllerParameter()
                            {
                                valueType = animatorControllerParameter.type,
                                isSyncd = false
                            };
                        }
                    }
                }
            }
            //GONetLog.Debug("[DREETS] added GONetParticipant");
        }

        private static void GONetParticipant_EditorOnlyOnDestroy(GONetParticipant gonetParticipant)
        {
            //GONetLog.Debug("[DREETS] ***DESTROYED*** GONetParticipant....");
        }

        //ASK SHAUN POSSIBLE REFACTOR: These methods are empty and never being called. Delete?
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
        static readonly FileBackedMap<string, int> gonetParticipantAssetsPaths_to_lastFrameCountProcessed = new FileBackedMap<string, int>(GENERATION_FILE_PATH + "gonetParticipantAssetsPaths_to_lastFrameCountProcessed" + BINARY_FILE_SUFFIX);

        static void OnEditorApplicationQuitting()
        {
            gonetParticipantAssetsPaths_to_lastFrameCountProcessed.Clear(); // don't want the frameCount from this session carrying over to next session
        }

        static int howDeepIsYourSaveStack = 0;

        /// <summary>
        /// Updates the assets folder snaps binary file.
        /// PRE: <paramref name="ensureToIncludeTheseGONetParticipantAssets_paths"/> is populated with asset path KNOWN to be <see cref="GONetParticipant"/> prefabs in the project!
        /// </summary>
        /// <param name="ensureToIncludeTheseGONetParticipantAssets_paths"></param>
        internal static void UpdateAssetsSnaps(IEnumerable<string> ensureToIncludeTheseGONetParticipantAssets_paths)
        {
            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
            const string USE_US_CULTURE_TO_ENSURE_PERIOD_INSTEAD_OF_COMMA_FOR_FLOATING_POINT_ToString = "en-US";
            Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(USE_US_CULTURE_TO_ENSURE_PERIOD_INSTEAD_OF_COMMA_FOR_FLOATING_POINT_ToString);

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

                    //Get the updated asset folder snaps
                    List<GONetParticipant_ComponentsWithAutoSyncMembers> newSnapsFromAssetFolders = new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
                    foreach (string gonetParticipantAssetPath in ensureToIncludeTheseGONetParticipantAssets_paths)
                    {
                        GONetParticipant gonetParticipantPrefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(gonetParticipantAssetPath);
                        if (gonetParticipantPrefab != null) // it would be null during some GONet generation flows herein when moving locations...but eventually it will be A-OK...avoid/skip for now
                        {
                            GONetParticipant_ComponentsWithAutoSyncMembers updatedSnapFromAssets = new GONetParticipant_ComponentsWithAutoSyncMembers(gonetParticipantPrefab);
                            updatedSnapFromAssets.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            newSnapsFromAssetFolders.Add(updatedSnapFromAssets);
                        }
                    }

                    SnapComparer snapComparer = SnapComparer.Instance;
                    //Create the allUniqueSnaps list in order to update codegen ids later
                    List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps = LoadAllSnapsFromPersistenceFile(GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
                    foreach (var snapFromAssetFolder in newSnapsFromAssetFolders)
                    {
                        bool found = false;
                        foreach (var uniqueSnap in allUniqueSnaps)
                        {
                            if(snapComparer.Equals(uniqueSnap, snapFromAssetFolder))
                            {
                                snapFromAssetFolder.codeGenerationId = uniqueSnap.codeGenerationId;
                                //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                                ApplyToGNPCodeGenerationId(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                                found = true;
                                break;
                            }
                        }

                        if(!found)
                        {
                            snapFromAssetFolder.codeGenerationId = GetNewCodeGenerationId(allUniqueSnaps);
                            //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                            ApplyToGNPCodeGenerationId(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                            allUniqueSnaps.Add(snapFromAssetFolder);
                        }
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    SaveUniqueSnapsToPersistenceFile(newSnapsFromAssetFolders, ASSET_FOLDER_SNAPS_FILE);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if (howDeepIsYourSaveStack <= 1) // gotta be careful we do not get into an endless cycle as the method we are in now is called when scene saved.
                    {
                        foreach (string gonetParticipantAssetPath in ensureToIncludeTheseGONetParticipantAssets_paths)
                        {
                            GONetParticipant gonetParticipantPrefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(gonetParticipantAssetPath);
                            if (gonetParticipantPrefab != null) // it would be null during some GONet generation flows herein when moving locations...but eventually it will be A-OK...avoid/skip for now
                            {
                                PrefabUtility.SavePrefabAsset(gonetParticipantPrefab.gameObject);// this will save any changes like codeGenerationId change from above logic
                            }
                        }

                        //GONetLog.Debug("magoo...save time....loop endlessly!");
                        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        // TODO call this for stuff in gonetParticipants_prefabsCreatedSinceLastGeneratorRun: PrefabUtility.SavePrefabAsset()
                    }
                }
                finally
                {
                    howDeepIsYourSaveStack--;
                }
            }

            Thread.CurrentThread.CurrentCulture = previousCulture;
        }

        /// <summary>
        /// Returns the slowest available code generation id of a List of snaps. It there is a gap, it will return the gap id.
        /// </summary>
        /// <param name="allUniqueSnaps"></param>
        /// <returns></returns>
        private static byte GetNewCodeGenerationId(List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps)
        {
            for (byte i = 1; i < 255; ++i)
            {
                if (!allUniqueSnaps.Exists(x => x.codeGenerationId == i))
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Generates the SyncEvent_GeneratedTypes enum based on a list of unique snaps.
        /// </summary>
        /// <param name="allUniqueSnaps"></param>
        private static void GenerateSyncEventEnum(List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps)
        {
            SyncEvent_GeneratedTypes_Generator.GenerateEnum(allUniqueSnaps);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Apply changes to the code generation ID of a GNP. This needs to be call every time you set a snap's code generation id and you want it to be reflected in the attached GNPs aswell.
        /// </summary>
        /// <param name="gnp"></param>
        /// <param name="codeGenId"></param>
        private static void ApplyToGNPCodeGenerationId(GONetParticipant gnp, int codeGenId)
        {
            if(gnp == null)
            {
                GONetLog.Error("The GONetParticipant is null");
                return;
            }

            SerializedObject so = new SerializedObject(gnp);
            so.Update();
            so.FindProperty(nameof(GONetParticipant.codeGenerationId)).intValue = codeGenId;
            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// This method is one of the key tasks (out of three) of the GONet's code generation pipeline. The goal of this code generation task is to create all the necessary generated scripts for
        /// GONet to work properly.
        /// </summary>
        internal static void GenerateFiles()
        {
            /*This method is one of the key tasks (out of three) of the GONet's code generation pipeline. The goal of this code generation task is to create all the necessary generated scripts for
            * GONet to work properly. It is executed in the following cases:
            * 1. Inmediately before Unity changes from EditorMode to PlayMode. UnityEditor.EditorApplication.playModeStateChanged.OnEditorPlayModeStateChanged(PlayModeStateChange state)
            * 2. Inmediately before Unity starts the build process. UnityEditor.Builds.IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
            *
            * This is how this method works:
            * 1. We Generate one GONetAutoMagicalSyncCompanion_Generated_X.bin file per unique snap where X is the code generation id of that unique snap. Each of these files contains information
            *    about the GONetAutoMagicalSync fields attached to a specific GONetParticipant apart from information about syncable animation parameters.
            * 2. We generate one single BobWad_Generated.bin file containing all the information related to IGONetEvents.
            * 3. We refresh the assets data base since we have created new scripts within the Assets folder.
            */

            List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps = LoadAllSnapsFromPersistenceFile(GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH);
            { //Generate one GONetParticipant_AutoMagicalSyncCompanion_Generated file per unique snap.
                int count = allUniqueSnaps.Count;
                for (int i = 0; i < count; ++i)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers one = allUniqueSnaps[i];
                    one.ApplyProfileToAttributes_IfAppropriate(); // this needs to be done for everyone prior to generation!

                    //ASK SHAUN POSSIBLE REFACTOR: Since it is not possible to get into this catch condition should we make it generic (catch(Exception e))?
                    try
                    {
                        GenerateClass(one);
                    }
                    catch (NullReferenceException)
                    {
                        // This is expected to happen when removing [GONetAutoMagicalSync] from members  and no more exist on any members in that MB/class and then running generation is trying to access information to generate class for that old data 
                    }
                }
            }

            {//Generate the BobWad_Generated.cs file.
                //This list contains all the structs that implement IGONetEvent or any child interface
                List<Type> allUniqueStructTypes = GetAllConcreteIGONetEventTypes();
                byte[] usedCodegenIds = new byte[allUniqueSnaps.Count];
                for (int i = 0; i < allUniqueSnaps.Count; ++i)
                {
                    usedCodegenIds[i] = allUniqueSnaps[i].codeGenerationId;
                }
                BobWad_Generated_Generator.GenerateClass(usedCodegenIds, allUniqueSnaps, allUniqueStructTypes);
            }

            AssetDatabase.SaveAssets(); // since we are generating the class that is the real thing of value here, ensure we also save the asset to match current state
            AssetDatabase.Refresh(); // get the Unity editor to recognize any new code just added and recompile it
        }

        private static List<Type> GetAllConcreteIGONetEventTypes()
        {
            List<Type> structNames = new List<Type>();
            foreach (var types in AppDomain.CurrentDomain.GetAssemblies()
                                  .OrderBy(a => a.FullName).Select(a => a.GetLoadableTypes()
                                  .Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(IGONetEvent)) && !t.IsAbstract /* SHAUN excluded for MemoryPack not allowing interface unoins to be for structs!!! TODO consider another alternative than converting all to classes!!! && TypeUtils.IsStruct(t) */)
                                  .OrderBy(t2 => t2.FullName)))
            {
                foreach (var type in types)
                {
                    structNames.Add(type);
                }
            }

            return structNames;
        }

        /// <summary>
        /// Updates the code generation unique snaps preparing them for the generation process. This method only track snaps within any scene inside the build settings. Those scenes that are not 
        /// within this list will be ignored.
        /// </summary>
        public static void UpdateAllUniqueSnaps()
        {
            //You can not execute this if you are in play mode or you are about to enter.
            if (EditorApplication.isPlayingOrWillChangePlaymode) // see http://wiki.unity3d.com/index.php/SaveOnPlay for the idea here!
            {
                GONetLog.Warning("You can't execute this while Unity is in Play mode. Exit Play mode and try again. Aborting process...");
                return;
            }

            //Save an empty file in order to force assets folder to update too
            SaveUniqueSnapsToPersistenceFile(new List<GONetParticipant_ComponentsWithAutoSyncMembers>(), GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene currentScene;
            string currentScenePath = System.String.Empty;

            {//Save the current scene's path for coming back to it at the end of this process. It would be weird if during this process you end up in another different scene.
                currentScene = EditorSceneManager.GetActiveScene();

                //Save current scene if neccesary to avoid loosing those changes
                if (currentScene.isDirty)
                {
                    EditorSceneManager.SaveScene(currentScene);
                }

                currentScenePath = currentScene.path;
            }

            SnapComparer snapComparer = SnapComparer.Instance;
            List<GONetParticipant_ComponentsWithAutoSyncMembers> uniqueSnapsFromAssetsFolder;
            List<GONetParticipant_ComponentsWithAutoSyncMembers> uniqueSnapsFromScenes;
            List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps = new List<GONetParticipant_ComponentsWithAutoSyncMembers>();

            {//Load all unique snaps (From both the in scenes file and the in assets folder file)
                uniqueSnapsFromScenes = LoadAllSnapsFromPersistenceFile(GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);

                foreach (var uniqueSnapFromScene in uniqueSnapsFromScenes)
                {
                    if (!allUniqueSnaps.Contains(uniqueSnapFromScene, snapComparer))
                    {
                        //In case two different unique snaps have the same codegen id and it is different from 0, the last one gets an unset id in order to assing it later.
                        foreach (var snaps in allUniqueSnaps)
                        {
                            if (snaps.codeGenerationId == uniqueSnapFromScene.codeGenerationId)
                            {
                                uniqueSnapFromScene.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            }

                            break;
                        }
                    }
                }

                uniqueSnapsFromAssetsFolder = LoadAllSnapsFromPersistenceFile(ASSET_FOLDER_SNAPS_FILE);
                foreach (var uniqueSnapFromAssetsFolder in uniqueSnapsFromAssetsFolder)
                {
                    if (!allUniqueSnaps.Contains(uniqueSnapFromAssetsFolder, snapComparer))
                    {
                        //In case two different unique snaps have the same codegen id and it is different from 0, the last one gets an unset id in order to assing it later.
                        foreach (var snaps in allUniqueSnaps)
                        {
                            if (snaps.codeGenerationId == uniqueSnapFromAssetsFolder.codeGenerationId)
                            {
                                uniqueSnapFromAssetsFolder.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            }

                            break;
                        }

                        allUniqueSnaps.Add(uniqueSnapFromAssetsFolder);
                    }
                }
            }

            //Get all the possible unique snaps that are within any scene inside the build.
            List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleInSceneUniqueSnaps;
            {
                LoadAllPossibleUniqueSnapsFromLoopingBuildScenes(out possibleInSceneUniqueSnaps);
            }

            {//Apply changes (Additions, deletes and/or modifications) to both the unique snaps list and to the corresponding in scene game objects
                HashSet<GONetParticipant_ComponentsWithAutoSyncMembers> snapsToDelete = new HashSet<GONetParticipant_ComponentsWithAutoSyncMembers>();
                foreach (var uniqueSnap in allUniqueSnaps)
                {
                    if (!possibleInSceneUniqueSnaps.Contains(uniqueSnap, snapComparer) && !uniqueSnapsFromAssetsFolder.Contains(uniqueSnap, snapComparer))
                    {
                        snapsToDelete.Add(uniqueSnap);
                    }
                }

                foreach (var snapToDelete in snapsToDelete)
                {
                    allUniqueSnaps.Remove(snapToDelete);

                    if (uniqueSnapsFromScenes.Contains(snapToDelete, snapComparer))
                    {
                        uniqueSnapsFromScenes.Remove(snapToDelete);
                    }
                }

                foreach (var possibleUniqueSnap in possibleInSceneUniqueSnaps)
                {
                    //If this snap is new, generate a new code gen id for it.
                    if (!allUniqueSnaps.Contains(possibleUniqueSnap, snapComparer))
                    {
                        possibleUniqueSnap.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                        allUniqueSnaps.Add(possibleUniqueSnap);
                        uniqueSnapsFromScenes.Add(possibleUniqueSnap);
                    }
                }

                //Set all unset codegeneration id
                foreach (var uniqueSnap in allUniqueSnaps)
                {
                    if (uniqueSnap.codeGenerationId == GONetParticipant.CodeGenerationId_Unset)
                    {
                        uniqueSnap.codeGenerationId = GetNewCodeGenerationId(allUniqueSnaps);
                    }
                }

                //Apply changes not only to snaps but also to their corresponding GNPs
                int scenesCount = EditorSceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < scenesCount; ++i)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

                    Scene openScene = EditorSceneManager.OpenScene(scenePath);
                    EditorSceneManager.SetActiveScene(openScene);
                    if (openScene.IsValid())
                    {
                        bool hasSceneChanged = false;
                        List<GONetParticipant> gnps;
                        GetGNPsWithinScene(openScene, out gnps);
                        GONetParticipant_ComponentsWithAutoSyncMembers snap;

                        foreach (GONetParticipant gnp in gnps)
                        {
                            snap = new GONetParticipant_ComponentsWithAutoSyncMembers(gnp);
                            foreach (var uniqueSnap in allUniqueSnaps)
                            {
                                if (snapComparer.Equals(uniqueSnap, snap))
                                {
                                    if (gnp.codeGenerationId != uniqueSnap.codeGenerationId)
                                    {
                                        ApplyToGNPCodeGenerationId(gnp, uniqueSnap.codeGenerationId);
                                        hasSceneChanged = true;
                                    }
                                }
                            }
                        }

                        if (hasSceneChanged)
                        {
                            EditorSceneManager.SaveScene(openScene);
                        }
                    }
                    else
                    {
                        GONetLog.Warning("You are trying to open an invalid scene. Scene path: " + scenePath);
                    }
                }
            }

            {//Go back to the initial scene when this process started
                currentScene = EditorSceneManager.OpenScene(currentScenePath);
                if (currentScene.IsValid())
                {
                    EditorSceneManager.SetActiveScene(currentScene);
                }
                else
                {
                    GONetLog.Error("Not switching back to the scene that the user was in when this process started. The current scene is not valid.");
                }
            }

            GenerateSyncEventEnum(allUniqueSnaps);

            //Save the updated unique snaps in their corresponding binary files
            SaveUniqueSnapsToPersistenceFile(uniqueSnapsFromScenes, GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
            SaveUniqueSnapsToPersistenceFile(allUniqueSnaps, GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH);

            if (File.Exists(GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN))
            {
                File.Delete(GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void LoadAllPossibleUniqueSnapsFromLoopingBuildScenes(out List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleUniqueSnaps)
        {
            possibleUniqueSnaps = new List<GONetParticipant_ComponentsWithAutoSyncMembers>();

            int scenesCount = EditorSceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < scenesCount; ++i)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);

                Scene openScene = EditorSceneManager.OpenScene(scenePath);
                EditorSceneManager.SetActiveScene(openScene);
                if (openScene.IsValid())
                {
                    List<GONetParticipant> gnps;
                    GetGNPsWithinScene(openScene, out gnps);
                    foreach (GONetParticipant gnp in gnps)
                    {
                        possibleUniqueSnaps.Add(new GONetParticipant_ComponentsWithAutoSyncMembers(gnp));
                    }
                }
                else
                {
                    GONetLog.Warning("You are trying to open an invalid scene. Scene path: " + scenePath);
                }
            }
        }

        private static void GetGNPsWithinScene(Scene scene, out List<GONetParticipant> gonetParticipantsInOpenScenes)
        {
            gonetParticipantsInOpenScenes = new List<GONetParticipant>();
            GameObject[] rootGOs = scene.GetRootGameObjects();
            for (int iRootGO = 0; iRootGO < rootGOs.Length; ++iRootGO)
            {
                GONetParticipant[] gonetParticipantsInOpenScene = rootGOs[iRootGO].GetComponentsInChildren<GONetParticipant>();
                gonetParticipantsInOpenScenes.AddRange(gonetParticipantsInOpenScene);
            }
        }

        //For debug only purposes. As soon as this feature is done this can be deleted
        private static void PrintGONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant_ComponentsWithAutoSyncMembers print)
        {
            Debug.Log($"SNAP WITH CODEGEN ID: {print.codeGenerationId}");
            for (int i = 0; i < print.ComponentMemberNames_By_ComponentTypeFullName.Length; ++i)
            {
                Debug.Log("COMPONENT TYPE NAME: " + print.ComponentMemberNames_By_ComponentTypeFullName[i].componentTypeFullName);
                for (int j = 0; j < print.ComponentMemberNames_By_ComponentTypeFullName[i].autoSyncMembers.Length; ++j)
                {
                    Debug.Log("AUTO SYNC MEMBER NAME: " + print.ComponentMemberNames_By_ComponentTypeFullName[i].autoSyncMembers[j].memberName);
                }
            }
        }

        private static List<GONetParticipant_ComponentsWithAutoSyncMembers> LoadAllSnapsFromPersistenceFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                byte[] snapsFileBytes = File.ReadAllBytes(filePath);
                if (snapsFileBytes != null && snapsFileBytes.Length > 0)
                {
                    var l = SerializationUtils.DeserializeFromBytes<List<GONetParticipant_ComponentsWithAutoSyncMembers>>(snapsFileBytes);
                    List<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember> validMembers = new List<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember>();

                    foreach (var ll in l)
                    {
                        foreach (var lll in ll.ComponentMemberNames_By_ComponentTypeFullName)
                        {
                            foreach (var llll in lll.autoSyncMembers)
                            {
                                //If PostDeserialize_InitAttribute returns false it means that the attribute is no longer valid within this component
                                //(It could be deleted and the persistence file could not be updated)
                                if (llll.PostDeserialize_InitAttribute(lll.componentTypeAssemblyQualifiedName))
                                {
                                    validMembers.Add(llll);
                                }
                            }

                            lll.autoSyncMembers = validMembers.ToArray();
                            validMembers.Clear();
                        }
                    }
                    return l;
                }
            }

            return new List<GONetParticipant_ComponentsWithAutoSyncMembers>();
        }

        private static void SaveUniqueSnapsToPersistenceFile(List<GONetParticipant_ComponentsWithAutoSyncMembers> allSnaps, string filePath)
        {
            if (!Directory.Exists(GENERATION_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATION_FILE_PATH);
            }

            allSnaps.ForEach(x => GONetLog.Debug("SAVING to persistence x.codeGenerationId: " + x.codeGenerationId + " x.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

            int returnBytesUsedCount;
            byte[] snapsFileBytes = SerializationUtils.SerializeToBytes(allSnaps, out returnBytesUsedCount, out bool doesNeedToReturn);
            FileUtils.WriteBytesToFile(filePath, snapsFileBytes, returnBytesUsedCount, FileMode.Truncate);
            if (doesNeedToReturn)
            {
                SerializationUtils.ReturnByteArray(snapsFileBytes);
            }
        }

        private static void RemoveGaps(byte[] codeGenerationIdsUsed_beforeAndAfter, List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps)
        {
            int iMax = codeGenerationIdsUsed_beforeAndAfter.Max();

            for (byte i = 1; i < iMax; i++)
            {
                if (codeGenerationIdsUsed_beforeAndAfter[i] == GONetParticipant.CodeGenerationId_Unset)
                {
                    codeGenerationIdsUsed_beforeAndAfter[i] = i;
                    allUniqueSnaps[allUniqueSnaps.FindIndex(x => x.codeGenerationId == iMax)].codeGenerationId = i;
                    codeGenerationIdsUsed_beforeAndAfter[iMax] = GONetParticipant.CodeGenerationId_Unset;
                    --iMax;
                }
            }
        }

        private static void FillGapsWhenMoreDeletesThanAdds(byte[] codeGenerationIdsUsed_beforeAndAfter)
        {
            //GONetLog.Debug("billems...before filling gaps...max_codeGenerationId: " + codeGenerationIdsUsed_beforeAndAfter.Max());

            Stack<byte> stack_occupiedBefore = new Stack<byte>();
            int iMax = codeGenerationIdsUsed_beforeAndAfter.Max();
            for (byte i = 1; i <= iMax; ++i)
            {
                if (codeGenerationIdsUsed_beforeAndAfter[i] != GONetParticipant.CodeGenerationId_Unset)
                {
                    stack_occupiedBefore.Push(i);
                }
            }
            for (byte i = 1; i < iMax; ++i)
            {
                if (codeGenerationIdsUsed_beforeAndAfter[i] == GONetParticipant.CodeGenerationId_Unset)
                { // in a gap, fill it from the end of the populated/set values
                    byte iEnd = stack_occupiedBefore.Pop();
                    codeGenerationIdsUsed_beforeAndAfter[iEnd] = i;
                    --iMax; // ensure we don't check this index again since we just zeroed it out by moving the old index up to the new gap
                    //GONetLog.Debug($"\tgappems...[{iEnd}]=" + i);
                }
                //else GONetLog.Debug($"\tnorm[{i}]:{codeGenerationIdsUsed_beforeAndAfter[i]}");
            }

            //GONetLog.Debug("billems...after filling gaps...max_codeGenerationId: " + codeGenerationIdsUsed_beforeAndAfter.Max());
        }

        /// <summary>
        /// POST: If an non-0 index is available (i.e., <see cref="GONetParticipant.CodeGenerationId_Unset"/>), 
        ///       <paramref name="codeGenerationIdsUsed_beforeAndAfter"/> is updated and at that index
        ///       it is set to the new codegen value and that value is returned.
        /// </summary>
        private static byte AssignNextAvailableCodeGenId(byte[] codeGenerationIdsUsed_beforeAndAfter)
        {
            int length = codeGenerationIdsUsed_beforeAndAfter.Length;
            for (byte i = 1; i < length; ++i)
            {
                if (codeGenerationIdsUsed_beforeAndAfter[i] == GONetParticipant.CodeGenerationId_Unset)
                {
                    codeGenerationIdsUsed_beforeAndAfter[i] = i;
                    return i;
                }
            }

            throw new Exception("OOops none available!");
        }

        private static void GenerateClass(GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry)
        {
            var t4Template = new GONetParticipant_AutoMagicalSyncCompanion_GeneratedTemplate(uniqueEntry);
            string generatedClassText = t4Template.TransformText();

            if (!Directory.Exists(GENERATED_FILE_PATH))
            {
                Directory.CreateDirectory(GENERATED_FILE_PATH);
            }
            string writeToPath = string.Concat(GENERATED_FILE_PATH, t4Template.ClassName, C_SHARP_FILE_SUFFIX);
            File.WriteAllText(writeToPath, generatedClassText);
        }
    }

    /// <summary>
    /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember
    {
        /// <summary>
        /// If false, the member is a property.
        /// </summary>
        public bool isField;
        public string memberTypeFullName;
        public string memberName;

        /// <summary>
        /// A value of 0 indicates this single member does NOT represent an animator controller parameter id
        /// </summary>
        public int animatorControllerParameterId = 0;
        public string animatorControllerParameterMethodSuffix;
        public string animatorControllerParameterTypeFullName;
        public string animatorControllerName;
        public string animatorControllerParameterName;

        [MemoryPackIgnore]
        public GONetAutoMagicalSyncAttribute attribute;
        private int nameHash;
        private string methodSuffix;
        private string typeFullName;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        [MemoryPackConstructor]
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

        /// <summary>
        /// Initializes through reflection the auto magical sync member.
        /// </summary>
        /// <param name="memberOwner_componentTypeAssemblyQualifiedName"></param>
        /// <returns>
        /// This method returns true if the initialization process was succesfully.
        /// However it will return false if the member with name <see cref="memberName"/> is no longer within the component <see cref="memberOwner_componentTypeAssemblyQualifiedName"/>
        /// </returns>
        internal bool PostDeserialize_InitAttribute(string memberOwner_componentTypeAssemblyQualifiedName)
        {
            Type memberOwnerType = Type.GetType(memberOwner_componentTypeAssemblyQualifiedName);
            MemberInfo[] syncMembers = memberOwnerType.GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);

            if (syncMembers.Length == 0) //If length is 0 it means the member could not be found (It has been deleted)
            {
                return false;
            }

            MemberInfo syncMember = syncMembers[0];
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

            return true;
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
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Vector2, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(Vector2Serializer)) },
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Vector3, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(Vector3Serializer)) },
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Vector4, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(Vector4Serializer)) },
                                new SyncType_CustomSerializer_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Quaternion, CustomSerializerType = new TypeReferences.ClassTypeReference(typeof(QuaternionSerializer)) },
                            };
                        defaultProfile.SyncValueTypeValueBlendingOverrides = new SyncType_CustomValueBlending_Pair[] {
                                new SyncType_CustomValueBlending_Pair() { ValueType = GONetSyncableValueTypes.System_Single, CustomValueBlendingType = new TypeReferences.ClassTypeReference(typeof(GONetDefaultValueBlending_Float)) },
                                new SyncType_CustomValueBlending_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Vector3, CustomValueBlendingType = new TypeReferences.ClassTypeReference(typeof(GONetDefaultValueBlending_Vector3)) },
                                new SyncType_CustomValueBlending_Pair() { ValueType = GONetSyncableValueTypes.UnityEngine_Quaternion, CustomValueBlendingType = new TypeReferences.ClassTypeReference(typeof(GONetDefaultValueBlending_Quaternion)) },
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

                if (profile.SyncValueTypeValueBlendingOverrides != null && profile.SyncValueTypeValueBlendingOverrides.Length > 0)
                {
                    GONetSyncableValueTypes gonetSyncType;
                    if (gonetSyncTypeByRealTypeMap.TryGetValue(syncMemberType, out gonetSyncType))
                    {
                        if (profile.SyncValueTypeValueBlendingOverrides.Any(x => x.ValueType == gonetSyncType))
                        {
                            SyncType_CustomValueBlending_Pair first = profile.SyncValueTypeValueBlendingOverrides.First(x => x.ValueType == gonetSyncType);
                            attribute.CustomValueBlending_Type = first.CustomValueBlendingType.Type;

                            if (attribute.CustomValueBlending_Type != null)
                            {
                                GONetLog.Debug("GONet will use the custom value blending type: " + attribute.CustomValueBlending_Type.FullName);
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
    [MemoryPackable]
    public partial class GONetParticipant_ComponentsWithAutoSyncMembers_Single
    {
        public string componentTypeName;
        public string componentTypeFullName;
        public string componentTypeAssemblyQualifiedName;
        public bool isTransformIntrinsics;

        /// <summary>
        /// in deterministic order!
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] autoSyncMembers;

        /// <summary>
        /// IMPORTANT: do NOT use.  This is for deserialize/load from persistence:
        /// </summary>
        [MemoryPackConstructor]
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single() { }

        /// <param name="component_autoSyncMembers">in deterministic order!</param>
        internal GONetParticipant_ComponentsWithAutoSyncMembers_Single(
            Component component, 
            GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember[] component_autoSyncMembers,
            bool isTransformIntrinsics = false)
        {
            Type componentType = component.GetType();
            componentTypeName = componentType.Name;
            componentTypeFullName = componentType.FullName;
            componentTypeAssemblyQualifiedName = componentType.AssemblyQualifiedName;

            autoSyncMembers = component_autoSyncMembers;
            this.isTransformIntrinsics = isTransformIntrinsics;
        }
    }

    /// <summary>
    /// This is what represents a unique combination of <see cref="GONetAutoMagicalSyncAttribute"/>s on a <see cref="GameObject"/> with <see cref="GONetParticipant"/> "installed."
    /// A set of these gets persisted into a file at <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.SNAPS_FILE"/>.
    /// 
    /// NOTE: once called this Snap...renamed to this.
    /// 
    /// </summary>
    [MemoryPackable]
    public partial class GONetParticipant_ComponentsWithAutoSyncMembers
    {
        /// <summary>
        /// Assigned once know to be a unique combo.
        /// Relates directly to <see cref="GONetParticipant.codeGenerationId"/>.
        /// IMPORTANT: MessagePack for C# is not supporting internal field.....so, this is now public...boo!
        /// </summary>
        public byte codeGenerationId;

        /// <summary>
        /// In deterministic order....with the one for <see cref="GONetParticipant.GONetId"/> first due to special case processing....also <see cref="GONetParticipant.ASSumed_GONetId_INDEX"/>
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single[] ComponentMemberNames_By_ComponentTypeFullName;

        /// <summary>
        /// This is ONLY populated when <see cref="GONetParticipant_ComponentsWithAutoSyncMembers.GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant)"/> is used during 
        /// the time leading up to doing a generation call (i.e., <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.UpdateGenerationInformation"/>).
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
        [MemoryPackConstructor]
        public GONetParticipant_ComponentsWithAutoSyncMembers() { }

        internal GONetParticipant_ComponentsWithAutoSyncMembers(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;

            int iSinglesAdded = -1;
            int gonetIdOriginalIndex_single = -1;
            int gonetIdOriginalIndex_singleMember = -1;

            var componentMemberNames_By_ComponentTypeFullName = new LinkedList<GONetParticipant_ComponentsWithAutoSyncMembers_Single>();

            //Check if there are null components within each GNP gameobject and if so, delete them
            List<MonoBehaviour> monoBehaviourComponents = gonetParticipant.GetComponents<MonoBehaviour>().ToList();
            List<MonoBehaviour> nullMonoComponents = new List<MonoBehaviour>();
            for (int i = 0; i < monoBehaviourComponents.Count; i++)
            {
                if (monoBehaviourComponents[i] == null)
                {
                    nullMonoComponents.Add(monoBehaviourComponents[i]);
                }
            }

            foreach (var nullComponent in nullMonoComponents)
            {
                monoBehaviourComponents.Remove(nullComponent);
            }

            foreach (MonoBehaviour component in monoBehaviourComponents.OrderBy(c => c.GetType().FullName))
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

                //Identify thos members that are not public and print a Warning in order to tell the user that GONet will not track/sync these ones.
                IEnumerable<MemberInfo> nonPublicMembers = component
                    .GetType()
                    .GetMembers(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(member => (member.MemberType == MemberTypes.Property || member.MemberType == MemberTypes.Field)
                                    && member.GetCustomAttribute(typeof(GONetAutoMagicalSyncAttribute), true) != null);

                int nonPublicMembersCount = nonPublicMembers.Count();
                for (int iprivateMember = 0; iprivateMember < nonPublicMembersCount; ++iprivateMember)
                {
                    MemberInfo nonPublicMember = nonPublicMembers.ElementAt(iprivateMember);
                    GONetLog.Warning($"IMPORTANT GONet consideration: The {nonPublicMember.MemberType} called {nonPublicMember.Name} within {component.GetType().FullName} needs to be public in order to be tracked by the GONetAutoMagicalSyncValue attribute.");
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

                var newSingle_transform = 
                    new GONetParticipant_ComponentsWithAutoSyncMembers_Single(
                        gonetParticipant.transform, component_autoSyncMembers_transform, isTransformIntrinsics: true);
                componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle_transform);
            }

            Animator animator = gonetParticipant.GetComponent<Animator>();
            if (AnimationEditorUtils.TryGetAnimatorControllerParameters(animator, out var parameters)) // IMPORTANT: in editor, looks like animator.parameterCount is [sometimes!...figured out when...it is only when the Animator window is open and its controller is selected...editor tries to do tricky stuff that whacks this all out for some reason] 0 even when shit is there....hence the usage of animator.runtimeAnimatorController.parameters instead of animator.parameters
            { // intrinsic Animator properties that cannot manually have the [GONetAutoMagicalSync] added.... (e.g., transform rotation and position)
                if (parameters != null && parameters.Length > 0)
                {
                    var component_autoSyncMembers_animator_parameter = new List<GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember>();

                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (!StringUtils.IsStringValidForCSharpNamingConventions(parameters[i].name))
                        {
                            GONetLog.Error($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                            Debug.LogError($"The animation parameter name '{parameters[i].name}' is not valid. Skipping this parameter. Please, check the rules that a string must follow in order to be valid. You can find them within the class StringUtils.IsStringValidForCSharpNamingConventions");
                            continue;
                        }

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

                    if (component_autoSyncMembers_animator_parameter.Count > 0)
                    {
                        var newSingle_animator = new GONetParticipant_ComponentsWithAutoSyncMembers_Single(gonetParticipant.GetComponent<Animator>(), component_autoSyncMembers_animator_parameter.ToArray());
                        componentMemberNames_By_ComponentTypeFullName.AddLast(newSingle_animator);
                    }
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
            if (!Application.isPlaying)
            {
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
        }
    }
}