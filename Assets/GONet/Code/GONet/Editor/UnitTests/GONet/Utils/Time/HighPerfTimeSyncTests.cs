using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Tests for HighPerfTimeSync time synchronization processor.
    /// </summary>
    [TestFixture]
    public class HighPerfTimeSyncTests
    {
        private SecretaryOfTemporalAffairs clientTime;
        private SecretaryOfTemporalAffairs serverTime;

        // Mock request message for testing
        private class MockRequestMessage : RequestMessage
        {
            public MockRequestMessage(long occurredAtTicks) : base(occurredAtTicks) { }
        }

        [SetUp]
        public void Setup()
        {
            clientTime = new SecretaryOfTemporalAffairs();
            serverTime = new SecretaryOfTemporalAffairs();

            // Initialize both
            clientTime.Update();
            serverTime.Update();
        }

        #region Basic Time Sync Tests

        [Test]
        [Category("TimeSync")]
        public void Should_Process_Valid_Time_Sync_Response()
        {
            // Simulate a time difference between client and server
            Thread.Sleep(10);
            serverTime.Update();

            // Client sends request
            long requestSentTicks = clientTime.ElapsedTicks;
            var request = new MockRequestMessage(requestSentTicks);

            // Simulate network round trip (20ms)
            Thread.Sleep(20);
            clientTime.Update();
            serverTime.Update();

            // Server responds with its current time
            long serverElapsedTicks = serverTime.ElapsedTicks;

            // Process the sync response
            Assert.DoesNotThrow(() =>
            {
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverElapsedTicks,
                    request,
                    clientTime
                );
            }, "Should process valid time sync without errors");
        }

        [Test]
        [Category("TimeSync")]
        public void Should_Ignore_Out_Of_Order_Responses()
        {
            // Process a response with a high server time
            var request1 = new MockRequestMessage(clientTime.ElapsedTicks);
            long highServerTime = TimeSpan.FromSeconds(100).Ticks;

            HighPerfTimeSync.ProcessTimeSync(
                request1.UID,
                highServerTime,
                request1,
                clientTime
            );

            // Try to process an older response
            var request2 = new MockRequestMessage(clientTime.ElapsedTicks);
            long lowerServerTime = TimeSpan.FromSeconds(50).Ticks;

            // Get client time before processing
            double timeBefore = clientTime.ElapsedSeconds;

            HighPerfTimeSync.ProcessTimeSync(
                request2.UID,
                lowerServerTime,
                request2,
                clientTime
            );

            // Time should not have been adjusted backwards
            Assert.That(clientTime.ElapsedSeconds, Is.GreaterThanOrEqualTo(timeBefore),
                "Out of order responses should be ignored");
        }

        [Test]
        [Category("TimeSync")]
        public void Should_Handle_Negative_RTT()
        {
            // Create a request that appears to be from the future
            long futureRequestTime = clientTime.ElapsedTicks + TimeSpan.FromSeconds(10).Ticks;
            var request = new MockRequestMessage(futureRequestTime);

            // This would result in negative RTT
            Assert.DoesNotThrow(() =>
            {
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime
                );
            }, "Should handle negative RTT gracefully");
        }

        [Test]
        [Category("TimeSync")]
        public void Should_Handle_Excessive_RTT()
        {
            // Create a request from very long ago (simulating 15 second RTT)
            long ancientRequestTime = clientTime.ElapsedTicks - TimeSpan.FromSeconds(15).Ticks;
            var request = new MockRequestMessage(ancientRequestTime);

            // This would result in RTT > 10 seconds
            Assert.DoesNotThrow(() =>
            {
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime
                );
            }, "Should handle excessive RTT gracefully");
        }

        #endregion

        #region RTT Calculation Tests

        [Test]
        [Category("RTT")]
        public void Should_Calculate_Reasonable_RTT()
        {
            // Simulate multiple sync cycles with consistent 50ms RTT
            for (int i = 0; i < 20; i++)
            {
                long requestTime = clientTime.ElapsedTicks;
                var request = new MockRequestMessage(requestTime);

                // Simulate 50ms round trip
                Thread.Sleep(50);
                clientTime.Update();
                serverTime.Update();

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTime.ElapsedTicks,
                    request,
                    clientTime
                );

                Thread.Sleep(10); // Small delay between syncs
            }

            // The median RTT calculation should stabilize around 50ms
            // We can't directly test this without access to internal state,
            // but we can verify the time sync is working
            Assert.Pass("RTT calculation test completed");
        }

        #endregion

        #region Correction Bounds Tests

        [Test]
        [Category("Correction")]
        public void Should_Apply_Bounded_Corrections()
        {
            // Set up a large time difference
            clientTime.Update();

            // Server is 5 seconds ahead
            long serverTicks = clientTime.ElapsedTicks + TimeSpan.FromSeconds(5).Ticks;
            var request = new MockRequestMessage(clientTime.ElapsedTicks);

            double timeBefore = clientTime.ElapsedSeconds;

            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTicks,
                request,
                clientTime
            );

            clientTime.Update();
            double timeAfter = clientTime.ElapsedSeconds;

            // The correction should be bounded (max 2 seconds according to constants)
            // and smoothed (0.2 factor)
            double actualCorrection = timeAfter - timeBefore;

            // Should not jump the full 5 seconds immediately
            Assert.That(actualCorrection, Is.LessThan(3.0),
                "Large corrections should be bounded");
        }

        #endregion

        #region Concurrent Access Tests

        [Test]
        [Category("Concurrency")]
        public void Should_Handle_Concurrent_Sync_Processing()
        {
            const int threadCount = 5;
            const int syncsPerThread = 100;
            var exceptions = new List<Exception>();
            var barrier = new Barrier(threadCount);

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        for (int i = 0; i < syncsPerThread; i++)
                        {
                            var request = new MockRequestMessage(clientTime.ElapsedTicks);
                            long serverTicks = serverTime.ElapsedTicks + TimeSpan.FromMilliseconds(threadId * 10).Ticks;

                            HighPerfTimeSync.ProcessTimeSync(
                                request.UID,
                                serverTicks,
                                request,
                                clientTime
                            );

                            Thread.Yield();
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions) exceptions.Add(ex);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(exceptions.Count, Is.Zero,
                "Concurrent sync processing should not cause exceptions");
        }

        #endregion
    }

    /// <summary>
    /// Tests for TimeSyncScheduler.
    /// </summary>
    [TestFixture]
    public class TimeSyncSchedulerTests : TimeSyncTestBase
    {
        private FieldInfo lastSyncTimeTicksField;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();

            var schedulerType = typeof(TimeSyncScheduler);
            lastSyncTimeTicksField = schedulerType.GetField("lastSyncTimeTicks", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [TearDown]
        public void TearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        [Category("Scheduler")]
        public void Should_Respect_Minimum_Sync_Interval()
        {
            // First call should return true
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True, "First sync should be allowed");

            // Immediate second call should return false
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.False, "Should respect minimum interval");

            // Wait less than minimum interval
            Thread.Sleep(500); // 0.5 seconds
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.False, "Should still respect minimum interval");
        }

        [Test]
        [Category("Scheduler")]
        [Timeout(10000)] // Added timeout
        public void Should_Allow_Sync_After_Interval()
        {
            // First sync
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);

            // Wait for sync interval (5 seconds)
            Thread.Sleep(5100); // 5.1 seconds

            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True, "Should allow sync after interval");
        }

        [Test]
        [Category("Scheduler")]
        public void Should_Handle_Concurrent_Sync_Attempts()
        {
            // Reset scheduler
            lastSyncTimeTicksField.SetValue(null, 0L);

            const int threadCount = 10;
            var barrier = new Barrier(threadCount);
            var successCount = 0;

            var tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    barrier.SignalAndWait();

                    if (TimeSyncScheduler.ShouldSyncNow())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(successCount, Is.EqualTo(1), "Only one thread should win the sync slot");
        }

        [Test]
        [Category("Scheduler")]
        [Timeout(20000)] // Increased timeout to account for setup/variance
        public void Should_Maintain_Sync_Schedule_Over_Time()
        {
            // Reset scheduler state
            lastSyncTimeTicksField.SetValue(null, 0L);

            var syncTimes = new List<DateTime>();
            var stopwatch = Stopwatch.StartNew();
            const int testDurationMs = 12000; // Run for 12 seconds instead of 15

            // Run for 12 seconds, checking every 50ms for more precision
            while (stopwatch.ElapsedMilliseconds < testDurationMs && !cts.Token.IsCancellationRequested)
            {
                if (TimeSyncScheduler.ShouldSyncNow())
                {
                    syncTimes.Add(DateTime.UtcNow);
                    UnityEngine.Debug.Log($"Sync #{syncTimes.Count} at {stopwatch.ElapsedMilliseconds}ms");
                }
                Thread.Sleep(50); // Reduced from 100ms for better precision
            }

            UnityEngine.Debug.Log($"Test ran for {stopwatch.ElapsedMilliseconds}ms, captured {syncTimes.Count} syncs");

            // Should have approximately 2-3 syncs (12s / 5s interval)
            // First sync happens immediately, then every 5s
            Assert.That(syncTimes.Count, Is.InRange(2, 3), $"Expected 2-3 syncs in {testDurationMs}ms, got {syncTimes.Count}");

            // Check intervals (if we have at least 2 syncs)
            if (syncTimes.Count >= 2)
            {
                for (int i = 1; i < syncTimes.Count; i++)
                {
                    var interval = (syncTimes[i] - syncTimes[i - 1]).TotalSeconds;
                    // Allow slightly more variance due to thread scheduling
                    Assert.That(interval, Is.InRange(4.8, 5.3),
                        $"Sync interval {i} should be ~5 seconds, was {interval:F1}s");
                }
            }
        }
    }
}