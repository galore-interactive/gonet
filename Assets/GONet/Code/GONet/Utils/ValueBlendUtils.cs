/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
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

using GONet.PluginAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

[assembly:InternalsVisibleTo("Assembly-CSharp-Editor")] // this is in support of unit tests

namespace GONet.Utils
{
    internal static class ValueBlendUtils
    {
        internal const int VALUE_COUNT_NEEDED_TO_EXTRAPOLATE = 2;

        internal static bool TryGetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            if (valueMonitoringSupport.TryGetBlendedValue(atElapsedTicks, out blendedValue, out didExtrapolate))
            {
                return true;
            }

            // If the above does not yield good value blend, then the below will attmept to use the default implementations available.
            // NOTE: This would most likely be due to the valueMonitoringSupport profile/template not having identified an implementation of IGONetAutoMagicalSync_CustomValueBlending for the type of blendedValue.
            if (valueMonitoringSupport.mostRecentChanges_usedSize > 0)
            {
                IGONetAutoMagicalSync_CustomValueBlending customBlending;
                if (defaultValueBlendings_byValueType.TryGetValue(valueMonitoringSupport.mostRecentChanges[0].numericValue.GONetSyncType, out customBlending))
                {
                    return customBlending.TryGetBlendedValue(valueMonitoringSupport.mostRecentChanges, valueMonitoringSupport.mostRecentChanges_usedSize, atElapsedTicks, out blendedValue, out didExtrapolate);
                }
            }

            didExtrapolate = false;
            blendedValue = default;
            return false;
        }

        static readonly Dictionary<GONetSyncableValueTypes, IGONetAutoMagicalSync_CustomValueBlending> defaultValueBlendings_byValueType = new Dictionary<GONetSyncableValueTypes, IGONetAutoMagicalSync_CustomValueBlending>(8)
        {
            { GONetSyncableValueTypes.System_Single, new GONetDefaultValueBlending_Float() },
            { GONetSyncableValueTypes.UnityEngine_Quaternion, new GONetDefaultValueBlending_Quaternion() },
            { GONetSyncableValueTypes.UnityEngine_Vector3, new GONetDefaultValueBlending_Vector3() }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetQuadraticBezierValue(float p0, float p1, float p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        internal static Vector3 CenterAround180(Vector3 eulerAngles)
        {
            return new Vector3(CenterAround180(eulerAngles.x), CenterAround180(eulerAngles.y), CenterAround180(eulerAngles.z));
        }

        internal static float CenterAround180(float f)
        {
            f += 180;
            if (f > 360) f -= 360;
            return f;
        }

        internal static bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector3 averageAcceleration)
        {
            averageAcceleration = new Vector3();

            if (valueCount > 1)
            {
                Vector3 totalVelocity = Vector3.zero;
                float totalSeconds = 0;
                for (int i = 0; i < valueCount - 1; ++i)
                {
                    Vector3 val_1 = valueBuffer[i].numericValue.UnityEngine_Vector3;
                    Vector3 val_2 = valueBuffer[i + 1].numericValue.UnityEngine_Vector3;
                    Vector3 velocity = val_1 - val_2;
                    totalVelocity += velocity;
                    totalSeconds += (float)TimeSpan.FromTicks(valueBuffer[i].elapsedTicksAtChange - valueBuffer[i + 1].elapsedTicksAtChange).TotalSeconds;
                }

                averageAcceleration = totalVelocity / totalSeconds;

                return true;
            }

            return false;
        }

        internal static bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector2 averageAcceleration)
        {
            averageAcceleration = new Vector2();

            if (valueCount > 1)
            {
                Vector2 totalVelocity = Vector2.zero;
                float totalSeconds = 0;
                for (int i = 0; i < valueCount - 1; ++i)
                {
                    Vector2 val_1 = valueBuffer[i].numericValue.UnityEngine_Vector2;
                    Vector2 val_2 = valueBuffer[i + 1].numericValue.UnityEngine_Vector2;
                    Vector2 velocity = val_1 - val_2;
                    totalVelocity += velocity;
                    totalSeconds += (float)TimeSpan.FromTicks(valueBuffer[i].elapsedTicksAtChange - valueBuffer[i + 1].elapsedTicksAtChange).TotalSeconds;
                }

                averageAcceleration = totalVelocity / totalSeconds;

                return true;
            }

            return false;
        }

        internal static bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector4 averageAcceleration)
        {
            averageAcceleration = new Vector4();

            if (valueCount > 1)
            {
                Vector4 totalVelocity = Vector4.zero;
                float totalSeconds = 0;
                for (int i = 0; i < valueCount - 1; ++i)
                {
                    Vector4 val_1 = valueBuffer[i].numericValue.UnityEngine_Vector3;
                    Vector4 val_2 = valueBuffer[i + 1].numericValue.UnityEngine_Vector3;
                    Vector4 velocity = val_1 - val_2;
                    totalVelocity += velocity;
                    totalSeconds += (float)TimeSpan.FromTicks(valueBuffer[i].elapsedTicksAtChange - valueBuffer[i + 1].elapsedTicksAtChange).TotalSeconds;
                }

                averageAcceleration = totalVelocity / totalSeconds;

                return true;
            }

            return false;
        }

        internal static bool DetermineTimeBetweenStats(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out float min, out float average, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            average = -1;

            if (valueCount > 1)
            {
                float total = 0;
                for (int i = 0; i < valueCount - 1; ++i)
                {
                    float millis_1 = (float)TimeSpan.FromTicks(valueBuffer[i].elapsedTicksAtChange).TotalMilliseconds;
                    float millis_2 = (float)TimeSpan.FromTicks(valueBuffer[i + 1].elapsedTicksAtChange).TotalMilliseconds;
                    float diffMillis = millis_1 - millis_2;
                    total += diffMillis;
                    if (diffMillis < min) min = diffMillis;
                    if (diffMillis > max) max = diffMillis;
                }

                average = total / (valueCount - 1);

                return true;
            }

            return false;
        }

        internal static Vector3 GetVector3AvgAccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest)
        {
            Vector3 averageAcceleration;
            if (TryDetermineAverageAccelerationPerSecond(valueBuffer, valueCount, out averageAcceleration))
            {
                NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
                Vector3 q1 = q1_snap.numericValue.UnityEngine_Vector3;

                NumericValueChangeSnapshot q2_snap = newest;
                Vector3 q2 = newest.numericValue.UnityEngine_Vector3;

                Vector3 diff_q2_q1 = q2 - q1;
                float diff_q2_q1_seconds = (float)TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange).TotalSeconds;
                Vector3 velocity_q2_q1 = diff_q2_q1 / diff_q2_q1_seconds;

                float atMinusNewest_seconds = (float)TimeSpan.FromTicks(atElapsedTicks - q2_snap.elapsedTicksAtChange).TotalSeconds;

                // s = 	s0 + v0t + ½at^2
                var s0 = q2;
                var v0 = velocity_q2_q1;
                var s = s0 + (v0 * atMinusNewest_seconds) + (0.5f * averageAcceleration * atMinusNewest_seconds * atMinusNewest_seconds);
                Vector3 extrapolatedViaAcceleration = s;

                //GONetLog.Debug($"\nvelocity_q1_q0: (x:{velocity_q1_q0.x}, y:{velocity_q1_q0.y}, z:{velocity_q1_q0.z})\nvelocity_q2_q1: (x:{velocity_q2_q1.x}, y:{velocity_q2_q1.y}, z:{velocity_q2_q1.z})");

                //Vector3 extrapolatedViaAcceleration = q2 + finalVelocity;

                return extrapolatedViaAcceleration;
            }

            throw new Exception("booboo");

        }

        internal static Vector3 GetVector3AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector3 acceleration)
        {
            NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
            Vector3 q0 = q0_snap.numericValue.UnityEngine_Vector3;

            NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
            Vector3 q1 = q1_snap.numericValue.UnityEngine_Vector3;

            NumericValueChangeSnapshot q2_snap = newest;
            Vector3 q2 = newest.numericValue.UnityEngine_Vector3;

            Vector3 diff_q2_q1 = q2 - q1;
            float diff_q2_q1_seconds = (float)TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange).TotalSeconds;
            Vector3 velocity_q2_q1 = diff_q2_q1 / diff_q2_q1_seconds;

            Vector3 diff_q1_q0 = q1 - q0;
            float diff_q1_q0_seconds = (float)TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange - q0_snap.elapsedTicksAtChange).TotalSeconds;
            Vector3 velocity_q1_q0 = diff_q1_q0 / diff_q1_q0_seconds;

            acceleration = (velocity_q2_q1 - velocity_q1_q0);

            float atMinusNewest_seconds = (float)TimeSpan.FromTicks(atElapsedTicks - q2_snap.elapsedTicksAtChange).TotalSeconds;
            Vector3 finalVelocity = velocity_q2_q1 + acceleration * atMinusNewest_seconds;

            // s = 	s0 + v0t + ½at^2
            Vector3 s0 = q2;
            Vector3 v0 = velocity_q2_q1;
            Vector3 s = s0 + (v0 * atMinusNewest_seconds) + (0.5f * acceleration * atMinusNewest_seconds * atMinusNewest_seconds);
            Vector3 extrapolatedViaAcceleration = s;

            //GONetLog.Debug($"\nvelocity_q1_q0: (x:{velocity_q1_q0.x}, y:{velocity_q1_q0.y}, z:{velocity_q1_q0.z})\nvelocity_q2_q1: (x:{velocity_q2_q1.x}, y:{velocity_q2_q1.y}, z:{velocity_q2_q1.z})");

            //Vector3 extrapolatedViaAcceleration = q2 + finalVelocity;

            return extrapolatedViaAcceleration;
        }

        internal static Vector2 GetVector2AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector2 acceleration)
        {
            NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
            Vector2 q0 = q0_snap.numericValue.UnityEngine_Vector2;

            NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
            Vector2 q1 = q1_snap.numericValue.UnityEngine_Vector2;

            NumericValueChangeSnapshot q2_snap = newest;
            Vector2 q2 = newest.numericValue.UnityEngine_Vector2;

            Vector2 diff_q2_q1 = q2 - q1;
            float diff_q2_q1_seconds = (float)TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange).TotalSeconds;
            Vector2 velocity_q2_q1 = diff_q2_q1 / diff_q2_q1_seconds;

            Vector2 diff_q1_q0 = q1 - q0;
            float diff_q1_q0_seconds = (float)TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange - q0_snap.elapsedTicksAtChange).TotalSeconds;
            Vector2 velocity_q1_q0 = diff_q1_q0 / diff_q1_q0_seconds;

            acceleration = (velocity_q2_q1 - velocity_q1_q0);

            float atMinusNewest_seconds = (float)TimeSpan.FromTicks(atElapsedTicks - q2_snap.elapsedTicksAtChange).TotalSeconds;
            Vector2 finalVelocity = velocity_q2_q1 + acceleration * atMinusNewest_seconds;

            // s = 	s0 + v0t + ½at^2
            var s0 = q2;
            var v0 = velocity_q2_q1;
            var s = s0 + (v0 * atMinusNewest_seconds) + (0.5f * acceleration * atMinusNewest_seconds * atMinusNewest_seconds);
            Vector2 extrapolatedViaAcceleration = s;

            //GONetLog.Debug($"\nvelocity_q1_q0: (x:{velocity_q1_q0.x}, y:{velocity_q1_q0.y}, z:{velocity_q1_q0.z})\nvelocity_q2_q1: (x:{velocity_q2_q1.x}, y:{velocity_q2_q1.y}, z:{velocity_q2_q1.z})");

            //Vector3 extrapolatedViaAcceleration = q2 + finalVelocity;

            return extrapolatedViaAcceleration;
        }

        internal static Vector4 GetVector4AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector4 acceleration)
        {
            NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
            Vector4 q0 = q0_snap.numericValue.UnityEngine_Vector4;

            NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
            Vector4 q1 = q1_snap.numericValue.UnityEngine_Vector4;

            NumericValueChangeSnapshot q2_snap = newest;
            Vector4 q2 = newest.numericValue.UnityEngine_Vector4;

            Vector4 diff_q2_q1 = q2 - q1;
            float diff_q2_q1_seconds = (float)TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange).TotalSeconds;
            Vector4 velocity_q2_q1 = diff_q2_q1 / diff_q2_q1_seconds;

            Vector4 diff_q1_q0 = q1 - q0;
            float diff_q1_q0_seconds = (float)TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange - q0_snap.elapsedTicksAtChange).TotalSeconds;
            Vector4 velocity_q1_q0 = diff_q1_q0 / diff_q1_q0_seconds;

            acceleration = (velocity_q2_q1 - velocity_q1_q0);

            float atMinusNewest_seconds = (float)TimeSpan.FromTicks(atElapsedTicks - q2_snap.elapsedTicksAtChange).TotalSeconds;
            Vector4 finalVelocity = velocity_q2_q1 + acceleration * atMinusNewest_seconds;

            // s = 	s0 + v0t + ½at^2
            var s0 = q2;
            var v0 = velocity_q2_q1;
            var s = s0 + (v0 * atMinusNewest_seconds) + (0.5f * acceleration * atMinusNewest_seconds * atMinusNewest_seconds);
            Vector4 extrapolatedViaAcceleration = s;

            //GONetLog.Debug($"\nvelocity_q1_q0: (x:{velocity_q1_q0.x}, y:{velocity_q1_q0.y}, z:{velocity_q1_q0.z})\nvelocity_q2_q1: (x:{velocity_q2_q1.x}, y:{velocity_q2_q1.y}, z:{velocity_q2_q1.z})");

            //Vector3 extrapolatedViaAcceleration = q2 + finalVelocity;

            return extrapolatedViaAcceleration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetQuadraticBezierValue(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 GetQuadraticBezierValue(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 GetQuadraticBezierValue(Vector4 p0, Vector4 p1, Vector4 p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        const int SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT = 3;
        const int SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT = 2;

        /// <summary>
        /// All the values in this array summed with all the values in <see cref="SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES"/> need to add up to 1.0f.
        /// The number of values much match <see cref="SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT"/>
        ///  NOTE: The order of items is how much effect will be made on the most recent first order (i.e., lowest index is the most recent, highest index is the oldest)
        /// </summary>
        static readonly float[] SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES = { 0.35f, 0.1f, 0.1f };
        /// <summary>
        /// All the values in this array summed with all the values in <see cref="SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES"/> need to add up to 1.0f.
        /// The number of values much match <see cref="SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT"/>
        ///  NOTE: The order of items is how much effect will be made on the most recent first order (i.e., lowest index is the most recent, highest index is the oldest)
        /// </summary>
        static readonly float[] SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES = { 0.4f, 0.05f };

        static readonly ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Quaternion>>> GetSmoothedRotation_m_outputs_byBufferByThread =
            new ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Quaternion>>>();
        static readonly ConcurrentDictionary<Thread, List<Quaternion>> GetSmoothedRotation_m_inputs_byThread = new ConcurrentDictionary<Thread, List<Quaternion>>();

        static readonly ConcurrentDictionary<Thread, Quaternion[]> GetSmoothedRotation_m_outputsRelative_byThread = new ConcurrentDictionary<Thread, Quaternion[]>();
        static readonly ConcurrentDictionary<Thread, Quaternion[]> GetSmoothedRotation_m_inputsRelative_byThread = new ConcurrentDictionary<Thread, Quaternion[]>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mostRecentValue"></param>
        /// <param name="olderValuesBuffer">required to be in newest value in lowest index order (i.e., most recent first)</param>
        /// <param name="bufferCount"></param>
        /// <returns></returns>
        internal static Quaternion GetSmoothedRotation(Quaternion mostRecentValue, NumericValueChangeSnapshot[] olderValuesBuffer, int bufferCount)
        {
            Quaternion result;

            List<Quaternion> GetSmoothedRotation_m_inputs; // NOTE: The order of items is most recent last order (i.e., highest index is the most recent, lowest index is the oldest)
            if (!GetSmoothedRotation_m_inputs_byThread.TryGetValue(Thread.CurrentThread, out GetSmoothedRotation_m_inputs))
            {
                GetSmoothedRotation_m_inputs_byThread[Thread.CurrentThread] = GetSmoothedRotation_m_inputs = new List<Quaternion>();
            }
            else
            {
                GetSmoothedRotation_m_inputs.Clear();
            }

            for (int i = bufferCount - 1; i >= 0; --i) // GetSmoothedRotation_m_inputs is most recent last order, which is the opposite of olderValuesBuffer
            {
                Quaternion value = olderValuesBuffer[i].numericValue.UnityEngine_Quaternion;
                GetSmoothedRotation_m_inputs.Add(value);
            }

            if (GetSmoothedRotation_m_inputs.Count < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT)
            {
                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    GetSmoothedRotation_m_inputs.Add(mostRecentValue);
                }
            }
            else
            {
                GetSmoothedRotation_m_inputs.Add(mostRecentValue);
            }


            Dictionary<NumericValueChangeSnapshot[], List<Quaternion>> GetSmoothedRotation_m_outputs_byBuffer;
            if (!GetSmoothedRotation_m_outputs_byBufferByThread.TryGetValue(Thread.CurrentThread, out GetSmoothedRotation_m_outputs_byBuffer))
            {
                GetSmoothedRotation_m_outputs_byBufferByThread[Thread.CurrentThread] = GetSmoothedRotation_m_outputs_byBuffer = new Dictionary<NumericValueChangeSnapshot[], List<Quaternion>>();
            }

            List<Quaternion> GetSmoothedRotation_m_outputs; // NOTE: The order of items is most recent last order (i.e., highest index is the most recent, lowest index is the oldest)
            if (!GetSmoothedRotation_m_outputs_byBuffer.TryGetValue(olderValuesBuffer, out GetSmoothedRotation_m_outputs))
            {
                GetSmoothedRotation_m_outputs_byBuffer[olderValuesBuffer] = GetSmoothedRotation_m_outputs = new List<Quaternion>();
            }
            else
            {
                int outputsToRemove = GetSmoothedRotation_m_outputs.Count - GetSmoothedRotation_m_inputs.Count;
                if (outputsToRemove > 0)
                {
                    GetSmoothedRotation_m_outputs.RemoveRange(0, outputsToRemove); // remove the oldest entries to keep this from growing indefinitely....matching the input count looks to be more to keep than is needed, leaves enough to operate on and is easy to accomplish
                }
            }


            if (GetSmoothedRotation_m_outputs.Count < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT)
            {
                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    GetSmoothedRotation_m_outputs.Add(mostRecentValue);
                }
            }

            {//*
             // Normalize all inputs to be relative to a recent rotation, to shrink the euler angle values, avoiding a singularity
                Quaternion basis = mostRecentValue;
                Quaternion invBasis = Quaternion.Inverse(basis);

                Quaternion[] inputs; // NOTE: The order of items is OPPOSITE of GetSmoothedRotation_m_inputs... therefore this is in most recent first order (i.e., lowest index is the most recent, highest index is the oldest)
                if (!GetSmoothedRotation_m_inputsRelative_byThread.TryGetValue(Thread.CurrentThread, out inputs))
                {
                    GetSmoothedRotation_m_inputsRelative_byThread[Thread.CurrentThread] = inputs = new Quaternion[SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT];
                }
                int iMostRecent_m_inputs = GetSmoothedRotation_m_inputs.Count - 1;
                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    inputs[i] = invBasis * GetSmoothedRotation_m_inputs[iMostRecent_m_inputs - i];
                }

                Quaternion[] outputs; // NOTE: The order of items is OPPOSITE of GetSmoothedRotation_m_outputs... therefore this is in most recent first order (i.e., lowest index is the most recent, highest index is the oldest)
                if (!GetSmoothedRotation_m_outputsRelative_byThread.TryGetValue(Thread.CurrentThread, out outputs))
                {
                    GetSmoothedRotation_m_outputsRelative_byThread[Thread.CurrentThread] = outputs = new Quaternion[SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT];
                }
                int iMostRecent_m_outputs = GetSmoothedRotation_m_outputs.Count - 1;
                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    outputs[i] = invBasis * GetSmoothedRotation_m_outputs[iMostRecent_m_outputs - i];
                }

                Quaternion temp = Quaternion.identity;

                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    float smoothingEffectorPercentage = /* SmoothingValuesModifiersUI.INPUTS[i]; */ SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    if (smoothingEffectorPercentage > 0)
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, inputs[i], smoothingEffectorPercentage);
                    }
                    else
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(inputs[i]), -smoothingEffectorPercentage);
                    }
                }

                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    float smoothingEffectorPercentage = /* SmoothingValuesModifiersUI.OUTPUTS[i]; */ SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    if (smoothingEffectorPercentage > 0)
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, outputs[i], smoothingEffectorPercentage);
                    }
                    else
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(outputs[i]), -smoothingEffectorPercentage);
                    }
                }

                result = basis * temp;
            }

            {
                /*
                Quaternion temp = Quaternion.identity;

                int iLastInput = GetSmoothedRotation_m_inputs.Count - 1;
                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    float smoothingEffectorPercentage = SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    if (smoothingEffectorPercentage > 0)
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, GetSmoothedRotation_m_inputs[iLastInput - i], smoothingEffectorPercentage);
                    }
                    else
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(GetSmoothedRotation_m_inputs[iLastInput - i]), -smoothingEffectorPercentage);
                    }
                }

                int iLastOutput = GetSmoothedRotation_m_outputs.Count - 1;
                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    float smoothingEffectorPercentage = SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    if (smoothingEffectorPercentage > 0)
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, GetSmoothedRotation_m_outputs[iLastOutput - i], smoothingEffectorPercentage);
                    }
                    else
                    {
                        temp *= Quaternion.Slerp(Quaternion.identity, Quaternion.Inverse(GetSmoothedRotation_m_outputs[iLastOutput - i]), -smoothingEffectorPercentage);
                    }
                }

                result = temp;
                */
            }

            GetSmoothedRotation_m_outputs.Add(result);

            return result;
        }

        static readonly ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Vector3>>> GetSmoothedVector3_m_outputs_byBufferByThread =
            new ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Vector3>>>();
        static readonly ConcurrentDictionary<Thread, List<Vector3>> GetSmoothedVector3_m_inputs_byThread = new ConcurrentDictionary<Thread, List<Vector3>>();

        internal static Vector3 GetSmoothedVector3(Vector3 mostRecentValue, NumericValueChangeSnapshot[] olderValuesBuffer, int bufferCount)
        {
            List<Vector3> GetSmoothedVector3_m_inputs;
            if (!GetSmoothedVector3_m_inputs_byThread.TryGetValue(Thread.CurrentThread, out GetSmoothedVector3_m_inputs))
            {
                GetSmoothedVector3_m_inputs_byThread[Thread.CurrentThread] = GetSmoothedVector3_m_inputs = new List<Vector3>();
            }
            else
            {
                GetSmoothedVector3_m_inputs.Clear();
            }

            for (int i = bufferCount - 1; i >= 0; --i) // m_inputs is most recent last order, which is the opposite of valueBuffer
            {
                Vector3 value = olderValuesBuffer[i].numericValue.UnityEngine_Vector3;
                GetSmoothedVector3_m_inputs.Add(value);
            }

            // Butterworth filter (order 2, cutoff=0.5)
            if (GetSmoothedVector3_m_inputs.Count < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT)
            {
                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    GetSmoothedVector3_m_inputs.Add(mostRecentValue);
                }
            }
            else
            {
                GetSmoothedVector3_m_inputs.Add(mostRecentValue);
            }

            Dictionary<NumericValueChangeSnapshot[], List<Vector3>> GetSmoothedVector3_m_outputs_byBuffer;
            if (!GetSmoothedVector3_m_outputs_byBufferByThread.TryGetValue(Thread.CurrentThread, out GetSmoothedVector3_m_outputs_byBuffer))
            {
                GetSmoothedVector3_m_outputs_byBufferByThread[Thread.CurrentThread] = GetSmoothedVector3_m_outputs_byBuffer = new Dictionary<NumericValueChangeSnapshot[], List<Vector3>>();
            }

            List<Vector3> GetSmoothedVector3_m_outputs;
            if (!GetSmoothedVector3_m_outputs_byBuffer.TryGetValue(olderValuesBuffer, out GetSmoothedVector3_m_outputs))
            {
                GetSmoothedVector3_m_outputs_byBuffer[olderValuesBuffer] = GetSmoothedVector3_m_outputs = new List<Vector3>();
            }
            else
            {
                int outputsToRemove = GetSmoothedVector3_m_outputs.Count - GetSmoothedVector3_m_inputs.Count;
                if (outputsToRemove > 0)
                {
                    GetSmoothedVector3_m_outputs.RemoveRange(0, outputsToRemove); // remove the oldest entries to keep this from growing indefinitely....matching the input count looks to be more to keep than is needed, leaves enough to operate on and is easy to accomplish
                }
            }

            if (GetSmoothedVector3_m_outputs.Count < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT)
            {
                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    GetSmoothedVector3_m_outputs.Add(mostRecentValue);
                }
            }

            Vector3 result = Vector3.zero;

            int iLastInput = GetSmoothedVector3_m_inputs.Count - 1;
            for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
            {
                result += GetSmoothedVector3_m_inputs[iLastInput - i] * SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
            }

            int iLastOutput = GetSmoothedVector3_m_outputs.Count - 1;
            for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
            {
                result += GetSmoothedVector3_m_outputs[iLastOutput - i] * SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
            }

            GetSmoothedVector3_m_outputs.Add(result);

            return result;
        }
    }
}
