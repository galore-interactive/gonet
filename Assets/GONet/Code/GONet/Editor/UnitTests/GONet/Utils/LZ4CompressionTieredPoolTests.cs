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

using NUnit.Framework;
using GONet.Utils;
using System;
using System.Linq;

namespace GONet.Editor.UnitTests.Utils
{
    [TestFixture]
    public class LZ4CompressionTieredPoolTests
    {
        private byte[] CreateTestData(int size, byte seed = 42)
        {
            var data = new byte[size];
            var random = new Random(seed);
            random.NextBytes(data);
            return data;
        }

        [Test]
        public void Compress_TinyData_PreservesIntegrity()
        {
            // Test compression with tiny data (RPC parameter size)
            byte[] original = CreateTestData(10);

            LZ4CompressionSupport.Instance.Compress(
                original, (ushort)original.Length,
                out byte[] compressed, out ushort compressedSize
            );

            LZ4CompressionSupport.Instance.Uncompress(
                compressed, compressedSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(original.Length, uncompressedSize, "Uncompressed size should match original");
            CollectionAssert.AreEqual(original, uncompressed.Take(uncompressedSize).ToArray(),
                "Data integrity should be preserved");

            SerializationUtils.ReturnByteArray(compressed);
            SerializationUtils.ReturnByteArray(uncompressed);
        }

        [Test]
        public void Compress_SmallData_PreservesIntegrity()
        {
            // Test with small message size (500 bytes)
            byte[] original = CreateTestData(500);

            LZ4CompressionSupport.Instance.Compress(
                original, (ushort)original.Length,
                out byte[] compressed, out ushort compressedSize
            );

            LZ4CompressionSupport.Instance.Uncompress(
                compressed, compressedSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(original.Length, uncompressedSize);
            CollectionAssert.AreEqual(original, uncompressed.Take(uncompressedSize).ToArray());

            SerializationUtils.ReturnByteArray(compressed);
            SerializationUtils.ReturnByteArray(uncompressed);
        }

        [Test]
        public void Compress_MediumData_PreservesIntegrity()
        {
            // Test with medium network message (5KB)
            byte[] original = CreateTestData(5000);

            LZ4CompressionSupport.Instance.Compress(
                original, (ushort)original.Length,
                out byte[] compressed, out ushort compressedSize
            );

            LZ4CompressionSupport.Instance.Uncompress(
                compressed, compressedSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(original.Length, uncompressedSize);
            CollectionAssert.AreEqual(original, uncompressed.Take(uncompressedSize).ToArray());

            SerializationUtils.ReturnByteArray(compressed);
            SerializationUtils.ReturnByteArray(uncompressed);
        }

        [Test]
        public void Compress_LargeData_PreservesIntegrity()
        {
            // Test with large bundle (32KB - within ushort limit of 65535)
            byte[] original = CreateTestData(32000);

            LZ4CompressionSupport.Instance.Compress(
                original, (ushort)original.Length,
                out byte[] compressed, out ushort compressedSize
            );

            LZ4CompressionSupport.Instance.Uncompress(
                compressed, compressedSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(original.Length, uncompressedSize);
            CollectionAssert.AreEqual(original, uncompressed.Take(uncompressedSize).ToArray());

            SerializationUtils.ReturnByteArray(compressed);
            SerializationUtils.ReturnByteArray(uncompressed);
        }

        [Test]
        public void Compress_VariableSizedBuffers_HeaderStoresLogicalSize()
        {
            // Verify that LZ4 header stores logical sizes, not buffer sizes
            // This proves TieredArrayPool compatibility

            int[] testSizes = { 5, 50, 500, 5000 };

            foreach (int size in testSizes)
            {
                byte[] original = CreateTestData(size);

                LZ4CompressionSupport.Instance.Compress(
                    original, (ushort)size,
                    out byte[] compressed, out ushort compressedSize
                );

                // Read header (first 4 bytes)
                uint header = System.BitConverter.ToUInt32(compressed, 0);
                uint headerMask = 0x7FFFFFFF; // Remove compression bit
                uint headerBodySizesOnly = header & headerMask;
                ushort compressedBodySize = (ushort)(headerBodySizesOnly >> 16);
                ushort uncompressedSizeFromHeader = (ushort)((headerBodySizesOnly << 16) >> 16);

                // Header should store the LOGICAL size, not the buffer size
                Assert.AreEqual(size, uncompressedSizeFromHeader,
                    $"Header should store logical size {size}, not buffer size");

                // Decompress and verify
                LZ4CompressionSupport.Instance.Uncompress(
                    compressed, compressedSize,
                    out byte[] uncompressed, out ushort uncompressedSize
                );

                Assert.AreEqual(size, uncompressedSize, "Decompressed size should match original");
                CollectionAssert.AreEqual(
                    original.Take(size),
                    uncompressed.Take(uncompressedSize),
                    "Data should match exactly"
                );

                SerializationUtils.ReturnByteArray(compressed);
                SerializationUtils.ReturnByteArray(uncompressed);
            }
        }

        [Test]
        public void Compress_MultipleRounds_DifferentTiers_AllSucceed()
        {
            // Simulate real-world scenario: multiple compressions from different tiers

            for (int round = 0; round < 10; round++)
            {
                // Tiny tier
                byte[] tiny = CreateTestData(10, (byte)round);
                LZ4CompressionSupport.Instance.Compress(tiny, (ushort)10, out var compTiny, out var compTinySize);
                LZ4CompressionSupport.Instance.Uncompress(compTiny, compTinySize, out var uncompTiny, out var uncompTinySize);
                Assert.AreEqual(10, uncompTinySize);
                CollectionAssert.AreEqual(tiny, uncompTiny.Take(10).ToArray());
                SerializationUtils.ReturnByteArray(compTiny);
                SerializationUtils.ReturnByteArray(uncompTiny);

                // Small tier
                byte[] small = CreateTestData(500, (byte)round);
                LZ4CompressionSupport.Instance.Compress(small, 500, out var compSmall, out var compSmallSize);
                LZ4CompressionSupport.Instance.Uncompress(compSmall, compSmallSize, out var uncompSmall, out var uncompSmallSize);
                Assert.AreEqual(500, uncompSmallSize);
                CollectionAssert.AreEqual(small, uncompSmall.Take(500).ToArray());
                SerializationUtils.ReturnByteArray(compSmall);
                SerializationUtils.ReturnByteArray(uncompSmall);

                // Medium tier
                byte[] medium = CreateTestData(5000, (byte)round);
                LZ4CompressionSupport.Instance.Compress(medium, 5000, out var compMedium, out var compMediumSize);
                LZ4CompressionSupport.Instance.Uncompress(compMedium, compMediumSize, out var uncompMedium, out var uncompMediumSize);
                Assert.AreEqual(5000, uncompMediumSize);
                CollectionAssert.AreEqual(medium, uncompMedium.Take(5000).ToArray());
                SerializationUtils.ReturnByteArray(compMedium);
                SerializationUtils.ReturnByteArray(uncompMedium);
            }
        }

        [Test]
        public void Compress_BelowThreshold_DoesNotCompress()
        {
            // Data below 100 bytes should not be compressed (only header added)
            byte[] tiny = CreateTestData(50);

            LZ4CompressionSupport.Instance.Compress(
                tiny, 50,
                out byte[] result, out ushort resultSize
            );

            // Result should be original data + 4-byte header (minimum)
            // Note: Actual size depends on LZ4 implementation details
            Assert.LessOrEqual(resultSize, 100,
                "Small data below threshold should not be significantly larger");
            Assert.GreaterOrEqual(resultSize, 54,
                "Should be at least original size + header");

            SerializationUtils.ReturnByteArray(result);
        }

        [Test]
        public void Compress_AboveThreshold_DoesCompress()
        {
            // Data above 100 bytes should be compressed
            // Create highly compressible data (repeating pattern)
            byte[] compressible = new byte[500];
            for (int i = 0; i < compressible.Length; i++)
            {
                compressible[i] = (byte)(i % 10);
            }

            LZ4CompressionSupport.Instance.Compress(
                compressible, 500,
                out byte[] result, out ushort resultSize
            );

            // Compressed size should be less than original (for compressible data)
            Assert.Less(resultSize, 500, "Compressible data above threshold should be compressed");

            // Verify decompression works
            LZ4CompressionSupport.Instance.Uncompress(
                result, resultSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(500, uncompressedSize);
            CollectionAssert.AreEqual(compressible, uncompressed.Take(500).ToArray());

            SerializationUtils.ReturnByteArray(result);
            SerializationUtils.ReturnByteArray(uncompressed);
        }

        [Test]
        public void Compress_IncompressibleData_AboveThreshold_SucceedsWithWorstCaseExpansion()
        {
            // CRITICAL TEST: Verifies the fix for LZ4 "corrupted block" errors
            // Incompressible random data causes worst-case LZ4 expansion
            // This test ensures we allocate enough buffer space for LZ4's output

            // Test at boundary of tiny/small tier (128-200 bytes)
            int[] criticalSizes = { 128, 150, 200, 256, 500, 1000 };

            foreach (int size in criticalSizes)
            {
                // Create incompressible random data
                byte[] incompressible = CreateTestData(size);

                // This should NOT throw "LZ4 block is corrupted, or invalid length has been given"
                LZ4CompressionSupport.Instance.Compress(
                    incompressible, (ushort)size,
                    out byte[] compressed, out ushort compressedSize
                );

                Assert.Greater(compressedSize, 0, $"Compressed size should be > 0 for {size} bytes");

                // Verify the compressed data includes proper header
                Assert.GreaterOrEqual(compressedSize, 4, "Should have at least 4-byte header");

                // Decompress and verify integrity
                LZ4CompressionSupport.Instance.Uncompress(
                    compressed, compressedSize,
                    out byte[] uncompressed, out ushort uncompressedSize
                );

                Assert.AreEqual(size, uncompressedSize, $"Uncompressed size should match original for {size} bytes");
                CollectionAssert.AreEqual(
                    incompressible.Take(size).ToArray(),
                    uncompressed.Take(uncompressedSize).ToArray(),
                    $"Data integrity should be preserved for {size} bytes"
                );

                SerializationUtils.ReturnByteArray(compressed);
                SerializationUtils.ReturnByteArray(uncompressed);
            }
        }

        [Test]
        public void Compress_EdgeCaseSizes_JustAboveThreshold_AllSucceed()
        {
            // Test sizes just above compression threshold (100 bytes)
            // These are most likely to trigger buffer allocation bugs
            int[] edgeSizes = { 101, 105, 110, 120, 127, 128, 129 };

            foreach (int size in edgeSizes)
            {
                byte[] data = CreateTestData(size);

                // Should not throw
                LZ4CompressionSupport.Instance.Compress(
                    data, (ushort)size,
                    out byte[] compressed, out ushort compressedSize
                );

                LZ4CompressionSupport.Instance.Uncompress(
                    compressed, compressedSize,
                    out byte[] uncompressed, out ushort uncompressedSize
                );

                Assert.AreEqual(size, uncompressedSize, $"Size {size} should round-trip correctly");
                CollectionAssert.AreEqual(data, uncompressed.Take(size).ToArray(),
                    $"Data integrity for size {size}");

                SerializationUtils.ReturnByteArray(compressed);
                SerializationUtils.ReturnByteArray(uncompressed);
            }
        }

        [Test]
        public void Compress_TierBoundaries_AllTiersHandleCorrectly()
        {
            // Test at exact tier boundaries to verify pool allocation
            // Tiny: 8-128, Small: 128-1024, Medium: 1024-12288, Large: 12288-65536
            //
            // IMPORTANT: LZ4CompressionSupport has a header limitation:
            // - Max uncompressed size: 65,535 bytes (16-bit)
            // - Max compressed size: 32,767 bytes (15-bit)
            // Random incompressible data can expand to ~1.01x original size
            // So we test up to ~32KB to stay within compressed size limit
            int[] tierBoundaries = {
                8, 127, 128, 129,           // Tiny boundaries
                1023, 1024, 1025,           // Small boundaries
                12287, 12288, 12289,        // Medium boundaries
                20000, 32000                // Large tier (within header limits)
            };

            foreach (int size in tierBoundaries)
            {
                byte[] data = CreateTestData(size);

                LZ4CompressionSupport.Instance.Compress(
                    data, (ushort)size,
                    out byte[] compressed, out ushort compressedSize
                );

                LZ4CompressionSupport.Instance.Uncompress(
                    compressed, compressedSize,
                    out byte[] uncompressed, out ushort uncompressedSize
                );

                Assert.AreEqual(size, uncompressedSize, $"Tier boundary {size} should work correctly");
                CollectionAssert.AreEqual(data, uncompressed.Take(size).ToArray(),
                    $"Data integrity at tier boundary {size}");

                SerializationUtils.ReturnByteArray(compressed);
                SerializationUtils.ReturnByteArray(uncompressed);
            }
        }

        [Test]
        public void Compress_StressTest_ManyRapidCompressions_NoCorruption()
        {
            // Stress test: Rapid compressions across all tiers
            // Simulates high-frequency network traffic
            var random = new Random(12345);

            for (int i = 0; i < 100; i++)
            {
                // Random size between 10 and 5000 bytes
                int size = random.Next(10, 5000);
                byte[] data = CreateTestData(size, (byte)i);

                LZ4CompressionSupport.Instance.Compress(
                    data, (ushort)size,
                    out byte[] compressed, out ushort compressedSize
                );

                LZ4CompressionSupport.Instance.Uncompress(
                    compressed, compressedSize,
                    out byte[] uncompressed, out ushort uncompressedSize
                );

                Assert.AreEqual(size, uncompressedSize, $"Iteration {i} size {size} failed");
                CollectionAssert.AreEqual(data, uncompressed.Take(size).ToArray(),
                    $"Iteration {i} size {size} data corrupted");

                SerializationUtils.ReturnByteArray(compressed);
                SerializationUtils.ReturnByteArray(uncompressed);
            }
        }

        [Test]
        public void Compress_HeaderSizeLimit_ThrowsForTooLargeData()
        {
            // DOCUMENTS KNOWN LIMITATION: LZ4CompressionSupport has header size constraints
            // - 15-bit compressed size: max 32,767 bytes
            // - 16-bit uncompressed size: max 65,535 bytes
            //
            // For incompressible data, compressed size ≈ uncompressed size + overhead
            // So effective max is ~32KB for random data
            //
            // MTU_x32 (44,800 bytes) exceeds this limit and will throw ArgumentOutOfRangeException
            // This is EXPECTED behavior - not a bug

            byte[] tooLarge = CreateTestData(44800); // MTU_x32

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            {
                LZ4CompressionSupport.Instance.Compress(
                    tooLarge, (ushort)44800,
                    out byte[] compressed, out ushort compressedSize
                );
            }, "Data larger than ~32KB should throw ArgumentOutOfRangeException due to header size limits");
        }

        [Test]
        public void Compress_MaxSafeSize_32KB_Succeeds()
        {
            // Verify that 32KB (max safe size for incompressible data) works correctly
            // This is the practical upper limit for LZ4CompressionSupport with random data

            byte[] maxSafeSize = CreateTestData(32000);

            // Should NOT throw
            LZ4CompressionSupport.Instance.Compress(
                maxSafeSize, 32000,
                out byte[] compressed, out ushort compressedSize
            );

            LZ4CompressionSupport.Instance.Uncompress(
                compressed, compressedSize,
                out byte[] uncompressed, out ushort uncompressedSize
            );

            Assert.AreEqual(32000, uncompressedSize, "32KB should compress/decompress successfully");
            CollectionAssert.AreEqual(maxSafeSize, uncompressed.Take(32000).ToArray(),
                "Data integrity at max safe size");

            SerializationUtils.ReturnByteArray(compressed);
            SerializationUtils.ReturnByteArray(uncompressed);
        }
    }
}
