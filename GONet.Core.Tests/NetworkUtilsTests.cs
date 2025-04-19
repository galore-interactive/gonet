using GONet.Utils;
using System.Net.Sockets;

[TestFixture]
public class NetworkUtilsTests
{
    const ushort TestPort = 43210;

    [TestCase("127.0.0.1")]
    [TestCase("::1")]
    [TestCase("ipv4.google.com")]          // v4‑only host
    [TestCase("ipv6.google.com")]          // v6‑only host
    public void Resolves_To_Valid_IPEndPoint(string host)
    {
        var ep = NetworkUtils.GetIPEndPointFromHostName(host, TestPort);
        Assert.That(ep.Port, Is.EqualTo(TestPort));
        Assert.That(ep.AddressFamily, Is.EqualTo(
            host.Contains(":") ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork));
    }
}
