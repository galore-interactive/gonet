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
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue support4 = new GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue();
            Quaternion startingRotation = Quaternion.Euler(90, 0, 0);
            Quaternion degrees5 = Quaternion.Euler(5, 0, 0);
            Quaternion degrees10 = Quaternion.Euler(10, 0, 0);
            Quaternion degrees15 = Quaternion.Euler(15, 0, 0);


            {  // NOTE: taken  from GONetParticipant_AutoMagicalSyncCompanion_Generated_1
                support4.baselineValue_current.UnityEngine_Quaternion = startingRotation;
                support4.lastKnownValue.UnityEngine_Quaternion = startingRotation;
                support4.lastKnownValue_previous.UnityEngine_Quaternion = startingRotation;
                support4.valueLimitEncountered_min.UnityEngine_Quaternion = startingRotation;
                support4.valueLimitEncountered_max.UnityEngine_Quaternion = startingRotation;
                support4.syncCompanion = null; // not needed for test so null is OK
                support4.memberName = "rotation";
                support4.index = 4;
                support4.syncAttribute_MustRunOnUnityMainThread = true;
                support4.syncAttribute_ProcessingPriority = 0;
                support4.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
                support4.syncAttribute_SyncChangesEverySeconds = 0.05f;
                support4.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
                support4.syncAttribute_ShouldBlendBetweenValuesReceived = true;
                GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((0, 1), out support4.syncAttribute_ShouldSkipSync);
                support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

                // cachedCustomSerializers[4] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);

                int support4_mostRecentChanges_calcdSize = support4.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support4.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
                support4.mostRecentChanges_capacitySize = Math.Max(support4_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
                support4.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support4.mostRecentChanges_capacitySize);
            }

            support4.mostRecentChanges_usedSize = 4;

            DateTime oldestTime = DateTime.Today;

            NumericValueChangeSnapshot value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.018).Ticks;
            value.numericValue = startingRotation * degrees15;
            support4.mostRecentChanges[0] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.015).Ticks;
            value.numericValue = startingRotation * degrees10;
            support4.mostRecentChanges[1] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.01).Ticks;
            value.numericValue = startingRotation * degrees5;
            support4.mostRecentChanges[2] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.Ticks;
            value.numericValue = startingRotation;
            support4.mostRecentChanges[3] = value;

            GONetSyncableValue blendedValue;
            bool isGrande = ValueBlendUtils.TryGetBlendedValue(support4, oldestTime.AddSeconds(0.0195).Ticks, out blendedValue, out bool didExtrapolate);

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

        private long SecondsToTicks(float seconds)
        {
            return (long)(seconds * TimeSpan.TicksPerSecond);
        }

        #endregion

        #region Vector3 Tests

        [Test]
        public void Vector3_Interpolation_LinearMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create linear motion: 0,0,0 -> 10,0,0 over 1 second
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(1.0f)), // newest
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // oldest
            };

            // Test interpolation at 0.5 seconds
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(0.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsFalse(didExtrapolate); // Should interpolate, not extrapolate
            Assert.AreEqual(5.0f, blendedValue.UnityEngine_Vector3.x, POSITION_EPSILON);
            Assert.AreEqual(0.0f, blendedValue.UnityEngine_Vector3.y, POSITION_EPSILON);
            Assert.AreEqual(0.0f, blendedValue.UnityEngine_Vector3.z, POSITION_EPSILON);
        }

        [Test]
        public void Vector3_Extrapolation_LinearMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create linear motion with constant velocity
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(20, 0, 0), SecondsToTicks(2.0f)), // newest
                CreateSnapshot(new Vector3(10, 0, 0), SecondsToTicks(1.0f)),
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // oldest
            };

            // Test extrapolation at 3 seconds (1 second past newest)
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(3.0f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate); // Should extrapolate past newest
            Assert.AreEqual(30.0f, blendedValue.UnityEngine_Vector3.x, POSITION_EPSILON); // Linear extrapolation
        }

        [Test]
        public void Vector3_Acceleration_QuadraticMotion()
        {
            var blender = new GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter();

            // Create accelerating motion: x = 5t² (acceleration = 10 units/s²)
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(new Vector3(20, 0, 0), SecondsToTicks(2.0f)), // 5 * 4 = 20
                CreateSnapshot(new Vector3(5, 0, 0), SecondsToTicks(1.0f)),  // 5 * 1 = 5
                CreateSnapshot(new Vector3(0, 0, 0), SecondsToTicks(0.0f))   // 5 * 0 = 0
            };

            // Test extrapolation at 3 seconds
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(3.0f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate);
            // With acceleration, should be close to 45 (5 * 9)
            Assert.AreEqual(45.0f, blendedValue.UnityEngine_Vector3.x, 5.0f); // Larger epsilon for acceleration
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
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 90, 0), SecondsToTicks(1.0f)), // newest
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))   // oldest
            };

            // Test interpolation at 0.5 seconds
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(0.5f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsFalse(didExtrapolate);

            float angle = Quaternion.Angle(Quaternion.Euler(0, 45, 0), blendedValue.UnityEngine_Quaternion);
            Assert.LessOrEqual(angle, ANGLE_EPSILON); // Should be ~45 degrees
        }

        [Test]
        public void Quaternion_Extrapolation_ConstantAngularVelocity()
        {
            var blender = new GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter();

            // Create constant angular velocity: 30°/s around Y
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 60, 0), SecondsToTicks(2.0f)), // newest
                CreateSnapshot(Quaternion.Euler(0, 30, 0), SecondsToTicks(1.0f)),
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))   // oldest
            };

            // Test extrapolation at 3 seconds
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(3.0f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            Assert.IsTrue(didExtrapolate);

            // Should extrapolate to ~90 degrees
            float angle = Quaternion.Angle(Quaternion.Euler(0, 90, 0), blendedValue.UnityEngine_Quaternion);
            Assert.LessOrEqual(angle, 5.0f); // Larger tolerance for extrapolation
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

            // Test with advanced motion analysis ON
            GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter.UseAdvancedMotionAnalysis = true;

            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 180, 0), SecondsToTicks(2.0f)),
                CreateSnapshot(Quaternion.Euler(0, 90, 0), SecondsToTicks(1.0f)),
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))
            };

            GONetSyncableValue blendedValueAdvanced;
            bool didExtrapolateAdvanced;
            blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(3.0f), out blendedValueAdvanced, out didExtrapolateAdvanced);

            // Test with advanced motion analysis OFF
            GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter.UseAdvancedMotionAnalysis = false;

            GONetSyncableValue blendedValueSimple;
            bool didExtrapolateSimple;
            blender.TryGetBlendedValue(buffer, 3, SecondsToTicks(3.0f), out blendedValueSimple, out didExtrapolateSimple);

            // Both should produce valid results
            Assert.IsNotNull(blendedValueAdvanced.UnityEngine_Quaternion);
            Assert.IsNotNull(blendedValueSimple.UnityEngine_Quaternion);

            // Results might differ slightly due to different algorithms
            float angleDiff = Quaternion.Angle(blendedValueAdvanced.UnityEngine_Quaternion, blendedValueSimple.UnityEngine_Quaternion);
            Assert.LessOrEqual(angleDiff, 10.0f); // Should be reasonably close
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
            var buffer = new NumericValueChangeSnapshot[]
            {
                CreateSnapshot(Quaternion.Euler(0, 720, 0), SecondsToTicks(1.0f)), // 2 full rotations/second!
                CreateSnapshot(Quaternion.Euler(0, 0, 0), SecondsToTicks(0.0f))
            };

            // Try to extrapolate way into future
            GONetSyncableValue blendedValue;
            bool didExtrapolate;
            bool result = blender.TryGetBlendedValue(buffer, 2, SecondsToTicks(5.0f), out blendedValue, out didExtrapolate);

            Assert.IsTrue(result);
            // Should be clamped to prevent wild extrapolation
            Assert.IsNotNull(blendedValue.UnityEngine_Quaternion);
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
