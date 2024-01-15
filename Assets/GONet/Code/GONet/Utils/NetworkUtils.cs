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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GONet.Utils
{
    public static class NetworkUtils
    {
        const string ANY_IP = "0.0.0.0"; // Now obsolete: IPAddress.Any.Address;
        const string LOOPBACK_IP = "127.0.0.1";
        const string LOCALHOST = "localhost";

        public static bool IsIPAddressOnLocalMachine(string ipAddressToCheck)
        {
            if (LOOPBACK_IP == ipAddressToCheck || LOCALHOST == ipAddressToCheck || ANY_IP == ipAddressToCheck)
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
            var endpoint = new IPEndPoint(IPAddress.Any, port);
            Socket socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
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
    }
}
