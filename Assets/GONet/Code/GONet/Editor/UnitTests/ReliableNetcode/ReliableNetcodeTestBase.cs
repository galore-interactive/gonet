using System;
using System.Net;
using NUnit.Framework;
using NetcodeIO.NET;
using ReliableNetcode;
using ReliableNetcode.Utils;

namespace GONet.Tests.ReliableNetcode
{
    /// <summary>
    /// Base class for ReliableNetcode integration tests.
    /// Provides test infrastructure for testing reliable/unreliable channels.
    ///
    /// Based on patterns from NetcodeIOIntegrationTests.cs.
    /// </summary>
    public class ReliableNetcodeTestBase
    {
        protected const ulong TEST_PROTOCOL_ID = 0x1122334455667788L;
        protected const int TEST_SERVER_PORT = 40000;

        protected static readonly byte[] PrivateKey = new byte[]
        {
            0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
            0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
            0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
            0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1
        };

        /// <summary>
        /// Creates a NetworkSimulator socket manager for testing.
        /// </summary>
        protected NetworkSimulatorSocketManager CreateSocketManager(int latencyMS = 50, int jitterMS = 10, int packetLoss = 0)
        {
            var socketMgr = new NetworkSimulatorSocketManager();
            socketMgr.LatencyMS = latencyMS;
            socketMgr.JitterMS = jitterMS;
            socketMgr.PacketLossChance = packetLoss;
            socketMgr.DuplicatePacketChance = 0;
            socketMgr.AutoTime = false;
            return socketMgr;
        }

        /// <summary>
        /// Creates a Netcode.IO server with pre-bound socket (avoids IPv6Any rebinding).
        /// </summary>
        protected Server CreateServer(NetworkSimulatorSocketManager socketMgr, IPEndPoint endpoint)
        {
            var serverSocket = socketMgr.CreateContext(endpoint);
            serverSocket.Bind(endpoint);
            Server server = new Server(serverSocket, 256, endpoint.Port, TEST_PROTOCOL_ID, PrivateKey);

            // Manually initialize server (avoid Start() rebinding to IPv6Any)
            var resetMethod = typeof(Server).GetMethod("resetConnectTokenHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod.Invoke(server, null);
            var isRunningField = typeof(Server).GetField("isRunning",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isRunningField.SetValue(server, true);

            return server;
        }

        /// <summary>
        /// Creates a Netcode.IO client with pre-bound socket.
        /// </summary>
        protected Client CreateClient(NetworkSimulatorSocketManager socketMgr, IPEndPoint endpoint)
        {
            Client client = new Client((ep) => {
                var socket = socketMgr.CreateContext(endpoint);
                socket.Bind(endpoint);
                return socket;
            });
            return client;
        }

        /// <summary>
        /// Generates a connect token for client authentication.
        /// </summary>
        protected byte[] CreateConnectToken(IPEndPoint[] serverEndpoints, ulong clientID = 0, int timeoutSeconds = 30)
        {
            byte[] userData = new byte[256];
            TokenFactory tokenFactory = new TokenFactory(TEST_PROTOCOL_ID, PrivateKey);
            return tokenFactory.GenerateConnectToken(serverEndpoints, timeoutSeconds, 5, 0, clientID, userData);
        }

        /// <summary>
        /// Creates a ReliableEndpoint wrapped around a Netcode.IO client.
        /// </summary>
        protected ReliableEndpoint CreateReliableEndpoint(Client client)
        {
            // Use reflection to get the internal socket from client
            var socketField = typeof(Client).GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var socket = (SocketContext)socketField.GetValue(client);

            if (socket == null)
            {
                throw new InvalidOperationException("Client socket is null. Client must be connected first.");
            }

            // Get the remote endpoint that the client is connected to
            var endpointField = typeof(Client).GetField("serverEndpoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var endpoints = (IPEndPoint[])endpointField.GetValue(client);
            IPEndPoint remoteEndpoint = endpoints[0];

            ReliableEndpoint endpoint = new ReliableEndpoint();
            endpoint.BindSocket(socket, remoteEndpoint);

            return endpoint;
        }

        /// <summary>
        /// Connects client to server and waits for connection to establish.
        /// Returns true if connected successfully.
        /// </summary>
        protected bool ConnectClientToServer(Client client, Server server, NetworkSimulatorSocketManager socketMgr,
            byte[] connectToken, double startTime, out double finalTime, int maxIterations = 500)
        {
            double time = startTime;
            const double dt = 1.0 / 10.0; // 100ms updates

            client.totalSeconds = time;
            server.totalSeconds = time;
            client.Connect(connectToken, false);

            for (int i = 0; i < maxIterations; i++)
            {
                time += dt;
                client.Tick(time);
                server.Tick(time);
                socketMgr.Update(time);

                if (client.State == ClientState.Connected)
                {
                    finalTime = time;
                    return true;
                }

                if (client.State <= ClientState.Disconnected)
                {
                    break;
                }
            }

            finalTime = time;
            return false;
        }

        /// <summary>
        /// Advances time by ticking client, server, and socket manager.
        /// </summary>
        protected void AdvanceTime(Client client, Server server, NetworkSimulatorSocketManager socketMgr, double deltaTime, ref double currentTime)
        {
            currentTime += deltaTime;
            client.Tick(currentTime);
            server.Tick(currentTime);
            socketMgr.Update(currentTime);
        }
    }
}
