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

using UnityEngine;

namespace GONet.Editor
{
    /// <summary>
    /// Project-wide settings for GONet development and build processes.
    /// This ScriptableObject should be placed under the Assets/GONet folder.
    /// </summary>
    [CreateAssetMenu(fileName = "GONetProjectSettings", menuName = "GONet/Project Settings", order = 1)]
    public class GONetProjectSettings : ScriptableObject
    {
        [Header("Client/Server Auto-Detection")]
        [Tooltip("Enable automatic client/server role detection based on port availability.\n\n" +
                "When enabled:\n" +
                "• First instance (port free) → Starts as SERVER\n" +
                "• Additional instances (port occupied) → Start as CLIENTS\n" +
                "• Command line args (-server/-client) always override auto-detection\n\n" +
                "This is ideal for local development and testing, eliminating the need for manual role selection.\n\n" +
                "When disabled:\n" +
                "• You must explicitly specify -server or -client via command line arguments\n" +
                "• Or use the manual keyboard shortcuts (Ctrl+Alt+S for server, Ctrl+Alt+C for client)\n\n" +
                "Default: Enabled (recommended for development workflow)")]
        public bool enableAutoRoleDetection = true;

        [Header("⚠️ EXPERIMENTAL FEATURES - Use with caution")]
        [Space(5)]
        [Header("Team Development Settings")]
        [Tooltip("⚠️ EXPERIMENTAL FEATURE - Changes take effect immediately during editor session.\n\n" +
                "Enable team-aware dirty checking that uses content hashing to detect actual GONet-related changes from teammates.\n\n" +
                "✅ ENABLE for team development: Detects when teammates make GONet changes that require rebuilding\n" +
                "❌ DISABLE for solo development: Uses faster timestamp-based checking (default)\n\n" +
                "When enabled, the system performs deep content analysis of GONet prefabs, sync settings, and scenes to detect only meaningful changes. " +
                "This is more accurate for team environments but requires additional processing time.\n\n" +
                "Note: This feature is experimental and may need refinement based on team usage patterns.")]
        public bool enableTeamAwareDirtyChecking = false;

        [Header("Performance Settings")]
        [Tooltip("Maximum number of parallel threads to use for content analysis.\n" +
                "Set to 0 to use system default (recommended).\n" +
                "Higher values may improve performance on multi-core systems but use more CPU.")]
        [Range(0, 16)]
        public int maxParallelThreads = 0;

        [Header("Debug Settings")]
        [Tooltip("Enable verbose logging for team-aware dirty checking operations.\n" +
                "Useful for debugging but may generate significant log output.")]
        public bool enableVerboseLogging = false;

        #region Singleton Access

        private static GONetProjectSettings _instance;
        private static bool _hasSearchedForInstance = false;

        /// <summary>
        /// Gets the project settings instance. Returns null if not found.
        /// </summary>
        public static GONetProjectSettings Instance
        {
            get
            {
                if (_instance == null && !_hasSearchedForInstance)
                {
                    _hasSearchedForInstance = true;
                    _instance = FindProjectSettingsAsset();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Searches for the GONetProjectSettings asset in the project.
        /// </summary>
        private static GONetProjectSettings FindProjectSettingsAsset()
        {
            // First try to find it directly under Assets/GONet/
            var directPath = "Assets/GONet/GONetProjectSettings.asset";
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<GONetProjectSettings>(directPath);
            if (settings != null)
                return settings;

            // Fall back to searching anywhere in the project
            var guids = UnityEditor.AssetDatabase.FindAssets("t:GONetProjectSettings");
            if (guids.Length > 0)
            {
                if (guids.Length > 1)
                {
                    GONet.GONetLog.Warning($"Found multiple GONetProjectSettings assets. Using the first one found. Consider having only one GONetProjectSettings asset in your project.");
                }

                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GONetProjectSettings>(path);
            }

            return null;
        }

        /// <summary>
        /// Creates a default GONetProjectSettings asset if none exists.
        /// </summary>
        [UnityEditor.MenuItem("GONet/Create Project Settings", priority = 1)]
        public static void CreateDefaultProjectSettings()
        {
            if (Instance != null)
            {
                UnityEditor.EditorUtility.DisplayDialog("GONet Project Settings",
                    "GONetProjectSettings already exists in the project.", "OK");
                UnityEditor.Selection.activeObject = Instance;
                return;
            }

            CreateProjectSettingsAsset();
        }

        /// <summary>
        /// Test menu item to verify team-aware dirty checking is working
        /// </summary>
        [UnityEditor.MenuItem("GONet/Debug/Test Team-Aware Settings", priority = 100)]
        public static void TestTeamAwareSettings()
        {
            var settings = Instance;
            if (settings == null)
            {
                UnityEngine.Debug.LogError("GONet: No project settings found!");
                return;
            }

            UnityEngine.Debug.Log($"GONet Project Settings Test:");
            UnityEngine.Debug.Log($"  - Settings found: {settings != null}");
            UnityEngine.Debug.Log($"  - enableTeamAwareDirtyChecking: {settings.enableTeamAwareDirtyChecking}");
            UnityEngine.Debug.Log($"  - maxParallelThreads: {settings.maxParallelThreads}");
            UnityEngine.Debug.Log($"  - enableVerboseLogging: {settings.enableVerboseLogging}");

            // Test the detection method
            var isEnabled = GONet.Editor.GONetSpawnSupport_DesignTime.IsTeamAwareDirtyCheckingEnabled();
            UnityEngine.Debug.Log($"  - IsTeamAwareDirtyCheckingEnabled(): {isEnabled}");
        }

        /// <summary>
        /// Creates the GONetProjectSettings asset.
        /// </summary>
        public static GONetProjectSettings CreateProjectSettingsAsset()
        {
            var settings = CreateInstance<GONetProjectSettings>();
            var assetPath = "Assets/GONet/GONetProjectSettings.asset";

            UnityEditor.AssetDatabase.CreateAsset(settings, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            UnityEditor.Selection.activeObject = settings;
            GONet.GONetLog.Debug($"GONetProjectSettings created at: {assetPath}");

            // Update cached instance
            _instance = settings;
            return settings;
        }

        #endregion

        #region Change Detection and Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            maxParallelThreads = Mathf.Clamp(maxParallelThreads, 0, 16);

            // Notify that settings have changed
            NotifySettingsChanged();
        }

        /// <summary>
        /// Called when settings values change in the inspector
        /// </summary>
        private void NotifySettingsChanged()
        {
            // Force refresh of cached instance so changes are picked up immediately
            _instance = this;

            if (enableVerboseLogging)
            {
                GONet.GONetLog.Debug($"GONet Project Settings changed: enableTeamAwareDirtyChecking={enableTeamAwareDirtyChecking}");
            }
        }

        #endregion
    }
}