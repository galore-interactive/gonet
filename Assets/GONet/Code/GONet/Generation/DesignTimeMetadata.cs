using GONet.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using GONetCodeGenerationId = System.Byte;

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
                GONetLog.Debug($"codeGenerationId: {value}, location: {location}");
            }
        }

        public override int GetHashCode()
        {
            return Location == null ? base.GetHashCode() : Location.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is DesignTimeMetadata otherMetadata))
            {
                return false;
            }

            return string.Equals(Location ?? string.Empty, otherMetadata.Location ?? string.Empty, StringComparison.Ordinal);
        }

        public static implicit operator DesignTimeMetadata(string location)
        {
            DesignTimeMetadata metadata = GONetSpawnSupport_Runtime.GetDesignTimeMetadata(location);
            metadata.Location = location;
            return metadata;
        }

        public static implicit operator string(DesignTimeMetadata metadata) => metadata.Location;

        public static string GetFullUniquePathInScene(GONetParticipant gnpInScene)
        {
            return string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, HierarchyUtils.GetFullUniquePath(gnpInScene.gameObject));

        }
    }

    [Serializable]
    public class DesignTimeMetadataLibrary
    {
        public DesignTimeMetadata[] Entries;
    }
}
