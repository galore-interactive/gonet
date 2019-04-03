using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GONet.Utils
{
    public static class NetworkUtils
    {
        public static bool IsIPAddressOnLocalMachine(string ipAddressToCheck)
        {
            const string loopbackIP = "127.0.0.1";
            const string localhost = "localhost";
            if (loopbackIP == ipAddressToCheck || localhost == ipAddressToCheck)
            {
                return true;
            }
            else
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.Any(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork && ipAddress.ToString() == ipAddressToCheck);
            }
        }

        public static bool IsLocalPortListening(int port)
        {
            bool isListening = (from p in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners() where p.Port == port select p).Count() == 1;
            return isListening;
        }
    }
}
