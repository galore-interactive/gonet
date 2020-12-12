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

        internal static bool TryGetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport, long atElapsedTicks, out GONetSyncableValue blendedValue)
        {
            if (valueMonitoringSupport.mostRecentChanges_usedSize > 0)
            {
                IGONetAutoMagicalSync_CustomValueBlending customBlending;
                if (getBlendedValues_byValueType.TryGetValue(valueMonitoringSupport.mostRecentChanges[0].numericValue.GONetSyncType, out customBlending))
                {
                    return customBlending.TryGetBlendedValue(valueMonitoringSupport.mostRecentChanges, valueMonitoringSupport.mostRecentChanges_usedSize, atElapsedTicks, out blendedValue);
                }
            }

            blendedValue = default;
            return false;
        }

        /// <summary>
        /// TODO refactor this to dynamically lookup all the implementations of IGONetAutoMagicalSync_CustomValueBlending and have a library available to users via Unity editor settings in sync profile/template
        /// </summary>
        static readonly Dictionary<GONetSyncableValueTypes, IGONetAutoMagicalSync_CustomValueBlending> getBlendedValues_byValueType = new Dictionary<GONetSyncableValueTypes, IGONetAutoMagicalSync_CustomValueBlending>(8)
        {
            { GONetSyncableValueTypes.System_Single, new GONetDefaultValueBlending_Float() },
            { GONetSyncableValueTypes.UnityEngine_Quaternion, new GONetDefaultValueBlending_Quaternion() },
            { GONetSyncableValueTypes.UnityEngine_Vector3, new GONetDefaultValueBlending_Vector3() }
        };


        public class GONetDefaultValueBlending_Float : IGONetAutoMagicalSync_CustomValueBlending
        {
            public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.System_Single;

            public string Description => "Provides a good blending solution for floats that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

            public bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
            {
                blendedValue = default;

                if (valueCount > 0)
                {
                    { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                        int newestBufferIndex = 0;
                        NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                        int oldestBufferIndex = valueCount - 1;
                        NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
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
                                        NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
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
                                        NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                        if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                        {
                                            NumericValueChangeSnapshot older = valueBuffer[i];

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
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetQuadraticBezierValue(float p0, float p1, float p2, float t)
        {
            float u = 1 - t;
            float uSquared = u * u;
            float tSquared = t * t;
            return (uSquared * p0) + (2 * u * t * p1) + (tSquared * p2);
        }

        public class GONetDefaultValueBlending_Quaternion : IGONetAutoMagicalSync_CustomValueBlending
        {
            private Quaternion GetQuaternionAccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest)
            {
                Quaternion extrapolatedViaAcceleration;
                NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
                Quaternion q0 = q0_snap.numericValue.UnityEngine_Quaternion;

                NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
                Quaternion q1 = q1_snap.numericValue.UnityEngine_Quaternion;

                NumericValueChangeSnapshot q2_snap = newest;
                Quaternion q2 = newest.numericValue.UnityEngine_Quaternion;
                Quaternion diffRotation_q2_q1 = q2 * Quaternion.Inverse(q1);


                Quaternion diffRotation_q1_q0 = q1 * Quaternion.Inverse(q0);
                long q1MinusQ0_ticks = q1_snap.elapsedTicksAtChange - q0_snap.elapsedTicksAtChange;
                long q2MinusQ1_ticks = q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange; // IMPORTANT: This is the main unit of measure...considered the whole of 1, which is why it is the denominator when calculating interpolationTime below
                float interpolationTime_q1q0 = q2MinusQ1_ticks / (float)q1MinusQ0_ticks;
                Quaternion identity = Quaternion.identity;
                Quaternion diffRotation_q1_q0_scaledTo_q2q1_Time =
                    QuaternionUtils.SlerpUnclamped(
                        ref identity,
                        ref diffRotation_q1_q0,
                        interpolationTime_q1q0);


                Quaternion diffDiff = diffRotation_q2_q1 * Quaternion.Inverse(diffRotation_q1_q0_scaledTo_q2q1_Time);
                Quaternion q3 = q2 * diffRotation_q2_q1 * diffDiff;
                long atMinusQ2_ticks = atElapsedTicks - q2_snap.elapsedTicksAtChange;


                float interpolationTime = atMinusQ2_ticks / (float)q2MinusQ1_ticks;
                extrapolatedViaAcceleration =
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
                    "\nd1: ", diffRotation_q1_q0_scaledTo_q2q1_Time.eulerAngles,
                    "\ndd: ", diffDiff.eulerAngles,
                    "\nsq: ", extrapolatedViaAcceleration.UnityEngine_Quaternion.eulerAngles,
                    "\nq3: ", q3.eulerAngles,
                    "\ninterpolationTime: ", interpolationTime,
                    ", q0(ms): ", TimeSpan.FromTicks(q0_snap.elapsedTicksAtChange).TotalMilliseconds,
                    ", q1(ms): ", TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange).TotalMilliseconds,
                    ", q2(ms): ", TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange).TotalMilliseconds,
                    ", at(ms): ", TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds,
                    ", atMinusNewest(ms): ", TimeSpan.FromTicks(atMinusQ2_ticks).TotalMilliseconds));
                //*/
                return extrapolatedViaAcceleration;
            }

            readonly Dictionary<NumericValueChangeSnapshot[], List<Vector3>> extrapolateQuaternionDiffHistory = new Dictionary<NumericValueChangeSnapshot[], List<Vector3>>();

            private void SeeHowWeDid_ExtrapolateQuaternion(NumericValueChangeSnapshot[] valueBuffer, int valueCount)
            {
                if (valueCount > 3)
                {
                    if (!extrapolateQuaternionDiffHistory.ContainsKey(valueBuffer))
                    {
                        extrapolateQuaternionDiffHistory[valueBuffer] = new List<Vector3>();
                    }


                    int newestBufferIndex = 1;
                    var newest = valueBuffer[newestBufferIndex];

                    NumericValueChangeSnapshot q0_snap = valueBuffer[newestBufferIndex + 2];
                    Quaternion q0 = q0_snap.numericValue.UnityEngine_Quaternion;

                    NumericValueChangeSnapshot q1_snap = valueBuffer[newestBufferIndex + 1];
                    Quaternion q1 = q1_snap.numericValue.UnityEngine_Quaternion;

                    NumericValueChangeSnapshot q2_snap = newest;
                    Quaternion q2 = newest.numericValue.UnityEngine_Quaternion;
                    Quaternion diffRotation_q2_q1 = q2 * Quaternion.Inverse(q1);


                    Quaternion diffRotation_q1_q0 = q1 * Quaternion.Inverse(q0);
                    long q1MinusQ0_ticks = q1_snap.elapsedTicksAtChange - q0_snap.elapsedTicksAtChange;
                    long q2MinusQ1_ticks = q2_snap.elapsedTicksAtChange - q1_snap.elapsedTicksAtChange; // IMPORTANT: This is the main unit of measure...considered the whole of 1, which is why it is the denominator when calculating interpolationTime below
                    float interpolationTime_q1q0 = q2MinusQ1_ticks / (float)q1MinusQ0_ticks;
                    Quaternion identity = Quaternion.identity;
                    Quaternion diffRotation_q1_q0_scaledTo_q2q1_Time =
                        QuaternionUtils.SlerpUnclamped(
                            ref identity,
                            ref diffRotation_q1_q0,
                            interpolationTime_q1q0);

                    Quaternion diffDiff = diffRotation_q2_q1 * Quaternion.Inverse(diffRotation_q1_q0_scaledTo_q2q1_Time);
                    Quaternion q3predicted = q2 * diffRotation_q2_q1 * diffDiff;


                    {
                        Vector3 axis_diff_q1_q0 = Vector3.zero;
                        float ang_diff_q1_q0;
                        diffRotation_q1_q0.ToAngleAxis(out ang_diff_q1_q0, out axis_diff_q1_q0);
                        float ang_diff_q1_q0Scaled = ang_diff_q1_q0 * (q1MinusQ0_ticks / (float)TimeSpan.FromSeconds(1).Ticks);

                        Vector3 axis_diff_q2_q1 = Vector3.zero;
                        float ang_diff_q2_q1;
                        diffRotation_q2_q1.ToAngleAxis(out ang_diff_q2_q1, out axis_diff_q2_q1);
                        float ang_diff_q2_q1Scaled = ang_diff_q2_q1 * (q2MinusQ1_ticks / (float)TimeSpan.FromSeconds(1).Ticks);

                        GONetLog.Debug($"\naxis_diff_q1_q0: (x:{axis_diff_q1_q0.x}, y:{axis_diff_q1_q0.y}, z:{axis_diff_q1_q0.z}) ang_diff_q1_q0: {ang_diff_q1_q0} ang_diff_q1_q0Scaled: {ang_diff_q1_q0Scaled}\naxis_diff_q2_q1: (x:{axis_diff_q2_q1.x}, y:{axis_diff_q2_q1.y}, z:{axis_diff_q2_q1.z}) ang_diff_q2_q1: {ang_diff_q2_q1} ang_diff_q2_q1Scaled: {ang_diff_q2_q1Scaled}");
                        /*
                                            if (ang > 180)
                                            {
                                                ang -= 360;
                                            }

                                            float dt_FIXME = 0.5f;
                                            ang = ang * (float)dt_FIXME % 360;
                                            var extrapd = Quaternion.AngleAxis(ang, axis) * q1;
                        */
                    }

                    int q3actualIndex = newestBufferIndex - 1;
                    var q3actual = valueBuffer[q3actualIndex];
                    long atElapsedTicks_q3Actual = valueBuffer[q3actualIndex].elapsedTicksAtChange;
                    long atMinusQ2_ticks = atElapsedTicks_q3Actual - q2_snap.elapsedTicksAtChange;

                    float interpolationTime = atMinusQ2_ticks / (float)q2MinusQ1_ticks;
                    Quaternion extrapolatedPrediction =
                        QuaternionUtils.SlerpUnclamped(
                            ref q2,
                            ref q3predicted,
                            interpolationTime);

                    Quaternion thisIsHowWeDid = q3actual.numericValue.UnityEngine_Quaternion * Quaternion.Inverse(extrapolatedPrediction);
                    extrapolateQuaternionDiffHistory[valueBuffer].Add(CenterAround180(thisIsHowWeDid.eulerAngles));
                    //GONetLog.Debug($"extrapolated - actual: (x:{thisIsHowWeDid.eulerAngles.x}, y:{thisIsHowWeDid.eulerAngles.y}, z:{thisIsHowWeDid.eulerAngles.z})");

                    Vector3 total = new Vector3();
                    foreach (var n in extrapolateQuaternionDiffHistory[valueBuffer])
                    {
                        total += n;
                    }
                    Vector3 average = total / extrapolateQuaternionDiffHistory[valueBuffer].Count;
                    GONetLog.Debug($"average diff: (x:{average.x}, y:{average.y}, z:{average.z})");

                    /* just double checking some numbers to make sure the math is good
                    GONetLog.Debug(string.Concat(
                        "\nq0: ", q0.eulerAngles,
                        "\nq1: ", q1.eulerAngles,
                        "\nq2: ", q2.eulerAngles,
                        "\nd2: ", diffRotation_q2_q1.eulerAngles,
                        "\nd1: ", diffRotation_q1_q0_scaledTo_q2q1_Time.eulerAngles,
                        "\ndd: ", diffDiff.eulerAngles,
                        "\nsq: ", blendedValue.UnityEngine_Quaternion.eulerAngles,
                        "\nq3: ", q3.eulerAngles,
                        "\ninterpolationTime: ", interpolationTime,
                        ", q0(ms): ", TimeSpan.FromTicks(q0_snap.elapsedTicksAtChange).TotalMilliseconds,
                        ", q1(ms): ", TimeSpan.FromTicks(q1_snap.elapsedTicksAtChange).TotalMilliseconds,
                        ", q2(ms): ", TimeSpan.FromTicks(q2_snap.elapsedTicksAtChange).TotalMilliseconds,
                        ", at(ms): ", TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds,
                        ", atMinusNewest(ms): ", TimeSpan.FromTicks(atMinusQ2_ticks).TotalMilliseconds));
                    //*/
                }
            }

            public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Quaternion;

            public string Description => "Provides a good blending solution for Quaternions that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

            public bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
            {
                blendedValue = default;

                if (valueCount > 0)
                {
                    { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                        int newestBufferIndex = 0;
                        NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                        int oldestBufferIndex = valueCount - 1;
                        NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
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
                                        if (valueCount > 3)
                                        {
                                            blendedValue = GetQuaternionAccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest);

                                            { // at this point, blendedValue is the raw extrapolated value, BUT there may be a need to smooth things out since we can review how well our previous extrapolation did once we get new data and that is essentially what is happening below:
                                                long TEMP_TicksBetweenSyncs = (long)(TimeSpan.FromSeconds(1 / 20f).Ticks * 0.9); // 20 Hz at the moment....TODO FIXME: maybe average the time between elements instead to be dynamic!!!
                                                long atMinusNewest_ticks = atElapsedTicks - newest.elapsedTicksAtChange;
                                                float timePercentageCompleteBeforeNextSync = atMinusNewest_ticks / (float)TEMP_TicksBetweenSyncs;
                                                if (timePercentageCompleteBeforeNextSync < 1)
                                                {
                                                    float timePercentageRemainingBeforeNextSync = 1 - timePercentageCompleteBeforeNextSync;

                                                    var newest_last = valueBuffer[newestBufferIndex + 1];
                                                    Quaternion newestAsExtrapolated = // TODO instead of calculating this each time, just store in a correlation buffer
                                                        GetQuaternionAccelerationBasedExtrapolation(
                                                            valueBuffer,
                                                            newest.elapsedTicksAtChange,
                                                            newestBufferIndex + 1,
                                                            newest_last);

                                                    Quaternion identity = Quaternion.identity;
                                                    Quaternion overExtrapolationNewest = newestAsExtrapolated * Quaternion.Inverse(newest.numericValue.UnityEngine_Quaternion);
                                                    Quaternion overExtrapolationNewest_adjustmentToSmooth =
                                                        QuaternionUtils.SlerpUnclamped(
                                                            ref identity,
                                                            ref overExtrapolationNewest,
                                                            timePercentageRemainingBeforeNextSync);

                                                    //GONetLog.Debug($"smooth by: (x:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.x}, y:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.y}, z:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.z})");

                                                    // TODO UNCOMMENT: blendedValue = blendedValue.UnityEngine_Quaternion * overExtrapolationNewest_adjustmentToSmooth;
                                                }
                                            }
                                        }
                                        else if (valueCount > 2)
                                        {
                                            blendedValue = GetQuaternionAccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest);
                                        }
                                        else
                                        {
                                            /* { // SQUAD life!  This is much better for dynamic movements, but is a bit more costly on CPU
                                                NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
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

                                            { // Simple impl that works well on more linear movements
                                                NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                                Quaternion diffRotation = newestValue * Quaternion.Inverse(justBeforeNewest.numericValue.UnityEngine_Quaternion);
                                                float interpolationTime = (atElapsedTicks - newest.elapsedTicksAtChange) / (float)(newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange);
                                                Quaternion extrapolatedRotation = newestValue * diffRotation;
                                                blendedValue = QuaternionUtils.SlerpUnclamped(
                                                    ref newestValue,
                                                    ref extrapolatedRotation,
                                                    interpolationTime);
                                            }
                                        }

                                        blendedValue = GetSmoothedRotation(blendedValue.UnityEngine_Quaternion, valueBuffer, valueCount);

                                        //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + blendedValue.UnityEngine_Quaternion);
                                    }
                                    else
                                    {
                                        blendedValue = newestValue;
                                        //GONetLog.Debug("QUAT new new beast");
                                    }

                                    // SeeHowWeDid_ExtrapolateQuaternion(valueBuffer, valueCount);
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
                                        NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                        if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                        {
                                            NumericValueChangeSnapshot older = valueBuffer[i];

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
        }

        private static Vector3 CenterAround180(Vector3 eulerAngles)
        {
            return new Vector3(CenterAround180(eulerAngles.x), CenterAround180(eulerAngles.y), CenterAround180(eulerAngles.z));
        }

        private static float CenterAround180(float f)
        {
            f += 180;
            if (f > 360) f -= 360;
            return f;
        }

        public class GONetDefaultValueBlending_Vector3 : IGONetAutoMagicalSync_CustomValueBlending
        {
            public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

            public string Description => "Provides a good blending solution for Vector3s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

            public bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue)
            {
                blendedValue = default;

                if (valueCount > 0)
                {
                    { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                        int newestBufferIndex = 0;
                        NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                        int oldestBufferIndex = valueCount - 1;
                        NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];
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
                                        //float average, min, max;
                                        //DetermineTimeBetweenStats(valueBuffer, valueCount, out min, out average, out max);
                                        //GONetLog.Debug($"millis between snaps: min: {min} avg: {average} max: {max}");

                                        if (valueCount > 3)
                                        {
                                            //Vector3 accerlation_current;
                                            //blendedValue = GetVector3AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out accerlation_current);



                                            //blendedValue = GetVector3AvgAccelerationBasedExtrapolation(valueBuffer, valueCount, atElapsedTicks, newestBufferIndex, newest);

                                            Vector3 averageAcceleration;
                                            if (TryDetermineAverageAccelerationPerSecond(valueBuffer, Math.Max(valueCount, 4), out averageAcceleration))
                                            {
                                                //GONetLog.Debug($"\navg accel: {averageAcceleration}");
                                                blendedValue = newest.numericValue.UnityEngine_Vector3 + averageAcceleration * (float)TimeSpan.FromTicks(atElapsedTicks - newest.elapsedTicksAtChange).TotalSeconds;
                                            }


                                            { // at this point, blendedValue is the raw extrapolated value, BUT there may be a need to smooth things out since we can review how well our previous extrapolation did once we get new data and that is essentially what is happening below:
                                                long TEMP_TicksBetweenSyncs = (long)(TimeSpan.FromSeconds(1 / 20f).Ticks * 0.9); // 20 Hz at the moment....TODO FIXME: maybe average the time between elements instead to be dynamic!!!
                                                long atMinusNewest_ticks = atElapsedTicks - newest.elapsedTicksAtChange;
                                                float timePercentageCompleteBeforeNextSync = atMinusNewest_ticks / (float)TEMP_TicksBetweenSyncs;
                                                float timePercentageRemainingBeforeNextSync = 1 - timePercentageCompleteBeforeNextSync;

                                                var newest_last = valueBuffer[newestBufferIndex + 1];
                                                Vector3 acceleration_previous;
                                                Vector3 newestAsExtrapolated = // TODO instead of calculating this each time, just store in a correlation buffer
                                                    GetVector3AccelerationBasedExtrapolation(
                                                        valueBuffer,
                                                        newest.elapsedTicksAtChange,
                                                        newestBufferIndex + 1,
                                                        newest_last,
                                                        out acceleration_previous);

                                                //Vector3 acceleration_average = (accerlation_current + acceleration_previous) / 2f;
                                                //GONetLog.Debug($"\naccel_prv: (x:{acceleration_previous.x}, y:{acceleration_previous.y}, z:{acceleration_previous.z}), \naccel_cur: (x:{accerlation_current.x}, y:{accerlation_current.y}, z:{accerlation_current.z}), \naccel_avg: (x:{acceleration_average.x}, y:{acceleration_average.y}, z:{acceleration_average.z})");

                                                bool shouldDoSmoothing = false; //  timePercentageCompleteBeforeNextSync < 1;
                                                if (shouldDoSmoothing)
                                                {
                                                    Vector3 identity = Vector3.zero;
                                                    Vector3 overExtrapolationNewest = newestAsExtrapolated - newest.numericValue.UnityEngine_Vector3;
                                                    Vector3 overExtrapolationNewest_adjustmentToSmooth = overExtrapolationNewest * timePercentageRemainingBeforeNextSync;
                                                    /*Vector3.LerpUnclamped(
                                                        identity,
                                                        overExtrapolationNewest,
                                                        timePercentageRemainingBeforeNextSync);*/

                                                    //GONetLog.Debug($"smooth by: (x:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.x}, y:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.y}, z:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.z})");

                                                    blendedValue = blendedValue.UnityEngine_Vector3 + overExtrapolationNewest_adjustmentToSmooth;
                                                }

                                            }
                                        }
                                        else if (valueCount > 2)
                                        {
                                            Vector3 acceleration;
                                            blendedValue = GetVector3AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out acceleration);
                                        }
                                        else
                                        {
                                            NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
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

                                        blendedValue = GetSmoothedVector3(blendedValue.UnityEngine_Vector3, valueBuffer, valueCount);
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
                                        NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                        if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                        {
                                            NumericValueChangeSnapshot older = valueBuffer[i];

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
        }

        private static bool TryDetermineAverageAccelerationPerSecond(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out Vector3 averageAcceleration)
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

        private static bool DetermineTimeBetweenStats(NumericValueChangeSnapshot[] valueBuffer, int valueCount, out float min, out float average, out float max)
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

        private static Vector3 GetVector3AvgAccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest)
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

        private static Vector3 GetVector3AccelerationBasedExtrapolation(NumericValueChangeSnapshot[] valueBuffer, long atElapsedTicks, int newestBufferIndex, NumericValueChangeSnapshot newest, out Vector3 acceleration)
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
            var s0 = q2;
            var v0 = velocity_q2_q1;
            var s = s0 + (v0 * atMinusNewest_seconds) + (0.5f * acceleration * atMinusNewest_seconds * atMinusNewest_seconds);
            Vector3 extrapolatedViaAcceleration = s;

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
        private static Quaternion GetSmoothedRotation(Quaternion mostRecentValue, NumericValueChangeSnapshot[] olderValuesBuffer, int bufferCount)
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

        private static Vector3 GetSmoothedVector3(Vector3 mostRecentValue, NumericValueChangeSnapshot[] olderValuesBuffer, int bufferCount)
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
