using NUnit.Framework;

namespace GONet.Tests.Netcode_IO
{
    /// <summary>
    /// Wrapper for NetcodeIO.NET.Tests.Tests static test methods.
    /// These tests validate the netcode.io layer (lowest networking layer).
    ///
    /// NOTE: Integration tests that require client-server interaction are ignored below
    /// because they fail in Unity Test Runner's synchronous execution model.
    /// They have been ported to NetcodeIOIntegrationTests.cs using a synchronous approach
    /// with manual time progression that works correctly in Unity.
    /// </summary>
    [TestFixture]
    public class NetcodeIOTestWrapper
    {
        [Test]
        public void TestSequence()
        {
            NetcodeIO.NET.Tests.Tests.TestSequence();
        }

        [Test]
        public void TestConnectToken()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectToken();
        }

        [Test]
        public void TestChallengeToken()
        {
            NetcodeIO.NET.Tests.Tests.TestChallengeToken();
        }

        [Test]
        public void TestEncryptionManager()
        {
            NetcodeIO.NET.Tests.Tests.TestEncryptionManager();
        }

        [Test]
        public void TestReplayProtection()
        {
            NetcodeIO.NET.Tests.Tests.TestReplayProtection();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestClientServerConnection")]
        public void TestClientServerConnection()
        {
            NetcodeIO.NET.Tests.Tests.TestClientServerConnection();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestClientServerKeepAlive")]
        public void TestClientServerKeepAlive()
        {
            NetcodeIO.NET.Tests.Tests.TestClientServerKeepAlive();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestClientServerMultipleClients")]
        public void TestClientServerMultipleClients()
        {
            NetcodeIO.NET.Tests.Tests.TestClientServerMultipleClients();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestClientServerMultipleServers")]
        public void TestClientServerMultipleServers()
        {
            NetcodeIO.NET.Tests.Tests.TestClientServerMultipleServers();
        }

        [Test]
        public void TestConnectTokenExpired()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectTokenExpired();
        }

        [Test]
        public void TestClientInvalidConnectToken()
        {
            NetcodeIO.NET.Tests.Tests.TestClientInvalidConnectToken();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestConnectionTimeout")]
        public void TestConnectionTimeout()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionTimeout();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestChallengeResponseTimeout")]
        public void TestChallengeResponseTimeout()
        {
            NetcodeIO.NET.Tests.Tests.TestChallengeResponseTimeout();
        }

        [Test]
        public void TestConnectionRequestTimeout()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionRequestTimeout();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestConnectionDenied")]
        public void TestConnectionDenied()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionDenied();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestClientSideDisconnect")]
        public void TestClientSideDisconnect()
        {
            NetcodeIO.NET.Tests.Tests.TestClientSideDisconnect();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestServerSideDisconnect")]
        public void TestServerSideDisconnect()
        {
            NetcodeIO.NET.Tests.Tests.TestServerSideDisconnect();
        }

        [Test]
        [Ignore("Requires threading - replaced by NetcodeIOIntegrationTests.TestReconnect")]
        public void TestReconnect()
        {
            NetcodeIO.NET.Tests.Tests.TestReconnect();
        }

        [Test]
        public void TestConnectionRequestPacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionRequestPacket();
        }

        [Test]
        public void TestConnectionDeniedPacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionDeniedPacket();
        }

        [Test]
        public void TestConnectionKeepAlivePacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionKeepAlivePacket();
        }

        [Test]
        public void TestConnectionChallengePacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionChallengePacket();
        }

        [Test]
        public void TestConnectionPayloadPacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionPayloadPacket();
        }

        [Test]
        public void TestConnectionDisconnectPacket()
        {
            NetcodeIO.NET.Tests.Tests.TestConnectionDisconnectPacket();
        }

        // Note: SoakTestClientServerConnection intentionally not included
        // It's a long-running stress test (runs for N minutes), not suitable for automated test suite
    }
}
