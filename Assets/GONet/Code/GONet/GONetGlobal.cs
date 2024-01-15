/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
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

using GONet.Utils;
using System;
using System.Collections;
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
    public sealed class GONetGlobal : GONetParticipantCompanionBehaviour
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

        [Tooltip("GONet needs to know immediately on start of the program whether or not this game instance is a client or the server in order to initialize properly.  When using the provided Start_CLIENT.bat and Start_SERVER.bat files with builds, that will be taken care of for you.  However, when using the editor as a client (connecting to a server build), setting this flag to true is the only way for GONet to know immediately this game instance is a client.  If you run in the editor and see errors in the log on start up (e.g., \"[Log:Error] (Thread:1) (29 Dec 2019 20:24:06.970) (frame:-1s) (GONetEventBus handler error) Event Type: GONet.GONetParticipantStartedEvent\"), then it is likely because you are running as a client and this flag is not set to true.")]
        public bool shouldAttemptAutoStartAsClient = true;

        private readonly List<GONetParticipant> enabledGONetParticipants = new List<GONetParticipant>(1000);
        /// <summary>
        /// <para>A convenient collection of all the <see cref="GONetParticipant"/> instances that are currently enabled no matter what the value of <see cref="GONetParticipant.OwnerAuthorityId"/> value is.</para>
        /// <para>Elements are added here once Start() was called on the <see cref="GONetParticipant"/> and removed once OnDisable() is called.</para>
        /// <para>Do NOT attempt to modify this collection as to avoid creating issues for yourself/others.</para>
        /// </summary>
        public IEnumerable<GONetParticipant> EnabledGONetParticipants => enabledGONetParticipants;

        public static readonly string ServerIPAddress_Default = GONetMain.isServerOverride ? "0.0.0.0" : "127.0.0.1";
        public const int ServerPort_Default = 40000;

        public delegate void ServerConnectionInfoChanged(string serverIP, int serverPort);
        public static event ServerConnectionInfoChanged ActualServerConnectionInfoSet;

        public static bool AreAllServerConnectionInfoActualsSet => !string.IsNullOrWhiteSpace(serverIPAddress_Actual) && serverPort_Actual != -1;

        /// <summary>
        /// DO NOT SET THIS OUTSIDE GONET INTERNAL CODE!
        /// </summary>
        internal static string serverIPAddress_Actual;
        /// <summary>
        /// IMPORTANT: This will be NULL/empty when the actual serer ip address is not known!
        /// </summary>
        public static string ServerIPAddress_Actual { get => serverIPAddress_Actual; internal set { serverIPAddress_Actual = value; FireEventIfBothActualsSet(); } }

        /// <summary>
        /// DO NOT SET THIS OUTSIDE GONET INTERNAL CODE!
        /// </summary>
        internal static int serverPort_Actual = -1;
        /// <summary>
        /// IMPORTANT: This will be -1 when the actual serer ip address is not known!
        /// </summary>
        public static int ServerPort_Actual { get => serverPort_Actual; internal set { serverPort_Actual = value; FireEventIfBothActualsSet(); } }

        private static void FireEventIfBothActualsSet()
        {
            if (AreAllServerConnectionInfoActualsSet)
            {
                ActualServerConnectionInfoSet?.Invoke(serverIPAddress_Actual, serverPort_Actual);
            }
        }

        protected override void Awake()
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

            base.Awake(); // YUK: code smell...having to break OO protocol here and call base here as it needs to come AFTER the init stuff is done in GONetMain.InitOnUnityMainThread() and unity main thread identified or exceptions will be thrown in base.Awake() when subscribing

            GONetSpawnSupport_Runtime.CacheAllProjectDesignTimeLocations(this);

            enabledGONetParticipants.Clear();

            if (shouldAttemptAutoStartAsClient)
            {
                Editor_AttemptStartAsClientIfAppropriate();
            }
        }

        public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
        {
            base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

            if (isServer)
            {
                GONetMain.gonetServer.ClientDisconnected += Server_ClientDisconnected;
            }
        }

        private void Server_ClientDisconnected(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            Server_MakeDoublySureAllClientOwnedGNPsDestroyed(gonetConnection_ServerToClient.OwnerAuthorityId);
        }

        private void Server_MakeDoublySureAllClientOwnedGNPsDestroyed(ushort ownerAuthorityId)
        {
            for (int i = enabledGONetParticipants.Count - 1;  i >= 0; --i)
            {
                GONetParticipant enabledGNP = enabledGONetParticipants[i];
                if (enabledGNP.OwnerAuthorityId == ownerAuthorityId && enabledGNP && enabledGNP.gameObject)
                {
                    Destroy(enabledGNP.gameObject);
                }
            }
        }

        public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantEnabled(gonetParticipant);

            AddIfAppropriate(gonetParticipant);
        }

        public override void OnGONetParticipantStarted(GONetParticipant gonetParticipant)
        {
            base.OnGONetParticipantStarted(gonetParticipant);

            AddIfAppropriate(gonetParticipant);

            ushort toBeRemotelyControlledByAuthorityId;
            if (GONetMain.IsServer && GONetSpawnSupport_Runtime.Server_TryGetMarkToBeRemotelyControlledBy(gonetParticipant, out toBeRemotelyControlledByAuthorityId))
            {
                GONetMain.Server_AssumeAuthorityOver(gonetParticipant);

                // IMPORTANT: only now, after assuming authority, will the following change actually get propogated to the non-owners (i.e., since only the owner can make a auto-propogated change)
                gonetParticipant.RemotelyControlledByAuthorityId = toBeRemotelyControlledByAuthorityId;

                GONetSpawnSupport_Runtime.Server_UnmarkToBeRemotelyControlled_ProcessingComplete(gonetParticipant);
            }
        }

        private void AddIfAppropriate(GONetParticipant gonetParticipant)
        {
            if (!enabledGONetParticipants.Contains(gonetParticipant)) // may have already been added elsewhere
            {
                enabledGONetParticipants.Add(gonetParticipant);
            }
        }

        public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
        {
            enabledGONetParticipants.Remove(gonetParticipant); // regardless of whether or not it was present before this call, it will not be present afterward
        }

        private void Editor_AttemptStartAsClientIfAppropriate()
        {
            bool isAppropriate = 
                Application.isEditor &&
                !GONetMain.IsClient && 
                !GONetMain.IsServer && 
                NetworkUtils.IsIPAddressOnLocalMachine(GONetGlobal.ServerIPAddress_Default) && // just for editor, we can assume we only want to auto start client when server running locally
                NetworkUtils.IsLocalPortListening(GONetGlobal.ServerPort_Default); // just for editor, we can assume we only want to auto start client when server running locally
            
            if (isAppropriate) // do not attempt to start a client when we already know this is the server...no matter what the shouldAttemptAutoStartAsClient set to true seems to indicate!
            {
                var sampleSpawner = GetComponent<GONetSampleSpawner>();
                if (sampleSpawner)
                {
                    sampleSpawner.InstantiateClientIfNotAlready();
                }
                else
                {
                    const string UNABLE = "Unable to honor your setting of true on ";
                    const string BECAUSE = " because we could not find ";
                    const string ATTACHED = " attached to this GameObject, which is required to automatically start the client in this manner.";
                    GONetLog.Error(string.Concat(UNABLE, nameof(shouldAttemptAutoStartAsClient), BECAUSE, nameof(GONetSampleSpawner), ATTACHED));
                }
            }
            else
            {
                const string INAP = "It was deemed inappropriate to auto-start a client; however, do not fret if this is a client that was started via a build executable passing in '-client' as a command line argument since that would still be honored in which case this is a client.";
                GONetLog.Info(INAP);
            }
        }

        private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode loadMode)
        {
            { // do auto-assign authority id stuffs for all gonet stuff in scene
                List<GONetParticipant> gonetParticipantsInLevel = new List<GONetParticipant>();
                GameObject[] sceneObjects = sceneLoaded.GetRootGameObjects();
                FindAndAppend(sceneObjects, gonetParticipantsInLevel, 
                    (gnp) => gnp.designTimeLocation.StartsWith(GONetSpawnSupport_Runtime.SCENE_HIERARCHY_PREFIX)); // IMPORTANT: or else!
                GONetMain.RecordParticipantsAsDefinedInScene(gonetParticipantsInLevel);

                if (GONetMain.IsClientVsServerStatusKnown)
                {
                    GONetMain.AssignOwnerAuthorityIds_IfAppropriate(gonetParticipantsInLevel);
                }
                else
                {
                    StartCoroutine(AssignOwnerAuthorityIds_WhenAppropriate(gonetParticipantsInLevel));
                }
            }
        }

        private IEnumerator AssignOwnerAuthorityIds_WhenAppropriate(List<GONetParticipant> gonetParticipantsInLevel)
        {
            while (!GONetMain.IsClientVsServerStatusKnown)
            {
                yield return null;
            }

            GONetMain.AssignOwnerAuthorityIds_IfAppropriate(gonetParticipantsInLevel);
        }

        private static void FindAndAppend<T>(GameObject[] gameObjects, /* IN/OUT */ List<T> listToAppend, Func<T, bool> filter)
        {
            int count = gameObjects != null ? gameObjects.Length : 0;
            for (int i = 0; i < count; ++i)
            {
                T t = gameObjects[i].GetComponent<T>();
                if (t != null && filter(t))
                {
                    listToAppend.Add(t);
                }
                foreach (Transform childTransform in gameObjects[i].transform)
                {
                    FindAndAppend(new[] { childTransform.gameObject }, listToAppend, filter);
                }
            }
        }

        private void Update()
        {
            GONetMain.Update(this);
        }

        private void OnApplicationQuit()
        {
            GONetMain.Shutdown();
        }
    }
}
