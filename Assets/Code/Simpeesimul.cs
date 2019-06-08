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
            GONetMain.gonetClient = new GONetClient(new Client());
            //GONetMain.gonetClient.MessageReceived += Client_OnMessageReceived; // TODO replace this with an EventBus subscription to a certain message type!
            GONetMain.gonetClient.ConnectToServer(serverIP, serverPort);
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
