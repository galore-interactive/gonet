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
using GONet.Utils;
using MessagePack.LZ4;
using System.Collections.Concurrent;
using System.Threading;

namespace GONet
{
    public interface IByteArrayCompressionSupport
    {
        void Compress(byte[] uncompressed, ushort uncompressedBytesUsed, out byte[] compressed, out ushort compressedBytesUsed);

        void Uncompress(byte[] compressed, ushort compressedBytesUsed, out byte[] uncompressed, out ushort uncompressedBytesUsed);
    }

    public sealed class LZ4CompressionSupport : IByteArrayCompressionSupport
    {
        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> byteArrayPoolByThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

        public static LZ4CompressionSupport Instance { get; } = new LZ4CompressionSupport();

        public const int ONLY_COMPRESS_IF_LARGER_THAN_BYTE_COUNT = 100; // NOTE: Cannot be less than LZ4MessagePackSerializer.NotCompressionSize;

        const uint IS_COMPRESSED_MASK = (uint)1 << 31;
        const uint HEADER_MASK = IS_COMPRESSED_MASK ^ uint.MaxValue;
        const int HEADER_LENTGH = sizeof(uint);

        LZ4CompressionSupport() { }

        /// <summary>
        /// IMPORTANT: It is incumbent upon the caller to take <paramref name="compressed"/> after this method call and pass it to <see cref="SerializationUtils.ReturnByteArray(byte[])"/> when finished with it =AND= ensure that call is made on the same thread as this method call here was made on!
        /// </summary>
        public void Compress(byte[] uncompressed, ushort uncompressedBytesUsed, out byte[] compressed, out ushort compressedBytesUsed)
        {
            if (uncompressed == null)
            {
                throw new ArgumentNullException(nameof(uncompressed));
            }

            bool shouldCompress = uncompressed.Length > ONLY_COMPRESS_IF_LARGER_THAN_BYTE_COUNT;
            int sizeToBorrow = shouldCompress ? uncompressedBytesUsed + HEADER_LENTGH : LZ4Codec.MaximumOutputLength(uncompressedBytesUsed);
            int compressedBodySize;
            compressed = SerializationUtils.BorrowByteArray(sizeToBorrow);

            { // write body
                if (shouldCompress)
                {
                    int lz4Length = LZ4Codec.Encode(uncompressed, 0, uncompressedBytesUsed, compressed, HEADER_LENTGH, compressed.Length - HEADER_LENTGH);
                    compressedBodySize = lz4Length;
                    compressedBytesUsed = (ushort)(uint)(lz4Length + HEADER_LENTGH);
                }
                else
                {
                    Buffer.BlockCopy(uncompressed, 0, compressed, HEADER_LENTGH, uncompressedBytesUsed);
                    compressedBodySize = uncompressedBytesUsed;
                    compressedBytesUsed = (ushort)(uint)sizeToBorrow;
                }
            }

            { // write length/header
                uint headerBodySizesOnly = (uint)(compressedBodySize << 16) | (uint)uncompressedBytesUsed;
                if (headerBodySizesOnly > HEADER_MASK)
                {
                    SerializationUtils.ReturnByteArray(compressed);
                    const string LARGO = "size of header too large...15-bit max for size of compressed byte count and 16-bit max for size of uncompressed byte count.";
                    throw new ArgumentOutOfRangeException(LARGO);
                }
                uint header = shouldCompress ? (IS_COMPRESSED_MASK | headerBodySizesOnly) : headerBodySizesOnly;
                byte[] headerBytes = Utils.BitConverter.BorrowByteArray();
                Utils.BitConverter.GetBytes(header, headerBytes);
                Buffer.BlockCopy(headerBytes, 0, compressed, 0, HEADER_LENTGH);
                Utils.BitConverter.ReturnByteArray(headerBytes);
            }

            //GONetLog.Debug("uncompressedBytesUsed: " + uncompressedBytesUsed + " compressedBytesUsed: " + compressedBytesUsed);
        }

        /// <summary>
        /// IMPORTANT: It is incumbent upon the caller to take <paramref name="uncompressed"/> after this method call and pass it to <see cref="SerializationUtils.ReturnByteArray(byte[])"/> when finished with it =AND= ensure that call is made on the same thread as this method call here was made on!
        /// </summary>
        public void Uncompress(byte[] compressed, ushort compressedBytesUsed, out byte[] uncompressed, out ushort uncompressedBytesUsed)
        {
            if (compressed == null)
            {
                throw new ArgumentNullException(nameof(compressed));
            }

            if (compressedBytesUsed < HEADER_LENTGH)
            {
                throw new ArgumentOutOfRangeException(nameof(compressedBytesUsed));
            }

            // read length/header
            uint header = System.BitConverter.ToUInt32(compressed, 0);
            uint headerBodySizesOnly = header & HEADER_MASK;
            ushort compressedBodySize = (ushort)(headerBodySizesOnly >> 16);
            uncompressedBytesUsed = (ushort)((headerBodySizesOnly << 16) >> 16);
            uncompressed = SerializationUtils.BorrowByteArray(uncompressedBytesUsed);
            bool isCompressed = (header & IS_COMPRESSED_MASK) == IS_COMPRESSED_MASK;

            // read body
            if (isCompressed)
            {
                LZ4Codec.Decode(compressed, HEADER_LENTGH, compressedBodySize, uncompressed, 0, uncompressedBytesUsed);
            }
            else
            {
                Buffer.BlockCopy(compressed, HEADER_LENTGH, uncompressed, 0, uncompressedBytesUsed);
            }
        }
    }
}
