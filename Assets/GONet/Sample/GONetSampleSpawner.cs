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

using GONet;
using GONet.Sample;
using NetcodeIO.NET;
using System;
using UnityEngine;

public class GONetSampleSpawner : MonoBehaviourGONetCallbacks
{
    public GONetSampleClientOrServer GONetServerPREFAB;
    public GONetSampleClientOrServer GONetClientPREFAB;

    public GONetParticipant authorityPrefab;
    private GONetParticipant myAuthorityInstance;
    public GONetParticipant MyAuthorityInstance => myAuthorityInstance;
    public Transform[] spawnPoints;

    private bool hasServerSpawned;
    private bool hasClientSpawned;

    #region sample event subscription for client (connection) state changes

    protected override void Awake()
    {
        base.Awake();

        EventBus.Subscribe<ClientStateChangedEvent>(OnClientStateChanged_LogIt);
        EventBus.Subscribe<RemoteClientStateChangedEvent>(OnRemoteClientStateChanged_LogIt);

        // Add sample status UI component if it doesn't already exist
        // This is sample code - moved from GONetGlobal to keep core framework clean
        var gonetGlobal = GetComponent<GONetGlobal>();
        if (gonetGlobal != null && gonetGlobal.GetComponent<GONetStatusUI>() == null)
        {
            gonetGlobal.gameObject.AddComponent<GONetStatusUI>();
        }
    }

    private void OnRemoteClientStateChanged_LogIt(GONetEventEnvelope<RemoteClientStateChangedEvent> eventEnvelope)
    {
        GONetLog.Append($"As indicated by the server, GONet client with InitiatingClientConnectionUID: {eventEnvelope.Event.InitiatingClientConnectionUID} has changed state to: {Enum.GetName(typeof(ClientState), eventEnvelope.Event.StateNow)}");

        if (IsClient)
        {
            if (eventEnvelope.Event.InitiatingClientConnectionUID == GONetMain.GONetClient.InitiatingClientConnectionUID)
            {
                const string MY = " and it is my client.";
                GONetLog.Append(MY);
            }
            else
            {
                const string NOT = " and it is NOT my client (i.e., some other client's status changed).";
                GONetLog.Append(NOT);
            }
        }
        else
        {
            const string SRVR = " and this is the server acknowledging it to myself.";
            GONetLog.Append(SRVR);
        }

        GONetLog.Append_FlushDebug();
    }

    private void OnClientStateChanged_LogIt(GONetEventEnvelope<ClientStateChangedEvent> eventEnvelope)
    {
        GONetLog.Append($"As indicated by the initiating client, GONet client with InitiatingClientConnectionUID: {eventEnvelope.Event.InitiatingClientConnectionUID} indicated it has changed state to: {Enum.GetName(typeof(ClientState), eventEnvelope.Event.StateNow)}");

        if (IsClient)
        {
            if (eventEnvelope.Event.InitiatingClientConnectionUID == GONetMain.GONetClient.InitiatingClientConnectionUID)
            {
                const string MY = " and it is my client.";
                GONetLog.Append(MY);
            }
            else
            {
                const string NOT = " and it is NOT my client (i.e., some other client's status changed).";
                GONetLog.Append(NOT);
            }
        }
        else
        {
            const string SRVR = " and this is the server acknowledging it.";
            GONetLog.Append(SRVR);
        }

        GONetLog.Append_FlushDebug();
    }

    #endregion

    protected override void Start()
    {
        base.Start();
        
        if (Application.platform == RuntimePlatform.Android) // NOTE: for sample's sake, this check and inner block will make all android machines clients...change as your needs dictate!
        {
            InstantiateClientIfNotAlready();
        }
        else
        { // now that platform support is expanding, this below really probably only applies to PC/Windows (via BAT script) and Mac/Linux (via shell script)...not sure
            ProcessCmdLine();
        }
    }

    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        InstantiateAuthorityPrefabIfNotAlready();
    }

    public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantEnabled(gonetParticipant);


        if (!gonetParticipant.IsMine)
        {
            var characterController = gonetParticipant.GetComponentInChildren<CharacterController>();
            if (characterController)
            { 
                TurnOffNonAuthorityStuff(gonetParticipant, characterController);
            }
        }
    }

    /// <summary>
    /// PRE: <paramref name="gonetParticipant"/> IsMine is false.
    /// </summary>
    private void TurnOffNonAuthorityStuff(GONetParticipant gonetParticipant, CharacterController characterController)
    {
        characterController.enabled = false;
        foreach (var turnOff in gonetParticipant.GetComponentsInChildren<Camera>())
        {
            turnOff.gameObject.SetActive(false);
        }
        /*
        foreach (var turnOff in gonetParticipant.GetComponentsInChildren<CinemachineVirtualCamera>())
        {
            turnOff.gameObject.SetActive(false);
        }
        foreach (var turnOff in gonetParticipant.GetComponentsInChildren<ThirdPersonController>())
        {
            turnOff.enabled = false;
        }
        foreach (var turnOff in gonetParticipant.GetComponentsInChildren<PlayerInput>())
        {
            turnOff.enabled = false;
        }
        */
    }

    /// <summary>
    /// Processes command line arguments and auto-detects server/client role based on port availability.
    ///
    /// Priority order:
    /// 1. Explicit command line arguments (-server or -client) - highest priority
    /// 2. Auto-detection based on port availability (if enabled in GONet Project Settings)
    ///
    /// Auto-detection behavior (when enabled):
    /// - If target port is FREE → Start as SERVER
    /// - If target port is OCCUPIED → Start as CLIENT (assumes server already running)
    ///
    /// This can be disabled in GONet Project Settings (enableAutoRoleDetection = false).
    /// When disabled, you must use explicit command line args or keyboard shortcuts.
    /// </summary>
    void ProcessCmdLine()
    {
        // Skip if we're already starting as client or server
        // (e.g., Editor_AttemptStartAsClientIfAppropriate in GONetGlobal already handled it)
        if (hasServerSpawned || hasClientSpawned)
        {
            GONetLog.Info("[GONetSampleSpawner] Already spawned server or client, skipping ProcessCmdLine auto-detection");
            return;
        }

        string[] args = Environment.GetCommandLineArgs();
        bool hasExplicitServerArg = false;
        bool hasExplicitClientArg = false;

        // First pass: check for explicit command line arguments (highest priority)
        foreach (string arg in args)
        {
            const string SERVER = "-server";
            if (arg == SERVER)
            {
                hasExplicitServerArg = true;
                InstantiateServerIfNotAlready();
                GONetLog.Info("[GONetSampleSpawner] Explicit -server argument detected, starting as server");
            }
            else
            {
                const string CLIENT = "-client";
                if (arg == CLIENT)
                {
                    hasExplicitClientArg = true;
                    InstantiateClientIfNotAlready();
                    GONetLog.Info("[GONetSampleSpawner] Explicit -client argument detected, starting as client");
                }
            }
        }

        // If no explicit args, check if auto-detection is enabled
        if (!hasExplicitServerArg && !hasExplicitClientArg)
        {
            // Check GONetGlobal for auto-detection preference
            var gonetGlobal = GetComponent<GONet.GONetGlobal>();
            bool autoDetectionEnabled = gonetGlobal != null ? gonetGlobal.enableAutoRoleDetection : true;

            if (!autoDetectionEnabled)
            {
                GONetLog.Info("[GONetSampleSpawner] Auto role detection is disabled. Use command line args (-server/-client) or keyboard shortcuts (Ctrl+Alt+S/C).");
                return;
            }

            // Use automatic port-based detection
            int targetPort = GONet.GONetGlobal.ServerPort_Actual;
            bool isPortOccupied = GONet.Utils.NetworkUtils.IsLocalPortListening(targetPort);

            if (isPortOccupied)
            {
                // Port is occupied, assume another server is running → become client
                InstantiateClientIfNotAlready();
                GONetLog.Info($"[GONetSampleSpawner] Auto-detection: Port {targetPort} is occupied, starting as CLIENT");
            }
            else
            {
                // Port is free → become server
                InstantiateServerIfNotAlready();
                GONetLog.Info($"[GONetSampleSpawner] Auto-detection: Port {targetPort} is free, starting as SERVER");
            }
        }
    }

    private void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftCommand)) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
        {
            InstantiateClientIfNotAlready();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftCommand)) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S))
        {
            InstantiateServerIfNotAlready();
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftCommand)) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            InstantiateAuthorityPrefabIfNotAlready();
        }
    }

    private void InstantiateAuthorityPrefabIfNotAlready()
    {
        if (!myAuthorityInstance && authorityPrefab)
        {
            Transform spawnPoint = transform;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int iRandom = UnityEngine.Random.Range(0, spawnPoints.Length);
                spawnPoint = spawnPoints[iRandom];
            }

            myAuthorityInstance = Instantiate(authorityPrefab, spawnPoint.position, spawnPoint.rotation);
        }
    }

    public void InstantiateServerIfNotAlready()
    {
        if (!hasServerSpawned)
        {
            Instantiate(GONetServerPREFAB);
            hasServerSpawned = true;
        }
    }

    public void InstantiateClientIfNotAlready()
    {
        if (!hasClientSpawned)
        {
            Instantiate(GONetClientPREFAB);
            hasClientSpawned = true;
        }
    }
}
