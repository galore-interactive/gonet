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
using GONet.Utils;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils.IO;
using ReliableNetcode;
using UnityEngine;

public class Simpeesimul : MonoBehaviour
{
    public bool isServer = true;

    public const string serverIP = "127.0.0.1";
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
            //GONetMain.gonetClient.MessageReceived += Client_OnMessageReceived; // TODO replace this with an EventBus subscription to a certain message type!
            GONetMain.GONetClient.ConnectToServer(serverIP, serverPort);
        }
    }

    int client_messagesReceivedCount = 0;
    private void Client_OnMessageReceived(byte[] messageBytes, int bytesUsedCount)
    {
        using (var testPacketReader = ByteArrayReaderWriter.Get(messageBytes))
        {
            uint serverSaysItsSentCount = testPacketReader.ReadUInt32();
            GONetLog.Debug("client received message.....total: " + ++client_messagesReceivedCount + ", server says it has sent total: " + serverSaysItsSentCount + " size of this one: " + bytesUsedCount);
        }
    }
}
