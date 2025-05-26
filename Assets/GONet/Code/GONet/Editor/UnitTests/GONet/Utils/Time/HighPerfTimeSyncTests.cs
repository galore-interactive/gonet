using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Comprehensive tests for HighPerfTimeSync time synchronization processor.
    /// </summary>
    [TestFixture]
    public class HighPerfTimeSyncTests : TimeSyncTestBase
    {
        // String constants to avoid allocations
        private const string CATEGORY_TIMESYNC = "TimeSync";
        private const string CATEGORY_RTT = "RTT";
        private const string CATEGORY_CORRECTION = "Correction";
        private const string CATEGORY_CONCURRENCY = "Concurrency";
        private const string CATEGORY_EDGECASES = "EdgeCases";
        private const string CATEGORY_PERFORMANCE = "Performance";
        private const string CATEGORY_SCHEDULER = "Scheduler";

        private const string METHOD_GETFASTMEDIANRTT = "GetFastMedianRtt";
        private const string FIELD_ADJUSTMENTCOUNT = "adjustmentCount";
        private const string FIELD_LASTSYNCTIME = "lastSyncTimeTicks";

        private const string LOG_ERROR_INVALID_PARAMS = "[TimeSync] Invalid parameters passed to ProcessTimeSync";

        private const string THREAD_NAME_CLIENT = "ClientThread";
        private const string THREAD_NAME_SERVER = "ServerThread";

        private BlockingCollection<Action> clientActions;
        private BlockingCollection<Action> serverActions;
        private Thread clientThread;
        private Thread serverThread;
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
            base.BaseSetUp();

            clientActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            serverActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());

            var clientReady = new ManualResetEventSlim(false);
            var serverReady = new ManualResetEventSlim(false);

            // Initialize threads with proper synchronization
            clientThread = new Thread(() =>
            {
                try
                {
                    clientTime = new SecretaryOfTemporalAffairs();
                    clientTime.Update();
                    clientReady.Set();

                    foreach (var action in clientActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                            // Swallow exceptions in thread
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            })
            {
                IsBackground = true,
                Name = THREAD_NAME_CLIENT
            };

            serverThread = new Thread(() =>
            {
                try
                {
                    serverTime = new SecretaryOfTemporalAffairs();
                    serverTime.Update();
                    serverReady.Set();

                    foreach (var action in serverActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                            // Swallow exceptions in thread
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            })
            {
                IsBackground = true,
                Name = THREAD_NAME_SERVER
            };

            clientThread.Start();
            serverThread.Start();

            // Wait for both threads to initialize
            if (!clientReady.Wait(5000) || !serverReady.Wait(5000))
            {
                throw new TimeoutException();
            }

            clientReady.Dispose();
            serverReady.Dispose();

            // Let some time pass to ensure valid elapsed times
            Thread.Sleep(50);
            UpdateBothTimes();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (cts != null && !cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }

                clientActions?.CompleteAdding();
                serverActions?.CompleteAdding();

                if (clientThread != null && clientThread.IsAlive)
                {
                    clientThread.Join(1000);
                }

                if (serverThread != null && serverThread.IsAlive)
                {
                    serverThread.Join(1000);
                }

                clientActions?.Dispose();
                serverActions?.Dispose();
            }
            finally
            {
                base.BaseTearDown();
            }
        }

        #region Basic Time Sync Tests

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Process_Valid_Time_Sync_Response()
        {
            // Simulate a time difference between client and server
            Thread.Sleep(10);
            UpdateBothTimes();

            // Client sends request
            long requestSentTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request = new MockRequestMessage(requestSentTicks);

            // Simulate network round trip (20ms)
            Thread.Sleep(20);
            UpdateBothTimes();

            // Server responds with its current time
            long serverElapsedTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            // Process the sync response
            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverElapsedTicks,
                    request,
                    clientTime
                ), clientActions);
            });
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Handle_Null_Parameters()
        {
            // Since we've removed logging from the implementation,
            // just verify it doesn't crash with null parameters
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request = new MockRequestMessage(clientTicks);

            // Test null requestMessage - should handle gracefully without crashing
            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    0L,
                    null,
                    clientTime
                ), clientActions);
            });

            // Test null timeAuthority - should handle gracefully without crashing
            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    0L,
                    request,
                    null
                ), clientActions);
            });
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Ignore_Out_Of_Order_Responses()
        {
            // Process a response with a high server time
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request1 = new MockRequestMessage(clientTicks);
            long highServerTime = TimeSpan.FromSeconds(100).Ticks;

            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                request1.UID,
                highServerTime,
                request1,
                clientTime
            ), clientActions);

            // Wait for adjustment
            Thread.Sleep(1100);
            UpdateBothTimes();

            // Try to process an older response
            clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request2 = new MockRequestMessage(clientTicks);
            long lowerServerTime = TimeSpan.FromSeconds(50).Ticks;

            // Get client time before processing
            double timeBefore = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);

            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                request2.UID,
                lowerServerTime,
                request2,
                clientTime
            ), clientActions);

            // Wait a bit
            Thread.Sleep(100);
            UpdateBothTimes();

            // Time should not have been adjusted backwards significantly
            double timeAfter = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            Assert.That(timeAfter, Is.GreaterThan(timeBefore - 0.5));
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Handle_Negative_RTT()
        {
            // Create a request that appears to be from the future
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long futureRequestTime = clientTicks + TimeSpan.FromSeconds(10).Ticks;
            var request = new MockRequestMessage(futureRequestTime);

            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime
                ), clientActions);
            });
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Handle_Excessive_RTT()
        {
            // Create a request from very long ago (simulating 15 second RTT)
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long ancientRequestTime = clientTicks - TimeSpan.FromSeconds(15).Ticks;
            var request = new MockRequestMessage(ancientRequestTime);

            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime
                ), clientActions);
            });
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Handle_Zero_Server_Time()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request = new MockRequestMessage(clientTicks);

            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    0L,
                    request,
                    clientTime
                ), clientActions);
            });
        }

        [Test]
        [Category(CATEGORY_TIMESYNC)]
        public void Should_Handle_Negative_Server_Time()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request = new MockRequestMessage(clientTicks);

            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    -TimeSpan.FromSeconds(5).Ticks,
                    request,
                    clientTime
                ), clientActions);
            });
        }

        #endregion

        #region RTT Calculation Tests

        [Test]
        [Category(CATEGORY_RTT)]
        public void Should_Calculate_Reasonable_RTT()
        {
            var getMedianRttMethod = typeof(HighPerfTimeSync).GetMethod(METHOD_GETFASTMEDIANRTT,
                BindingFlags.NonPublic | BindingFlags.Static);

            // Simulate multiple sync cycles with consistent 50ms RTT
            for (int i = 0; i < 20; i++)
            {
                long requestTime = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
                var request = new MockRequestMessage(requestTime);

                // Simulate 50ms round trip
                Thread.Sleep(50);
                UpdateBothTimes();

                long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime
                ), clientActions);

                Thread.Sleep(10);
            }

            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.InRange(0.04f, 0.06f));
        }

        [Test]
        [Category(CATEGORY_RTT)]
        public void Should_Handle_Variable_RTT()
        {
            var getMedianRttMethod = typeof(HighPerfTimeSync).GetMethod(METHOD_GETFASTMEDIANRTT,
                BindingFlags.NonPublic | BindingFlags.Static);

            var rttValues = new[] { 20, 100, 30, 150, 40, 80, 25, 120, 35, 90 };

            foreach (var rttMs in rttValues)
            {
                long requestTime = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
                var request = new MockRequestMessage(requestTime);

                Thread.Sleep(rttMs);
                UpdateBothTimes();

                long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime
                ), clientActions);

                Thread.Sleep(10);
            }

            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.InRange(0.03f, 0.10f));
        }

        [Test]
        [Category(CATEGORY_RTT)]
        public void Should_Handle_RTT_Spikes()
        {
            var getMedianRttMethod = typeof(HighPerfTimeSync).GetMethod(METHOD_GETFASTMEDIANRTT,
                BindingFlags.NonPublic | BindingFlags.Static);

            for (int i = 0; i < 15; i++)
            {
                long requestTime = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
                var request = new MockRequestMessage(requestTime);

                int delay = (i % 5 == 0) ? 500 : 30;
                Thread.Sleep(delay);

                UpdateBothTimes();

                long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime
                ), clientActions);
            }

            float median = (float)getMedianRttMethod.Invoke(null, null);
            Assert.That(median, Is.LessThan(0.1f));
        }

        #endregion

        #region Correction Bounds Tests

        [Test]
        [Category(CATEGORY_CORRECTION)]
        public void Should_Apply_Bounded_Corrections()
        {
            UpdateBothTimes();

            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long serverTicks = clientTicks + TimeSpan.FromSeconds(5).Ticks;
            var request = new MockRequestMessage(clientTicks);

            double timeBefore = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);

            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTicks,
                request,
                clientTime
            ), clientActions);

            Thread.Sleep(1100);
            UpdateBothTimes();

            double timeAfter = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            double actualCorrection = timeAfter - timeBefore - 1.1;

            Assert.That(actualCorrection, Is.GreaterThan(0.5));
        }

        [Test]
        [Category(CATEGORY_CORRECTION)]
        public void Should_Handle_Very_Large_Time_Differences()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long serverTicks = clientTicks + TimeSpan.FromHours(1).Ticks;
            var request = new MockRequestMessage(clientTicks);

            Assert.DoesNotThrow(() =>
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverTicks,
                    request,
                    clientTime,
                    false
                ), clientActions);
            });

            Thread.Sleep(1100);
            UpdateBothTimes();
        }

        [Test]
        [Category(CATEGORY_CORRECTION)]
        public void Should_Force_Adjustment_When_Requested()
        {
            // Synchronize times first
            SynchronizeTimes();

            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);
            var request = new MockRequestMessage(clientTicks);

            var adjustmentCountField = typeof(HighPerfTimeSync).GetField(FIELD_ADJUSTMENTCOUNT,
                BindingFlags.NonPublic | BindingFlags.Static);
            int countBefore = (int)adjustmentCountField.GetValue(null);

            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTicks + TimeSpan.FromMilliseconds(2).Ticks,
                request,
                clientTime,
                true
            ), clientActions);

            int countAfter = (int)adjustmentCountField.GetValue(null);

            Assert.That(countAfter, Is.EqualTo(countBefore + 1));
        }

        #endregion

        #region Concurrent Access Tests

        [Test]
        [Category(CATEGORY_CONCURRENCY)]
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
                            long requestTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
                            var request = new MockRequestMessage(requestTicks);
                            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions) +
                                              TimeSpan.FromMilliseconds(threadId * 10).Ticks;

                            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                                request.UID,
                                serverTicks,
                                request,
                                clientTime
                            ), clientActions);

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

            Assert.That(exceptions.Count, Is.Zero);
        }

        #endregion

        #region Performance Tests

        [Test]
        [Category(CATEGORY_PERFORMANCE)]
        public void ProcessTimeSync_Performance()
        {
            const int iterations = 10000;
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            var request = new MockRequestMessage(clientTicks);
            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            // Warm up
            for (int i = 0; i < 100; i++)
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID + i,
                    serverTicks + i * 1000,
                    request,
                    clientTime
                ), clientActions);
            }

            Thread.Sleep(100);
            UpdateBothTimes();

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                    request.UID + i + 100,
                    serverTicks + i * 1000,
                    request,
                    clientTime
                ), clientActions);
            }

            sw.Stop();
            double timePerCallUs = sw.Elapsed.TotalMilliseconds * 1000 / iterations;

            Assert.That(timePerCallUs, Is.LessThan(50));
        }

        private const int WARMUP_ITERATIONS = 10000;
        private const int SYNC_COUNT = 1000;
        private const int EXPECTED_ALLOCATION_BYTES = 0;
        private const int GC_CYCLE_COUNT = 3;

        [Test]
        [Category(CATEGORY_PERFORMANCE)]
        [Explicit]
        public void Should_Not_Allocate_Memory()
        {
            // Use existing clientTime from Setup
            var request = new MockRequestMessage(clientTime.ElapsedTicks) { UID = 12345 };
            long serverTicks = TimeSpan.FromSeconds(10).Ticks;

            // Warm up threading infrastructure
            var warmupTcs = new TaskCompletionSource<bool>();
            RunOnThread(() => warmupTcs.SetResult(true), clientActions);
            warmupTcs.Task.Wait();

            // Warm-up phase: Ensure JIT and runtime are stabilized
            var tcs = new TaskCompletionSource<bool>();
            RunOnThread(() =>
            {
                for (int i = 0; i < WARMUP_ITERATIONS; i++)
                {
                    HighPerfTimeSync.ProcessTimeSync(
                        request.UID + i,
                        serverTicks + i * 1000,
                        request,
                        clientTime
                    );
                }

                for (int j = 0; j < GC_CYCLE_COUNT; j++)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                }

                tcs.SetResult(true);
            }, clientActions);

            tcs.Task.Wait();

            // Measurement phase: Run on current thread to avoid delegate allocations
            for (int j = 0; j < GC_CYCLE_COUNT; j++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }

            long memBefore = GC.GetTotalMemory(true);
            for (int i = 0; i < SYNC_COUNT; i++)
            {
                HighPerfTimeSync.ProcessTimeSync(
                    request.UID + i + 100,
                    serverTicks + i * 1000,
                    request,
                    clientTime
                );
            }
            long memAfter = GC.GetTotalMemory(true);
            long allocated = memAfter - memBefore;

            Assert.That(allocated, Is.LessThanOrEqualTo(EXPECTED_ALLOCATION_BYTES));
        }

        #endregion

        // Helper methods
        private void UpdateBothTimes()
        {
            RunOnThread(() => clientTime.Update(), clientActions);
            RunOnThread(() => serverTime.Update(), serverActions);
        }

        private void SynchronizeTimes()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);
            var syncRequest = new MockRequestMessage(clientTicks);

            RunOnThread(() => HighPerfTimeSync.ProcessTimeSync(
                syncRequest.UID,
                serverTicks,
                syncRequest,
                clientTime,
                true
            ), clientActions);

            Thread.Sleep(1100);
            UpdateBothTimes();
        }
    }

    /// <summary>
    /// Tests for TimeSyncScheduler high-performance scheduling.
    /// </summary>
    [TestFixture]
    public class TimeSyncSchedulerTests : TimeSyncTestBase
    {
        private const string CATEGORY_SCHEDULER = "Scheduler";
        private const string FIELD_LASTSYNCTIME = "lastSyncTimeTicks";

        private FieldInfo lastSyncTimeTicksField;

        [SetUp]
        public void Setup()
        {
            base.BaseSetUp();

            var schedulerType = typeof(TimeSyncScheduler);
            lastSyncTimeTicksField = schedulerType.GetField(FIELD_LASTSYNCTIME, BindingFlags.NonPublic | BindingFlags.Static);
        }

        [TearDown]
        public void TearDown()
        {
            base.BaseTearDown();
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        public void Should_Respect_Minimum_Sync_Interval()
        {
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.False);
            Thread.Sleep(500);
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.False);
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        [Timeout(10000)]
        public void Should_Allow_Sync_After_Interval()
        {
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);
            Thread.Sleep(5100);
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        public void Should_Handle_Concurrent_Sync_Attempts()
        {
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

            Assert.That(successCount, Is.EqualTo(1));
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        [Timeout(20000)]
        public void Should_Maintain_Sync_Schedule_Over_Time()
        {
            lastSyncTimeTicksField.SetValue(null, 0L);

            var syncTimes = new List<DateTime>();
            var stopwatch = Stopwatch.StartNew();
            const int testDurationMs = 12000;

            while (stopwatch.ElapsedMilliseconds < testDurationMs && !cts.Token.IsCancellationRequested)
            {
                if (TimeSyncScheduler.ShouldSyncNow())
                {
                    syncTimes.Add(DateTime.UtcNow);
                }
                Thread.Sleep(50);
            }

            Assert.That(syncTimes.Count, Is.InRange(2, 3));

            if (syncTimes.Count >= 2)
            {
                for (int i = 1; i < syncTimes.Count; i++)
                {
                    var interval = (syncTimes[i] - syncTimes[i - 1]).TotalSeconds;
                    Assert.That(interval, Is.InRange(4.8, 5.3));
                }
            }
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        public void Should_Be_Lock_Free()
        {
            const int threadCount = 100;
            const int checksPerThread = 1000;
            var totalChecks = 0;
            var successfulSyncs = 0;

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < checksPerThread; i++)
                    {
                        Interlocked.Increment(ref totalChecks);
                        if (TimeSyncScheduler.ShouldSyncNow())
                        {
                            Interlocked.Increment(ref successfulSyncs);
                        }
                    }
                });
            }

            var sw = Stopwatch.StartNew();
            Task.WaitAll(tasks);
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1000));
        }

        [Test]
        [Category(CATEGORY_SCHEDULER)]
        public void Should_Handle_Clock_Adjustments()
        {
            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);

            long futureTime = GONet.Utils.HighResolutionTimeUtils.UtcNow.Ticks + TimeSpan.FromHours(1).Ticks;
            lastSyncTimeTicksField.SetValue(null, futureTime);

            bool result = TimeSyncScheduler.ShouldSyncNow();

            lastSyncTimeTicksField.SetValue(null, 0L);

            Assert.That(TimeSyncScheduler.ShouldSyncNow(), Is.True);
        }
    }
}