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

using GONet.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.PluginAPI
{
    /// <summary>
    /// TODO document this for people to understand.  For now, check out the member comments/summaries.
    /// </summary>
    public interface IGONetAutoMagicalSync_CustomValueBlending
    {
        /// <summary>
        /// An instance of this class will only be able to blend values for a single GONet supported value type.  This is it.
        /// </summary>
        GONetSyncableValueTypes AppliesOnlyToGONetType { get; }

        /// <summary>
        /// Let the world know how this class is different than others and unique in its way of arriving at a smoothed "estimated" value given a history of actual values.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// IMPORTANT: <paramref name="valueBuffer"/> is a most recent "actual" value history in the form of an ordered array of <paramref name="valueCount"/> timestamp/value pairs and is sorted in most recent with lowest index to oldest with highest index order.
        /// </summary>
        /// <returns>true if a blended "best estimate" value at <paramref name="atElapsedTicks"/> was determined based on inputs and set in <paramref name="blendedValue"/>, false otherwise</returns>
        bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate);
    }

    #region GONet default implementations (i.e., should be good for general use cases - linear velocity and/or linear acceleration)

    public class GONetDefaultValueBlending_Float : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.System_Single;

        public string Description => "Provides a good blending solution for floats that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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
                                    blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                    didExtrapolate = true;
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
                extrapolateQuaternionDiffHistory[valueBuffer].Add(ValueBlendUtils.CenterAround180(thisIsHowWeDid.eulerAngles));
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

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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

                                                blendedValue = blendedValue.UnityEngine_Quaternion * overExtrapolationNewest_adjustmentToSmooth;
                                            }
                                        }
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        blendedValue = GetQuaternionAccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest);
                                        didExtrapolate = true;
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

                                    //GONetLog.Debug("QWUAT-WAD extroip'd....newest: " + newestValue + " extrap'd: " + blendedValue.UnityEngine_Quaternion);
                                    didExtrapolate = true;
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

    public class GONetDefaultValueBlending_Vector3 : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "Provides a good blending solution for Vector3s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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
                                        if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, Math.Max(valueCount, 4), out averageAcceleration))
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
                                                ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(
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

                                                //GONetLog.Debug($"smooth by: (x:{overExtrapolationNewest_adjustmentToSmooth.x}, y:{overExtrapolationNewest_adjustmentToSmooth.y}, z:{overExtrapolationNewest_adjustmentToSmooth.z})");

                                                blendedValue = blendedValue.UnityEngine_Vector3 + overExtrapolationNewest_adjustmentToSmooth;
                                            }

                                        }
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        Vector3 acceleration;
                                        blendedValue = ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out acceleration);
                                        didExtrapolate = true;
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
                                        blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                        //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                        didExtrapolate = true;
                                    }
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
                                //GONetLog.Debug("VECTOR3 went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(atElapsedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
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
                                        //GONetLog.Debug($"we loip'd 'eem.  l: {older.numericValue}, r: {newer.numericValue}, t:{interpolationTime}");
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
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(GONetMain.Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                //GONetLog.Debug("return blended: " + blendedValue);
                return true;
            }

            return false;
        }
    }

    public class GONetDefaultValueBlending_Vector2 : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector2;

        public string Description => "Provides a good blending solution for Vector2s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                        Vector2 newestValue = newest.numericValue.UnityEngine_Vector2;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    //float average, min, max;
                                    //DetermineTimeBetweenStats(valueBuffer, valueCount, out min, out average, out max);
                                    //GONetLog.Debug($"millis between snaps: min: {min} avg: {average} max: {max}");

                                    if (valueCount > 3)
                                    {
                                        //Vector2 accerlation_current;
                                        //blendedValue = GetVector2AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out accerlation_current);



                                        //blendedValue = GetVector2AvgAccelerationBasedExtrapolation(valueBuffer, valueCount, atElapsedTicks, newestBufferIndex, newest);

                                        Vector2 averageAcceleration;
                                        if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, Math.Max(valueCount, 4), out averageAcceleration))
                                        {
                                            //GONetLog.Debug($"\navg accel: {averageAcceleration}");
                                            blendedValue = newest.numericValue.UnityEngine_Vector2 + averageAcceleration * (float)TimeSpan.FromTicks(atElapsedTicks - newest.elapsedTicksAtChange).TotalSeconds;
                                        }


                                        { // at this point, blendedValue is the raw extrapolated value, BUT there may be a need to smooth things out since we can review how well our previous extrapolation did once we get new data and that is essentially what is happening below:
                                            long TEMP_TicksBetweenSyncs = (long)(TimeSpan.FromSeconds(1 / 20f).Ticks * 0.9); // 20 Hz at the moment....TODO FIXME: maybe average the time between elements instead to be dynamic!!!
                                            long atMinusNewest_ticks = atElapsedTicks - newest.elapsedTicksAtChange;
                                            float timePercentageCompleteBeforeNextSync = atMinusNewest_ticks / (float)TEMP_TicksBetweenSyncs;
                                            float timePercentageRemainingBeforeNextSync = 1 - timePercentageCompleteBeforeNextSync;

                                            var newest_last = valueBuffer[newestBufferIndex + 1];
                                            Vector2 acceleration_previous;
                                            Vector2 newestAsExtrapolated = // TODO instead of calculating this each time, just store in a correlation buffer
                                                ValueBlendUtils.GetVector2AccelerationBasedExtrapolation(
                                                    valueBuffer,
                                                    newest.elapsedTicksAtChange,
                                                    newestBufferIndex + 1,
                                                    newest_last,
                                                    out acceleration_previous);

                                            //Vector2 acceleration_average = (accerlation_current + acceleration_previous) / 2f;
                                            //GONetLog.Debug($"\naccel_prv: (x:{acceleration_previous.x}, y:{acceleration_previous.y}, z:{acceleration_previous.z}), \naccel_cur: (x:{accerlation_current.x}, y:{accerlation_current.y}, z:{accerlation_current.z}), \naccel_avg: (x:{acceleration_average.x}, y:{acceleration_average.y}, z:{acceleration_average.z})");

                                            bool shouldDoSmoothing = false; //  timePercentageCompleteBeforeNextSync < 1;
                                            if (shouldDoSmoothing)
                                            {
                                                Vector2 identity = Vector2.zero;
                                                Vector2 overExtrapolationNewest = newestAsExtrapolated - newest.numericValue.UnityEngine_Vector2;
                                                Vector2 overExtrapolationNewest_adjustmentToSmooth = overExtrapolationNewest * timePercentageRemainingBeforeNextSync;
                                                /*Vector2.LerpUnclamped(
                                                    identity,
                                                    overExtrapolationNewest,
                                                    timePercentageRemainingBeforeNextSync);*/

                                                //GONetLog.Debug($"smooth by: (x:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.x}, y:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.y}, z:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.z})");

                                                blendedValue = blendedValue.UnityEngine_Vector2 + overExtrapolationNewest_adjustmentToSmooth;
                                            }

                                        }
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        Vector2 acceleration;
                                        blendedValue = ValueBlendUtils.GetVector2AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out acceleration);
                                        didExtrapolate = true;
                                    }
                                    else
                                    {
                                        NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                        Vector2 justBeforeNewest_numericValue = justBeforeNewest.numericValue.UnityEngine_Vector2;
                                        Vector2 valueDiffBetweenLastTwo = newestValue - justBeforeNewest_numericValue;
                                        long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                        long atMinusNewestTicks = atElapsedTicks - newest.elapsedTicksAtChange;
                                        int extrapolationSections = (int)Math.Ceiling(atMinusNewestTicks / (float)ticksBetweenLastTwo);
                                        long extrapolated_TicksAtChange = newest.elapsedTicksAtChange + (ticksBetweenLastTwo * extrapolationSections);
                                        Vector2 extrapolated_ValueNew = newestValue + (valueDiffBetweenLastTwo * extrapolationSections);

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
                                        blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                        //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                        didExtrapolate = true;
                                    }
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR2 new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.UnityEngine_Vector2;
                                //GONetLog.Debug("VECTOR2 went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
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
                                        blendedValue = Vector2.Lerp(
                                            older.numericValue.UnityEngine_Vector2,
                                            newer.numericValue.UnityEngine_Vector2,
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
                            GONetLog.Debug("not a vector2?");
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

    public class GONetDefaultValueBlending_Vector4 : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector4;

        public string Description => "Provides a good blending solution for Vector4s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                        Vector4 newestValue = newest.numericValue.UnityEngine_Vector4;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    //float average, min, max;
                                    //DetermineTimeBetweenStats(valueBuffer, valueCount, out min, out average, out max);
                                    //GONetLog.Debug($"millis between snaps: min: {min} avg: {average} max: {max}");

                                    if (valueCount > 3)
                                    {
                                        //Vector4 accerlation_current;
                                        //blendedValue = GetVector4AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out accerlation_current);



                                        //blendedValue = GetVector4AvgAccelerationBasedExtrapolation(valueBuffer, valueCount, atElapsedTicks, newestBufferIndex, newest);

                                        Vector4 averageAcceleration;
                                        if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, Math.Max(valueCount, 4), out averageAcceleration))
                                        {
                                            //GONetLog.Debug($"\navg accel: {averageAcceleration}");
                                            blendedValue = newest.numericValue.UnityEngine_Vector4 + averageAcceleration * (float)TimeSpan.FromTicks(atElapsedTicks - newest.elapsedTicksAtChange).TotalSeconds;
                                        }


                                        { // at this point, blendedValue is the raw extrapolated value, BUT there may be a need to smooth things out since we can review how well our previous extrapolation did once we get new data and that is essentially what is happening below:
                                            long TEMP_TicksBetweenSyncs = (long)(TimeSpan.FromSeconds(1 / 20f).Ticks * 0.9); // 20 Hz at the moment....TODO FIXME: maybe average the time between elements instead to be dynamic!!!
                                            long atMinusNewest_ticks = atElapsedTicks - newest.elapsedTicksAtChange;
                                            float timePercentageCompleteBeforeNextSync = atMinusNewest_ticks / (float)TEMP_TicksBetweenSyncs;
                                            float timePercentageRemainingBeforeNextSync = 1 - timePercentageCompleteBeforeNextSync;

                                            var newest_last = valueBuffer[newestBufferIndex + 1];
                                            Vector4 acceleration_previous;
                                            Vector4 newestAsExtrapolated = // TODO instead of calculating this each time, just store in a correlation buffer
                                                ValueBlendUtils.GetVector4AccelerationBasedExtrapolation(
                                                    valueBuffer,
                                                    newest.elapsedTicksAtChange,
                                                    newestBufferIndex + 1,
                                                    newest_last,
                                                    out acceleration_previous);

                                            //Vector4 acceleration_average = (accerlation_current + acceleration_previous) / 2f;
                                            //GONetLog.Debug($"\naccel_prv: (x:{acceleration_previous.x}, y:{acceleration_previous.y}, z:{acceleration_previous.z}), \naccel_cur: (x:{accerlation_current.x}, y:{accerlation_current.y}, z:{accerlation_current.z}), \naccel_avg: (x:{acceleration_average.x}, y:{acceleration_average.y}, z:{acceleration_average.z})");

                                            bool shouldDoSmoothing = false; //  timePercentageCompleteBeforeNextSync < 1;
                                            if (shouldDoSmoothing)
                                            {
                                                Vector4 identity = Vector4.zero;
                                                Vector4 overExtrapolationNewest = newestAsExtrapolated - newest.numericValue.UnityEngine_Vector4;
                                                Vector4 overExtrapolationNewest_adjustmentToSmooth = overExtrapolationNewest * timePercentageRemainingBeforeNextSync;
                                                /*Vector4.LerpUnclamped(
                                                    identity,
                                                    overExtrapolationNewest,
                                                    timePercentageRemainingBeforeNextSync);*/

                                                //GONetLog.Debug($"smooth by: (x:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.x}, y:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.y}, z:{overExtrapolationNewest_adjustmentToSmooth.eulerAngles.z})");

                                                blendedValue = blendedValue.UnityEngine_Vector4 + overExtrapolationNewest_adjustmentToSmooth;
                                            }

                                        }
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        Vector4 acceleration;
                                        blendedValue = ValueBlendUtils.GetVector4AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out acceleration);
                                        didExtrapolate = true;
                                    }
                                    else
                                    {
                                        NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                        Vector4 justBeforeNewest_numericValue = justBeforeNewest.numericValue.UnityEngine_Vector4;
                                        Vector4 valueDiffBetweenLastTwo = newestValue - justBeforeNewest_numericValue;
                                        long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                        long atMinusNewestTicks = atElapsedTicks - newest.elapsedTicksAtChange;
                                        int extrapolationSections = (int)Math.Ceiling(atMinusNewestTicks / (float)ticksBetweenLastTwo);
                                        long extrapolated_TicksAtChange = newest.elapsedTicksAtChange + (ticksBetweenLastTwo * extrapolationSections);
                                        Vector4 extrapolated_ValueNew = newestValue + (valueDiffBetweenLastTwo * extrapolationSections);

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
                                        blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                        //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                        didExtrapolate = true;
                                    }
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR3 new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.UnityEngine_Vector4;
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
                                        blendedValue = Vector4.Lerp(
                                            older.numericValue.UnityEngine_Vector4,
                                            newer.numericValue.UnityEngine_Vector4,
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
                            GONetLog.Debug("not a vector4?");
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

    #endregion

    #region experimental/specialty implementations

    public class GONetValueBlending_Float_ExtrapolateWithLowPassSmoothingFilter : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.System_Single;

        public string Description => "Provides a good blending solution for floats that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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
                                    blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                    didExtrapolate = true;
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

    public class GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter : IGONetAutoMagicalSync_CustomValueBlending
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
                extrapolateQuaternionDiffHistory[valueBuffer].Add(ValueBlendUtils.CenterAround180(thisIsHowWeDid.eulerAngles));
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

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

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
                            const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;
                            if (IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        blendedValue = GetQuaternionAccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest);
                                        didExtrapolate = true;
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

                                    blendedValue = ValueBlendUtils.GetSmoothedRotation(blendedValue.UnityEngine_Quaternion, valueBuffer, valueCount);

                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + blendedValue.UnityEngine_Quaternion);
                                    didExtrapolate = true;
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

    public class GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter : IGONetAutoMagicalSync_CustomValueBlending
    {
        public static bool ShouldLog = false;

        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "Provides a good blending solution for Vector3s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = false;

            if (valueCount > 0)
            {
                //{ // TODO FIXME remove this temp test!!!!!
                    int newestBufferIndex_TEMP = 0;
                    NumericValueChangeSnapshot newest_TEMP = valueBuffer[newestBufferIndex_TEMP];
                    //blendedValue = newest_TEMP.numericValue;
                    //didExtrapolate = false; // false?
                    //return true;
                //}

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
                            if (ShouldLog) GONetLog.Debug("value count: " + valueCount);

                            const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;
                            if (IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                if (ShouldLog) GONetLog.Debug("\tif EXTRAPO");
                                bool isEnoughInfoToExtrapolate = valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
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
                                        if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, Math.Max(valueCount, 4), out averageAcceleration))
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
                                                ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(
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
                                        didExtrapolate = true;
                                    }
                                    else if (valueCount > 2)
                                    {
                                        Vector3 acceleration;
                                        blendedValue = ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, newestBufferIndex, newest, out acceleration);
                                        didExtrapolate = true;
                                    }
                                    else
                                    {
                                        NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                        Vector3 justBeforeNewest_numericValue = justBeforeNewest.numericValue.UnityEngine_Vector3;
                                        Vector3 valueDiffBetweenLastTwo = newestValue - justBeforeNewest_numericValue;
                                        long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                        long atMinusNewestTicks = atElapsedTicks - newest.elapsedTicksAtChange;
                                        int extrapolationSections = (int)Math.Ceiling(atMinusNewestTicks / (float)ticksBetweenLastTwo);
                                        if (extrapolationSections < 0) extrapolationSections = -extrapolationSections;
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
                                        blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeNewest_numericValue, newestValue, extrapolated_ValueNew, bezierTime);
                                        //GONetLog.Debug("extroip'd....p0: " + justBeforeNewest_numericValue + " p1: " + newestValue + " p2: " + extrapolated_ValueNew + " blended: " + blendedValue + " t: " + bezierTime);
                                        if (float.IsNaN(bezierTime) || float.IsNaN(blendedValue.UnityEngine_Vector3.x))
                                        {
                                            GONetLog.Warning($"extrapolationSections: {extrapolationSections}, denominator: {denominator}, atMinusNewestTicks: {atMinusNewestTicks}");
                                        }
                                    }

                                    blendedValue = ValueBlendUtils.GetSmoothedVector3(blendedValue.UnityEngine_Vector3, valueBuffer, valueCount);
                                    didExtrapolate = true;
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR3 new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                if (ShouldLog) GONetLog.Debug("\telse if  YOU OLD DEVIL YOU");
                                blendedValue = oldest.numericValue.UnityEngine_Vector3;
                                //GONetLog.Debug("VECTOR3 went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                if (ShouldLog) GONetLog.Debug("\telse LOIP!");
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
                                    if (ShouldLog) GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                                }
                            }
                        }
                        else
                        {
                            blendedValue = newestValue;
                            if (ShouldLog) GONetLog.Debug("not a vector3?");
                        }
                    }
                    else
                    {
                        if (ShouldLog) GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(GONetMain.Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                //float maguey = (newest_TEMP.numericValue.UnityEngine_Vector3 - blendedValue.UnityEngine_Vector3).magnitude;
                //if (maguey > 0.75f) GONetLog.Debug($"\t\tdiff: bigger {maguey} , extrapo? (can: {atElapsedTicks >= newest_TEMP.elapsedTicksAtChange} vs will: {valueCount >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE})");
                return true;
            }

            if (ShouldLog) GONetLog.Debug($"NO VALUES!!! hash: {valueBuffer.GetHashCode()}");
            return false;
        }
    }

    #endregion
}
