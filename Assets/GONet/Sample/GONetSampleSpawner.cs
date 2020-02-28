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

using GONet;
using System;
using UnityEngine;

public class GONetSampleSpawner : MonoBehaviour
{
    public GONetSampleClientOrServer GONetServerPREFAB;
    public GONetSampleClientOrServer GONetClientPREFAB;

    public GONetParticipant authorityPrefab;
    public GONetParticipant nonAuthorityPrefab;

    private bool hasServerSpawned;
    private bool hasClientSpawned;

    private void Start()
    {
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

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.C))
        {
            InstantiateClientIfNotAlready();
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S))
        {
            InstantiateServerIfNotAlready();
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            if (authorityPrefab)
            {
                if (nonAuthorityPrefab)
                {
                    GONetMain.Instantiate_WithNonAuthorityAlternate(authorityPrefab, nonAuthorityPrefab, transform.position, transform.rotation);
                }
                else
                {
                    Instantiate(authorityPrefab, transform.position, transform.rotation);
                }
            }
        }
    }

    private void InstantiateServerIfNotAlready()
    {
        if (!hasServerSpawned)
        {
            Instantiate(GONetServerPREFAB);
            hasServerSpawned = true;
        }
    }

    internal void InstantiateClientIfNotAlready()
    {
        if (!hasClientSpawned)
        {
            Instantiate(GONetClientPREFAB);
            hasClientSpawned = true;
        }
    }
}
