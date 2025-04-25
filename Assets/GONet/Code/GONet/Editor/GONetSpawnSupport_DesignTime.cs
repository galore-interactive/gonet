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
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Editor
{
    /// <summary>
    /// sister class of <see cref="GONetSpawnSupport_Runtime"/>.
    /// </summary>
    [InitializeOnLoad]
    public static class GONetSpawnSupport_DesignTime
    {
        private static bool IsCompiling
        {
            get => EditorPrefs.GetBool(IsCompilingKey, false);
            set { EditorPrefs.SetBool(IsCompilingKey, value); GONetLog.Debug($"Setting IsCompiling to: {value}"); }
        }

        private static void OnCompilationStarted(object obj)
        {
            IsCompiling = true;
            GONetLog.Debug("......................................................COMPILE start");
        }

        private static void OnCompilationFinished(object obj)
        {
            GONetLog.Debug("COMPILE end - setting up delay");

            // Use delay call to ensure this runs after Unity settles post-compilation
            EditorApplication.delayCall += () =>
            {
                GONetLog.Debug("......................................................COMPILE end (after delay)");
                //IsCompiling = false;
            };
        }
        private static void OnBeforeAssemblyReload()
        {
            GONetLog.Debug("Before assembly reload - still compiling...");
            IsCompiling = true;
        }

        private static void OnAfterAssemblyReload()
        {
            GONetLog.Debug("After assembly reload - now it's safe to reset flags.");
            // Use delay call to ensure this runs after Unity settles post-compilation
            EditorApplication.delayCall += () =>
            {
                GONetLog.Debug("......................................................COMPILE end (after delay)");
                IsCompiling = false;
            };
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // Handle actions post-reload (useful for checking compilation state)
            if (EditorPrefs.HasKey(IsCompilingKey) && EditorPrefs.GetBool(IsCompilingKey, false))
            {
                GONetLog.Debug("Scripts reloaded while compiling; performing post-compilation cleanup.");
                OnCompilationFinished(null); // Ensure post-compilation cleanup runs
            }
        }

        private static void CompilationRecoveryCheck()//
        {
            /*
            // If IsCompiling is true but Unity is not actually compiling, reset the state
            if (IsCompiling && !EditorApplication.isCompiling)
            {
                GONetLog.Debug("Recovery mechanism: resetting IsCompiling as Unity is not compiling.");
                EditorApplication.delayCall += () =>
                {
                    GONetLog.Debug("......................................................COMPILE end (after delay)");
                    IsCompiling = false;
                };
            }
            */
        }
        internal static bool IsInitialEditorLoad { get; private set; } = true;
        internal static bool IsQuitting { get; set; }

        private const string IsCompilingKey = "GONet_IsCompiling"; // Key for EditorPrefs

        static GONetSpawnSupport_DesignTime()
        {
            /* GONet v1.4 only does stuff in the build....not "in real time" like this:
            EditorApplication.hierarchyChanged += OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif
            */
            
            // Instead, in v1.4+, we just monitor GONet related changes and take note that there are changes from last build to warn users later
            EditorApplication.hierarchyChanged += OnHierarchyChanged_TakeNoteOfAnyGONetChanges_SceneOnly;
            EditorApplication.projectChanged += OnProjectChanged_TakeNoteOfAnyGONetChanges_ProjectOnly;

            GONetParticipant.OnDestroyCalled += GONetParticipant_OnDestroyCalled;
            GONetParticipant.OnAwakeEditor += GONetParticipant_OnAwakeEditor;

            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            
            // Recover the IsCompiling state from EditorPrefs (in case of domain reload)
            if (EditorPrefs.HasKey(IsCompilingKey) && EditorPrefs.GetBool(IsCompilingKey, false))
            {
                //GONetLog.Debug("Recovered from domain reload after compilation. Performing post-compilation actions.");
                //OnCompilationFinished(null); // Run the post-compilation logic
            }

            // Periodic recovery check to handle edge cases like crashes
            EditorApplication.update += CompilationRecoveryCheck;
            EditorApplication.update += ResetInitialEditorLoadFlag;

            //SceneManager.sceneLoading += OnSceneLoading;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Check when Unity is about to enter play mode (ExitingEditMode)
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                bool didPreventEnteringPlaymode = false;
                string filePath = GetDesignTimeDirtyReasonsFilePath();
                AddDirtyReasonIfScenesInBuildDiffer(filePath);

                // Check for the existence of the "is dirty" file
                if (File.Exists(filePath))
                {
                    // Read the contents of the file
                    string fileContents = GetLimitedFilePreview(filePath, 10);

                    // Show a warning to the user
                    ShowGONetWarning(fileContents);

                    EditorApplication.isPlaying = false;
                    didPreventEnteringPlaymode = true;
                }

                if (!didPreventEnteringPlaymode)
                {
                    GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.
                        OnEditorPlayModeStateChanged_BlockEnteringPlaymodeIfUniqueSnapsChanged(state, out didPreventEnteringPlaymode);
                }

                /* if needed as security, double safe cleanup operation:
                if (didPreventEnteringPlaymode)
                {
                    // double check we always delete these files
                    GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.DeleteGeneratedFiles();
                }
                */
            }
            else
            {
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.
                    OnEditorPlayModeStateChanged_BlockEnteringPlaymodeIfUniqueSnapsChanged(state, out bool didPreventEnteringPlaymode);
            }

            static void AddDirtyReasonIfScenesInBuildDiffer(string dirtyReasonFilePath)
            {
                if (TryGetGONetMostRecentSuccessfulBuild(out GONetMostRecentSuccessfulBuild record))
                {
                    List<string> currentScenePaths = EditorBuildSettings.scenes
                        .Where(scene => scene.enabled)
                        .Select(scene => scene.path)
                        .ToList();

                    bool areAnyDeviataions = !currentScenePaths.SequenceEqual(record.ScenePathsIncluded);
                    if (areAnyDeviataions)
                    {
                        const string errorMessage = "The scene paths listed in the last successful build do not match the current list of scene paths in the build settings.";
                        AddGONetDesignTimeDirtyReason(errorMessage);
                    }
                }
            }
        }

        private static string GetLimitedFilePreview(string filePath, int maxLines)
        {
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length <= maxLines)
            {
                return string.Join("\n", lines);
            }

            // Return only the first few lines with an indication of truncation
            return string.Join("\n", lines, 0, maxLines) + $"\n\n...and {lines.Length - maxLines} more lines.";
        }
        private static void ShowGONetWarning(string fileContents)
        {
            // Create the warning message
            string warningMessage = "WARNING: GONet will not function properly until you create another build, because the server and all clients are required to have the same information as it pertains to all the things that are going to be networked.\n\n" +
                                    "Please review the following reasons (i.e., things that changed during design-time since the last build):\n\n" +
                                    fileContents;

            // Show a dialog with the warning and file contents
            EditorUtility.DisplayDialog("GONet Warning", warningMessage, "OK");
        }
        private static void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            //GONetLog.Debug($" %& %^$B& ^$%#YMB$^Y ^$%BMKBYL ^%MUYK ^MV&UKY&^ MVUKY ^MBV UKY^MBUKY^ BVMUK^");
        }

        private static void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            //GONetLog.Debug($" %& %^$B& ^$%#YMB$^Y ^$%BMKBYL ^%MUYK ^MV&UKY&^ MVUKY ^MBV UKY^MBUKY^ BVMUK^");
        }

        private static void ResetInitialEditorLoadFlag()
        {
            // Skip first frame after editor loads to avoid initial noise
            if (Time.frameCount > 0)
            {
                IsInitialEditorLoad = false;
                EditorApplication.update -= ResetInitialEditorLoadFlag; // Unsubscribe after first frame
            }
        }

        private static void GONetParticipant_OnAwakeEditor(GONetParticipant gonetParticipant)
        {
            bool isHappeningDueToChangingPlayModeOrInitialOpenInEditor =
                (!GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange.HasValue ||
                 GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.EnteredEditMode ||
                 GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.ExitingPlayMode) &&
                (GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount == Time.frameCount ||
                 Time.frameCount == 0);

            bool isTargetedDesignTimeOnlyAction =
                !EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !isHappeningDueToChangingPlayModeOrInitialOpenInEditor &&
                !EditorApplication.isUpdating && // handle scene loading or editor updates
                !Application.isBatchMode && // Avoid triggering in CI/CD build pipelines
                !IsQuitting;
            if (isTargetedDesignTimeOnlyAction &&
                (IsInSceneIncludedInBuild(gonetParticipant) || DesignTimeMetadata.TryGetFullPathInProject(gonetParticipant, out string fullPathInProject)))
            {
                // if in here, we know this is a new GNP being added into scene in editor edit mode (i.e., design time add)
                //string dirtyReason = $"GONetParticipant was awakened on GameObject: {DesignTimeMetadata.GetFullPath(gonetParticipant)} (Design-time only). {GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange}:{GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount}:{Time.frameCount}";
                string dirtyReason = $"GONetParticipant was awakened on GameObject: {DesignTimeMetadata.GetFullPath(gonetParticipant)}";
                AddGONetDesignTimeDirtyReason(dirtyReason);
            }
            /* troubleshoot assistance when above not working:
            else
            {
                // Use StringBuilder to log all relevant values
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Entered the else block for GONetParticipant Awake on GameObject: {gonetParticipant.gameObject.name}.");
                sb.AppendLine($"EditorApplication.isPlaying: {EditorApplication.isPlaying}");
                sb.AppendLine($"EditorApplication.isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");
                sb.AppendLine($"EditorApplication.isUpdating (scene loading): {EditorApplication.isUpdating}");
                sb.AppendLine($"isHappeningDueToChangingPlayModeOrInitialOpenInEditor: {isHappeningDueToChangingPlayModeOrInitialOpenInEditor}");
                sb.AppendLine($"LastPlayModeStateChange: {GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange}");
                sb.AppendLine($"LastPlayModeStateChange_frameCount: {GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount}");
                sb.AppendLine($"Current Time.frameCount: {Time.frameCount}");

                // Log the accumulated information
                GONetLog.Debug(sb.ToString());
            }
            */
        }

        internal static void IndicateGONetDesignTimeNoLongerDirty()
        {
            string filePath = GetDesignTimeDirtyReasonsFilePath();
            File.Delete(filePath);
        }

        private static string GetDesignTimeDirtyReasonsFilePath()
        {
            string folderPath = GetDesignTimeDirtyReasonFolder();
            string filePath = Path.Combine(folderPath, "GONetDesignTimeDirtyReasons.log");
            return filePath;
        }

        internal static void RecordScenesInSuccessfulBuild()
        {
            HashSet<string> scenePathsIncluded = new();
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                scenePathsIncluded.Add(buildScene.path);
            }
            GONetMostRecentSuccessfulBuild record = new()
            {
                ScenePathsIncluded = scenePathsIncluded.ToArray(),
                DateTimeBuildSucceeded = DateTime.UtcNow,
            };

            string folderPath = GetDesignTimeDirtyReasonFolder();
            string filePath = Path.Combine(folderPath, string.Concat(nameof(GONetMostRecentSuccessfulBuild), ".json"));
            File.WriteAllText(filePath, JsonUtility.ToJson(record));
        }

        internal static bool TryGetGONetMostRecentSuccessfulBuild(out GONetMostRecentSuccessfulBuild record)
        {
            record = null;
            string folderPath = GetDesignTimeDirtyReasonFolder();
            string filePath = Path.Combine(folderPath, string.Concat(nameof(GONetMostRecentSuccessfulBuild), ".json"));
            if (!File.Exists(filePath)) return false;

            try
            {
                record = JsonUtility.FromJson<GONetMostRecentSuccessfulBuild>(File.ReadAllText(filePath));
            }
            catch (Exception ex) { }
            return record != null;
        }

        [Serializable]
        public class GONetMostRecentSuccessfulBuild
        {
            public string[] ScenePathsIncluded;

            [SerializeField] private string dateTimeBuildSucceeded;
            public DateTime DateTimeBuildSucceeded
            {
                get => string.IsNullOrEmpty(dateTimeBuildSucceeded) ? default : DateTime.Parse(dateTimeBuildSucceeded);
                set => dateTimeBuildSucceeded = value.ToLocalTime().ToString("o"); // "o" for round-trip (ISO 8601) format
            }
        }

        private static string GetDesignTimeDirtyReasonFolder()
        {
            return Path.Combine(Application.dataPath, "GONet", "Code", "GONet", "Editor", "Generation");
        }

        internal static void AddGONetDesignTimeDirtyReason(string dirtyReason)
        {
            string folderPath = GetDesignTimeDirtyReasonFolder();
            string filePath = GetDesignTimeDirtyReasonsFilePath();

            // Create the directory if it doesn't exist
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            try
            {//
                // Check if the dirtyReason already exists in the file to avoid duplicates
                if (File.Exists(filePath))
                {
                    string existingContent = File.ReadAllText(filePath);
                    if (existingContent.Contains(dirtyReason))
                    {
                        GONetLog.Debug($"isCompiling? {EditorApplication.isCompiling}:{IsCompiling}:{EditorApplication.isUpdating}, Skipped logging duplicate design-time dirty reason: {dirtyReason}");
                        return; // Exit early if the exact dirtyReason already exists
                    }
                }

                // Get the current date and time in a readable format
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Create the log entry with the reason and timestamp
                string logEntry = $"{timestamp}: {dirtyReason}\n";

                // Append to the file, creating it if it doesn't exist
                File.AppendAllText(filePath, logEntry);

                // Optionally, log confirmation to Unity console
                GONetLog.Debug($"Logged design-time dirty reason: {dirtyReason}");
            }
            catch (Exception ex)
            {
                // Handle any file writing errors
                GONetLog.Debug($"Error writing to log file: {ex.Message}");
            }
        }

        private static void GONetParticipant_OnDestroyCalled(GONetParticipant gonetParticipant)
        {
            bool isHappeningDueToChangingPlayModeOrInitialOpenInEditor =
                (!GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange.HasValue ||
                 GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.EnteredEditMode ||
                 GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.ExitingPlayMode) &&
                (GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount == Time.frameCount ||
                 Time.frameCount == 0);

            bool isTargetedDesignTimeOnlyAction =
                !EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode &&
                !isHappeningDueToChangingPlayModeOrInitialOpenInEditor &&
                !EditorApplication.isUpdating && // handle scene loading or editor updates
                !Application.isBatchMode && // Avoid triggering in CI/CD build pipelines
                !IsQuitting;
            if (isTargetedDesignTimeOnlyAction && 
                (IsInSceneIncludedInBuild(gonetParticipant) || DesignTimeMetadata.TryGetFullPathInProject(gonetParticipant, out string fullPathInProject)))
            {
                // if in here, we know this is a new GNP being added into scene in editor edit mode (i.e., design time add)
                string dirtyReason = $"GONetParticipant was removed from GameObject: {DesignTimeMetadata.GetFullPath(gonetParticipant)} (Design-time only).";
                AddGONetDesignTimeDirtyReason(dirtyReason);
            }
        }

        private static void OnProjectChanged_TakeNoteOfAnyGONetChanges_ProjectOnly()
        {
            HashSet<GONetParticipant> projectGnps = new();

            // IMPORTANT: have to load them all up for else the following call will not "find" them all and only the ones that happened to be loaded already would be found/processed
            Resources.LoadAll<GONetParticipant>(string.Empty);
            foreach (var gonetParticipant in Resources.FindObjectsOfTypeAll<GONetParticipant>())
            {
                AddIfAppropriate(projectGnps, gonetParticipant);
            }

            // IMPORTANT: have to do this because the above call to Resources.FindObjectsOfTypeAll<GONetParticipant>() does NOT identify a prefab that just had GNP added to it this frame!!!
            foreach (GONetParticipant gonetParticipant in GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GetGNPsAddedToPrefabThisFrame())
            {
                AddIfAppropriate(projectGnps, gonetParticipant);
            }

            // Collect the paths for each GONetParticipant
            HashSet<string> allPathsToGnpsInProject = new();
            foreach (var gonetParticipant in projectGnps)
            {
                string fullPath = DesignTimeMetadata.GetFullPath(gonetParticipant);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    allPathsToGnpsInProject.Add(fullPath);
                }
            }
            
            GONetLog.Debug($"Here are all {allPathsToGnpsInProject.Count} GNPs in project:\n{string.Join("\n", allPathsToGnpsInProject)}");
            ProcessAnyDesignTimeDirty_IfAppropriate(allPathsToGnpsInProject);

            static void AddIfAppropriate(HashSet<GONetParticipant> projectGnps, GONetParticipant gonetParticipant)
            {
                // Check if the GONetParticipant is part of a scene
                Scene scene = gonetParticipant.gameObject.scene;
                bool isPresumedInProject = scene == null || string.IsNullOrEmpty(scene.path);

                // Check if the scene is included in the build settings
                bool isInSceneInBuild = !isPresumedInProject && IsSceneIncludedInBuild(scene.path);
                if (isPresumedInProject || isInSceneInBuild)
                {
                    projectGnps.Add(gonetParticipant);
                }
                else
                {
                    GONetLog.Debug($"GONetParticipant found in excluded scene (i.e., not in build, so GONet does not care now): {DesignTimeMetadata.GetFullPath(gonetParticipant)}");
                }

                GONetLog.Debug($"SLEEPS RESOURCES gnp:{DesignTimeMetadata.GetFullPath(gonetParticipant)}");
            }
        }

        private static bool IsInSceneIncludedInBuild(GONetParticipant gonetParticipant)
        {
            // Check if the GONetParticipant is part of a scene
            Scene scene = gonetParticipant.gameObject.scene;
            bool isPresumedInProject = scene == null || string.IsNullOrEmpty(scene.path);
            if (isPresumedInProject) return false;

            return IsSceneIncludedInBuild(scene.path);
        }

        private static bool IsSceneIncludedInBuild(string scenePath)
        {
            foreach (var buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.path == scenePath && buildScene.enabled)
                {
                    return true; // Scene is included in the build settings
                }
            }
            return false;
        }

        private static void ProcessAnyDesignTimeDirty_IfAppropriate(HashSet<string> fullPathsToDesignTimeGnps)
        {
            // compare this list and the current metadata associated with all these gnps to that stored in the last build's metadata..if different TAKE NOTE and refer to this when entering play mode and show warning!
            IEnumerable<DesignTimeMetadata> designTimeMetadatasFromLastBuild =
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();
            // HashSet for fast lookup of the GNP paths from the last build's metadata
            HashSet<string> pathsFromLastBuild = new HashSet<string>(
                designTimeMetadatasFromLastBuild.Select(metadata => metadata.Location));

            // Compare the current GNP paths to the previous build's metadata
            foreach (var currentPath in fullPathsToDesignTimeGnps)
            {
                if (!pathsFromLastBuild.Contains(currentPath))
                {
                    AddGONetDesignTimeDirtyReason($"GONetParticipant at {currentPath} was added or modified after the last build.");
                }
            }
        }

        private static void OnHierarchyChanged_TakeNoteOfAnyGONetChanges_SceneOnly()
        {
            // Skip if compiling, updating, or in play mode
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                //GONetLog.Debug("Skipping hierarchy check - editor is busy compiling or updating.");
                return;
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // SKIP first/oth frame due to this method being called when coming out of other GONet generation stuff (e.g., editor support: "Fix GONet Generated Code")
            if (Time.frameCount == 0) return;
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            bool isHierarchyChangingDueToExitingPlayModeInEditor =
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange.HasValue &&
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange == PlayModeStateChange.EnteredEditMode &&
                GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.LastPlayModeStateChange_frameCount == Time.frameCount; // IMPORTANT: this is how we know it "just" changed from play to edit mode...otherwise we could never run the logic we want after exiting the play mode and we start messing around with the hierarchy

            if (!Application.isPlaying && 
                !isHierarchyChangingDueToExitingPlayModeInEditor && // it would not be design time if we are playing (in editor) now would it?
                !IsCompiling &&
                !IsInitialEditorLoad)
            {
                HashSet<string> pathsToGnpsInScene = new();
                int count = SceneManager.loadedSceneCount;
                for (int i = 0; i < count; ++i)
                { //
                    Scene loadedScene = EditorSceneManager.GetSceneAt(i);
                    if (!IsSceneIncludedInBuild(loadedScene.path)) continue; // only consider scene changes when scene is included in the build since GONet does not care otherwise

                    const string SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES = "/";
                    string scenePrefix = string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, loadedScene.name, SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES);

                    foreach (var rootGO in loadedScene.GetRootGameObjects())
                    {
                        foreach (var gonetParticipant in rootGO.GetComponentsInChildren<GONetParticipant>())
                        {
                            string fullUniquePath = DesignTimeMetadata.GetFullUniquePathInScene(gonetParticipant);
                            // TODO check if this exists in the metadata from last build and if not take note of the change/addition!
                            pathsToGnpsInScene.Add(fullUniquePath);
                        }
                    }
                }

                GONetLog.Debug($"Here are all {pathsToGnpsInScene.Count} scene GNPs:\n{string.Join("\n", pathsToGnpsInScene)}");
                ProcessAnyDesignTimeDirty_IfAppropriate(pathsToGnpsInScene);
                HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate();
            }

            static void HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate()
            {
                IEnumerable<DesignTimeMetadata> designTimeLocations_gonetParticipants_lastBuild =
                    GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();
                
                IEnumerable<string> gnpPrefabAssetPaths_lastBuild = 
                    designTimeLocations_gonetParticipants_lastBuild
                        .Where(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                        .Select(x => x.Location.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length));

                // TODO FIXME doing this every time the hiearchy changes is crazy....mainy due to high processing time....need to attempt to move this entire method logic to be called in the other option: AssetPostprocessor.OnPostprocessAllAssets, where we hope we can more narrowly focus in on the specific data that is changing instead of searching the entire project essentially!
                //   --- UPDATE to above TODO FIXME: there is an implementation of this inside AssetPostprocessor/Magoo.OnPostprocessAllAssets (find OnPostprocessAllAssets_TakeNoteOfAnyGONetChanges), so this here can/should probably be removed as redundant and this is less performant for sure.
                List<GONetParticipant> gnpsInProjectResources = 
                    GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GatherGONetParticipantsInAllResourcesFolders();
                
                HashSet<string> gnpsInProjectResources_paths = new(gnpsInProjectResources.Select(g => AssetDatabase.GetAssetPath(g)));

                {// Check for GNP deletes: was previously in gnpPrefabAssetPaths_lastBuild, but NOT in the updated list of gnp prefabs
                 // Check for GNP deletes: previously in gnpPrefabAssetPaths_lastBuild, but NOT in currentGnpAssetPaths
                    IEnumerable<string> deletedGnpPaths = gnpPrefabAssetPaths_lastBuild
                        .Where(path => !gnpsInProjectResources_paths.Contains(path));

                    foreach (string deletedPath in deletedGnpPaths)
                    {
                        AddGONetDesignTimeDirtyReason($"GONetParticipant prefab deleted from project resources: {deletedPath}");
                    }
                }

                {// Check for GNP adds: was previously NOT in gnpPrefabAssetPaths_lastBuild, but is now in the updated list of gnp prefabs
                 // Check for GNP adds: previously NOT in gnpPrefabAssetPaths_lastBuild, but is now in currentGnpAssetPaths
                    IEnumerable<string> addedGnpPaths = gnpsInProjectResources_paths
                        .Where(path => !gnpPrefabAssetPaths_lastBuild.Contains(path));

                    foreach (string addedPath in addedGnpPaths)
                    {
                        AddGONetDesignTimeDirtyReason($"GONetParticipant prefab added to project resources: {addedPath}");
                    }
                }
            }
        }

        private static void OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly()
        {
            // GONet 1.4 stops doing this unless we are building or manually calling 'fix': EnsureDesignTimeLocationsCurrent_ProjectOnly();
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

        internal static void ClearAllDesignTimeMetadata()
        {
            GONetSpawnSupport_Runtime.ClearAllDesignTimeMetadata();
            OverwritePersistenceWith(Enumerable.Empty<DesignTimeMetadata>());
        }

        /// <summary>
        /// POST: contents of <see cref="allDesignTimeLocationsEncountered"/> persisted.
        /// </summary>
        private static void OverwritePersistenceWith(IEnumerable<DesignTimeMetadata> newCompleteDesignTimeLocations)
        {
            if (!ProcessBuildHelper.IsBuilding)
            {
                GONetLog.Warning($"Oops.  Will not overwrite persistence with {nameof(newCompleteDesignTimeLocations)}, because GONet v1.4+ only does that during the time when a build is occurring.  Gotta ensure old logic does not screw things up!");
                return;
            }

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
            string fileContents = JsonUtility.ToJson(designTimeMetadataLibrary, prettyPrint: true);
            GONetLog.Debug($"~~~~~~~~~~~~GEEPs isBuilding? {ProcessBuildHelper.IsBuilding} writing all text to: {fullPath}\n{fileContents}");
            File.WriteAllText(fullPath, fileContents);
        }
    }
}
