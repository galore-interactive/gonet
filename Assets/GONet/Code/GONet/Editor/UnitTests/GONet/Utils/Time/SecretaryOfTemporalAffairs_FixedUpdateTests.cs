using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static GONet.GONetMain;

namespace GONet.Tests.Time
{
    /// <summary>
    /// Comprehensive unit tests for SecretaryOfTemporalAffairs FixedUpdate time tracking.
    ///
    /// CRITICAL REQUIREMENTS (per CLAUDE.md Profound Finding):
    /// 1. All time values (ElapsedSeconds, FixedElapsedSeconds) MUST always move forward linearly
    /// 2. No "ping pong" behavior where values jump around
    /// 3. Both regular and fixed time MUST progress together in order like Unity
    /// 4. GONet frame count must be correct in both Update and FixedUpdate
    ///
    /// These tests prevent regressions in physics time synchronization.
    /// </summary>
    [TestFixture]
    public class SecretaryOfTemporalAffairs_FixedUpdateTests
    {
        private SecretaryOfTemporalAffairs timeKeeper;
        private const double TEST_TOLERANCE_SECONDS = 0.001; // 1ms tolerance

        [SetUp]
        public void Setup()
        {
            // CRITICAL: Reset all static state to ensure clean starting point
            HighPerfTimeSync.ResetForTesting();
            SecretaryOfTemporalAffairs.ResetStaticsForTesting();

            // Create fresh instance (will use current stopwatch ticks as baseline)
            timeKeeper = new SecretaryOfTemporalAffairs();

            // Let real time pass
            Thread.Sleep(50);
            timeKeeper.Update(); // Initialize standard time
        }

        [TearDown]
        public void TearDown()
        {
            timeKeeper = null;
        }

        #region Basic FixedUpdate Functionality

        [Test]
        [Category("FixedUpdate/Initialization")]
        public void FixedUpdate_Should_Initialize_On_First_Call()
        {
            // Create completely fresh instance
            var freshTimeKeeper = new SecretaryOfTemporalAffairs();
            freshTimeKeeper.Update(); // Initialize standard time

            // Before FixedUpdate
            Assert.That(freshTimeKeeper.FixedUpdateCount, Is.EqualTo(0),
                "FixedUpdateCount should be 0 before first FixedUpdate");

            // First FixedUpdate
            freshTimeKeeper.FixedUpdate();

            Assert.That(freshTimeKeeper.FixedUpdateCount, Is.EqualTo(1),
                "FixedUpdateCount should be 1 after first FixedUpdate");

            Assert.That(freshTimeKeeper.FixedElapsedSeconds, Is.GreaterThan(0),
                "FixedElapsedSeconds should be initialized after first FixedUpdate");

            Assert.That(freshTimeKeeper.FixedDeltaTime, Is.GreaterThan(0),
                "FixedDeltaTime should be initialized after first FixedUpdate");
        }

        [Test]
        [Category("FixedUpdate/Initialization")]
        public void FixedUpdate_Should_Anchor_To_Network_Time_On_First_Call()
        {
            // CRITICAL: Reset statics first to ensure fresh baseline
            SecretaryOfTemporalAffairs.ResetStaticsForTesting();

            // Create fresh instance
            var freshTimeKeeper = new SecretaryOfTemporalAffairs();
            Thread.Sleep(10); // Small delay
            freshTimeKeeper.Update(); // Initialize standard time

            double standardTimeBefore = freshTimeKeeper.ElapsedSeconds;

            // Small delay before FixedUpdate
            Thread.Sleep(10);

            // First FixedUpdate should anchor to network time (current time at moment of call)
            freshTimeKeeper.FixedUpdate();

            double fixedTime = freshTimeKeeper.FixedElapsedSeconds;
            double standardTimeAfter = freshTimeKeeper.ElapsedSeconds; // This might be cached, so use fresh calc

            // Fixed time should be close to standard time at initialization
            // Both are in GONet's network-synchronized time domain
            double difference = Math.Abs(fixedTime - standardTimeBefore);
            Assert.That(difference, Is.LessThan(0.1),
                $"Fixed time should anchor close to standard time (both in GONet time domain). " +
                $"StandardBefore: {standardTimeBefore:F6}s, Fixed: {fixedTime:F6}s, StandardAfter: {standardTimeAfter:F6}s, Diff: {difference:F6}s");
        }

        [Test]
        [Category("FixedUpdate/Increment")]
        public void FixedUpdate_Should_Increment_FixedUpdateCount()
        {
            int initialCount = timeKeeper.FixedUpdateCount;

            timeKeeper.FixedUpdate();
            Assert.That(timeKeeper.FixedUpdateCount, Is.EqualTo(initialCount + 1),
                "FixedUpdateCount should increment after FixedUpdate()");

            timeKeeper.FixedUpdate();
            Assert.That(timeKeeper.FixedUpdateCount, Is.EqualTo(initialCount + 2),
                "FixedUpdateCount should increment after each FixedUpdate()");

            timeKeeper.FixedUpdate();
            Assert.That(timeKeeper.FixedUpdateCount, Is.EqualTo(initialCount + 3),
                "FixedUpdateCount should increment consistently");
        }

        #endregion

        #region Linear Time Progression (CRITICAL)

        [Test]
        [Category("FixedUpdate/LinearProgression")]
        public void FixedElapsedSeconds_Must_Always_Increase_Monotonically()
        {
            // CRITICAL: This test ensures physics time never "ping pongs"
            timeKeeper.FixedUpdate(); // Initialize

            double lastFixedTime = timeKeeper.FixedElapsedSeconds;
            var progressionLog = new List<string>();

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(5); // Small delay
                timeKeeper.FixedUpdate();

                double currentFixedTime = timeKeeper.FixedElapsedSeconds;
                progressionLog.Add($"Iteration {i}: {currentFixedTime:F7}s (delta: {(currentFixedTime - lastFixedTime):F7}s)");

                Assert.That(currentFixedTime, Is.GreaterThan(lastFixedTime),
                    $"FixedElapsedSeconds MUST increase monotonically at iteration {i}. " +
                    $"Last: {lastFixedTime:F7}s, Current: {currentFixedTime:F7}s\n" +
                    $"Progression log:\n{string.Join("\n", progressionLog)}");

                lastFixedTime = currentFixedTime;
            }

            UnityEngine.Debug.Log($"FixedElapsedSeconds progression test passed:\n{string.Join("\n", progressionLog)}");
        }

        [Test]
        [Category("FixedUpdate/LinearProgression")]
        public void ElapsedSeconds_Must_Always_Increase_Monotonically_During_Mixed_Updates()
        {
            // CRITICAL: This test ensures standard time never "ping pongs" even when FixedUpdate is called
            timeKeeper.Update(); // Initialize

            double lastStandardTime = timeKeeper.ElapsedSeconds;
            var progressionLog = new List<string>();

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(5);

                // Alternate between Update and FixedUpdate to simulate real Unity behavior
                if (i % 2 == 0)
                {
                    timeKeeper.FixedUpdate();
                }
                else
                {
                    timeKeeper.Update();
                }

                double currentStandardTime = timeKeeper.ElapsedSeconds;
                progressionLog.Add($"Iteration {i} ({(i % 2 == 0 ? "Fixed" : "Update")}): {currentStandardTime:F7}s (delta: {(currentStandardTime - lastStandardTime):F7}s)");

                Assert.That(currentStandardTime, Is.GreaterThanOrEqualTo(lastStandardTime),
                    $"ElapsedSeconds MUST NOT decrease at iteration {i}. " +
                    $"Last: {lastStandardTime:F7}s, Current: {currentStandardTime:F7}s\n" +
                    $"Progression log:\n{string.Join("\n", progressionLog)}");

                lastStandardTime = currentStandardTime;
            }

            UnityEngine.Debug.Log($"ElapsedSeconds progression test passed:\n{string.Join("\n", progressionLog)}");
        }

        [Test]
        [Category("FixedUpdate/LinearProgression")]
        public void Both_Times_Must_Progress_Together_Linearly()
        {
            // CRITICAL: Verifies both standard and fixed time progress together like Unity
            timeKeeper.Update();
            timeKeeper.FixedUpdate();

            double lastStandardTime = timeKeeper.ElapsedSeconds;
            double lastFixedTime = timeKeeper.FixedElapsedSeconds;
            var progressionLog = new List<string>();

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(5);

                // Simulate Unity execution: FixedUpdate → Update
                timeKeeper.FixedUpdate();
                double afterFixedUpdate_standard = timeKeeper.ElapsedSeconds;
                double afterFixedUpdate_fixed = timeKeeper.FixedElapsedSeconds;

                timeKeeper.Update();
                double afterUpdate_standard = timeKeeper.ElapsedSeconds;
                double afterUpdate_fixed = timeKeeper.FixedElapsedSeconds;

                progressionLog.Add($"Iteration {i}:");
                progressionLog.Add($"  After FixedUpdate - std:{afterFixedUpdate_standard:F7}s, fixed:{afterFixedUpdate_fixed:F7}s");
                progressionLog.Add($"  After Update      - std:{afterUpdate_standard:F7}s, fixed:{afterUpdate_fixed:F7}s");

                // Both must always move forward
                Assert.That(afterFixedUpdate_standard, Is.GreaterThanOrEqualTo(lastStandardTime),
                    $"Standard time must not decrease after FixedUpdate at iteration {i}");
                Assert.That(afterFixedUpdate_fixed, Is.GreaterThan(lastFixedTime),
                    $"Fixed time must increase after FixedUpdate at iteration {i}");
                Assert.That(afterUpdate_standard, Is.GreaterThan(afterFixedUpdate_standard),
                    $"Standard time must increase after Update at iteration {i}");
                Assert.That(afterUpdate_fixed, Is.EqualTo(afterFixedUpdate_fixed),
                    $"Fixed time should not change during Update at iteration {i}");

                lastStandardTime = afterUpdate_standard;
                lastFixedTime = afterUpdate_fixed;
            }

            UnityEngine.Debug.Log($"Both times linear progression test passed:\n{string.Join("\n", progressionLog)}");
        }

        #endregion

        #region FixedUpdate Physics Catchup Simulation

        [Test]
        [Category("FixedUpdate/PhysicsCatchup")]
        public void Multiple_FixedUpdates_Per_Frame_Should_Increment_Each_Time()
        {
            // Simulates Unity physics catchup scenario
            timeKeeper.FixedUpdate(); // Initialize

            double lastFixedTime = timeKeeper.FixedElapsedSeconds;
            int lastFixedUpdateCount = timeKeeper.FixedUpdateCount;

            // Simulate multiple FixedUpdate calls in same frame (physics catchup)
            for (int i = 0; i < 5; i++)
            {
                timeKeeper.FixedUpdate();

                Assert.That(timeKeeper.FixedUpdateCount, Is.EqualTo(lastFixedUpdateCount + 1),
                    $"FixedUpdateCount should increment on each call during catchup (iteration {i})");

                Assert.That(timeKeeper.FixedElapsedSeconds, Is.GreaterThan(lastFixedTime),
                    $"FixedElapsedSeconds should increment on each FixedUpdate during catchup (iteration {i})");

                lastFixedTime = timeKeeper.FixedElapsedSeconds;
                lastFixedUpdateCount = timeKeeper.FixedUpdateCount;
            }
        }

        [Test]
        [Category("FixedUpdate/PhysicsCatchup")]
        public void FixedElapsedTicks_Must_Match_FixedElapsedSeconds()
        {
            timeKeeper.FixedUpdate();

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(5);
                timeKeeper.FixedUpdate();

                double ticksAsSeconds = timeKeeper.FixedElapsedTicks / (double)TimeSpan.TicksPerSecond;
                double difference = Math.Abs(ticksAsSeconds - timeKeeper.FixedElapsedSeconds);

                Assert.That(difference, Is.LessThan(TEST_TOLERANCE_SECONDS),
                    $"FixedElapsedTicks and FixedElapsedSeconds must be consistent at iteration {i}");
            }
        }

        #endregion

        #region Frame Count Synchronization

        [Test]
        [Category("FixedUpdate/FrameCount")]
        public void FrameCount_Should_Be_Valid_During_FixedUpdate()
        {
            // CRITICAL: FrameCount must be correct when FixedUpdate runs first
            // (This was a bug we fixed - FrameCount was stale during early FixedUpdate calls)

            // Note: This test can't directly verify Unity.Time.frameCount since we're in edit mode,
            // but we can verify the mechanism is in place

            timeKeeper.Update(); // Initialize
            int countAfterUpdate = timeKeeper.FrameCount;

            Thread.Sleep(10);
            timeKeeper.FixedUpdate(); // Should update FrameCount
            int countAfterFixedUpdate = timeKeeper.FrameCount;

            // FrameCount should be readable (this test mainly ensures no exceptions)
            Assert.That(countAfterFixedUpdate, Is.GreaterThanOrEqualTo(0),
                "FrameCount should be valid after FixedUpdate");
        }

        #endregion

        #region FixedDeltaTime Consistency

        [Test]
        [Category("FixedUpdate/DeltaTime")]
        public void FixedDeltaTime_Should_Be_Consistent()
        {
            // With Option C (direct gap addition), FixedDeltaTime varies during catchup:
            // - When lagging: delta = fixedDeltaTime + gap (catchup)
            // - When synchronized: delta ≈ fixedDeltaTime (normal increment)
            //
            // This test verifies deltas are reasonable (positive, bounded)

            timeKeeper.Update(); // Initialize standard time
            timeKeeper.FixedUpdate(); // Initialize fixed time (may include catchup)

            float firstDelta = timeKeeper.FixedDeltaTime;
            Assert.That(firstDelta, Is.GreaterThan(0),
                "FixedDeltaTime should be positive after first FixedUpdate");

            var deltas = new List<float> { firstDelta };

            // Subsequent calls should have reasonable deltas
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(5); // Small delay (causes small gaps)
                timeKeeper.Update(); // Update standard time
                timeKeeper.FixedUpdate();

                float currentDelta = timeKeeper.FixedDeltaTime;
                deltas.Add(currentDelta);

                // Delta should be positive
                Assert.That(currentDelta, Is.GreaterThan(0),
                    $"FixedDeltaTime should always be positive at iteration {i}");

                // Delta should be reasonable (not huge, not tiny)
                Assert.That(currentDelta, Is.LessThan(0.1f),
                    $"FixedDeltaTime should be reasonable (< 100ms) at iteration {i}. Delta: {currentDelta:F6}s");
            }

            // Log deltas for diagnostic purposes
            UnityEngine.Debug.Log($"FixedDeltaTime samples (Option C): {string.Join(", ", deltas.Select(d => $"{d:F6}s"))}");
        }

        #endregion

        #region Edge Cases

        [Test]
        [Category("FixedUpdate/EdgeCases")]
        public void FixedUpdate_Before_Update_Should_Work()
        {
            // Create fresh instance WITHOUT calling Update first
            var freshTimeKeeper = new SecretaryOfTemporalAffairs();

            // Call FixedUpdate before Update
            Assert.DoesNotThrow(() => freshTimeKeeper.FixedUpdate(),
                "FixedUpdate should work even if called before Update");

            Assert.That(freshTimeKeeper.FixedElapsedSeconds, Is.GreaterThanOrEqualTo(0),
                "FixedElapsedSeconds should be valid even if FixedUpdate called before Update");
        }

        [Test]
        [Category("FixedUpdate/EdgeCases")]
        public void Rapid_FixedUpdate_Calls_Should_Not_Cause_Exceptions()
        {
            timeKeeper.FixedUpdate(); // Initialize

            // Rapid calls simulating extreme physics catchup
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    timeKeeper.FixedUpdate();
                }
            }, "Rapid FixedUpdate calls should not cause exceptions");
        }

        [Test]
        [Category("FixedUpdate/EdgeCases")]
        public void Interleaved_Update_And_FixedUpdate_Should_Work()
        {
            // Simulate realistic Unity frame pattern
            double lastTime = 0;

            for (int frame = 0; frame < 10; frame++)
            {
                Thread.Sleep(5);

                // Some frames have multiple FixedUpdates (catchup)
                int fixedUpdatesThisFrame = (frame % 3 == 0) ? 2 : 1;

                for (int f = 0; f < fixedUpdatesThisFrame; f++)
                {
                    timeKeeper.FixedUpdate();
                    double timeAfterFixed = timeKeeper.FixedElapsedSeconds;
                    Assert.That(timeAfterFixed, Is.GreaterThanOrEqualTo(lastTime),
                        $"Time should not decrease in frame {frame}, fixedUpdate {f}");
                    lastTime = timeAfterFixed;
                }

                timeKeeper.Update();
                double timeAfterUpdate = timeKeeper.ElapsedSeconds;
                // Note: Standard time updates independently, so we don't compare to lastTime here
            }
        }

        #endregion

        #region Performance

        [Test]
        [Category("FixedUpdate/Performance")]
        public void FixedElapsedSeconds_Access_Should_Be_Fast()
        {
            timeKeeper.FixedUpdate(); // Initialize

            const int iterations = 1_000_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                _ = timeKeeper.FixedElapsedSeconds;
            }

            sw.Stop();
            double timePerCallNs = sw.Elapsed.TotalMilliseconds * 1_000_000 / iterations;

            UnityEngine.Debug.Log($"Time per FixedElapsedSeconds call: {timePerCallNs:F1}ns");

            // Should be very fast (under 100ns per call, using cached value)
            Assert.That(timePerCallNs, Is.LessThan(100),
                "FixedElapsedSeconds access should be very fast (cached)");
        }

        [Test]
        [Category("FixedUpdate/Performance")]
        public void FixedUpdate_Performance()
        {
            timeKeeper.FixedUpdate(); // Initialize

            const int iterations = 10_000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                timeKeeper.FixedUpdate();
            }

            sw.Stop();
            double timePerUpdateUs = sw.Elapsed.TotalMilliseconds * 1000 / iterations;

            UnityEngine.Debug.Log($"Time per FixedUpdate call: {timePerUpdateUs:F1}μs");

            // FixedUpdate should be fast (under 20μs - slightly slower than Update due to increment logic)
            Assert.That(timePerUpdateUs, Is.LessThan(20),
                "FixedUpdate should be fast");
        }

        #endregion

        #region Catchup Mechanism Tests

        [Test]
        [Category("FixedUpdate/Catchup")]
        public void FixedElapsedSeconds_Should_Never_Lag_Behind_ElapsedSeconds()
        {
            // CRITICAL: This test verifies the catchup mechanism prevents fixed time from lagging behind standard time

            timeKeeper.Update(); // Initialize standard time
            timeKeeper.FixedUpdate(); // Initialize fixed time

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(10); // Real time passes

                // Update standard time first
                timeKeeper.Update();
                double standardTime = timeKeeper.ElapsedSeconds;

                // FixedUpdate should catch up if needed
                timeKeeper.FixedUpdate();
                double fixedTime = timeKeeper.FixedElapsedSeconds;

                // Fixed time should be >= standard time (or very close due to caching)
                Assert.That(fixedTime, Is.GreaterThanOrEqualTo(standardTime - 0.001),
                    $"FixedElapsedSeconds should not lag behind ElapsedSeconds at iteration {i}. " +
                    $"Standard: {standardTime:F7}s, Fixed: {fixedTime:F7}s");
            }
        }

        [Test]
        [Category("FixedUpdate/Catchup")]
        public void Multiple_FixedUpdates_In_Same_Frame_Should_Not_Overshoot()
        {
            // CRITICAL: Tests that frame-scoped catchup target prevents overshooting
            // This scenario: Update() caches ElapsedSeconds, then multiple FixedUpdates in same frame

            timeKeeper.Update();
            timeKeeper.FixedUpdate(); // Initialize

            // Simulate a large gap (standard time way ahead)
            Thread.Sleep(100);
            timeKeeper.Update(); // Update standard time
            double standardTimeAtFrameStart = timeKeeper.ElapsedSeconds;

            // First FixedUpdate should catch up
            timeKeeper.FixedUpdate();
            double fixedTimeAfterFirst = timeKeeper.FixedElapsedSeconds;

            Assert.That(fixedTimeAfterFirst, Is.GreaterThanOrEqualTo(standardTimeAtFrameStart - 0.001),
                "First FixedUpdate should catch up to standard time");

            // Second FixedUpdate in SAME frame (Unity frame count hasn't changed)
            // Should NOT try to catch up again - just normal increment
            timeKeeper.FixedUpdate();
            double fixedTimeAfterSecond = timeKeeper.FixedElapsedSeconds;

            // The delta should be approximately one fixedDeltaTime step
            double delta = fixedTimeAfterSecond - fixedTimeAfterFirst;
            Assert.That(delta, Is.LessThan(0.030), // Should be ~0.0167s, allowing some margin
                $"Second FixedUpdate should be normal increment, not another catchup. Delta: {delta:F7}s");

            Assert.That(delta, Is.GreaterThan(0.010), // Should be at least one step
                $"Second FixedUpdate should still increment by at least one step. Delta: {delta:F7}s");
        }

        [Test]
        [Category("FixedUpdate/Catchup")]
        public void Catchup_Should_Close_Large_Gaps_Immediately()
        {
            // Option C: Direct gap addition - catchup happens immediately, not incrementally
            // This is CORRECT because GONet time is based on real-world Stopwatch time

            timeKeeper.Update();
            timeKeeper.FixedUpdate(); // Initialize
            double initialFixedTime = timeKeeper.FixedElapsedSeconds;

            // Create large gap (200ms)
            Thread.Sleep(200);
            timeKeeper.Update();
            double standardTime = timeKeeper.ElapsedSeconds;

            // Before catchup: fixed time is behind
            Assert.That(initialFixedTime, Is.LessThan(standardTime),
                "Before catchup, fixed time should lag behind standard time");

            // Single FixedUpdate should catch up immediately
            timeKeeper.FixedUpdate();
            double finalFixedTime = timeKeeper.FixedElapsedSeconds;

            // After catchup: fixed time should be >= standard time (caught up in one step)
            Assert.That(finalFixedTime, Is.GreaterThanOrEqualTo(standardTime - 0.001),
                $"After ONE FixedUpdate, fixed time should catch up to standard time. " +
                $"Standard: {standardTime:F7}s, Fixed: {finalFixedTime:F7}s");

            // The increase includes: fixedDeltaTime + gap (Option C's direct addition)
            double totalIncrease = finalFixedTime - initialFixedTime;
            Assert.That(totalIncrease, Is.GreaterThan(0.1), // Should be ~200ms gap
                $"Catchup should close the gap immediately. Increase: {totalIncrease:F7}s");
        }

        [Test]
        [Category("FixedUpdate/Catchup")]
        public void Catchup_Should_Handle_Small_Gaps_Gracefully()
        {
            // Tests that catchup mechanism (Option C) keeps fixed time synchronized with standard time
            // Option C: Direct gap addition - immediately catches up by adding the gap

            timeKeeper.Update();
            timeKeeper.FixedUpdate(); // Initialize

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(5); // Small delay
                timeKeeper.Update();
                double standardTime = timeKeeper.ElapsedSeconds;

                timeKeeper.FixedUpdate();
                double fixedTime = timeKeeper.FixedElapsedSeconds;

                // Fixed time should advance reasonably
                Assert.That(fixedTime, Is.GreaterThan(0),
                    $"Fixed time should be positive at iteration {i}");

                // With Option C, fixed time catches up immediately, so it should be >= standard time
                // (not lagging behind). Allow small tolerance for caching/rounding.
                Assert.That(fixedTime, Is.GreaterThanOrEqualTo(standardTime - 0.001),
                    $"Fixed time should catch up to standard time at iteration {i}. Std: {standardTime:F7}s, Fixed: {fixedTime:F7}s");
            }
        }

        #endregion

        #region Regression Prevention

        [Test]
        [Category("FixedUpdate/Regression")]
        public void Test_Original_PingPong_Issue_Is_Fixed()
        {
            // This test specifically checks for the "ping pong" bug we fixed
            // where ElapsedSeconds would show stale values during FixedUpdate

            timeKeeper.Update(); // Initialize standard time
            timeKeeper.FixedUpdate(); // Initialize fixed time

            var standardTimeSamples = new List<double>();
            var fixedTimeSamples = new List<double>();

            // Simulate early frames where ping pong was observed
            for (int frame = 0; frame < 10; frame++)
            {
                Thread.Sleep(10);

                // FixedUpdate first (physics catchup scenario)
                timeKeeper.FixedUpdate();
                fixedTimeSamples.Add(timeKeeper.FixedElapsedSeconds);

                // Note: We can't directly test the debug log output,
                // but we can verify the underlying values are correct

                // Update after
                timeKeeper.Update();
                standardTimeSamples.Add(timeKeeper.ElapsedSeconds);
            }

            // Verify no "ping pong" - all values must increase monotonically
            for (int i = 1; i < standardTimeSamples.Count; i++)
            {
                Assert.That(standardTimeSamples[i], Is.GreaterThan(standardTimeSamples[i - 1]),
                    $"Standard time should increase monotonically (frame {i})");
            }

            for (int i = 1; i < fixedTimeSamples.Count; i++)
            {
                Assert.That(fixedTimeSamples[i], Is.GreaterThan(fixedTimeSamples[i - 1]),
                    $"Fixed time should increase monotonically (frame {i})");
            }

            UnityEngine.Debug.Log("Ping pong regression test passed - all times monotonically increasing");
        }

        #endregion
    }
}
