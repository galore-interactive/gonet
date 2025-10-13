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
using MemoryPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet.Editor
{
    #region Data Structures (Non-nested for MemoryPack compatibility)

    [Serializable]
    [MemoryPackable]
    public partial class ContentSnapshot
    {
        public DateTime SnapshotTime;
        public Dictionary<string, string> PrefabContentHashes = new Dictionary<string, string>();
        public Dictionary<string, string> SceneContentHashes = new Dictionary<string, string>();
        public Dictionary<string, string> SyncProfileHashes = new Dictionary<string, string>();
        public Dictionary<string, string> ConfigObjectHashes = new Dictionary<string, string>();
        public Dictionary<string, bool> AddressableSettings = new Dictionary<string, bool>();
    }

    [Serializable]
    [MemoryPackable]
    public partial class GONetPrefabData
    {
        public string PrefabPath;
        public string GameObjectName;
        public GONetParticipantData ParticipantData;
        public List<ComponentSyncData> ComponentSyncSettings = new List<ComponentSyncData>();
    }

    [Serializable]
    [MemoryPackable]
    public partial class GONetParticipantData
    {
        public string UnityGuid;
        public string DesignTimeLocation;
        public bool IsPositionSyncd;
        public bool IsRotationSyncd;
        // Add other GONet-specific fields as needed
    }

    [Serializable]
    [MemoryPackable]
    public partial class ComponentSyncData
    {
        public string ComponentTypeName;
        public List<SyncMemberData> SyncMembers = new List<SyncMemberData>();
    }

    [Serializable]
    [MemoryPackable]
    public partial class SyncMemberData
    {
        public string MemberName;
        public string MemberTypeName;
        public bool IsField;
        public string SettingsProfileTemplateName;
        public float SyncChangesEverySeconds;
        public bool ShouldBlendBetweenValuesReceived;
        // Add other sync attribute properties as needed
    }

    [Serializable]
    [MemoryPackable]
    public partial class GONetSceneData
    {
        public string ScenePath;
        public string SceneName;
        public List<SceneGONetParticipantData> Participants = new List<SceneGONetParticipantData>();
    }

    [Serializable]
    [MemoryPackable]
    public partial class SceneGONetParticipantData
    {
        public string GameObjectName;
        public string HierarchyPath;
        public GONetParticipantData ParticipantData;
    }

    #endregion

    /// <summary>
    /// High-performance, multi-threaded content snapshot system for team-aware dirty checking.
    /// Extracts and hashes only GONet-relevant content to detect meaningful changes from teammates.
    /// </summary>
    public static class GONetContentSnapshot
    {
        #region Public API

        /// <summary>
        /// Creates a complete content snapshot of all GONet-relevant data using multi-threading for performance.
        /// </summary>
        public static async Task<ContentSnapshot> CreateSnapshotAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var snapshot = new ContentSnapshot
            {
                SnapshotTime = DateTime.UtcNow
            };

            // First, gather all file paths and addressable settings on the main thread (required for AssetDatabase calls)
            var prefabPaths = GetAllGONetPrefabPaths().ToArray();
            var scenePaths = GetAllGONetScenePaths().ToArray();
            var profilePaths = GetAllSyncProfilePaths().ToArray();
            var addressableSettings = GatherAddressableSettings(prefabPaths);

            // CRITICAL: Extract scene data on main thread BEFORE Task.Run() because EditorSceneManager APIs require main thread
            var sceneDataList = ExtractAllSceneData(scenePaths);

            UnityEngine.Debug.Log($"GONet: Content snapshot discovery - Prefabs: {prefabPaths.Length}, Scenes: {scenePaths.Length}, Profiles: {profilePaths.Length}, Addressable Settings: {addressableSettings.Count}");

            // Then run content analysis in parallel on background threads
            var tasks = new List<Task>
            {
                Task.Run(() => PopulatePrefabHashesFromPaths(snapshot, prefabPaths)),
                Task.Run(() => PopulateSceneHashesFromSceneData(snapshot, sceneDataList)),
                Task.Run(() => PopulateSyncProfileHashesFromPaths(snapshot, profilePaths)),
                Task.Run(() => PopulateConfigObjectHashes(snapshot)),
                Task.Run(() => PopulateAddressableSettingsFromData(snapshot, addressableSettings))
            };

            await Task.WhenAll(tasks);

            stopwatch.Stop();
            GONetLog.Debug($"GONet content snapshot created in {stopwatch.ElapsedMilliseconds}ms - " +
                          $"Prefabs: {snapshot.PrefabContentHashes.Count}, " +
                          $"Scenes: {snapshot.SceneContentHashes.Count}, " +
                          $"Profiles: {snapshot.SyncProfileHashes.Count}, " +
                          $"Configs: {snapshot.ConfigObjectHashes.Count}, " +
                          $"Addressable Settings: {snapshot.AddressableSettings.Count}");

            return snapshot;
        }

        /// <summary>
        /// Saves a content snapshot to disk using GONet's SerializationUtils.
        /// </summary>
        public static void SaveSnapshot(ContentSnapshot snapshot, string filePath)
        {
            try
            {
                byte[] serializedData = SerializationUtils.SerializeToBytes(snapshot, out int bytesUsed, out bool needsReturn);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllBytes(filePath, serializedData.Take(bytesUsed).ToArray());

                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serializedData);
                }

                GONetLog.Debug($"Content snapshot saved: {filePath} ({bytesUsed} bytes)");
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Failed to save content snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a content snapshot from disk.
        /// </summary>
        public static ContentSnapshot LoadSnapshot(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                byte[] data = File.ReadAllBytes(filePath);
                var snapshot = SerializationUtils.DeserializeFromBytes<ContentSnapshot>(data);

                GONetLog.Debug($"Content snapshot loaded: {filePath}");
                return snapshot;
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Failed to load content snapshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Compares two snapshots and returns detected changes.
        /// </summary>
        public static List<string> CompareSnapshots(ContentSnapshot current, ContentSnapshot previous)
        {
            var changes = new List<string>();

            if (previous == null)
            {
                changes.Add("No previous snapshot found - treating all content as new");
                return changes;
            }

            // Compare prefab hashes
            CompareHashDictionaries("Prefab", current.PrefabContentHashes, previous.PrefabContentHashes, changes);
            CompareHashDictionaries("Scene", current.SceneContentHashes, previous.SceneContentHashes, changes);
            CompareHashDictionaries("Sync Profile", current.SyncProfileHashes, previous.SyncProfileHashes, changes);
            CompareHashDictionaries("Config Object", current.ConfigObjectHashes, previous.ConfigObjectHashes, changes);
            CompareAddressableSettings("Addressable", current.AddressableSettings, previous.AddressableSettings, changes);

            return changes;
        }

        #endregion

        #region Private Implementation

        private static void PopulatePrefabHashes(ContentSnapshot snapshot)
        {
            var prefabPaths = GetAllGONetPrefabPaths();
            PopulatePrefabHashesFromPaths(snapshot, prefabPaths.ToArray());
        }

        private static void PopulatePrefabHashesFromPaths(ContentSnapshot snapshot, string[] prefabPaths)
        {
            var results = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(prefabPaths, prefabPath =>
            {
                try
                {
                    var prefabData = ExtractPrefabGONetData(prefabPath);
                    if (prefabData != null)
                    {
                        string hash = ComputeContentHash(prefabData);
                        results[prefabPath] = hash;
                    }
                }
                catch (Exception ex)
                {
                    GONetLog.Warning($"Error processing prefab {prefabPath}: {ex.Message}");
                }
            });

            foreach (var kvp in results)
            {
                snapshot.PrefabContentHashes[kvp.Key] = kvp.Value;
            }
        }

        private static void PopulateSceneHashes(ContentSnapshot snapshot)
        {
            var scenePaths = GetAllGONetScenePaths();
            var sceneDataList = ExtractAllSceneData(scenePaths.ToArray());
            PopulateSceneHashesFromSceneData(snapshot, sceneDataList);
        }

        /// <summary>
        /// Extracts GONet data from all scenes on the MAIN THREAD.
        /// MUST be called on main thread because EditorSceneManager APIs require main thread.
        /// </summary>
        private static List<(string path, GONetSceneData data)> ExtractAllSceneData(string[] scenePaths)
        {
            var sceneDataList = new List<(string path, GONetSceneData data)>();

            foreach (string scenePath in scenePaths)
            {
                try
                {
                    var sceneData = ExtractSceneGONetData(scenePath);
                    if (sceneData != null)
                    {
                        sceneDataList.Add((scenePath, sceneData));
                    }
                }
                catch (Exception ex)
                {
                    GONetLog.Warning($"Error extracting scene data from {scenePath}: {ex.Message}");
                }
            }

            return sceneDataList;
        }

        /// <summary>
        /// Computes hashes for pre-extracted scene data in parallel on background threads.
        /// Thread-safe because hash computation doesn't touch Unity APIs.
        /// </summary>
        private static void PopulateSceneHashesFromSceneData(ContentSnapshot snapshot, List<(string path, GONetSceneData data)> sceneDataList)
        {
            var results = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(sceneDataList, sceneDataTuple =>
            {
                try
                {
                    string hash = ComputeContentHash(sceneDataTuple.data);
                    results[sceneDataTuple.path] = hash;
                }
                catch (Exception ex)
                {
                    GONetLog.Warning($"Error computing hash for scene {sceneDataTuple.path}: {ex.Message}");
                }
            });

            foreach (var kvp in results)
            {
                snapshot.SceneContentHashes[kvp.Key] = kvp.Value;
            }
        }

        private static void PopulateSyncProfileHashes(ContentSnapshot snapshot)
        {
            var profilePaths = GetAllSyncProfilePaths();
            PopulateSyncProfileHashesFromPaths(snapshot, profilePaths.ToArray());
        }

        private static void PopulateSyncProfileHashesFromPaths(ContentSnapshot snapshot, string[] profilePaths)
        {
            var results = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(profilePaths, profilePath =>
            {
                try
                {
                    // Note: AssetDatabase.LoadAssetAtPath can be called from background threads
                    var profile = AssetDatabase.LoadAssetAtPath<GONetAutoMagicalSyncSettings_ProfileTemplate>(profilePath);
                    if (profile != null)
                    {
                        string hash = ComputeContentHash(profile);
                        results[profilePath] = hash;
                    }
                }
                catch (Exception ex)
                {
                    GONetLog.Warning($"Error processing sync profile {profilePath}: {ex.Message}");
                }
            });

            foreach (var kvp in results)
            {
                snapshot.SyncProfileHashes[kvp.Key] = kvp.Value;
            }
        }

        private static void PopulateConfigObjectHashes(ContentSnapshot snapshot)
        {
            // Add any other GONet configuration objects here
            // For now, this is a placeholder for future configuration objects
        }

        private static IEnumerable<string> GetAllGONetPrefabPaths()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var gonetPrefabPaths = new List<string>();

            // Do everything on main thread - no parallel processing here
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    // Quick check if prefab contains GONetParticipant
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null && prefab.GetComponent<GONetParticipant>() != null)
                    {
                        gonetPrefabPaths.Add(path);
                    }
                }
            }

            return gonetPrefabPaths;
        }

        private static IEnumerable<string> GetAllGONetScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .Where(SceneContainsGONetParticipants);
        }

        private static IEnumerable<string> GetAllSyncProfilePaths()
        {
            var guids = AssetDatabase.FindAssets("t:GONetAutoMagicalSyncSettings_ProfileTemplate");
            var paths = new List<string>();

            // Do everything on main thread - no LINQ with AssetDatabase calls
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        private static bool SceneContainsGONetParticipants(string scenePath)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                bool hasParticipants = false;

                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    if (rootGameObject.GetComponentsInChildren<GONetParticipant>().Length > 0)
                    {
                        hasParticipants = true;
                        break;
                    }
                }

                EditorSceneManager.CloseScene(scene, true);
                return hasParticipants;
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Error checking scene {scenePath} for GONet participants: {ex.Message}");
                return false;
            }
        }

        private static GONetPrefabData ExtractPrefabGONetData(string prefabPath)
        {
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) return null;

                var participant = prefab.GetComponent<GONetParticipant>();
                if (participant == null) return null;

                return new GONetPrefabData
                {
                    PrefabPath = prefabPath,
                    GameObjectName = prefab.name,
                    ParticipantData = ExtractParticipantData(participant),
                    ComponentSyncSettings = ExtractComponentSyncData(participant).ToList()
                };
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Error extracting prefab data from {prefabPath}: {ex.Message}");
                return null;
            }
        }

        private static GONetParticipantData ExtractParticipantData(GONetParticipant participant)
        {
            return new GONetParticipantData
            {
                UnityGuid = participant.UnityGuid,
                DesignTimeLocation = participant.DesignTimeLocation ?? string.Empty,
                IsPositionSyncd = participant.IsPositionSyncd,
                IsRotationSyncd = participant.IsRotationSyncd
            };
        }

        private static IEnumerable<ComponentSyncData> ExtractComponentSyncData(GONetParticipant participant)
        {
            var syncData = new List<ComponentSyncData>();

            // Analyze all components on the GameObject and children for sync attributes
            var allComponents = participant.GetComponentsInChildren<Component>(true);

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                var componentType = component.GetType();
                var syncMembers = new List<SyncMemberData>();

                // Check for fields and properties with GONet sync attributes
                var fields = componentType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var properties = componentType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    var attributes = field.GetCustomAttributes(typeof(GONetAutoMagicalSyncAttribute), true);
                    if (attributes.Length > 0)
                    {
                        syncMembers.Add(new SyncMemberData
                        {
                            MemberName = field.Name,
                            MemberTypeName = field.FieldType.Name,
                            IsField = true
                        });
                    }
                }

                foreach (var property in properties)
                {
                    var attributes = property.GetCustomAttributes(typeof(GONetAutoMagicalSyncAttribute), true);
                    if (attributes.Length > 0)
                    {
                        syncMembers.Add(new SyncMemberData
                        {
                            MemberName = property.Name,
                            MemberTypeName = property.PropertyType.Name,
                            IsField = false
                        });
                    }
                }

                if (syncMembers.Count > 0)
                {
                    syncData.Add(new ComponentSyncData
                    {
                        ComponentTypeName = componentType.Name,
                        SyncMembers = syncMembers
                    });
                }
            }

            return syncData;
        }

        private static GONetSceneData ExtractSceneGONetData(string scenePath)
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var participants = new List<SceneGONetParticipantData>();

                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    var sceneParticipants = rootGameObject.GetComponentsInChildren<GONetParticipant>();
                    foreach (var participant in sceneParticipants)
                    {
                        participants.Add(new SceneGONetParticipantData
                        {
                            GameObjectName = participant.gameObject.name,
                            HierarchyPath = GetHierarchyPath(participant.gameObject),
                            ParticipantData = ExtractParticipantData(participant)
                        });
                    }
                }

                EditorSceneManager.CloseScene(scene, true);

                return new GONetSceneData
                {
                    ScenePath = scenePath,
                    SceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath),
                    Participants = participants
                };
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Error extracting scene data from {scenePath}: {ex.Message}");
                return null;
            }
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            string path = gameObject.name;
            Transform parent = gameObject.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static string ComputeContentHash<T>(T data)
        {
            try
            {
                byte[] serializedData = SerializationUtils.SerializeToBytes(data, out int bytesUsed, out bool needsReturn);
                byte[] dataToHash = serializedData.Take(bytesUsed).ToArray();

                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(serializedData);
                }

                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(dataToHash);
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Error computing content hash: {ex.Message}");
                return string.Empty;
            }
        }

        private static void CompareHashDictionaries(string category, Dictionary<string, string> current, Dictionary<string, string> previous, List<string> changes)
        {
            // Check for new or modified items
            foreach (var kvp in current)
            {
                if (!previous.ContainsKey(kvp.Key))
                {
                    changes.Add($"New {category}: {kvp.Key}");
                }
                else if (previous[kvp.Key] != kvp.Value)
                {
                    changes.Add($"Modified {category}: {kvp.Key}");
                }
            }

            // Check for deleted items
            foreach (var kvp in previous)
            {
                if (!current.ContainsKey(kvp.Key))
                {
                    changes.Add($"Deleted {category}: {kvp.Key}");
                }
            }
        }

        private static void CompareAddressableSettings(string category, Dictionary<string, bool> current, Dictionary<string, bool> previous, List<string> changes)
        {
            // Check for new or modified addressable settings
            foreach (var kvp in current)
            {
                if (!previous.ContainsKey(kvp.Key))
                {
                    string action = kvp.Value ? "marked as addressable" : "marked as non-addressable";
                    changes.Add($"New prefab {action}: {kvp.Key}");
                }
                else if (previous[kvp.Key] != kvp.Value)
                {
                    string action = kvp.Value ? "marked as addressable" : "unmarked as addressable";
                    changes.Add($"Prefab {action}: {kvp.Key}");
                }
            }

            // Check for deleted prefabs
            foreach (var kvp in previous)
            {
                if (!current.ContainsKey(kvp.Key))
                {
                    changes.Add($"Deleted prefab: {kvp.Key}");
                }
            }
        }

        /// <summary>
        /// Gathers addressable settings for all prefab paths on the main thread.
        /// This must be done on the main thread due to Addressables API restrictions.
        /// </summary>
        private static Dictionary<string, bool> GatherAddressableSettings(string[] prefabPaths)
        {
            var addressableSettings = new Dictionary<string, bool>();

#if ADDRESSABLES_AVAILABLE
            try
            {
                var addressablesConfig = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
                if (addressablesConfig == null)
                {
                    // No addressables configuration - all prefabs are non-addressable
                    foreach (var path in prefabPaths)
                    {
                        addressableSettings[path] = false;
                    }
                    return addressableSettings;
                }

                // Check each prefab path to see if it's configured as addressable
                foreach (var prefabPath in prefabPaths)
                {
                    try
                    {
                        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            var entry = addressablesConfig.FindAssetEntry(guid);
                            addressableSettings[prefabPath] = entry != null;
                        }
                        else
                        {
                            addressableSettings[prefabPath] = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        GONetLog.Warning($"Error checking addressable status for {prefabPath}: {ex.Message}");
                        addressableSettings[prefabPath] = false; // Default to non-addressable on error
                    }
                }
            }
            catch (Exception ex)
            {
                GONetLog.Warning($"Error gathering addressable settings: {ex.Message}");
                // Default all to non-addressable on error
                foreach (var path in prefabPaths)
                {
                    addressableSettings[path] = false;
                }
            }
#else
            // Addressables not available - all prefabs are non-addressable
            foreach (var path in prefabPaths)
            {
                addressableSettings[path] = false;
            }
#endif

            return addressableSettings;
        }

        private static void PopulateAddressableSettingsFromData(ContentSnapshot snapshot, Dictionary<string, bool> addressableData)
        {
            // Simple copy - no threading issues since this is just copying data
            foreach (var kvp in addressableData)
            {
                snapshot.AddressableSettings[kvp.Key] = kvp.Value;
            }
        }

        #endregion
    }
}