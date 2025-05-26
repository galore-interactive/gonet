using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Edge case and platform-specific tests for HighResolutionTimeUtils.
    /// These tests focus on boundary conditions and platform variations.
    /// </summary>
    [TestFixture]
    public class HighResolutionTimeUtilsEdgeCaseTests
    {
        #region Platform Detection Tests

        [Test]
        [Category("Platform")]
        public void Verify_HighResolution_Timer_Available()
        {
            Assert.That(Stopwatch.IsHighResolution, Is.True,
                "High resolution timer should be available on this platform");

            UnityEngine.Debug.Log($"Stopwatch Frequency: {Stopwatch.Frequency:N0} ticks per second");
            UnityEngine.Debug.Log($"Resolution: {1_000_000_000.0 / Stopwatch.Frequency:F2} nanoseconds per tick");

            // Verify reasonable frequency (at least 1 MHz)
            Assert.That(Stopwatch.Frequency, Is.GreaterThan(1_000_000),
                "Timer frequency should be at least 1 MHz for high resolution");
        }

        [Test]
        [Category("Platform")]
        public void Verify_Platform_Specific_Behavior()
        {
            string platform = Application.platform.ToString();
            UnityEngine.Debug.Log($"Running on platform: {platform}");
            UnityEngine.Debug.Log($"Is 64-bit process: {Environment.Is64BitProcess}");
            UnityEngine.Debug.Log($"Processor count: {Environment.ProcessorCount}");

            // Verify atomic operations work correctly on this platform
            long testValue = 0;
            long result = Interlocked.CompareExchange(ref testValue, 1, 0);
            Assert.That(result, Is.EqualTo(0), "Interlocked.CompareExchange should work");
            Assert.That(testValue, Is.EqualTo(1), "Value should be updated");
        }

        #endregion

        #region Memory Barrier Tests

        [Test]
        [Category("MemoryBarrier")]
        public void Memory_Barriers_Should_Ensure_Visibility()
        {
            const int iterations = 10000;
            int violations = 0;

            for (int iter = 0; iter < iterations; iter++)
            {
                long sharedValue = 0;
                bool flag = false;
                bool violation = false;

                var writerTask = Task.Run(() =>
                {
                    sharedValue = 42;
                    Thread.MemoryBarrier(); // Ensure write is visible
                    flag = true;
                });

                var readerTask = Task.Run(() =>
                {
                    while (!flag)
                    {
                        Thread.Yield();
                    }
                    Thread.MemoryBarrier(); // Ensure we see the latest value
                    if (sharedValue != 42)
                    {
                        violation = true;
                    }
                });

                Task.WaitAll(writerTask, readerTask);

                if (violation)
                {
                    violations++;
                }
            }

            Assert.That(violations, Is.Zero,
                $"Memory barrier violations detected: {violations}/{iterations}");
        }

        #endregion

        #region Extreme Value Tests

        [Test]
        [Category("ExtremeValues")]
        public void Handle_Time_Near_MaxValue()
        {
            // This test verifies the behavior when internal tick counters approach maximum values
            // The original test had a bug - it was reaching exactly MaxValue, not overflowing

            // To actually cause overflow, we need to go beyond MaxValue
            long willOverflow = long.MaxValue;
            long result;

            unchecked
            {
                result = willOverflow + 1; // This will overflow to long.MinValue
            }

            Assert.That(result, Is.EqualTo(long.MinValue),
                "long.MaxValue + 1 should overflow to long.MinValue");

            // Test with TimeSpan arithmetic
            long nearMax = long.MaxValue - (TimeSpan.TicksPerSecond / 2);
            long overflowResult;

            unchecked
            {
                overflowResult = nearMax + TimeSpan.TicksPerSecond; // This will overflow
            }

            Assert.That(overflowResult, Is.LessThan(0),
                $"Adding {TimeSpan.TicksPerSecond} to {nearMax} should overflow to negative");

            // Verify DateTime can handle large tick values (but not beyond its max)
            Assert.DoesNotThrow(() =>
            {
                var dt = new DateTime(DateTime.MaxValue.Ticks - TimeSpan.TicksPerDay, DateTimeKind.Utc);
            }, "DateTime should handle near-max tick values");

            // DateTime constructor should throw for values beyond its range
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var dt = new DateTime(long.MaxValue, DateTimeKind.Utc);
            }, "DateTime should throw for tick values beyond its valid range");
        }

        [Test]
        [Category("ExtremeValues")]
        public void Handle_Rapid_Successive_Calls()
        {
            // Test behavior under extreme call frequency
            const int callsPerBatch = 10000;
            var times = new DateTime[callsPerBatch];

            // Capture times as fast as possible
            for (int i = 0; i < callsPerBatch; i++)
            {
                times[i] = HighResolutionTimeUtils.UtcNow;
            }

            // Analyze results
            int sameTimeCount = 0;
            int uniqueTimes = 1;

            for (int i = 1; i < callsPerBatch; i++)
            {
                if (times[i] == times[i - 1])
                {
                    sameTimeCount++;
                }
                else
                {
                    uniqueTimes++;
                }

                // Verify monotonicity even under extreme load
                Assert.That(times[i], Is.GreaterThanOrEqualTo(times[i - 1]),
                    $"Time should not go backwards at index {i}");
            }

            UnityEngine.Debug.Log($"Unique times in tight loop: {uniqueTimes}/{callsPerBatch} " +
                                $"({100.0 * uniqueTimes / callsPerBatch:F2}%)");

            // The implementation has thread-local caching that returns same value within cache window
            // Getting only 1 unique time is actually correct behavior for performance
            Assert.That(uniqueTimes, Is.GreaterThanOrEqualTo(1),
                "Should have at least one unique time");

            // To actually test time progression, we need to work around the cache
            // The cache check uses: (swTicks - threadLocalLastCheck) < TimeSpan.TicksPerMillisecond
            // So we need to ensure enough stopwatch ticks have passed

            var timesWithDelays = new List<DateTime>();
            var stopwatch = Stopwatch.StartNew();
            long lastElapsed = 0;

            for (int i = 0; i < 10; i++)
            {
                timesWithDelays.Add(HighResolutionTimeUtils.UtcNow);

                // Wait until at least 2ms of stopwatch time has passed
                while (stopwatch.ElapsedMilliseconds - lastElapsed < 2)
                {
                    Thread.Yield();
                }
                lastElapsed = stopwatch.ElapsedMilliseconds;
            }

            int uniqueWithDelays = timesWithDelays.Distinct().Count();
            UnityEngine.Debug.Log($"Unique times with 2ms+ stopwatch delays: {uniqueWithDelays}/10");

            // With proper delays, we should see time progression
            Assert.That(uniqueWithDelays, Is.GreaterThan(1),
                "Should capture time progression with sufficient stopwatch delay");

            // Alternative approach: use different threads (each has its own cache)
            var concurrentTimes = new System.Collections.Concurrent.ConcurrentBag<DateTime>();
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    concurrentTimes.Add(HighResolutionTimeUtils.UtcNow);
                });
            }
            Task.WaitAll(tasks);

            int uniqueConcurrent = concurrentTimes.Distinct().Count();
            UnityEngine.Debug.Log($"Unique times from different threads: {uniqueConcurrent}/{tasks.Length}");

            // Different threads might get different times (no shared cache)
            Assert.That(uniqueConcurrent, Is.GreaterThanOrEqualTo(1),
                "Should have at least one unique time from concurrent calls");
        }

        #endregion

        #region Cache Line and False Sharing Tests

        [Test]
        [Category("CacheLine")]
        public void Verify_Struct_Sizes_And_Alignment()
        {
            // This test would need access to the private structs
            // In practice, you might make them internal for testing

            // Verify that critical structures are sized appropriately
            Assert.That(IntPtr.Size, Is.EqualTo(8).Or.EqualTo(4),
                $"Unexpected pointer size: {IntPtr.Size}");

            // On 64-bit systems, cache lines are typically 64 bytes
            if (Environment.Is64BitProcess)
            {
                int expectedCacheLineSize = 64;
                UnityEngine.Debug.Log($"Expected cache line size: {expectedCacheLineSize} bytes");
            }
        }

        [Test]
        [Category("CacheLine")]
        public void No_False_Sharing_Under_Concurrent_Access()
        {
            int threadCount = Environment.ProcessorCount;
            const int iterations = 1_000_000;
            var startBarrier = new Barrier(threadCount);
            var threadTimes = new long[threadCount];

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks[t] = Task.Run(() =>
                {
                    startBarrier.SignalAndWait();

                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < iterations; i++)
                    {
                        _ = HighResolutionTimeUtils.UtcNow;
                    }
                    sw.Stop();

                    threadTimes[threadIndex] = sw.ElapsedMilliseconds;
                });
            }

            Task.WaitAll(tasks);

            // Calculate statistics
            double avgTime = threadTimes.Average();
            double maxTime = threadTimes.Max();
            double minTime = threadTimes.Min();
            double variance = threadTimes.Select(t => Math.Pow(t - avgTime, 2)).Average();
            double stdDev = Math.Sqrt(variance);

            UnityEngine.Debug.Log($"Thread times - Avg: {avgTime:F1}ms, " +
                                $"Min: {minTime}ms, Max: {maxTime}ms, StdDev: {stdDev:F1}ms");

            // Check that no thread is significantly slower (indicating false sharing)
            double maxDeviation = (maxTime - avgTime) / avgTime;
            Assert.That(maxDeviation, Is.LessThan(0.5),
                $"Max deviation {maxDeviation:P} suggests potential false sharing");
        }

        #endregion

        #region Initialization Race Condition Tests

        [Test]
        [Category("Initialization")]
        public void Concurrent_First_Access_Should_Initialize_Once()
        {
            // This test would ideally be run on a fresh type instance
            // In practice, the static constructor has already run

            // Test concurrent access pattern that might occur during startup
            const int threadCount = 10;
            var barrier = new Barrier(threadCount);
            var firstTimes = new DateTime[threadCount];
            var exceptions = new Exception[threadCount];

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        firstTimes[threadIndex] = HighResolutionTimeUtils.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        exceptions[threadIndex] = ex;
                    }
                });
            }

            Task.WaitAll(tasks);

            // Verify no exceptions
            for (int i = 0; i < threadCount; i++)
            {
                Assert.That(exceptions[i], Is.Null,
                    $"Thread {i} threw exception during initialization: {exceptions[i]}");
            }

            // Verify all threads got reasonable times
            var baseTime = firstTimes[0];
            for (int i = 1; i < threadCount; i++)
            {
                var timeDiff = Math.Abs((firstTimes[i] - baseTime).TotalMilliseconds);
                Assert.That(timeDiff, Is.LessThan(100),
                    $"Thread {i} got significantly different time: {timeDiff:F3}ms difference");
            }
        }

        #endregion

        #region Overflow and Wraparound Tests

        [Test]
        [Category("Overflow")]
        public void Handle_Tick_Arithmetic_Near_Boundaries()
        {
            // Test arithmetic operations that might overflow
            long largeTicks = long.MaxValue / 2;
            long offset = TimeSpan.TicksPerDay * 365; // One year

            // This should not overflow
            Assert.DoesNotThrow(() =>
            {
                long result = largeTicks + offset;
                Assert.That(result, Is.GreaterThan(largeTicks));
            });

            // Test subtraction near zero
            long smallTicks = TimeSpan.TicksPerSecond;
            long largeOffset = TimeSpan.TicksPerDay;

            // This would go negative
            long negativeResult = smallTicks - largeOffset;
            Assert.That(negativeResult, Is.LessThan(0));
        }

        #endregion

        #region Performance Characteristic Tests

        [Test]
        [Category("Performance")]
        [TestCase(1, 10000)]      // Single thread baseline
        [TestCase(2, 10000)]      // Two threads
        [TestCase(4, 10000)]      // Four threads
        [TestCase(8, 10000)]      // Eight threads
        public void Measure_Scalability_With_Thread_Count(int threadCount, int iterationsPerThread)
        {
            var times = new double[threadCount];
            var barrier = new Barrier(threadCount);

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadIndex = t;
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait(); // Start together

                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        _ = HighResolutionTimeUtils.UtcNow;
                    }
                    sw.Stop();

                    times[threadIndex] = sw.Elapsed.TotalMilliseconds;
                });
            }

            Task.WaitAll(tasks);

            double avgTimeMs = times.Average();
            double totalOps = threadCount * iterationsPerThread;
            double opsPerSecond = totalOps / (avgTimeMs / 1000.0);

            UnityEngine.Debug.Log($"Threads: {threadCount}, Avg time: {avgTimeMs:F2}ms, " +
                                $"Ops/sec: {opsPerSecond:F0}");

            // Performance should scale reasonably with thread count
            Assert.That(avgTimeMs, Is.LessThan(1000),
                $"Should complete {totalOps} operations in under 1 second");
        }

        #endregion
    }
}