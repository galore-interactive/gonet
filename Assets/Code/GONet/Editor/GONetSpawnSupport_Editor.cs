using GONet.Generation;
using GONet.Utils;
using System;
using System.Collections.Generic;
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
        const string SCENE_HIERARCHY_PREFIX = "scene://";
        const string PROJECT_HIERARCHY_PREFIX = "project://";

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
                        string currentLocation = string.Concat(PROJECT_HIERARCHY_PREFIX, projectPath);
                        // this seems unnecessary and problematic for project assets: EnsureDesignTimeLocationCurrent(gonetParticipant, currentLocation); // have to do proper unity serialization stuff for this to stick!
                        gonetParticipant.designTimeLocation = currentLocation; // so, set it  directly and it seems to stick/save/persist just fine
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
                            string fullUniquePath = string.Concat(SCENE_HIERARCHY_PREFIX, HierarchyUtils.GetFullUniquePath(gonetParticipant.gameObject));
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
        /// Do all proper unity serialization stuff or else a change will NOT stick/save/persist.
        /// </summary>
        private static void EnsureDesignTimeLocationCurrent(GONetParticipant gonetParticipant, string currentLocation)
        {
            SerializedObject serializedObject = new SerializedObject(gonetParticipant); // use the damned unity serializtion stuff or be doomed to fail on saving stuff to scene as you hope/expect!!!
            SerializedProperty serializedProperty = serializedObject.FindProperty(nameof(GONetParticipant.designTimeLocation));
            serializedObject.Update();
            serializedProperty.stringValue = currentLocation; // set it this way or else it will NOT work with prefabs!
            serializedObject.ApplyModifiedProperties();

            GONetLog.Debug("set design time location for name: " + gonetParticipant.gameObject.name + " to NEW value: " + gonetParticipant.designTimeLocation);
        }
    }
}
