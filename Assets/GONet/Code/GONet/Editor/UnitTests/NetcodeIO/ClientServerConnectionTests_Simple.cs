using System;
using System.Net;
using NUnit.Framework;
using NetcodeIO.NET;
using NetcodeIO.NET.Utils;
using NetcodeIO.NET.Utils.IO;
using UnityEngine.TestTools;

namespace GONet.Tests.Netcode_IO
{
    /// <summary>
    /// Simpler client-server connection tests that run synchronously on main thread.
    /// The network simulator doesn't require actual threading - just careful time management.
    /// </summary>
    [TestFixture]
    public class ClientServerConnectionTests_Simple
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
        /// NOTE: This test can be flaky when run with other tests due to:
        /// 1. Aggressive network conditions (250ms latency, 250ms jitter, 5% packet loss, 10% duplicates)
        /// 2. Challenge response packets may be lost in simulated packet loss
        /// 3. Client gets stuck in SendingChallengeResponse state waiting for ACK
        ///
        /// If this test fails when running full suite, try running it in isolation.
        /// Random packet loss means it may pass on retry.
        /// </summary>
        [Test]
        [Timeout(30000)]
        public void TestClientServerConnection_Synchronous()
        {
            // Ignore expected networking errors that occur during high-latency simulated connections
            LogAssert.ignoreFailingMessages = true;

            UnityEngine.Debug.Log("Starting TestClientServerConnection_Synchronous");

            double time = 0.0;
            double dt = 1.0 / 10.0; // 100ms updates

            // Create network simulator with aggressive conditions (can cause flakiness)
            NetworkSimulatorSocketManager socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = 250;
            socketMgr.JitterMS = 250;
            socketMgr.PacketLossChance = 5;   // 5% packet loss - challenge response may be lost!
            socketMgr.DuplicatePacketChance = 10;
            socketMgr.AutoTime = false;

            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);

            // Use IPv6Loopback for both server and client to avoid IPv6Any/Loopback mismatch in NetworkSimulator
            // Note: Server.Start() will try to rebind to IPv6Any, but we need to keep it on Loopback for NetworkSimulator
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT);
            IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.IPv6Loopback, TEST_SERVER_PORT + 100);

            // Create server - use pre-bound socket to prevent Server.Start() from rebinding to IPv6Any
            var serverSocket = socketMgr.CreateContext(serverEndpoint);
            serverSocket.Bind(serverEndpoint);
            Server server = new Server(serverSocket, 256, TEST_SERVER_PORT, TEST_PROTOCOL_ID, PrivateKey);

            // Manually initialize server (Server.Start() would rebind to IPv6Any which breaks NetworkSimulator)
            // Call resetConnectTokenHistory() via reflection
            var resetMethod = typeof(Server).GetMethod("resetConnectTokenHistory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod.Invoke(server, null);

            // Set isRunning to true via reflection
            var isRunningField = typeof(Server).GetField("isRunning", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField.SetValue(server, true);

            server.totalSeconds = time;
            UnityEngine.Debug.Log("Server created and bound");

            // Create client
            Client client = new Client((endpoint) =>
            {
                var socket = socketMgr.CreateContext(clientEndpoint);
                socket.Bind(clientEndpoint);
                return socket;
            });
            client.totalSeconds = time;
            UnityEngine.Debug.Log("Client created");

            // Generate connect token
            ulong clientID = 1000;
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
            UnityEngine.Debug.Log($"Client connecting... State: {client.State}");

            // Connection loop - exactly like the original test
            int iteration = 0;
            while (true)
            {
                time += dt;
                client.totalSeconds = time;
                server.totalSeconds = time;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time); // Process network simulation AFTER ticks

                iteration++;
                if (iteration % 10 == 0 || iteration <= 5)
                {
                    UnityEngine.Debug.Log($"Iteration {iteration}: time={time:F2}s, ClientState={client.State}");
                }

                if (client.State <= ClientState.Disconnected)
                {
                    UnityEngine.Debug.Log($"Client disconnected at iteration {iteration}. State: {client.State}, Time: {time:F2}s");
                    break;
                }

                if (client.State == ClientState.Connected)
                {
                    UnityEngine.Debug.Log($"Client connected at iteration {iteration}! Time: {time:F2}s");
                    break;
                }

                // Safety timeout
                if (iteration > 2000)
                {
                    UnityEngine.Debug.Log($"Test timeout after {iteration} iterations");
                    break;
                }
            }

            Assert.AreEqual(ClientState.Connected, client.State, $"Client failed to connect: {client.State}");

            // Verify message exchange
            int clientMessagesReceived = 0;
            int serverMessagesReceived = 0;

            server.OnClientMessageReceived += (sender, payload, size) =>
            {
                // Echo message back to client
                sender.SendPayload(payload, size);
                serverMessagesReceived++;
            };

            client.OnMessageReceived += (payload, size) =>
            {
                clientMessagesReceived++;
            };

            // Send messages until both have at least 10
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

            Assert.GreaterOrEqual(clientMessagesReceived, 10, "Client did not receive enough messages");
            Assert.GreaterOrEqual(serverMessagesReceived, 10, "Server did not receive enough messages");

            UnityEngine.Debug.Log($"Test PASSED - Client received {clientMessagesReceived}, Server received {serverMessagesReceived}");

            client.Disconnect();
            server.Stop();

            // Restore log assertion behavior
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
