/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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
using UnityEngine;

public class GONetTestSpawner : MonoBehaviour
{
    public Simpeesimul GONetServerPREFAB;
    public Simpeesimul GONetClientPREFAB;

    public GONetParticipant authorityPrefab;
    public GONetParticipant nonAuthorityPrefab;

    private bool hasServerSpawned;

    /* auto spawn...but ok prior to server being created?
    private void Start()
    {
        GONetMain.Instantiate_WithNonAuthorityAlternate(authorityPrefab, nonAuthorityPrefab, transform.position, transform.rotation);
    }
    */

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            Instantiate(GONetClientPREFAB);
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.S) && !hasServerSpawned)
        {
            Instantiate(GONetServerPREFAB);
            hasServerSpawned = true;
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.D))
        {
            GameObject cubeta = GameObject.Find("Cubetas");
            Instantiate(cubeta);
        }

        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.R))
        {
            GONetMain.Instantiate_WithNonAuthorityAlternate(authorityPrefab, nonAuthorityPrefab, transform.position, transform.rotation);
        }
    }
}
