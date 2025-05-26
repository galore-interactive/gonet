using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    [TestFixture]
    public class TimeSyncActualBehaviorTests : TimeSyncTestBase
    {
        private BlockingCollection<Action> clientActions;
        private BlockingCollection<Action> serverActions;
        private Thread clientThread;
        private Thread serverThread;
        private SecretaryOfTemporalAffairs clientTime;
        private SecretaryOfTemporalAffairs serverTime;

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
                    clientReady.Set(); // Signal that client is ready

                    foreach (var action in clientActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (OperationCanceledException)
                        {
                            break; // Exit gracefully on cancellation
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Client action error: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Client thread error: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "ClientThread"
            };

            serverThread = new Thread(() =>
            {
                try
                {
                    serverTime = new SecretaryOfTemporalAffairs();
                    serverTime.Update();
                    serverReady.Set(); // Signal that server is ready

                    foreach (var action in serverActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (OperationCanceledException)
                        {
                            break; // Exit gracefully on cancellation
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Server action error: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Server thread error: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "ServerThread"
            };

            clientThread.Start();
            serverThread.Start();

            // Wait for both threads to initialize with timeout
            if (!clientReady.Wait(5000) || !serverReady.Wait(5000))
            {
                throw new TimeoutException("Failed to initialize test threads");
            }

            clientReady.Dispose();
            serverReady.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                // Signal shutdown
                if (cts != null && !cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }

                // Complete the collections to unblock the threads
                clientActions?.CompleteAdding();
                serverActions?.CompleteAdding();

                // Give threads time to exit gracefully
                if (clientThread != null && clientThread.IsAlive)
                {
                    if (!clientThread.Join(1000))
                    {
                        UnityEngine.Debug.LogWarning("Client thread did not exit gracefully");
                    }
                }

                if (serverThread != null && serverThread.IsAlive)
                {
                    if (!serverThread.Join(1000))
                    {
                        UnityEngine.Debug.LogWarning("Server thread did not exit gracefully");
                    }
                }

                // Dispose collections
                clientActions?.Dispose();
                serverActions?.Dispose();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error during teardown: {ex.Message}");
            }
            finally
            {
                base.BaseTearDown();
            }
        }

        [Test]
        [Category("ActualBehavior")]
        public void Should_Understand_How_Duplicate_Detection_Works()
        {
            // The lastProcessedResponseTicks field tracks the server time
            // to prevent processing old responses

            var timeSyncType = typeof(HighPerfTimeSync);
            var lastProcessedField = timeSyncType.GetField("lastProcessedResponseTicks",
                BindingFlags.NonPublic | BindingFlags.Static);

            UpdateBothTimes();

            // First sync
            var request1 = CreateTimeSyncRequest();
            Thread.Sleep(50);
            UpdateBothTimes();
            long serverTime1 = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            HighPerfTimeSync.ProcessTimeSync(
                request1.UID,
                serverTime1,
                request1,
                clientTime,
                false
            );

            long lastProcessed1 = (long)lastProcessedField.GetValue(null);
            UnityEngine.Debug.Log($"After first sync: lastProcessed = {lastProcessed1 / (double)TimeSpan.TicksPerSecond:F3}s");

            // Second sync with newer server time
            Thread.Sleep(100);
            UpdateBothTimes();
            var request2 = CreateTimeSyncRequest();
            Thread.Sleep(50);
            UpdateBothTimes();
            long serverTime2 = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            HighPerfTimeSync.ProcessTimeSync(
                request2.UID,
                serverTime2,
                request2,
                clientTime,
                false
            );

            long lastProcessed2 = (long)lastProcessedField.GetValue(null);
            UnityEngine.Debug.Log($"After second sync: lastProcessed = {lastProcessed2 / (double)TimeSpan.TicksPerSecond:F3}s");

            Assert.That(serverTime2, Is.GreaterThan(serverTime1), "Server time should increase");
            Assert.That(lastProcessed2, Is.GreaterThan(lastProcessed1), "Last processed should update");
        }

        [Test]
        [Category("ActualBehavior")]
        public void Should_Understand_Minimum_Threshold_With_OneWayDelay()
        {
            // The system calculates: adjustedServerTime = serverTime + oneWayDelay
            // So even with identical times, it sees a 25ms difference

            UpdateBothTimes();

            // Set server and client to exact same time
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            RunOnThread(() => serverTime.SetFromAuthority(clientTicks), serverActions);
            Thread.Sleep(1100);

            UpdateBothTimes();

            var request = CreateTimeSyncRequest();
            // No delay - immediate response
            long serverTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);
            long clientTicksCurrent = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);

            UnityEngine.Debug.Log($"Raw difference: {(serverTicks - clientTicksCurrent) / (double)TimeSpan.TicksPerSecond:F6}s");

            // But ProcessTimeSync adds one-way delay (25ms) to server time
            // So it calculates: adjustedServerTime = serverTicks + 25ms
            // Resulting in a 25ms difference even when times are identical

            var timeSyncType = typeof(HighPerfTimeSync);
            var adjustmentCountField = timeSyncType.GetField("adjustmentCount",
                BindingFlags.NonPublic | BindingFlags.Static);
            int beforeCount = (int)adjustmentCountField.GetValue(null);

            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverTicks,
                request,
                clientTime,
                false
            );

            int afterCount = (int)adjustmentCountField.GetValue(null);

            // It will adjust because it sees 25ms difference (above 5ms threshold)
            Assert.That(afterCount, Is.EqualTo(beforeCount + 1),
                "Will adjust due to one-way delay calculation");
        }

        [Test]
        [Category("ActualBehavior")]
        public void Should_Sync_Large_Time_Differences_Correctly()
        {
            // Test what actually happens with large differences

            // Initialize both times
            UpdateBothTimes();

            // Set server ahead by 1 hour from client's current time
            long clientCurrentTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            RunOnThread(() => serverTime.SetFromAuthority(clientCurrentTicks + TimeSpan.FromHours(1).Ticks), serverActions);

            // Wait for interpolation to complete
            Thread.Sleep(1100);

            UpdateBothTimes();

            double clientSeconds = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            double serverSeconds = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
            double initialDiff = serverSeconds - clientSeconds;
            UnityEngine.Debug.Log($"Initial difference: {initialDiff:F1}s");
            Assert.That(initialDiff, Is.GreaterThan(3500), "Initial setup should have ~1 hour difference");

            // With the 30-second clamp, we need multiple syncs to close a 1-hour gap
            // Let's test that the first sync reduces the gap significantly
            var request = CreateTimeSyncRequest();
            Thread.Sleep(25);

            long serverResponseTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

            HighPerfTimeSync.ProcessTimeSync(
                request.UID,
                serverResponseTicks,
                request,
                clientTime,
                false
            );

            // The sync calls SetFromAuthority which starts interpolation
            // After 1 second, check the adjustment
            Thread.Sleep(1100);

            UpdateBothTimes();

            clientSeconds = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            serverSeconds = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
            double afterFirstSyncDiff = Math.Abs(serverSeconds - clientSeconds);
            UnityEngine.Debug.Log($"Difference after first sync: {afterFirstSyncDiff:F3}s");

            // The difference should be reduced by the clamp amount (30 seconds)
            // So from ~3600s to ~3570s
            Assert.That(afterFirstSyncDiff, Is.LessThan(initialDiff - 25),
                "First sync should reduce difference by at least 25 seconds (allowing for some timing variance)");

            // To fully sync, we'd need multiple rounds
            // Let's verify that subsequent syncs continue to close the gap
            for (int i = 0; i < 3; i++)
            {
                var request2 = CreateTimeSyncRequest();
                Thread.Sleep(25);

                serverResponseTicks = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

                HighPerfTimeSync.ProcessTimeSync(
                    request2.UID,
                    serverResponseTicks,
                    request2,
                    clientTime,
                    false
                );

                Thread.Sleep(1100);
                UpdateBothTimes();
            }

            clientSeconds = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            serverSeconds = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
            double finalDiff = Math.Abs(serverSeconds - clientSeconds);
            UnityEngine.Debug.Log($"Final difference after multiple syncs: {finalDiff:F3}s");

            // After multiple syncs, we should be much closer
            Assert.That(finalDiff, Is.LessThan(afterFirstSyncDiff),
                "Multiple syncs should continue to reduce the difference");
        }

        // Helper methods
        private void UpdateBothTimes()
        {
            RunOnThread(() => clientTime.Update(), clientActions);
            RunOnThread(() => serverTime.Update(), serverActions);
        }

        private RequestMessage CreateTimeSyncRequest()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            return new RequestMessage(clientTicks);
        }
    }
}