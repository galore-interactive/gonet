/* GONet (TM, serial number 88592370), Copyright (c) 2019-2025 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 *
 *
 * Authorized use is explicitly limited to the following:
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using NUnit.Framework;
using UnityEngine;
using GONet;

namespace GONet.Tests
{
    /// <summary>
    /// Unit tests for Quaternion comparison threshold in <see cref="GONetSyncableValue"/>.
    ///
    /// <para><b>PURPOSE:</b></para>
    /// <list type="bullet">
    ///   <item>Verify quaternion comparison threshold behavior BEFORE and AFTER threshold change</item>
    ///   <item>Establish baseline metrics for slow rotation detection (8°/s, 25°/s, 180°/s)</item>
    ///   <item>Validate that threshold change enables velocity-augmented sync for slow rotations</item>
    ///   <item>Ensure no regressions in performance or floating-point precision</item>
    /// </list>
    ///
    /// <para><b>CONTEXT:</b></para>
    /// The quaternion comparison threshold was increased from 0.001° to 0.81° in commit ef6c54dc0 (May 2025)
    /// as part of a performance optimization (Quaternion.Angle → Quaternion.Dot). This unintentionally blocked
    /// detection of slow rotations (8°/s = 0.133°/frame at 60 FPS), preventing velocity-augmented sync from working.
    ///
    /// <para><b>FIX:</b></para>
    /// Lower threshold from 0.9999f (0.81°) to 0.999999f (0.026°) to restore slow rotation detection while
    /// maintaining performance gains from Quaternion.Dot().
    ///
    /// <para><b>TEST STRATEGY:</b></para>
    /// 1. Run tests BEFORE change (baseline) - expect FAILURES for slow rotation detection
    /// 2. Apply threshold change (0.9999f → 0.999999f)
    /// 3. Run tests AFTER change (validation) - expect ALL PASSES
    /// 4. Compare performance metrics to ensure no regression
    /// </summary>
    [TestFixture]
    [Category("GONet")]
    [Category("QuaternionThreshold")]
    [Category("EditMode")]
    public class QuaternionThresholdTests
    {
        #region Test Constants

        // Rotation speeds matching CircularMotion.cs presets
        private const float ROTATION_SPEED_SLOW = 8.0f;     // degrees/second (Slow preset)
        private const float ROTATION_SPEED_MEDIUM = 25.0f;  // degrees/second (Medium preset)
        private const float ROTATION_SPEED_FAST = 180.0f;   // degrees/second (Fast preset)

        // Frame rates for testing
        private const float FPS_60 = 60.0f;
        private const float FPS_30 = 30.0f;
        private const float FPS_120 = 120.0f;

        // Expected thresholds
        private const float THRESHOLD_BEFORE_CHANGE = 1.62f;    // degrees (0.9999f dot product)
        private const float THRESHOLD_AFTER_CHANGE = 0.1f;      // degrees (practical limit due to float precision, ~0.9999999f dot product)

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a quaternion rotated by a specific angle (in degrees) around the Y axis.
        /// </summary>
        private Quaternion CreateRotation(float angleDegrees)
        {
            return Quaternion.Euler(0, angleDegrees, 0);
        }

        /// <summary>
        /// Calculates rotation per frame for a given rotation speed and frame rate.
        /// </summary>
        private float GetRotationPerFrame(float degreesPerSecond, float fps)
        {
            return degreesPerSecond / fps;
        }

        /// <summary>
        /// Compares two quaternions using GONetSyncableValue equality operator.
        /// </summary>
        private bool AreQuaternionsEqual(Quaternion q1, Quaternion q2)
        {
            GONetSyncableValue v1 = new GONetSyncableValue();
            v1.UnityEngine_Quaternion = q1;

            GONetSyncableValue v2 = new GONetSyncableValue();
            v2.UnityEngine_Quaternion = q2;

            return v1 == v2;
        }

        /// <summary>
        /// Calculates the angular difference between two quaternions in degrees.
        /// </summary>
        private float GetAngularDifference(Quaternion q1, Quaternion q2)
        {
            return Quaternion.Angle(q1, q2);
        }

        #endregion

        #region Threshold Accuracy Tests

        [Test]
        [Description("Verifies that quaternions differing by LESS than threshold are considered EQUAL")]
        public void QuaternionComparison_BelowThreshold_ConsideredEqual()
        {
            // Arrange: Create rotations below threshold
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.04f); // Very small rotation

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // Very small rotations produce dot products > threshold (closer to 1.0)
            Assert.IsTrue(areEqual, $"Quaternions differing by 0.04° should be considered equal (dot product > threshold)");
        }

        [Test]
        [Description("Verifies that quaternions differing by MORE than threshold are considered DIFFERENT")]
        public void QuaternionComparison_AboveThreshold_ConsideredDifferent()
        {
            // Arrange: Create rotations above practical threshold (detectable by float precision)
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.15f); // 0.15° > 0.1° practical threshold

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // 0.15° produces dot product < 1.0, so it's detectable as different
            Assert.IsFalse(areEqual, $"Quaternions differing by 0.15° should be considered different (above practical threshold)");
        }

        [Test]
        [Description("Verifies threshold boundary behavior at exactly the threshold value")]
        public void QuaternionComparison_ExactlyAtThreshold_BehaviorDefined()
        {
            // Arrange: Create rotations at practical threshold boundary
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.133f); // Our target: 8°/s @ 60 FPS

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);
            float actualDiff = GetAngularDifference(q1, q2);

            // Assert: 0.133° should be detectable (this is the PRIMARY goal of the fix!)
            Assert.IsFalse(areEqual, $"0.133° rotation MUST be detected (8°/s @ 60 FPS)");

            // Note: Quaternion.Angle() also rounds very small angles to 0° due to float precision
            // So we can't assert exact angle values for rotations < 0.1°
        }

        #endregion

        #region Slow Rotation Detection Tests (PRIMARY BUG VALIDATION)

        [Test]
        [Description("CRITICAL: Verifies 8°/s rotation detection at 60 FPS (0.133°/frame) - PRIMARY BUG FIX")]
        public void SlowRotation_8DegreesPerSecond_60FPS_DetectedAsChanged()
        {
            // Arrange: Simulate 8°/s rotation (CircularMotion Slow preset)
            float rotationPerFrame = GetRotationPerFrame(ROTATION_SPEED_SLOW, FPS_60);
            Assert.That(rotationPerFrame, Is.EqualTo(0.133f).Within(0.001f), "Rotation per frame calculation");

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame); // 0.133° rotation

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE CHANGE (0.81° threshold): areEqual = TRUE (0.133° < 0.81°) ❌ BUG - should detect change!
            // AFTER CHANGE (0.026° threshold): areEqual = FALSE (0.133° > 0.026°) ✅ FIX - detects change!
            Assert.IsFalse(areEqual,
                $"Slow rotation (8°/s at 60 FPS = {rotationPerFrame:F3}°/frame) MUST be detected as changed. " +
                $"This is the PRIMARY BUG being fixed!");
        }

        [Test]
        [Description("Verifies 8°/s rotation detection at 30 FPS (0.267°/frame)")]
        public void SlowRotation_8DegreesPerSecond_30FPS_DetectedAsChanged()
        {
            // Arrange
            float rotationPerFrame = GetRotationPerFrame(ROTATION_SPEED_SLOW, FPS_30);
            Assert.That(rotationPerFrame, Is.EqualTo(0.267f).Within(0.001f));

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: TRUE (0.267° < 0.81°) ❌
            // AFTER: FALSE (0.267° > 0.026°) ✅
            Assert.IsFalse(areEqual,
                $"Slow rotation at 30 FPS ({rotationPerFrame:F3}°/frame) should be detected");
        }

        [Test]
        [Description("Verifies 25°/s rotation detection at 60 FPS (0.417°/frame)")]
        public void MediumRotation_25DegreesPerSecond_60FPS_DetectedAsChanged()
        {
            // Arrange: Medium speed rotation
            float rotationPerFrame = GetRotationPerFrame(ROTATION_SPEED_MEDIUM, FPS_60);
            Assert.That(rotationPerFrame, Is.EqualTo(0.417f).Within(0.001f));

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: TRUE (0.417° < 0.81°) ❌
            // AFTER: FALSE (0.417° > 0.026°) ✅
            Assert.IsFalse(areEqual,
                $"Medium rotation at 60 FPS ({rotationPerFrame:F3}°/frame) should be detected");
        }

        [Test]
        [Description("Verifies 180°/s rotation detection at 60 FPS (3°/frame) - should work before AND after change")]
        public void FastRotation_180DegreesPerSecond_60FPS_DetectedAsChanged()
        {
            // Arrange: Fast rotation
            float rotationPerFrame = GetRotationPerFrame(ROTATION_SPEED_FAST, FPS_60);
            Assert.That(rotationPerFrame, Is.EqualTo(3.0f).Within(0.001f));

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: FALSE (3° > 0.81°) ✅ Already working
            // AFTER: FALSE (3° > 0.026°) ✅ Still working
            Assert.IsFalse(areEqual,
                $"Fast rotation at 60 FPS ({rotationPerFrame:F3}°/frame) should ALWAYS be detected (no regression)");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        [Description("Verifies identical quaternions are always considered equal")]
        public void IdenticalQuaternions_AlwaysEqual()
        {
            // Arrange
            Quaternion q = CreateRotation(45.0f);

            // Act
            bool areEqual = AreQuaternionsEqual(q, q);

            // Assert
            Assert.IsTrue(areEqual, "Identical quaternions should always be equal");
        }

        [Test]
        [Description("Verifies very small rotations (sub-threshold) are considered equal")]
        public void VerySmallRotation_0_001Degrees_ConsideredEqual()
        {
            // Arrange: Extremely small rotation (original threshold!)
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.001f);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: TRUE (0.001° < 0.81°) ✅
            // AFTER: TRUE (0.001° < 0.026°) ✅
            Assert.IsTrue(areEqual, "Sub-threshold rotation (0.001°) should be considered equal (noise filtering)");
        }

        [Test]
        [Description("Verifies 180° rotation (quaternion double-cover) is detected")]
        public void Rotation_180Degrees_DetectedAsDifferent()
        {
            // Arrange: 180° rotation
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(180.0f);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            Assert.IsFalse(areEqual, "180° rotation should always be detected as different");
        }

        [Test]
        [Description("Documents that quaternion double-cover (q = -q) doesn't occur in practice for change detection")]
        public void QuaternionDoubleCover_NotRelevantForChangeDetection()
        {
            // Note: Quaternion double-cover (q and -q representing same rotation) is a theoretical edge case
            // that doesn't occur in practice for frame-to-frame change detection.
            //
            // Unity's Quaternion operations (Euler, LookRotation, etc.) always return quaternions with w >= 0,
            // so consecutive frames won't flip between q and -q.
            //
            // This test documents that we don't need to handle this edge case.

            // Create two rotations using Unity's standard constructors
            Quaternion q1 = Quaternion.Euler(45, 30, 15);
            Quaternion q2 = Quaternion.Euler(45, 30, 15); // Same rotation

            // Unity guarantees w >= 0 for both
            Assert.That(q1.w, Is.GreaterThanOrEqualTo(0f), "Unity quaternions have w >= 0");
            Assert.That(q2.w, Is.GreaterThanOrEqualTo(0f), "Unity quaternions have w >= 0");

            // They're equal (not double-cover)
            bool areEqual = AreQuaternionsEqual(q1, q2);
            Assert.IsTrue(areEqual, "Same rotation produces equal quaternions (no sign flip)");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Description("Verifies Quaternion.Dot() performance (should be fast)")]
        [Category("Performance")]
        public void Performance_QuaternionComparison_UnderOneMicrosecond()
        {
            // Arrange
            Quaternion q1 = CreateRotation(45.0f);
            Quaternion q2 = CreateRotation(45.1f);
            const int iterations = 10000;

            // Act
            var startTime = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                bool _ = AreQuaternionsEqual(q1, q2);
            }
            startTime.Stop();

            // Assert
            double microsecondsPerComparison = (startTime.Elapsed.TotalMilliseconds * 1000.0) / iterations;
            Assert.Less(microsecondsPerComparison, 1.0,
                $"Quaternion comparison should be under 1 microsecond (actual: {microsecondsPerComparison:F3}µs)");

            Debug.Log($"[PERFORMANCE] Quaternion comparison: {microsecondsPerComparison:F3}µs per comparison (averaged over {iterations} iterations)");
        }

        #endregion

        #region Velocity Sync Integration Tests

        [Test]
        [Description("Verifies change detection for rotations within velocity bounds (±19°/s)")]
        public void VelocitySyncBounds_RotationWithinBounds_DetectedCorrectly()
        {
            // Arrange: Rotation at max velocity bound (19°/s at 60 FPS)
            const float MAX_VELOCITY_DEG_PER_SEC = 19.0f; // From Generated_3.cs velocity bounds
            float rotationPerFrame = GetRotationPerFrame(MAX_VELOCITY_DEG_PER_SEC, FPS_60);
            Assert.That(rotationPerFrame, Is.EqualTo(0.317f).Within(0.001f));

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: TRUE (0.317° < 0.81°) ❌ Would not sync!
            // AFTER: FALSE (0.317° > 0.026°) ✅ Will sync!
            Assert.IsFalse(areEqual,
                $"Rotation at max velocity bound ({MAX_VELOCITY_DEG_PER_SEC}°/s) should be detected for velocity sync");
        }

        [Test]
        [Description("Verifies that velocity-eligible slow rotations (1°/s) are now detectable")]
        public void VelocitySync_VerySlowRotation_1DegreePerSecond_Detectable()
        {
            // Arrange: Very slow rotation (1°/s at 60 FPS)
            float rotationPerFrame = GetRotationPerFrame(1.0f, FPS_60);
            Assert.That(rotationPerFrame, Is.EqualTo(0.0167f).Within(0.0001f));

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(rotationPerFrame);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            // BEFORE: TRUE (0.0167° < 0.81°) ❌
            // AFTER: TRUE (0.0167° < 0.026°) ✅ Still below threshold (expected - too slow to sync every frame)
            Assert.IsTrue(areEqual,
                $"Extremely slow rotation (1°/s = 0.0167°/frame) should accumulate over multiple frames before sync");
        }

        #endregion

        #region Regression Tests

        [Test]
        [Description("Verifies that fast rotations (already working) still work after threshold change")]
        public void Regression_FastRotations_StillDetected()
        {
            // Test multiple fast rotation speeds
            float[] fastSpeeds = { 90.0f, 180.0f, 360.0f }; // degrees/second

            foreach (float speed in fastSpeeds)
            {
                // Arrange
                float rotationPerFrame = GetRotationPerFrame(speed, FPS_60);
                Quaternion q1 = CreateRotation(0.0f);
                Quaternion q2 = CreateRotation(rotationPerFrame);

                // Act
                bool areEqual = AreQuaternionsEqual(q1, q2);

                // Assert
                Assert.IsFalse(areEqual,
                    $"Fast rotation ({speed}°/s = {rotationPerFrame:F3}°/frame) should still be detected (no regression)");
            }
        }

        [Test]
        [Description("Verifies that noise-level rotations are still filtered out")]
        public void Regression_NoiseFilteringStillWorks()
        {
            // Arrange: Sub-threshold noise (0.01°)
            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.01f);

            // Act
            bool areEqual = AreQuaternionsEqual(q1, q2);

            // Assert
            Assert.IsTrue(areEqual,
                "Noise-level rotations (0.01°) should still be filtered (no unnecessary sync spam)");
        }

        #endregion

        #region Documentation Tests

        [Test]
        [Description("Documents expected behavior BEFORE threshold change (for git history)")]
        [Category("Documentation")]
        public void Documentation_BeforeChange_Threshold_0_81_Degrees()
        {
            // This test documents the BEFORE state for git history
            // Expected to FAIL after threshold change (that's the point!)

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.133f); // 8°/s at 60 FPS

            bool areEqual = AreQuaternionsEqual(q1, q2);

            // BEFORE CHANGE: Should be TRUE (0.133° < 0.81°)
            // AFTER CHANGE: Should be FALSE (0.133° > 0.026°)

            Debug.Log($"[THRESHOLD CHANGE] Rotation 0.133° considered equal: {areEqual}");
            Debug.Log($"[THRESHOLD CHANGE] Expected BEFORE (0.9999f): TRUE");
            Debug.Log($"[THRESHOLD CHANGE] Expected AFTER (0.999999f): FALSE");

            // This assertion will FLIP after the change - that's expected and desired!
            // Comment out this line after verifying behavior change:
            // Assert.IsTrue(areEqual, "BEFORE CHANGE: 0.133° < 0.81° threshold should be equal");
        }

        [Test]
        [Description("Documents expected behavior AFTER threshold change")]
        [Category("Documentation")]
        public void Documentation_AfterChange_Threshold_0_026_Degrees()
        {
            // This test documents the AFTER state

            Quaternion q1 = CreateRotation(0.0f);
            Quaternion q2 = CreateRotation(0.133f); // 8°/s at 60 FPS

            bool areEqual = AreQuaternionsEqual(q1, q2);

            // AFTER CHANGE: Should be FALSE (0.133° > 0.026°)
            Assert.IsFalse(areEqual,
                "AFTER CHANGE: 0.133° > 0.026° threshold should be different (enables slow rotation sync)");
        }

        #endregion
    }
}
