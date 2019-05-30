using GONet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GONet
{
    public static class GONetSpawnSupport_Runtime
    {
        public const string GONET_STREAMING_ASSETS_FOLDER = "GONet";
        public const string DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS = GONET_STREAMING_ASSETS_FOLDER + "/DesignTimeLocations.txt";

        public const string SCENE_HIERARCHY_PREFIX = "scene://";
        public const string PROJECT_HIERARCHY_PREFIX = "project://";
        const string RESOURCES = "Resources/";

        private static readonly Dictionary<string, GONetParticipant> designTimeLocationToProjectTemplate = new Dictionary<string, GONetParticipant>(100);

        public static void CacheAllProjectDesignTimeLocations()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS);
            if (File.Exists(fullPath))
            {
                string fileContents = File.ReadAllText(fullPath);
                foreach (string designTimeLocation in fileContents.Split(Environment.NewLine.ToCharArray()))
                {
                    if (designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
                    {
                        GONetParticipant template = LookupResourceTemplateFromProjectLocation(designTimeLocation.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty));
                        if ((object)template != null)
                        {
                            GONetLog.Debug("found template for design time location: " + designTimeLocation);
                            designTimeLocationToProjectTemplate[designTimeLocation] = template;
                        }
                    }
                }
            }

            /* does not get all the goodies, because they are not all loaded leaving for ref for now
            foreach (var gnp in Resources.LoadAll<GONetParticipant>(string.Empty))// FindObjectsOfTypeAll<GONetParticipant>())
            //foreach (var gnp in Resources.FindObjectsOfTypeAll<GONetParticipant>())
            {
                if (gnp.designTimeLocation.StartsWith(PROJECT_HIERARCHY_PREFIX))
                {
                    GONetParticipant template = LookupResourceTemplateFromProjectLocation(gnp.designTimeLocation.Replace(PROJECT_HIERARCHY_PREFIX, string.Empty));
                    if ((object)template != null)
                    {
                        GONetLog.Debug("found template for design time location: " + gnp.designTimeLocation);
                        designTimeLocationToProjectTemplate[gnp.designTimeLocation] = template;
                    }
                }
            }
            */
        }

        private static GONetParticipant LookupResourceTemplateFromProjectLocation(string projectLocation)
        {
            if (projectLocation.Contains(RESOURCES))
            {
                const string PREFAB_EXTENSION = ".prefab";
                string resourceLocation = projectLocation.Substring(projectLocation.IndexOf(RESOURCES) + RESOURCES.Length).Replace(PREFAB_EXTENSION, string.Empty);
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
                GONetLog.Debug("expecting to find template for design time location: " + designTimeLocation);
                return designTimeLocationToProjectTemplate[designTimeLocation];
            }

            throw new ArgumentException(string.Concat("Must include supported prefix defined as const herein. value received: ", designTimeLocation), nameof(designTimeLocation));
        }
    }
}
