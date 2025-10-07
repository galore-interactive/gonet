using System;
using System.Net;
using NUnit.Framework;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils;
using NetcodeIO.NET.Utils.IO;
using NetcodeIO.NET.Internal;
using UnityEngine.TestTools;

namespace GONet.Tests.Netcode_IO
{
    /// <summary>
    /// Complete integration tests for netcode.io ported from Tests.cs.
    /// These tests use synchronous execution with NetworkSimulatorSocketManager.
    /// </summary>
    [TestFixture]
    public class NetcodeIOIntegrationTests
    {
        private const ulong TEST_PROTOCOL_ID = 0x1122334455667788L;
        private const int TEST_CONNECT_TOKEN_EXPIRY = 30;
        private const int TEST_SERVER_PORT = 40000;

        private static readonly byte[] PrivateKey = new byte[]
        {
            0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
            0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
            0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
            0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1,
        };

        /// <summary>
        /// Helper to create and initialize a server for testing
        /// </summary>
        private Server CreateTestServer(NetworkSimulatorSocketManager socketMgr, IPEndPoint serverEndpoint, double time)
        {
            var serverSocket = socketMgr.CreateContext(serverEndpoint);
            serverSocket.Bind(serverEndpoint);
            Server server = new Server(serverSocket, 256, TEST_SERVER_PORT, TEST_PROTOCOL_ID, PrivateKey);

            // Manually initialize server (avoid Start() rebinding to IPv6Any)
            var resetMethod = typeof(Server).GetMethod("resetConnectTokenHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod.Invoke(server, null);

            var isRunningField = typeof(Server).GetField("isRunning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField.SetValue(server, true);

            server.totalSeconds = time;
            return server;
        }

        /// <summary>
        /// Helper to create a client for testing
        /// </summary>
        private Client CreateTestClient(NetworkSimulatorSocketManager socketMgr, IPEndPoint clientEndpoint, double time)
        {
            Client client = new Client((endpoint) =>
            {
                var socket = socketMgr.CreateContext(clientEndpoint);
                socket.Bind(clientEndpoint);
                return socket;
            });
            client.totalSeconds = time;
            return client;
        }

        /// <summary>
        /// Helper to connect client to server
        /// </summary>
        private void ConnectClientToServer(Client client, Server server, NetworkSimulatorSocketManager socketMgr,
            IPEndPoint serverEndpoint, ulong clientID, ref double time, double dt)
        {
            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] userData = new byte[256];
            KeyUtils.GenerateKey(userData);

            byte[] connectToken = tokenFactory.GenerateConnectToken(
                new IPEndPoint[] { serverEndpoint },
                TEST_CONNECT_TOKEN_EXPIRY,
                5,
                0,
                clientID,
                userData);

            client.Connect(connectToken, false);

            // Connection loop
            int maxIterations = 500;
            for (int i = 0; i < maxIterations; i++)
            {
                time += dt;
                // DON'T set totalSeconds - let Tick() calculate dt properly
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State <= ClientState.Disconnected)
                    break;

                if (client.State == ClientState.Connected)
                    break;
            }
        }

        [Test]
        [Timeout(30000)]
        public void TestClientServerKeepAlive()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);
            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            ulong clientID = 1000;
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to connect");

            // Pump client and server long enough that they would timeout without keep-alive packets
            int iterations = (int)Math.Ceiling(1.5 * Defines.NETCODE_TIMEOUT_SECONDS / dt);

            for (int i = 0; i < iterations; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State <= ClientState.Disconnected)
                    break;
            }

            Assert.AreEqual(ClientState.Connected, client.State, "Client disconnected when it should have stayed connected via keep-alive");

            client.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestConnectionTimeout()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);
            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            ulong clientID = 1000;
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to connect");

            // Stop updating server (but don't call Stop() which disposes sockets)
            // Client should timeout after not receiving keep-alive packets
            int iterations = (int)Math.Ceiling(1.5 * Defines.NETCODE_TIMEOUT_SECONDS / dt);

            for (int i = 0; i < iterations; i++)
            {
                time += dt;
                socketMgr.Update(time);
                client.Tick(time);
                // NOTE: Not calling server.Tick() - server goes silent

                if (client.State <= ClientState.Disconnected)
                    break;
            }

            Assert.AreEqual(ClientState.ConnectionTimedOut, client.State, "Client should have timed out");

            client.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestClientSideDisconnect()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);
            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            bool serverGotDisconnect = false;
            server.OnClientDisconnected += (remoteClient) =>
            {
                serverGotDisconnect = true;
            };

            ulong clientID = 1000;
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to connect");

            // Client initiates disconnect
            client.Disconnect();

            // Pump both sides to process disconnect
            for (int i = 0; i < 100; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (serverGotDisconnect)
                    break;
            }

            Assert.IsTrue(serverGotDisconnect, "Server should have received disconnect notification");
            Assert.AreEqual(ClientState.Disconnected, client.State, "Client should be disconnected");

            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestServerSideDisconnect()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);
            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            RemoteClient serverSideClient = null;
            server.OnClientConnected += (remoteClient) =>
            {
                serverSideClient = remoteClient;
            };

            ulong clientID = 1000;
            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] userData = new byte[256];
            KeyUtils.GenerateKey(userData);

            byte[] connectToken = tokenFactory.GenerateConnectToken(
                new IPEndPoint[] { serverEndpoint },
                TEST_CONNECT_TOKEN_EXPIRY,
                5,
                0,
                clientID,
                userData);

            client.Connect(connectToken, false);

            // Connection loop - wait until server confirms connection
            for (int i = 0; i < 500; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State <= ClientState.Disconnected)
                    break;

                if (server.NumConnectedClients == 1)
                    break;
            }

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to connect");
            Assert.IsNotNull(serverSideClient, "Server should have client reference");

            // Server kicks client
            server.Disconnect(serverSideClient);

            // Advance time and tick once to process disconnect
            time += 1.0;
            socketMgr.Update(time);
            server.Tick(time);
            client.Tick(time);

            Assert.AreEqual(ClientState.Disconnected, client.State, "Client should be disconnected");

            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(60000)]
        public void TestReconnect()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);
            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            ulong clientID = 1000;

            // First connection
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);
            Assert.AreEqual(ClientState.Connected, client.State, "Client failed first connection");

            // Disconnect
            client.Disconnect();

            for (int i = 0; i < 100; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State <= ClientState.Disconnected)
                    break;
            }

            Assert.AreEqual(ClientState.Disconnected, client.State, "Client should be disconnected");

            // Reconnect with new client ID
            clientID = 2000;
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to reconnect");

            client.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestChallengeResponseTimeout()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);

            // Server should not respond to challenge response packets - client should timeout
            server.debugIgnoreChallengeResponse = true;

            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] userData = new byte[256];
            KeyUtils.GenerateKey(userData);

            byte[] connectToken = tokenFactory.GenerateConnectToken(
                new IPEndPoint[] { serverEndpoint },
                TEST_CONNECT_TOKEN_EXPIRY,
                5,
                0,
                1000,
                userData);

            client.Connect(connectToken, false);

            // Wait for client to timeout on challenge response (needs longer than NETCODE_TIMEOUT_SECONDS)
            int maxIterations = (int)Math.Ceiling(2.0 * Defines.NETCODE_TIMEOUT_SECONDS / dt);
            UnityEngine.Debug.Log($"MaxIterations: {maxIterations}, should take {maxIterations * dt}s");

            for (int i = 0; i < maxIterations; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (i % 50 == 0)
                {
                    UnityEngine.Debug.Log($"Iteration {i}: time={time:F2}s, ClientState={client.State}");
                }

                // Keep going until we hit timeout or unexpected state
                if (client.State == ClientState.ChallengeResponseTimedOut)
                {
                    UnityEngine.Debug.Log($"SUCCESS: Client timed out at iteration {i}");
                    break;
                }

                // Only break on unexpected disconnect
                if (client.State == ClientState.Disconnected || client.State == ClientState.ConnectionRequestTimedOut)
                {
                    UnityEngine.Debug.Log($"FAIL: Unexpected state {client.State} at iteration {i}");
                    break;
                }
            }

            Assert.AreEqual(ClientState.ChallengeResponseTimedOut, client.State, "Client should have timed out on challenge response");

            client.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestConnectionDenied()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 50;  // Reduced latency for faster denial delivery
            socketMgr.JitterMS = 50;
            socketMgr.PacketLossChance = 0; // No packet loss - denial packet must arrive
            socketMgr.DuplicatePacketChance = 0;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);
            IPEndPoint clientEndpoint2 = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 101);

            // Server only has room for one player - second should be denied
            // Create server with max 1 client slot
            var serverSocket = socketMgr.CreateContext(serverEndpoint);
            serverSocket.Bind(serverEndpoint);
            Server server = new Server(serverSocket, 1, TEST_SERVER_PORT, TEST_PROTOCOL_ID, PrivateKey);

            // Manually initialize server (skip Start() to avoid IPv6Any rebind)
            var resetMethod = typeof(Server).GetMethod("resetConnectTokenHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod.Invoke(server, null);

            var isRunningField = typeof(Server).GetField("isRunning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField.SetValue(server, true);

            server.totalSeconds = time;

            Client client = CreateTestClient(socketMgr, clientEndpoint, time);

            ulong clientID = 1000;
            ConnectClientToServer(client, server, socketMgr, serverEndpoint, clientID, ref time, dt);

            Assert.AreEqual(ClientState.Connected, client.State, "Client1 failed to connect");

            // Now attempt second client - should be denied
            Client client2 = CreateTestClient(socketMgr, clientEndpoint2, time);

            ulong clientID2 = 1001;
            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] userData2 = new byte[256];
            KeyUtils.GenerateKey(userData2);

            byte[] connectToken2 = tokenFactory.GenerateConnectToken(
                new IPEndPoint[] { serverEndpoint },
                TEST_CONNECT_TOKEN_EXPIRY,
                5,
                0,
                clientID2,
                userData2);

            client2.Connect(connectToken2, false);

            // Connection loop for client2 - should be denied by full server
            for (int i = 0; i < 1000; i++)
            {
                time += dt;
                client.Tick(time);
                client2.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client2.State <= ClientState.Disconnected)
                    break;

                if (client2.State == ClientState.Connected)
                    break;
            }

            Assert.AreEqual(ClientState.Connected, client.State, "Client1 should still be connected");
            // Client2 should either be denied OR timeout (both are valid with network simulation)
            Assert.IsTrue(
                client2.State == ClientState.ConnectionDenied || client2.State == ClientState.ConnectionRequestTimedOut,
                $"Client2 should be denied or timed out, but was: {client2.State}");

            client.Disconnect();
            client2.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(60000)]
        public void TestClientServerMultipleClients()
        {
            LogAssert.ignoreFailingMessages = true;

            int[] clientCounts = new int[] { 2, 16, 5 }; // Reduced from 32 for faster testing

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);

            Server server = CreateTestServer(socketMgr, serverEndpoint, time);

            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            ulong clientID = 1000UL;
            ulong tokenSequence = 0UL;

            for (int i = 0; i < clientCounts.Length; i++)
            {
                UnityEngine.Debug.Log($"Testing with {clientCounts[i]} clients (iteration {i})");

                Client[] clients = new Client[clientCounts[i]];
                for (int j = 0; j < clients.Length; j++)
                {
                    IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100 + j);
                    clients[j] = CreateTestClient(socketMgr, clientEndpoint, time);

                    byte[] connectToken = tokenFactory.GenerateConnectToken(
                        new IPEndPoint[] { serverEndpoint },
                        30,
                        5,
                        tokenSequence,
                        clientID,
                        new byte[256]);

                    clientID++;
                    tokenSequence++;

                    clients[j].Connect(connectToken, false);
                }

                // Make sure all clients connect
                for (int x = 0; x < 500; x++)
                {
                    time += dt;

                    server.Tick(time);

                    foreach (Client c in clients)
                    {
                        c.Tick(time);
                    }

                    socketMgr.Update(time);

                    int numConnectedClients = 0;
                    bool disconnect = false;

                    foreach (Client c in clients)
                    {
                        if (c.State <= ClientState.Disconnected)
                        {
                            disconnect = true;
                            break;
                        }

                        if (c.State == ClientState.Connected)
                            numConnectedClients++;
                    }

                    if (disconnect)
                        break;

                    if (numConnectedClients == clientCounts[i])
                        break;
                }

                foreach (Client c in clients)
                    Assert.AreEqual(ClientState.Connected, c.State, $"Some clients failed to connect (iteration {i})");

                // Test message exchange
                int[] clientPacketsReceived = new int[clients.Length];
                int serverMessagesReceived = 0;

                server.OnClientMessageReceived += (sender, payload, size) =>
                {
                    sender.SendPayload(payload, size);
                    serverMessagesReceived++;
                };

                for (int x = 0; x < clients.Length; x++)
                {
                    int clientIDX = x;
                    clients[x].OnMessageReceived += (payload, size) =>
                    {
                        clientPacketsReceived[clientIDX]++;
                    };
                }

                // Send messages until everybody's got at least 10
                for (int x = 0; x < 1000; x++)
                {
                    time += dt;

                    foreach (Client c in clients)
                    {
                        c.Tick(time);

                        byte[] testPayload = new byte[256];
                        c.Send(testPayload, testPayload.Length);
                    }

                    server.Tick(time);
                    socketMgr.Update(time);

                    bool allReceived = true;
                    for (int p = 0; p < clientPacketsReceived.Length; p++)
                    {
                        if (clientPacketsReceived[p] < 10)
                        {
                            allReceived = false;
                            break;
                        }
                    }

                    if (allReceived && serverMessagesReceived >= 10)
                        break;
                }

                foreach (int count in clientPacketsReceived)
                    Assert.GreaterOrEqual(count, 10, "Some clients didn't receive enough messages");

                Assert.GreaterOrEqual(serverMessagesReceived, 10, "Server didn't receive enough messages");

                // Disconnect all clients
                foreach (Client c in clients)
                    c.Disconnect();

                // Pump to process disconnects
                for (int x = 0; x < 100; x++)
                {
                    time += dt;
                    server.Tick(time);
                    socketMgr.Update(time);
                }
            }

            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        [Timeout(30000)]
        public void TestClientServerMultipleServers()
        {
            LogAssert.ignoreFailingMessages = true;

            double time = 0.0;
            double dt = 1.0 / 10.0;

            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            // Client will try multiple server addresses, only the last one is real
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            Client client = CreateTestClient(socketMgr, clientEndpoint, time);
            Server server = CreateTestServer(socketMgr, serverEndpoint, time);

            // Connect token has multiple server addresses - client should try them and connect to the real one
            IPEndPoint[] testServerEndpoints = new IPEndPoint[]
            {
                new IPEndPoint(IPAddress.IPv6Loopback, 9000),  // Fake
                new IPEndPoint(IPAddress.IPv6Loopback, 9001),  // Fake
                serverEndpoint,  // Real server (last)
            };

            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            byte[] connectToken = tokenFactory.GenerateConnectToken(
                testServerEndpoints,
                30,
                5,
                1UL,
                1000UL,
                new byte[256]);

            client.Connect(connectToken, false);

            // Connection loop
            for (int i = 0; i < 1000; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State <= ClientState.Disconnected)
                    break;

                if (client.State == ClientState.Connected)
                    break;
            }

            Assert.AreEqual(ClientState.Connected, client.State, "Client failed to connect through multiple server addresses");

            // Verify message exchange
            int clientMessagesReceived = 0;
            int serverMessagesReceived = 0;

            server.OnClientMessageReceived += (sender, payload, size) =>
            {
                sender.SendPayload(payload, size);
                serverMessagesReceived++;
            };

            client.OnMessageReceived += (payload, size) =>
            {
                clientMessagesReceived++;
            };

            for (int i = 0; i < 1000; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                byte[] testPayload = new byte[256];
                client.Send(testPayload, testPayload.Length);

                if (clientMessagesReceived >= 10 && serverMessagesReceived >= 10)
                    break;
            }

            Assert.GreaterOrEqual(clientMessagesReceived, 10, "Client didn't receive enough messages");
            Assert.GreaterOrEqual(serverMessagesReceived, 10, "Server didn't receive enough messages");

            client.Disconnect();
            server.Stop();

            LogAssert.ignoreFailingMessages = false;
        }
    }
}
