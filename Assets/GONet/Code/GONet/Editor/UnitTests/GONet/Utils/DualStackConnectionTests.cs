using NetcodeIO.NET;
using NUnit.Framework;
using System;
using System.Threading;

namespace GONet.Tests
{
    [TestFixture]
    public class DualStackConnectionThreadedTests
    {
        const int Port = 46_000;

        /*
        GONetServer _server;
        Thread _serverThread;
        volatile bool _serverRunning;       // thread‑safe flag

        [SetUp]
        public void Setup()
        {
            _server = new GONetServer(maxClientCount: 10, Port);
            _server.Start();                // open sockets

            _serverRunning = true;
            _serverThread = new Thread(ServerLoop) { IsBackground = true };
            _serverThread.Start();
        }

        void ServerLoop()
        {
            // tight loop that mimics Unity's update until flag flipped in TearDown
            while (_serverRunning)
            {
                _server.Update();
                Thread.Sleep(10);           // ~100 Hz, keeps CPU low
            }
        }

        [TearDown]
        public void Teardown()
        {
            _serverRunning = false;         // signal thread to stop
            _serverThread?.Join();

            _server.Stop();                 // close sockets, free resources
        }

        [Test]
        public void Server_Binds_IPv6Any_Client_V4_and_V6_Connect()
        {
            //var client4 = new GONetClient();
            var client6 = new GONetClient();

            try
            {
                const int TimeoutSeconds = 10;
                //client4.ConnectToServer("127.0.0.1", Port, TimeoutSeconds);
                client6.ConnectToServer("::1", Port, TimeoutSeconds);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(TimeoutSeconds))
                {
                    //client4.Update();
                    client6.Update();
                    Thread.Sleep(10); // sleep a bit as to not peg the CPU
                                      //if (client4.IsConnectedToServer && client6.IsConnectedToServer) break;
                    if (client6.IsConnectedToServer) break;
                }

                //Assert.IsTrue(client4.IsConnectedToServer, "IPv4 client failed to connect");
                Assert.IsTrue(client6.IsConnectedToServer, "IPv6 client failed to connect");
            }
            finally
            {
                //client4.Disconnect();
                client6.Disconnect();
            }
        }
        */

        [Test]
        [Ignore("Test fails due to port binding conflicts when multiple tests run in parallel. " +
                "Requires refactoring to use dynamic ports or proper test isolation. " +
                "See: SocketException 'Only one usage of each socket address is normally permitted.'")]
        public void Server_Binds_IPv6Any_Client_V4_and_V6_Connect()
        {
            var _server = new GONetServer(maxClientCount: 10, Port);
            var client4 = new GONetClient();
            var client6 = new GONetClient();

            try
            {
                _server.Start();                // open sockets

                const int TimeoutSeconds = 10;
                client4.ConnectToServer("127.0.0.1", Port, TimeoutSeconds);
                client6.ConnectToServer("::1", Port, TimeoutSeconds);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.Elapsed < TimeSpan.FromSeconds(TimeoutSeconds))
                {
                    client4.Update();
                    client6.Update();

                    _server.Update();

                    Thread.Sleep(10); // sleep a bit as to not peg the CPU

                    if (client4.IsConnectedToServer && client6.IsConnectedToServer) break;
                    //if (client4.IsConnectedToServer) break;
                    //if (client6.IsConnectedToServer) break;
                }

                Assert.IsTrue(client4.IsConnectedToServer, "IPv4 client failed to connect");
                Assert.IsTrue(client6.IsConnectedToServer, "IPv6 client failed to connect");
            }
            finally
            {
                client4.Disconnect();
                client6.Disconnect();
                _server.Stop();
            }
        }
    }
}