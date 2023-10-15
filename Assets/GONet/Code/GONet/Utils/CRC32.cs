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

using System.Security.Cryptography;

namespace GONet.Utils
{
    /// <summary>
    /// Source: http://damieng.com/blog/2006/08/08/Calculating_CRC32_in_C_and_NET w/ a few conveniences added (minor):
    /// Commented out functions we won't use (for clarity), added functionality to use a string as input (uses UTF-8 encoding).
    /// Started writing my own, but this is as slick as it gets imo...
    /// </summary>
    public class CRC32 : HashAlgorithm
    {
        /// <summary>
        /// Default used for initialization
        /// </summary>
        public const uint DefaultPolynomial = 0xedb88320;
        /// <summary>
        /// Default used for initialization
        /// </summary>
        public const uint DefaultSeed = 0xffffffff;

        private uint hash;
        private readonly uint seed;
        private readonly uint[] table;
        private static uint[] defaultTable;

        /// <summary>
        /// converts from unsigned int to signed int
        /// </summary>
        /// <param name="value">unsigned int being converted</param>
        /// <returns>converted signed in</returns>
        public static unsafe int UInt32ToInt32(uint value)
        {
            int* result = (int*)&value;
            return *result;
        }

        /// <summary>
        /// converts from signed int to unsigned int
        /// </summary>
        /// <param name="value">signed int</param>
        /// <returns>converted unsigned in</returns>
        public static unsafe uint Int32ToUInt32(int value)
        {
            uint* result = (uint*)&value;
            return *result;
        }

        /// <summary>
        /// converts from unsigned long to signed long
        /// </summary>
        /// <param name="value">unsigned long to be converted</param>
        /// <returns>converted signed long</returns>
        public static unsafe long UInt64ToInt64(ulong value)
        {
            long* result = (long*)&value;
            return *result;
        }

        /// <summary>
        /// converts from signed long to unsigned long
        /// </summary>
        /// <param name="value">signed long to be converted</param>
        /// <returns>converted unsigned long</returns>
        public static unsafe ulong Int64ToUInt64(long value)
        {
            ulong* result = (ulong*)&value;
            return *result;
        }

        public CRC32()
        {
            table = InitializeTable(DefaultPolynomial);
            seed = DefaultSeed;
            Initialize();
        }

        /// <param name="polynomial">specified polynomial used for initialization instead of Default</param>
        /// <param name="seed">specified seed used for initialization instead of default</param>
        public CRC32(uint polynomial, uint seed)
        {
            table = InitializeTable(polynomial);
            this.seed = seed;
            Initialize();
        }

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = UInt32ToBigEndianBytes(~hash);
            this.HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize
        {
            get { return 32; }
        }

        //public static uint Compute(byte[] buffer)
        //{
        //    return ~CalculateHash(InitializeTable(DefaultPolynomial), DefaultSeed, buffer, 0, buffer.Length);
        //}

        //public static uint Compute(uint seed, byte[] buffer)
        //{
        //    return ~CalculateHash(InitializeTable(DefaultPolynomial), seed, buffer, 0, buffer.Length);
        //}

        //public static uint Compute(uint polynomial, uint seed, byte[] buffer)
        //{
        //    return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
        //}

        /// <summary>
        /// Calculates Hash
        /// </summary>
        /// <param name="buffer">temporarily holds data</param>
        /// <param name="start">starting point</param>
        /// <param name="size">how far to go</param>
        /// <returns>hash value</returns>
        public uint Calculate(byte[] buffer, int start, int size)
        {
            uint crc = seed;
            for (int i = start; i < size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
                }
            return crc;
        }

        /// <summary>
        /// Calculates Hash
        /// </summary>
        /// <param name="buffer">temporarily holds data</param>
        /// <param name="start">starting point</param>
        /// <param name="size">how far to go</param>
        /// <returns>hash value</returns>
        public uint Calculate(uint[] buffer, int start, int size)
        {
            uint crc = seed;
            for (int i = start; i < size; i++)
                unchecked
                {
                    uint value = buffer[i];
                    crc = (crc >> 8) ^ table[((byte)(value & byte.MaxValue)) ^ crc & 0xff];
                    crc = (crc >> 8) ^ table[((byte)((value >> 8) & byte.MaxValue)) ^ crc & 0xff];
                    crc = (crc >> 8) ^ table[((byte)((value >> 16) & byte.MaxValue)) ^ crc & 0xff];
                    crc = (crc >> 8) ^ table[((byte)(value >> 24)) ^ crc & 0xff];
                }
            return crc;
        }

        /// <summary>
        /// calculates hash
        /// </summary>
        /// <param name="input">used to create buffer</param>
        /// <returns>hash value</returns>
        public uint Calculate(string input)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(input);
            return Calculate(buffer, 0, buffer.Length);
        }

        private static uint[] InitializeTable(uint polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
                return defaultTable;

            uint[] createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint)i;
                for (int j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

        private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
        {
            uint crc = seed;
            for (int i = start; i < size; i++)
                unchecked
                {
                    crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
                }
            return crc;
        }

        private byte[] UInt32ToBigEndianBytes(uint x)
        {
            return new byte[] {
                (byte)((x >> 24) & 0xff),
                (byte)((x >> 16) & 0xff),
                (byte)((x >> 8) & 0xff),
                (byte)(x & 0xff)
            };
        }
    }
}
