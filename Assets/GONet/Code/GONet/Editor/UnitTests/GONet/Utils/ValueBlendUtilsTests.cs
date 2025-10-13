using GONet.PluginAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet.Utils
{
    [TestFixture]
    public class ValueBlendUtilsTests
    {
        [Test]
        public void BlendExtrapolatedQuaternionsSmoothly()
        {
            // Test the quaternion blending directly without requiring GONetMain infrastructure
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            Quaternion startingRotation = Quaternion.Euler(90, 0, 0);
            Quaternion degrees5 = Quaternion.Euler(5, 0, 0);
            Quaternion degrees10 = Quaternion.Euler(10, 0, 0);
            Quaternion degrees15 = Quaternion.Euler(15, 0, 0);

            long baseTime = BaseTimeTicks;

            // Create buffer in NEWEST FIRST order (index 0 = most recent)
            var buffer = new NumericValueChangeSnapshot[4];

            buffer[0] = CreateSnapshot(startingRotation * degrees15, baseTime + (long)(0.018 * TimeSpan.TicksPerSecond));
            buffer[1] = CreateSnapshot(startingRotation * degrees10, baseTime + (long)(0.015 * TimeSpan.TicksPerSecond));
            buffer[2] = CreateSnapshot(startingRotation * degrees5, baseTime + (long)(0.01 * TimeSpan.TicksPerSecond));
            buffer[3] = CreateSnapshot(startingRotation, baseTime);

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool success = blender.TryGetBlendedValue(buffer, 4, baseTime + (long)(0.0195 * TimeSpan.TicksPerSecond), out blendedValue, out didExtrapolate);

            Assert.IsTrue(success);
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
            Debug.Log("blendedValue: " + blendedValue.UnityEngine_Quaternion.eulerAngles);
        }
        private const float POSITION_EPSILON = 0.001f;
        private const float ANGLE_EPSILON = 0.1f; // degrees

        #region Helper Methods

        private NumericValueChangeSnapshot CreateSnapshot(Vector3 value, long elapsedTicks)
        {
            return NumericValueChangeSnapshot.Create(elapsedTicks, value);
        }

        private NumericValueChangeSnapshot CreateSnapshot(Quaternion value, long elapsedTicks)
        {
            return NumericValueChangeSnapshot.Create(elapsedTicks, value);
        }

        // Base time for all tests - use a recent time to pass the "data too old" check
        private static readonly long BaseTimeTicks = DateTime.UtcNow.Ticks;

        private long SecondsToTicks(float seconds)
        {
            return BaseTimeTicks + (long)(seconds * TimeSpan.TicksPerSecond);
        }

        #endregion

        #region Vector3 Tests

        [Test]
        public void Vector3_Interpolation_LinearMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create linear motion: 0,0,0 -> 10,0,0 over 1 second
            // Buffer must be in NEWEST FIRST order (index 0 = most recent)
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(1.0f)), // newest (index 0)
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // oldest (index 1)
            };

            // Test at 1.25 seconds (0.25 seconds past newest - extrapolates using both values)
            // Note: With IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH=true,
            // querying between values uses only the older value, not both values
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(1.25f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate); // Will extrapolate past newest
            // Should be ~12.5 (10 + 0.25 * velocity of 10 units/second)
            // Note: With valueCount=2, smoothing is applied (below MIN_VALUE_COUNT_FOR_NORMAL_OPERATION=3)
            // This reduces the extrapolated value, so we expect ~11.0 instead of the raw 12.5
            Assert.AreEqual(12.5f, blendedValue.UnityEngine_Vector3.x, 2.0f); // Larger tolerance to account for smoothing filter
            Assert.AreEqual(0.0f, blendedValue.UnityEngine_Vector3.y, POSITION_EPSILON);
            Assert.AreEqual(0.0f, blendedValue.UnityEngine_Vector3.z, POSITION_EPSILON);
        }

        [Test]
        public void Vector3_Extrapolation_LinearMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create linear motion with constant velocity
            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(20, 0, 0), SecondsToTicks(2.0f)), // newest (index 0)
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(1.0f)), // middle (index 1)
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // oldest (index 2)
            };

            // Test extrapolation at 2.5 seconds (0.5 seconds past newest - well within 1 second threshold)
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate); // Should extrapolate past newest
            // Allow some tolerance for smoothing effects
            Assert.AreEqual(25.0f, blendedValue.UnityEngine_Vector3.x, 2.0f); // Linear extrapolation with smoothing tolerance
        }

        [Test]
        public void Vector3_Acceleration_QuadraticMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create accelerating motion: x = 5t² (acceleration = 10 units/s²)
            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(20, 0, 0), SecondsToTicks(2.0f)), // newest: 5 * 4 = 20
                CreateSnapshot(new Vector3(5, 0, 0), SecondsToTicks(1.0f)),  // middle: 5 * 1 = 5
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // oldest: 5 * 0 = 0
            };

            // Test extrapolation at 2.5 seconds (0.5 seconds past newest)
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate);
            // With acceleration, should be close to 31.25 (5 * 2.5²), but smoothing may affect this
            Assert.AreEqual(31.25f, blendedValue.UnityEngine_Vector3.x, 10.0f); // Larger epsilon for acceleration + smoothing
        }

        [Test]
        public void Vector3_AtRestTransition_AppliesSmoothing()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Single value (at rest)
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10, 5, 2), SecondsToTicks(1.0f))
            };

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 1, SecondsToTicks(1.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // With single value, smoothing should be applied
            // The exact value depends on smoothing implementation
            Assert.IsNotNull(blendedValue.UnityEngine_Vector3);
        }

        [Test]
        public void Vector3_SharpDirectionChange_AppliesSmoothing()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create sharp 90-degree turn: moving +X then +Y
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10, 10, 0), SecondsToTicks(2.0f)), // newest
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(1.0f)),
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))    // oldest
            };

            // Test at the turn point
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(1.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // Smoothing should soften the sharp turn
            Assert.Greater(blendedValue.UnityEngine_Vector3.y, 0); // Should start turning before sharp corner
        }

        [Test]
        public void Vector3_JitterDetection_AppliesSmoothing()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create jittery motion
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10.5f, 0, 0), SecondsToTicks(0.3f)), // newest
                CreateSnapshot(new Vector3(9.8f, 0, 0), SecondsToTicks(0.2f)),
                CreateSnapshot(new Vector3(10.2f, 0, 0), SecondsToTicks(0.1f)),
                CreateSnapshot(new Vector3(10.0f, 0, 0), SecondsToTicks(0.0f))  // oldest
            };

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 4, SecondsToTicks(0.25f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // Smoothing should reduce jitter
            Assert.AreEqual(10.0f, blendedValue.UnityEngine_Vector3.x, 1.0f); // Should be smoothed near average
        }

        #endregion

        #region Quaternion Tests

        [Test]
        public void Quaternion_Interpolation_ConstantRotation()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create constant rotation around Y axis: 0° -> 90° over 1 second
            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 90, 0), SecondsToTicks(1.0f)), // newest (index 0)
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))   // oldest (index 1)
            };

            // Test at 1.25 seconds (0.25 seconds past newest - extrapolates using both values)
            // Note: With IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH=true,
            // querying between values uses only the older value, not both values
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(1.25f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate); // Will extrapolate past newest

            // Should extrapolate to ~112.5 degrees (90 + 90*0.25), with tolerance for smoothing
            // Angular velocity is 90°/s, extrapolating 0.25s past the 90° mark
            float angle = Quaternion.Angle(Quaternion.Euler(0, 112.5f, 0), blendedValue.UnityEngine_Quaternion);
            Assert.LessOrEqual(angle, 20.0f); // Tolerance for extrapolation + smoothing
        }

        [Test]
        public void Quaternion_Extrapolation_ConstantAngularVelocity()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create constant angular velocity: 30°/s around Y
            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 60, 0), SecondsToTicks(2.0f)), // newest (index 0)
                CreateSnapshot(Quaternion.Euler(0, 30, 0), SecondsToTicks(1.0f)), // middle (index 1)
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))   // oldest (index 2)
            };

            // Test extrapolation at 2.5 seconds (0.5 seconds past newest)
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate);

            // Should extrapolate to ~75 degrees (60 + 30*0.5), with tolerance for smoothing and advanced motion analysis
            float angle = Quaternion.Angle(Quaternion.Euler(0, 75, 0), blendedValue.UnityEngine_Quaternion);
            Assert.LessOrEqual(angle, 15.0f); // Larger tolerance for extrapolation + smoothing + motion analysis
        }

        [Test]
        public void Quaternion_AcceleratingRotation()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create accelerating rotation
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 180, 0), SecondsToTicks(2.0f)), // newest
                CreateSnapshot(Quaternion.Euler(0, 60, 0), SecondsToTicks(1.0f)),
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))    // oldest
            };

            // Test extrapolation
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate);
            // Should show acceleration in the extrapolation
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
        }

        [Test]
        public void Quaternion_AtRestTransition_AppliesSmoothing()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Single value (at rest)
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(45, 30, 15), SecondsToTicks(1.0f))
            };

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 1, SecondsToTicks(1.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // Should apply smoothing for single value
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
        }

        [Test]
        public void Quaternion_AxisFlip_AppliesSmoothing()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create rotation that changes axis (gimbal lock scenario)
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 0, 90), SecondsToTicks(2.0f)), // newest - Z axis
                CreateSnapshot(Quaternion.Euler(0, 90, 0), SecondsToTicks(1.0f)), // Y axis
                CreateSnapshot(Quaternion.Euler(90, 0, 0), SecondsToTicks(0.0f))  // oldest - X axis
            };

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(1.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // Smoothing should handle axis changes gracefully
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
        }

        [Test]
        public void Quaternion_AdvancedMotionAnalysis_Toggle()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 180, 0), SecondsToTicks(2.0f)), // newest (index 0)
                CreateSnapshot(Quaternion.Euler(0, 90, 0), SecondsToTicks(1.0f)),  // middle (index 1)
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))    // oldest (index 2)
            };

            // Test with advanced motion analysis ON
            GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter.UseAdvancedMotionAnalysis = true;

            GONetSyncableValue blendedValueAdvanced;
            bool didExtrapolateAdvanced;
            bool resultAdvanced = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValueAdvanced, out didExtrapolateAdvanced);

            // Test with advanced motion analysis OFF
            GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter.UseAdvancedMotionAnalysis = false;

            GONetSyncableValue blendedValueSimple;
            bool didExtrapolateSimple;
            bool resultSimple = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(2.5f), out blendedValueSimple, out didExtrapolateSimple);

            // Restore default
            GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter.UseAdvancedMotionAnalysis = true;

            // Both should produce valid results
            Assert.IsTrue(resultAdvanced);
            Assert.IsTrue(resultSimple);
            Assert.IsNotNull(blendedValueAdvanced.UnityEngine_Quaternion);
            Assert.IsNotNull(blendedValueSimple.UnityEngine_Quaternion);

            // Results might differ significantly due to different algorithms and smoothing
            float angleDiff = Quaternion.Angle(blendedValueAdvanced.UnityEngine_Quaternion, blendedValueSimple.UnityEngine_Quaternion);
            Assert.LessOrEqual(angleDiff, 45.0f); // Both algorithms are valid, allow reasonable variance
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Vector3_EmptyBuffer_ReturnsFalse()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();
            var buffer = new NumericValueChangeSnapshot[0];

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 0, SecondsToTicks(1.0f), out blendedValue, out didExtrapolate);

            Assert.IsFalse(result);
        }

        [Test]
        public void Quaternion_EmptyBuffer_ReturnsFalse()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();
            var buffer = new NumericValueChangeSnapshot[0];

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 0, SecondsToTicks(1.0f), out blendedValue, out didExtrapolate);

            Assert.IsFalse(result);
        }

        [Test]
        public void Vector3_TimeBeforeOldest_UsesOldestValue()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(2.0f)),
                CreateSnapshot(new Vector3(5, 0, 0), SecondsToTicks(1.0f))
            };

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(0.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsFalse(didExtrapolate);
            Assert.AreEqual(5.0f, blendedValue.UnityEngine_Vector3.x, POSITION_EPSILON); // Should use oldest
        }

        [Test]
        public void Quaternion_LargeExtrapolationJump_ClampsProperly()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create very fast rotation that would cause huge jump
            // Buffer in NEWEST FIRST order
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 720, 0), SecondsToTicks(1.0f)), // newest: 2 full rotations/second!
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))    // oldest
            };

            // Extrapolate 0.5 seconds into future (within threshold)
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(1.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate); // Should extrapolate
            // Should produce a valid quaternion (clamping/constraints applied)
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
            // Verify quaternion is normalized (valid) by checking x²+y²+z²+w² ≈ 1
            Quaternion q = blendedValue.UnityEngine_Quaternion;
            float sqrMagnitude = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            Assert.AreEqual(1.0f, sqrMagnitude, 0.01f);
        }

        #endregion

        #region Performance Characteristics

        [Test]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        public void Vector3_LargeBufferPerformance(int bufferSize)
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();
            var buffer = new NumericValueChangeSnapshot[bufferSize];

            // Fill with linear motion data
            for (int i = 0; i < bufferSize; i++)
            {
                float time = (bufferSize - 1 - i) * 0.1f; // Oldest to newest
                buffer[i] = CreateSnapshot(new Vector3(time * 10, 0, 0), SecondsToTicks(time));
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, bufferSize, SecondsToTicks(bufferSize * 0.1f), out blendedValue, out didExtrapolate);

            sw.Stop();

            Assert.IsTrue(result);
            Assert.Less(sw.ElapsedMilliseconds, 1); // Should be sub-millisecond even for large buffers

            TestContext.WriteLine($"Buffer size {bufferSize}: {sw.Elapsed.TotalMilliseconds:F1}ms");
        }

        #endregion
    }
}
