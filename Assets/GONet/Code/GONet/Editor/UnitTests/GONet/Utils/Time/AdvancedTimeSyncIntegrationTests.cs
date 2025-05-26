using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GONet.Utils;
using NUnit.Framework;
using UnityEngine;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    [TestFixture]
    public class AdvancedTimeSyncIntegrationTests : TimeSyncTestBase
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
        [Category("AdvancedSync")]
        [Timeout(60000)] // 60 seconds for full test
        public void Should_Handle_Variable_Network_Conditions()
        {
            UpdateBothTimes();

            // Set initial offset
            RunOnThread(() => serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(5).Ticks), serverActions);
            Thread.Sleep(1100); // Wait for interpolation

            // Do an initial sync to get closer before starting the test
            var initialRequest = CreateTimeSyncRequest();
            Thread.Sleep(25);
            HighPerfTimeSync.ProcessTimeSync(
                initialRequest.UID,
                RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions),
                initialRequest,
                clientTime,
                true // Force initial adjustment
            );
            Thread.Sleep(1100); // Wait for initial sync to complete

            var networkProfiles = new[]
            {
                new { Name = "Good", MinRtt = 20, MaxRtt = 40, PacketLoss = 0.0 },
                new { Name = "Fair", MinRtt = 50, MaxRtt = 150, PacketLoss = 0.01 },
                new { Name = "Poor", MinRtt = 100, MaxRtt = 500, PacketLoss = 0.05 },
                new { Name = "Terrible", MinRtt = 200, MaxRtt = 1000, PacketLoss = 0.10 }
            };

            var random = new System.Random(42);
            var results = new Dictionary<string, List<double>>();

            foreach (var profile in networkProfiles)
            {
                UnityEngine.Debug.Log($"\n=== Testing {profile.Name} Network Conditions ===");
                results[profile.Name] = new List<double>();

                for (int i = 0; i < 5; i++) // 5 cycles per profile
                {
                    if (cts.Token.IsCancellationRequested) break;

                    UpdateBothTimes();

                    // Simulate packet loss
                    if (random.NextDouble() < profile.PacketLoss)
                    {
                        UnityEngine.Debug.Log($"Packet lost (cycle {i})");
                        Thread.Sleep(100);
                        continue;
                    }

                    var request = CreateTimeSyncRequest();

                    // Variable RTT
                    int rtt = random.Next(profile.MinRtt, profile.MaxRtt);
                    Thread.Sleep(rtt / 2);

                    long serverResponseTime = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);

                    Thread.Sleep(rtt / 2);
                    UpdateBothTimes();

                    HighPerfTimeSync.ProcessTimeSync(
                        request.UID,
                        serverResponseTime,
                        request,
                        clientTime,
                        false
                    );

                    // Wait for adjustment - need full interpolation time
                    Thread.Sleep(1100);
                    UpdateBothTimes();

                    double diff = Math.Abs(
                        RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                        RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
                    );

                    results[profile.Name].Add(diff);
                    UnityEngine.Debug.Log($"Cycle {i}: RTT={rtt}ms, Diff={diff:F3}s");
                }

                if (cts.Token.IsCancellationRequested) break;
            }

            // Verify sync quality degrades gracefully
            if (results.ContainsKey("Good") && results["Good"].Count > 0)
                Assert.That(results["Good"].Average(), Is.LessThan(0.15), "Good network should achieve tight sync");
            if (results.ContainsKey("Fair") && results["Fair"].Count > 0)
                Assert.That(results["Fair"].Average(), Is.LessThan(0.25), "Fair network should maintain reasonable sync");
            if (results.ContainsKey("Poor") && results["Poor"].Count > 0)
                Assert.That(results["Poor"].Average(), Is.LessThan(0.5), "Poor network should still sync");

            foreach (var kvp in results.Where(k => k.Value.Count > 0))
            {
                UnityEngine.Debug.Log($"{kvp.Key} Network - Avg diff: {kvp.Value.Average():F3}s, Max: {kvp.Value.Max():F3}s");
            }
        }

        [Test]
        [Category("AdvancedSync")]
        [Timeout(15000)]
        public void Should_Handle_Asymmetric_Latency()
        {
            UpdateBothTimes();

            // Simulate asymmetric network (common in real networks)
            // Upload slower than download
            var uploadLatency = 100; // ms
            var downloadLatency = 20; // ms

            RunOnThread(() => serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(2).Ticks), serverActions);
            Thread.Sleep(1100);

            var differences = new List<double>();

            for (int i = 0; i < 10; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                UpdateBothTimes();

                var request = CreateTimeSyncRequest();
                Thread.Sleep(uploadLatency);

                long serverResponseTime = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);
                Thread.Sleep(downloadLatency);

                UpdateBothTimes();

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    serverResponseTime,
                    request,
                    clientTime,
                    i == 0
                );

                Thread.Sleep(1100);
                UpdateBothTimes();

                double diff = Math.Abs(
                    RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                    RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
                );

                differences.Add(diff);
                UnityEngine.Debug.Log($"Cycle {i}: Asymmetric latency - Diff={diff:F3}s");
            }

            // Even with asymmetric latency, median RTT should help achieve decent sync
            double avgDiff = differences.Skip(5).Average(); // Skip initial convergence
            Assert.That(avgDiff, Is.LessThan(0.15),
                $"Should handle asymmetric latency reasonably well. Avg diff: {avgDiff:F3}s");
        }

        [Test]
        [Category("AdvancedSync")]
        [Timeout(20000)]
        public void Should_Recover_From_Time_Jump()
        {
            UpdateBothTimes();

            // Achieve initial sync
            for (int i = 0; i < 5; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                var request = CreateTimeSyncRequest();
                Thread.Sleep(25);

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions),
                    request,
                    clientTime,
                    i == 0
                );

                Thread.Sleep(1100);
                UpdateBothTimes();
            }

            double syncedDiff = Math.Abs(
                RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
            );

            UnityEngine.Debug.Log($"Initial synced difference: {syncedDiff:F3}s");
            Assert.That(syncedDiff, Is.LessThan(0.1), "Should be synced initially");

            // Simulate server time jump (e.g., NTP adjustment)
            UnityEngine.Debug.Log("\n=== Simulating server time jump ===");
            RunOnThread(() => serverTime.SetFromAuthority(serverTime.ElapsedTicks + TimeSpan.FromSeconds(30).Ticks), serverActions);
            Thread.Sleep(1100);
            UpdateBothTimes();

            double jumpedDiff = Math.Abs(
                RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
            );

            UnityEngine.Debug.Log($"Difference after jump: {jumpedDiff:F3}s");
            Assert.That(jumpedDiff, Is.GreaterThan(25), "Should show large difference after jump");

            // Perform recovery syncs
            for (int i = 0; i < 10; i++)
            {
                if (cts.Token.IsCancellationRequested) break;

                var request = CreateTimeSyncRequest();
                Thread.Sleep(25);

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions),
                    request,
                    clientTime,
                    false
                );

                Thread.Sleep(1100);
                UpdateBothTimes();

                double diff = Math.Abs(
                    RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                    RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
                );

                UnityEngine.Debug.Log($"Recovery cycle {i}: Diff={diff:F3}s");
            }

            double recoveredDiff = Math.Abs(
                RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
            );

            Assert.That(recoveredDiff, Is.LessThan(0.2),
                $"Should recover from time jump. Final diff: {recoveredDiff:F3}s");
        }

        [Test]
        [Category("AdvancedSync")]
        [Timeout(10000)]
        public void Should_Maintain_Sync_Under_Continuous_Load()
        {
            // Simplified test - just use main client/server threads
            UpdateBothTimes();

            // Set initial offset
            RunOnThread(() => serverTime.SetFromAuthority(clientTime.ElapsedTicks + TimeSpan.FromSeconds(2).Ticks), serverActions);
            Thread.Sleep(1100);

            var differences = new List<double>();
            var stopwatch = Stopwatch.StartNew();

            // Run for 5 seconds
            while (stopwatch.Elapsed.TotalSeconds < 5 && !cts.Token.IsCancellationRequested)
            {
                UpdateBothTimes();

                var request = CreateTimeSyncRequest();
                Thread.Sleep(30); // Simulate network delay

                HighPerfTimeSync.ProcessTimeSync(
                    request.UID,
                    RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions),
                    request,
                    clientTime,
                    differences.Count == 0
                );

                Thread.Sleep(1100); // Wait for interpolation
                UpdateBothTimes();

                double diff = Math.Abs(
                    RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions) -
                    RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions)
                );

                differences.Add(diff);
                UnityEngine.Debug.Log($"Sync cycle {differences.Count}: Diff={diff:F3}s");

                // Wait before next sync
                Thread.Sleep(500);
            }

            if (differences.Count > 0)
            {
                double avgDiff = differences.Average();
                double maxDiff = differences.Max();
                double minDiff = differences.Min();

                UnityEngine.Debug.Log($"Continuous sync results: Samples={differences.Count}, " +
                                     $"Avg={avgDiff:F3}s, Max={maxDiff:F3}s, Min={minDiff:F3}s");

                Assert.That(avgDiff, Is.LessThan(0.1), "Should maintain good sync under continuous load");
                Assert.That(maxDiff, Is.LessThan(0.2), "Maximum drift should be bounded");
            }
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

        private double CalculateStdDev(List<double> values)
        {
            if (values.Count < 2) return 0;
            double avg = values.Average();
            double sum = values.Sum(d => Math.Pow(d - avg, 2));
            return Math.Sqrt(sum / (values.Count - 1));
        }
    }
}