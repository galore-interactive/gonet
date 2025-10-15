using GONet.Utils;
using System;
using UnityEngine;
using GONetCodeGenerationId = System.Byte;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace GONet.Generation
{
    [Serializable]
    public class DesignTimeMetadata
    {
        [SerializeField] private string location;
        public string Location
        {
            get => location ?? string.Empty;
            set
            {
                string previous = location;
                location = value;
                GONetSpawnSupport_Runtime.ChangeLocation(previous, value, this);
                //GONetLog.Debug($"assignment genId: {codeGenerationId}, *location: {value}");
            }
        }

        [SerializeField] private string unityGuid;
        public string UnityGuid
        {
            get => unityGuid ?? string.Empty;
            set => unityGuid = value;
        }

        [SerializeField] private GONetCodeGenerationId codeGenerationId;
        public GONetCodeGenerationId CodeGenerationId
        {
            get => codeGenerationId;
            set
            {
                codeGenerationId = value;
                //GONetLog.Debug($"assignment *genId: {value}, location: {location}");
            }
        }

        /// <summary>
        /// Gets the resource load type based on the location prefix.
        /// Supports backwards compatibility: project:// and resources:// both map to Resources.
        /// </summary>
        public ResourceLoadType LoadType
        {
            get
            {
                if (Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                    return ResourceLoadType.Addressables;
                else
                    return ResourceLoadType.Resources; // Both project:// and resources:// prefixes use Resources loading
            }
        }

        /// <summary>
        /// Gets the addressable key from the location (for addressables only)
        /// </summary>
        public string AddressableKey
        {
            get
            {
                if (Location.StartsWith(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX))
                    return Location.Substring(GONetSpawnSupport_Runtime.ADDRESSABLES_HIERARCHY_PREFIX.Length);
                else
                    return string.Empty;
            }
        }

        public override int GetHashCode()
        {
            if (!string.IsNullOrEmpty(UnityGuid))
            {
                return UnityGuid.GetHashCode();
            }

            return string.IsNullOrEmpty(Location) ? base.GetHashCode() : Location.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is DesignTimeMetadata otherMetadata))
            {
                return false;
            }

            // Compare based on UnityGuid if present
            if (!string.IsNullOrEmpty(UnityGuid) && !string.IsNullOrEmpty(otherMetadata.UnityGuid))
            {
                return string.Equals(UnityGuid, otherMetadata.UnityGuid, StringComparison.Ordinal);
            }

            // Fallback to comparing based on Location if UnityGuid is not present
            return string.Equals(Location ?? string.Empty, otherMetadata.Location ?? string.Empty, StringComparison.Ordinal);
        }

        public static implicit operator DesignTimeMetadata(string location)
        {
            // IMPORTANT: If location is empty/null, GetDesignTimeMetadata will now return default
            // This prevents the "TON CLEETLE!" error when trying to cache empty location strings
            DesignTimeMetadata metadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(location);

            // Only set Location if it's not empty (metadata will be default if location was empty)
            if (!string.IsNullOrWhiteSpace(location) && metadata != default)
            {
                metadata.Location = location;
            }

            return metadata;
        }

        public static implicit operator string(DesignTimeMetadata metadata) => metadata?.Location ?? string.Empty;

        public static string GetFullUniquePathInScene(GONetParticipant gnpInScene)
        {
            return string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, HierarchyUtils.GetFullUniquePath(gnpInScene.gameObject));

        }

#if UNITY_EDITOR
        /// <summary>
        /// returns false if <paramref name="gnpPresumablyInProject"/> is not in correct place in project / asset database as expected for prefabs!
        /// </summary>
        /* This worked on nov 3, just before new impl below
        public static bool TryGetFullPathInProject(GONetParticipant gnpPresumablyInProject, out string fullPathInProject)
        {
            // Attempt to get the direct asset path
            string projectPath = AssetDatabase.GetAssetPath(gnpPresumablyInProject);

            // Check if it's directly an asset in the project
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                fullPathInProject = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                return true;
            }

            // Check if we're in Prefab Mode and if the object is part of that prefab
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene == gnpPresumablyInProject.gameObject.scene)
            {
                // We're in Prefab Mode, so get the path of the prefab being edited
                projectPath = prefabStage.assetPath;
                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    fullPathInProject = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                    return true;
                }
            }

            // If it's part of a prefab instance in a scene, return false
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gnpPresumablyInProject.gameObject);
            if (prefabRoot != null)
            {
                fullPathInProject = default;
                return false;
            }

            // If none of the checks succeeded, it's not a valid project asset
            fullPathInProject = default;
            return false;
        }
        */
        public static bool TryGetFullPathInProject(GONetParticipant gnpPresumablyInProject, out string fullPathInProject)
        {
            // 1. Direct Project Asset Check
            string projectPath = AssetDatabase.GetAssetPath(gnpPresumablyInProject);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                fullPathInProject = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                return true;
            }

            // 2. Full Prefab Mode Check Using PrefabStageUtility
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.scene == gnpPresumablyInProject.gameObject.scene)
            {
                // The prefab is in full Prefab Mode (separate scene context)
                projectPath = prefabStage.assetPath;
                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    fullPathInProject = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                    return true;
                }
            }

            // 3. Prefab Preview Mode Check (Edit in Context Mode)
            GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(gnpPresumablyInProject.gameObject);
            if (prefabAsset != null)
            {
                // We are in Prefab Preview Mode if `prefabAsset` has a valid project path and we aren't in a full Prefab Mode stage
                projectPath = AssetDatabase.GetAssetPath(prefabAsset);
                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    fullPathInProject = string.Concat(GONetSpawnSupport_Runtime.PROJECT_HIERARCHY_PREFIX, projectPath);
                    return true;
                }
            }

            // 4. Prefab Instance in a Scene (returns false if in scene context)
            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(gnpPresumablyInProject.gameObject);
            if (prefabRoot != null)
            {
                fullPathInProject = default;
                return false;
            }

            // If none of the above checks succeeded, it's not a valid project asset
            fullPathInProject = default;
            return false;
        }
        public static string GetFullPath(GONetParticipant gnpAnywhere)
        {
            try
            {
                if (TryGetFullPathInProject(gnpAnywhere, out string fullPathInProject))
                {
                    return fullPathInProject;
                }
                
                return GetFullUniquePathInScene(gnpAnywhere);
            }
            catch
            {
                return gnpAnywhere.gameObject.name;
            }
        }
#endif
    }

    [Serializable]
    public enum ResourceLoadType : byte
    {
        Resources = 0,
        Addressables = 1
    }

    [Serializable]
    public class DesignTimeMetadataLibrary
    {
        public DesignTimeMetadata[] Entries;

        /// <summary>
        /// Gets the 16-bit index for a given design time location.
        /// This is used for bandwidth optimization in spawn events (2 bytes vs 40-80 bytes).
        /// Returns ushort.MaxValue if location not found.
        /// </summary>
        public ushort GetIndexForLocation(string location)
        {
            if (Entries == null || string.IsNullOrWhiteSpace(location))
                return ushort.MaxValue;

            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].Location == location)
                    return (ushort)i;
            }

            return ushort.MaxValue;
        }

        /// <summary>
        /// Gets the design time location for a given index.
        /// Returns null if index is invalid.
        /// </summary>
        public string GetLocationForIndex(ushort index)
        {
            if (Entries == null || index >= Entries.Length || index == ushort.MaxValue)
                return null;

            return Entries[index].Location;
        }
    }
}
