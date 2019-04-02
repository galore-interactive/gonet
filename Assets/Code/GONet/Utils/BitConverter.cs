using System;
using System.Security;

namespace GONet.Utils
{
    /// <summary>
    /// If the private <see cref="ArrayPool{T}"/> is configured properly, this clasee
    /// does not allocate memory during runtime like would be the case when using <see cref="System.BitConverter"/>.
    /// </summary>
    public unsafe static class BitConverter
    {
        /// <summary>
        /// Use this to borrow and return byte arrays as needed for the GetBytes calls.
        /// </summary>
        public static readonly ArrayPool<byte> ByteArrayPool = new ArrayPool<byte>(25, 5, 8, 8);

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
