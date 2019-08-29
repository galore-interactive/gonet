﻿/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

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
        public const string RESOURCES = "Resources/";

        private static readonly Dictionary<string, GONetParticipant> designTimeLocationToProjectTemplate = new Dictionary<string, GONetParticipant>(100);

        private static readonly string[] EmptyStringArray = new string[0];

        public static IEnumerable<string> LoadDesignTimeLocationsFromPersistence()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS);
            if (File.Exists(fullPath))
            {
                string fileContents = File.ReadAllText(fullPath);
                return fileContents.Split(Environment.NewLine.ToCharArray());
            }

            return EmptyStringArray;
        }

        public static void CacheAllProjectDesignTimeLocations()
        {
            foreach (string designTimeLocation in LoadDesignTimeLocationsFromPersistence())
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
            if (projectLocation != null && projectLocation.Contains(RESOURCES))
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

        public static GONetParticipant LookupTemplateFromDesignTimeLocation(string designTimeLocation)
        {
            if (designTimeLocation != null)
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
            }

            throw new ArgumentException(string.Concat("Must include supported prefix defined as const herein. value received: ", designTimeLocation), nameof(designTimeLocation));
        }

        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.designTimeLocation;
            return authorityInstance;
        }


        /// <summary>
        /// The public API for this is <see cref="GONetMain.Instantiate_WithNonAuthorityAlternate(GONetParticipant, GONetParticipant, Vector3, Quaternion)"/>.
        /// </summary>
        internal static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal, Vector3 position, Quaternion rotation)
        {
            // take note of nonAuthorityAlternateOriginal to make use of this during auto

            GONetParticipant authorityInstance = UnityEngine.Object.Instantiate(authorityOriginal, position, rotation);
            nonAuthorityDesignTimeLocationByAuthorityInstanceMap[authorityInstance] = nonAuthorityAlternateOriginal.designTimeLocation;
            return authorityInstance;
        }

        static readonly Dictionary<GONetParticipant, string> nonAuthorityDesignTimeLocationByAuthorityInstanceMap = new Dictionary<GONetParticipant, string>(100);

        internal static bool TryGetNonAuthorityDesignTimeLocation(GONetParticipant authorityInstance, out string nonAuthorityDesignTimeLocation)
        {
            // TODO consider removing authorityInstance as it will only serve its purpose for one call here (at least at time of writing)...cost of keeping around even after it is dead?
            return nonAuthorityDesignTimeLocationByAuthorityInstanceMap.TryGetValue(authorityInstance, out nonAuthorityDesignTimeLocation);
        }
    }
}
