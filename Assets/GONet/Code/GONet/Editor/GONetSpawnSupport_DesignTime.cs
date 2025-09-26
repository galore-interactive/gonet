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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ADDRESSABLES_AVAILABLE
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
#endif

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
            set { EditorPrefs.SetBool(IsCompilingKey, value); /*GONetLog.Debug($"Setting IsCompiling to: {value}");*/ }
        }

        private static void OnCompilationStarted(object obj)
        {
            IsCompiling = true;
            //GONetLog.Debug("......................................................COMPILE start");
        }

        private static void OnCompilationFinished(object obj)
        {
            //GONetLog.Debug("COMPILE end - setting up delay");

            // Use delay call to ensure this runs after Unity settles post-compilation
            EditorApplication.delayCall += () =>
            {
                //GONetLog.Debug("......................................................COMPILE end (after delay)");
                //IsCompiling = false;
            };
        }
        private static void OnBeforeAssemblyReload()
        {
            //GONetLog.Debug("Before assembly reload - still compiling...");
            IsCompiling = true;
        }

        private static void OnAfterAssemblyReload()
        {
            //GONetLog.Debug("After assembly reload - now it's safe to reset flags.");
            // Use delay call to ensure this runs after Unity settles post-compilation
            EditorApplication.delayCall += () =>
            {
                //GONetLog.Debug("......................................................COMPILE end (after delay)");
                IsCompiling = false;
            };
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            // Handle actions post-reload (useful for checking compilation state)
            if (EditorPrefs.HasKey(IsCompilingKey) && EditorPrefs.GetBool(IsCompilingKey, false))
            {
                //GONetLog.Debug("Scripts reloaded while compiling; performing post-compilation cleanup.");
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

#if ADDRESSABLES_AVAILABLE
            // Hook into Addressables build completion events for more robust change detection
            RegisterAddressableBuildCallbacks();
#endif

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

        /// <summary>
        /// Performs immediate change detection before play mode transition.
        /// Uses either traditional timestamp-based checking or advanced content-based checking
        /// depending on the team-aware dirty checking configuration.
        /// </summary>
        private static void CheckForChangesBeforePlayMode()
        {
            try
            {
                // Check if team-aware dirty checking is enabled
                bool useContentBasedChecking = IsTeamAwareDirtyCheckingEnabled();

                if (useContentBasedChecking)
                {
                    PerformContentBasedDirtyChecking();
                }
                else
                {
                    // Use traditional addressables checking for solo development
#if ADDRESSABLES_AVAILABLE
                    CheckForAddressablesChangesBeforePlayMode_Traditional();
#endif
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Warning($"Error in pre-play mode change detection: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if team-aware dirty checking is enabled by looking at GONetProjectSettings configuration.
        /// </summary>
        internal static bool IsTeamAwareDirtyCheckingEnabled()
        {
            try
            {
                var projectSettings = GONetProjectSettings.Instance;
                bool isEnabled = projectSettings != null && projectSettings.enableTeamAwareDirtyChecking;
                UnityEngine.Debug.Log($"GONet: IsTeamAwareDirtyCheckingEnabled - projectSettings: {(projectSettings != null ? "found" : "null")}, enableTeamAwareDirtyChecking: {(projectSettings?.enableTeamAwareDirtyChecking ?? false)}, result: {isEnabled}");
                return isEnabled;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"GONet: Error checking team-aware dirty checking setting: {ex.Message}");
                return false; // Default to false if we can't determine
            }
        }

        /// <summary>
        /// Performs advanced content-based dirty checking for team environments.
        /// Uses multi-threaded content hashing to detect actual GONet-relevant changes.
        /// </summary>
        private static async void PerformContentBasedDirtyChecking()
        {
            try
            {
                GONetLog.Debug("Starting team-aware content-based dirty checking...");

                // Create current content snapshot
                var currentSnapshot = await GONetContentSnapshot.CreateSnapshotAsync();

                // Load previous snapshot from last build
                string snapshotPath = GetContentSnapshotFilePath();
                var previousSnapshot = GONetContentSnapshot.LoadSnapshot(snapshotPath);

                // Compare snapshots to find changes
                var changes = GONetContentSnapshot.CompareSnapshots(currentSnapshot, previousSnapshot);

                // Record any detected changes as dirty reasons
                foreach (var change in changes)
                {
                    AddGONetDesignTimeDirtyReason($"Team-aware detection: {change}");
                    GONetLog.Debug($"Content-based detection: {change}");
                }

                if (changes.Any())
                {
                    GONetLog.Debug($"Team-aware dirty checking found {changes.Count} GONet-related changes from teammates or uncommitted work");
                }
                else
                {
                    GONetLog.Debug("Team-aware dirty checking: No GONet-related changes detected");
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Warning($"Error in content-based dirty checking: {ex.Message}");
                // Fallback to traditional checking if content-based fails
#if ADDRESSABLES_AVAILABLE
                CheckForAddressablesChangesBeforePlayMode_Traditional();
#endif
            }
        }

        /// <summary>
        /// Traditional addressables change detection for solo development (faster, timestamp-based).
        /// </summary>
#if ADDRESSABLES_AVAILABLE
        private static void CheckForAddressablesChangesBeforePlayMode_Traditional()
        {
            try
            {
                // Get current addressables GONetParticipants
                var currentAddressableGNPs = GatherAddressableGONetParticipants();

                // Get last build metadata for addressables prefabs
                var lastBuildMetadata = GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence()
                    .Where(m => m.Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                    .ToList();

                // Convert current addressables to location format for comparison
                var currentAddressableLocations = new HashSet<string>();
                foreach (var gnp in currentAddressableGNPs)
                {
                    string assetPath = AssetDatabase.GetAssetPath(gnp);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        string location = $"{GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX}{assetPath}";
                        currentAddressableLocations.Add(location);
                    }
                }

                // Check for new addressable prefabs
                foreach (var currentLocation in currentAddressableLocations)
                {
                    if (!lastBuildMetadata.Any(m => m.Location == currentLocation))
                    {
                        string dirtyReason = $"GONetParticipant at {currentLocation} was added or modified after the last build.";
                        AddGONetDesignTimeDirtyReason(dirtyReason);
                        GONetLog.Debug($"Traditional detection: {dirtyReason}");
                    }
                }

                // Check for removed addressable prefabs
                foreach (var lastBuildMeta in lastBuildMetadata)
                {
                    if (!currentAddressableLocations.Contains(lastBuildMeta.Location))
                    {
                        string dirtyReason = $"GONetParticipant prefab removed from addressables: {lastBuildMeta.Location}";
                        AddGONetDesignTimeDirtyReason(dirtyReason);
                        GONetLog.Debug($"Traditional detection: {dirtyReason}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Warning($"Error in traditional addressables check: {ex.Message}");
            }
        }
#endif

        /// <summary>
        /// Gets the file path for storing content snapshots.
        /// </summary>
        private static string GetContentSnapshotFilePath()
        {
            string folderPath = GetDesignTimeDirtyReasonFolder();
            return Path.Combine(folderPath, "GONetTeamAwareDirtyCheckSnapshot_MemoryPack.bin");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Check when Unity is about to enter play mode (ExitingEditMode)
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // IMPORTANT: Check for changes BEFORE checking for dirty file existence
                // This ensures changes are detected and recorded before the play mode warning check
                // Uses either traditional or content-based checking depending on configuration
                CheckForChangesBeforePlayMode();

                bool didPreventEnteringPlaymode = false;
                string filePath = GetDesignTimeDirtyReasonsFilePath();
                AddDirtyReasonIfScenesInBuildDiffer(filePath);

                // Check for the existence of the "is dirty" file
                if (File.Exists(filePath))
                {
                    // Read the contents of the file
                    string fileContents = GetLimitedFilePreview(filePath, 10);

                    // Show a warning to the user
                    didPreventEnteringPlaymode = ShowGONetWarning_ShouldPreventEnteringPlaymode(fileContents);

                    EditorApplication.isPlaying = !didPreventEnteringPlaymode;
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

        private static bool ShowGONetWarning_ShouldPreventEnteringPlaymode(string fileContents)
        {
            // Create the warning message
            string warningMessage = "WARNING: GONet will not function properly until you create another build, because the server and all clients are required to have the same information as it pertains to all the things that are going to be networked.\n\n" +
                                    "Please review the following reasons (i.e., things that changed during design-time since the last build):\n\n" +
                                    fileContents;

            // Show a dialog with the warning and file contents
            bool didProceedAnyway = 
                EditorUtility.DisplayDialog(
                    "GONet Warning", warningMessage, 
                    "Proceed Anyway (*NOT* Recommended)", 
                    "Cancel, I'll Rebuild First (Please and Thank You!)", 
                    DialogOptOutDecisionType.ForThisSession, "GONet Enter Playe Mode Build Warning");

            return !didProceedAnyway;
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

#if ADDRESSABLES_AVAILABLE
            // Also clear addressables session tracking when builds succeed
            ClearAddressablesSessionTracking();
#endif

            // Save content snapshot if team-aware dirty checking is enabled
            if (IsTeamAwareDirtyCheckingEnabled())
            {
                UnityEngine.Debug.Log("GONet: Team-aware dirty checking is enabled, saving content snapshot...");
                SaveContentSnapshotAfterBuild();
            }
            else
            {
                UnityEngine.Debug.Log("GONet: Team-aware dirty checking is disabled, skipping content snapshot.");
            }
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

                // Force immediate disk flush to prevent race conditions with play mode transition
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Flush(true); // Force OS to flush to disk
                }

                // Additional safety: Brief pause to ensure file system operations complete
                // before any potential play mode transition that might check for file existence
                System.Threading.Thread.Sleep(10);

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

            // Collect the design time locations for each GONetParticipant (includes addressables paths)
            HashSet<string> allPathsToGnpsInProject = new();
            foreach (var gonetParticipant in projectGnps)
            {
                string designTimeLocation = gonetParticipant.DesignTimeLocation;
                if (!string.IsNullOrEmpty(designTimeLocation))
                {
                    allPathsToGnpsInProject.Add(designTimeLocation);
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

                //GONetLog.Debug($"SLEEPS RESOURCES gnp:{DesignTimeMetadata.GetFullPath(gonetParticipant)}");
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

        /// <summary>
        /// Scene-specific version that only compares against objects from the same scenes being scanned.
        /// This prevents false positives when scanning only currently loaded scenes.
        /// </summary>
        private static void ProcessAnyDesignTimeDirty_IfAppropriate_SceneSpecific(HashSet<string> fullPathsToDesignTimeGnps, HashSet<string> sceneNames)
        {
            // Skip change detection only if we're actively building
            // During builds, metadata caching may not be complete, leading to false positives
            if (BuildPipeline.isBuildingPlayer)
            {
                GONetLog.Debug($"Skipping design-time dirty detection during build: isBuildingPlayer={BuildPipeline.isBuildingPlayer}");
                return;
            }

            // Skip if we have zero current paths but metadata caching isn't complete yet
            // This prevents false "everything was removed" during editor startup/domain reload
            if (fullPathsToDesignTimeGnps.Count == 0 && !GONetSpawnSupport_Runtime.IsDesignTimeMetadataCached)
            {
                GONetLog.Debug($"Skipping design-time dirty detection: Found 0 current paths but metadata caching isn't complete yet");
                return;
            }

            // compare this list and the current metadata associated with all these gnps to that stored in the last build's metadata..if different TAKE NOTE and refer to this when entering play mode and show warning!
            IEnumerable<DesignTimeMetadata> designTimeMetadatasFromLastBuild =
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();

            // Filter to only include scene paths from the specific scenes we're scanning
            HashSet<string> pathsFromLastBuildInTheseScenes = new HashSet<string>();
            foreach (var metadata in designTimeMetadatasFromLastBuild)
            {
                if (metadata.Location.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX))
                {
                    // Extract scene name from path like "scene://SceneName/GameObject/Path"
                    string locationAfterPrefix = metadata.Location.Substring(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX.Length);
                    int firstSlashIndex = locationAfterPrefix.IndexOf('/');
                    if (firstSlashIndex > 0)
                    {
                        string sceneNameFromPath = locationAfterPrefix.Substring(0, firstSlashIndex);
                        if (sceneNames.Contains(sceneNameFromPath))
                        {
                            pathsFromLastBuildInTheseScenes.Add(metadata.Location);
                        }
                    }
                }
            }

            GONetLog.Debug($"Filtered last build paths to {pathsFromLastBuildInTheseScenes.Count} paths from scenes: {string.Join(", ", sceneNames)}");

            // Compare the current GNP paths to the previous build's metadata (only from the same scenes)
            foreach (var currentPath in fullPathsToDesignTimeGnps)
            {
                if (!pathsFromLastBuildInTheseScenes.Contains(currentPath))
                {
                    AddGONetDesignTimeDirtyReason($"GONetParticipant at {currentPath} was added or modified after the last build.");
                }
            }

            // Check for prefabs that were removed from the current scanning (only within the same scenes)
            foreach (var lastBuildPath in pathsFromLastBuildInTheseScenes)
            {
                if (!fullPathsToDesignTimeGnps.Contains(lastBuildPath))
                {
                    GONetLog.Debug($"Confirmed removal for scene path {lastBuildPath}");
                    AddGONetDesignTimeDirtyReason($"GONetParticipant prefab removed from scene: {lastBuildPath}");
                }
            }
        }

        private static void ProcessAnyDesignTimeDirty_IfAppropriate(HashSet<string> fullPathsToDesignTimeGnps)
        {
            // Skip change detection only if we're actively building
            // During builds, metadata caching may not be complete, leading to false positives
            if (BuildPipeline.isBuildingPlayer)
            {
                GONetLog.Debug($"Skipping design-time dirty detection during build: isBuildingPlayer={BuildPipeline.isBuildingPlayer}");
                return;
            }

            // Skip if we have zero current paths but metadata caching isn't complete yet
            // This prevents false "everything was removed" during editor startup/domain reload
            // Note: During normal addressables operations, we now wait for caching to complete before calling this method
            if (fullPathsToDesignTimeGnps.Count == 0 && !GONetSpawnSupport_Runtime.IsDesignTimeMetadataCached)
            {
                GONetLog.Debug($"Skipping design-time dirty detection: Found 0 current paths but metadata caching isn't complete yet");
                return;
            }

            // compare this list and the current metadata associated with all these gnps to that stored in the last build's metadata..if different TAKE NOTE and refer to this when entering play mode and show warning!
            IEnumerable<DesignTimeMetadata> designTimeMetadatasFromLastBuild =
                GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();
            // HashSet for fast lookup of the GNP paths from the last build's metadata
            HashSet<string> pathsFromLastBuild = new HashSet<string>(
                designTimeMetadatasFromLastBuild.Select(metadata => metadata.Location));

            // Compare the current GNP paths to the previous build's metadata
            foreach (var currentPath in fullPathsToDesignTimeGnps)
            {
                bool foundMatch = pathsFromLastBuild.Contains(currentPath);

                // Check for backwards compatibility: resources:// vs project:// prefixes should be treated as equivalent
                if (!foundMatch && currentPath.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                {
                    // Try to find equivalent project:// path from last build
                    string equivalentProjectPath = currentPath.Replace(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX, GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX);
                    foundMatch = pathsFromLastBuild.Contains(equivalentProjectPath);
                }
                else if (!foundMatch && currentPath.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                {
                    // Try to find equivalent resources:// path from last build (edge case)
                    string equivalentResourcesPath = currentPath.Replace(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX);
                    foundMatch = pathsFromLastBuild.Contains(equivalentResourcesPath);
                }

                if (!foundMatch)
                {
                    AddGONetDesignTimeDirtyReason($"GONetParticipant at {currentPath} was added or modified after the last build.");
                }
            }

            // Determine what type of scan this is based on the current paths
            bool isSceneScan = fullPathsToDesignTimeGnps.Any(p => p.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX));
            bool isProjectScan = fullPathsToDesignTimeGnps.Any(p =>
                p.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX) ||
                p.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) ||
                p.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX));

            // Check for prefabs that were removed from the current scanning (only within the same category)
            foreach (var lastBuildPath in pathsFromLastBuild)
            {
                // Skip checking removals for categories not being scanned in this pass
                bool isLastBuildScene = lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX);
                bool isLastBuildProject = lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX) ||
                                         lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) ||
                                         lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX);

                // Only check for removals within the same scan type
                if ((isSceneScan && !isLastBuildScene) || (isProjectScan && !isLastBuildProject))
                {
                    continue; // Skip this path - wrong category for this scan
                }

                bool foundInCurrent = fullPathsToDesignTimeGnps.Contains(lastBuildPath);

                // Check for backwards compatibility when checking removals too
                if (!foundInCurrent && lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                {
                    // Try to find equivalent project:// path in current scan
                    string equivalentProjectPath = lastBuildPath.Replace(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX, GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX);
                    foundInCurrent = fullPathsToDesignTimeGnps.Contains(equivalentProjectPath);
                }
                else if (!foundInCurrent && lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                {
                    // Try to find equivalent resources:// path in current scan
                    string equivalentResourcesPath = lastBuildPath.Replace(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX);
                    foundInCurrent = fullPathsToDesignTimeGnps.Contains(equivalentResourcesPath);
                }

                if (!foundInCurrent)
                {
                    // Additional safeguard: For non-scene prefabs, check if this is a false positive by verifying
                    // the asset actually no longer exists in the project/addressables
                    if (!lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX))
                    {
                        // Extract the asset path from the prefixed path
                        string assetPath = "";
                        if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                        {
                            assetPath = lastBuildPath.Substring(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX.Length);
                        }
                        else if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                        {
                            assetPath = lastBuildPath.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length);
                        }
                        else if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                        {
                            assetPath = lastBuildPath.Substring(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX.Length);
                        }

                        // If we extracted an asset path, verify the asset doesn't actually still exist
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            // Check if the asset still exists and has a GONetParticipant
                            GONetParticipant stillExists = AssetDatabase.LoadAssetAtPath<GONetParticipant>(assetPath);
                            if (stillExists != null)
                            {
                                GONetLog.Debug($"Skipping false positive removal for prefixed path {lastBuildPath}: asset still exists at {assetPath}");
                                continue; // Skip this false positive
                            }
                        }
                    }

                    // Determine the type of removal based on the prefix
                    string removalType = "project resources";
                    if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                    {
                        removalType = "addressables";
                    }
                    else if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX) ||
                             lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                    {
                        removalType = "project resources";
                    }
                    else if (lastBuildPath.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX))
                    {
                        removalType = "scene";
                    }

                    GONetLog.Debug($"Confirmed removal for prefixed path {lastBuildPath}: type={removalType}");
                    AddGONetDesignTimeDirtyReason($"GONetParticipant prefab removed from {removalType}: {lastBuildPath}");
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
                HashSet<string> loadedSceneNames = new(); // Track which scenes we're scanning
                int count = SceneManager.loadedSceneCount;
                for (int i = 0; i < count; ++i)
                { //
                    Scene loadedScene = EditorSceneManager.GetSceneAt(i);
                    if (!IsSceneIncludedInBuild(loadedScene.path)) continue; // only consider scene changes when scene is included in the build since GONet does not care otherwise

                    const string SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES = "/";
                    string scenePrefix = string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, loadedScene.name, SLASHY_LITTLE_WALLACE_PREVENTS_DELETING_SIMILARLY_NAMED_SCENES);

                    loadedSceneNames.Add(loadedScene.name); // Track this scene name

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

                GONetLog.Debug($"Here are all {pathsToGnpsInScene.Count} scene GNPs in loaded scenes ({string.Join(", ", loadedSceneNames)}):\n{string.Join("\n", pathsToGnpsInScene)}");
                ProcessAnyDesignTimeDirty_IfAppropriate_SceneSpecific(pathsToGnpsInScene, loadedSceneNames);
                HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate();
            }

            static void HandlePotentialChangeInPrefabPreviewMode_ProcessAnyDesignTimeDirty_IfAppropriate()
            {
                // Update the addressable asset paths cache before processing changes
                UpdateAddressableAssetPathsCache();

                IEnumerable<DesignTimeMetadata> designTimeLocations_gonetParticipants_lastBuild =
                    GONetSpawnSupport_Runtime.LoadDesignTimeMetadataFromPersistence();
                
                // Get paths from last build for project://, resources://, and addressables:// prefixes
                IEnumerable<string> gnpPrefabAssetPaths_lastBuild =
                    designTimeLocations_gonetParticipants_lastBuild
                        .Where(x => x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX) ||
                                   x.Location.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX) ||
                                   x.Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                        .Select(x => {
                            if (x.Location.StartsWith(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX))
                                return x.Location.Substring(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX.Length);
                            else if (x.Location.StartsWith(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX))
                                return x.Location.Substring(GONetSpawnSupport_Runtime.RESOURCES_HIERARCHY_PREFIX.Length);
                            else if (x.Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                                return x.Location.Substring(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX.Length);
                            else
                                return x.Location; // Fallback for unknown prefixes
                        });

                // TODO FIXME doing this every time the hiearchy changes is crazy....mainy due to high processing time....need to attempt to move this entire method logic to be called in the other option: AssetPostprocessor.OnPostprocessAllAssets, where we hope we can more narrowly focus in on the specific data that is changing instead of searching the entire project essentially!
                //   --- UPDATE to above TODO FIXME: there is an implementation of this inside AssetPostprocessor/Magoo.OnPostprocessAllAssets (find OnPostprocessAllAssets_TakeNoteOfAnyGONetChanges), so this here can/should probably be removed as redundant and this is less performant for sure.
                List<GONetParticipant> gnpsInProjectResources =
                    GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GatherGONetParticipantsInAllResourcesFolders();

                // Also gather addressable GONetParticipants for comprehensive change detection
                List<GONetParticipant> gnpsInAddressables = GatherAddressableGONetParticipants();

                // Combine both resources and addressables paths for comprehensive comparison
                HashSet<string> allGnpPaths = new();
                allGnpPaths.UnionWith(gnpsInProjectResources.Select(g => AssetDatabase.GetAssetPath(g)));
                allGnpPaths.UnionWith(gnpsInAddressables.Select(g => AssetDatabase.GetAssetPath(g)));

                // Debug logging to understand the comparison issue
                GONetLog.Debug($"Last build paths ({gnpPrefabAssetPaths_lastBuild.Count()}): {string.Join(", ", gnpPrefabAssetPaths_lastBuild.Take(10))}");
                GONetLog.Debug($"Current paths ({allGnpPaths.Count}): {string.Join(", ", allGnpPaths.Take(10))}");

                {// Check for GNP deletes: was previously in gnpPrefabAssetPaths_lastBuild, but NOT in the updated list of gnp prefabs
                 // Check for GNP deletes: previously in gnpPrefabAssetPaths_lastBuild, but NOT in currentGnpAssetPaths
                    IEnumerable<string> deletedGnpPaths = gnpPrefabAssetPaths_lastBuild
                        .Where(path => !allGnpPaths.Contains(path));

                    foreach (string deletedPath in deletedGnpPaths)
                    {
                        // Safeguard: Check if this is a false positive - if the asset still exists in either collection,
                        // then it wasn't actually deleted, likely due to timing issues during addressables modifications
                        bool stillExistsInResources = gnpsInProjectResources.Any(g => AssetDatabase.GetAssetPath(g) == deletedPath);
                        bool stillExistsInAddressables = gnpsInAddressables.Any(g => AssetDatabase.GetAssetPath(g) == deletedPath);

                        if (stillExistsInResources || stillExistsInAddressables)
                        {
                            GONetLog.Debug($"Skipping false positive deletion for {deletedPath}: stillInResources={stillExistsInResources}, stillInAddressables={stillExistsInAddressables}");
                            continue; // Skip this false positive
                        }

                        // Check if this was an addressable asset in the last build to provide the correct message
                        bool wasAddressableAsset = WasAddressableInLastBuild(deletedPath, designTimeLocations_gonetParticipants_lastBuild);
                        string messageType = wasAddressableAsset ? "addressable" : "project resources";

                        GONetLog.Debug($"Confirmed deletion for {deletedPath}: wasAddressable={wasAddressableAsset}");
                        AddGONetDesignTimeDirtyReason($"GONetParticipant prefab deleted from {messageType}: {deletedPath}");
                    }
                }

                {// Check for GNP adds: was previously NOT in gnpPrefabAssetPaths_lastBuild, but is now in the updated list of gnp prefabs
                 // Check for GNP adds: previously NOT in gnpPrefabAssetPaths_lastBuild, but is now in currentGnpAssetPaths
                    IEnumerable<string> addedGnpPaths = allGnpPaths
                        .Where(path => !gnpPrefabAssetPaths_lastBuild.Contains(path));

                    foreach (string addedPath in addedGnpPaths)
                    {
                        // Check if this is an addressable asset to provide the correct message
                        bool isAddressableAsset = false;
#if ADDRESSABLES_AVAILABLE
                        isAddressableAsset = IsAddressableAsset(addedPath);
#endif
                        string messageType = isAddressableAsset ? "addressable" : "project resources";
                        AddGONetDesignTimeDirtyReason($"GONetParticipant prefab added to {messageType}: {addedPath}");
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
            RemoveFromPersistence_WherePrefixMatches(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX);

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

            // Scan for addressable GONetParticipant prefabs
            EnsureDesignTimeLocationsCurrent_AddressablesOnly();
        }

        internal static void OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly_Single(GONetParticipant gonetParticipant)
        {
            if (gonetParticipant != null)
            {
                string projectPath = AssetDatabase.GetAssetPath(gonetParticipant);
                bool isProjectAsset = !string.IsNullOrWhiteSpace(projectPath);
                if (isProjectAsset)
                {
#if ADDRESSABLES_AVAILABLE
                    // Check if this is an addressable asset first
                    if (IsAddressableAsset(projectPath))
                    {
                        return; // Don't create project:// entry for addressable assets
                    }
#endif

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

                    // this seems unnecessary and problematic for project assets:
                    EnsureDesignTimeLocationCurrent(gonetParticipant, currentLocation); // have to do proper unity serialization stuff for this to stick!

                    // Check and update addressables information
                    UpdateAddressablesMetadata(gonetParticipant, projectPath);

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

#if ADDRESSABLES_AVAILABLE
        /// <summary>
        /// Updates the DesignTimeMetadata for a GONetParticipant with addressables information if available.
        /// </summary>
        private static void UpdateAddressablesMetadata(GONetParticipant gonetParticipant, string assetPath)
        {
            var designTimeMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(gonetParticipant);

            try
            {
                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings == null)
                {
                    // No addressables configured - LoadType and AddressableKey are now computed from location prefix
                    return;
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                var entry = addressableSettings.FindAssetEntry(guid);

                if (entry != null && !string.IsNullOrEmpty(entry.address))
                {
                    // Asset is addressable - LoadType and AddressableKey are now computed from location prefix
                    GONetLog.Debug($"GONetParticipant '{gonetParticipant.name}' detected as addressable with key: '{entry.address}'");
                }
                else
                {
                    // Asset is not addressable - LoadType and AddressableKey are now computed from location prefix
                }
            }
            catch (System.Exception ex)
            {
                GONetLog.Warning($"Failed to check addressables status for '{assetPath}': {ex.Message}");

                // Fallback to Resources on error - LoadType and AddressableKey are now computed from location prefix
            }
        }

        private const string SESSION_STATE_ADDRESSABLES_CACHE_KEY = "GONet.AddressableAssetPaths";

#if ADDRESSABLES_AVAILABLE
        /// <summary>
        /// Registers callbacks for Addressables build events and group modifications
        /// </summary>
        private static void RegisterAddressableBuildCallbacks()
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings != null)
            {
                // Hook into build completion events
                addressableSettings.OnDataBuilderComplete += OnAddressablesBuildComplete;

                // Hook into group/entry modification events
                AddressableAssetSettings.OnModificationGlobal += OnAddressablesModification;
            }
        }

        /// <summary>
        /// Called when an Addressables build completes
        /// </summary>
        private static void OnAddressablesBuildComplete(AddressableAssetSettings settings,
                                                       IDataBuilder builder,
                                                       IDataBuilderResult result)
        {
            UnityEngine.Debug.Log("GONet: Addressables build completed, checking for GONetParticipant changes");

            // Force update of the cache after addressables build
            UpdateAddressableAssetPathsCache();

            // Delay change detection to ensure addressables system is fully updated
            EditorApplication.delayCall += () => {
                EditorApplication.delayCall += () => {
                    GONetLog.Debug("Running addressables build change detection");

                    // Note: If metadata isn't cached, the detection will use false positive protection

                    OnHierarchyChanged_TakeNoteOfAnyGONetChanges_SceneOnly();
                };
            };
        }

        /// <summary>
        /// Called when Addressables groups or entries are modified
        /// </summary>
        private static void OnAddressablesModification(AddressableAssetSettings settings,
                                                       AddressableAssetSettings.ModificationEvent eventType,
                                                       object eventData)
        {
            // Only care about entry-related modifications that might affect GONet prefabs
            if (eventType == AddressableAssetSettings.ModificationEvent.EntryAdded ||
                eventType == AddressableAssetSettings.ModificationEvent.EntryRemoved ||
                eventType == AddressableAssetSettings.ModificationEvent.EntryModified)
            {
                UnityEngine.Debug.Log($"GONet: Addressables modification detected ({eventType}), checking for GONet changes");
                UpdateAddressableAssetPathsCache();

                // Use direct detection approach that doesn't depend on metadata caching
                // Execute immediately to ensure dirty flag is set before any potential play mode transition
                ProcessAddressablesModificationDirect(eventType, eventData);
            }
        }

        /// <summary>
        /// Directly processes addressables modifications for GONetParticipant prefabs without depending on metadata caching
        /// </summary>
        private static void ProcessAddressablesModificationDirect(AddressableAssetSettings.ModificationEvent eventType, object eventData)
        {
            try
            {
                // Extract the asset path from the modification event
                string assetPath = null;
                string address = null;

                if (eventData is AddressableAssetEntry entry)
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    address = entry.address;
                }

                if (string.IsNullOrEmpty(assetPath))
                {
                    GONetLog.Debug($"Could not extract asset path from addressables modification event");
                    return;
                }

                // Check if this asset is a GONetParticipant prefab
                if (!assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                {
                    return; // Not a prefab, ignore
                }

                GONetParticipant gnp = AssetDatabase.LoadAssetAtPath<GONetParticipant>(assetPath);
                if (gnp == null)
                {
                    return; // Not a GONetParticipant prefab, ignore
                }

                // Now we know this is a GONetParticipant prefab that was modified in addressables
                // Determine the change type and record it if not already recorded this session
                string changeType = eventType switch
                {
                    AddressableAssetSettings.ModificationEvent.EntryAdded => "added",
                    AddressableAssetSettings.ModificationEvent.EntryRemoved => "removed",
                    AddressableAssetSettings.ModificationEvent.EntryModified => "modified",
                    _ => "changed"
                };

                // Check for session deduplication - prevent recording same change multiple times
                if (WasChangeAlreadyRecordedThisSession(assetPath, changeType))
                {
                    GONetLog.Debug($"Skipping duplicate addressables change in session: {changeType} {assetPath}");
                    return;
                }

                // Record the change using our holistic dual persistence system
                RecordAddressablesChange(assetPath, changeType);
                GONetLog.Debug($"Direct addressables change detected and recorded: {changeType} {assetPath}");
            }
            catch (System.Exception ex)
            {
                GONetLog.Warning($"Error processing addressables modification: {ex.Message}");
            }
        }

        /// <summary>
        /// Records addressables changes using dual persistence: SessionState (session) + File (cross-session)
        /// Integrates with GONet's existing dirty reason system for consistent behavior
        /// </summary>
        private static void RecordAddressablesChange(string assetPath, string changeType)
        {
            // Create the dirty reason message using GONet's standard format
            string actionText = changeType switch
            {
                "added" => "added to",
                "removed" => "removed from",
                "modified" => "modified in",
                _ => "changed in"
            };

            // Use the standard format consistent with other detection systems
            string locationPrefix = "addressables://";
            string dirtyReason = $"GONetParticipant at {locationPrefix}{assetPath} was added or modified after the last build.";

            // Use GONet's existing file persistence system - this handles:
            // - File creation/management
            // - Timestamp formatting
            // - Duplicate detection
            // - Cross-session persistence
            AddGONetDesignTimeDirtyReason(dirtyReason);

            // ALSO store in SessionState for this-session deduplication and fast access
            // This prevents us from detecting the same change multiple times within a session
            const string ADDRESSABLES_SESSION_KEY = "GONet.AddressablesSession";
            string sessionKey = $"{changeType}:{assetPath}";

            string existingSession = SessionState.GetString(ADDRESSABLES_SESSION_KEY, "");
            if (string.IsNullOrEmpty(existingSession))
            {
                SessionState.SetString(ADDRESSABLES_SESSION_KEY, sessionKey);
            }
            else if (!existingSession.Contains(sessionKey))
            {
                SessionState.SetString(ADDRESSABLES_SESSION_KEY, $"{existingSession};{sessionKey}");
            }

            GONetLog.Debug($"Recorded addressables change: {dirtyReason}");
        }

        /// <summary>
        /// Checks if this addressables change was already recorded in this session
        /// Prevents duplicate detection within the same editor session
        /// </summary>
        private static bool WasChangeAlreadyRecordedThisSession(string assetPath, string changeType)
        {
            const string ADDRESSABLES_SESSION_KEY = "GONet.AddressablesSession";
            string sessionData = SessionState.GetString(ADDRESSABLES_SESSION_KEY, "");
            string changeKey = $"{changeType}:{assetPath}";

            return !string.IsNullOrEmpty(sessionData) && sessionData.Contains(changeKey);
        }

        /// <summary>
        /// Clears session-specific addressables tracking
        /// Called after successful builds to reset session state
        /// Note: File persistence is handled by GONet's existing build completion system
        /// </summary>
        private static void ClearAddressablesSessionTracking()
        {
            const string ADDRESSABLES_SESSION_KEY = "GONet.AddressablesSession";
            SessionState.EraseString(ADDRESSABLES_SESSION_KEY);
            GONetLog.Debug("Cleared addressables session tracking");
        }
#endif

        /// <summary>
        /// Updates the cache of addressable asset paths using Unity's SessionState for domain-reload safety
        /// </summary>
        private static void UpdateAddressableAssetPathsCache()
        {
            var cachedPaths = new List<string>();

            var allGNPs = GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GatherGONetParticipantsInAllResourcesFolders();
            foreach (var gnp in allGNPs)
            {
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(gnp);
                if (!string.IsNullOrEmpty(assetPath) && !assetPath.Contains("/Resources/"))
                {
                    // This must be an addressable since it's not in Resources but was found by the gather method
                    cachedPaths.Add(assetPath);
                }
            }

            // Store in SessionState using JSON serialization for the list
            string json = JsonUtility.ToJson(new SerializableStringList { items = cachedPaths.ToArray() });
            SessionState.SetString(SESSION_STATE_ADDRESSABLES_CACHE_KEY, json);

            // UnityEngine.Debug.Log($"UpdateAddressableAssetPathsCache: Cached {cachedPaths.Count} addressable asset paths in SessionState");
        }

        [System.Serializable]
        private class SerializableStringList
        {
            public string[] items;
        }

        /// <summary>
        /// Gets the cached addressable asset paths from Unity's SessionState
        /// </summary>
        private static HashSet<string> GetCachedAddressableAssetPaths()
        {
            var result = new HashSet<string>();

            string json = SessionState.GetString(SESSION_STATE_ADDRESSABLES_CACHE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var data = JsonUtility.FromJson<SerializableStringList>(json);
                    if (data?.items != null)
                    {
                        foreach (var item in data.items)
                        {
                            result.Add(item);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to deserialize addressable cache from SessionState: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the given asset path was an addressable in the last build based on stored metadata
        /// </summary>
        private static bool WasAddressableInLastBuild(string assetPath, IEnumerable<DesignTimeMetadata> lastBuildMetadata)
        {
            return lastBuildMetadata.Any(x =>
                x.Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX) &&
                x.Location.Substring(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX.Length) == assetPath);
        }

        /// <summary>
        /// Checks if the given asset path is configured as an addressable asset
        /// </summary>
        private static bool IsAddressableAsset(string assetPath)
        {
            // Check if the asset is not in a Resources folder (which would make it Resources-based)
            if (assetPath.Contains("/Resources/"))
            {
                return false;
            }

            // Check the SessionState cache that gets populated when GatherGONetParticipantsInAllResourcesFolders runs
            var cachedPaths = GetCachedAddressableAssetPaths();
            return cachedPaths.Contains(assetPath);
        }

        /// <summary>
        /// Scans for addressable GONetParticipant prefabs and creates metadata entries with ADDRESSABLES_HIERARCHY_PREFIX
        /// </summary>
        internal static void EnsureDesignTimeLocationsCurrent_AddressablesOnly()
        {
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings == null)
            {
                return;
            }

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
                        GONetParticipant prefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(assetPath);
                        if (prefab != null)
                        {
                            // Found an addressable GONetParticipant prefab
                            string addressableLocation = string.Concat(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX, entry.address);

                            // Create or update design time metadata
                            var designTimeMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(prefab);
                            if (designTimeMetadata != null)
                            {
                                designTimeMetadata.Location = addressableLocation;
                                designTimeMetadata.UnityGuid = entry.guid;

                                // Ensure it's in the persistence system
                                EnsureExistsInPersistence_WithTheseValues(addressableLocation);
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
                            GONetParticipant prefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(prefabPath);
                            if (prefab != null)
                            {
                                // For folders, use the full prefab filename (including .prefab extension) as the addressable key
                                string prefabFileName = System.IO.Path.GetFileName(prefabPath);
                                string addressableKey = string.Concat(entry.address, "/", prefabFileName);
                                string addressableLocation = string.Concat(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX, addressableKey);

                                // Create or update design time metadata
                                var designTimeMetadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(prefab);
                                if (designTimeMetadata != null)
                                {
                                    designTimeMetadata.Location = addressableLocation;
                                    designTimeMetadata.UnityGuid = prefabGuid;

                                    // Ensure it's in the persistence system
                                    EnsureExistsInPersistence_WithTheseValues(addressableLocation);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gathers all GONetParticipant prefabs that are currently marked as addressable assets.
        /// This is used for change detection to identify when prefabs are added/removed from addressables.
        /// </summary>
        /// <returns>List of GONetParticipant components from addressable prefabs</returns>
        internal static List<GONetParticipant> GatherAddressableGONetParticipants()
        {
            List<GONetParticipant> addressableGNPs = new List<GONetParticipant>();

            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings == null)
            {
                return addressableGNPs;
            }

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
                        GONetParticipant prefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(assetPath);
                        if (prefab != null)
                        {
                            addressableGNPs.Add(prefab);
                        }
                    }
                    // Check if it's a folder - scan for prefabs inside
                    else if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });

                        foreach (string prefabGuid in prefabGuids)
                        {
                            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                            GONetParticipant prefab = AssetDatabase.LoadAssetAtPath<GONetParticipant>(prefabPath);
                            if (prefab != null)
                            {
                                addressableGNPs.Add(prefab);
                            }
                        }
                    }
                }
            }

            return addressableGNPs;
        }
#else
        /// <summary>
        /// Fallback when Addressables is not available - no addressables scanning needed.
        /// </summary>
        internal static void EnsureDesignTimeLocationsCurrent_AddressablesOnly()
        {
            // No addressables support, nothing to scan
        }

        /// <summary>
        /// Fallback when Addressables is not available - returns empty list of addressable GONetParticipants.
        /// </summary>
        /// <returns>Empty list since addressables is not available</returns>
        internal static List<GONetParticipant> GatherAddressableGONetParticipants()
        {
            return new List<GONetParticipant>();
        }

        /// <summary>
        /// Fallback when Addressables is not available - ensures metadata uses Resources load type.
        /// </summary>
        private static void UpdateAddressablesMetadata(GONetParticipant gonetParticipant, string assetPath)
        {
            // LoadType and AddressableKey are now computed from location prefix - no need to set them
        }
#endif

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

        /// <summary>
        /// Saves a content snapshot after a successful build (async operation, fire and forget)
        /// </summary>
        private static async void SaveContentSnapshotAfterBuild()
        {
            try
            {
                UnityEngine.Debug.Log("GONet: Creating content snapshot after successful build...");

                var snapshot = await GONetContentSnapshot.CreateSnapshotAsync();
                string snapshotPath = GetContentSnapshotFilePath();

                GONetContentSnapshot.SaveSnapshot(snapshot, snapshotPath);

                UnityEngine.Debug.Log($"GONet: Content snapshot saved to {snapshotPath}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"GONet: Failed to save content snapshot after build: {ex.Message}");
                UnityEngine.Debug.LogException(ex);
            }
        }
    }
}
