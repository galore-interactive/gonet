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
using GONet.Generation;
using GONet.PluginAPI;
using GONet.Utils;
using MemoryPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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
        private static bool isBuilding;
        public static bool IsBuilding => isBuilding;
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            isBuilding = true;
            GONetSpawnSupport_DesignTime.ClearAllDesignTimeMetadata();
            CompilationPipeline.compilationStarted += CompilationPipelineOnCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipelineOnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += CompilationPipelineOnCompilationFinished;

            GONetLog.Debug($"~~~~~~~~~~~~GEEPs start  BUILD did this!");
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.UpdateAllUniqueSnaps();
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GenerateFiles();
            FileUtils.CopyFile(
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH, 
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH_LAST_BUILD);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DeleteGeneratedFiles();

            if (report == null)
            {
                GONetLog.Error($"The build report is not available after build is reported as being completed.  GONet will not process things completely.");
                return;
            }

            if (report.summary.result == BuildResult.Unknown)
            {
                // delay the call to process this in hopes that unity will have the report status ready to check at that time, which in testing it does!
                EditorApplication.delayCall += () => OnPostprocessBuild_ProcessDirtyIfAppropriate(report);
            }
            else
            {
                OnPostprocessBuild_ProcessDirtyIfAppropriate(report);
            }
        }

        private static void OnPostprocessBuild_ProcessDirtyIfAppropriate(BuildReport report)
        {
            GONetLog.Debug($"~~~~~~~~~~~~GEEPs end  BUILD did this!  report.summary.result: {report.summary.result}");
            isBuilding = false;
            if (report.summary.result == BuildResult.Succeeded)
            {
                GONetSpawnSupport_DesignTime.IndicateGONetDesignTimeNoLongerDirty();
                GONetSpawnSupport_DesignTime.RecordScenesInSuccessfulBuild();
            }
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
        internal const string GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH_LAST_BUILD = GENERATION_FILE_PATH + "All_Unique_Snaps_LastBuild" + BINARY_FILE_SUFFIX;
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
            GONetParticipant.ResetCalled += GONetParticipant_EditorOnlyReset;
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

        internal static void OnPostprocessAllAssets_TakeNoteOfAnyGONetChanges(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            IEnumerable<DesignTimeMetadata> designTimeLocations_gonetParticipants =
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();
            // Get paths from design-time metadata for both project:// and resources:// prefixes (for backwards compatibility)
            IEnumerable<string> gnpPrefabAssetPaths = designTimeLocations_gonetParticipants
                .Where(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) || x.Location.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                .Select(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX)
                    ? x.Location.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length)
                    : x.Location.Substring(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX.Length));

            if (importedAssets != null) foreach (string importedAsset in importedAssets)
            {
                //Debug.Log("Reimported Asset: " + importedAsset);
                HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate(importedAsset);
            }

            if (deletedAssets != null) foreach (string deletedAsset in deletedAssets)
            {
                //Debug.Log("Deleted Asset: " + deletedAsset);
                if (gnpPrefabAssetPaths.Contains(deletedAsset))
                {
                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason($"GNP prefab asset deleted: {deletedAsset}");
                }
                if (deletedAsset.EndsWith(".asset") &&
                    deletedAsset.StartsWith(GONetEditorWindow.ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH))
                {
                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                        $"Likely {nameof(GONetAutoMagicalSyncSettings_ProfileTemplate)} deleted: {deletedAsset}");
                }
            }

            // Official GONet paths
            const string ResourcesFolder = "/Resources/";

            if (movedAssets != null && movedFromAssetPaths != null) for (int i = 0; i < movedAssets.Length; i++)
            {
                string movedAsset = movedAssets[i];
                string movedFrom = movedFromAssetPaths[i];

                //Debug.Log("Moved Asset: " + movedAsset + " from: " + movedFrom);

                // Check for GONetAutoMagicalSyncSettings_ProfileTemplate
                if (movedAsset.EndsWith(".asset") &&
                    AssetDatabase.LoadAssetAtPath<GONetAutoMagicalSyncSettings_ProfileTemplate>(movedAsset) != null &&
                    !movedAsset.StartsWith(GONetEditorWindow.ASSETS_SYNC_SETTINGS_PROFILES_FOLDER_PATH))
                {
                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                        $"{nameof(GONetAutoMagicalSyncSettings_ProfileTemplate)} moved out of the official folder: {movedFrom} -> {movedAsset}");
                }

                // Check for GONetParticipant prefabs
                if (movedAsset.EndsWith(".prefab") &&
                    AssetDatabase.LoadAssetAtPath<GameObject>(movedAsset)?.GetComponent<GONetParticipant>() != null &&
                    !movedAsset.Contains(ResourcesFolder))
                {
                    GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                        $"{nameof(GONetParticipant)} prefab moved out of a Resources folder: {movedFrom} -> {movedAsset}");
                }
            }
        }

        #region imported asset, on GNP prefab change:
        static void HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate(string importedAssetPath)
        {
            // Ensure the modified path is within the Assets folder and has a valid prefab extension, and in/under Resources folder
            if (!importedAssetPath.StartsWith("Assets/") || !importedAssetPath.EndsWith(".prefab") | !IsInResourcesFolder(importedAssetPath))
            {
                return;
            }

            // Load last build's design time metadata to get previously tracked GONetParticipant prefabs
            IEnumerable<DesignTimeMetadata> designTimeLocations_gonetParticipants_lastBuild =
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();

            // Get paths from last build for both project:// and resources:// prefixes (for backwards compatibility)
            HashSet<string> gnpPrefabAssetPaths_lastBuild =
                designTimeLocations_gonetParticipants_lastBuild
                    .Where(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) || x.Location.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                    .Select(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX)
                        ? x.Location.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length)
                        : x.Location.Substring(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX.Length))
                    .ToHashSet();

            // Determine if the modified prefab path was in the last build's data
            bool wasInLastBuild = gnpPrefabAssetPaths_lastBuild.Contains(importedAssetPath);
            bool hasGONetParticipant = PrefabContainsGONetParticipant(importedAssetPath);

            if (!wasInLastBuild && hasGONetParticipant)
            {
                GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason($"GONetParticipant added to prefab: {importedAssetPath}");
            }
            else if (wasInLastBuild && !hasGONetParticipant)
            {
                GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason($"GONetParticipant removed from prefab: {importedAssetPath}");
            }
        }

        static bool PrefabContainsGONetParticipant(string prefabPath)
        {
            // Load the prefab asset at the specified path and check if it contains a GONetParticipant component
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return false;
            }

            return prefab.GetComponentInChildren<GONetParticipant>() != null;
        }

        static bool IsInResourcesFolder(string path)
        {
            // Check if the path contains "/Resources/" (indicating it's in a Resources folder within the Assets hierarchy)
            return path.Contains("/Resources/");
        }
        #endregion

        /// <summary>
        /// POST: All assets/project SNAPs updated!
        /// </summary>
        internal static void OnPostprocessAllAssets_UpdateAssetProjectSnaps(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            HashSet<string> existingGONetParticipantAssetPaths = new HashSet<string>();
            IEnumerable<DesignTimeMetadata> designTimeLocations_gonetParticipants = 
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();

            if (importedAssets != null) foreach (string importedAsset in importedAssets)
            {
                //Debug.Log("Reimported Asset: " + importedAsset);

                string possibleDesignTimeAssetLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, importedAsset);
                if (designTimeLocations_gonetParticipants.Any(x => x.Location == possibleDesignTimeAssetLocation))
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

            if (deletedAssets != null) foreach (string deletedAsset in deletedAssets)
            {
                //Debug.Log("Deleted Asset: " + deletedAsset);

                existingGONetParticipantAssetPaths.Remove(deletedAsset); // no matter what, if this was deleted....get this out of here
            }

            if (movedAssets != null && movedFromAssetPaths != null) for (int i = 0; i < movedAssets.Length; i++)
            {
                //Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);

                existingGONetParticipantAssetPaths.Remove(movedFromAssetPaths[i]); // no matter what, if this was moved....get this old one out of here

                string movedAsset = movedAssets[i];
                string possibleDesignTimeAssetLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, movedAsset);
                if (designTimeLocations_gonetParticipants.Any(x => x.Location == possibleDesignTimeAssetLocation))
                {
                    existingGONetParticipantAssetPaths.Add(movedAsset);
                }
            }

            { // after that, we need to see about do things to ensure ALL prefabs get generated when stuff (e.g., C# files) change that possibly have some changes to sync stuffs
                // Get paths from design-time metadata for both project:// and resources:// prefixes (for backwards compatibility)
            IEnumerable<string> gnpPrefabAssetPaths = designTimeLocations_gonetParticipants
                .Where(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) || x.Location.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                .Select(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX)
                    ? x.Location.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length)
                    : x.Location.Substring(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX.Length));
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

        internal static void OnEditorPlayModeStateChanged_BlockEnteringPlaymodeIfUniqueSnapsChanged(PlayModeStateChange state, out bool didPreventEnteringPlaymode)
        {
            didPreventEnteringPlaymode = false;
            LastPlayModeStateChange = state;

            /* v1.3.1 TURN OFF automatic code generation related activities in preference for only doing it during a build to avoid excessive actions that are only really required to be done for builds
                // TODO add comment about why are we doing this......   
            */
            // One main reason we do still need to do this (i.e., disregard the comment from v1.3.1 turning this off) is
            // GONetEventBus.InitializeEventMap() method requires all the generated SyncEvent_Xxx classes be present for Subscribe to sync events to work...
            // And honestly, the more I think about it in this moment (2024 AUG 17), all these generated files ARE always needed during runtime of the game../
            // So, I cannot actually fathom what I was thinking when removing it for v1.3.1 ... I mean, I understand I was trying to reduce all the gonet operations 
            // occurring in the editor that development teams might find aggrevating, but we need the generated files when playing!!!  We have done other work
            // since the v1.3.1 days to reduce the operations and we should be in good shape....this has to stay!
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // check for GONet changes since last build and warn about GONet not going to run and a build is needed!
                if (File.Exists(GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH_LAST_BUILD))
                {
                    UpdateAllUniqueSnaps(shouldBypassChangePlaymodeCheck: true); // TODO FIXME this is terribly slow thing to do each time entering play mode...but right now, it is needed to updated the current unique SNAPS for comparison with same info but from last build
                    if (!FileUtils.DoFilesHaveSameContents(GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH, GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH_LAST_BUILD))
                    {
                        string dirtyReason = $"GONet detected one or more changes in the unique configurations of 'components with auto sync members' in the project (i.e., known internally as 'SNAPs').  One likely reason for this is the addition/removal of [{nameof(GONetAutoMagicalSyncAttribute).Replace("Attribute", string.Empty)}] from fields/properties.";
                        GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(dirtyReason);
                        EditorApplication.isPlaying = false; // Cancel entering Play Mode
                        Debug.LogError(string.Concat("GONet prevented entering play mode for the following reason: ", dirtyReason));
                        didPreventEnteringPlaymode = true;
                        return;
                    }
                }

                GenerateFiles();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                DeleteGeneratedFiles();
            }
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

        /// <summary>
        /// This little beast face is here to prevent the continual processing of assets in an infinite loop!
        /// </summary>
        static readonly FileBackedMap<string, int> gonetParticipantAssetsPaths_to_lastFrameCountProcessed = new FileBackedMap<string, int>(GENERATION_FILE_PATH + "gonetParticipantAssetsPaths_to_lastFrameCountProcessed" + BINARY_FILE_SUFFIX);

        static void OnEditorApplicationQuitting()
        {
            gonetParticipantAssetsPaths_to_lastFrameCountProcessed.Clear(); // don't want the frameCount from this session carrying over to next session
            GONetSpawnSupport_DesignTime.IsQuitting = true;
        }

        static int howDeepIsYourSaveStack = 0;

        /// <summary>
        /// Updates the assets folder snaps binary file.
        /// PRE: <paramref name="ensureToIncludeTheseGONetParticipantAssets_paths"/> is populated with asset path KNOWN to be <see cref="GONetParticipant"/> prefabs in the project!
        /// </summary>
        /// <param name="ensureToIncludeTheseGONetParticipantAssets_paths"></param>
        internal static void UpdateAssetsSnaps(IEnumerable<string> ensureToIncludeTheseGONetParticipantAssets_paths = default)
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
                    if (ensureToIncludeTheseGONetParticipantAssets_paths != null) foreach (string gonetParticipantAssetPath in ensureToIncludeTheseGONetParticipantAssets_paths)
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
                    foreach (var snapFromAssetFolder in newSnapsFromAssetFolders.OrderBy(x => x.SingleMemberCount))
                    {
                        bool found = false;
                        foreach (var uniqueSnap in allUniqueSnaps)
                        {
                            if(snapComparer.Equals(uniqueSnap, snapFromAssetFolder))
                            {
                                snapFromAssetFolder.codeGenerationId = uniqueSnap.codeGenerationId;
                                //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                                ApplyDesignTimeMetadataToGnp(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                                found = true;
                                break;
                            }
                        }

                        if(!found)
                        {
                            snapFromAssetFolder.codeGenerationId = GetNewCodeGenerationId(allUniqueSnaps);
                            //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                            ApplyDesignTimeMetadataToGnp(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                            allUniqueSnaps.Add(snapFromAssetFolder);
                        }
                    }

                    //if (howDeepIsYourSaveStack <= 1) // gotta be careful we do not get into an endless cycle as the method we are in now is called when scene saved.
                    {
                        // persist ALL....
                        foreach (var dtm in GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata() )
                        {
                            GONetSpawnSupport_DesignTime.EnsureExistsInPersistence_WithTheseValues(dtm);
                        }
                        GONetLog.Debug($"~~~~~~~~~~~~~~~~GEEPs PROJECT howDeepIsYourSaveStack: {howDeepIsYourSaveStack}, DTM.Count: {GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata().Count()}, all locations: {string.Join("\n", GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata().Select(x => x.Location))}");
                    }

                    SaveUniqueSnapsToPersistenceFile(newSnapsFromAssetFolders, ASSET_FOLDER_SNAPS_FILE);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                finally
                {
                    howDeepIsYourSaveStack--;
                }
            }

            Thread.CurrentThread.CurrentCulture = previousCulture;
        }

        /// <summary>
        /// Returns the lowest available code generation id of a List of snaps. It there is a gap, it will return the gap id.
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
        private static DesignTimeMetadata ApplyDesignTimeMetadataToGnp(GONetParticipant gnp, byte codeGenId, string updateLocation = default, string updateGuid = default)
        {
            if ((object)gnp == null)
            {
                GONetLog.Error("The GONetParticipant is null");
                return default;
            }

            DesignTimeMetadata metadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(gnp);

            metadata.CodeGenerationId = codeGenId;
            if (updateLocation != default)
            {
                metadata.Location = updateLocation;
            }
            if (!string.IsNullOrWhiteSpace(updateGuid))
            {
                metadata.UnityGuid = updateGuid;
            }
            return metadata;
        }

        /// <summary>
        /// This method is one of the key tasks (out of three) of the GONet's code generation pipeline. The goal of this code generation task is to create all the necessary generated scripts for
        /// GONet to work properly.
        /// </summary>
        internal static void GenerateFiles()
        {
            GONetParticipant.isGenerating = true;

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


                    // Custom contract resolver to include private members
                    var contractResolver = new DefaultContractResolver
                    {
                        DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                    };

                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        ContractResolver = contractResolver,
                        Formatting = Formatting.Indented
                    };

                    string json = JsonConvert.SerializeObject(one, settings);

                    Debug.Log($"Generating files.  Considering snap:\n{json}");
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

            GenerateAllRpcCompanions();

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

            GONetParticipant.isGenerating = false;
        }

        /// <summary>
        /// NOTE: Excluded editor only classes.
        /// </summary>
        private static List<Type> GetAllConcreteIGONetEventTypes(bool excludeEditorOnlyTypes = true)
        {
            List<Type> structNames = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                if (excludeEditorOnlyTypes && TypeUtils.IsEditorOnlyAssembly(assembly)) continue;

                var types = assembly.GetLoadableTypes()
                    .Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(IGONetEvent)) && !t.IsAbstract /* SHAUN excluded for MemoryPack not allowing interface unoins to be for structs!!! TODO consider another alternative than converting all to classes!!! && TypeUtils.IsStruct(t) */
                                && (!excludeEditorOnlyTypes || !TypeUtils.IsEditorOnlyType(t)))
                    .OrderBy(t2 => t2.FullName);

                structNames.AddRange(types);
            }

            return structNames;
        }

        /// <summary>
        /// Updates the code generation unique snaps preparing them for the generation process. This method only track snaps within any scene inside the build settings. Those scenes that are not 
        /// within this list will be ignored.
        /// </summary>
        public static void UpdateAllUniqueSnaps(bool shouldBypassChangePlaymodeCheck = false)
        {
            //You can not execute this if you are in play mode or you are about to enter.
            if (!shouldBypassChangePlaymodeCheck && 
                EditorApplication.isPlayingOrWillChangePlaymode) // see http://wiki.unity3d.com/index.php/SaveOnPlay for the idea here!
            {
                GONetLog.Warning("You can't execute this while Unity is in Play mode. Exit Play mode and try again. Aborting process...");
                return;
            }

            GONetLog.Debug($"~~~~~~~~~~~~GEEPs start");

            bool wasNewDtmForced = GONetSpawnSupport_Runtime.IsNewDtmForced;
            try
            {
                GONetSpawnSupport_Runtime.IsNewDtmForced = true;

                //Save an empty file in order to force assets folder to update too
                SaveUniqueSnapsToPersistenceFile(new(), GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Scene initialActiveScene;
                string initialActiveScenePath = System.String.Empty;

                {//Save the current scene's path for coming back to it at the end of this process. It would be weird if during this process you end up in another different scene.
                    initialActiveScene = EditorSceneManager.GetActiveScene();

                    //Save current scene if neccesary to avoid loosing those changes
                    if (initialActiveScene.isDirty)
                    {
                        EditorSceneManager.SaveScene(initialActiveScene);
                    }

                    initialActiveScenePath = initialActiveScene.path;
                }

                SnapComparer snapComparer = SnapComparer.Instance;
                List<GONetParticipant_ComponentsWithAutoSyncMembers> loadedSnapsFromAssetsFolder;
                List<GONetParticipant_ComponentsWithAutoSyncMembers> loadedSnapsFromScenes;
                List<GONetParticipant_ComponentsWithAutoSyncMembers> snapsFromProjectAssetResourcesFolders = new();
                List<GONetParticipant_ComponentsWithAutoSyncMembers> buildingAllSnapsMasterList = new();
                
                List<GONetParticipant> gnpsInProjectResources = default;

                { // the folling replaces this: UpdateAssetsSnaps();
                    gnpsInProjectResources = GatherGONetParticipantsInAllResourcesFolders();

                    // Also scan for addressable GONetParticipant prefabs during build
                    GONetSpawnSupport_DesignTime.EnsureDesignTimeLocationsCurrent_AddressablesOnly();

                    //Get the updated asset folder snaps and ensure guids set on prefabs
                    foreach (GONetParticipant gonetParticipantPrefab in gnpsInProjectResources)
                    {
                        if (gonetParticipantPrefab != null) // it would be null during some GONet generation flows herein when moving locations...but eventually it will be A-OK...avoid/skip for now
                        {
                            GONetParticipant_ComponentsWithAutoSyncMembers updatedSnapFromAssets = new(gonetParticipantPrefab);
                            updatedSnapFromAssets.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            snapsFromProjectAssetResourcesFolders.Add(updatedSnapFromAssets);
                            EnsurePrefabGuidSet(gonetParticipantPrefab);
                        }
                    }

                    //SnapComparer snapComparer = SnapComparer.Instance;
                    //Create the allUniqueSnaps list in order to update codegen ids later
                    List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnaps = LoadAllSnapsFromPersistenceFile(GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
                    foreach (var snapFromAssetFolder in snapsFromProjectAssetResourcesFolders.OrderBy(x => x.SingleMemberCount))
                    {
                        bool found = false;
                        foreach (var uniqueSnap in allUniqueSnaps)
                        {
                            if (snapComparer.Equals(uniqueSnap, snapFromAssetFolder))
                            {
                                snapFromAssetFolder.codeGenerationId = uniqueSnap.codeGenerationId;
                                //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                                ApplyDesignTimeMetadataToGnp(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            snapFromAssetFolder.codeGenerationId = GetNewCodeGenerationId(allUniqueSnaps);
                            //In order to not only save the new snap codegen id but also within its related GNP we need to perform this step too
                            ApplyDesignTimeMetadataToGnp(snapFromAssetFolder.gonetParticipant, snapFromAssetFolder.codeGenerationId);
                            allUniqueSnaps.Add(snapFromAssetFolder);
                        }
                    }

                    //if (howDeepIsYourSaveStack <= 1) // gotta be careful we do not get into an endless cycle as the method we are in now is called when scene saved.
                    {
                        // persist ALL....
                        foreach (var dtm in GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata())
                        {
                            GONetSpawnSupport_DesignTime.EnsureExistsInPersistence_WithTheseValues(dtm);
                        }
                        GONetLog.Debug($"~~~~~~~~~~~~~~~~GEEPs PROJECT howDeepIsYourSaveStack: {howDeepIsYourSaveStack}, DTM.Count: {GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata().Count()}, all locations: {string.Join("\n", GONetSpawnSupport_Runtime.GetAllDesignTimeMetadata().Select(x => x.Location))}");
                    }

                    SaveUniqueSnapsToPersistenceFile(snapsFromProjectAssetResourcesFolders, ASSET_FOLDER_SNAPS_FILE);
                }

                {//Load all unique snaps (From both the in scenes file and the in assets folder file) and clear out gen ids
                    loadedSnapsFromScenes = LoadAllSnapsFromPersistenceFile(GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
                    foreach (var loadedSnapFromScene in loadedSnapsFromScenes)
                    {
                        if (!buildingAllSnapsMasterList.Contains(loadedSnapFromScene, snapComparer))
                        {
                            loadedSnapFromScene.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            buildingAllSnapsMasterList.Add(loadedSnapFromScene);
                        }
                    }

                    loadedSnapsFromAssetsFolder = LoadAllSnapsFromPersistenceFile(ASSET_FOLDER_SNAPS_FILE);
                    foreach (var loadedSnapFromAssetsFolder in loadedSnapsFromAssetsFolder)
                    {
                        if (!buildingAllSnapsMasterList.Contains(loadedSnapFromAssetsFolder, snapComparer))
                        {
                            loadedSnapFromAssetsFolder.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            buildingAllSnapsMasterList.Add(loadedSnapFromAssetsFolder);
                        }
                    }
                }

                CreateAllPossibleUniqueSnapsAndGNPsFromResources(
                    gnpsInProjectResources,
                    out List<SnapGnpAssignment> createdInProjectResourcesAssetsUniqueAssignments);

                //Get all the possible unique snaps that are within any scene inside the build.
                CreateAllPossibleUniqueSnapsAndGNPsFromLoopingBuildScenes(
                    out List<GONetParticipant_ComponentsWithAutoSyncMembers> createdInSceneUniqueSnaps,
                    out List<SnapGnpAssignment> createdInSceneUniqueAssignments);

#if ADDRESSABLES_AVAILABLE
                //Also get snaps from Addressables scenes
                CreateAllPossibleUniqueSnapsAndGNPsFromAddressablesScenes(
                    ref createdInSceneUniqueSnaps,
                    ref createdInSceneUniqueAssignments);
#endif

                {//Apply changes (Additions, deletes and/or modifications) to both the unique snaps list and to the corresponding in scene game objects
                    HashSet<GONetParticipant_ComponentsWithAutoSyncMembers> sceneSnapsToDelete = new();
                    foreach (var uniqueSnap in buildingAllSnapsMasterList)
                    {
                        if (!createdInSceneUniqueSnaps.Contains(uniqueSnap, snapComparer) &&
                            !loadedSnapsFromAssetsFolder.Contains(uniqueSnap, snapComparer))
                        {
                            sceneSnapsToDelete.Add(uniqueSnap);
                        }
                    }

                    foreach (var snapToDelete in sceneSnapsToDelete)
                    {
                        buildingAllSnapsMasterList.Remove(snapToDelete);

                        if (loadedSnapsFromScenes.Contains(snapToDelete, snapComparer))
                        {
                            loadedSnapsFromScenes.Remove(snapToDelete);
                        }
                    }

                    foreach (var possibleUniqueSnap in createdInSceneUniqueSnaps)
                    {
                        //If this snap is new, generate a new code gen id for it.
                        if (!buildingAllSnapsMasterList.Contains(possibleUniqueSnap, snapComparer))
                        {
                            possibleUniqueSnap.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            buildingAllSnapsMasterList.Add(possibleUniqueSnap);
                            loadedSnapsFromScenes.Add(possibleUniqueSnap);
                        }
                    }

                    foreach (var possibleUniqueSnap in snapsFromProjectAssetResourcesFolders)
                    {
                        //If this snap is new, generate a new code gen id for it.
                        if (!buildingAllSnapsMasterList.Contains(possibleUniqueSnap, snapComparer))
                        {
                            possibleUniqueSnap.codeGenerationId = GONetParticipant.CodeGenerationId_Unset;
                            buildingAllSnapsMasterList.Add(possibleUniqueSnap);
                            loadedSnapsFromScenes.Add(possibleUniqueSnap);
                        }
                    }

                    //Set all unset codegeneration id
                    foreach (var uniqueSnap in buildingAllSnapsMasterList.OrderBy(x => x.SingleMemberCount))
                    {
                        // with current state of code, this should always be unset and will get assigned new
                        uniqueSnap.codeGenerationId = GetNewCodeGenerationId(buildingAllSnapsMasterList);

                        foreach (var inSceneAssignmentMatch in
                            createdInSceneUniqueAssignments.Where(x => snapComparer.Equals(x.assignedSnap, uniqueSnap)))
                        {
                            inSceneAssignmentMatch.assignedSnap.codeGenerationId = uniqueSnap.codeGenerationId;
                        }
                        foreach (var inProjectResourcesAssetsAssignmentMatch in
                            createdInProjectResourcesAssetsUniqueAssignments.Where(x => snapComparer.Equals(x.assignedSnap, uniqueSnap)))
                        {
                            inProjectResourcesAssetsAssignmentMatch.assignedSnap.codeGenerationId = uniqueSnap.codeGenerationId;
                        }
                        
                    }

                    //Apply changes not only to snaps but also to their corresponding GNPs
                    foreach (SnapGnpAssignment inSceneSnapGnpAssignment in createdInSceneUniqueAssignments)
                    {
                        GONetParticipant_ComponentsWithAutoSyncMembers actualPersistedSnap = inSceneSnapGnpAssignment.assignedSnap;
                        GONetParticipant inSceneGnp = inSceneSnapGnpAssignment.gnp;
                        GONetLog.Debug($"uniqueSnap.hash: {actualPersistedSnap.GetHashCode()}, SCENE gnp.genId: {inSceneGnp.CodeGenerationId}, snap.genId: {actualPersistedSnap.codeGenerationId}");
                        DesignTimeMetadata dtm =
                            ApplyDesignTimeMetadataToGnp(inSceneGnp, actualPersistedSnap.codeGenerationId, inSceneSnapGnpAssignment.fullProjectOrScenePath);
                        GONetSpawnSupport_DesignTime.EnsureExistsInPersistence_WithTheseValues(dtm);
                    }
                    foreach (SnapGnpAssignment inProjectResourcesAssetsAssignmentMatch in createdInProjectResourcesAssetsUniqueAssignments)
                    {
                        GONetParticipant_ComponentsWithAutoSyncMembers actualPersistedSnap = inProjectResourcesAssetsAssignmentMatch.assignedSnap;
                        GONetParticipant inProjectGnp = inProjectResourcesAssetsAssignmentMatch.gnp;
                        GONetLog.Debug($"uniqueSnap.hash: {actualPersistedSnap.GetHashCode()}, PROJECT gnp.genId: {inProjectGnp.CodeGenerationId}, snap.genId: {actualPersistedSnap.codeGenerationId}");
                        DesignTimeMetadata dtm =
                            ApplyDesignTimeMetadataToGnp(
                                inProjectGnp, 
                                actualPersistedSnap.codeGenerationId, 
                                inProjectResourcesAssetsAssignmentMatch.fullProjectOrScenePath,
                                inProjectGnp.UnityGuid);
                        GONetSpawnSupport_DesignTime.EnsureExistsInPersistence_WithTheseValues(dtm);
                    }

#if ADDRESSABLES_AVAILABLE
                    // Save and close Addressables scenes after CodeGenerationIds have been applied
                    SaveAndCloseAddressablesScenes(initialActiveScene);
#endif
                }

                {//Go back to the initial scene when this process started
                    if (initialActiveScene.IsValid())
                    {
                        EditorSceneManager.SetActiveScene(initialActiveScene);
                    }
                    else
                    {
                        GONetLog.Error("Not switching back to the scene that the user was in when this process started. The current scene is not valid.");
                    }
                }

                GONetLog.Debug($"dreetsi enum");
                GenerateSyncEventEnum(buildingAllSnapsMasterList);

                //Save the updated unique snaps in their corresponding binary files
                SaveUniqueSnapsToPersistenceFile(loadedSnapsFromScenes, GENERATED_IN_SCENE_UNIQUE_SNAPS_FILE_PATH);
                SaveUniqueSnapsToPersistenceFile(buildingAllSnapsMasterList, GENERATED_ALL_UNIQUE_SNAPS_FILE_PATH);

                //UpdateAssetsSnaps(); // Doing this a second time to ensure all the assigned 

                if (File.Exists(GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN))
                {
                    File.Delete(GENERATION_FILE_PATH + TEST_FILENAME_TO_FORCE_GEN);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                GONetSpawnSupport_Runtime.IsNewDtmForced = wasNewDtmForced;
            }
            GONetLog.Debug($"~~~~~~~~~~~~GEEPs end ( no more updates to DTM.json! )");
        }

        /// <summary>
        /// Do all proper unity serialization stuff or else a change will NOT stick/save/persist.
        /// </summary>
        private static void EnsurePrefabGuidSet(GONetParticipant gonetParticipantPrefab)
        {
            string goName = gonetParticipantPrefab.gameObject.name; // IMPORTANT: after a call to serializedObject.ApplyModifiedProperties(), gonetParticipant is unity "null" and this line MUst come before that!

            /*
            SerializedObject serializedObject = new SerializedObject(gonetParticipant); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
            SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.DesignTimeLocation));
            serializedObject.Update();
            serializedProperty.stringValue = currentLocation; // set it this way or else it will NOT work with prefabs!
            gonetParticipant.DesignTimeLocation = currentLocation; // doubly sure
            serializedObject.ApplyModifiedProperties();
            */

            string unityGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(gonetParticipantPrefab));

            {
                SerializedObject serializedObject = new SerializedObject(gonetParticipantPrefab); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
                SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.UnityGuid));
                serializedObject.Update();
                serializedProperty.stringValue = unityGuid; // set it this way or else it will NOT work with prefabs!
                gonetParticipantPrefab.UnityGuid = unityGuid;
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Using brute force and loading all prefabs in Resources subfolders even if they don't have GNP.
        /// 
        /// Consider Using Asset Labels
        ///If you prefer a more controlled approach, you can label prefabs containing GONetParticipant and search by label.
        ///Steps to Implement Labeling
        ///Label Existing Prefabs:
        ///Run a script that iterates over all prefabs under Assets, checks for GONetParticipant, and adds a label.
        ///Automatically Label New Prefabs:
        ///Implement an AssetPostprocessor that labels prefabs when they are imported or modified.
        ///Search by Label:
        ///Modify your search query to use the label, e.g., l:GONetParticipant t:Prefab.
        /// </summary>
        /// <returns></returns>
        internal static List<GONetParticipant> GatherGONetParticipantsInAllResourcesFolders()
        {
            List<GONetParticipant> gonetParticipantsInPrefabs = new List<GONetParticipant>();

            // 1. Find all 'Resources' folders under 'Assets'
            string[] resourcesFolderGUIDs = AssetDatabase.FindAssets("Resources t:folder", new[] { "Assets" });

            List<string> resourcesFolders = new List<string>();

            foreach (string guid in resourcesFolderGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // Ensure the folder name is exactly 'Resources'
                if (Path.GetFileName(path) == "Resources")
                {
                    resourcesFolders.Add(path);
                }
            }

            foreach (string resourcesFolder in resourcesFolders)
            {
                // Find all prefab GUIDs under the Resources folder
                string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { resourcesFolder });

                foreach (string guid in prefabGUIDs)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    if (prefab != null)
                    {
                        // Get all GONetParticipant components in the prefab's hierarchy
                        GONetParticipant[] gnps = prefab.GetComponentsInChildren<GONetParticipant>(includeInactive: true);

                        if (gnps.Length > 0)
                        {
                            gonetParticipantsInPrefabs.AddRange(gnps);
                            Debug.Log($"Found {gnps.Length} GONetParticipant(s) in Resources prefab: {assetPath}");
                        }
                    }
                }
            }

#if ADDRESSABLES_AVAILABLE
            // 2. Find all addressable GONetParticipant prefabs
            var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings != null)
            {
                foreach (var group in addressableSettings.groups)
                {
                    if (group == null) continue;

                    foreach (var entry in group.entries)
                    {
                        if (entry == null || string.IsNullOrEmpty(entry.address)) continue;

                        // Load the asset to check if it contains a GONetParticipant
                        string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                        if (string.IsNullOrEmpty(assetPath)) continue;

                        // Check if it's a prefab file
                        if (assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            if (prefab != null)
                            {
                                // Get all GONetParticipant components in the prefab's hierarchy
                                GONetParticipant[] gnps = prefab.GetComponentsInChildren<GONetParticipant>(includeInactive: true);

                                if (gnps.Length > 0)
                                {
                                    gonetParticipantsInPrefabs.AddRange(gnps);
                                    string guid = AssetDatabase.AssetPathToGUID(assetPath);
                                    Debug.Log($"Found {gnps.Length} GONetParticipant(s) in addressable prefab: {assetPath} (address: {entry.address}) (guid: {guid}) (entry.guid: {entry.guid})");
                                }
                            }
                        }
                        // Check if it's a folder - scan for prefabs inside
                        else if (AssetDatabase.IsValidFolder(assetPath))
                        {
                            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });

                            foreach (string prefabGuid in prefabGuids)
                            {
                                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                                if (prefab != null)
                                {
                                    // Get all GONetParticipant components in the prefab's hierarchy
                                    GONetParticipant[] gnps = prefab.GetComponentsInChildren<GONetParticipant>(includeInactive: true);

                                    if (gnps.Length > 0)
                                    {
                                        string prefabFileName = System.IO.Path.GetFileName(prefabPath);
                                        string addressableKey = string.Concat(entry.address, "/", prefabFileName);
                                        gonetParticipantsInPrefabs.AddRange(gnps);
                                        Debug.Log($"Found {gnps.Length} GONetParticipant(s) in addressable folder prefab: {prefabPath} (address: {addressableKey})");
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif

            return gonetParticipantsInPrefabs;
        }

        class SnapGnpAssignment
        {
            public GONetParticipant gnp;
            public string fullProjectOrScenePath;
            public GONetParticipant_ComponentsWithAutoSyncMembers assignedSnap;
        }

        private static void CreateAllPossibleUniqueSnapsAndGNPsFromResources(
            List<GONetParticipant> gnpsInProjectResources,
            out List<SnapGnpAssignment> createdInProjectResourcesAssetsUniqueAssignments)
        {
            createdInProjectResourcesAssetsUniqueAssignments = new();

            foreach (GONetParticipant gnp in gnpsInProjectResources)
            {
                string projectPath = AssetDatabase.GetAssetPath(gnp);
                bool isProjectAsset = !string.IsNullOrWhiteSpace(projectPath);
                if (isProjectAsset)
                {
                    // Use resources:// prefix for assets in Resources folders, project:// for others
                    string currentLocation;
                    if (projectPath.Contains("/Resources/"))
                    {
                        currentLocation = string.Concat(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX, projectPath);
                    }
                    else
                    {
                        currentLocation = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                    }

#if ADDRESSABLES_AVAILABLE
                    // Check if this is an addressable asset and use the appropriate prefix
                    var addressableSettings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                    bool isAddressable = false;
                    string addressableKey = null;

                    if (addressableSettings != null)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(projectPath);
                        var entry = addressableSettings.FindAssetEntry(guid);
                        if (entry != null && !string.IsNullOrEmpty(entry.address))
                        {
                            isAddressable = true;
                            addressableKey = entry.address;
                            currentLocation = string.Concat(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX, entry.address);
                        }
                        else
                        {
                            // Check if this prefab is in an addressable folder
                            foreach (var group in addressableSettings.groups)
                            {
                                if (group == null) continue;

                                foreach (var groupEntry in group.entries)
                                {
                                    if (groupEntry == null || string.IsNullOrEmpty(groupEntry.address)) continue;

                                    string entryPath = AssetDatabase.GUIDToAssetPath(groupEntry.guid);
                                    if (AssetDatabase.IsValidFolder(entryPath) && projectPath.StartsWith(entryPath + "/"))
                                    {
                                        // This prefab is inside an addressable folder
                                        string prefabFileName = System.IO.Path.GetFileName(projectPath);
                                        addressableKey = string.Concat(groupEntry.address, "/", prefabFileName);
                                        isAddressable = true;
                                        currentLocation = string.Concat(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX, addressableKey);
                                        break;
                                    }
                                }
                                if (isAddressable) break;
                            }
                        }
                    }
#endif

                    GONetParticipant_ComponentsWithAutoSyncMembers assignedSnap = new(gnp);
                    // TODO? possibleInSceneUniqueSnaps.Add(assignedSnap);
                    createdInProjectResourcesAssetsUniqueAssignments.Add(new()
                    {
                        fullProjectOrScenePath = currentLocation,
                        assignedSnap = assignedSnap,
                        gnp = gnp,
                    });

#if ADDRESSABLES_AVAILABLE
                    // Update the GONetParticipant's design time metadata if it's addressable
                    if (isAddressable && addressableKey != null)
                    {
                        var designTimeMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(gnp);
                        designTimeMetadata.UnityGuid = AssetDatabase.AssetPathToGUID(projectPath);
                        GONetLog.Debug($"CreateAllPossibleUniqueSnapsAndGNPsFromResources: Set addressable metadata for '{gnp.name}' with key: '{addressableKey}'");
                    }
#endif
                }
                else
                {
                    GONetLog.Error("gnp has no project path!?!? gnp.go.name: " + gnp.gameObject.name);
                }
            }
        }

        private static void CreateAllPossibleUniqueSnapsAndGNPsFromLoopingBuildScenes(
            out List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleInSceneUniqueSnaps, 
            out List<SnapGnpAssignment> allSceneSnapGnpAssignments)
        {
            possibleInSceneUniqueSnaps = new();
            allSceneSnapGnpAssignments = new();

            int scenesCount = EditorSceneManager.sceneCountInBuildSettings;

            // Record the initially active scene
            Scene initialActiveScene = EditorSceneManager.GetActiveScene();

            for (int i = 0; i < scenesCount; ++i)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                Scene scene = EditorSceneManager.GetSceneByPath(scenePath);

                Scene openScene;

                if (scene.isLoaded)
                {
                    // The scene is already open; use the existing scene
                    openScene = scene;
                }
                else
                {
                    // Open the scene additively without unloading other scenes
                    openScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }

                if (openScene.IsValid())
                {
                    // Process GONetParticipants
                    List<GONetParticipant> gnps = new List<GONetParticipant>();
                    GetGNPsWithinScene(openScene, out gnps);

                    foreach (GONetParticipant gnp in gnps)
                    {
                        // Process each GNP
                        string gnpFullScenePath = DesignTimeMetadata.GetFullUniquePathInScene(gnp);
                        GONetParticipant_ComponentsWithAutoSyncMembers assignedSnap = new(gnp);
                        possibleInSceneUniqueSnaps.Add(assignedSnap);
                        allSceneSnapGnpAssignments.Add(new()
                        {
                            fullProjectOrScenePath = gnpFullScenePath,
                            assignedSnap = assignedSnap,
                            gnp = gnp,
                        });
                    }
                }
                else
                {
                    GONetLog.Warning("Invalid scene: " + scenePath);
                }

                // Unload the scene to free up memory, unless it was already open or is the initial active scene
                if (!scene.isLoaded && openScene != initialActiveScene)
                {
                    EditorSceneManager.CloseScene(openScene, true);
                }
            }

            // Ensure the initial scene is the active scene
            EditorSceneManager.SetActiveScene(initialActiveScene);
        }

#if ADDRESSABLES_AVAILABLE
        private static void CreateAllPossibleUniqueSnapsAndGNPsFromAddressablesScenes(
            ref List<GONetParticipant_ComponentsWithAutoSyncMembers> possibleInSceneUniqueSnaps,
            ref List<SnapGnpAssignment> allSceneSnapGnpAssignments)
        {
            try
            {
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    GONetLog.Debug("[GONet] No Addressables settings found - skipping Addressables scene scan");
                    return;
                }

                // Collect all scene asset entries from Addressables
                List<string> addressableScenesPath = new List<string>();
                foreach (var group in settings.groups)
                {
                    if (group == null) continue;

                    foreach (var entry in group.entries)
                    {
                        if (entry == null) continue;

                        // Check if this is a scene asset
                        string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                        if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".unity"))
                        {
                            addressableScenesPath.Add(assetPath);
                        }
                    }
                }

                if (addressableScenesPath.Count == 0)
                {
                    GONetLog.Debug("[GONet] No scenes found in Addressables");
                    return;
                }

                GONetLog.Debug($"[GONet] Found {addressableScenesPath.Count} scene(s) in Addressables: {string.Join(", ", addressableScenesPath)}");

                // Record the initially active scene
                Scene initialActiveScene = EditorSceneManager.GetActiveScene();

                // Load each Addressables scene additively, scan it, then unload it
                foreach (string scenePath in addressableScenesPath)
                {
                    try
                    {
                        GONetLog.Debug($"[GONet] Loading Addressables scene for scanning: {scenePath}");

                        Scene scene = EditorSceneManager.GetSceneByPath(scenePath);
                        Scene openScene;

                        if (scene.isLoaded)
                        {
                            // The scene is already open; use the existing scene
                            openScene = scene;
                        }
                        else
                        {
                            // Open the scene additively without unloading other scenes
                            openScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                        }

                        if (openScene.IsValid())
                        {
                            // Process GONetParticipants
                            List<GONetParticipant> gnps = new List<GONetParticipant>();
                            GetGNPsWithinScene(openScene, out gnps);

                            foreach (GONetParticipant gnp in gnps)
                            {
                                // Process each GNP
                                string gnpFullScenePath = DesignTimeMetadata.GetFullUniquePathInScene(gnp);
                                GONetParticipant_ComponentsWithAutoSyncMembers assignedSnap = new(gnp);
                                possibleInSceneUniqueSnaps.Add(assignedSnap);
                                allSceneSnapGnpAssignments.Add(new()
                                {
                                    fullProjectOrScenePath = gnpFullScenePath,
                                    assignedSnap = assignedSnap,
                                    gnp = gnp,
                                });
                                GONetLog.Debug($"[GONet] Found GNP in Addressables scene '{openScene.name}': {gnp.gameObject.name}");
                            }
                        }
                        else
                        {
                            GONetLog.Warning($"[GONet] Invalid Addressables scene: {scenePath}");
                        }

                        // DON'T close the scene yet - we need to keep it loaded so that
                        // CodeGenerationIds can be applied to the GONetParticipants later
                        // The scenes will need to be saved after IDs are applied
                    }
                    catch (System.Exception ex)
                    {
                        GONetLog.Warning($"[GONet] Failed to load/scan Addressables scene '{scenePath}': {ex.Message}");
                    }
                }

                // Ensure the initial scene is the active scene
                EditorSceneManager.SetActiveScene(initialActiveScene);

                GONetLog.Debug($"[GONet] Finished scanning Addressables scenes");
            }
            catch (System.Exception ex)
            {
                GONetLog.Error($"[GONet] Error while scanning Addressables scenes: {ex.Message}");
            }
        }

        private static void SaveAndCloseAddressablesScenes(Scene initialActiveScene)
        {
            try
            {
                var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                {
                    return;
                }

                // Find all Addressables scenes
                HashSet<string> addressableScenePaths = new HashSet<string>();
                foreach (var group in settings.groups)
                {
                    if (group == null) continue;

                    foreach (var entry in group.entries)
                    {
                        if (entry == null) continue;

                        string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                        if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".unity"))
                        {
                            addressableScenePaths.Add(assetPath);
                        }
                    }
                }

                // Save and close each loaded Addressables scene
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    // Check if this is an Addressables scene and not the initial active scene
                    if (addressableScenePaths.Contains(scene.path) && scene != initialActiveScene)
                    {
                        if (scene.isDirty)
                        {
                            GONetLog.Debug($"[GONet] Saving Addressables scene: {scene.path}");
                            EditorSceneManager.SaveScene(scene);
                        }

                        GONetLog.Debug($"[GONet] Closing Addressables scene: {scene.path}");
                        EditorSceneManager.CloseScene(scene, false); // Don't remove unsaved changes since we just saved
                    }
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Error($"[GONet] Error while saving/closing Addressables scenes: {ex.Message}");
            }
        }
#endif

        private static void GetGNPsWithinScene(Scene scene, out List<GONetParticipant> gonetParticipantsInOpenScenes)
        {
            gonetParticipantsInOpenScenes = new List<GONetParticipant>();
            GameObject[] rootGOs = scene.GetRootGameObjects();
            for (int iRootGO = 0; iRootGO < rootGOs.Length; ++iRootGO)
            {
                // NOTE: including inactive GNPs as well since they may be enabled at some point
                GONetParticipant[] gonetParticipantsInOpenScene = rootGOs[iRootGO].GetComponentsInChildren<GONetParticipant>(includeInactive: true);
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

            //allSnaps.ForEach(x => GONetLog.Debug("SAVING to persistence gnp-(loc:" + x.gonetParticipant.DesignTimeLocation + ", genId:"+x.gonetParticipant.CodeGenerationId+"), snap.genId: " + x.codeGenerationId + " snap.ComponentMemberNames_By_ComponentTypeFullName.Length: " + x.ComponentMemberNames_By_ComponentTypeFullName.Length));

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

        #region RPC support
        private static void GenerateAllRpcCompanions()
        {
            // Find all MonoBehaviours with RPC methods in the project
            var rpcComponentTypes = new Dictionary<Type, List<MethodInfo>>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                if (TypeUtils.IsEditorOnlyAssembly(assembly)) continue;
                var types = assembly.GetLoadableTypes()
                    .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract)
                    .OrderBy(t => t.FullName);
                foreach (var type in types)
                {
                    var rpcMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(m => m.GetCustomAttribute<GONetRpcAttribute>() != null)
                        .OrderBy(m => m.Name)
                        .ToList();
                    if (rpcMethods.Any())
                    {
                        rpcComponentTypes[type] = rpcMethods;
                    }
                }
            }

            // Generate a companion class for each component type with RPCs
            foreach (var kvp in rpcComponentTypes)
            {
                Type componentType = kvp.Key;
                List<MethodInfo> rpcMethods = kvp.Value;

                // Validate at generation time
                var errors = new List<string>();
                if (!typeof(GONetParticipantCompanionBehaviour).IsAssignableFrom(componentType))
                {
                    errors.Add($"ERROR: Class '{componentType.FullName}' has RPC methods but doesn't extend GONetParticipantCompanionBehaviour");
                    errors.Add($"  FIX: public class {componentType.FullName} : GONetParticipantCompanionBehaviour");
                }

                foreach (var method in rpcMethods)
                {
                    ValidateRpcMethod(componentType, method, errors, false, false); // Context validation happens at runtime
                }

                if (errors.Any())
                {
                    string errorMsg = $"\n====== GONet RPC Validation Failed ======\n{string.Join("\n", errors)}\n=========================================";
                    Debug.LogError(errorMsg);
                    continue; // Skip generation for this type
                }

                // Special handling for base class vs derived classes
                if (componentType == typeof(GONetParticipantCompanionBehaviour))
                {
                    // For the base class itself, only generate for its own methods
                    GenerateRpcCode(componentType, rpcMethods.Where(m => m.DeclaringType == componentType).ToList());
                }
                else
                {
                    // For derived classes, pass ALL methods (including inherited base methods)
                    // The GenerateRpcCode method will separate them internally
                    GenerateRpcCode(componentType, rpcMethods);
                }
            }
        }

        private static void GenerateRpcCode(Type componentType, IEnumerable<MethodInfo> rpcMethods)
        {
            var sb = new StringBuilder();
            string className = componentType.Name;
            string generatedFileName = $"{className}_RPC_Generated";

            // Separate base class and derived class methods
            var baseRpcMethods = rpcMethods.Where(m => m.DeclaringType == typeof(GONetParticipantCompanionBehaviour)).ToList();
            var derivedRpcMethods = rpcMethods.Where(m => m.DeclaringType == componentType).ToList();

            // File header
            sb.AppendLine("// GENERATED BY GONet - DO NOT EDIT");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using GONet;");
            sb.AppendLine("using GONet.Utils;");
            sb.AppendLine("using MemoryPack;");
            sb.AppendLine();
            sb.AppendLine($"namespace {(string.IsNullOrWhiteSpace(componentType.Namespace) ? "GONet" : componentType.Namespace)}");
            sb.AppendLine("{");

            // Generate RPC ID constants (includes both base and derived)
            GenerateRpcIdConstants(sb, className, baseRpcMethods, derivedRpcMethods);

            // Add initialization class
            GenerateInitializationClass(sb, className);

            // Generate Dispatcher class
            sb.AppendLine($"    internal class {className}_RpcDispatcher : IRpcDispatcher");
            sb.AppendLine("    {");
            sb.AppendLine("        private static bool isInitialized = false;");
            sb.AppendLine();

            // Generate const strings for method names (only derived class methods)
            foreach (var method in derivedRpcMethods)
            {
                sb.AppendLine($"        private const string {method.Name.ToUpper()} = nameof({className}.{method.Name});");
            }
            sb.AppendLine();

            // Generate property accessor delegates for TargetRpcs (only derived)
            GenerateTargetPropertyAccessors(sb, componentType, derivedRpcMethods);

            // Generate validation delegates (only derived)
            GenerateValidationDelegates(sb, componentType, derivedRpcMethods);

            GenerateMetadataDictionaryWithBase(sb, componentType, baseRpcMethods, derivedRpcMethods);

            // Generate dispatcher methods (only for derived)
            GenerateDispatcherMethods(sb, componentType, derivedRpcMethods);

            // Static constructor
            GenerateStaticConstructor(sb, className, componentType, baseRpcMethods, derivedRpcMethods);

            // Force initialization method
            sb.AppendLine();
            sb.AppendLine("        internal static void EnsureInitialized()");
            sb.AppendLine("        {");
            sb.AppendLine("            _ = Metadata;");
            sb.AppendLine("        }");

            // RPC handler registration (only for derived methods)
            sb.AppendLine();
            sb.AppendLine("        private static void RegisterRpcHandlers()");
            sb.AppendLine("        {");
            sb.AppendLine("            var eventBus = GONetMain.EventBus;");

            foreach (var method in derivedRpcMethods)
            {
                GenerateRpcHandlerRegistration(sb, method, className);
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Generate parameter data structures (only for derived)
            var processedTypes = new HashSet<string>();
            foreach (var method in derivedRpcMethods.Where(m => m.GetParameters().Any()))
            {
                GenerateParameterDataStruct(sb, method, processedTypes);
            }

            sb.AppendLine("}");

            // Write to file
            string rpcsFolderPath = $"{GENERATED_FILE_PATH}RPCs/";
            if (!Directory.Exists(rpcsFolderPath))
            {
                Directory.CreateDirectory(rpcsFolderPath);
            }

            string filePath = $"{rpcsFolderPath}{generatedFileName}.cs";
            File.WriteAllText(filePath, sb.ToString());
        }

        private static void GenerateRpcIdConstants(StringBuilder sb, string className, List<MethodInfo> baseRpcMethods, List<MethodInfo> derivedRpcMethods)
        {
            sb.AppendLine($"    internal static class {className}_RpcIds");
            sb.AppendLine("    {");

            // Include base class RPC IDs if this is a derived class
            if (baseRpcMethods.Any())
            {
                sb.AppendLine("        // Base class RPC IDs");
                foreach (var method in baseRpcMethods)
                {
                    uint rpcId = GONetEventBus.GetRpcId(typeof(GONetParticipantCompanionBehaviour), method.Name);
                    sb.AppendLine($"        internal const uint {method.Name}_RpcId = 0x{rpcId:X8};");
                }
                if (derivedRpcMethods.Any())
                {
                    sb.AppendLine();
                }
            }

            // Derived class RPC IDs
            if (derivedRpcMethods.Any())
            {
                sb.AppendLine("        // Derived class RPC IDs");
                foreach (var method in derivedRpcMethods)
                {
                    uint rpcId = GONetEventBus.GetRpcId(method.DeclaringType, method.Name);
                    sb.AppendLine($"        internal const uint {method.Name}_RpcId = 0x{rpcId:X8};");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateInitializationClass(StringBuilder sb, string className)
        {
            sb.AppendLine($"    internal static class {className}_RpcInitializer");
            sb.AppendLine("    {");
            sb.AppendLine("        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]");
            sb.AppendLine("        private static void Initialize()");
            sb.AppendLine("        {");
            sb.AppendLine($"            {className}_RpcDispatcher.EnsureInitialized();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void GenerateMetadataDictionaryWithBase(StringBuilder sb, Type componentType, List<MethodInfo> baseRpcMethods, List<MethodInfo> derivedRpcMethods)
        {
            sb.AppendLine("        public static readonly Dictionary<string, RpcMetadata> Metadata = new Dictionary<string, RpcMetadata>");
            sb.AppendLine("        {");

            // Add base class RPCs first
            if (baseRpcMethods.Any())
            {
                sb.AppendLine("            // Base class RPCs");
                foreach (var method in baseRpcMethods)
                {
                    GenerateMetadataEntry(sb, method, typeof(GONetParticipantCompanionBehaviour));
                }
            }

            // Add derived class RPCs
            if (derivedRpcMethods.Any())
            {
                sb.AppendLine("            // Derived class RPCs");
                foreach (var method in derivedRpcMethods)
                {
                    GenerateMetadataEntry(sb, method, componentType);
                }
            }

            sb.AppendLine("        };");
            sb.AppendLine();
        }

        private static void GenerateMetadataEntry(StringBuilder sb, MethodInfo method, Type declaringType)
        {
            var attr = method.GetCustomAttribute<GONetRpcAttribute>();
            string rpcType = "RpcType.ServerRpc";
            string target = "RpcTarget.All";
            string targetPropertyName = "null";
            string isMultipleTargets = "false";
            string validationMethodName = "null";
            bool expectsDeliveryReport = false;

            // Determine RPC type and properties
            if (attr is ClientRpcAttribute)
            {
                rpcType = "RpcType.ClientRpc";
            }
            else if (attr is TargetRpcAttribute targetAttr)
            {
                rpcType = "RpcType.TargetRpc";
                target = $"RpcTarget.{targetAttr.Target}";
                if (!string.IsNullOrEmpty(targetAttr.TargetPropertyName))
                {
                    targetPropertyName = $"\"{targetAttr.TargetPropertyName}\"";
                    isMultipleTargets = targetAttr.IsMultipleTargets ? "true" : "false";
                }
                if (!string.IsNullOrEmpty(targetAttr.ValidationMethodName))
                {
                    validationMethodName = $"\"{targetAttr.ValidationMethodName}\"";
                }
            }

            // Check if expecting delivery report
            if (attr is TargetRpcAttribute)
            {
                if (method.ReturnType.IsGenericType &&
                    method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                    method.ReturnType.GetGenericArguments()[0] == typeof(RpcDeliveryReport))
                {
                    expectsDeliveryReport = true;
                }
            }

            // Use appropriate class name based on declaring type
            string methodClassName = declaringType == typeof(GONetParticipantCompanionBehaviour) ?
                "GONetParticipantCompanionBehaviour" : declaringType.FullName;

            sb.AppendLine($"            {{ nameof({methodClassName}.{method.Name}), new RpcMetadata {{ ");
            sb.AppendLine($"                Type = {rpcType}, ");
            sb.AppendLine($"                IsReliable = {attr.IsReliable.ToString().ToLower()}, ");
            sb.AppendLine($"                IsMineRequired = {attr.IsMineRequired.ToString().ToLower()}, ");
            sb.AppendLine($"                IsPersistent = {attr.IsPersistent.ToString().ToLower()}, ");
            sb.AppendLine($"                Target = {target}, ");
            sb.AppendLine($"                TargetPropertyName = {targetPropertyName},");
            sb.AppendLine($"                IsMultipleTargets = {isMultipleTargets},");
            sb.AppendLine($"                ValidationMethodName = {validationMethodName},");
            sb.AppendLine($"                ExpectsDeliveryReport = {expectsDeliveryReport.ToString().ToLower()}");
            sb.AppendLine("            }},");
        }

        private static void GenerateStaticConstructor(StringBuilder sb, string className, Type componentType,
    List<MethodInfo> baseRpcMethods, List<MethodInfo> derivedRpcMethods)
        {
            sb.AppendLine();
            sb.AppendLine($"        static {className}_RpcDispatcher()");
            sb.AppendLine("        {");
            sb.AppendLine("            if (!isInitialized)");
            sb.AppendLine("            {");
            sb.AppendLine("                isInitialized = true;");

            // Register RpcId mappings for base methods
            if (baseRpcMethods.Any())
            {
                foreach (var method in baseRpcMethods)
                {
                    uint rpcId = GONetEventBus.GetRpcId(typeof(GONetParticipantCompanionBehaviour), method.Name);
                    sb.AppendLine($"                GONetMain.EventBus.RegisterRpcIdMapping(0x{rpcId:X8}, nameof(GONetParticipantCompanionBehaviour.{method.Name}), typeof({className}));");
                }
            }

            // Register RpcId mappings for derived methods
            foreach (var method in derivedRpcMethods)
            {
                uint rpcId = GONetEventBus.GetRpcId(componentType, method.Name);
                sb.AppendLine($"                GONetMain.EventBus.RegisterRpcIdMapping(0x{rpcId:X8}, nameof({className}.{method.Name}), typeof({className}));");
            }

            sb.AppendLine($"                GONetMain.EventBus.RegisterRpcDispatcher(typeof({className}), new {className}_RpcDispatcher());");
            sb.AppendLine($"                GONetMain.EventBus.RegisterRpcMetadata(typeof({className}), Metadata);");

            // Only register if we actually generated these collections
            bool hasTargetRpcs = derivedRpcMethods.Any(m => m.GetCustomAttribute<TargetRpcAttribute>() != null);
            if (hasTargetRpcs)
            {
                sb.AppendLine("                if (TargetPropertyAccessors.Count > 0)");
                sb.AppendLine($"                    GONetMain.EventBus.RegisterTargetPropertyAccessors(typeof({className}), TargetPropertyAccessors);");
                sb.AppendLine("                if (MultiTargetPropertyAccessors.Count > 0)");
                sb.AppendLine($"                    GONetMain.EventBus.RegisterMultiTargetPropertyAccessors(typeof({className}), MultiTargetPropertyAccessors);");
            }

            // Only check for validators if we have TargetRpcs with validation methods
            bool hasValidation = derivedRpcMethods.Any(m =>
            {
                var attr = m.GetCustomAttribute<TargetRpcAttribute>();
                return attr != null && !string.IsNullOrEmpty(attr.ValidationMethodName);
            });

            if (hasValidation)
            {
                sb.AppendLine("                if (EnhancedValidators.Count > 0)");
                sb.AppendLine($"                    GONetMain.EventBus.RegisterEnhancedValidators(typeof({className}), EnhancedValidators, ValidatorParameterCounts);");
            }

            sb.AppendLine("                RegisterRpcHandlers();");
            sb.AppendLine($"                GONetLog.Debug(\"Initialized RPC dispatcher for {className}\");");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }

        private static void GenerateTargetPropertyAccessors(StringBuilder sb, Type componentType, IEnumerable<MethodInfo> rpcMethods)
        {
            // Single target accessors
            sb.AppendLine("        // Property accessor delegates for single targets");
            sb.AppendLine("        private static readonly Dictionary<string, Func<object, ushort>> TargetPropertyAccessors = ");
            sb.AppendLine("            new Dictionary<string, Func<object, ushort>>");
            sb.AppendLine("        {");

            foreach (var method in rpcMethods)
            {
                var targetAttr = method.GetCustomAttribute<TargetRpcAttribute>();
                if (targetAttr != null && !string.IsNullOrEmpty(targetAttr.TargetPropertyName) && !targetAttr.IsMultipleTargets)
                {
                    var property = componentType.GetProperty(targetAttr.TargetPropertyName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null && property.PropertyType == typeof(ushort))
                    {
                        sb.AppendLine($"            {{ nameof({componentType.FullName}.{method.Name}), ");
                        sb.AppendLine($"              (obj) => (({componentType.FullName})obj).{targetAttr.TargetPropertyName} }},");
                    }
                }
            }
            sb.AppendLine("        };");
            sb.AppendLine();

            // Multi-target accessors that fill buffer
            sb.AppendLine("        // Property accessor delegates for multiple targets (fills buffer, returns count)");
            sb.AppendLine("        private static readonly Dictionary<string, Func<object, ushort[], int>> MultiTargetPropertyAccessors = ");
            sb.AppendLine("            new Dictionary<string, Func<object, ushort[], int>>");
            sb.AppendLine("        {");

            foreach (var method in rpcMethods)
            {
                var targetAttr = method.GetCustomAttribute<TargetRpcAttribute>();
                if (targetAttr != null && !string.IsNullOrEmpty(targetAttr.TargetPropertyName) && targetAttr.IsMultipleTargets)
                {
                    var property = componentType.GetProperty(targetAttr.TargetPropertyName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null)
                    {
                        if (property.PropertyType == typeof(List<ushort>))
                        {
                            sb.AppendLine($"            {{ nameof({componentType.FullName}.{method.Name}), ");
                            sb.AppendLine($"              (obj, buffer) => ");
                            sb.AppendLine($"              {{");
                            sb.AppendLine($"                  var list = (({componentType.FullName})obj).{targetAttr.TargetPropertyName};");
                            sb.AppendLine($"                  if (list == null) return 0;");
                            sb.AppendLine($"                  int count = Math.Min(list.Count, buffer.Length);");
                            sb.AppendLine($"                  for (int i = 0; i < count; i++) buffer[i] = list[i];");
                            sb.AppendLine($"                  return count;");
                            sb.AppendLine($"              }} }},");
                        }
                        else if (property.PropertyType == typeof(ushort[]))
                        {
                            sb.AppendLine($"            {{ nameof({componentType.FullName}.{method.Name}), ");
                            sb.AppendLine($"              (obj, buffer) => ");
                            sb.AppendLine($"              {{");
                            sb.AppendLine($"                  var array = (({componentType.FullName})obj).{targetAttr.TargetPropertyName};");
                            sb.AppendLine($"                  if (array == null) return 0;");
                            sb.AppendLine($"                  int count = Math.Min(array.Length, buffer.Length);");
                            sb.AppendLine($"                  Array.Copy(array, buffer, count);");
                            sb.AppendLine($"                  return count;");
                            sb.AppendLine($"              }} }},");
                        }
                    }
                }
            }
            sb.AppendLine("        };");
            sb.AppendLine();
        }

        private static void GenerateValidationDelegates(StringBuilder sb, Type componentType, IEnumerable<MethodInfo> rpcMethods)
        {
            // First collect all validation info
            var validatorInfo = new List<(MethodInfo method, TargetRpcAttribute attr, int paramCount)>();
            foreach (var method in rpcMethods)
            {
                var targetAttr = method.GetCustomAttribute<TargetRpcAttribute>();
                if (targetAttr != null && !string.IsNullOrEmpty(targetAttr.ValidationMethodName))
                {
                    var validationMethod = componentType.GetMethod(targetAttr.ValidationMethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (validationMethod != null && HasMatchingValidationSignature(method, validationMethod))
                    {
                        var paramCount = method.GetParameters().Length;
                        validatorInfo.Add((method, targetAttr, paramCount));
                    }
                }
            }

            // Generate parameter count metadata dictionary
            sb.AppendLine("        // Parameter count metadata for validators");
            sb.AppendLine("        private static readonly Dictionary<string, int> ValidatorParameterCounts = ");
            sb.AppendLine("            new Dictionary<string, int>");
            sb.AppendLine("        {");

            foreach (var (method, targetAttr, paramCount) in validatorInfo)
            {
                sb.AppendLine($"            {{ nameof({componentType.Name}.{method.Name}), {paramCount} }},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();

            // Generate validation delegates dictionary
            sb.AppendLine("        // Validation delegates that return full validation results with parameter-specific signatures");
            sb.AppendLine("        private static readonly Dictionary<string, object> EnhancedValidators = ");
            sb.AppendLine("            new Dictionary<string, object>");
            sb.AppendLine("        {");

            foreach (var (method, targetAttr, paramCount) in validatorInfo)
            {
                var rpcParams = method.GetParameters();

                sb.AppendLine($"            {{ nameof({componentType.Name}.{method.Name}),");

                // Generate appropriate delegate based on parameter count
                switch (paramCount)
                {
                    case 0:
                        GenerateZeroParamValidator(sb, componentType, method, targetAttr);
                        break;
                    case 1:
                        GenerateOneParamValidator(sb, componentType, method, targetAttr, rpcParams[0]);
                        break;
                    case 2:
                        GenerateTwoParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 3:
                        GenerateThreeParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 4:
                        GenerateFourParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 5:
                        GenerateFiveParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 6:
                        GenerateSixParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 7:
                        GenerateSevenParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    case 8:
                        GenerateEightParamValidator(sb, componentType, method, targetAttr, rpcParams);
                        break;
                    default:
                        throw new InvalidOperationException($"Validation methods with {paramCount} parameters are not currently supported. Maximum is 8 parameters.");
                }

                sb.AppendLine("            },");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
        }

        private static bool HasMatchingValidationSignature(MethodInfo rpcMethod, MethodInfo validationMethod)
        {
            if (validationMethod.ReturnType != typeof(RpcValidationResult))
                return false;

            var rpcParams = rpcMethod.GetParameters();
            var valParams = validationMethod.GetParameters();

            if (rpcParams.Length != valParams.Length)
                return false;

            for (int i = 0; i < rpcParams.Length; i++)
            {
                if (!valParams[i].ParameterType.IsByRef)
                    return false;

                var valType = valParams[i].ParameterType.GetElementType();
                if (valType != rpcParams[i].ParameterType)
                    return false;
            }

            return true;
        }

        private static void GenerateZeroParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr)
        {
            sb.AppendLine($"              (Func<object, ushort, ushort[], int, RpcValidationResult>)((obj, source, targets, count) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}();");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateOneParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo param)
        {
            string dataTypeName = GetParameterDataStructName(method);
            string paramType = GetFriendlyTypeName(param.ParameterType);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");
            sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}(ref {param.Name});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");
            sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateTwoParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateThreeParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateFourParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateFiveParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateSixParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateSevenParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateEightParamValidator(StringBuilder sb, Type componentType, MethodInfo method, TargetRpcAttribute targetAttr, ParameterInfo[] parameters)
        {
            string dataTypeName = GetParameterDataStructName(method);

            sb.AppendLine($"              (Func<object, ushort, ushort[], int, byte[], RpcValidationResult>)((obj, source, targets, count, data) =>");
            sb.AppendLine($"              {{");
            sb.AppendLine($"                  var instance = ({componentType.Name})obj;");
            sb.AppendLine($"                  var result = RpcValidationResult.CreatePreAllocated(count);");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  var validationContext = new RpcValidationContext");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      SourceAuthorityId = source,");
            sb.AppendLine($"                      TargetAuthorityIds = targets,");
            sb.AppendLine($"                      TargetCount = count,");
            sb.AppendLine($"                      PreAllocatedResult = result");
            sb.AppendLine($"                  }};");
            sb.AppendLine($"                  ");
            sb.AppendLine($"                  try");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.SetValidationContext(validationContext);");
            sb.AppendLine($"                      var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(data);");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                      var {param.Name} = args.{param.Name};");
            }

            var refParams = string.Join(", ", parameters.Select(p => $"ref {p.Name}"));
            sb.AppendLine($"                      result = instance.{targetAttr.ValidationMethodName}({refParams});");
            sb.AppendLine($"                      ");
            sb.AppendLine($"                      if (result.WasModified)");
            sb.AppendLine($"                      {{");

            foreach (var param in parameters)
            {
                sb.AppendLine($"                          args.{param.Name} = {param.Name};");
            }

            sb.AppendLine($"                          int bytesUsed;");
            sb.AppendLine($"                          bool needsReturn;");
            sb.AppendLine($"                          result.ModifiedData = SerializationUtils.SerializeToBytes(args, out bytesUsed, out needsReturn);");
            sb.AppendLine($"                      }}");
            sb.AppendLine($"                      return result;");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"                  finally");
            sb.AppendLine($"                  {{");
            sb.AppendLine($"                      GONetMain.EventBus.ClearValidationContext();");
            sb.AppendLine($"                  }}");
            sb.AppendLine($"              }})");
        }

        private static void GenerateRpcHandlerRegistration(StringBuilder sb, MethodInfo method, string className)
        {
            // Skip generating handlers for base class methods - they're handled by the base class generator
            if (method.DeclaringType == typeof(GONetParticipantCompanionBehaviour))
            {
                return; // Don't generate handler, just metadata
            }

            var parameters = method.GetParameters();
            bool hasParameters = parameters.Length > 0;
            bool returnsValue = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
            bool isAsync = typeof(Task).IsAssignableFrom(method.ReturnType);

            // Check if this is a TargetRpc
            var targetAttr = method.GetCustomAttribute<TargetRpcAttribute>();
            bool isTargetRpc = targetAttr != null;

            sb.AppendLine($"            eventBus.RegisterRpcHandler({className}_RpcIds.{method.Name}_RpcId, async (envelope) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                var rpcEvent = envelope.Event;");
            sb.AppendLine($"                var gnp = GONetMain.GetGONetParticipantById(rpcEvent.GONetId);");
            sb.AppendLine($"                var instance = gnp?.GetComponent<{className}>();");
            sb.AppendLine("                ");
            sb.AppendLine("                // CRITICAL: If participant exists but component doesn't, throw exception to trigger deferral");
            sb.AppendLine("                // This handles runtime-added components that receive RPCs before they're initialized");
            sb.AppendLine("                if (instance == null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    if (gnp != null)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        throw new GONetEventBus.RpcComponentNotReadyException(rpcEvent.GONetId, rpcEvent.RpcId);");
            sb.AppendLine("                    }");
            sb.AppendLine("                    return; // Participant doesn't exist - silently ignore");
            sb.AppendLine("                }");
            sb.AppendLine();

            // Add target checking for TargetRpc
            if (isTargetRpc)
            {
                sb.AppendLine("                // NO more route VALIDATION HERE - if we received it, we're a valid target!");
                sb.AppendLine("                // The server already validated during routing");
                sb.AppendLine();
            }

            // Deserialize parameters if needed (and not already done)
            if (hasParameters)
            {
                string dataTypeName = GetParameterDataStructName(method);
                sb.AppendLine($"                var args = SerializationUtils.DeserializeFromBytes<{dataTypeName}>(rpcEvent.Data);");
            }

            // Build the method call
            string argList = hasParameters ?
                string.Join(", ", parameters.Select(p => $"args.{p.Name}")) :
                "";

            if (!isAsync)
            {
                // Synchronous void method
                sb.AppendLine($"                instance.{method.Name}({argList});");
            }
            else if (returnsValue)
            {
                // Async method with return value
                sb.AppendLine($"                var result = await instance.{method.Name}({argList});");
                sb.AppendLine();
                sb.AppendLine("                // Only send response to remote clients, not to server itself");
                sb.AppendLine("                if (rpcEvent.CorrelationId != 0 && envelope.SourceAuthorityId != GONetMain.MyAuthorityId)");
                sb.AppendLine("                {");
                sb.AppendLine("                    int bytesUsed;");
                sb.AppendLine("                    bool needsReturn;");
                sb.AppendLine("                    byte[] responseData = SerializationUtils.SerializeToBytes(result, out bytesUsed, out needsReturn);");
                sb.AppendLine();
                sb.AppendLine("                    var response = RpcResponseEvent.Borrow();");
                sb.AppendLine("                    response.OccurredAtElapsedTicks = GONetMain.Time.ElapsedTicks;");
                sb.AppendLine("                    response.CorrelationId = rpcEvent.CorrelationId;");
                sb.AppendLine("                    response.Success = true;");
                sb.AppendLine("                    response.Data = responseData;");
                sb.AppendLine();
                sb.AppendLine("                    eventBus.Publish(response, targetClientAuthorityId: envelope.SourceAuthorityId, shouldPublishReliably: true);");
                sb.AppendLine("                    // Publish auto-returns the event and the data");
                sb.AppendLine("                }");
            }
            else
            {
                // Async void/Task method (no return value)
                sb.AppendLine($"                await instance.{method.Name}({argList});");
            }

            sb.AppendLine("            });");
        }

        private static void GenerateParameterDataStruct(StringBuilder sb, MethodInfo method, HashSet<string> processedTypes)
        {
            string structName = GetParameterDataStructName(method);

            if (processedTypes.Contains(structName))
            {
                return;
            }
            processedTypes.Add(structName);

            var parameters = method.GetParameters();

            sb.AppendLine("    [MemoryPackable]");
            sb.AppendLine($"    public partial struct {structName}");
            sb.AppendLine("    {");

            foreach (var param in parameters)
            {
                string typeName = GetFriendlyTypeName(param.ParameterType);
                sb.AppendLine($"        public {typeName} {param.Name} {{ get; set; }}");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static string GetParameterDataStructName(MethodInfo method)
        {
            // Use simple type name (without namespace) to create valid C# struct names.
            // The generated structs are declared within the same namespace as the component,
            // so type resolution works correctly without needing the full namespace prefix.
            // This allows RPC classes to exist in namespaces (e.g., GONet.GONetGlobal).
            return $"{method.DeclaringType.Name}_{method.Name}_RpcData";
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string genericTypeName = type.GetGenericTypeDefinition().FullName;
                genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));
                string genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericTypeName}<{genericArgs}>".Replace('+', '.');
            }

            return type.FullName.Replace('+', '.');
        }

        private static void GenerateDispatcherMethods(StringBuilder sb, Type componentType, IEnumerable<MethodInfo> rpcMethods)
        {
            // Group methods by parameter count
            var methodsByParamCount = rpcMethods.GroupBy(m => m.GetParameters().Length).ToDictionary(g => g.Key, g => g.ToList());

            // Generate synchronous Dispatch methods (existing)
            for (int paramCount = 0; paramCount <= 8; paramCount++)
            {
                GenerateSyncDispatchMethod(sb, componentType, methodsByParamCount, paramCount);
            }

            // Generate async DispatchAsync methods (new)
            for (int paramCount = 0; paramCount <= 8; paramCount++)
            {
                GenerateAsyncDispatchMethod(sb, componentType, methodsByParamCount, paramCount);
            }
        }

        private static void GenerateSyncDispatchMethod(StringBuilder sb, Type componentType, Dictionary<int, List<MethodInfo>> methodsByParamCount, int paramCount)
        {
            // Generate method signature
            var genericParams = paramCount > 0 ?
                $"<{string.Join(", ", Enumerable.Range(1, paramCount).Select(i => $"T{i}"))}>" : "";
            var methodParams = paramCount > 0 ?
                string.Join(", ", Enumerable.Range(1, paramCount).Select(i => $"T{i} arg{i}")) : "";
            var commaMethodParams = paramCount > 0 ? $", {methodParams}" : "";

            sb.AppendLine($"        public void Dispatch{paramCount}{genericParams}(object instance, string methodName{commaMethodParams})");
            sb.AppendLine("        {");

            if (methodsByParamCount.ContainsKey(paramCount))
            {
                sb.AppendLine($"            var typed = ({componentType.FullName})instance;");
                sb.AppendLine("            switch (methodName)");
                sb.AppendLine("            {");

                foreach (var method in methodsByParamCount[paramCount])
                {
                    var parameters = method.GetParameters();
                    sb.AppendLine($"                case {method.Name.ToUpper()}:");

                    var args = string.Join(", ", parameters.Select((p, i) => $"({GetFriendlyTypeName(p.ParameterType)})(object)arg{i + 1}"));
                    if (method.ReturnType == typeof(void))
                    {
                        sb.AppendLine($"                    typed.{method.Name}({args});");
                    }
                    else if (method.ReturnType == typeof(Task))
                    {
                        // For async Task methods (including TargetRpc), we need to handle them properly
                        sb.AppendLine($"                    _ = typed.{method.Name}({args});"); // Fire and forget
                    }
                    sb.AppendLine("                    break;");
                }

                sb.AppendLine("            }");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void GenerateAsyncDispatchMethod(StringBuilder sb, Type componentType, Dictionary<int, List<MethodInfo>> methodsByParamCount, int paramCount)
        {
            // Generate method signature with TResult
            var genericParams = paramCount > 0 ?
                $"<TResult, {string.Join(", ", Enumerable.Range(1, paramCount).Select(i => $"T{i}"))}>" :
                "<TResult>";
            var methodParams = paramCount > 0 ?
                string.Join(", ", Enumerable.Range(1, paramCount).Select(i => $"T{i} arg{i}")) : "";
            var commaMethodParams = paramCount > 0 ? $", {methodParams}" : "";

            sb.AppendLine($"        public async Task<TResult> DispatchAsync{paramCount}{genericParams}(object instance, string methodName{commaMethodParams})");
            sb.AppendLine("        {");

            if (methodsByParamCount.ContainsKey(paramCount))
            {
                // Only include async methods that return Task<T>
                var asyncMethods = methodsByParamCount[paramCount]
                    .Where(m => m.ReturnType.IsGenericType &&
                               m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    .ToList();

                if (asyncMethods.Any())
                {
                    sb.AppendLine($"            var typed = ({componentType.FullName})instance;");
                    sb.AppendLine("            switch (methodName)");
                    sb.AppendLine("            {");

                    foreach (var method in asyncMethods)
                    {
                        var parameters = method.GetParameters();
                        sb.AppendLine($"                case {method.Name.ToUpper()}:");

                        var args = parameters.Length > 0 ?
                            string.Join(", ", parameters.Select((p, i) => $"({GetFriendlyTypeName(p.ParameterType)})(object)arg{i + 1}")) :
                            "";

                        // Double cast through object to handle the generic return
                        sb.AppendLine($"                    return (TResult)(object)await typed.{method.Name}({args});");
                    }

                    sb.AppendLine("                default:");
                    sb.AppendLine("                    return default(TResult);");
                    sb.AppendLine("            }");
                }
                else
                {
                    sb.AppendLine("            return default(TResult);");
                }
            }
            else
            {
                sb.AppendLine("            return default(TResult);");
            }

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void ValidateRpcMethod(Type componentType, MethodInfo method, List<string> errors, bool isOnGONetGlobal, bool isOnGONetLocal)
        {
            var attr = method.GetCustomAttribute<GONetRpcAttribute>();
            string methodId = $"{method.DeclaringType.FullName}.{method.Name}()";

            // Return type validation
            Type returnType = method.ReturnType;
            bool isVoid = returnType == typeof(void);
            bool isTask = returnType == typeof(Task);
            bool isGenericTask = returnType.IsGenericType &&
                                 returnType.GetGenericTypeDefinition() == typeof(Task<>);

            if (attr is ServerRpcAttribute)
            {
                if (!isVoid && !isTask && !isGenericTask)
                {
                    errors.Add($"ERROR: {methodId} - ServerRpc must return void, Task, or Task<T>");
                    errors.Add($"  Current: {returnType.FullName}");
                    errors.Add($"  FIX: Change to one of:");
                    errors.Add($"    - void {method.Name}(...) for fire-and-forget");
                    errors.Add($"    - async Task {method.Name}(...) for async without return");
                    errors.Add($"    - async Task<YourType> {method.Name}(...) for request-response");
                }
            }
            else if (attr is ClientRpcAttribute || attr is TargetRpcAttribute)
            {
                // Check for allowed return types
                bool isValidReturn = isVoid || isTask;

                // TargetRpc can also return Task<RpcDeliveryReport> for delivery confirmation
                if (attr is TargetRpcAttribute && returnType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                    returnType.GetGenericArguments()[0] == typeof(RpcDeliveryReport))
                {
                    isValidReturn = true;
                }

                if (!isValidReturn)
                {
                    errors.Add($"ERROR: {methodId} - {attr.GetType().FullName} must return void, Task, or Task<RpcDeliveryReport> (TargetRpc only)");
                    errors.Add($"  Current: {returnType.FullName}");
                    errors.Add($"  FIX: Change return type appropriately");
                }
            }

            // Async validation
            if (isGenericTask || isTask)
            {
                bool hasAsyncModifier = method.GetCustomAttribute<System.Runtime.CompilerServices.AsyncStateMachineAttribute>() != null;
                if (!hasAsyncModifier)
                {
                    errors.Add($"WARNING: {methodId} returns Task but missing 'async' modifier");
                    errors.Add($"  FIX: Add 'async' keyword: async {returnType.FullName} {method.Name}(...)");
                }
            }

            // Parameter validation
            foreach (var param in method.GetParameters())
            {
                // GONetRpcContext must not be a parameter anymore
                if (param.ParameterType == typeof(GONetRpcContext))
                {
                    errors.Add($"ERROR: {methodId} - GONetRpcContext cannot be a method parameter");
                    errors.Add($"  FIX: Remove the GONetRpcContext parameter and access it via GONetEventBus.CurrentRpcContext instead:");
                    errors.Add($"  var context = GONetEventBus.CurrentRpcContext;");
                    continue;
                }

                if (!IsMemoryPackable(param.ParameterType))
                {
                    errors.Add($"ERROR: {methodId} parameter '{param.Name}' type '{param.ParameterType.FullName}' not serializable");
                    errors.Add($"  FIX: Add [MemoryPackable] attribute to '{param.ParameterType.FullName}'");
                    errors.Add($"  FIX: Make the class/struct partial: 'public partial class {param.ParameterType.FullName}'");
                }

                if (param.IsOut || param.ParameterType.IsByRef)
                {
                    errors.Add($"ERROR: {methodId} - RPC cannot have ref/out parameters");
                    errors.Add($"  Parameter: {param.Name}");
                    errors.Add($"  FIX: Use return values or separate RPCs instead");
                }
            }

            // Context-specific validation
            if (attr.IsMineRequired)
            {
                if (isOnGONetGlobal)
                {
                    errors.Add($"WARNING: {methodId} has IsMineRequired=true but is on GONetGlobal GameObject");
                    errors.Add($"  GONetGlobal is shared by all - IsMine checks may not make sense");
                    errors.Add($"  CONSIDER: Set [ServerRpc(IsMineRequired = false)] for global operations");
                }
            }

            if (attr is TargetRpcAttribute targetRpc)
            {
                if (isOnGONetGlobal && targetRpc.Target == RpcTarget.Owner)
                {
                    errors.Add($"ERROR: {methodId} targets 'Owner' but is on GONetGlobal");
                    errors.Add($"  GONetGlobal doesn't have an individual owner");
                    errors.Add($"  FIX: Use RpcTarget.All or move this RPC to GONetLocal/instance");
                }

                if (!string.IsNullOrEmpty(targetRpc.TargetPropertyName))
                {
                    var property = componentType.GetProperty(targetRpc.TargetPropertyName);
                    bool isListOrArray = property?.PropertyType == typeof(List<ushort>) ||
                                        property?.PropertyType == typeof(ushort[]);

                    if (isListOrArray && !targetRpc.IsMultipleTargets)
                    {
                        errors.Add($"ERROR: {methodId} - TargetPropertyName '{targetRpc.TargetPropertyName}' is a collection but isMultipleTargets is false");
                    }
                    else if (!isListOrArray && targetRpc.IsMultipleTargets)
                    {
                        errors.Add($"ERROR: {methodId} - TargetPropertyName '{targetRpc.TargetPropertyName}' is not a collection but isMultipleTargets is true");
                    }
                }
            }

            // Enhanced validation for TargetRpc with validation method
            var targetAttr = method.GetCustomAttribute<TargetRpcAttribute>();
            if (targetAttr != null && !string.IsNullOrEmpty(targetAttr.ValidationMethodName))
            {
                var validationMethod = method.DeclaringType.GetMethod(targetAttr.ValidationMethodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (validationMethod == null)
                {
                    errors.Add($"ERROR: {methodId} - Validation method '{targetAttr.ValidationMethodName}' not found");
                    errors.Add($"  FIX: Add method: internal RpcValidationResult {targetAttr.ValidationMethodName}(...)");

                    // Provide signature example
                    var rpcParams = method.GetParameters();
                    if (rpcParams.Length == 0)
                    {
                        errors.Add($"  Expected signature: internal RpcValidationResult {targetAttr.ValidationMethodName}()");
                    }
                    else
                    {
                        var paramList = string.Join(", ", rpcParams.Select(p => $"ref {p.ParameterType.Name} {p.Name}"));
                        errors.Add($"  Expected signature: internal RpcValidationResult {targetAttr.ValidationMethodName}({paramList})");
                    }
                }
                else
                {
                    // Check return type
                    if (validationMethod.ReturnType != typeof(RpcValidationResult))
                    {
                        errors.Add($"ERROR: {methodId} - Validation method '{targetAttr.ValidationMethodName}' must return RpcValidationResult");
                        errors.Add($"  Current return type: {validationMethod.ReturnType.Name}");
                        errors.Add($"  FIX: Change return type to RpcValidationResult");
                    }

                    // Check parameters match with ref
                    var rpcParams = method.GetParameters();
                    var validationParams = validationMethod.GetParameters();

                    if (rpcParams.Length != validationParams.Length)
                    {
                        errors.Add($"ERROR: {methodId} - Validation method parameter count mismatch");
                        errors.Add($"  RPC has {rpcParams.Length} parameters");
                        errors.Add($"  Validation method has {validationParams.Length} parameters");
                        errors.Add($"  FIX: Validation method must have the same parameters as the RPC, but passed by ref");
                    }
                    else
                    {
                        bool hasParameterErrors = false;
                        for (int i = 0; i < rpcParams.Length; i++)
                        {
                            var rpcParam = rpcParams[i];
                            var valParam = validationParams[i];

                            // Check if ref
                            if (!valParam.ParameterType.IsByRef)
                            {
                                errors.Add($"ERROR: {methodId} - Validation parameter '{valParam.Name}' at position {i} must be passed by ref");
                                errors.Add($"  Current: {valParam.ParameterType.Name} {valParam.Name}");
                                errors.Add($"  FIX: ref {rpcParam.ParameterType.Name} {valParam.Name}");
                                hasParameterErrors = true;
                                continue;
                            }

                            // Check type matches (accounting for ref)
                            var valParamType = valParam.ParameterType.GetElementType();
                            if (valParamType != rpcParam.ParameterType)
                            {
                                errors.Add($"ERROR: {methodId} - Validation parameter type mismatch at position {i}");
                                errors.Add($"  RPC parameter: {rpcParam.ParameterType.Name} {rpcParam.Name}");
                                errors.Add($"  Validation parameter: {valParamType.Name} {valParam.Name}");
                                errors.Add($"  FIX: ref {rpcParam.ParameterType.Name} {valParam.Name}");
                                hasParameterErrors = true;
                            }

                            // Warn about name mismatch
                            if (valParam.Name != rpcParam.Name)
                            {
                                errors.Add($"WARNING: {methodId} - Parameter name mismatch at position {i}");
                                errors.Add($"  RPC parameter name: {rpcParam.Name}");
                                errors.Add($"  Validation parameter name: {valParam.Name}");
                                errors.Add($"  Consider using the same name for clarity");
                            }
                        }

                        if (!hasParameterErrors)
                        {
                            // Valid signature - provide usage example
                            GONetLog.Debug($"Valid validation method found: {targetAttr.ValidationMethodName}");
                        }
                    }
                }
            }

            // Check for property/target mismatch
            if (targetAttr != null && !string.IsNullOrEmpty(targetAttr.TargetPropertyName))
            {
                var property = method.DeclaringType.GetProperty(targetAttr.TargetPropertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (property == null)
                {
                    errors.Add($"ERROR: {methodId} - Target property '{targetAttr.TargetPropertyName}' not found");
                }
                else
                {
                    bool isListOrArray = property.PropertyType == typeof(List<ushort>) ||
                                        property.PropertyType == typeof(ushort[]);

                    if (isListOrArray && !targetAttr.IsMultipleTargets)
                    {
                        errors.Add($"ERROR: {methodId} - Property '{targetAttr.TargetPropertyName}' is a collection but isMultipleTargets is false");
                        errors.Add($"  FIX: Set isMultipleTargets: true in the TargetRpc attribute");
                    }
                    else if (!isListOrArray && targetAttr.IsMultipleTargets)
                    {
                        errors.Add($"ERROR: {methodId} - Property '{targetAttr.TargetPropertyName}' is not a collection but isMultipleTargets is true");
                        errors.Add($"  Property type: {property.PropertyType.Name}");
                        errors.Add($"  FIX: Either use List<ushort> or ushort[] for the property, or set isMultipleTargets: false");
                    }
                    else if (!isListOrArray && property.PropertyType != typeof(ushort))
                    {
                        errors.Add($"ERROR: {methodId} - Target property must be ushort, List<ushort>, or ushort[]");
                        errors.Add($"  Current type: {property.PropertyType.Name}");
                    }
                }
            }
        }

        private static bool IsMemoryPackable(Type type)
        {
            if (type.GetCustomAttribute<MemoryPackableAttribute>() != null)
                return true;

            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(Vector3) ||
                type == typeof(Vector2) ||
                type == typeof(Quaternion) ||
                type.IsEnum)
                return true;

            if (type.IsArray)
                return IsMemoryPackable(type.GetElementType());

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(Dictionary<,>))
                {
                    return type.GetGenericArguments().All(IsMemoryPackable);
                }
            }

            return false;
        }
        #endregion
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
            try
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
            catch (Exception ex)
            {
                GONetLog.Warning($"Exception processing '{memberOwner_componentTypeAssemblyQualifiedName}'.  It might be OK to ignore if this information in cache is outdated.  Message: {ex.Message}");
                return false;
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
        /// Assigned once known to be a unique combo.
        /// Relates directly to <see cref="GONetParticipant.CodeGenerationId"/>.
        /// IMPORTANT: MessagePack for C# is not supporting internal field.....so, this is now public...boo!
        /// </summary>
        public byte codeGenerationId;

        /// <summary>
        /// In deterministic order....with the one for <see cref="GONetParticipant.GONetId"/> first due to special case processing....also <see cref="GONetParticipant.ASSumed_GONetId_INDEX"/>
        /// </summary>
        public GONetParticipant_ComponentsWithAutoSyncMembers_Single[] ComponentMemberNames_By_ComponentTypeFullName;

        public int SingleMemberCount
        {
            get
            {
                int count = 0;
                try
                {
                    foreach (var a in ComponentMemberNames_By_ComponentTypeFullName)
                    {
                        count += a.autoSyncMembers.Length;
                    }
                }
                catch { }
                return count;
            }
        }

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
            //* v1.3.1 TURN OFF automatic code generation related activities in preference for only doing it during a build to avoid excessive actions that are only really required to be done for builds
            if (!Application.isPlaying)
            {
                // GONET 1.4 is going to stop doing this unless a build is going: GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
                
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.OnPostprocessAllAssets_TakeNoteOfAnyGONetChanges(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
            }
            //*/
        }
    }
}