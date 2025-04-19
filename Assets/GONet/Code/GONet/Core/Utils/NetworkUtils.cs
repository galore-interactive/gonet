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
using System.Collections.Generic;
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

        public static bool AreSameAddressFamilyOrMapped(IPAddress a, IPAddress b) =>
            a.Equals(b) ||
                (a.AddressFamily != b.AddressFamily &&
                    (a.MapToIPv4().Equals(b) || a.MapToIPv6().Equals(b)));

        /// <summary>
        /// Returns <c>true</c> when the two <see cref="EndPoint"/>s refer to the same
        /// IP *and* both ports match, treating IPv4‑mapped IPv6 addresses
        /// (<c>::ffff:x.x.x.x</c>) as equivalent to their raw‑IPv4 form.
        /// <para/>
        /// If either <see cref="EndPoint"/> is not an <see cref="IPEndPoint"/>,
        /// the method returns <c>false</c>.
        /// </summary>
        public static bool AreSameAddressFamilyOrMapped(EndPoint aEP, EndPoint bEP)
        {
            // must be IPEndPoint instances
            if (aEP is not IPEndPoint a || bEP is not IPEndPoint b)
                return false;

            // ports must match first
            if (a.Port != b.Port)
                return false;

            // identical addresses → early‑out
            if (a.Address.Equals(b.Address))
                return true;

            // cross‑family: treat v4‑mapped‑v6 as the same host
            if (a.AddressFamily != b.AddressFamily)
            {
                if (a.AddressFamily == AddressFamily.InterNetworkV6 && a.Address.IsIPv4MappedToIPv6 &&
                    a.Address.MapToIPv4().Equals(b.Address))
                    return true;

                if (b.AddressFamily == AddressFamily.InterNetworkV6 && b.Address.IsIPv4MappedToIPv6 &&
                    b.Address.MapToIPv4().Equals(a.Address))
                    return true;
            }

            return false;
        }

        public static bool DoEndpointsMatch(IPEndPoint listen4, IPEndPoint listen6, IPEndPoint tokenEP)
        {
            // 1. Port must match exactly
            if (tokenEP.Port != listen4.Port && tokenEP.Port != listen6.Port)
            {
                return false;
            }

            // 2. wildcard bind → accept any address
            if (listen4.Address.Equals(IPAddress.Any) || listen4.Address.Equals(IPAddress.IPv6Any) ||
                listen6.Address.Equals(IPAddress.Any) || listen6.Address.Equals(IPAddress.IPv6Any))
            {
                return true;   // port already matched above
            }

            // 3. Compare addresses with v4‑mapped equivalence
            bool addrMatches =
                AreSameAddressFamilyOrMapped(tokenEP.Address, listen4.Address) ||
                AreSameAddressFamilyOrMapped(tokenEP.Address, listen6.Address);

            return addrMatches;
        }

        public static bool AreSameIP(IPAddress a, IPAddress b)
        {
            if (a.Equals(b)) return true;

            // treat v4‑mapped‑v6 as equal to raw v4
            if (a.AddressFamily != b.AddressFamily)
            {
                if (a.IsIPv4MappedToIPv6 && a.MapToIPv4().Equals(b)) return true;
                if (b.IsIPv4MappedToIPv6 && b.MapToIPv4().Equals(a)) return true;
            }
            return false;
        }

        public static IEnumerable<IPEndPoint> BuildDualStackEndpointList(string host, int port)
        {
            // 1. Resolve whatever the user typed.
            IPAddress[] resolved;
            if (!IPAddress.TryParse(host, out var literal))
                resolved = Dns.GetHostAddresses(host);
            else
                resolved = new[] { literal };

            bool hasV4 = resolved.Any(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            bool hasV6 = resolved.Any(ip => ip.AddressFamily == AddressFamily.InterNetworkV6);

            var list = new List<IPEndPoint>(
                resolved.Select(ip => new IPEndPoint(ip, port)));

            // 2a. If we only got IPv4 but we KNOW the server is dual‑stack,
            //     inject the mapped‑v6 form *or* ::1 for loop‑back.
            if (!hasV6)
            {
                if (IPAddress.IsLoopback(resolved[0]))
                    list.Insert(0, new IPEndPoint(IPAddress.IPv6Loopback, port));
                else
                    list.Insert(0, new IPEndPoint(resolved[0].MapToIPv6(), port));
            }

            // 2b. If we only got IPv6, inject IPv4.
            if (!hasV4)
            {
                if (resolved[0].IsIPv4MappedToIPv6)
                    list.Add(new IPEndPoint(resolved[0].MapToIPv4(), port));
                else if (IPAddress.IsLoopback(resolved[0]))
                    list.Add(new IPEndPoint(IPAddress.Loopback, port));
                // else: we can’t infer a public v4; leave list unchanged
            }

            /* Optional: put IPv6 first so the client tries it before v4 */
            return list.OrderBy(ep => ep.AddressFamily == AddressFamily.InterNetworkV6 ? 0 : 1);
        }
    }
}
