/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GONet.Utils
{
    public static class NetworkUtils
    {
        const string ANY_IP = "[::]"; // IPv6 any address, also works for IPv4 in dual-stack contexts
        const string LOOPBACK_IP = "127.0.0.1";
        const string LOCALHOST = "localhost";
        const string LOOPBACK_IPV6 = "::1";

        public static bool IsIPAddressOnLocalMachine(string ipAddressToCheck)
        {
            if (LOOPBACK_IP == ipAddressToCheck || LOOPBACK_IPV6 == ipAddressToCheck || LOCALHOST == ipAddressToCheck || ANY_IP == ipAddressToCheck)
            {
                return true;
            }
            else
            {
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                return host.AddressList.Any(ipAddress =>
                    (ipAddress.AddressFamily == AddressFamily.InterNetwork || ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    && ipAddress.ToString() == ipAddressToCheck);
            }
        }

        /// <summary>
        /// This is a brute force mechanism to test if we can bind to that port...if not its already taken, if so, its not (and we close/unbind when done)
        /// </summary>
        public static bool IsLocalPortListening(int port)
        {
            var endpoint = new IPEndPoint(IPAddress.IPv6Any, port);
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                // allow dual-stack
                // TODO does this make sense in this contect? socket.DualMode = true;
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                socket.Bind(endpoint);
            }
            catch (SocketException socketException)
            {
                const string IN_USE = "Address already in use";
                if (socketException.ErrorCode == (int)SocketError.AddressAlreadyInUse || socketException.Message == IN_USE)
                {
                    return true;
                }
            }
            finally
            {
                socket.Close();
            }

            return false;
        }

        public static IPEndPoint GetIPEndPointFromHostName(string hostName, int port, bool throwIfMoreThanOneIP = false)
        {
            var addresses = System.Net.Dns.GetHostAddresses(hostName);
            if (addresses.Length == 0)
            {
                throw new ArgumentException("Unable to retrieve address from specified host name.", nameof(hostName));
            }
            else if (throwIfMoreThanOneIP && addresses.Length > 1)
            {
                throw new ArgumentException("There is more than one IP address for the specified host.", nameof(hostName));
            }

            // Prefer IPv6 if available, otherwise use the first address
            var preferredAddress = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6) ?? addresses[0];
            return new IPEndPoint(preferredAddress, port);
        }

        public static string GetEndpointDebugString(EndPoint endpoint)
        {
            if (endpoint == null)
                return "Endpoint is null";

            if (endpoint is IPEndPoint ipEndPoint)
            {
                return $"Endpoint Details:\n" +
                       $"  - Type: IPEndPoint\n" +
                       $"  - Address: {ipEndPoint.Address}\n" +
                       $"  - Port: {ipEndPoint.Port}\n" +
                       $"  - Address Family: {ipEndPoint.AddressFamily}\n" +
                       $"  - Is IPv4: {ipEndPoint.AddressFamily == AddressFamily.InterNetwork}\n" +
                       $"  - Is IPv6: {ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6}\n" +
                       $"  - Is IPv4 Mapped to IPv6: {ipEndPoint.Address.IsIPv4MappedToIPv6}\n" +
                       $"  - Full Address: {ipEndPoint}";
            }
            else
            {
                return $"Endpoint Details:\n" +
                       $"  - Type: {endpoint.GetType().Name}\n" +
                       $"  - Address: {endpoint}";
            }
        }
    }
}
