using GONet;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils.IO;
using ReliableNetcode;
using UnityEngine;

public class Simpeesimul : MonoBehaviour
{
    public bool isServer = true;

    public string serverIP = "127.0.0.1";
    public int serverPort = 40000;

    private void Awake()
    {
        if (isServer)
        {
            GONetMain.gonetServer = new GONetServer(64, serverIP, serverPort);
            GONetMain.gonetServer.Start();
        }
        else
        {
            GONetMain.gonetClient = new GONetClient(new Client());
            GONetMain.gonetClient.MessageReceived += Client_OnMessageReceived;
            GONetMain.gonetClient.ConnectToServer(serverIP, serverPort);
        }
    }

    int client_messagesReceivedCount = 0;
    private void Client_OnMessageReceived(byte[] messageBytes, int bytesUsedCount)
    {
        using (var testPacketReader = ByteArrayReaderWriter.Get(messageBytes))
        {
            uint serverSaysItsSentCount = testPacketReader.ReadUInt32();
            Debug.Log("client received message.....total: " + ++client_messagesReceivedCount + ", server says it has sent total: " + serverSaysItsSentCount + " size of this one: " + bytesUsedCount);
        }
    }

    public const int SERVER_MAX_CONNECTIONS = 10;
    uint[] server_messagesSentCount = new uint[SERVER_MAX_CONNECTIONS];

    private void ______Update()
    {
        if (isServer)
        {
            const int MSG_SIZE = 1024;
            byte[] testPacket = new byte[MSG_SIZE];

            for (int iConnection = 0; iConnection < GONetMain.gonetServer.numConnections; ++iConnection)
            {
                for (int iMessage = 0; iMessage < 1; ++iMessage)
                {
                    using (var testPacketWriter = ByteArrayReaderWriter.Get(testPacket))
                    {
                        testPacketWriter.Write(++server_messagesSentCount[iConnection]);
                    }

                    GONetMain.gonetServer.remoteClients[iConnection].SendMessage(testPacket, MSG_SIZE, QosType.Reliable);
                }
            }
        }
    }
}
