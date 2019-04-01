using GONet;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils.IO;
using ReliableNetcode;
using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class Simpeesimul : MonoBehaviour
{
    GONetServer server;

    GONetConnection_ClientToServer client;

    public bool isServer = true;

    public string serverIP = "127.0.0.1";
    public int serverPort = 40000;

    private void Awake()
    {
        if (isServer)
        {
            server = new GONetServer(64, serverIP, serverPort);
            server.Start();
        }
        else
        {
            client = new GONetConnection_ClientToServer(new Client());
            client.ReceiveCallback = Client_OnMessageReceived;
            client.Connect(serverIP, serverPort);
        }
    }

    int client_messagesReceivedCount = 0;
    private void Client_OnMessageReceived(byte[] payload, int payloadSize)
    {
        using (var testPacketReader = ByteArrayReaderWriter.Get(payload))
        {
            uint serverSaysItsSentCount = testPacketReader.ReadUInt32();
            Debug.Log("client received message.....total: " + ++client_messagesReceivedCount + ", server says it has sent total: " + serverSaysItsSentCount + " size of this one: " + payloadSize);
        }
    }

    public const int SERVER_MAX_CONNECTIONS = 10;
    uint[] server_messagesSentCount = new uint[SERVER_MAX_CONNECTIONS];

    private void Update()
    {
        if (isServer)
        {
            const int MSG_SIZE = 1024;
            byte[] testPacket = new byte[MSG_SIZE];

            for (int iConnection = 0; iConnection < server.numConnections; ++iConnection)
            {
                for (int iMessage = 0; iMessage < 1; ++iMessage)
                {
                    using (var testPacketWriter = ByteArrayReaderWriter.Get(testPacket))
                    {
                        testPacketWriter.Write(++server_messagesSentCount[iConnection]);
                    }

                    server.remoteClients[iConnection].SendMessage(testPacket, MSG_SIZE, QosType.Reliable);
                }
            }

            server.Update(); // have to do this in order for anything to really be processed, in or out.
        }
        else
        {
            client.Update(); // have to do this in order for anything to really be processed, in or out.
        }
    }

    private void OnApplicationQuit()
    {
        if (isServer)
        {
            server.Stop();
        }
        else
        {
            client.Disconnect();
        }
    }
}
