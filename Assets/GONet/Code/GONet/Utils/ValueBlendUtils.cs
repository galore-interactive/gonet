/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet.Utils
{
    internal static class ValueBlendUtils
    {
        internal const int VALUE_COUNT_NEEDED_TO_EXTRAPOLATE = 2;

        internal static bool TryGetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport, long atElapsedTicks, out GONetSyncableValue blendedValue)
        {
            if (valueMonitoringSupport.mostRecentChanges_usedSize > 0)
            {
                GetBlendedValue getBlendedValue;
                if (getBlendedValues_byValueType.TryGetValue(valueMonitoringSupport.mostRecentChanges[0].numericValue.GONetSyncType, out getBlendedValue))
                {
                    return getBlendedValue(valueMonitoringSupport.mostRecentChanges, valueMonitoringSupport.mostRecentChanges_usedSize, atElapsedTicks, out blendedValue);
                }
            }

            blendedValue = default;
            return false;
        }

        static readonly Dictionary<GONetSyncableValueTypes, GetBlendedValue> getBlendedValues_byValueType = new Dictionary<GONetSyncableValueTypes, GetBlendedValue>(8)
        {
            { GONetSyncableValueTypes.System_Single, GetBlendedValue_Float },
            { GONetSyncableValueTypes.UnityEngine_Quaternion, GetBlendedValue_Quaternion },
            { GONetSyncableValueTypes.UnityEngine_Vector3, GetBlendedValue_Vector3 }
        };

        delegate bool GetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue);

        static bool GetBlendedValue_Float(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
        {
            blendedValue = default;

            if (valueCount > 0)
            {
                { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                    int newestBufferIndex = 0;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                    int oldestBufferIndex = valueCount - 1;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
                    bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) < GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;
                    if (isNewestRecentEnoughToProcess)
                    {
                        float newestValue = newest.numericValue.System_Single;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    float justBeforeNewest_numericValue = justBeforeNewest.numericValue.System_Single;
                                    float valueDiffBetweenLastTwo = newestValue - justBeforeNewest_numericValue;
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                    long extrapolated_TicksAtSend = newest.elapsedTicksAtChange + ticksBetweenLastTwo;
                                    float extrapolated_ValueNew = newestValue + valueDiffBetweenLastTwo;
                                    float interpolationTime = (atElapsedTicks - newest.elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest.elapsedTicksAtChange);

                                    float bezierTime = 0.5f + (interpolationTime / 2f);
                                    blendedValue = GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.System_Single;
                                //GONetLog.Debug("went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " mostRecentChanges_capacitySize: " + mostRecentChanges_capacitySize + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                bool didWeLoip = false;
                                for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                    if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (atElapsedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
                                        blendedValue = Mathf.Lerp(
                                            older.numericValue.System_Single,
                                            newer.numericValue.System_Single,
                                            interpolationTime);
                                        //GONetLog.Debug("we loip'd 'eem");
                                        didWeLoip = true;
                                        break;
                                    }
                                }
                                if (!didWeLoip)
                                {
                                    GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                                }
                            }
                        }
                        else
                        {
                            blendedValue = newestValue;
                            GONetLog.Debug("not a float?");
                        }
                    }
                    else
                    {
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(GONetMain.Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetQuadraticBezierValue(float p0, float p1, float p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        static bool GetBlendedValue_Quaternion(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
        {
            blendedValue = default;

            if (valueCount > 0)
            {
                { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                    int newestBufferIndex = 0;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                    int oldestBufferIndex = valueCount - 1;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
                    bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) < GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;
                    if (isNewestRecentEnoughToProcess)
                    {
                        Quaternion newestValue = newest.numericValue.UnityEngine_Quaternion;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    if (valueCount > 2000)
                                    {
                                        { // use velocity and acceleration presuming that it will be more accurate than regular interpolation
                                            
                                            //* as many quats:
                                            {
                                                int olderBufferIndex = newestBufferIndex + 1;
                                                Quaternion older_numericValue = valueBuffer[olderBufferIndex].numericValue.UnityEngine_Quaternion;
                                                long older_elapsedTicksAtChange = valueBuffer[olderBufferIndex].elapsedTicksAtChange;

                                                int oldererBufferIndex = olderBufferIndex + 1;
                                                Quaternion olderer_numericValue = valueBuffer[oldererBufferIndex].numericValue.UnityEngine_Quaternion;
                                                long olderer_elapsedTicksAtChange = valueBuffer[oldererBufferIndex].elapsedTicksAtChange;

                                                long time_newerOlder = newest.elapsedTicksAtChange - older_elapsedTicksAtChange;
                                                long time = atElapsedTicks - newest.elapsedTicksAtChange;

                                                Quaternion older_olderer_diff = Quaternion.Inverse(olderer_numericValue) * older_numericValue;
                                                Vector3 older_velocity = older_olderer_diff.eulerAngles / (older_elapsedTicksAtChange - olderer_elapsedTicksAtChange);

                                                Quaternion newest_older_diff = Quaternion.Inverse(older_numericValue) * newest.numericValue.UnityEngine_Quaternion;
                                                Vector3 newer_velocity = newest_older_diff.eulerAngles / time_newerOlder;
                                                Vector3 newer_acceleration = (newer_velocity - older_velocity) / time_newerOlder;

                                                Vector3 blended_velocity = newer_velocity + newer_acceleration * time;
                                                Vector3 blendedValue_eulers = newest.numericValue.UnityEngine_Quaternion.eulerAngles + blended_velocity * time;
                                                blendedValue = Quaternion.Euler(blendedValue_eulers);
                                            }
                                            //*/

                                            /* eulers
                                            int olderBufferIndex = newestBufferIndex + 1;
                                            Vector3 older_numericValue = valueBuffer[olderBufferIndex].numericValue.UnityEngine_Quaternion.eulerAngles;
                                            long older_elapsedTicksAtChange = valueBuffer[olderBufferIndex].elapsedTicksAtChange;

                                            int oldererBufferIndex = olderBufferIndex + 1;
                                            Vector3 olderer_numericValue = valueBuffer[oldererBufferIndex].numericValue.UnityEngine_Quaternion.eulerAngles;
                                            long olderer_elapsedTicksAtChange = valueBuffer[oldererBufferIndex].elapsedTicksAtChange;

                                            long time_newerOlder = newest.elapsedTicksAtChange - older_elapsedTicksAtChange;
                                            long time = atElapsedTicks - newest.elapsedTicksAtChange;

                                            Vector3 older_velocity = (older_numericValue - olderer_numericValue) / (older_elapsedTicksAtChange - olderer_elapsedTicksAtChange);
                                            Vector3 newer_velocity = (newest.numericValue.UnityEngine_Quaternion.eulerAngles - older_numericValue) / time_newerOlder;
                                            Vector3 newer_acceleration = (newer_velocity - older_velocity) / time_newerOlder;

                                            Vector3 blended_velocity = newer_velocity + newer_acceleration * time;
                                            Vector3 blendedValue_eulers = newest.numericValue.UnityEngine_Quaternion.eulerAngles + blended_velocity * time;
                                            blendedValue = Quaternion.Euler(blendedValue_eulers);
                                            */
                                            //GONetLog.Debug(string.Concat("we extroip_accelerated his ace...blended_velocity: ", blended_velocity.x, ",", blended_velocity.y, ",", blended_velocity.z, " newerAcceleration: ", newer_acceleration.x, ",", newer_acceleration.y, ",", newer_acceleration.z));
                                        }
                                    }
                                    else if (valueCount > 2)
                                    { // shaun's way of acceleration-based extrapolation of quaternions:
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
                                        Quaternion q0 = q0_snap.numericValue.UnityEngine_Quaternion;
                                        
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
                                        Quaternion q1 = q1_snap.numericValue.UnityEngine_Quaternion;

                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot q2_snap = newest;
                                        Quaternion q2 = newest.numericValue.UnityEngine_Quaternion;
                                        Quaternion diffRotation_q2_q1 = q2 * Quaternion.Inverse(q1);
                                        Quaternion diffRotation_q1_q0 = q1 * Quaternion.Inverse(q0);
                                        Quaternion diffDiff = diffRotation_q2_q1 * Quaternion.Inverse(diffRotation_q1_q0);
                                        Quaternion q3 = q2 * diffRotation_q2_q1 * diffDiff;
                                        long atMinusNewest_ticks = atElapsedTicks - q2_snap.elapsedTicksAtChange;
                                        long newestMinusJustBefore_ticks = q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange;
                                        float interpolationTime = atMinusNewest_ticks / (float)newestMinusJustBefore_ticks;
                                        blendedValue =
                                            QuaternionUtils.SlerpUnclamped(
                                                ref q2,
                                                ref q3,
                                                interpolationTime);
                                        
                                        /* just double checking some numbers to make sure the math is good
                                        GONetLog.Debug(string.Concat(
                                            "\nq0: ", q0.eulerAngles,
                                            "\nq1: ", q1.eulerAngles,
                                            "\nq2: ", q2.eulerAngles,
                                            "\nd2: ", diffRotation_q2_q1.eulerAngles,
                                            "\nd1: ", diffRotation_q1_q0.eulerAngles,
                                            "\ndd: ", diffDiff.eulerAngles,
                                            "\nsq: ", blendedValue.UnityEngine_Quaternion.eulerAngles,
                                            "\nq3: ", q3.eulerAngles,
                                            "\ninterpolationTime: ", interpolationTime,
                                            ", q0(ms): ", TimeSpan.FromTicks(q0_snap.elapsedTicksAtChange).TotalMilliseconds,
                                            ", q1(ms): ", TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange).TotalMilliseconds,
                                            ", q2(ms): ", TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange).TotalMilliseconds,
                                            ", at(ms): ", TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds,
                                            ", atMinusNewest(ms): ", TimeSpan.FromTicks(atMinusNewest_ticks).TotalMilliseconds));
                                        */
                                    }
                                    else
                                    {
                                        /* { // SQUAD life!  This is much better for dynamic movements, but is a bit more costly on CPU
                                            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                            Quaternion diffRotation = newestValue * Quaternion.Inverse(justBeforeNewest.numericValue.UnityEngine_Quaternion);
                                            long atMinusNewest_ticks = atElapsedTicks - newest.elapsedTicksAtChange;
                                            long newestMinusJustBefore_ticks = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;
                                            float interpolationTime_InitialRaw = atMinusNewest_ticks / (float)newestMinusJustBefore_ticks;
                                            Quaternion extrapolatedRotation = newestValue * diffRotation;

                                            float interpolationTime1 = 
                                                interpolationTime_InitialRaw > 1
                                                    ? interpolationTime_InitialRaw + 0.1f
                                                    : 1.1f; // ensure at least 1 so math below does not get whack (e.g., atInterpolationTime1_ticks)
                                            interpolationTime1 = 2.5f; // TODO get rid of me
                                            Quaternion extrapolated1 = 
                                                QuaternionUtils.SlerpUnclamped(
                                                    ref newestValue,
                                                    ref extrapolatedRotation,
                                                    interpolationTime1);

                                            float interpolationTime2 = interpolationTime1 * 2;
                                            Quaternion extrapolated2 =
                                                QuaternionUtils.SlerpUnclamped(
                                                    ref newestValue,
                                                    ref extrapolatedRotation,
                                                    interpolationTime2);

                                            Quaternion 
                                                q0 = justBeforeNewest.numericValue.UnityEngine_Quaternion, 
                                                q1 = newest.numericValue.UnityEngine_Quaternion, 
                                                q2 = extrapolated1, 
                                                q3 = extrapolated2;
                                            
                                            float atInterpolationTime1_ticks = newest.elapsedTicksAtChange + (atMinusNewest_ticks * interpolationTime1);
                                            float t1MinusNewest_ticks = atInterpolationTime1_ticks - newest.elapsedTicksAtChange;
                                            float interpolationTimeSquad = atMinusNewest_ticks / t1MinusNewest_ticks; // % between q1 and q2

                                            { // start working toward moving q0 backward to be equidistant in time from q1 as q1 is to q2
                                                float newQ0InterpolationBackwardTime = newestMinusJustBefore_ticks / (float)t1MinusNewest_ticks;
                                                newQ0InterpolationBackwardTime = 1 / newQ0InterpolationBackwardTime;
                                                q0 =
                                                    QuaternionUtils.SlerpUnclamped(
                                                        ref q1,
                                                        ref q0,
                                                        newQ0InterpolationBackwardTime);
                                            }

                                            // We want to interpolate between q1 and q2 by an interpolation factor t
                                            Quaternion q, a, b, p;
                                            QuaternionUtils.SquadSetup(ref q0, ref q1, ref q2, ref q3, out q, out a, out b, out p);
                                            blendedValue = QuaternionUtils.Squad(ref q, ref a, ref b, ref p, interpolationTimeSquad).normalized;

                                            //* just double checking some numbers to make sure the math is good
                                            GONetLog.Debug(string.Concat(
                                                "\nq0: ", q0.eulerAngles, 
                                                "\nq1: ", q1.eulerAngles,
                                                "\nq2: ", q2.eulerAngles,
                                                "\nq3: ", q3.eulerAngles, 
                                                "\nsq: ", blendedValue.UnityEngine_Quaternion.eulerAngles,
                                                "\ninterpolationTimeSquad: ", interpolationTimeSquad,
                                                ", interpolationTime_InitialRaw: ", interpolationTime_InitialRaw,
                                                ", q0(ms): ", TimeSpan.FromTicks(justBeforeNewest.elapsedTicksAtChange).TotalMilliseconds,
                                                ", q1(ms): ", TimeSpan.FromTicks(newest.elapsedTicksAtChange).TotalMilliseconds,
                                                ", at(ms): ", TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds,
                                                ", q2(ms): ", TimeSpan.FromTicks((long)atInterpolationTime1_ticks).TotalMilliseconds,
                                                ", atMinusNewest(ms): ", TimeSpan.FromTicks(atMinusNewest_ticks).TotalMilliseconds,
                                                ", t1MinusNewest(ms): ", TimeSpan.FromTicks((long)t1MinusNewest_ticks).TotalMilliseconds));
                                            //* /
                                        }*/
                                        
                                        /*{ // Simple impl that works well on more linear movements
                                            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                            Quaternion diffRotation = newestValue * Quaternion.Inverse(justBeforeNewest.numericValue.UnityEngine_Quaternion);
                                            float interpolationTime = (atElapsedTicks - newest.elapsedTicksAtChange) / (float)(newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange);
                                            Quaternion extrapolatedRotation = newestValue * diffRotation;
                                            blendedValue = QuaternionUtils.SlerpUnclamped(
                                                ref newestValue,
                                                ref extrapolatedRotation,
                                                interpolationTime);
                                        }*/
                                    }

                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + blendedValue.UnityEngine_Quaternion);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("QUAT new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.UnityEngine_Quaternion;
                                //GONetLog.Debug("QUAT went old school on 'eem..... at elapsed seconds: " + TimeSpan.FromTicks(atElapsedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                bool didWeLoip = false;
                                for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                    if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (atElapsedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
                                        blendedValue = Quaternion.Slerp(
                                            older.numericValue.UnityEngine_Quaternion,
                                            newer.numericValue.UnityEngine_Quaternion,
                                            interpolationTime);
                                        //GONetLog.Debug("we loip'd 'eem. are they the same value, which should only be the case if our recent quantization equality checks are not working? " + (older.numericValue.UnityEngine_Quaternion.eulerAngles == newer.numericValue.UnityEngine_Quaternion.eulerAngles ? "Yes" : "No"));
                                        didWeLoip = true;
                                        break;
                                    }
                                }
                                if (!didWeLoip)
                                {
                                    GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                                }
                            }
                        }
                        else
                        {
                            blendedValue = newestValue;
                            GONetLog.Debug("not a quaternion?");
                        }
                    }
                    else
                    {
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                return true;
            }

            return false;
        }

        static bool GetBlendedValue_Vector3(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
        {
            blendedValue = default;

            if (valueCount > 0)
            {
                { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                    int newestBufferIndex = 0;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                    int oldestBufferIndex = valueCount - 1;
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
                    bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) < GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;
                    if (isNewestRecentEnoughToProcess)
                    {
                        Vector3 newestValue = newest.numericValue.UnityEngine_Vector3;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    Vector3 justBeforeNewest_numericValue = justBeforeNewest.numericValue.UnityEngine_Vector3;
                                    Vector3 valueDiffBetweenLastTwo = newestValue - justBeforeNewest_numericValue;
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                    long atMinusNewestTicks = atElapsedTicks - newest.elapsedTicksAtChange;
                                    int extrapolationSections = (int)Math.Ceiling(atMinusNewestTicks / (float)ticksBetweenLastTwo);
                                    long extrapolated_TicksAtChange = newest.elapsedTicksAtChange + (ticksBetweenLastTwo * extrapolationSections);
                                    Vector3 extrapolated_ValueNew = newestValue + (valueDiffBetweenLastTwo * extrapolationSections);

                                    /* the above 4 lines is preferred over what we would have done below here accumulating in a loop as somehow the loop would get infinite or at least stop the simulation
                                    do
                                    {
                                        extrapolated_TicksAtChange += ticksBetweenLastTwo;
                                        extrapolated_ValueNew += valueDiffBetweenLastTwo;
                                        ++extrapolationSections;
                                    } while (extrapolated_TicksAtChange < atElapsedTicks);
                                    */

                                    long denominator = extrapolated_TicksAtChange - newest.elapsedTicksAtChange;
                                    if (denominator == 0)
                                    {
                                        denominator = 1;
                                    }
                                    float interpolationTime = atMinusNewestTicks / (float)denominator;
                                    float oneSectionPercentage = (1 / (float)(extrapolationSections + 1));
                                    float remainingSectionPercentage = 1f - oneSectionPercentage;
                                    float bezierTime = oneSectionPercentage + (interpolationTime * remainingSectionPercentage);
                                    blendedValue = GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR3 new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.UnityEngine_Vector3;
                                //GONetLog.Debug("VECTOR3 went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                bool didWeLoip = false;
                                for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                    if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (atElapsedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
                                        blendedValue = Vector3.Lerp(
                                            older.numericValue.UnityEngine_Vector3,
                                            newer.numericValue.UnityEngine_Vector3,
                                            interpolationTime);
                                        //GONetLog.Debug("we loip'd 'eem");
                                        didWeLoip = true;
                                        break;
                                    }
                                }
                                if (!didWeLoip)
                                {
                                    GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                                }
                            }
                        }
                        else
                        {
                            blendedValue = newestValue;
                            GONetLog.Debug("not a vector3?");
                        }
                    }
                    else
                    {
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetQuadraticBezierValue(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }
    }
}
