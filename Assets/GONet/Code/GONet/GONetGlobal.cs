﻿/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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