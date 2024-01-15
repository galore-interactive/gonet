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
using NetcodeIO.NET;
using System;
using UnityEngine;

public class GONetSampleSpawner : MonoBehaviourGONetCallbacks
{
    public GONetSampleClientOrServer GONetServerPREFAB;
    public GONetSampleClientOrServer GONetClientPREFAB;

    public GONetParticipant authorityPrefab;

    private bool hasServerSpawned;
    private bool hasClientSpawned;

    #region sample event subscription for client (connection) state changes

    protected override void Awake()
    {
        base.Awake();

        EventBus.Subscribe<ClientStateChangedEvent>(OnClientStateChanged_LogIt);
        EventBus.Subscribe<RemoteClientStateChangedEvent>(OnRemoteClientStateChanged_LogIt);
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

    void ProcessCmdLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            const string SERVER = "-server";
            if (arg == SERVER)
            {
                InstantiateServerIfNotAlready();
            }
            else
            {
                const string CLIENT = "-client";
                if (arg == CLIENT)
                {
                    InstantiateClientIfNotAlready();
                }
            }
        }
    }

    //private void Update()
    //{
    //    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
    //    {
    //        InstantiateClientIfNotAlready();
    //    }

    //    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S))
    //    {
    //        InstantiateServerIfNotAlready();
    //    }

    //    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
    //    {
    //        Instantiate(authorityPrefab, transform.position, transform.rotation);
    //    }
    //}

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
