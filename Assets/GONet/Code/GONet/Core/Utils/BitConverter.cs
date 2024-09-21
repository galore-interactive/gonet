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
using System.Collections.Concurrent;
using System.Security;
using System.Threading;

namespace GONet.Utils
{
    /// <summary>
    /// If the private <see cref="ArrayPool{T}"/> is configured properly, this clasee
    /// does not allocate memory during runtime like would be the case when using <see cref="System.BitConverter"/>.
    /// </summary>
    public unsafe static class BitConverter
    {
        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> byteArrayPoolByThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

        /// <summary>
        /// Use this to borrow byte arrays as needed for the GetBytes calls.
        /// Ensure you subsequently call <see cref=""/>
        /// </summary>
        /// <returns>byte array of size 8</returns>
        public static byte[] BorrowByteArray()
        {
            ArrayPool<byte> arrayPool;
            if (!byteArrayPoolByThreadMap.TryGetValue(Thread.CurrentThread, out arrayPool))
            {
                arrayPool = new ArrayPool<byte>(25, 5, 8, 8);
                byteArrayPoolByThreadMap[Thread.CurrentThread] = arrayPool;
            }
            return arrayPool.Borrow();
        }

        /// <summary>
        /// PRE: Required that <paramref name="borrowed"/> was returned from a call to <see cref="BorrowByteArray(int)"/> and not already passed in here.
        /// </summary>
        public static void ReturnByteArray(byte[] borrowed)
        {
            byteArrayPoolByThreadMap[Thread.CurrentThread].Return(borrowed);
        }

        /// <param name="value">input</param>
        /// <param name="destination">input and output</param>
        /// <param name="offset">offset to use in <paramref name="destination"/> when placing the <paramref name="value"/></param>
        public static void GetBytes(bool value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(bool) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(bool*)(ptrToStart + offset) = value;
            }
        }

        public static void GetBytes(char value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(char) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(char*)(ptrToStart + offset) = value;
            }
        }

        [SecuritySafeCritical]
        public static void GetBytes(short value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(short) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(short*)(ptrToStart + offset) = value;
            }
        }

        [SecuritySafeCritical]
        public static void GetBytes(int value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(int) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(int*)(ptrToStart + offset) = value;
            }
        }

        [SecuritySafeCritical]
        public static void GetBytes(long value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(long) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(long*)(ptrToStart + offset) = value;
            }
        }

        public static void GetBytes(ushort value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(ushort) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(ushort*)(ptrToStart + offset) = value;
            }
        }

        public static void GetBytes(uint value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(uint) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(uint*)(ptrToStart + offset) = value;
            }
        }

        public static void GetBytes(ulong value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(ulong) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(ulong*)(ptrToStart + offset) = value;
            }
        }

        [SecuritySafeCritical]
        public static void GetBytes(float value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(float) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(float*)(ptrToStart + offset) = value;
            }
        }

        [SecuritySafeCritical]
        public static void GetBytes(double value, byte[] destination, int offset = 0)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (offset < 0 || (offset + sizeof(double) > destination.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            fixed (byte* ptrToStart = destination)
            {
                *(double*)(ptrToStart + offset) = value;
            }
        }
    }
}
