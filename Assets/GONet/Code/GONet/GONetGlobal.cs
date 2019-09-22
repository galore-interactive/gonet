/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GONet
{
    /// <summary>
    /// Very important, in fact required, that this get added to one and only one <see cref="GameObject"/> in the first scene loaded in your game.
    /// This is where all the links into Unity life cycle stuffs start for GONet at large.
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    [RequireComponent(typeof(GONetSessionContext))] // NOTE: requiring GONetSessionContext will thereby get the DontDestroyOnLoad behavior
    public sealed class GONetGlobal : MonoBehaviour
    {
        #region TODO this should be configurable/set elsewhere potentially AFTER loading up and depending on other factors like match making etc...

        //public string serverIP;

        //public int serverPort;

        [Tooltip("***IMPORTANT: When Awake() is called, this value will be locked in place, whereas any adjustments at runtime will yield nothing.\nWhen a Sync Settings Profile or [GONetAutoMagicalSync] setting for " + nameof(GONetAutoMagicalSyncSettings_ProfileTemplate.ShouldBlendBetweenValuesReceived) + " is set to true, this value is used throughout GONet for the length of time in milliseconds to buffer up received sync values from other machines in the network before applying the data locally.\n*When 0, everything will have to be predicted client-side (e.g., extrapolation) since the data received is always old.\n*Non-zero positive values will yield much more accurate (although out of date) data assuming the buffer lead time is large enough to account for lag (network/processing).")]
        [Range(0, 1000)]
        public int valueBlendingBufferLeadTimeMilliseconds = (int)TimeSpan.FromSeconds(GONetMain.BLENDING_BUFFER_LEAD_SECONDS_DEFAULT).TotalMilliseconds;

        #endregion

        [Tooltip("GONet requires GONetGlobal to have a prefab for GONetLocal set here.  Each machine in the network game will instantiate one instance of this prefab.")]
        [SerializeField]
        internal GONetLocal gonetLocalPrefab;

        private readonly List<GONetParticipant> enabledGONetParticipants = new List<GONetParticipant>(1000);
        /// <summary>
        /// <para>A convenient collection of all the <see cref="GONetParticipant"/> instances that are currently enabled no matter what the value of <see cref="GONetParticipant.OwnerAuthorityId"/> value is.</para>
        /// <para>Elements are added here once Start() was called on the <see cref="GONetParticipant"/> and removed once OnDisable() is called.</para>
        /// <para>Do NOT attempt to modify this collection as to avoid creating issues for yourself/others.</para>
        /// </summary>
        public IEnumerable<GONetParticipant> EnabledGONetParticipants => enabledGONetParticipants;

        private void Awake()
        {
            if (gonetLocalPrefab == null)
            {
                Debug.LogError("Sorry.  We have to exit the application.  GONet requires GONetGlobal to have a prefab for GONetLocal set in the field named " + nameof(gonetLocalPrefab));
#if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            GONetMain.InitOnUnityMainThread(this, gameObject.GetComponent<GONetSessionContext>(), valueBlendingBufferLeadTimeMilliseconds);

            GONetSpawnSupport_Runtime.CacheAllProjectDesignTimeLocations();

            enabledGONetParticipants.Clear();
            GONetMain.EventBus.Subscribe<GONetParticipantEnabledEvent>(OnGNPEnabled_AddToList);
            GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(OnGNPStarted_AddToList);
            GONetMain.EventBus.Subscribe<GONetParticipantDisabledEvent>(OnGNPDisabled_RemoveFromList);
        }

        private void OnGNPEnabled_AddToList(GONetEventEnvelope<GONetParticipantEnabledEvent> eventEnvelope)
        {
            bool isTooEarlyToAdd_StartRequiredFirst = (object)eventEnvelope.GONetParticipant == null;
            if (!isTooEarlyToAdd_StartRequiredFirst)
            {
                enabledGONetParticipants.Add(eventEnvelope.GONetParticipant);
            }
        }

        private void OnGNPStarted_AddToList(GONetEventEnvelope<GONetParticipantStartedEvent> eventEnvelope)
        {
            if ((object)eventEnvelope.GONetParticipant != null && // not sure why this would be the case there, but have to double check..no likie the null
                !enabledGONetParticipants.Contains(eventEnvelope.GONetParticipant)) // may have already been added in OnGNPEnabled_AddToList
            {
                enabledGONetParticipants.Add(eventEnvelope.GONetParticipant);
            }
        }

        private void OnGNPDisabled_RemoveFromList(GONetEventEnvelope<GONetParticipantDisabledEvent> eventEnvelope)
        {
            enabledGONetParticipants.Remove(eventEnvelope.GONetParticipant);
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
