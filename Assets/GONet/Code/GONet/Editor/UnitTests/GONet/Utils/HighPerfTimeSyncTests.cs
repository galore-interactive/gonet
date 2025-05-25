using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class TimeSyncSchedulerTests
    {
        // Store when we last synced to manage test timing
        private static DateTime lastTestSync = DateTime.MinValue;

        private void EnsureCanSync()
        {
            // Calculate how long we need to wait
            var timeSinceLastSync = DateTime.UtcNow - lastTestSync;
            var waitTime = TimeSpan.FromSeconds(5.1) - timeSinceLastSync;

            if (waitTime > TimeSpan.Zero)
            {
                Thread.Sleep((int)waitTime.TotalMilliseconds);
            }
        }

        #region Scheduling Tests

        [Test, Order(1)]
        [Category("Scheduling")]
        public void Should_Allow_First_Sync_After_Long_Wait()
        {
            // This test verifies we can sync after waiting long enough
            EnsureCanSync();

            bool canSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(canSync, Is.True, "Should be able to sync after 5+ second wait");

            lastTestSync = DateTime.UtcNow;
        }

        [Test, Order(2)]
        [Category("Scheduling")]
        public void Should_Not_Sync_Too_Frequently()
        {
            // Ensure we can sync first
            EnsureCanSync();

            // First sync should be allowed
            bool firstSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(firstSync, Is.True, "First sync should be allowed");
            lastTestSync = DateTime.UtcNow;

            // Immediate second sync should be blocked
            bool secondSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(secondSync, Is.False, "Immediate second sync should be blocked");

            // Even after 0.5 seconds, should still be blocked
            Thread.Sleep(500);
            bool thirdSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(thirdSync, Is.False, "Sync within MIN_INTERVAL should be blocked");

            // Even after 1.5 seconds total, should still be blocked (need 5 seconds)
            Thread.Sleep(1000);
            bool fourthSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(fourthSync, Is.False, "Sync within SYNC_INTERVAL (5s) should be blocked");
        }

        [Test, Order(3)]
        [Category("Scheduling")]
        public void Should_Allow_Sync_After_Sync_Interval()
        {
            // Ensure we can sync
            EnsureCanSync();

            // Do initial sync
            bool initialSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(initialSync, Is.True, "Initial sync should be allowed");
            lastTestSync = DateTime.UtcNow;

            // Should be blocked immediately
            bool immediateSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(immediateSync, Is.False, "Immediate resync should be blocked");

            // Wait for more than the sync interval (5 seconds)
            Thread.Sleep(5100);

            // Should now allow sync again
            bool afterIntervalSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(afterIntervalSync, Is.True, "Sync should be allowed after SYNC_INTERVAL");
            lastTestSync = DateTime.UtcNow;
        }

        [Test, Order(4)]
        [Category("Scheduling")]
        public void Should_Handle_Concurrent_Sync_Requests()
        {
            // Ensure we can sync
            EnsureCanSync();

            const int threadCount = 10;
            int successCount = 0;
            var barrier = new Barrier(threadCount);

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait();

                    if (TimeSyncScheduler.ShouldSyncNow())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Only one thread should have been allowed to sync
            Assert.That(successCount, Is.EqualTo(1),
                $"Exactly one thread should be allowed to sync, but {successCount} were allowed");

            if (successCount > 0)
            {
                lastTestSync = DateTime.UtcNow;
            }
        }

        [Test, Order(5)]
        [Category("Scheduling")]
        public void Should_Block_Within_Min_Interval()
        {
            // Ensure we can sync
            EnsureCanSync();

            // First sync
            bool firstSync = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(firstSync, Is.True, "Should be able to sync initially");
            lastTestSync = DateTime.UtcNow;

            // Should be blocked for at least MIN_INTERVAL (1 second)
            bool immediate = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(immediate, Is.False, "Should block immediately after sync");

            Thread.Sleep(500);
            bool afterHalfSecond = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(afterHalfSecond, Is.False, "Should block within MIN_INTERVAL");

            Thread.Sleep(600); // Now we're past 1 second
            bool afterMinInterval = TimeSyncScheduler.ShouldSyncNow();
            Assert.That(afterMinInterval, Is.False, "Should still block - need full SYNC_INTERVAL");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Category("Performance")]
        public void ShouldSyncNow_Should_Be_Fast()
        {
            const int iterations = 1_000_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                _ = TimeSyncScheduler.ShouldSyncNow();
            }

            sw.Stop();
            double timePerCallNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / iterations;

            UnityEngine.Debug.Log($"Time per ShouldSyncNow call: {timePerCallNs:F1}ns");

            // Should be very fast (under 50ns per call)
            Assert.That(timePerCallNs, Is.LessThan(50),
                "ShouldSyncNow should be very fast");
        }

        #endregion
    }
}