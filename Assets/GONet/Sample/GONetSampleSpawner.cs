/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
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

using GONet;
using System;
using System.Collections.Generic;
using UnityEngine;

public class GONetSampleSpawner : MonoBehaviour
{
    public GONetSampleClientOrServer GONetServerPREFAB;
    public GONetSampleClientOrServer GONetClientPREFAB;

    public GONetParticipant authorityPrefab;
    public GONetParticipant nonAuthorityPrefab;

    public PendulumJalou clientSpawned_AssumeServerAuthorityTest_Prefab;

    readonly List<PendulumJalou> pendulumJalous = new List<PendulumJalou>();

    GONetParticipant authorityInstance_lastInstantiatedByMe;

    private bool hasServerSpawned;

    private void Awake()
    {
        GONetMain.EventBus.Subscribe<GONetParticipantStartedEvent>(OnPendulumJalouStarted, envelope => envelope.GONetParticipant != null && envelope.GONetParticipant.gameObject.GetComponent<PendulumJalou>() != null);
    }

    private void OnPendulumJalouStarted(GONetEventEnvelope<GONetParticipantStartedEvent> eventEnvelope)
    {
        pendulumJalous.Add(eventEnvelope.GONetParticipant.gameObject.GetComponent<PendulumJalou>());
    }

    private void Start()
    {
        ProcessCmdLine();
    }

    void ProcessCmdLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            if (arg == "-server")
            {
                Instantiate(GONetServerPREFAB);
                hasServerSpawned = true;
            }
            else if (arg == "-client")
            {
                Instantiate(GONetClientPREFAB);
            }
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
        {
            Instantiate(GONetClientPREFAB);
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S) && !hasServerSpawned)
        {
            Instantiate(GONetServerPREFAB);
            hasServerSpawned = true;
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            authorityInstance_lastInstantiatedByMe = GONetMain.Instantiate_WithNonAuthorityAlternate(authorityPrefab, nonAuthorityPrefab, transform.position, transform.rotation);
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.J)) // j for pendulum jalou!
        {
            if (GONetMain.IsServer)
            {
                foreach (var penny in pendulumJalous)
                {
                    if (!GONetMain.IsMine(penny.gnp))
                    {
                        if (GONetMain.Server_AssumeAuthorityOver(penny.gnp))
                        {
                            GONetLog.Debug("HAHA.  You thought you owned me you pesky little client..  I am the server!  I own this one now too: " + penny.gnp.GONetId);
                        }
                    }
                }
            }
            else
            {
                Vector3 position = authorityInstance_lastInstantiatedByMe == null ? Vector3.zero : authorityInstance_lastInstantiatedByMe.transform.position;
                Quaternion rotation = authorityInstance_lastInstantiatedByMe == null ? Quaternion.identity : authorityInstance_lastInstantiatedByMe.transform.rotation;
                Instantiate(clientSpawned_AssumeServerAuthorityTest_Prefab, position, rotation);
            }
        }
    }
}
