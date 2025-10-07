using System;
using System.Net;
using System.Threading;
using NUnit.Framework;
using NetcodeIO.NET;

namespace GONet.Tests.Netcode_IO
{
    /// <summary>
    /// Integration tests for client-server connection using real threads and network simulation.
    /// Based on GONet's HighPerfTimeSyncIntegrationTests pattern.
    ///
    /// NOTE: This threaded approach has been superseded by the simpler synchronous tests in
    /// NetcodeIOIntegrationTests.cs, which are more reliable and easier to debug.
    /// See ClientServerConnectionTests_Simple.cs and NetcodeIOIntegrationTests.cs for working tests.
    /// </summary>
    [TestFixture]
    public class ClientServerConnectionTests : NetcodeIOTestBase
    {
        [Test]
        [Ignore("Superseded by synchronous tests in NetcodeIOIntegrationTests.cs - see TestClientServerConnection, TestClientServerKeepAlive, etc.")]
        [Timeout(30000)] // 30 second timeout
        public void TestClientServerConnection_WithThreads()
        {
            UnityEngine.Debug.Log("Starting TestClientServerConnection_WithThreads");

            const double DELTA_TIME = 1.0 / 10.0; // 100ms updates
            Server server = null;
            Client client = null;
            bool connected = false;

            try
            {
                // Create server on server thread
                IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), TEST_SERVER_PORT);
                IPEndPoint clientEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), TEST_SERVER_PORT + 100);

                RunOnServerThread(() =>
                {
                    server = new Server(socketMgr.CreateContext(serverEndpoint), 256, TEST_SERVER_PORT, TEST_PROTOCOL_ID, PrivateKey);
                    server.Start(false);
                    server.totalSeconds = GetCurrentTime();
                    UnityEngine.Debug.Log("Server started");
                });

                // Create client on client thread
                RunOnClientThread(() =>
                {
                    client = new Client((endpoint) =>
                    {
                        var socket = socketMgr.CreateContext(clientEndpoint);
                        socket.Bind(clientEndpoint);
                        return socket;
                    });
                    client.totalSeconds = GetCurrentTime();
                    UnityEngine.Debug.Log("Client created");
                });

                // Generate connect token
                ulong clientID = 1000;
                byte[] connectToken = CreateConnectToken(new IPEndPoint[] { serverEndpoint }, clientID, timeoutSeconds: 5);

                // Client connects
                RunOnClientThread(() =>
                {
                    client.Connect(connectToken, false);
                    UnityEngine.Debug.Log($"Client connecting... State: {client.State}");
                });

                // Connection loop - run on main test thread, dispatch work to client/server threads
                int maxIterations = 2000; // 200 seconds max (increased for high latency)
                int iteration = 0;

                while (iteration < maxIterations)
                {
                    iteration++;

                    // Update client FIRST with current time
                    double time = GetCurrentTime();
                    ClientState clientState = RunOnClientThread(() =>
                    {
                        client.totalSeconds = time;
                        client.Tick(time);
                        return client.State;
                    }, timeoutMs: 1000);

                    // Update server with same time
                    RunOnServerThread(() =>
                    {
                        server.totalSeconds = time;
                        server.Tick(time);
                    }, timeoutMs: 1000);

                    // Update network simulator AFTER both client and server tick (critical!)
                    // This processes queued packets
                    AdvanceTime(DELTA_TIME);

                    // Check if client disconnected or connected
                    if (clientState <= ClientState.Disconnected)
                    {
                        UnityEngine.Debug.Log($"Client disconnected at iteration {iteration}. State: {clientState}, Time: {time:F2}s");
                        break;
                    }

                    if (clientState == ClientState.Connected)
                    {
                        UnityEngine.Debug.Log($"Client connected at iteration {iteration}! Time: {time:F2}s");
                        connected = true;
                        break;
                    }

                    // Progress logging
                    if (iteration % 10 == 0)
                    {
                        UnityEngine.Debug.Log($"Iteration {iteration}, ClientState: {clientState}, Time: {time:F2}s");
                    }
                }

                Assert.IsTrue(connected, $"Client failed to connect. Final state: {RunOnClientThread(() => client.State)}");

                // Verify we can send messages
                int clientMessagesReceived = 0;
                int serverMessagesReceived = 0;

                RunOnServerThread(() =>
                {
                    server.OnClientMessageReceived += (sender, payload, size) =>
                    {
                        // Send message back to client
                        sender.SendPayload(payload, size);
                        serverMessagesReceived++;
                    };
                });

                RunOnClientThread(() =>
                {
                    client.OnMessageReceived += (payload, size) =>
                    {
                        clientMessagesReceived++;
                    };
                });

                // Send messages until both client and server have at least 10 messages
                for (int i = 0; i < 1000 && (clientMessagesReceived < 10 || serverMessagesReceived < 10); i++)
                {
                    double time = GetCurrentTime();

                    RunOnClientThread(() =>
                    {
                        client.Tick(time);
                        byte[] testPayload = new byte[256];
                        client.Send(testPayload, testPayload.Length);
                    });

                    RunOnServerThread(() =>
                    {
                        server.Tick(time);
                    });

                    // Update network simulator AFTER both tick (consistent with connection loop)
                    AdvanceTime(DELTA_TIME);

                    if (i % 100 == 0)
                    {
                        UnityEngine.Debug.Log($"Message iteration {i}: Client received {clientMessagesReceived}, Server received {serverMessagesReceived}");
                    }
                }

                Assert.GreaterOrEqual(clientMessagesReceived, 10, "Client did not receive enough messages");
                Assert.GreaterOrEqual(serverMessagesReceived, 10, "Server did not receive enough messages");

                UnityEngine.Debug.Log($"Test PASSED - Client received {clientMessagesReceived}, Server received {serverMessagesReceived}");
            }
            finally
            {
                // Cleanup
                if (client != null)
                {
                    RunOnClientThread(() => client.Disconnect(), timeoutMs: 1000);
                }
                if (server != null)
                {
                    RunOnServerThread(() => server.Stop(), timeoutMs: 1000);
                }
            }
        }
    }
}
