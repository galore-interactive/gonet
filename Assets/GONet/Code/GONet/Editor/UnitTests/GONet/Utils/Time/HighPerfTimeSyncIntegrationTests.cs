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
    public class HighPerfTimeSyncIntegrationTests
    {
        private SecretaryOfTemporalAffairs clientTime;
        private SecretaryOfTemporalAffairs serverTime;
        private CancellationTokenSource cts;
        private long networkLatencyTicks;
        private System.Random jitterRandom;

        private Thread clientThread;
        private Thread serverThread;
        private BlockingCollection<Action> clientActions;
        private BlockingCollection<Action> serverActions;

        [SetUp]
        public void Setup()
        {
            clientTime = null;
            serverTime = null;
            cts = new CancellationTokenSource();
            networkLatencyTicks = TimeSpan.FromMilliseconds(50).Ticks;
            jitterRandom = new System.Random(42);

            // Initialize blocking collections for thread-safe action queuing
            clientActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());
            serverActions = new BlockingCollection<Action>(new ConcurrentQueue<Action>());

            // Start dedicated threads for client and server updates
            clientThread = new Thread(() =>
            {
                try
                {
                    clientTime = new SecretaryOfTemporalAffairs();
                    clientTime.Update();
                    UnityEngine.Debug.Log("Client thread initialized.");

                    foreach (var action in clientActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Client Thread Action Failed: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("Client thread canceled.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Client Thread Failed: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "ClientThread"
            };
            clientThread.Start();

            serverThread = new Thread(() =>
            {
                try
                {
                    serverTime = new SecretaryOfTemporalAffairs();
                    serverTime.Update();
                    UnityEngine.Debug.Log("Server thread initialized.");

                    foreach (var action in serverActions.GetConsumingEnumerable(cts.Token))
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Server Thread Action Failed: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    UnityEngine.Debug.Log("Server thread canceled.");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Server Thread Failed: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "ServerThread"
            };
            serverThread.Start();

            // Wait for threads to initialize
            Thread.Sleep(100); // Give threads a moment to start
        }

        [TearDown]
        public void TearDown()
        {
            cts.Cancel();
            clientActions.CompleteAdding();
            serverActions.CompleteAdding();

            clientThread.Join(1000);
            serverThread.Join(1000);

            if (clientThread.IsAlive)
            {
                UnityEngine.Debug.LogWarning("Client thread did not terminate cleanly.");
            }
            if (serverThread.IsAlive)
            {
                UnityEngine.Debug.LogWarning("Server thread did not terminate cleanly.");
            }

            cts?.Dispose();
            clientActions?.Dispose();
            serverActions?.Dispose();
        }

        [Test]
        [Category("BasicSync")]
        [Timeout(20000)]
        public void Should_Handle_Multiple_Sync_Cycles_ThreadSafe()
        {
            UpdateBothTimes();
            Thread.Sleep(100); // Ensure initial state stabilizes

            // Set a consistent initial difference
            long initialOffset = TimeSpan.FromSeconds(3).Ticks;
            long clientInitialTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            RunOnThread(() => { serverTime.SetFromAuthority(clientInitialTicks + initialOffset); }, serverActions);

            // Wait for initial interpolation to complete (1 second)
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100); // 1s total
                UpdateBothTimes();
            }

            // Recalibrate the server time to ensure exactly 3s difference
            double clientCurrent = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
            Thread.Sleep(10); // Small delay to minimize timing variation
            double serverCurrent = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
            double currentDifference = serverCurrent - clientCurrent;
            double adjustmentSeconds = 3.0 - currentDifference;
            long adjustmentTicks = (long)(adjustmentSeconds * TimeSpan.TicksPerSecond);
            UnityEngine.Debug.Log($"Recalibration: clientCurrent={clientCurrent:F3}s, serverCurrent={serverCurrent:F3}s, currentDifference={currentDifference:F3}s, adjustmentTicks={(adjustmentTicks / (double)TimeSpan.TicksPerSecond):F3}s");
            RunOnThread(() => { serverTime.SetFromAuthority(serverTime.ElapsedTicks + adjustmentTicks); }, serverActions);

            // Wait for recalibration interpolation to complete (1 second)
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100); // 1s total
                UpdateBothTimes();
            }

            // Ensure the initial difference is close to 3 seconds
            double clientVal, serverVal, initialDifference;
            var sw = Stopwatch.StartNew();
            do
            {
                clientVal = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
                Thread.Sleep(10); // Small delay to minimize timing variation
                serverVal = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
                initialDifference = Math.Abs(serverVal - clientVal);
                if (sw.ElapsedMilliseconds > 2000) // Timeout after 2 seconds
                {
                    Assert.Fail($"Failed to achieve initial difference of ~3s. Got {initialDifference:F3}s");
                }
                Thread.Sleep(50);
                UpdateBothTimes();
            } while (Math.Abs(initialDifference - 3.0) > 0.1); // Allow 100ms tolerance

            var differences = new List<double> { initialDifference };
            UnityEngine.Debug.Log($"Initial difference: {initialDifference:F3}s");
            UnityEngine.Debug.Log($"Client State: {RunOnThread<string>(() => clientTime.DebugState(), clientActions)}");
            UnityEngine.Debug.Log($"Server State: {RunOnThread<string>(() => serverTime.DebugState(), serverActions)}");

            for (int cycle = 0; cycle < 5; cycle++)
            {
                UnityEngine.Debug.Log($"\n=== Sync Cycle {cycle + 1} ===");

                var request = CreateTimeSyncRequest();
                SimulateNetworkDelay();
                UpdateBothTimes();

                // Ensure both client and server times are updated before sampling
                for (int i = 0; i < 5; i++)
                {
                    UpdateBothTimes();
                    Thread.Sleep(10);
                }

                long serverResponseTime = RunOnThread<long>(() => serverTime.ElapsedTicks, serverActions);
                UnityEngine.Debug.Log($"Server Response Time: {serverResponseTime / (double)TimeSpan.TicksPerSecond:F3}s");
                double clientTimeBeforeSync = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
                UnityEngine.Debug.Log($"Client Time Before Sync: {clientTimeBeforeSync:F3}s");
                SimulateNetworkDelay();
                UpdateBothTimes();

                var (settled, remainingMs) = clientTime.CheckAdjustmentStatus();
                if (!settled)
                {
                    Thread.Sleep(remainingMs > 0 ? remainingMs : 100);
                }

                HighPerfTimeSync.ProcessTimeSync(request.UID, serverResponseTime, request, clientTime, cycle == 0);

                // Ensure interpolation completes
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(100); // 1s total
                    UpdateBothTimes();
                }

                // Ensure updates are complete before final sampling
                for (int i = 0; i < 5; i++)
                {
                    UpdateBothTimes();
                    Thread.Sleep(10);
                }

                clientVal = RunOnThread<double>(() => clientTime.ElapsedSeconds, clientActions);
                Thread.Sleep(10); // Small delay to minimize timing variation
                serverVal = RunOnThread<double>(() => serverTime.ElapsedSeconds, serverActions);
                double diff = Math.Abs(serverVal - clientVal);
                differences.Add(diff);
                UnityEngine.Debug.Log($"Client: {clientVal:F3}s, Server: {serverVal:F3}s, Difference after cycle {cycle + 1}: {diff:F3}s");
                UnityEngine.Debug.Log($"Client State: {RunOnThread<string>(() => clientTime.DebugState(), clientActions)}");
                UnityEngine.Debug.Log($"Server State: {RunOnThread<string>(() => serverTime.DebugState(), serverActions)}");
            }

            UnityEngine.Debug.Log($"Final difference: {differences.Last():F3}s, Initial: {differences.First():F3}s, Expected RTT contribution: {(50.0 / 1000.0 / 2):F3}s");

            Assert.That(differences.Last(), Is.LessThan(differences.First() * 0.5).Or.LessThan(0.3),
                $"Should converge over multiple cycles. Initial: {differences.First():F3}s, Final: {differences.Last():F3}s");
            Assert.That(differences.Last(), Is.LessThan(0.3),
                $"Should achieve tight sync after multiple cycles, but final difference is {differences.Last():F3}s");
        }

        private void UpdateBothTimes()
        {
            RunOnThread(() => clientTime.Update(), clientActions);
            RunOnThread(() => serverTime.Update(), serverActions);
        }

        private T RunOnThread<T>(Func<T> func, BlockingCollection<Action> actions)
        {
            T result = default(T);
            var resetEvent = new ManualResetEventSlim(false);
            Exception taskException = null;

            actions.Add(() =>
            {
                try
                {
                    result = func();
                    resetEvent.Set();
                }
                catch (Exception ex)
                {
                    taskException = ex;
                    UnityEngine.Debug.LogError($"RunOnThread Failed: {ex.Message}");
                    resetEvent.Set();
                }
            });

            if (!resetEvent.Wait(2000, cts.Token))
            {
                throw new TimeoutException($"RunOnThread timed out for function: {func.Method.Name}");
            }

            if (taskException != null) throw taskException;

            resetEvent.Dispose();
            return result;
        }

        private void RunOnThread(Action action, BlockingCollection<Action> actions)
        {
            var resetEvent = new ManualResetEventSlim(false);
            Exception taskException = null;

            actions.Add(() =>
            {
                try
                {
                    action();
                    resetEvent.Set();
                }
                catch (Exception ex)
                {
                    taskException = ex;
                    UnityEngine.Debug.LogError($"RunOnThread Failed: {ex.Message}");
                    resetEvent.Set();
                }
            });

            if (!resetEvent.Wait(2000, cts.Token))
            {
                throw new TimeoutException($"RunOnThread timed out for action.");
            }

            if (taskException != null) throw taskException;

            resetEvent.Dispose();
        }

        private RequestMessage CreateTimeSyncRequest()
        {
            long clientTicks = RunOnThread<long>(() => clientTime.ElapsedTicks, clientActions);
            return new RequestMessage(clientTicks);
        }

        private void SimulateNetworkDelay(bool withJitter = false)
        {
            double baseDelayMs = new TimeSpan(networkLatencyTicks).TotalMilliseconds / 2;
            if (withJitter)
            {
                baseDelayMs += jitterRandom.NextDouble() * 10;
            }
            Thread.Sleep((int)baseDelayMs);
        }
    }
}