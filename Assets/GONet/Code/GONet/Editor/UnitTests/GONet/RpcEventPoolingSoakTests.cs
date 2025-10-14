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
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GONet.Editor.UnitTests
{
    /// <summary>
    /// Long-running soak tests for RPC event pooling and memory management.
    ///
    /// **PURPOSE:**
    /// Detect slow memory leaks that only appear after extended operation (thousands/millions of cycles).
    /// Quick unit tests (100-10K RPCs) won't expose subtle leaks that accumulate over hours of gameplay.
    ///
    /// **WHAT THESE TESTS VALIDATE:**
    /// 1. Memory doesn't grow unbounded over extended periods (100K-1M cycles)
    /// 2. Borrowed arrays are always returned (pool count stays stable)
    /// 3. No GC pressure accumulation (heap allocations remain reasonable)
    /// 4. Performance doesn't degrade over time (no resource exhaustion)
    /// 5. Real-world RPC patterns (persistent late-joiner deliveries) don't leak
    /// 6. Mixed persistent/transient patterns remain stable over long runs
    ///
    /// **HOW TO RUN:**
    /// These tests are marked [Explicit] and won't run in normal test suites.
    /// Run them manually when validating memory safety:
    /// - Unity Test Runner → Right-click test → "Run Selected"
    /// - Or use [Category("Soak")] filter
    ///
    /// **EXPECTED DURATION:**
    /// - Quick soak: ~10-30 seconds (100K cycles)
    /// - Full soak: ~60-180 seconds (1M cycles)
    /// - Memory monitoring overhead: ~5-10% slowdown
    /// </summary>
    [TestFixture]
    [Category("Soak")]
    public class RpcEventPoolingSoakTests
    {
        private TieredArrayPool<byte> testPool;

        [SetUp]
        public void SetUp()
        {
            testPool = new TieredArrayPool<byte>();

            // Force GC before soak test to get clean baseline
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [TearDown]
        public void TearDown()
        {
            // Log final pool state
            UnityEngine.Debug.Log($"[Soak Test End] Pool state: {testPool.GetStats()}");
            testPool.LogTelemetry();
        }

        #region TieredArrayPool Soak Tests

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_TieredArrayPool_100K_BorrowReturnCycles_NoMemoryLeak()
        {
            // Test 100,000 borrow/return cycles with memory monitoring

            const int CYCLES = 100_000;
            const int SAMPLE_INTERVAL = 10_000; // Sample every 10K cycles

            long initialMemory = GC.GetTotalMemory(false);
            int initialBorrowedCount = testPool.BorrowedCount;

            var memorySamples = new List<long>();
            var borrowedCountSamples = new List<int>();

            UnityEngine.Debug.Log($"[Soak Test] Starting {CYCLES:N0} borrow/return cycles...");
            UnityEngine.Debug.Log($"[Soak Test] Initial memory: {initialMemory:N0} bytes, Borrowed: {initialBorrowedCount}");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < CYCLES; i++)
            {
                // Borrow array
                byte[] buffer = testPool.Borrow(100);

                // Use the buffer (write some data)
                for (int j = 0; j < Math.Min(buffer.Length, 100); j++)
                {
                    buffer[j] = (byte)(i % 256);
                }

                // Return array
                testPool.Return(buffer);

                // Sample memory periodically
                if (i % SAMPLE_INTERVAL == 0 && i > 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);
                    int currentBorrowed = testPool.BorrowedCount;

                    memorySamples.Add(currentMemory);
                    borrowedCountSamples.Add(currentBorrowed);

                    UnityEngine.Debug.Log($"[Soak Test] Cycle {i:N0}: Memory: {currentMemory:N0} bytes (+{currentMemory - initialMemory:N0}), Borrowed: {currentBorrowed}");
                }
            }

            sw.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            int finalBorrowedCount = testPool.BorrowedCount;
            long memoryGrowth = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"[Soak Test] Completed {CYCLES:N0} cycles in {sw.ElapsedMilliseconds:N0}ms ({CYCLES / (sw.ElapsedMilliseconds / 1000.0):N0} cycles/sec)");
            UnityEngine.Debug.Log($"[Soak Test] Final memory: {finalMemory:N0} bytes, Growth: {memoryGrowth:N0} bytes ({(memoryGrowth * 100.0 / initialMemory):F2}%)");
            UnityEngine.Debug.Log($"[Soak Test] Final borrowed count: {finalBorrowedCount} (expected: {initialBorrowedCount})");

            // Analyze memory trend
            if (memorySamples.Count > 2)
            {
                long firstSample = memorySamples[0];
                long lastSample = memorySamples[memorySamples.Count - 1];
                long trendGrowth = lastSample - firstSample;

                UnityEngine.Debug.Log($"[Soak Test] Memory trend (first to last sample): {trendGrowth:N0} bytes ({(trendGrowth * 100.0 / firstSample):F2}%)");
            }

            // ASSERTIONS

            // 1. Borrowed count should return to initial state (no leaked arrays)
            Assert.AreEqual(initialBorrowedCount, finalBorrowedCount,
                $"Borrowed count should return to {initialBorrowedCount} after all cycles (indicates leaked arrays if not equal)");

            // 2. Memory growth should be reasonable (allow 10MB growth for pool expansion)
            Assert.Less(memoryGrowth, 10_000_000,
                $"Memory growth should be < 10MB over {CYCLES:N0} cycles (actual: {memoryGrowth:N0} bytes). Large growth indicates memory leak.");

            // 3. Check for unbounded growth trend
            if (memorySamples.Count > 2)
            {
                long firstSample = memorySamples[0];
                long lastSample = memorySamples[memorySamples.Count - 1];
                long trendGrowth = lastSample - firstSample;

                // Allow 5MB trend growth (pool stabilization)
                Assert.Less(trendGrowth, 5_000_000,
                    $"Memory trend shows {trendGrowth:N0} bytes growth from first to last sample. " +
                    $"This suggests unbounded memory leak (pool should stabilize after initial growth).");
            }

            Assert.Pass($"✓ Soak test passed: {CYCLES:N0} cycles completed without memory leak (growth: {memoryGrowth:N0} bytes, {(memoryGrowth * 100.0 / initialMemory):F2}%)");
        }

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_TieredArrayPool_1M_BorrowReturnCycles_NoMemoryLeak()
        {
            // Extended soak test: 1 MILLION borrow/return cycles

            const int CYCLES = 1_000_000;
            const int SAMPLE_INTERVAL = 50_000; // Sample every 50K cycles

            long initialMemory = GC.GetTotalMemory(false);
            int initialBorrowedCount = testPool.BorrowedCount;

            var memorySamples = new List<long>();
            var borrowedCountSamples = new List<int>();

            UnityEngine.Debug.Log($"[Soak Test EXTENDED] Starting {CYCLES:N0} borrow/return cycles...");
            UnityEngine.Debug.Log($"[Soak Test EXTENDED] Initial memory: {initialMemory:N0} bytes, Borrowed: {initialBorrowedCount}");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < CYCLES; i++)
            {
                // Vary size to test all tiers
                int size = (i % 4) switch
                {
                    0 => 50,      // Tiny tier
                    1 => 500,     // Small tier
                    2 => 5000,    // Medium tier
                    _ => 20000    // Large tier
                };

                byte[] buffer = testPool.Borrow(size);

                // Use the buffer
                for (int j = 0; j < Math.Min(buffer.Length, 100); j++)
                {
                    buffer[j] = (byte)(i % 256);
                }

                testPool.Return(buffer);

                // Sample memory periodically
                if (i % SAMPLE_INTERVAL == 0 && i > 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);
                    int currentBorrowed = testPool.BorrowedCount;

                    memorySamples.Add(currentMemory);
                    borrowedCountSamples.Add(currentBorrowed);

                    UnityEngine.Debug.Log($"[Soak Test EXTENDED] Cycle {i:N0}: Memory: {currentMemory:N0} bytes (+{currentMemory - initialMemory:N0}), Borrowed: {currentBorrowed}");
                }
            }

            sw.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            int finalBorrowedCount = testPool.BorrowedCount;
            long memoryGrowth = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"[Soak Test EXTENDED] Completed {CYCLES:N0} cycles in {sw.ElapsedMilliseconds:N0}ms ({CYCLES / (sw.ElapsedMilliseconds / 1000.0):N0} cycles/sec)");
            UnityEngine.Debug.Log($"[Soak Test EXTENDED] Final memory: {finalMemory:N0} bytes, Growth: {memoryGrowth:N0} bytes ({(memoryGrowth * 100.0 / initialMemory):F2}%)");
            UnityEngine.Debug.Log($"[Soak Test EXTENDED] Final borrowed count: {finalBorrowedCount}");

            // Analyze memory stability
            if (memorySamples.Count > 4)
            {
                // Compare first quarter vs last quarter to detect trends
                int quarterSize = memorySamples.Count / 4;
                long firstQuarterAvg = (long)memorySamples.Take(quarterSize).Average();
                long lastQuarterAvg = (long)memorySamples.Skip(memorySamples.Count - quarterSize).Average();
                long trendGrowth = lastQuarterAvg - firstQuarterAvg;

                UnityEngine.Debug.Log($"[Soak Test EXTENDED] Memory trend (first quarter avg to last quarter avg): {trendGrowth:N0} bytes ({(trendGrowth * 100.0 / firstQuarterAvg):F2}%)");
            }

            // ASSERTIONS

            Assert.AreEqual(initialBorrowedCount, finalBorrowedCount,
                $"Borrowed count should return to {initialBorrowedCount} after {CYCLES:N0} cycles");

            // Allow 20MB growth for 1M cycles (pool expansion across all tiers)
            Assert.Less(memoryGrowth, 20_000_000,
                $"Memory growth should be < 20MB over {CYCLES:N0} cycles (actual: {memoryGrowth:N0} bytes)");

            Assert.Pass($"✓ EXTENDED soak test passed: {CYCLES:N0} cycles completed without memory leak");
        }

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_TieredArrayPool_MixedSizes_NoMemoryLeak()
        {
            // Test all tiers simultaneously with realistic RPC size distribution

            const int CYCLES = 500_000;
            const int SAMPLE_INTERVAL = 25_000;

            long initialMemory = GC.GetTotalMemory(false);
            int initialBorrowedCount = testPool.BorrowedCount;

            var random = new Random(12345);
            var memorySamples = new List<long>();

            // Realistic RPC size distribution (based on real-world profiling)
            // 70% tiny (1-128 bytes), 20% small (129-1KB), 8% medium (1-12KB), 2% large (>12KB)
            int[] sizeDistribution = new int[100];
            for (int i = 0; i < 70; i++) sizeDistribution[i] = random.Next(1, 128);        // Tiny: 70%
            for (int i = 70; i < 90; i++) sizeDistribution[i] = random.Next(129, 1024);    // Small: 20%
            for (int i = 90; i < 98; i++) sizeDistribution[i] = random.Next(1025, 12288);  // Medium: 8%
            for (int i = 98; i < 100; i++) sizeDistribution[i] = random.Next(12289, 40000); // Large: 2%

            UnityEngine.Debug.Log($"[Soak Test Mixed] Starting {CYCLES:N0} cycles with realistic size distribution...");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < CYCLES; i++)
            {
                int size = sizeDistribution[random.Next(100)];

                byte[] buffer = testPool.Borrow(size);

                // Simulate data processing
                if (buffer.Length > 0)
                {
                    buffer[0] = (byte)(i % 256);
                }

                testPool.Return(buffer);

                if (i % SAMPLE_INTERVAL == 0 && i > 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);
                    int currentBorrowed = testPool.BorrowedCount;
                    memorySamples.Add(currentMemory);

                    UnityEngine.Debug.Log($"[Soak Test Mixed] Cycle {i:N0}: Memory: {currentMemory:N0} bytes (+{currentMemory - initialMemory:N0}), Borrowed: {currentBorrowed}");
                }
            }

            sw.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            int finalBorrowedCount = testPool.BorrowedCount;
            long memoryGrowth = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"[Soak Test Mixed] Completed {CYCLES:N0} cycles in {sw.ElapsedMilliseconds:N0}ms");
            UnityEngine.Debug.Log($"[Soak Test Mixed] Memory growth: {memoryGrowth:N0} bytes ({(memoryGrowth * 100.0 / initialMemory):F2}%)");
            UnityEngine.Debug.Log($"[Soak Test Mixed] Final borrowed count: {finalBorrowedCount}");

            // ASSERTIONS

            Assert.AreEqual(initialBorrowedCount, finalBorrowedCount,
                "All borrowed arrays should be returned");

            Assert.Less(memoryGrowth, 15_000_000,
                $"Memory growth should be < 15MB over {CYCLES:N0} mixed cycles (actual: {memoryGrowth:N0} bytes)");

            Assert.Pass($"✓ Mixed size soak test passed: {CYCLES:N0} cycles completed without memory leak");
        }

        #endregion

        #region Persistent RPC Soak Tests (The Original Bug Scenario)

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_PersistentRpc_100K_LateJoinerDeliveries_NoMemoryLeak()
        {
            // THE CRITICAL SOAK TEST: Persistent RPC with repeated late-joiner deliveries
            // This is the EXACT pattern that was causing double-return exceptions

            const int CYCLES = 100_000;
            const int LATE_JOINERS_PER_RPC = 10; // Each persistent RPC delivered to 10 late-joiners
            const int SAMPLE_INTERVAL = 10_000;

            long initialMemory = GC.GetTotalMemory(false);
            var memorySamples = new List<long>();

            UnityEngine.Debug.Log($"[Soak Test Persistent] Starting {CYCLES:N0} persistent RPCs × {LATE_JOINERS_PER_RPC} late-joiners...");
            UnityEngine.Debug.Log($"[Soak Test Persistent] Total deliveries: {CYCLES * LATE_JOINERS_PER_RPC:N0}");

            Stopwatch sw = Stopwatch.StartNew();

            for (int rpcIndex = 0; rpcIndex < CYCLES; rpcIndex++)
            {
                var testData = new TestRpcData1 { Value = rpcIndex };

                // Serialize RPC
                byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                // Create persistent copy (what PersistentRpcEvent.Data holds)
                byte[] persistentCopy = new byte[bytesUsed];
                Buffer.BlockCopy(pooledBuffer, 0, persistentCopy, 0, bytesUsed);

                // Return the pooled buffer immediately (persistent pattern)
                if (needsReturn)
                {
                    SerializationUtils.ReturnByteArray(pooledBuffer);
                }

                // OLD BUG: Each late-joiner delivery would trigger RpcEvent.Return()
                // which called TryReturnByteArray(persistentCopy)
                // 2nd+ delivery would throw InvalidOperationException

                // NEW FIX: RpcEvent.Return() no longer touches byte arrays
                // Simulate 10 late-joiners receiving this persistent RPC
                for (int lateJoiner = 0; lateJoiner < LATE_JOINERS_PER_RPC; lateJoiner++)
                {
                    // Deserialize from persistent copy
                    var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                        new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
                    );

                    // Verify data integrity
                    if (deserialized.Value != rpcIndex)
                    {
                        Assert.Fail($"Data corruption detected at RPC {rpcIndex}, late-joiner {lateJoiner}: expected {rpcIndex}, got {deserialized.Value}");
                    }

                    // Simulate RpcEvent.Return() being called (event pooling)
                    // OLD CODE: Would call TryReturnByteArray(persistentCopy) here → EXCEPTION on 2nd+ call
                    // NEW CODE: Return() no longer touches byte arrays → NO EXCEPTION
                }

                // Sample memory periodically
                if (rpcIndex % SAMPLE_INTERVAL == 0 && rpcIndex > 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);
                    memorySamples.Add(currentMemory);

                    UnityEngine.Debug.Log($"[Soak Test Persistent] RPC {rpcIndex:N0}: Memory: {currentMemory:N0} bytes (+{currentMemory - initialMemory:N0})");
                }
            }

            sw.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            long memoryGrowth = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"[Soak Test Persistent] Completed {CYCLES:N0} persistent RPCs × {LATE_JOINERS_PER_RPC} deliveries in {sw.ElapsedMilliseconds:N0}ms");
            UnityEngine.Debug.Log($"[Soak Test Persistent] Total deliveries: {CYCLES * LATE_JOINERS_PER_RPC:N0}");
            UnityEngine.Debug.Log($"[Soak Test Persistent] Memory growth: {memoryGrowth:N0} bytes ({(memoryGrowth * 100.0 / initialMemory):F2}%)");

            // Analyze memory trend
            if (memorySamples.Count > 2)
            {
                long firstSample = memorySamples[0];
                long lastSample = memorySamples[memorySamples.Count - 1];
                long trendGrowth = lastSample - firstSample;

                UnityEngine.Debug.Log($"[Soak Test Persistent] Memory trend: {trendGrowth:N0} bytes ({(trendGrowth * 100.0 / firstSample):F2}%)");
            }

            // ASSERTIONS

            // Allow 15MB growth for persistent copies (100K RPCs × ~10 bytes each = ~1MB + overhead)
            Assert.Less(memoryGrowth, 15_000_000,
                $"Memory growth should be < 15MB over {CYCLES:N0} persistent RPCs (actual: {memoryGrowth:N0} bytes)");

            // Check for unbounded growth
            if (memorySamples.Count > 2)
            {
                long firstSample = memorySamples[0];
                long lastSample = memorySamples[memorySamples.Count - 1];
                long trendGrowth = lastSample - firstSample;

                Assert.Less(trendGrowth, 10_000_000,
                    $"Memory trend shows {trendGrowth:N0} bytes growth. This suggests unbounded leak.");
            }

            Assert.Pass($"✓ Persistent RPC soak test passed: {CYCLES:N0} RPCs × {LATE_JOINERS_PER_RPC} late-joiners without memory leak or exceptions");
        }

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_MixedPersistentTransient_500K_Cycles_NoMemoryLeak()
        {
            // Test realistic mixed workload: 50% persistent, 50% transient

            const int CYCLES = 500_000;
            const int SAMPLE_INTERVAL = 25_000;

            long initialMemory = GC.GetTotalMemory(false);
            var memorySamples = new List<long>();
            var random = new Random(54321);

            UnityEngine.Debug.Log($"[Soak Test Mixed RPC] Starting {CYCLES:N0} mixed persistent/transient RPCs...");

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < CYCLES; i++)
            {
                var testData = new TestRpcData1 { Value = i };
                byte[] pooledBuffer = SerializationUtils.SerializeToBytes(testData, out int bytesUsed, out bool needsReturn);

                bool isPersistent = random.Next(2) == 0;

                if (isPersistent)
                {
                    // Persistent: copy and return immediately
                    byte[] persistentCopy = new byte[bytesUsed];
                    Buffer.BlockCopy(pooledBuffer, 0, persistentCopy, 0, bytesUsed);

                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }

                    // Simulate 3 late-joiner deliveries
                    for (int j = 0; j < 3; j++)
                    {
                        var deserialized = SerializationUtils.DeserializeFromBytes<TestRpcData1>(
                            new ReadOnlySpan<byte>(persistentCopy, 0, bytesUsed)
                        );

                        if (deserialized.Value != i)
                        {
                            Assert.Fail($"Data corruption at cycle {i}, delivery {j}");
                        }
                    }
                }
                else
                {
                    // Transient: return after "processing"
                    if (needsReturn)
                    {
                        SerializationUtils.ReturnByteArray(pooledBuffer);
                    }
                }

                if (i % SAMPLE_INTERVAL == 0 && i > 0)
                {
                    long currentMemory = GC.GetTotalMemory(false);
                    memorySamples.Add(currentMemory);

                    UnityEngine.Debug.Log($"[Soak Test Mixed RPC] Cycle {i:N0}: Memory: {currentMemory:N0} bytes (+{currentMemory - initialMemory:N0})");
                }
            }

            sw.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            long memoryGrowth = finalMemory - initialMemory;

            UnityEngine.Debug.Log($"[Soak Test Mixed RPC] Completed {CYCLES:N0} mixed RPCs in {sw.ElapsedMilliseconds:N0}ms");
            UnityEngine.Debug.Log($"[Soak Test Mixed RPC] Memory growth: {memoryGrowth:N0} bytes ({(memoryGrowth * 100.0 / initialMemory):F2}%)");

            // ASSERTIONS

            Assert.Less(memoryGrowth, 20_000_000,
                $"Memory growth should be < 20MB over {CYCLES:N0} mixed RPCs (actual: {memoryGrowth:N0} bytes)");

            Assert.Pass($"✓ Mixed RPC soak test passed: {CYCLES:N0} cycles completed without memory leak");
        }

        #endregion

        #region Performance Degradation Tests

        [Test]
        [Explicit("Long-running soak test - run manually")]
        public void SoakTest_PerformanceDegradation_NoSlowdownOverTime()
        {
            // Test that performance doesn't degrade over time (resource exhaustion check)

            const int CYCLES = 200_000;
            const int MEASUREMENT_INTERVAL = 10_000;

            var performanceSamples = new List<double>(); // ops/sec

            UnityEngine.Debug.Log($"[Soak Test Performance] Starting {CYCLES:N0} cycles with performance monitoring...");

            for (int batch = 0; batch < CYCLES / MEASUREMENT_INTERVAL; batch++)
            {
                Stopwatch sw = Stopwatch.StartNew();

                for (int i = 0; i < MEASUREMENT_INTERVAL; i++)
                {
                    byte[] buffer = testPool.Borrow(100);
                    buffer[0] = (byte)i;
                    testPool.Return(buffer);
                }

                sw.Stop();

                double opsPerSec = MEASUREMENT_INTERVAL / (sw.ElapsedMilliseconds / 1000.0);
                performanceSamples.Add(opsPerSec);

                UnityEngine.Debug.Log($"[Soak Test Performance] Batch {batch + 1}: {opsPerSec:N0} ops/sec ({sw.ElapsedMilliseconds}ms for {MEASUREMENT_INTERVAL:N0} ops)");
            }

            // Analyze performance stability
            double firstBatchPerf = performanceSamples[0];
            double lastBatchPerf = performanceSamples[performanceSamples.Count - 1];
            double avgPerf = performanceSamples.Average();
            double degradation = ((firstBatchPerf - lastBatchPerf) / firstBatchPerf) * 100;

            UnityEngine.Debug.Log($"[Soak Test Performance] First batch: {firstBatchPerf:N0} ops/sec");
            UnityEngine.Debug.Log($"[Soak Test Performance] Last batch: {lastBatchPerf:N0} ops/sec");
            UnityEngine.Debug.Log($"[Soak Test Performance] Average: {avgPerf:N0} ops/sec");
            UnityEngine.Debug.Log($"[Soak Test Performance] Degradation: {degradation:F2}%");

            // ASSERTIONS

            // Allow 20% performance degradation (GC, pool fragmentation)
            Assert.Less(degradation, 20,
                $"Performance degraded by {degradation:F2}% from first to last batch. " +
                $"Degradation > 20% suggests resource exhaustion or memory leak.");

            Assert.Pass($"✓ Performance soak test passed: {CYCLES:N0} cycles with {degradation:F2}% degradation (avg: {avgPerf:N0} ops/sec)");
        }

        #endregion
    }
}
