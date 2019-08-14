﻿/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using GONet.Utils;
using NetcodeIO.NET;

using GONetChannelId = System.Byte;

namespace GONet
{
    public class GONetServer
    {
        public int MaxSlots { get; private set; }

        Server server;
        public uint numConnections = 0;
        public GONetConnection_ServerToClient[] remoteClients;
        Dictionary<RemoteClient, GONetConnection_ServerToClient> remoteClientToGONetConnectionMap = new Dictionary<RemoteClient, GONetConnection_ServerToClient>(10);
        ConcurrentQueue<RemoteClient> newlyConnectedClients = new ConcurrentQueue<RemoteClient>();

        public delegate void ClientActionDelegate(GONetConnection_ServerToClient gonetConnection_ServerToClient);
        /// <summary>
        /// This *will* be called from main Unity thread.
        /// </summary>
        public event ClientActionDelegate ClientConnected;

        public GONetServer(int maxSlots, string address, int port)
        {
            MaxSlots = maxSlots;

            server = new Server(maxSlots, address, port, GONetMain.noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest, GONetMain._privateKey);

            remoteClients = new GONetConnection_ServerToClient[maxSlots];

            server.LogLevel = NetcodeLogLevel.Debug;

            server.OnClientConnected += OnClientConnected;
            server.OnClientDisconnected += OnClientDisconnected;
            server.OnClientMessageReceived += OnClientMessageReceived;

            if (NetworkUtils.IsIPAddressOnLocalMachine(address))
            {
                if (NetworkUtils.IsLocalPortListening(port))
                {
                    GONetLog.Warning("Instantiated a server <instance> locally and the port is already occupied!!!  Calling the <instance>.Start() method will likely fail and return false.");
                }
            }
            else
            {
                GONetLog.Warning("Instantiated a server <instance> locally and the address is not on this local machine!!!  Calling the <instance>.Start() method will likely fail and return false.");
            }
        }

        /// <summary>
        /// If the server can start, it will and return true......returns false otherwise.
        /// </summary>
        public bool Start()
        {
            try
            {
                server.Start();
                GONetMain.isServerOverride = true; // wherever the server is running is automatically considered "the" server
            }
            catch (Exception)
            { // one main reason why here is if a server is already started on this machine on port....so this process will turn into a client
                // TODO log
                return false;
            }

            return true;
        }

        /// <summary>
        /// Call this every frame (from the main Unity thread!) in order to process all network traffic in a timely manner.
        /// </summary>
        public void Update()
        {
            for (int iConnection = 0; iConnection < numConnections; ++iConnection)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = remoteClients[iConnection];
                gONetConnection_ServerToClient.Update(); // have to do this in order for anything to really be processed, in or out.
            }

            ProcessNewClientConnections_MainUnityThread();
        }

        public void SendBytesToAllClients(byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            for (int iConnection = 0; iConnection < numConnections; ++iConnection)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = remoteClients[iConnection];

                gONetConnection_ServerToClient.SendMessageOverChannel(bytes, bytesUsedCount, channelId);
            }
        }

        public void ForEachClient(Action<GONetConnection_ServerToClient> doThis)
        {
            for (int iConnection = 0; iConnection < numConnections; ++iConnection)
            {
                GONetConnection_ServerToClient gONetConnection_ServerToClient = remoteClients[iConnection];
                doThis(gONetConnection_ServerToClient);
            }
        }

        /// <summary>
        /// NOTE: This will likely be called on a thread other than the main Unity thread
        /// </summary>
        private void OnClientMessageReceived(RemoteClient sender, byte[] payload, int payloadSize)
        {
            GONetConnection_ServerToClient serverToClientConnection;
            if (remoteClientToGONetConnectionMap.TryGetValue(sender, out serverToClientConnection))
            {
                serverToClientConnection.ReceivePacket(payload, payloadSize);
            }
            else // we will ASSume the first time through is the connection message and OnClientConnected will not have been called yet and we will just go straight to process
            {
                // nothing to do i suppose
            }
        }

        /// <summary>
        /// NOTE: This will likely be called on a thread other than the main Unity thread
        /// </summary>
        private void OnClientDisconnected(RemoteClient client)
        {
            GONetLog.Debug("client *DIS*connected"); // TODO remove unity stuffs
        }

        /// <summary>
        /// NOTE: This will likely be called on a thread other than the main Unity thread
        /// </summary>
        private void OnClientConnected(RemoteClient client)
        {
            GONetLog.Debug("client connected"); // TODO remove unity stuffs

            newlyConnectedClients.Enqueue(client);
        }

        private void ProcessNewClientConnections_MainUnityThread()
        {
            int count = newlyConnectedClients.Count;
            for (int i = 0; i < count && !newlyConnectedClients.IsEmpty; ++i)
            {
                RemoteClient client;
                if (newlyConnectedClients.TryDequeue(out client))
                {
                    ProcessNewClientConnection_MainUnityThread(client);
                } // else TODO warn
            }
        }

        private void ProcessNewClientConnection_MainUnityThread(RemoteClient client)
        {
            if (numConnections < MaxSlots)
            {
                GONetConnection_ServerToClient gonetConnection_ServerToClient = new GONetConnection_ServerToClient(client);
                remoteClients[numConnections++] = gonetConnection_ServerToClient;
                remoteClientToGONetConnectionMap[client] = gonetConnection_ServerToClient;

                ClientConnected?.Invoke(gonetConnection_ServerToClient);
            }
        }

        public void Stop()
        {
            server.Stop();
        }
    }
}
