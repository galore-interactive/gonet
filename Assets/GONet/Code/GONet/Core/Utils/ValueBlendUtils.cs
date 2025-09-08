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
        // Constants
        internal const int VALUE_COUNT_NEEDED_TO_EXTRAPOLATE = 2;
        internal const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;

        /// <summary>
        /// Some areas of code herein and related code guard some debug logging calls with this to optionally log more.
        /// </summary>
        public static bool ShouldLog { get; set; } = false;

        /// <summary>
        /// This is used when <see cref="IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH"/> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int DetermineUsableValueCountForForcedExtrapolation(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            int newestBufferIndex,
            int oldestBufferIndex,
            out int baseIndex)
        {
            // Pin array for direct access (eliminates bounds checking)
            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                long newestTicks = bufferPtr[newestBufferIndex].elapsedTicksAtChange;

                // Fast path 1: At or after newest (common case for extrapolation)
                if (atElapsedTicks >= newestTicks)
                {
                    baseIndex = newestBufferIndex;
                    return valueCount; // More direct than oldestBufferIndex - newestBufferIndex + 1
                }

                long oldestTicks = bufferPtr[oldestBufferIndex].elapsedTicksAtChange;

                // Fast path 2: Before oldest (rare but quick to check)
                if (atElapsedTicks <= oldestTicks)
                {
                    baseIndex = oldestBufferIndex;
                    return 1;
                }

                // For 8-20 values, binary search is optimal
                // Using pointers for maximum performance
                int left = newestBufferIndex;
                int right = oldestBufferIndex;

                // Binary search with pointer arithmetic
                while (left < right)
                {
                    // Bit shift for fast division by 2
                    int mid = left + ((right - left) >> 1);

                    // Direct pointer access is faster than array indexing
                    long midTicks = bufferPtr[mid].elapsedTicksAtChange;

                    // Branchless version could be:
                    // int goLeft = midTicks > atElapsedTicks ? 1 : 0;
                    // left = goLeft * (mid + 1) + (1 - goLeft) * left;
                    // right = goLeft * right + (1 - goLeft) * mid;
                    // But simple branch is actually faster for binary search

                    if (midTicks > atElapsedTicks)
                    {
                        left = mid + 1;
                    }
                    else
                    {
                        right = mid;
                    }
                }

                baseIndex = left;

                // Direct calculation
                return oldestBufferIndex - left + 1;
            }
        }

        internal static bool TryGetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
        {
            //GONetLog.Debug($"grease");
            if (valueMonitoringSupport.TryGetBlendedValue(atElapsedTicks, out blendedValue, out didExtrapolatePastMostRecentChanges))
            {
                //GONetLog.Debug($"grease got first blendo");
                return true;
            }

            //if (valueMonitoringSupport.mostRecentChanges_usedSize > 0) GONetLog.Debug($"grease count: {valueMonitoringSupport.mostRecentChanges_usedSize}, gross capacity: {valueMonitoringSupport.mostRecentChanges_capacitySize}");
            // If the above does not yield good value blend, then the below will attmept to use the default implementations available.
            // NOTE: This would most likely be due to the valueMonitoringSupport profile/template not having identified an implementation of IGONetAutoMagicalSync_CustomValueBlending for the type of blendedValue.
            if (valueMonitoringSupport.mostRecentChanges_usedSize > 0)
            {
                IGONetAutoMagicalSync_CustomValueBlending customBlending;
                if (defaultValueBlendings_byValueType.TryGetValue(valueMonitoringSupport.mostRecentChanges[0].numericValue.GONetSyncType, out customBlending))
                {
                    return customBlending.TryGetBlendedValue(valueMonitoringSupport.mostRecentChanges, valueMonitoringSupport.mostRecentChanges_usedSize, atElapsedTicks, out blendedValue, out didExtrapolatePastMostRecentChanges);
                }
            }

            didExtrapolatePastMostRecentChanges = false;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCountFromStarting, out Vector3 averageAcceleration, int iStarting = 0)
        {
            if (valueCountFromStarting <= 1)
            {
                averageAcceleration = Vector3.zero;
                return false;
            }

            // Pin array once for maximum performance
            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Component accumulators - no Vector3 struct operations
                float totalVelocity_x = 0f;
                float totalVelocity_y = 0f;
                float totalVelocity_z = 0f;
                float totalSeconds = 0f;

                int endIndex = iStarting + valueCountFromStarting - 1;

                // Optimized loop with direct pointer access
                for (int i = iStarting; i < endIndex; ++i)
                {
                    // Direct pointers to Vector3 components (offset 1 byte for GONetSyncType)
                    float* snap1_components = (float*)((byte*)&bufferPtr[i].numericValue + 1);
                    float* snap2_components = (float*)((byte*)&bufferPtr[i + 1].numericValue + 1);

                    // Direct component access - no property calls or struct copies
                    float snap1_x = snap1_components[0];
                    float snap1_y = snap1_components[1];
                    float snap1_z = snap1_components[2];

                    float snap2_x = snap2_components[0];
                    float snap2_y = snap2_components[1];
                    float snap2_z = snap2_components[2];

                    // Accumulate velocity components directly
                    totalVelocity_x += snap1_x - snap2_x;
                    totalVelocity_y += snap1_y - snap2_y;
                    totalVelocity_z += snap1_z - snap2_z;

                    // Direct tick access and optimized conversion
                    totalSeconds += (bufferPtr[i].elapsedTicksAtChange - bufferPtr[i + 1].elapsedTicksAtChange) * 1e-7f;
                }

                // Early exit check
                if (totalSeconds <= 0f)
                {
                    averageAcceleration = Vector3.zero;
                    return false;
                }

                // Single division with reciprocal multiplication
                float invTotalSeconds = 1f / totalSeconds;

                // Component-wise final calculation
                averageAcceleration = new Vector3(
                    totalVelocity_x * invTotalSeconds,
                    totalVelocity_y * invTotalSeconds,
                    totalVelocity_z * invTotalSeconds
                );

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector2 averageAcceleration)
        {
            if (valueCount <= 1)
            {
                averageAcceleration = Vector2.zero;
                return false;
            }

            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Component accumulators - no Vector2 struct operations
                float totalVelocity_x = 0f;
                float totalVelocity_y = 0f;
                float totalSeconds = 0f;

                for (int i = 0; i < valueCount - 1; ++i)
                {
                    // Direct pointers to Vector2 components (offset 1 byte for GONetSyncType)
                    float* val1_components = (float*)((byte*)&bufferPtr[i].numericValue + 1);
                    float* val2_components = (float*)((byte*)&bufferPtr[i + 1].numericValue + 1);

                    // Direct component access
                    float val1_x = val1_components[0];
                    float val1_y = val1_components[1];
                    float val2_x = val2_components[0];
                    float val2_y = val2_components[1];

                    // Accumulate velocity components
                    totalVelocity_x += val1_x - val2_x;
                    totalVelocity_y += val1_y - val2_y;

                    // Direct tick access and optimized conversion
                    totalSeconds += (bufferPtr[i].elapsedTicksAtChange - bufferPtr[i + 1].elapsedTicksAtChange) * 1e-7f;
                }

                if (totalSeconds <= 0f)
                {
                    averageAcceleration = Vector2.zero;
                    return false;
                }

                // Single division with reciprocal
                float invTotalSeconds = 1f / totalSeconds;
                averageAcceleration = new Vector2(
                    totalVelocity_x * invTotalSeconds,
                    totalVelocity_y * invTotalSeconds
                );

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector4 averageAcceleration)
        {
            if (valueCount <= 1)
            {
                averageAcceleration = Vector4.zero;
                return false;
            }

            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Component accumulators - no Vector4 struct operations
                float totalVelocity_x = 0f;
                float totalVelocity_y = 0f;
                float totalVelocity_z = 0f;
                float totalVelocity_w = 0f;
                float totalSeconds = 0f;

                for (int i = 0; i < valueCount - 1; ++i)
                {
                    // Direct pointers to Vector4 components (offset 1 byte for GONetSyncType)
                    float* val1_components = (float*)((byte*)&bufferPtr[i].numericValue + 1);
                    float* val2_components = (float*)((byte*)&bufferPtr[i + 1].numericValue + 1);

                    // Direct component access
                    float val1_x = val1_components[0];
                    float val1_y = val1_components[1];
                    float val1_z = val1_components[2];
                    float val1_w = val1_components[3];
                    float val2_x = val2_components[0];
                    float val2_y = val2_components[1];
                    float val2_z = val2_components[2];
                    float val2_w = val2_components[3];

                    // Accumulate velocity components
                    totalVelocity_x += val1_x - val2_x;
                    totalVelocity_y += val1_y - val2_y;
                    totalVelocity_z += val1_z - val2_z;
                    totalVelocity_w += val1_w - val2_w;

                    // Direct tick access and optimized conversion
                    totalSeconds += (bufferPtr[i].elapsedTicksAtChange - bufferPtr[i + 1].elapsedTicksAtChange) * 1e-7f;
                }

                if (totalSeconds <= 0f)
                {
                    averageAcceleration = Vector4.zero;
                    return false;
                }

                // Single division with reciprocal
                float invTotalSeconds = 1f / totalSeconds;
                averageAcceleration = new Vector4(
                    totalVelocity_x * invTotalSeconds,
                    totalVelocity_y * invTotalSeconds,
                    totalVelocity_z * invTotalSeconds,
                    totalVelocity_w * invTotalSeconds
                );

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool DetermineTimeBetweenStats(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out float min, out float average, out float max)
        {
            if (valueCount <= 1)
            {
                min = float.MaxValue;
                max = float.MinValue;
                average = -1;
                return false;
            }

            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                min = float.MaxValue;
                max = float.MinValue;
                float total = 0f;

                // Optimized tick to milliseconds conversion constant
                const float TICKS_TO_MILLIS = 1e-4f; // 1e-7 * 1000

                for (int i = 0; i < valueCount - 1; ++i)
                {
                    // Direct tick access and optimized conversion
                    float millis_1 = bufferPtr[i].elapsedTicksAtChange * TICKS_TO_MILLIS;
                    float millis_2 = bufferPtr[i + 1].elapsedTicksAtChange * TICKS_TO_MILLIS;
                    float diffMillis = millis_1 - millis_2;

                    total += diffMillis;
                    if (diffMillis < min) min = diffMillis;
                    if (diffMillis > max) max = diffMillis;
                }

                average = total / (valueCount - 1);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector3 GetVector3AvgAccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest)
        {
            Vector3 averageAcceleration;
            if (TryDetermineAverageAccelerationPerSecond(valueBuffer, valueCount, out averageAcceleration, 0))
            {
                fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                {
                    // Direct pointer access to Vector3 components
                    float* q1_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 1].numericValue + 1);
                    float* q2_components = (float*)((byte*)&newest.numericValue + 1);

                    // Direct component access
                    float q1_x = q1_components[0], q1_y = q1_components[1], q1_z = q1_components[2];
                    float q2_x = q2_components[0], q2_y = q2_components[1], q2_z = q2_components[2];

                    // Component-wise difference
                    float diff_x = q2_x - q1_x;
                    float diff_y = q2_y - q1_y;
                    float diff_z = q2_z - q1_z;

                    // Direct tick access and optimized conversion
                    float diff_q2_q1_seconds = (newest.elapsedTicksAtChange - bufferPtr[newestBufferIndex + 1].elapsedTicksAtChange) * 1e-7f;
                    float atMinusNewest_seconds = (atElapsedTicks - newest.elapsedTicksAtChange) * 1e-7f;

                    // Guard against division by zero
                    if (diff_q2_q1_seconds <= 0f)
                    {
                        return new Vector3(q2_x, q2_y, q2_z);
                    }

                    // Component-wise velocity calculation
                    float inv_diff_seconds = 1f / diff_q2_q1_seconds;
                    float vel_x = diff_x * inv_diff_seconds;
                    float vel_y = diff_y * inv_diff_seconds;
                    float vel_z = diff_z * inv_diff_seconds;

                    // Physics equation: s = s0 + v0*t + 0.5*a*t²
                    float halfTime_squared = 0.5f * atMinusNewest_seconds * atMinusNewest_seconds;

                    return new Vector3(
                        q2_x + (vel_x * atMinusNewest_seconds) + (averageAcceleration.x * halfTime_squared),
                        q2_y + (vel_y * atMinusNewest_seconds) + (averageAcceleration.y * halfTime_squared),
                        q2_z + (vel_z * atMinusNewest_seconds) + (averageAcceleration.z * halfTime_squared)
                    );
                }
            }

            throw new Exception("booboo");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector2 GetVector2AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector2 acceleration)
        {
            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Direct pointers to Vector2 components (offset 1 byte for GONetSyncType)
                float* q0_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 2].numericValue + 1);
                float* q1_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 1].numericValue + 1);
                float* q2_components = (float*)((byte*)&newest.numericValue + 1);

                // Direct component access
                float q0_x = q0_components[0], q0_y = q0_components[1];
                float q1_x = q1_components[0], q1_y = q1_components[1];
                float q2_x = q2_components[0], q2_y = q2_components[1];

                // Direct tick access
                long q2_ticks = newest.elapsedTicksAtChange;
                long q1_ticks = bufferPtr[newestBufferIndex + 1].elapsedTicksAtChange;
                long q0_ticks = bufferPtr[newestBufferIndex + 2].elapsedTicksAtChange;

                // Optimized time calculations
                const float TICKS_TO_SECONDS = 1e-7f;
                float diff_q2_q1_seconds = (q2_ticks - q1_ticks) * TICKS_TO_SECONDS;
                float diff_q1_q0_seconds = (q1_ticks - q0_ticks) * TICKS_TO_SECONDS;
                float atMinusNewest_seconds = (atElapsedTicks - q2_ticks) * TICKS_TO_SECONDS;

                // Guard against invalid time differences
                if (diff_q2_q1_seconds <= 0f || diff_q1_q0_seconds <= 0f)
                {
                    acceleration = Vector2.zero;
                    return new Vector2(q2_x, q2_y);
                }

                // Pre-calculate reciprocals
                float inv_diff_q2_q1 = 1f / diff_q2_q1_seconds;
                float inv_diff_q1_q0 = 1f / diff_q1_q0_seconds;

                // Component-wise velocity calculations
                float vel_q2_q1_x = (q2_x - q1_x) * inv_diff_q2_q1;
                float vel_q2_q1_y = (q2_y - q1_y) * inv_diff_q2_q1;
                float vel_q1_q0_x = (q1_x - q0_x) * inv_diff_q1_q0;
                float vel_q1_q0_y = (q1_y - q0_y) * inv_diff_q1_q0;

                // Acceleration components
                float accel_x = vel_q2_q1_x - vel_q1_q0_x;
                float accel_y = vel_q2_q1_y - vel_q1_q0_y;

                acceleration = new Vector2(accel_x, accel_y);

                // Physics equation: s = s0 + v0*t + 0.5*a*t²
                float halfTime_squared = 0.5f * atMinusNewest_seconds * atMinusNewest_seconds;

                return new Vector2(
                    q2_x + (vel_q2_q1_x * atMinusNewest_seconds) + (accel_x * halfTime_squared),
                    q2_y + (vel_q2_q1_y * atMinusNewest_seconds) + (accel_y * halfTime_squared)
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector4 GetVector4AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector4 acceleration)
        {
            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Direct pointers to Vector4 components (offset 1 byte for GONetSyncType)
                float* q0_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 2].numericValue + 1);
                float* q1_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 1].numericValue + 1);
                float* q2_components = (float*)((byte*)&newest.numericValue + 1);

                // Direct component access
                float q0_x = q0_components[0], q0_y = q0_components[1], q0_z = q0_components[2], q0_w = q0_components[3];
                float q1_x = q1_components[0], q1_y = q1_components[1], q1_z = q1_components[2], q1_w = q1_components[3];
                float q2_x = q2_components[0], q2_y = q2_components[1], q2_z = q2_components[2], q2_w = q2_components[3];

                // Direct tick access
                long q2_ticks = newest.elapsedTicksAtChange;
                long q1_ticks = bufferPtr[newestBufferIndex + 1].elapsedTicksAtChange;
                long q0_ticks = bufferPtr[newestBufferIndex + 2].elapsedTicksAtChange;

                // Optimized time calculations
                const float TICKS_TO_SECONDS = 1e-7f;
                float diff_q2_q1_seconds = (q2_ticks - q1_ticks) * TICKS_TO_SECONDS;
                float diff_q1_q0_seconds = (q1_ticks - q0_ticks) * TICKS_TO_SECONDS;
                float atMinusNewest_seconds = (atElapsedTicks - q2_ticks) * TICKS_TO_SECONDS;

                // Guard against invalid time differences
                if (diff_q2_q1_seconds <= 0f || diff_q1_q0_seconds <= 0f)
                {
                    acceleration = Vector4.zero;
                    return new Vector4(q2_x, q2_y, q2_z, q2_w);
                }

                // Pre-calculate reciprocals
                float inv_diff_q2_q1 = 1f / diff_q2_q1_seconds;
                float inv_diff_q1_q0 = 1f / diff_q1_q0_seconds;

                // Component-wise velocity calculations
                float vel_q2_q1_x = (q2_x - q1_x) * inv_diff_q2_q1;
                float vel_q2_q1_y = (q2_y - q1_y) * inv_diff_q2_q1;
                float vel_q2_q1_z = (q2_z - q1_z) * inv_diff_q2_q1;
                float vel_q2_q1_w = (q2_w - q1_w) * inv_diff_q2_q1;

                float vel_q1_q0_x = (q1_x - q0_x) * inv_diff_q1_q0;
                float vel_q1_q0_y = (q1_y - q0_y) * inv_diff_q1_q0;
                float vel_q1_q0_z = (q1_z - q0_z) * inv_diff_q1_q0;
                float vel_q1_q0_w = (q1_w - q0_w) * inv_diff_q1_q0;

                // Acceleration components
                float accel_x = vel_q2_q1_x - vel_q1_q0_x;
                float accel_y = vel_q2_q1_y - vel_q1_q0_y;
                float accel_z = vel_q2_q1_z - vel_q1_q0_z;
                float accel_w = vel_q2_q1_w - vel_q1_q0_w;

                acceleration = new Vector4(accel_x, accel_y, accel_z, accel_w);

                // Physics equation: s = s0 + v0*t + 0.5*a*t²
                float halfTime_squared = 0.5f * atMinusNewest_seconds * atMinusNewest_seconds;

                return new Vector4(
                    q2_x + (vel_q2_q1_x * atMinusNewest_seconds) + (accel_x * halfTime_squared),
                    q2_y + (vel_q2_q1_y * atMinusNewest_seconds) + (accel_y * halfTime_squared),
                    q2_z + (vel_q2_q1_z * atMinusNewest_seconds) + (accel_z * halfTime_squared),
                    q2_w + (vel_q2_q1_w * atMinusNewest_seconds) + (accel_w * halfTime_squared)
                );
            }
        }

        // Optimized Bezier functions with manual component calculations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetQuadraticBezierValue(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            float u_squared = u * u;
            float t_squared = t * t;
            float two_u_t = 2f * u * t;

            return new Vector3(
                (u_squared * p0.x) + (two_u_t * p1.x) + (t_squared * p2.x),
                (u_squared * p0.y) + (two_u_t * p1.y) + (t_squared * p2.y),
                (u_squared * p0.z) + (two_u_t * p1.z) + (t_squared * p2.z)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 GetQuadraticBezierValue(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            float u_squared = u * u;
            float t_squared = t * t;
            float two_u_t = 2f * u * t;

            return new Vector2(
                (u_squared * p0.x) + (two_u_t * p1.x) + (t_squared * p2.x),
                (u_squared * p0.y) + (two_u_t * p1.y) + (t_squared * p2.y)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 GetQuadraticBezierValue(Vector4 p0, Vector4 p1, Vector4 p2, float t)
        {
            float u = 1f - t;
            float u_squared = u * u;
            float t_squared = t * t;
            float two_u_t = 2f * u * t;

            return new Vector4(
                (u_squared * p0.x) + (two_u_t * p1.x) + (t_squared * p2.x),
                (u_squared * p0.y) + (two_u_t * p1.y) + (t_squared * p2.y),
                (u_squared * p0.z) + (two_u_t * p1.z) + (t_squared * p2.z),
                (u_squared * p0.w) + (two_u_t * p1.w) + (t_squared * p2.w)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector3 GetVector3AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector3 acceleration)
        {
            // Get direct pointers to Vector3 data - zero overhead access
            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                // Direct pointer arithmetic to Vector3 data (offset 1 byte for GONetSyncType)
                float* q0_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 2].numericValue + 1);
                float* q1_components = (float*)((byte*)&bufferPtr[newestBufferIndex + 1].numericValue + 1);
                float* q2_components = (float*)((byte*)&newest.numericValue + 1);

                // Direct component access - no struct copies whatsoever
                float q0_x = q0_components[0], q0_y = q0_components[1], q0_z = q0_components[2];
                float q1_x = q1_components[0], q1_y = q1_components[1], q1_z = q1_components[2];
                float q2_x = q2_components[0], q2_y = q2_components[1], q2_z = q2_components[2];

                // Direct tick access from structs (no property calls)
                long q2_ticks = newest.elapsedTicksAtChange;
                long q1_ticks = bufferPtr[newestBufferIndex + 1].elapsedTicksAtChange;
                long q0_ticks = bufferPtr[newestBufferIndex + 2].elapsedTicksAtChange;

                // Optimized time calculations (compile-time constant)
                const float TICKS_TO_SECONDS = 1e-7f;
                float diff_q2_q1_seconds = (q2_ticks - q1_ticks) * TICKS_TO_SECONDS;
                float diff_q1_q0_seconds = (q1_ticks - q0_ticks) * TICKS_TO_SECONDS;
                float atMinusNewest_seconds = (atElapsedTicks - q2_ticks) * TICKS_TO_SECONDS;

                // Early exit for invalid time differences
                if (diff_q2_q1_seconds <= 0f || diff_q1_q0_seconds <= 0f)
                {
                    acceleration = new Vector3(0f, 0f, 0f);
                    return new Vector3(q2_x, q2_y, q2_z);
                }

                // Pre-calculate reciprocals (multiplication is faster than division)
                float inv_diff_q2_q1 = 1f / diff_q2_q1_seconds;
                float inv_diff_q1_q0 = 1f / diff_q1_q0_seconds;

                // Manual component-wise velocity calculations (no Vector3 operators)
                float vel_q2_q1_x = (q2_x - q1_x) * inv_diff_q2_q1;
                float vel_q2_q1_y = (q2_y - q1_y) * inv_diff_q2_q1;
                float vel_q2_q1_z = (q2_z - q1_z) * inv_diff_q2_q1;

                float vel_q1_q0_x = (q1_x - q0_x) * inv_diff_q1_q0;
                float vel_q1_q0_y = (q1_y - q0_y) * inv_diff_q1_q0;
                float vel_q1_q0_z = (q1_z - q0_z) * inv_diff_q1_q0;

                // Acceleration components (no Vector3 subtraction)
                float accel_x = vel_q2_q1_x - vel_q1_q0_x;
                float accel_y = vel_q2_q1_y - vel_q1_q0_y;
                float accel_z = vel_q2_q1_z - vel_q1_q0_z;

                // Set output acceleration
                acceleration = new Vector3(accel_x, accel_y, accel_z);

                // Physics equation: s = s0 + v0*t + 0.5*a*t² (manual component calculation)
                float halfTime_squared = 0.5f * atMinusNewest_seconds * atMinusNewest_seconds;

                // Final position calculation - all component-wise for maximum performance
                return new Vector3(
                    q2_x + (vel_q2_q1_x * atMinusNewest_seconds) + (accel_x * halfTime_squared),
                    q2_y + (vel_q2_q1_y * atMinusNewest_seconds) + (accel_y * halfTime_squared),
                    q2_z + (vel_q2_q1_z * atMinusNewest_seconds) + (accel_z * halfTime_squared)
                );
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Quaternion_ApplySmoothing_IfAppropriate(ref NumericValueChangeSnapshot[] valueBuffer, int valueCount, ref GONetSyncableValue blendedValue)
        {
            const int MIN_VALUE_COUNT_FOR_NORMAL_OPERATION = 3;
            const float ANGULAR_VELOCITY_CHANGE_THRESHOLD = 45f; // Degrees per second change
            const float AT_REST_ANGULAR_VELOCITY_THRESHOLD = 5f; // Degrees per second
            const bool ShouldLog = false;

            // Always calculate smoothed value to maintain state
            var smoothedValue = GetSmoothedRotation(blendedValue.UnityEngine_Quaternion, valueBuffer, valueCount);

            bool shouldApplySmoothing = false;
            float smoothingStrength = 1.0f;

            // Detect at-rest transitions
            bool isLikelyAtRestTransition = false;
            if (valueCount == 1)
            {
                isLikelyAtRestTransition = true;
            }
            else if (valueCount == 2)
            {
                unsafe
                {
                    fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                    {
                        Quaternion* q0 = (Quaternion*)((byte*)&bufferPtr[0].numericValue + 1);
                        Quaternion* q1 = (Quaternion*)((byte*)&bufferPtr[1].numericValue + 1);

                        // Calculate angular velocity
                        float angle = Quaternion.Angle(*q0, *q1);
                        long timeDelta = bufferPtr[0].elapsedTicksAtChange - bufferPtr[1].elapsedTicksAtChange;

                        if (timeDelta > 0)
                        {
                            float deltaTime = (float)(timeDelta * 1e-7);
                            float angularVelocity = angle / deltaTime;

                            if (angularVelocity < AT_REST_ANGULAR_VELOCITY_THRESHOLD)
                            {
                                isLikelyAtRestTransition = true;
                            }
                        }
                    }
                }
            }

            // Condition 1: Low value count or at-rest
            if (valueCount <= MIN_VALUE_COUNT_FOR_NORMAL_OPERATION || isLikelyAtRestTransition)
            {
                shouldApplySmoothing = true;
                if (ShouldLog) GONetLog.Debug($"Applying quaternion smoothing: low value count ({valueCount}) or at-rest");
            }
            // Condition 2: Sudden angular velocity change
            else if (valueCount >= 3)
            {
                unsafe
                {
                    fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                    {
                        Quaternion* q0 = (Quaternion*)((byte*)&bufferPtr[0].numericValue + 1);
                        Quaternion* q1 = (Quaternion*)((byte*)&bufferPtr[1].numericValue + 1);
                        Quaternion* q2 = (Quaternion*)((byte*)&bufferPtr[2].numericValue + 1);

                        // Calculate angular velocities
                        float angle1 = Quaternion.Angle(*q0, *q1);
                        float angle2 = Quaternion.Angle(*q1, *q2);

                        long timeDelta1 = bufferPtr[0].elapsedTicksAtChange - bufferPtr[1].elapsedTicksAtChange;
                        long timeDelta2 = bufferPtr[1].elapsedTicksAtChange - bufferPtr[2].elapsedTicksAtChange;

                        if (timeDelta1 > 0 && timeDelta2 > 0)
                        {
                            float angVel1 = angle1 / ((float)(timeDelta1 * 1e-7));
                            float angVel2 = angle2 / ((float)(timeDelta2 * 1e-7));

                            float angVelChange = MathF.Abs(angVel1 - angVel2);

                            if (angVelChange > ANGULAR_VELOCITY_CHANGE_THRESHOLD)
                            {
                                shouldApplySmoothing = true;
                                smoothingStrength = angVelChange / 90f < 1.0f ? angVelChange / 90f : 1.0f;
                                if (ShouldLog) GONetLog.Debug($"Applying quaternion smoothing: angular velocity spike {angVelChange:F1}°/s");
                            }
                        }

                        // Check for axis flip (gimbal lock avoidance)
                        Vector3 axis1, axis2;
                        float angleOut;
                        (*q1 * Quaternion.Inverse(*q0)).ToAngleAxis(out angleOut, out axis1);
                        (*q2 * Quaternion.Inverse(*q1)).ToAngleAxis(out angleOut, out axis2);

                        float axisDot = Vector3.Dot(axis1, axis2);
                        if (axisDot < 0.5f) // Significant axis change
                        {
                            shouldApplySmoothing = true;
                            if (ShouldLog) GONetLog.Debug($"Applying quaternion smoothing: axis flip detected");
                        }
                    }
                }
            }

            // Condition 3: Significant difference from smoothed
            if (!shouldApplySmoothing)
            {
                float angleFromSmoothed = Quaternion.Angle(smoothedValue, blendedValue.UnityEngine_Quaternion);
                const float SMOOTHING_ANGLE_THRESHOLD = 5f; // degrees

                if (angleFromSmoothed > SMOOTHING_ANGLE_THRESHOLD)
                {
                    shouldApplySmoothing = true;
                    smoothingStrength = angleFromSmoothed / 15f < 1.0f ? angleFromSmoothed / 15f : 1.0f;
                    if (ShouldLog) GONetLog.Debug($"Applying quaternion smoothing: angle delta {angleFromSmoothed:F1}°");
                }
            }

            // Apply smoothing
            if (shouldApplySmoothing)
            {
                if (smoothingStrength < 1.0f)
                {
                    blendedValue = Quaternion.Slerp(blendedValue.UnityEngine_Quaternion, smoothedValue, smoothingStrength);
                }
                else
                {
                    blendedValue = smoothedValue;
                }
            }

            return shouldApplySmoothing;
        }

        static readonly ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Vector3>>> GetSmoothedVector3_m_outputs_byBufferByThread =
            new ConcurrentDictionary<Thread, Dictionary<NumericValueChangeSnapshot[], List<Vector3>>>();
        static readonly ConcurrentDictionary<Thread, List<Vector3>> GetSmoothedVector3_m_inputs_byThread = new ConcurrentDictionary<Thread, List<Vector3>>();

        // Optimized GetSmoothedVector3 with reduced allocations and better cache usage
        internal static Vector3 GetSmoothedVector3(Vector3 mostRecentValue, NumericValueChangeSnapshot[] olderValuesBuffer, int bufferCount)
        {
            // Thread-local storage access
            List<Vector3> GetSmoothedVector3_m_inputs;
            if (!GetSmoothedVector3_m_inputs_byThread.TryGetValue(Thread.CurrentThread, out GetSmoothedVector3_m_inputs))
            {
                GetSmoothedVector3_m_inputs_byThread[Thread.CurrentThread] = GetSmoothedVector3_m_inputs = new List<Vector3>(SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT + 1);
            }
            else
            {
                GetSmoothedVector3_m_inputs.Clear();
            }

            // Use unsafe access for better performance when adding to inputs
            unsafe
            {
                fixed (NumericValueChangeSnapshot* bufferPtr = olderValuesBuffer)
                {
                    // Add values in reverse order more efficiently
                    for (int i = bufferCount - 1; i >= 0; --i)
                    {
                        // Direct pointer access to Vector3 data
                        Vector3* vecPtr = (Vector3*)((byte*)&bufferPtr[i].numericValue + 1);
                        GetSmoothedVector3_m_inputs.Add(*vecPtr);
                    }
                }
            }

            // Ensure minimum required inputs with optimized filling
            int currentCount = GetSmoothedVector3_m_inputs.Count;
            int fillCount = currentCount < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT ?
                            SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT - currentCount : 0;

            if (fillCount > 0)
            {
                for (int i = 0; i < fillCount; ++i)
                {
                    GetSmoothedVector3_m_inputs.Add(mostRecentValue);
                }
            }
            else
            {
                GetSmoothedVector3_m_inputs.Add(mostRecentValue);
            }

            // Output buffer management
            Dictionary<NumericValueChangeSnapshot[], List<Vector3>> GetSmoothedVector3_m_outputs_byBuffer;
            if (!GetSmoothedVector3_m_outputs_byBufferByThread.TryGetValue(Thread.CurrentThread, out GetSmoothedVector3_m_outputs_byBuffer))
            {
                GetSmoothedVector3_m_outputs_byBufferByThread[Thread.CurrentThread] = GetSmoothedVector3_m_outputs_byBuffer = new Dictionary<NumericValueChangeSnapshot[], List<Vector3>>();
            }

            List<Vector3> GetSmoothedVector3_m_outputs;
            if (!GetSmoothedVector3_m_outputs_byBuffer.TryGetValue(olderValuesBuffer, out GetSmoothedVector3_m_outputs))
            {
                GetSmoothedVector3_m_outputs_byBuffer[olderValuesBuffer] = GetSmoothedVector3_m_outputs = new List<Vector3>(SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT + 1);
            }
            else
            {
                // Optimized trimming - only remove what's necessary
                int targetCount = GetSmoothedVector3_m_inputs.Count;
                if (GetSmoothedVector3_m_outputs.Count > targetCount)
                {
                    GetSmoothedVector3_m_outputs.RemoveRange(0, GetSmoothedVector3_m_outputs.Count - targetCount);
                }
            }

            // Fill outputs if needed
            int outputCount = GetSmoothedVector3_m_outputs.Count;
            if (outputCount < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT)
            {
                int fillCount2 = SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT - outputCount;
                for (int i = 0; i < fillCount2; ++i)
                {
                    GetSmoothedVector3_m_outputs.Add(mostRecentValue);
                }
            }

            // Optimized calculation using component-wise operations to avoid Vector3 operator overhead
            float result_x = 0f, result_y = 0f, result_z = 0f;

            int iLastInput = GetSmoothedVector3_m_inputs.Count - 1;

            // Process inputs - unroll for common case of 3 inputs
            if (SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT == 3)
            {
                Vector3 v0 = GetSmoothedVector3_m_inputs[iLastInput];
                Vector3 v1 = GetSmoothedVector3_m_inputs[iLastInput - 1];
                Vector3 v2 = GetSmoothedVector3_m_inputs[iLastInput - 2];

                float w0 = SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[0];
                float w1 = SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[1];
                float w2 = SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[2];

                result_x = v0.x * w0 + v1.x * w1 + v2.x * w2;
                result_y = v0.y * w0 + v1.y * w1 + v2.y * w2;
                result_z = v0.z * w0 + v1.z * w1 + v2.z * w2;
            }
            else
            {
                // Fallback for different configurations
                for (int i = 0; i < SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    Vector3 v = GetSmoothedVector3_m_inputs[iLastInput - i];
                    float weight = SMOOTHING_INPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    result_x += v.x * weight;
                    result_y += v.y * weight;
                    result_z += v.z * weight;
                }
            }

            int iLastOutput = GetSmoothedVector3_m_outputs.Count - 1;

            // Process outputs - unroll for common case of 2 outputs
            if (SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT == 2)
            {
                Vector3 v0 = GetSmoothedVector3_m_outputs[iLastOutput];
                Vector3 v1 = GetSmoothedVector3_m_outputs[iLastOutput - 1];

                float w0 = SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[0];
                float w1 = SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[1];

                result_x += v0.x * w0 + v1.x * w1;
                result_y += v0.y * w0 + v1.y * w1;
                result_z += v0.z * w0 + v1.z * w1;
            }
            else
            {
                // Fallback for different configurations
                for (int i = 0; i < SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES_COUNT; ++i)
                {
                    Vector3 v = GetSmoothedVector3_m_outputs[iLastOutput - i];
                    float weight = SMOOTHING_OUTPUTS_HISTORY_EFFECTOR_PERCENTAGES[i];
                    result_x += v.x * weight;
                    result_y += v.y * weight;
                    result_z += v.z * weight;
                }
            }

            Vector3 result = new Vector3(result_x, result_y, result_z);
            GetSmoothedVector3_m_outputs.Add(result);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Vector3_ApplySmoothing_IfAppropriate(ref NumericValueChangeSnapshot[] valueBuffer, int valueCount, ref GONetSyncableValue blendedValue)
        {
            // Add these constants at the class level
            const int MIN_VALUE_COUNT_FOR_NORMAL_OPERATION = 3; // Below this, we're likely at rest or starting motion
            const float DIRECTION_CHANGE_THRESHOLD = 90f; // Degrees - for detecting sharp turns
            const float VELOCITY_CHANGE_THRESHOLD = 0.5f; // Normalized velocity change threshold

            // Always calculate smoothed value to maintain good state data
            var smoothedValue = GetSmoothedVector3(blendedValue.UnityEngine_Vector3, valueBuffer, valueCount);

            // Determine if we should apply smoothing
            bool shouldApplySmoothing = false;
            float smoothingStrength = 1.0f; // Full smoothing by default when applied

            // Detect at-rest transitions by analyzing the value pattern
            bool isLikelyAtRestTransition = false;
            if (valueCount == 1)
            {
                // Single value often indicates just came to rest or just started moving
                isLikelyAtRestTransition = true;
            }
            else if (valueCount == 2)
            {
                // Check if velocity is very low (potential at-rest)
                unsafe
                {
                    fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                    {
                        float* pos0 = (float*)((byte*)&bufferPtr[0].numericValue + 1);
                        float* pos1 = (float*)((byte*)&bufferPtr[1].numericValue + 1);

                        float dx = pos0[0] - pos1[0];
                        float dy = pos0[1] - pos1[1];
                        float dz = pos0[2] - pos1[2];
                        float distSqr = dx * dx + dy * dy + dz * dz;

                        long timeDelta = bufferPtr[0].elapsedTicksAtChange - bufferPtr[1].elapsedTicksAtChange;
                        if (timeDelta > 0)
                        {
                            float deltaTime = (float)(timeDelta * 1e-7); // Ticks to seconds
                            float velocitySqr = distSqr / (deltaTime * deltaTime);

                            // Very low velocity threshold for at-rest detection
                            const float AT_REST_VELOCITY_SQR_THRESHOLD = 0.01f; // 0.1 units/second squared
                            if (velocitySqr < AT_REST_VELOCITY_SQR_THRESHOLD)
                            {
                                isLikelyAtRestTransition = true;
                            }
                        }
                    }
                }
            }

            // Condition 1: Low value count or likely at-rest transitions
            if (valueCount <= MIN_VALUE_COUNT_FOR_NORMAL_OPERATION || isLikelyAtRestTransition)
            {
                shouldApplySmoothing = true;
                if (ShouldLog) GONetLog.Debug($"Applying smoothing due to low value count ({valueCount}) or at-rest transition");
            }
            // Condition 2: Sharp direction change detection with optimized calculations
            else if (valueCount >= 2)
            {
                // Use unsafe access for maximum performance
                unsafe
                {
                    fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                    {
                        // Direct component access for velocity calculations
                        float* snap0_components = (float*)((byte*)&bufferPtr[0].numericValue + 1);
                        float* snap1_components = (float*)((byte*)&bufferPtr[1].numericValue + 1);

                        long tickDelta1 = bufferPtr[0].elapsedTicksAtChange - bufferPtr[1].elapsedTicksAtChange;
                        if (tickDelta1 > 0)
                        {
                            float deltaTime1 = (float)(tickDelta1 * 1e-7); // Ticks to seconds
                            float invDeltaTime1 = 1.0f / deltaTime1;

                            // Component-wise velocity calculation
                            float vel_x = (snap0_components[0] - snap1_components[0]) * invDeltaTime1;
                            float vel_y = (snap0_components[1] - snap1_components[1]) * invDeltaTime1;
                            float vel_z = (snap0_components[2] - snap1_components[2]) * invDeltaTime1;

                            float currentVelSqrMag = vel_x * vel_x + vel_y * vel_y + vel_z * vel_z;

                            if (valueCount >= 3 && currentVelSqrMag > 0.01f)
                            {
                                float* snap2_components = (float*)((byte*)&bufferPtr[2].numericValue + 1);
                                long tickDelta2 = bufferPtr[1].elapsedTicksAtChange - bufferPtr[2].elapsedTicksAtChange;

                                if (tickDelta2 > 0)
                                {
                                    float deltaTime2 = (float)(tickDelta2 * 1e-7);
                                    float invDeltaTime2 = 1.0f / deltaTime2;

                                    float prevVel_x = (snap1_components[0] - snap2_components[0]) * invDeltaTime2;
                                    float prevVel_y = (snap1_components[1] - snap2_components[1]) * invDeltaTime2;
                                    float prevVel_z = (snap1_components[2] - snap2_components[2]) * invDeltaTime2;

                                    float prevVelSqrMag = prevVel_x * prevVel_x + prevVel_y * prevVel_y + prevVel_z * prevVel_z;

                                    if (prevVelSqrMag > 0.01f)
                                    {
                                        // Calculate angle using dot product (avoiding Vector3 construction)
                                        float dot = vel_x * prevVel_x + vel_y * prevVel_y + vel_z * prevVel_z;
                                        float currentVelMag = MathF.Sqrt(currentVelSqrMag);
                                        float prevVelMag = MathF.Sqrt(prevVelSqrMag);
                                        float cosAngle = dot / (currentVelMag * prevVelMag);

                                        // Clamp to valid range for Acos
                                        cosAngle = cosAngle > 1.0f ? 1.0f : (cosAngle < -1.0f ? -1.0f : cosAngle);
                                        float angle = MathF.Acos(cosAngle) * 57.29578f; // Radians to degrees

                                        if (angle > DIRECTION_CHANGE_THRESHOLD)
                                        {
                                            shouldApplySmoothing = true;
                                            // Scale smoothing based on angle severity
                                            smoothingStrength = angle / 180.0f < 1.0f ? angle / 180.0f : 1.0f;
                                            if (ShouldLog) GONetLog.Debug($"Applying smoothing due to sharp direction change: {angle:F1}°");
                                        }

                                        // Check for velocity spike
                                        float velDiff_x = vel_x - prevVel_x;
                                        float velDiff_y = vel_y - prevVel_y;
                                        float velDiff_z = vel_z - prevVel_z;
                                        float velocityChangeMag = MathF.Sqrt(velDiff_x * velDiff_x + velDiff_y * velDiff_y + velDiff_z * velDiff_z);
                                        float avgVelMag = (currentVelMag + prevVelMag) * 0.5f;

                                        if (avgVelMag > 0.01f)
                                        {
                                            float normalizedVelChange = velocityChangeMag / avgVelMag;
                                            if (normalizedVelChange > VELOCITY_CHANGE_THRESHOLD)
                                            {
                                                shouldApplySmoothing = true;
                                                if (ShouldLog) GONetLog.Debug($"Applying smoothing due to velocity spike: {normalizedVelChange:F2}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Condition 3: Check smoothing delta threshold
            if (!shouldApplySmoothing)
            {
                // Only apply smoothing if it would make a meaningful difference
                float deltaFromSmoothed = (smoothedValue - blendedValue.UnityEngine_Vector3).magnitude;
                const float SMOOTHING_DELTA_THRESHOLD = 0.05f; // Only smooth if difference is significant

                if (deltaFromSmoothed > SMOOTHING_DELTA_THRESHOLD)
                {
                    // Additional jitter check for edge cases
                    if (valueCount >= 3)
                    {
                        unsafe
                        {
                            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                            {
                                // Quick variance calculation using squared distances
                                float* pos0 = (float*)((byte*)&bufferPtr[0].numericValue + 1);
                                float* pos1 = (float*)((byte*)&bufferPtr[1].numericValue + 1);
                                float* pos2 = (float*)((byte*)&bufferPtr[2].numericValue + 1);

                                // Calculate two deltas
                                float d1_x = pos0[0] - pos1[0];
                                float d1_y = pos0[1] - pos1[1];
                                float d1_z = pos0[2] - pos1[2];

                                float d2_x = pos1[0] - pos2[0];
                                float d2_y = pos1[1] - pos2[1];
                                float d2_z = pos1[2] - pos2[2];

                                // Simple variance approximation
                                float diff_x = d1_x - d2_x;
                                float diff_y = d1_y - d2_y;
                                float diff_z = d1_z - d2_z;
                                float variance = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;

                                const float JITTER_VARIANCE_THRESHOLD = 0.1f;
                                if (variance > JITTER_VARIANCE_THRESHOLD)
                                {
                                    shouldApplySmoothing = true;
                                    smoothingStrength = deltaFromSmoothed / 0.2f < 1.0f ? deltaFromSmoothed / 0.2f : 1.0f; // Scale based on delta
                                    if (ShouldLog) GONetLog.Debug($"Applying smoothing due to jitter: variance={variance:F3}, delta={deltaFromSmoothed:F3}");
                                }
                            }
                        }
                    }
                }
            }

            // Apply smoothing with optional strength blending
            if (shouldApplySmoothing)
            {
                if (smoothingStrength < 1.0f)
                {
                    // Partial smoothing for gradual transitions
                    blendedValue = Vector3.Lerp(blendedValue.UnityEngine_Vector3, smoothedValue, smoothingStrength);
                }
                else
                {
                    blendedValue = smoothedValue;
                }
            }

            return shouldApplySmoothing;
        }
    }
}
