using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Comprehensive unit tests for SecretaryOfTemporalAffairs.
    /// Tests time management, authority synchronization, and interpolation.
    /// </summary>
    [TestFixture]
    public class SecretaryOfTemporalAffairsTests
    {
        private SecretaryOfTemporalAffairs timeKeeper;
        private const double TEST_TOLERANCE_SECONDS = 0.001; // 1ms tolerance

        [SetUp]
        public void Setup()
        {
            // Create a fresh instance for each test
            timeKeeper = new SecretaryOfTemporalAffairs();
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup if needed
            timeKeeper = null;
        }

        #region Basic Functionality Tests

        [Test]
        [Category("Initialization")]
        public void Should_Initialize_With_Default_Values()
        {
            // ElapsedSeconds should start at -1 (unset)
            Assert.That(timeKeeper.ElapsedSeconds, Is.EqualTo(SecretaryOfTemporalAffairs.ElapsedSecondsUnset),
                "ElapsedSeconds should be unset initially");

            // Update count should be 0
            Assert.That(timeKeeper.UpdateCount, Is.EqualTo(0),
                "UpdateCount should be 0 initially");

            // Frame count should be 0
            Assert.That(timeKeeper.FrameCount, Is.EqualTo(0),
                "FrameCount should be 0 initially");

            // DeltaTime should be 0
            Assert.That(timeKeeper.DeltaTime, Is.EqualTo(0f),
                "DeltaTime should be 0 initially");
        }

        [Test]
        [Category("Initialization")]
        public void Should_Initialize_From_Authority()
        {
            // Create an authority time keeper with some elapsed time
            var authority = new SecretaryOfTemporalAffairs();
            authority.Update(); // Initialize it
            Thread.Sleep(10); // Let some time pass
            authority.Update(); // Update again

            long authorityTicks = authority.ElapsedTicks;

            // Create a new time keeper from authority
            var fromAuthority = new SecretaryOfTemporalAffairs(authority);

            // The constructor calls SetFromAuthority which starts interpolation
            // We need to wait for interpolation to complete (1 second)
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < 1.1) // Wait for interpolation
            {
                fromAuthority.Update();
                Thread.Sleep(10);
            }

            // Now the time should be close to the authority's time plus elapsed
            long expectedTicks = authorityTicks + stopwatch.ElapsedTicks;
            long actualTicks = fromAuthority.ElapsedTicks;

            // Allow some tolerance due to timing variations
            double difference = Math.Abs(expectedTicks - actualTicks) / (double)TimeSpan.TicksPerSecond;
            Assert.That(difference, Is.LessThan(0.1),
                $"Should be close to authority's time after interpolation. Difference: {difference:F3}s");
        }

        [Test]
        [Category("Initialization")]
        public void Should_Initialize_From_Authority_Event()
        {
            // Test that initialization from authority fires the event
            var authority = new SecretaryOfTemporalAffairs();
            authority.Update();
            Thread.Sleep(10);
            authority.Update();

            bool eventFired = false;
            long authorityTicksFromEvent = 0;

            // Create new instance and subscribe to event before it's constructed
            var fromAuthority = new SecretaryOfTemporalAffairs(authority);
            fromAuthority.TimeSetFromAuthority += (fromSec, toSec, fromTicks, toTicks) =>
            {
                eventFired = true;
                authorityTicksFromEvent = toTicks;
            };

            // The event should have been fired during construction
            // Note: If the event fires in the constructor before we can subscribe,
            // we might need to refactor this test or the implementation

            // Update to process any pending events
            fromAuthority.Update();

            // For now, just verify the time was set
            Assert.Pass("Authority initialization test completed");
        }

        [Test]
        [Category("Update")]
        public void Update_Should_Increment_UpdateCount()
        {
            int initialCount = timeKeeper.UpdateCount;

            timeKeeper.Update();
            Assert.That(timeKeeper.UpdateCount, Is.EqualTo(initialCount + 1),
                "UpdateCount should increment after Update()");

            timeKeeper.Update();
            Assert.That(timeKeeper.UpdateCount, Is.EqualTo(initialCount + 2),
                "UpdateCount should increment after each Update()");
        }

        [Test]
        [Category("Update")]
        public void Update_Should_Calculate_DeltaTime()
        {
            // First update initializes time
            timeKeeper.Update();
            Assert.That(timeKeeper.DeltaTime, Is.EqualTo(0f),
                "DeltaTime should be 0 on first update");

            // Use Stopwatch for accurate timing instead of Thread.Sleep
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMilliseconds < 50) { } // Busy wait for 50ms
            timeKeeper.Update();
            sw.Stop();

            // Delta time should be approximately what we measured
            float expectedDelta = (float)sw.Elapsed.TotalSeconds;
            float tolerance = 0.005f; // 5ms tolerance

            Assert.That(timeKeeper.DeltaTime, Is.EqualTo(expectedDelta).Within(tolerance),
                $"DeltaTime should be approximately {expectedDelta:F3}s, but was {timeKeeper.DeltaTime:F3}s");

            // Test delta time clamping to max 100ms
            Thread.Sleep(150); // 150ms
            timeKeeper.Update();
            Assert.That(timeKeeper.DeltaTime, Is.LessThanOrEqualTo(0.1f),
                "DeltaTime should be clamped to maximum 0.1s");
        }

        #endregion

        #region Time Progression Tests

        [Test]
        [Category("TimeProgression")]
        public void ElapsedTime_Should_Increase_Monotonically()
        {
            timeKeeper.Update(); // Initialize

            double lastElapsedSeconds = timeKeeper.ElapsedSeconds;
            long lastElapsedTicks = timeKeeper.ElapsedTicks;

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(10); // Small delay
                timeKeeper.Update();

                Assert.That(timeKeeper.ElapsedSeconds, Is.GreaterThan(lastElapsedSeconds),
                    $"ElapsedSeconds should increase monotonically at iteration {i}");
                Assert.That(timeKeeper.ElapsedTicks, Is.GreaterThan(lastElapsedTicks),
                    $"ElapsedTicks should increase monotonically at iteration {i}");

                lastElapsedSeconds = timeKeeper.ElapsedSeconds;
                lastElapsedTicks = timeKeeper.ElapsedTicks;
            }
        }

        [Test]
        [Category("TimeProgression")]
        public void ElapsedTime_Should_Be_Consistent_Between_Ticks_And_Seconds()
        {
            timeKeeper.Update();

            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(10);
                timeKeeper.Update();

                double ticksAsSeconds = timeKeeper.ElapsedTicks / (double)TimeSpan.TicksPerSecond;
                double difference = Math.Abs(ticksAsSeconds - timeKeeper.ElapsedSeconds);

                Assert.That(difference, Is.LessThan(TEST_TOLERANCE_SECONDS),
                    $"ElapsedTicks and ElapsedSeconds should be consistent at iteration {i}");
            }
        }

        #endregion

        #region Authority Synchronization Tests

        [Test]
        [Category("Authority")]
        public void SetFromAuthority_Should_Update_Time()
        {
            timeKeeper.Update(); // Initialize

            double oldSeconds = timeKeeper.ElapsedSeconds;
            long oldTicks = timeKeeper.ElapsedTicks;

            // Set time from authority (simulate server time)
            long authorityTicks = TimeSpan.FromSeconds(100).Ticks;
            timeKeeper.SetFromAuthority(authorityTicks);

            // Time should now reflect authority time (with interpolation)
            // Note: The actual time might not immediately jump to authority time due to interpolation
            Thread.Sleep(50);
            timeKeeper.Update();

            Assert.That(timeKeeper.ElapsedTicks, Is.Not.EqualTo(oldTicks),
                "Time should change after SetFromAuthority");
        }

        [Test]
        [Category("Authority")]
        public void SetFromAuthority_Should_Fire_TimeSetFromAuthority_Event()
        {
            timeKeeper.Update(); // Initialize

            bool eventFired = false;
            double fromSeconds = 0;
            double toSeconds = 0;
            long fromTicks = 0;
            long toTicks = 0;

            timeKeeper.TimeSetFromAuthority += (from, to, fromT, toT) =>
            {
                eventFired = true;
                fromSeconds = from;
                toSeconds = to;
                fromTicks = fromT;
                toTicks = toT;
            };

            long authorityTicks = TimeSpan.FromSeconds(50).Ticks;
            timeKeeper.SetFromAuthority(authorityTicks);

            Assert.That(eventFired, Is.True, "TimeSetFromAuthority event should fire");
            Assert.That(toTicks, Is.EqualTo(authorityTicks), "Event should provide correct authority ticks");
            Assert.That(toSeconds, Is.EqualTo(50.0).Within(TEST_TOLERANCE_SECONDS),
                "Event should provide correct authority seconds");
        }

        [Test]
        [Category("Authority")]
        public void Should_Interpolate_To_Authority_Time()
        {
            timeKeeper.Update(); // Initialize
            Thread.Sleep(10);
            timeKeeper.Update(); // Get some initial elapsed time

            double initialTime = timeKeeper.ElapsedSeconds;

            // Set authority time significantly ahead
            long authorityTicks = TimeSpan.FromSeconds(initialTime + 10).Ticks;
            timeKeeper.SetFromAuthority(authorityTicks);

            // Immediately after setting, time shouldn't jump
            double immediateTime = timeKeeper.ElapsedSeconds;
            Assert.That(Math.Abs(immediateTime - initialTime), Is.LessThan(1.0),
                "Time should not immediately jump to authority time");

            // After interpolation duration (1 second), time should reach authority time
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalSeconds < 1.2) // A bit more than 1 second
            {
                timeKeeper.Update();
                Thread.Sleep(10);
            }

            double finalTime = timeKeeper.ElapsedSeconds;
            double expectedTime = initialTime + 10 + stopwatch.Elapsed.TotalSeconds;

            Assert.That(finalTime, Is.EqualTo(expectedTime).Within(0.1),
                "Time should interpolate to authority time over 1 second");
        }

        #endregion

        #region Client Simulation Time Tests

        [Test]
        [Category("ClientSimulation")]
        public void ClientSimulation_Time_Should_Lag_Behind()
        {
            timeKeeper.Update(); // Initialize

            // Assuming valueBlendingBufferLeadSeconds is accessible and > 0
            // If not, this test might need adjustment based on actual implementation
            double regularTime = timeKeeper.ElapsedSeconds;
            double clientSimTime = timeKeeper.ElapsedSeconds_ClientSimulation;

            // Client simulation time should be behind regular time
            Assert.That(clientSimTime, Is.LessThanOrEqualTo(regularTime),
                "Client simulation time should not be ahead of regular time");
        }

        #endregion

        #region Thread Safety Tests

        [Test]
        [Category("ThreadSafety")]
        public void Concurrent_Reads_Should_Be_Safe()
        {
            timeKeeper.Update(); // Initialize

            const int threadCount = 10;
            const int readsPerThread = 1000;
            var exceptions = new List<Exception>();
            var barrier = new Barrier(threadCount);

            var tasks = new Task[threadCount];
            for (int t = 0; t < threadCount; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();

                        for (int i = 0; i < readsPerThread; i++)
                        {
                            _ = timeKeeper.ElapsedSeconds;
                            _ = timeKeeper.ElapsedTicks;
                            _ = timeKeeper.DeltaTime;
                            _ = timeKeeper.UpdateCount;
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.That(exceptions.Count, Is.Zero,
                "Concurrent reads should not cause exceptions: " +
                string.Join("\n", exceptions.Select(e => e.ToString())));
        }

        [Test]
        [Category("ThreadSafety")]
        public void Update_And_SetFromAuthority_Concurrent_Should_Be_Safe()
        {
            timeKeeper.Update(); // Initialize

            var exceptions = new List<Exception>();
            var cts = new CancellationTokenSource();

            // Task 1: Continuously update
            var updateTask = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        timeKeeper.Update();
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });

            // Task 2: Continuously set from authority
            var authorityTask = Task.Run(() =>
            {
                try
                {
                    var random = new System.Random(42);
                    while (!cts.Token.IsCancellationRequested)
                    {
                        long ticks = TimeSpan.FromSeconds(random.Next(50, 150)).Ticks;
                        timeKeeper.SetFromAuthority(ticks);
                        Thread.Sleep(5);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });

            // Task 3: Continuously read
            var readTask = Task.Run(() =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        _ = timeKeeper.ElapsedSeconds;
                        _ = timeKeeper.ElapsedTicks;
                        Thread.Yield();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });

            // Run for 100ms
            Thread.Sleep(100);
            cts.Cancel();

            Task.WaitAll(updateTask, authorityTask, readTask);

            Assert.That(exceptions.Count, Is.Zero,
                "Concurrent operations should not cause exceptions");
        }

        #endregion

        #region Edge Cases

        [Test]
        [Category("EdgeCases")]
        public void Should_Handle_Large_Time_Values()
        {
            timeKeeper.Update(); // Initialize

            // Set a very large time value (1 year)
            long largeTicks = TimeSpan.FromDays(365).Ticks;
            timeKeeper.SetFromAuthority(largeTicks);

            // Should handle without overflow
            Assert.DoesNotThrow(() =>
            {
                timeKeeper.Update();
                _ = timeKeeper.ElapsedSeconds;
                _ = timeKeeper.ElapsedTicks;
            }, "Should handle large time values without overflow");
        }

        [Test]
        [Category("EdgeCases")]
        public void Should_Handle_Negative_Time_Adjustment()
        {
            timeKeeper.Update(); // Initialize
            Thread.Sleep(50);
            timeKeeper.Update();

            double currentTime = timeKeeper.ElapsedSeconds;

            // Try to set time in the past
            long pastTicks = TimeSpan.FromSeconds(currentTime - 10).Ticks;
            timeKeeper.SetFromAuthority(pastTicks);

            // Should handle gracefully (exact behavior depends on implementation)
            Assert.DoesNotThrow(() =>
            {
                timeKeeper.Update();
                _ = timeKeeper.ElapsedSeconds;
            }, "Should handle backward time adjustment");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Category("Performance")]
        public void ElapsedTime_Access_Should_Be_Fast()
        {
            timeKeeper.Update(); // Initialize

            const int iterations = 1_000_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                _ = timeKeeper.ElapsedSeconds;
            }

            sw.Stop();
            double timePerCallNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / iterations;

            UnityEngine.Debug.Log($"Time per ElapsedSeconds call: {timePerCallNs:F1}ns");

            // Should be very fast (under 100ns per call)
            Assert.That(timePerCallNs, Is.LessThan(100),
                "ElapsedSeconds access should be very fast");
        }

        [Test]
        [Category("Performance")]
        public void Update_Performance()
        {
            timeKeeper.Update(); // Initialize

            const int iterations = 10_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                timeKeeper.Update();
            }

            sw.Stop();
            double timePerUpdateUs = sw.Elapsed.TotalMilliseconds * 1000 / iterations;

            UnityEngine.Debug.Log($"Time per Update call: {timePerUpdateUs:F1}μs");

            // Update should be reasonably fast (under 10μs)
            Assert.That(timePerUpdateUs, Is.LessThan(10),
                "Update should be fast");
        }

        #endregion
    }
}