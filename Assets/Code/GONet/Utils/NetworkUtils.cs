using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GONet.Utils
{
    public static class NetworkUtils
    {
        const string LOOPBACK_IP = "127.0.0.1";
        const string LOCALHOST = "localhost";

        public static bool IsIPAddressOnLocalMachine(string ipAddressToCheck)
        {
            if (LOOPBACK_IP == ipAddressToCheck || LOCALHOST == ipAddressToCheck)
            {
                return true;
            }
            else
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.Any(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork && ipAddress.ToString() == ipAddressToCheck);
            }
        }

        /// <summary>
        /// This is a brute force mechanism to test if we can bind to that port...if not its already taken, if so, its not (and we close/unbind when done)
        /// </summary>
        public static bool IsLocalPortListening(int port)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(LOOPBACK_IP), port);
            Socket socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.Bind(endpoint);
                return false;
            }
            catch (SocketException)
            {
                return true;
            }
            finally
            {
                socket.Close();
            }
        }
    }
}
