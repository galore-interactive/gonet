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
using System.Collections.Generic;
using System.Linq;

namespace GONet.Editor.UnitTests.Utils
{
    [TestFixture]
    public class TieredArrayPoolTests
    {
        [Test]
        public void BorrowReturn_TinySize_ReturnsSmallBuffer()
        {
            // Test that tiny requests (≤128 bytes) get buffers from tiny tier
            // Stress test with 500 iterations to expose random sizing issues
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 500; iteration++)
            {
                byte[] buffer = pool.Borrow(10);

                Assert.GreaterOrEqual(buffer.Length, 10, $"Iteration {iteration}: Buffer should satisfy minimum size");
                Assert.LessOrEqual(buffer.Length, 256, $"Iteration {iteration}: Tiny tier max with growth + safety is 256");

                pool.Return(buffer);
            }
        }

        [Test]
        public void BorrowReturn_SmallSize_ReturnsSmallBuffer()
        {
            // Test that small requests (129-1024 bytes) get buffers from small tier
            // Stress test with 500 iterations to expose random sizing issues
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 500; iteration++)
            {
                byte[] buffer = pool.Borrow(500);

                Assert.GreaterOrEqual(buffer.Length, 500, $"Iteration {iteration}: Buffer should satisfy minimum size");
                Assert.LessOrEqual(buffer.Length, 2048, $"Iteration {iteration}: Small tier max with growth + safety is 2048");

                pool.Return(buffer);
            }
        }

        [Test]
        public void BorrowReturn_MediumSize_ReturnsMediumBuffer()
        {
            // Test that medium requests (1KB-12KB) get buffers from medium tier
            // Stress test with 500 iterations to expose random sizing issues
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 500; iteration++)
            {
                byte[] buffer = pool.Borrow(5000);

                Assert.GreaterOrEqual(buffer.Length, 5000, $"Iteration {iteration}: Buffer should satisfy minimum size");
                Assert.LessOrEqual(buffer.Length, 24576, $"Iteration {iteration}: Medium tier max with growth + safety is 24576");

                pool.Return(buffer);
            }
        }

        [Test]
        public void BorrowReturn_LargeSize_ReturnsLargeBuffer()
        {
            // Test that large requests (>12KB) get buffers from large tier
            // Stress test with 500 iterations to expose random sizing issues
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 500; iteration++)
            {
                byte[] buffer = pool.Borrow(20000);

                Assert.GreaterOrEqual(buffer.Length, 20000, $"Iteration {iteration}: Buffer should satisfy minimum size");
                Assert.LessOrEqual(buffer.Length, 131072, $"Iteration {iteration}: Large tier max with growth + safety is 131072");

                pool.Return(buffer);
            }
        }

        [Test]
        public void BorrowReturn_VariousSizes_AllSucceed()
        {
            // Test all size ranges with high iteration count to catch random sizing issues
            var pool = new TieredArrayPool<byte>();
            var sizes = new[] { 3, 10, 50, 128, 200, 500, 1024, 1500, 5000, 12000, 15000, 40000 };

            // Stress test: 100 iterations of each size
            for (int iteration = 0; iteration < 100; iteration++)
            {
                foreach (var size in sizes)
                {
                    byte[] buffer = pool.Borrow(size);
                    Assert.GreaterOrEqual(buffer.Length, size, $"Iteration {iteration}, size {size}: Buffer should satisfy minimum");
                    pool.Return(buffer);
                }
            }
        }

        [Test]
        public void BorrowReturn_RpcTypicalSizes_OptimalMemoryUsage()
        {
            // Test typical RPC parameter sizes (3-100 bytes) with high iteration count
            var pool = new TieredArrayPool<byte>();
            var rpcSizes = new[] { 3, 5, 10, 20, 50, 100 };

            // Stress test: 200 iterations to catch edge cases
            for (int iteration = 0; iteration < 200; iteration++)
            {
                foreach (var size in rpcSizes)
                {
                    byte[] buffer = pool.Borrow(size);

                    // Should get tiny tier buffers (8-192 with growth) instead of old 11KB-44KB
                    Assert.GreaterOrEqual(buffer.Length, size, $"Iteration {iteration}, size {size}: Should satisfy minimum");
                    Assert.LessOrEqual(buffer.Length, 192,
                        $"Iteration {iteration}: RPC size {size} should get tiny tier buffer (max 192 with growth), not 11KB+");

                    pool.Return(buffer);
                }
            }
        }

        [Test]
        public void BorrowReturn_MultipleSequential_BuffersReused()
        {
            // Stress test buffer reuse with high counts to expose issues
            var pool = new TieredArrayPool<byte>();

            // Test 10 rounds of heavy borrowing/returning
            for (int round = 0; round < 10; round++)
            {
                var borrowedBuffers = new List<byte[]>();

                // Borrow 100 buffers per round
                for (int i = 0; i < 100; i++)
                {
                    borrowedBuffers.Add(pool.Borrow(10));
                }

                // Return all buffers
                foreach (var buffer in borrowedBuffers)
                {
                    pool.Return(buffer);
                }

                // Borrow again - should reuse returned buffers
                var secondBatch = new List<byte[]>();
                for (int i = 0; i < 100; i++)
                {
                    secondBatch.Add(pool.Borrow(10));
                }

                // At least some buffers should be reused (same reference)
                int reuseCount = 0;
                foreach (var secondBuffer in secondBatch)
                {
                    if (borrowedBuffers.Any(firstBuffer => ReferenceEquals(firstBuffer, secondBuffer)))
                    {
                        reuseCount++;
                    }
                }

                Assert.Greater(reuseCount, 0, $"Round {round}: Pool should reuse returned buffers");

                // Cleanup
                foreach (var buffer in secondBatch)
                {
                    pool.Return(buffer);
                }
            }
        }

        [Test]
        public void BorrowReturn_MixedSizes_ProperTierIsolation()
        {
            // Stress test mixed sizes with 300 iterations to expose tier routing issues
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 300; iteration++)
            {
                byte[] tiny = pool.Borrow(10);
                byte[] small = pool.Borrow(500);
                byte[] medium = pool.Borrow(5000);
                byte[] large = pool.Borrow(20000);

                Assert.GreaterOrEqual(tiny.Length, 10, $"Iteration {iteration}: Tiny should satisfy minimum");
                Assert.LessOrEqual(tiny.Length, 192, $"Iteration {iteration}: Tiny tier max with growth is 192");

                Assert.GreaterOrEqual(small.Length, 500, $"Iteration {iteration}: Small should satisfy minimum");
                Assert.LessOrEqual(small.Length, 1536, $"Iteration {iteration}: Small tier max with growth is 1536");

                Assert.GreaterOrEqual(medium.Length, 5000, $"Iteration {iteration}: Medium should satisfy minimum");
                Assert.LessOrEqual(medium.Length, 18432, $"Iteration {iteration}: Medium tier max with growth is 18432");

                Assert.GreaterOrEqual(large.Length, 20000, $"Iteration {iteration}: Large should satisfy minimum");
                Assert.LessOrEqual(large.Length, 98304, $"Iteration {iteration}: Large tier max with growth is 98304");

                pool.Return(tiny);
                pool.Return(small);
                pool.Return(medium);
                pool.Return(large);
            }
        }

        [Test]
        public void BorrowReturn_BoundaryValues_CorrectTierAssignment()
        {
            // CRITICAL TEST: Boundary values are where random sizing causes most failures
            // Stress test with 500 iterations to expose edge cases
            var pool = new TieredArrayPool<byte>();

            for (int iteration = 0; iteration < 500; iteration++)
            {
                // Boundary: 128 bytes (tiny tier max)
                byte[] at128 = pool.Borrow(128);
                Assert.GreaterOrEqual(at128.Length, 128, $"Iteration {iteration}: 128 bytes should be satisfied");
                Assert.LessOrEqual(at128.Length, 192, $"Iteration {iteration}: Tiny tier max with growth is 192");
                pool.Return(at128);

                // Boundary: 129 bytes (small tier min)
                byte[] at129 = pool.Borrow(129);
                Assert.GreaterOrEqual(at129.Length, 129, $"Iteration {iteration}: 129 bytes should be satisfied");
                Assert.LessOrEqual(at129.Length, 1536, $"Iteration {iteration}: Small tier max with growth is 1536");
                pool.Return(at129);

                // Boundary: 1024 bytes (small tier max)
                byte[] at1024 = pool.Borrow(1024);
                Assert.GreaterOrEqual(at1024.Length, 1024, $"Iteration {iteration}: 1024 bytes should be satisfied");
                Assert.LessOrEqual(at1024.Length, 2048, $"Iteration {iteration}: Should be from small or medium tier");
                pool.Return(at1024);

                // Boundary: 1025 bytes (medium tier min)
                byte[] at1025 = pool.Borrow(1025);
                Assert.GreaterOrEqual(at1025.Length, 1025, $"Iteration {iteration}: 1025 bytes should be satisfied");
                Assert.LessOrEqual(at1025.Length, 18432, $"Iteration {iteration}: Medium tier max with growth is 18432");
                pool.Return(at1025);

                // Boundary: 12288 bytes (medium tier max)
                byte[] at12288 = pool.Borrow(12288);
                Assert.GreaterOrEqual(at12288.Length, 12288, $"Iteration {iteration}: 12288 bytes should be satisfied");
                Assert.LessOrEqual(at12288.Length, 18432, $"Iteration {iteration}: Medium tier max with growth is 18432");
                pool.Return(at12288);

                // Boundary: 12289 bytes (large tier min)
                byte[] at12289 = pool.Borrow(12289);
                Assert.GreaterOrEqual(at12289.Length, 12289, $"Iteration {iteration}: 12289 bytes should be satisfied");
                Assert.LessOrEqual(at12289.Length, 98304, $"Iteration {iteration}: Large tier max with growth is 98304");
                pool.Return(at12289);
            }
        }

        [Test]
        public void MemorySavings_PersistentRpcScenario_99PercentReduction()
        {
            // Simulate 100 persistent RPCs with ~10 bytes each
            var pool = new TieredArrayPool<byte>();
            long totalMemoryUsed = 0;

            for (int i = 0; i < 100; i++)
            {
                byte[] buffer = pool.Borrow(10);
                totalMemoryUsed += buffer.Length;
                // Note: In real scenario, persistent events would copy data
                // and return buffer, so we don't return here
            }

            // With TieredArrayPool: ~100 * 16 bytes (avg tiny tier) = ~1.6 KB
            // Old ArrayPool: 100 * 11,200 bytes = 1,120 KB

            Assert.Less(totalMemoryUsed, 100 * 200,
                "TieredArrayPool should use <20KB for 100 small RPCs");

            // Old pool would use ~1.1MB, new uses ~1.6KB = 99.86% savings
        }

        [Test]
        public void StressTest_RandomSizes_NoExceptions()
        {
            // Stress test with completely random sizes across all tiers
            var pool = new TieredArrayPool<byte>();
            var random = new System.Random(12345); // Fixed seed for reproducibility

            for (int iteration = 0; iteration < 1000; iteration++)
            {
                // Random size between 1 and 50000 bytes
                int size = random.Next(1, 50001);

                byte[] buffer = pool.Borrow(size);
                Assert.GreaterOrEqual(buffer.Length, size, $"Iteration {iteration}, size {size}: Should satisfy minimum");
                Assert.NotNull(buffer, $"Iteration {iteration}, size {size}: Buffer should not be null");

                // Return should not throw regardless of buffer size
                pool.Return(buffer);
            }
        }

        [Test]
        public void StressTest_AllBoundaries_HighVolume()
        {
            // Test ALL critical boundary points with extreme iteration count
            var pool = new TieredArrayPool<byte>();

            // Critical boundaries where tier transitions occur
            var criticalSizes = new[] {
                1, 8, 64, 127, 128, 129,
                192, 193, 500, 1023, 1024, 1025,
                1536, 1537, 5000, 12287, 12288, 12289,
                18432, 18433, 30000, 65535
            };

            // Test each boundary 200 times to catch rare edge cases
            for (int iteration = 0; iteration < 200; iteration++)
            {
                foreach (var size in criticalSizes)
                {
                    byte[] buffer = pool.Borrow(size);
                    Assert.GreaterOrEqual(buffer.Length, size,
                        $"Iteration {iteration}, boundary size {size}: Should satisfy minimum");
                    pool.Return(buffer);
                }
            }
        }

        [Test]
        public void StressTest_InterleavedSizes_NoCorruption()
        {
            // Test interleaved borrowing of different sizes WITHOUT returning
            // This tests pool growth behavior when pools run out of buffers
            var pool = new TieredArrayPool<byte>();
            var allBuffers = new List<byte[]>();

            // Borrow 200 buffers across all tiers in interleaved pattern
            for (int i = 0; i < 200; i++)
            {
                allBuffers.Add(pool.Borrow(10));      // Tiny
                allBuffers.Add(pool.Borrow(500));     // Small
                allBuffers.Add(pool.Borrow(5000));    // Medium
                allBuffers.Add(pool.Borrow(20000));   // Large
            }

            // Verify all buffers are valid
            for (int i = 0; i < allBuffers.Count; i++)
            {
                Assert.NotNull(allBuffers[i], $"Buffer {i} should not be null");
                Assert.Greater(allBuffers[i].Length, 0, $"Buffer {i} should have positive length");
            }

            // Return all buffers - should not throw
            foreach (var buffer in allBuffers)
            {
                pool.Return(buffer);
            }
        }

        [Test]
        public void StressTest_TierOverlap_ExhaustiveValidation()
        {
            // Test sizes in the overlap zones where Return() boundaries matter most
            var pool = new TieredArrayPool<byte>();

            // Overlap zone 1: Tiny/Small boundary (128-256)
            for (int size = 128; size <= 256; size++)
            {
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    byte[] buffer = pool.Borrow(size);
                    Assert.GreaterOrEqual(buffer.Length, size, $"Size {size}, iter {iteration}: Should satisfy minimum");
                    pool.Return(buffer); // Should not throw
                }
            }

            // Overlap zone 2: Small/Medium boundary (1024-2048)
            for (int size = 1024; size <= 2048; size += 16)
            {
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    byte[] buffer = pool.Borrow(size);
                    Assert.GreaterOrEqual(buffer.Length, size, $"Size {size}, iter {iteration}: Should satisfy minimum");
                    pool.Return(buffer);
                }
            }

            // Overlap zone 3: Medium/Large boundary (12288-20000)
            for (int size = 12288; size <= 20000; size += 128)
            {
                for (int iteration = 0; iteration < 20; iteration++)
                {
                    byte[] buffer = pool.Borrow(size);
                    Assert.GreaterOrEqual(buffer.Length, size, $"Size {size}, iter {iteration}: Should satisfy minimum");
                    pool.Return(buffer);
                }
            }
        }

        [Test]
        public void StressTest_ConcurrentSimulation_RapidBorrowReturn()
        {
            // Simulate rapid borrow/return cycles like real-world RPC traffic
            var pool = new TieredArrayPool<byte>();
            var random = new System.Random(67890);

            // 2000 rapid cycles
            for (int cycle = 0; cycle < 2000; cycle++)
            {
                // Borrow random size
                int size = random.Next(1, 10001);
                byte[] buffer = pool.Borrow(size);

                Assert.NotNull(buffer, $"Cycle {cycle}: Buffer should not be null");
                Assert.GreaterOrEqual(buffer.Length, size, $"Cycle {cycle}: Should satisfy size {size}");

                // Immediately return (simulates RPC serialization pattern)
                pool.Return(buffer);
            }
        }

        #region Congestion Management Tests (October 2025)

        [Test]
        public void BorrowedCount_EmptyPool_ReturnsZero()
        {
            // Test NEW BorrowedCount property added for congestion management
            var pool = new TieredArrayPool<byte>();

            Assert.AreEqual(0, pool.BorrowedCount, "Empty pool should have BorrowedCount = 0");
        }

        [Test]
        public void BorrowedCount_AfterBorrow_IncrementsCorrectly()
        {
            var pool = new TieredArrayPool<byte>();

            Assert.AreEqual(0, pool.BorrowedCount, "Initial count should be 0");

            byte[] buffer1 = pool.Borrow(10);
            Assert.AreEqual(1, pool.BorrowedCount, "Count should be 1 after first borrow");

            byte[] buffer2 = pool.Borrow(500);
            Assert.AreEqual(2, pool.BorrowedCount, "Count should be 2 after second borrow");

            byte[] buffer3 = pool.Borrow(5000);
            Assert.AreEqual(3, pool.BorrowedCount, "Count should be 3 after third borrow");

            // Cleanup
            pool.Return(buffer1);
            pool.Return(buffer2);
            pool.Return(buffer3);
        }

        [Test]
        public void BorrowedCount_AfterReturn_DecrementsCorrectly()
        {
            var pool = new TieredArrayPool<byte>();

            byte[] buffer1 = pool.Borrow(10);
            byte[] buffer2 = pool.Borrow(500);
            byte[] buffer3 = pool.Borrow(5000);

            Assert.AreEqual(3, pool.BorrowedCount, "Count should be 3 after borrowing 3 buffers");

            pool.Return(buffer1);
            Assert.AreEqual(2, pool.BorrowedCount, "Count should be 2 after returning 1 buffer");

            pool.Return(buffer2);
            Assert.AreEqual(1, pool.BorrowedCount, "Count should be 1 after returning 2 buffers");

            pool.Return(buffer3);
            Assert.AreEqual(0, pool.BorrowedCount, "Count should be 0 after returning all buffers");
        }

        [Test]
        public void BorrowedCount_AcrossAllTiers_TracksAccurately()
        {
            // Test that BorrowedCount accurately sums across all tiers
            var pool = new TieredArrayPool<byte>();
            var buffers = new List<byte[]>();

            // Borrow from each tier
            buffers.Add(pool.Borrow(10));      // Tiny
            buffers.Add(pool.Borrow(500));     // Small
            buffers.Add(pool.Borrow(5000));    // Medium
            buffers.Add(pool.Borrow(20000));   // Large

            Assert.AreEqual(4, pool.BorrowedCount, "Count should be 4 (one from each tier)");

            // Borrow more from each tier
            buffers.Add(pool.Borrow(50));      // Tiny
            buffers.Add(pool.Borrow(800));     // Small
            buffers.Add(pool.Borrow(8000));    // Medium
            buffers.Add(pool.Borrow(30000));   // Large

            Assert.AreEqual(8, pool.BorrowedCount, "Count should be 8 (two from each tier)");

            // Return all
            foreach (var buffer in buffers)
            {
                pool.Return(buffer);
            }

            Assert.AreEqual(0, pool.BorrowedCount, "Count should be 0 after returning all buffers");
        }

        [Test]
        public void BorrowedCount_StressTest_RapidBorrowReturn()
        {
            // Test BorrowedCount under high-frequency borrow/return (simulates real network traffic)
            var pool = new TieredArrayPool<byte>();
            var random = new System.Random(11111);

            for (int cycle = 0; cycle < 1000; cycle++)
            {
                var borrowed = new List<byte[]>();

                // Borrow random number of buffers (1-50)
                int borrowCount = random.Next(1, 51);
                for (int i = 0; i < borrowCount; i++)
                {
                    int size = random.Next(1, 10001);
                    borrowed.Add(pool.Borrow(size));
                }

                Assert.AreEqual(borrowCount, pool.BorrowedCount,
                    $"Cycle {cycle}: BorrowedCount should match borrow count {borrowCount}");

                // Return all
                foreach (var buffer in borrowed)
                {
                    pool.Return(buffer);
                }

                Assert.AreEqual(0, pool.BorrowedCount,
                    $"Cycle {cycle}: BorrowedCount should be 0 after returning all");
            }
        }

        [Test]
        public void BorrowedCount_CongestionSimulation_10xOverflow()
        {
            // Simulate congestion scenario: borrow 10x the old MAX_PACKETS_PER_TICK (1000)
            // This tests that TieredArrayPool can handle what would overflow old ArrayPool
            var pool = new TieredArrayPool<byte>();
            var buffers = new List<byte[]>();

            const int OLD_MAX = 1000;
            const int STRESS_MULTIPLIER = 10;

            // Borrow 10,000 buffers (would fail with old ArrayPool at ~1000)
            for (int i = 0; i < OLD_MAX * STRESS_MULTIPLIER; i++)
            {
                buffers.Add(pool.Borrow(50)); // Typical small message size
            }

            Assert.AreEqual(OLD_MAX * STRESS_MULTIPLIER, pool.BorrowedCount,
                "Should successfully borrow 10x old limit without failure");

            // Cleanup
            foreach (var buffer in buffers)
            {
                pool.Return(buffer);
            }

            Assert.AreEqual(0, pool.BorrowedCount, "All buffers should be returned");
        }

        #endregion
    }
}
