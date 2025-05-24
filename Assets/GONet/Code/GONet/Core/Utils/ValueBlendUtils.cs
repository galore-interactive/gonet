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
            //GONetLog.Debug($"grease");
            if (valueMonitoringSupport.TryGetBlendedValue(atElapsedTicks, out blendedValue, out didExtrapolate))
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
