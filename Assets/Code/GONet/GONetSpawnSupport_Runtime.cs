using GONet.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet
{
    public static class GONetSpawnSupport_Runtime
    {
        public const string SCENE_HIERARCHY_PREFIX = "scene://";
        public const string PROJECT_HIERARCHY_PREFIX = "project://";
        const string RESOURCES = "Resources/";

        private static readonly Dictionary<string, GONetParticipant> designTimeLocationToProjectTemplate = new Dictionary<string, GONetParticipant>(100);

        public static void CacheAllProjectDesignTimeLocations()
        {
            foreach (var gnp in Resources.FindObjectsOfTypeAll<GONetParticipant>())
            {
                if (gnp.designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
                    GONetParticipant template = LookupResourceTemplateFromProjectLocation(gnp.designTimeLocation.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty));
                    if ((object)template != null)
                    {
                        designTimeLocationToProjectTemplate[gnp.designTimeLocation] = template;
                    }
                }
            }
        }

        private static GONetParticipant LookupResourceTemplateFromProjectLocation(string projectLocation)
        {
            if (projectLocation.Contains(RESOURCES))
            {
                string resourceLocation = projectLocation.Substring(projectLocation.IndexOf(RESOURCES) + RESOURCES.Length);
                GONetParticipant gonetParticipant = Resources.Load<GONetParticipant>(resourceLocation);
                return gonetParticipant;
            }
            else
            {
                GONetLog.Warning("magoo....cannot find non-Resources assets at runtime.  project location: " + projectLocation);
                return null;
            }
        }

        public static GONetParticipant LookupFromDesignTimeLocation(string designTimeLocation)
        {
            if (designTimeLocation.StartsWith(SCENE_HIERARCHY_PREFIX))
            {
                string fullUniquePath = designTimeLocation.Replace(SCENE_HIERARCHY_PREFIX, string.Empty);
                return HierarchyUtils.FindByFullUniquePath(fullUniquePath).GetComponent<GONetParticipant>();
            }
            else if (designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
            {
                return designTimeLocationToProjectTemplate[designTimeLocation];
            }

            throw new ArgumentException("Must include supported prefix defined as const herein.", nameof(designTimeLocation));
        }
    }
}
