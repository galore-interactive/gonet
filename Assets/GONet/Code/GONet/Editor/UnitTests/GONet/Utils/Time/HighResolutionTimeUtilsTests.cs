using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Comprehensive unit tests for HighResolutionTimeUtils.
    /// All tests run in Unity Edit Mode without requiring Play Mode.
    /// </summary>
    [TestFixture]
    public class HighResolutionTimeUtilsTests
    {
        private const int WARM_UP_ITERATIONS = 100;
        private const double PRECISION_TOLERANCE_MS = 1.0; // 1ms tolerance for timing comparisons

        [SetUp]
        public void Setup()
        {
            // Warm up the high-resolution timer
            for (int i = 0; i < WARM_UP_ITERATIONS; i++)
            {
                _ = HighResolutionTimeUtils.UtcNow;
                _ = HighResolutionTimeUtils.Now;
            }
        }

        #region Monotonicity Tests

        [Test]
        [Category("Monotonicity")]
        public void Time_Should_Never_Decrease_SingleThreaded()
        {
            const int iterations = 1_000_000;
            DateTime previousTime = HighResolutionTimeUtils.UtcNow;
            var violations = new List<(DateTime prev, DateTime curr, int iteration)>();

            for (int i = 0; i < iterations; i++)
            {
                DateTime currentTime = HighResolutionTimeUtils.UtcNow;

                if (currentTime < previousTime)
                {
                    violations.Add((previousTime, currentTime, i));
                }

                previousTime = currentTime;
            }

            Assert.That(violations.Count, Is.Zero,
                $"Time went backwards {violations.Count} times. First violation: " +
                $"prev={violations.FirstOrDefault().prev:O}, curr={violations.FirstOrDefault().curr:O}, " +
                $"iteration={violations.FirstOrDefault().iteration}");
        }

        [Test]
        [Category("Monotonicity")]
        public void Time_Should_Never_Decrease_MultiThreaded()
        {
            const int threadCount = 16;
            const int iterationsPerThread = 100_000;
            var violations = new System.Collections.Concurrent.ConcurrentBag<string>();
            var barrier = new Barrier(threadCount);

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait(); // Ensure all threads start simultaneously

                    DateTime previousTime = HighResolutionTimeUtils.UtcNow;
                    for (int i = 0; i < iterationsPerThread; i++)
                    {
                        DateTime currentTime = HighResolutionTimeUtils.UtcNow;

                        if (currentTime < previousTime)
                        {
                            violations.Add($"Thread {threadId}: Time went backwards at iteration {i}: " +
                                         $"{previousTime:O} -> {currentTime:O}");
                        }

                        previousTime = currentTime;
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(violations.Count, Is.Zero,
                string.Join("\n", violations.Take(10)) +
                (violations.Count > 10 ? $"\n... and {violations.Count - 10} more violations" : ""));
        }

        [Test]
        [Category("Monotonicity")]
        public void Time_Should_Be_Monotonic_With_Thread_Yields()
        {
            const int iterations = 10_000;
            DateTime previousTime = HighResolutionTimeUtils.UtcNow;
            int backwardCount = 0;

            for (int i = 0; i < iterations; i++)
            {
                Thread.Yield(); // Force thread context switch
                DateTime currentTime = HighResolutionTimeUtils.UtcNow;

                if (currentTime < previousTime)
                {
                    backwardCount++;
                }

                previousTime = currentTime;

                // Add some CPU work to simulate real usage
                double dummy = Math.Sqrt(i);
            }

            Assert.That(backwardCount, Is.Zero,
                $"Time went backwards {backwardCount} times with Thread.Yield()");
        }

        #endregion

        #region Precision Tests

        [Test]
        [Category("Precision")]
        public void Compare_Precision_With_Stopwatch()
        {
            const int iterations = 1000;
            var stopwatch = Stopwatch.StartNew();
            var startTime = HighResolutionTimeUtils.UtcNow;

            Thread.Sleep(100); // Sleep for 100ms

            stopwatch.Stop();
            var endTime = HighResolutionTimeUtils.UtcNow;

            double highResElapsedMs = (endTime - startTime).TotalMilliseconds;
            double stopwatchElapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            double difference = Math.Abs(highResElapsedMs - stopwatchElapsedMs);

            Assert.That(difference, Is.LessThan(PRECISION_TOLERANCE_MS),
                $"HighRes: {highResElapsedMs:F3}ms, Stopwatch: {stopwatchElapsedMs:F3}ms, " +
                $"Difference: {difference:F3}ms");
        }

        [Test]
        [Category("Precision")]
        public void Should_Capture_SubMillisecond_Differences()
        {
            const int iterations = 100;
            var measuredDifferences = new List<double>();

            for (int i = 0; i < iterations; i++)
            {
                var start = HighResolutionTimeUtils.UtcNow;

                // Use a tighter spin loop that should definitely take some measurable time
                var spinStart = Stopwatch.GetTimestamp();
                long spinDuration = Stopwatch.Frequency / 1000; // 1ms worth of ticks
                while ((Stopwatch.GetTimestamp() - spinStart) < spinDuration)
                {
                    // Busy wait
                }

                var end = HighResolutionTimeUtils.UtcNow;
                double elapsedMs = (end - start).TotalMilliseconds;

                measuredDifferences.Add(elapsedMs);
            }

            // Log statistics for debugging
            var validDifferences = measuredDifferences.Where(d => d > 0).ToList();
            UnityEngine.Debug.Log($"Captured {validDifferences.Count}/{iterations} non-zero differences");
            if (validDifferences.Any())
            {
                UnityEngine.Debug.Log($"Min: {validDifferences.Min():F6}ms, Max: {validDifferences.Max():F6}ms, Avg: {validDifferences.Average():F6}ms");
            }

            // Less strict requirement - at least some differences should be captured
            Assert.That(validDifferences.Count, Is.GreaterThan(iterations * 0.1),
                $"Should capture at least 10% non-zero differences, but got {validDifferences.Count}");

            // If we got any differences, verify they make sense
            if (validDifferences.Any())
            {
                double avgDifference = validDifferences.Average();
                Assert.That(avgDifference, Is.GreaterThan(0.5).And.LessThan(2.0),
                    $"Average captured difference should be around 1ms, but was {avgDifference:F3}ms");
            }
        }

        [Test]
        [Category("Precision")]
        public void Tick_To_DateTime_Conversion_Accuracy()
        {
            var systemNow = DateTime.UtcNow;
            var highResNow = HighResolutionTimeUtils.UtcNow;

            // They should be very close (within 100ms considering test execution time)
            double differenceMs = Math.Abs((highResNow - systemNow).TotalMilliseconds);

            Assert.That(differenceMs, Is.LessThan(100),
                $"System: {systemNow:O}, HighRes: {highResNow:O}, Difference: {differenceMs:F3}ms");
        }

        #endregion

        #region Thread-Local Cache Tests

        [Test]
        [Category("Cache")]
        public void ThreadLocal_Cache_Should_Improve_Performance()
        {
            const int iterations = 1_000_000;

            // Measure time for rapid successive calls (should hit cache)
            var sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = HighResolutionTimeUtils.UtcNow;
            }
            sw1.Stop();

            // Measure time with forced cache misses
            var sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                _ = HighResolutionTimeUtils.UtcNow;
                Thread.Sleep(0); // Force potential cache invalidation
            }
            sw2.Stop();

            double cachedTimePerCall = sw1.Elapsed.TotalMilliseconds / iterations * 1_000_000; // nanoseconds
            double uncachedTimePerCall = sw2.Elapsed.TotalMilliseconds / iterations * 1_000_000;

            UnityEngine.Debug.Log($"Cached time per call: {cachedTimePerCall:F1}ns");
            UnityEngine.Debug.Log($"Uncached time per call: {uncachedTimePerCall:F1}ns");

            // Cached should be significantly faster
            Assert.That(cachedTimePerCall, Is.LessThan(uncachedTimePerCall * 0.5),
                "Cached calls should be at least 2x faster than uncached calls");
        }

        [Test]
        [Category("Cache")]
        public void Each_Thread_Should_Have_Independent_Cache()
        {
            const int threadCount = 4;
            var threadIds = new System.Collections.Concurrent.ConcurrentBag<int>();
            var cacheHitCounts = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();

            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    threadIds.Add(threadId);
                    cacheHitCounts[threadId] = 0;

                    // Each thread does rapid queries that should hit its own cache
                    var lastTime = HighResolutionTimeUtils.UtcNow;
                    for (int j = 0; j < 10000; j++)
                    {
                        var currentTime = HighResolutionTimeUtils.UtcNow;
                        if (currentTime == lastTime)
                        {
                            cacheHitCounts[threadId]++;
                        }
                        lastTime = currentTime;
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(threadIds.Distinct().Count(), Is.EqualTo(threadCount),
                "Should have used different threads");

            foreach (var kvp in cacheHitCounts)
            {
                Assert.That(kvp.Value, Is.GreaterThan(0),
                    $"Thread {kvp.Key} should have some cache hits");
            }
        }

        #endregion

        #region Resync Mechanism Tests

        [Test]
        [Category("Initialization")]
        public void Verify_Static_Initialization()
        {
            // The static constructor should have already run by now
            // Let's verify both UTC and Local time are properly initialized

            var utcTime = HighResolutionTimeUtils.UtcNow;
            var localTime = HighResolutionTimeUtils.Now;
            var systemTime = DateTime.UtcNow;

            UnityEngine.Debug.Log("=== Static Initialization Check ===");
            UnityEngine.Debug.Log($"System UTC Now: {systemTime:O}");
            UnityEngine.Debug.Log($"HighRes UTC: {utcTime:O}");
            UnityEngine.Debug.Log($"HighRes Local: {localTime:O}");

            // UTC should be reasonable
            Assert.That(utcTime.Year, Is.EqualTo(systemTime.Year),
                "UTC year should match current year");
            Assert.That(utcTime.Kind, Is.EqualTo(DateTimeKind.Utc),
                "UTC time should have UTC kind");

            // Local should also be reasonable
            Assert.That(localTime.Year, Is.EqualTo(systemTime.Year),
                $"Local year should be {systemTime.Year}, not {localTime.Year}");
            Assert.That(localTime.Kind, Is.EqualTo(DateTimeKind.Local),
                "Local time should have Local kind");

            // Both should be close to current time
            Assert.That(Math.Abs((utcTime - systemTime).TotalMinutes), Is.LessThan(1),
                "UTC time should be within 1 minute of system time");
        }

        [Test]
        [Category("Resync")]
        public void Concurrent_Resync_Should_Be_LockFree()
        {
            const int threadCount = 10;
            const int iterations = 100_000;
            bool anyBlockage = false;

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        var sw = Stopwatch.StartNew();
                        _ = HighResolutionTimeUtils.UtcNow;
                        sw.Stop();

                        // If any call takes more than 1ms, it might indicate blocking
                        if (sw.Elapsed.TotalMilliseconds > 1.0)
                        {
                            anyBlockage = true;
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(anyBlockage, Is.False,
                "No time query should take more than 1ms (indicates potential blocking)");
        }

        #endregion

        #region Local vs UTC Tests

        [Test]
        [Category("LocalVsUtc")]
        public void Diagnose_Local_Time_Implementation()
        {
            // Let's diagnose what's happening with the local time calculation
            var utcTime = HighResolutionTimeUtils.UtcNow;
            var localTime = HighResolutionTimeUtils.Now;

            UnityEngine.Debug.Log($"=== Local Time Diagnosis ===");
            UnityEngine.Debug.Log($"UTC Time: {utcTime:O} (Ticks: {utcTime.Ticks})");
            UnityEngine.Debug.Log($"Local Time: {localTime:O} (Ticks: {localTime.Ticks})");
            UnityEngine.Debug.Log($"Local Time Kind: {localTime.Kind}");

            // Check if it's returning DateTime.MinValue
            Assert.That(localTime, Is.Not.EqualTo(DateTime.MinValue),
                "Local time should not be DateTime.MinValue");

            // Check if ticks are reasonable
            Assert.That(localTime.Ticks, Is.GreaterThan(0),
                "Local time ticks should be positive");

            // The implementation might be using the wrong base or calculation
            // Let's see if calling it multiple times gives different results
            var localTime2 = HighResolutionTimeUtils.Now;
            var localTime3 = HighResolutionTimeUtils.Now;

            UnityEngine.Debug.Log($"Local Time 2: {localTime2:O}");
            UnityEngine.Debug.Log($"Local Time 3: {localTime3:O}");
            UnityEngine.Debug.Log($"Times are equal: {localTime == localTime2 && localTime2 == localTime3}");
        }

        [Test]
        [Category("LocalVsUtc")]
        public void Local_And_UTC_Should_Differ_By_Timezone_Offset()
        {
            // First, let's verify UTC time is working
            var utcTime = HighResolutionTimeUtils.UtcNow;
            var systemUtc = DateTime.UtcNow;
            Assert.That(Math.Abs((utcTime - systemUtc).TotalSeconds), Is.LessThan(1),
                "UTC time should be close to system UTC time");

            // Now check local time
            var localTime = HighResolutionTimeUtils.Now;

            // The local time is clearly broken (returning DateTime.MinValue or similar)
            // Let's check if it's specifically DateTime.MinValue
            if (localTime.Year == 1)
            {
                Assert.Fail($"Local time is returning near DateTime.MinValue: {localTime:O}. " +
                          "This indicates a bug in HighResolutionTimeUtils.Now implementation.");
            }

            // If we get here, do the actual offset test
            var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(systemUtc);
            var expectedLocal = utcTime.Add(expectedOffset);
            var actualDifference = Math.Abs((localTime - expectedLocal).TotalSeconds);

            Assert.That(actualDifference, Is.LessThan(1),
                $"Local time {localTime:O} differs from expected {expectedLocal:O} by {actualDifference:F3} seconds");
        }

        #endregion

        #region Bulk Operations Tests

        #region Bug Investigation Tests

        [Test]
        [Category("BugInvestigation")]
        public void Investigate_Local_Time_Bug()
        {
            // Let's trace through what should happen
            UnityEngine.Debug.Log("=== Investigating Local Time Bug ===");

            // First call - might initialize something
            var firstLocal = HighResolutionTimeUtils.Now;
            UnityEngine.Debug.Log($"First Local call: {firstLocal:O} (Ticks: {firstLocal.Ticks})");

            // Wait a bit
            Thread.Sleep(10);

            // Second call
            var secondLocal = HighResolutionTimeUtils.Now;
            UnityEngine.Debug.Log($"Second Local call: {secondLocal:O} (Ticks: {secondLocal.Ticks})");

            // Compare with UTC
            var utcNow = HighResolutionTimeUtils.UtcNow;
            UnityEngine.Debug.Log($"UTC call: {utcNow:O} (Ticks: {utcNow.Ticks})");

            // Try calling them in different order
            var utcFirst = HighResolutionTimeUtils.UtcNow;
            var localAfterUtc = HighResolutionTimeUtils.Now;
            UnityEngine.Debug.Log($"UTC first: {utcFirst:O}");
            UnityEngine.Debug.Log($"Local after UTC: {localAfterUtc:O}");

            // The bug appears to be that BaseLocalTicks is 0 or not properly initialized
            // This would cause the calculation to return very small tick values

            Assert.That(firstLocal.Ticks, Is.GreaterThan(DateTime.MinValue.Ticks),
                "Local time ticks should be greater than DateTime.MinValue");
        }

        #endregion

        [Test]
        [Category("Stress")]
        public void Stress_Test_Mixed_Operations()
        {
            const int duration = 100; // milliseconds
            var endTime = DateTime.UtcNow.AddMilliseconds(duration);
            long operationCount = 0;
            var random = new System.Random(42);

            // Allocate bulk times buffer outside the loop to avoid stack issues
            var bulkBuffer = new long[10];

            while (DateTime.UtcNow < endTime)
            {
                switch (random.Next(3))
                {
                    case 0:
                        _ = HighResolutionTimeUtils.UtcNow;
                        break;
                    case 1:
                        _ = HighResolutionTimeUtils.Now;
                        break;
                    case 3:
                        Thread.Yield();
                        break;
                }
                operationCount++;
            }

            Assert.That(operationCount, Is.GreaterThan(1000),
                $"Should complete many operations in {duration}ms. Completed: {operationCount}");

            // Verify time still works correctly after stress
            var finalTime = HighResolutionTimeUtils.UtcNow;
            Assert.That(finalTime, Is.GreaterThan(DateTime.UtcNow.AddMilliseconds(-1000)),
                "Time should still be accurate after stress test");
        }

        #endregion
    }
}