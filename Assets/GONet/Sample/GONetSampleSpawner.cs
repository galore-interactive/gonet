/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
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
using System;
using UnityEngine;

public class GONetSampleSpawner : MonoBehaviour
{
    public GONetSampleClientOrServer GONetServerPREFAB;
    public GONetSampleClientOrServer GONetClientPREFAB;

    public GONetParticipant authorityPrefab;

    private bool hasServerSpawned;

    private void Start()
    {
        ProcessCmdLine();
    }

    void ProcessCmdLine()
    {
        string[] args = Environment.GetCommandLineArgs();

        foreach (string arg in args)
        {
            const string SERVER = "-server";
            if (arg == SERVER)
            {
                Instantiate(GONetServerPREFAB);
                hasServerSpawned = true;
            }
            else
            {
                const string CLIENT = "-client";
                if (arg == CLIENT)
                {
                    Instantiate(GONetClientPREFAB);
                }
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
            Instantiate(authorityPrefab, transform.position, transform.rotation);
        }
    }
}
