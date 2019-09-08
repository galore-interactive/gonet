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
using NetcodeIO.NET;
using UnityEngine;

public class GONetSampleClientOrServer : MonoBehaviour
{
    public bool isServer = true;

    public const string serverIP = "10.0.0.53";
    public const int serverPort = 40000;

    private void Awake()
    {
        if (isServer)
        {
            GONetMain.gonetServer = new GONetServer(64, serverIP, serverPort);
            GONetMain.gonetServer.Start();
        }
        else
        {
            GONetMain.GONetClient = new GONetClient(new Client());
            GONetMain.GONetClient.ConnectToServer(serverIP, serverPort);
        }
    }
}
