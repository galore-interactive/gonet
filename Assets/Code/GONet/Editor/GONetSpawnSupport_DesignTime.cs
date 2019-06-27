/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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

using GONet.Generation;
using GONet.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
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
        static GONetSpawnSupport_DesignTime()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly;

#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly;
#else
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif
        }

        private static void OnProjectChanged_EnsureDesignTimeLocationsCurrent_ProjectOnly()
        {
            foreach (var gonetParticipant in Resources.FindObjectsOfTypeAll<GONetParticipant>())
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
                        
                        //gonetParticipant.designTimeLocation = currentLocation; // so, set it  directly and it seems to stick/save/persist just fine
                    }
                }
            }
        }

        private static void OnHierarchyChanged_EnsureDesignTimeLocationsCurrent_SceneOnly()
        {
            if (!Application.isPlaying) // it would not be design time if we are playing (in editor) now would it?
            {
                bool somethingChanged = false;
                int count = EditorSceneManager.loadedSceneCount;
                for (int i = 0; i < count; ++i)
                {
                    Scene loadedScene = EditorSceneManager.GetSceneAt(i);
                    foreach (var rootGO in loadedScene.GetRootGameObjects())
                    {
                        foreach (var gonetParticipant in rootGO.GetComponentsInChildren<GONetParticipant>())
                        {
                            string fullUniquePath = string.Concat(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX, HierarchyUtils.GetFullUniquePath(gonetParticipant.gameObject));
                            if (fullUniquePath != gonetParticipant.designTimeLocation)
                            {
                                somethingChanged = true;
                                EnsureDesignTimeLocationCurrent(gonetParticipant, fullUniquePath); // have to do proper unity serialization stuff for this to stick!
                            }
                        }
                    }
                }

                if (somethingChanged)
                {
                    EditorSceneManager.MarkAllScenesDirty();
                    //EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // this may be too much....they will save when they want to...normally
                }
            }
        }

        /// <summary>
        /// TODO make this an ObservableHashSet and only call <see cref="KeepDesignTimeLocationPersistenceUpdated"/> when it changes.
        /// </summary>
        static readonly HashSet<string> allDesignTimeLocationsEncountered = new HashSet<string>();

        /// <summary>
        /// Do all proper unity serialization stuff or else a change will NOT stick/save/persist.
        /// </summary>
        private static void EnsureDesignTimeLocationCurrent(GONetParticipant gonetParticipant, string currentLocation)
        {
            SerializedObject serializedObject = new SerializedObject(gonetParticipant); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
            SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.designTimeLocation));
            serializedObject.Update();
            serializedProperty.stringValue = currentLocation; // set it this way or else it will NOT work with prefabs!
            gonetParticipant.designTimeLocation = currentLocation; // doubly sure
            serializedObject.ApplyModifiedProperties();

            GONetLog.Debug("set design time location for name: " + gonetParticipant.gameObject.name + " to NEW value: " + gonetParticipant.designTimeLocation);

            allDesignTimeLocationsEncountered.Add(currentLocation);
            KeepDesignTimeLocationPersistenceUpdated();
        }

        /// <summary>
        /// POST: contents of <see cref="allDesignTimeLocationsEncountered"/> persisted.
        /// </summary>
        private static void KeepDesignTimeLocationPersistenceUpdated()
        {
            StringBuilder fileContents = new StringBuilder(5000);
            foreach (string designTimeLocation in allDesignTimeLocationsEncountered.OrderBy(x => x))
            {
                fileContents.Append(designTimeLocation).Append(Environment.NewLine);
            }

            string directory = Path.Combine(Application.streamingAssetsPath, GONetSpawnSupport_Runtime.GONET_STREAMING_ASSETS_FOLDER);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, GONetSpawnSupport_Runtime.DESIGN_TIME_LOCATIONS_FILE_POST_STREAMING_ASSETS);
            File.WriteAllText(fullPath, fileContents.ToString());
        }
    }
}
