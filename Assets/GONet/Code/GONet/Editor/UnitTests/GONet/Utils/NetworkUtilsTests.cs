// Assets/Tests/EditMode/NetworkUtilsTests.cs
// Assembly Definition recommended: Tests.EditMode (reference GONet.Runtime)

using NUnit.Framework;
using GONet;                        // adjust if your namespace differs
using System.Net;
using GONet.Utils;
using System.Net.Sockets;

public class NetworkUtilsTests
{
    const ushort TestPort = 43210;

    [TestCase("127.0.0.1")]
    [TestCase("::1")]
    [TestCase("ipv4.google.com")]   // v4‑only DNS
    [TestCase("ipv6.google.com")]   // v6‑only DNS
    public void Parses_Hostname_And_Returns_Endpoint(string host)
    {
        var ep = NetworkUtils.GetIPEndPointFromHostName(host, TestPort);

        Assert.AreEqual(TestPort, ep.Port);

        // Verify address family matches expectation
        if (host.Contains(":") || host.StartsWith("ipv6"))
            Assert.AreEqual(AddressFamily.InterNetworkV6, ep.AddressFamily);
        else
            Assert.AreEqual(AddressFamily.InterNetwork, ep.AddressFamily);
    }
}
