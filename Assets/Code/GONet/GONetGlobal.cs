using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet
{
    /// <summary>
    /// Very important, in fact required, that this get added to one and only one <see cref="GameObject"/> in the first scene loaded in your game.
    /// This is where all the links into Unity life cycle stuffs start for GONet at large.
    /// </summary>
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public sealed class GONetGlobal : MonoBehaviour
    {
        #region TODO this should be configurable/set elsewhere potentially AFTER loading up and depending on other factors like match making etc...

        //public string serverIP;

        //public int serverPort;

        #endregion

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            GONetMain.IsUnityApplicationEditor = Application.isEditor;
            GONetMain.GlobalSessionContext = gameObject.GetComponent<GONetSessionContext>();

            GONetSpawnSupport_Runtime.CacheAllProjectDesignTimeLocations();
        }

        private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            { // do auto-assign authority id stuffs for all gonet stuff in scene
                List<GONetParticipant> gonetParticipantsInLevel = new List<GONetParticipant>();
                GameObject[] sceneObjects = sceneLoaded.GetRootGameObjects();
                FindAndAppend(sceneObjects, gonetParticipantsInLevel);
                GONetMain.RecordParticipantsAsDefinedInScene(gonetParticipantsInLevel);
                GONetMain.AssignOwnerAuthorityIds_IfAppropriate(gonetParticipantsInLevel);
            }
        }

        private static void FindAndAppend<T>(GameObject[] gameObjects, /* IN/OUT */ List<T> listToAppend)
        {
            int count = gameObjects != null ? gameObjects.Length : 0;
            for (int i = 0; i < count; ++i)
            {
                T t = gameObjects[i].GetComponent<T>();
                if (t != null)
                {
                    listToAppend.Add(t);
                }
                foreach (Transform childTransform in gameObjects[i].transform)
                {
                    FindAndAppend(new[] { childTransform.gameObject }, listToAppend);
                }
            }
        }

        private void Update()
        {
            GONetMain.Update();
        }

        private void OnApplicationQuit()
        {
            GONetMain.Shutdown();
        }
    }
}
