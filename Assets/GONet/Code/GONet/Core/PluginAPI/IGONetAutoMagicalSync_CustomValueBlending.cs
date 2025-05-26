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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        /// <para>
        /// IMPORTANT: <paramref name="valueBuffer"/> is a most recent "actual" value history in the form of an ordered array of <paramref name="valueCount"/>
        ///            timestamp/value pairs and is sorted in most recent with lowest index to oldest with highest index order.
        /// </para>
        /// <para>
        /// IMPORTANT: <paramref name="didExtrapolatePastMostRecentChanges"/> should only be true after calling this if <paramref name="blendedValue"/>
        ///            represents a future value more recent than the most recent (valid) value inside <paramref name="valueBuffer"/>.
        ///            There are some implementations (e.g., <see cref="GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter"/>) that will 
        ///            force an extrapolation to be used instead of interpolation between known recent values even though it does not extrapolate past
        ///            the the most recent known values because it has reasons described therein in which case 
        ///            <paramref name="didExtrapolatePastMostRecentChanges"/> would be false.
        /// </para>
        /// </summary>
        /// <returns>true if a blended "best estimate" value at <paramref name="atElapsedTicks"/> was determined based on inputs and set in <paramref name="blendedValue"/>, false otherwise</returns>
        bool TryGetBlendedValue(NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges);
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
                                    //GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                                }
                            }
                        }
                        else
                        {
                            blendedValue = newestValue;
                            //GONetLog.Debug("not a vector3?");
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
        /// <summary>
        /// Enable advanced motion analysis using QuaternionDelta for better quality at the cost of ~50% more CPU.
        /// Recommended for FPS games and scenarios with varied rotation characteristics.
        /// </summary>
        public static bool UseAdvancedMotionAnalysis = true;

        /// <summary>
        /// Enable debug logging for motion analysis (only works when UseAdvancedMotionAnalysis is true)
        /// </summary>
        public static bool EnableMotionAnalysisLogging = false;

        /// <summary>
        /// Per-object motion profile cache (optional future enhancement)
        /// </summary>
        private readonly Dictionary<int, MotionProfile> motionProfiles = new Dictionary<int, MotionProfile>();

        private struct MotionProfile
        {
            public float averageAngularVelocity;
            public float maxAngularVelocity;
            public int sampleCount;

            public void Update(float angularVelocity)
            {
                maxAngularVelocity = angularVelocity > maxAngularVelocity ? angularVelocity : maxAngularVelocity;
                averageAngularVelocity = (averageAngularVelocity * sampleCount + angularVelocity) / (sampleCount + 1);
                sampleCount++;
            }
        }

        private struct QuaternionDelta
        {
            public Quaternion rotation;
            public float angleRadians;
            public Vector3 axis;
            public long timeDelta;

            public float AngularVelocity => timeDelta > 0 ? angleRadians / (timeDelta * 1e-7f) : 0f;
            public float AngularVelocityDegrees => AngularVelocity * Mathf.Rad2Deg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private QuaternionDelta ComputeDelta(Quaternion from, Quaternion to, long fromTime, long toTime)
        {
            Quaternion delta = to * Quaternion.Inverse(from);
            float angle;
            Vector3 axis;
            delta.ToAngleAxis(out angle, out axis);

            return new QuaternionDelta
            {
                rotation = delta,
                angleRadians = angle * Mathf.Deg2Rad,
                axis = axis,
                timeDelta = toTime - fromTime
            };
        }

        private Quaternion GetQuaternionAccelerationBasedExtrapolation(
            NumericValueChangeSnapshot[] buffer,
            long atElapsedTicks,
            int newestIdx,
            NumericValueChangeSnapshot newestSnap)
        {
            int N = buffer.Length;
            if (N < 2)
                return newestSnap.numericValue.UnityEngine_Quaternion;

            // Use the provided newestIdx as starting point
            int iS2 = newestIdx;

            // Early bounds check to avoid exceptions
            int maxIndex = N - 1;
            int minValidIndex = iS2 + 2 <= maxIndex ? iS2 : maxIndex - 2;

            // Clamp iS2 to ensure we have enough samples
            iS2 = minValidIndex < 0 ? 0 : minValidIndex;

            // Direct access with bounds already verified
            var s2_snap = buffer[iS2];
            var s1_snap = iS2 + 1 <= maxIndex ? buffer[iS2 + 1] : s2_snap;
            var s0_snap = iS2 + 2 <= maxIndex ? buffer[iS2 + 2] : s1_snap;
            var s3_snap = iS2 > 0 ? buffer[iS2 - 1] : (NumericValueChangeSnapshot?)null;

            // Pull quaternions
            Quaternion q0 = s0_snap.numericValue.UnityEngine_Quaternion;
            Quaternion q1 = s1_snap.numericValue.UnityEngine_Quaternion;
            Quaternion q2 = s2_snap.numericValue.UnityEngine_Quaternion;

            long t0 = s0_snap.elapsedTicksAtChange;
            long t1 = s1_snap.elapsedTicksAtChange;
            long t2 = s2_snap.elapsedTicksAtChange;

            // Branch based on configuration
            if (UseAdvancedMotionAnalysis)
            {
                return GetQuaternionExtrapolation_Advanced(
                    q0, q1, q2, t0, t1, t2,
                    s3_snap, atElapsedTicks);
            }
            else
            {
                return GetQuaternionExtrapolation_Simple(
                    q0, q1, q2, t0, t1, t2,
                    s3_snap, atElapsedTicks);
            }
        }

        private Quaternion GetQuaternionExtrapolation_Advanced(
            Quaternion q0, Quaternion q1, Quaternion q2,
            long t0, long t1, long t2,
            NumericValueChangeSnapshot? s3_snap,
            long atElapsedTicks)
        {
            // Use QuaternionDelta for better motion analysis
            QuaternionDelta delta10 = ComputeDelta(q0, q1, t0, t1);
            QuaternionDelta delta21 = ComputeDelta(q1, q2, t1, t2);

            // Log motion analysis if enabled
            if (EnableMotionAnalysisLogging)
            {
                GONetLog.Debug($"[Quaternion Motion] Δ1: {delta10.AngularVelocityDegrees:F1}°/s, Δ2: {delta21.AngularVelocityDegrees:F1}°/s");

                // Detect acceleration
                if (delta10.timeDelta > 0 && delta21.timeDelta > 0)
                {
                    float accel = (delta21.AngularVelocity - delta10.AngularVelocity) / (delta21.timeDelta * 1e-7f);
                    GONetLog.Debug($"[Quaternion Motion] Angular acceleration: {accel * Mathf.Rad2Deg:F1}°/s²");
                }
            }

            // Check if we have valid deltas
            if (delta21.timeDelta <= 0)
                return q2;

            // Compute or use actual q3
            Quaternion q3;
            if (s3_snap.HasValue)
            {
                q3 = s3_snap.Value.numericValue.UnityEngine_Quaternion;
            }
            else if (delta10.timeDelta > 0)
            {
                // Use delta information for more accurate prediction
                q3 = PredictQ3_WithDeltas(q2, delta10, delta21);
            }
            else
            {
                // Can't predict without valid spans
                q3 = q2;
            }

            // Calculate interpolation parameter
            long deltaFromS1 = atElapsedTicks - t1;
            float t = delta21.timeDelta > 0 ? deltaFromS1 / (float)delta21.timeDelta : 0f;

            // Determine if we're interpolating or extrapolating
            Quaternion result;
            if (t <= 0f)
            {
                result = q1;
            }
            else if (t < 1f)
            {
                result = QuaternionUtilsOptimized.SquadFast(q0, q1, q2, q3, t);
            }
            else
            {
                float extrapFactor = (atElapsedTicks - t2) / (float)delta21.timeDelta;
                result = QuaternionUtils.SlerpUnclamped(ref q2, ref q3, extrapFactor);
            }

            // Apply constraints with delta information
            result = ApplyExtrapolationConstraints_WithDeltas(q2, result, delta21);

            return result;
        }

        private Quaternion GetQuaternionExtrapolation_Simple(
            Quaternion q0, Quaternion q1, Quaternion q2,
            long t0, long t1, long t2,
            NumericValueChangeSnapshot? s3_snap,
            long atElapsedTicks)
        {
            // Calculate spans
            long span21 = t2 - t1;
            long span10 = t1 - t0;

            if (span21 <= 0)
                return q2;

            // Compute or use actual q3
            Quaternion q3;
            if (s3_snap.HasValue)
            {
                q3 = s3_snap.Value.numericValue.UnityEngine_Quaternion;
            }
            else if (span10 > 0)
            {
                q3 = PredictQ3_Simple(q0, q1, q2, span10, span21);
            }
            else
            {
                q3 = q2;
            }

            // Calculate interpolation parameter
            long deltaFromS1 = atElapsedTicks - t1;
            float t = span21 > 0 ? deltaFromS1 / (float)span21 : 0f;

            // Determine if we're interpolating or extrapolating
            Quaternion result;
            if (t <= 0f)
            {
                result = q1;
            }
            else if (t < 1f)
            {
                result = QuaternionUtilsOptimized.SquadFast(q0, q1, q2, q3, t);
            }
            else
            {
                float extrapFactor = (atElapsedTicks - t2) / (float)span21;
                result = QuaternionUtils.SlerpUnclamped(ref q2, ref q3, extrapFactor);
            }

            // Apply simple constraints
            result = ApplyExtrapolationConstraints_Simple(q2, result, span21);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion PredictQ3_WithDeltas(Quaternion q2, QuaternionDelta delta10, QuaternionDelta delta21)
        {
            // Scale factor based on time ratios
            float alpha = delta21.timeDelta / (float)delta10.timeDelta;

            // Scale the first delta's rotation
            Quaternion d10_scaled = Quaternion.SlerpUnclamped(
                Quaternion.identity,
                delta10.rotation,
                alpha);

            // Compute acceleration (change in angular velocity)
            Quaternion dd = delta21.rotation * Quaternion.Inverse(d10_scaled);

            // Apply to predict next position
            return q2 * delta21.rotation * dd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion PredictQ3_Simple(Quaternion q0, Quaternion q1, Quaternion q2, long span10, long span21)
        {
            // Direct quaternion operations
            Quaternion invQ1 = Quaternion.Inverse(q1);
            Quaternion d21 = q2 * invQ1;
            Quaternion d10 = q1 * Quaternion.Inverse(q0);

            float alpha = span21 / (float)span10;
            Quaternion d10_scaled = Quaternion.SlerpUnclamped(Quaternion.identity, d10, alpha);
            Quaternion dd = d21 * Quaternion.Inverse(d10_scaled);

            return q2 * d21 * dd;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion ApplyExtrapolationConstraints_WithDeltas(
            Quaternion lastKnown,
            Quaternion predicted,
            QuaternionDelta recentDelta)
        {
            // Use angular velocity from delta for dynamic threshold
            float angularVelocityDegPerSec = recentDelta.AngularVelocityDegrees;

            // Dynamic threshold based on recent angular velocity
            const float BASE_MAX_JUMP_DEGREES = 90f;
            const float MIN_MAX_JUMP_DEGREES = 30f;
            const float VELOCITY_SCALE_FACTOR = 0.01f; // Tunable

            // Higher velocity = lower threshold (more conservative)
            float maxJumpDegrees = BASE_MAX_JUMP_DEGREES / (1f + angularVelocityDegPerSec * VELOCITY_SCALE_FACTOR);
            maxJumpDegrees = maxJumpDegrees < MIN_MAX_JUMP_DEGREES ? MIN_MAX_JUMP_DEGREES : maxJumpDegrees;

            // Measure the jump
            float jumpAngle = QuaternionUtilsOptimized.AngleFast(lastKnown, predicted);

            if (jumpAngle > maxJumpDegrees)
            {
                predicted = QuaternionUtilsOptimized.RotateTowardsFast(lastKnown, predicted, maxJumpDegrees);

                if (EnableMotionAnalysisLogging && jumpAngle > maxJumpDegrees * 1.5f)
                {
                    GONetLog.Warning($"[Quaternion] Large jump: {jumpAngle:F1}° at {angularVelocityDegPerSec:F1}°/s (clamped to {maxJumpDegrees:F1}°)");
                }
            }

            // Adaptive smoothing based on angular velocity and jump size
            float velocityFactor = angularVelocityDegPerSec / 360f; // Normalize to revolutions/sec
            float jumpFactor = jumpAngle / 180f;
            float smoothingFactor = 0.2f + (velocityFactor * 0.2f) + (jumpFactor * 0.1f);
            smoothingFactor = smoothingFactor > 0.5f ? 0.5f : (smoothingFactor < 0.2f ? 0.2f : smoothingFactor);

            return QuaternionUtilsOptimized.SlerpFast(lastKnown, predicted, smoothingFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion ApplyExtrapolationConstraints_Simple(
            Quaternion lastKnown,
            Quaternion predicted,
            long recentSpanTicks)
        {
            const float BASE_MAX_JUMP_DEGREES = 90f;
            const float MIN_MAX_JUMP_DEGREES = 30f;

            // Simple threshold based on update rate
            float updateRateHz = TimeSpan.TicksPerSecond / (float)recentSpanTicks;
            float maxJumpDegrees = BASE_MAX_JUMP_DEGREES / (1f + updateRateHz * 0.1f);
            maxJumpDegrees = maxJumpDegrees < MIN_MAX_JUMP_DEGREES ? MIN_MAX_JUMP_DEGREES : maxJumpDegrees;

            float jumpAngle = QuaternionUtilsOptimized.AngleFast(lastKnown, predicted);

            if (jumpAngle > maxJumpDegrees)
            {
                predicted = QuaternionUtilsOptimized.RotateTowardsFast(lastKnown, predicted, maxJumpDegrees);
            }

            // Fixed smoothing factor
            return QuaternionUtilsOptimized.SlerpFast(lastKnown, predicted, 0.2f);
        }

        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Quaternion;

        public string Description => UseAdvancedMotionAnalysis
            ? "Advanced quaternion blending with motion analysis for varied rotation profiles (FPS, vehicles, etc)"
            : "Optimized quaternion blending for consistent rotation patterns";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks,
            out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (valueCount > 0)
            {
                int newestBufferIndex = 0;
                NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];
                int oldestBufferIndex = valueCount - 1;
                NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];

                bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) <
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;

                if (isNewestRecentEnoughToProcess)
                {
                    const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;

                    if (IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange)
                    {
                        // Find the base snapshot to extrapolate from
                        int iBase = newestBufferIndex;
                        if (atElapsedTicks < newest.elapsedTicksAtChange && atElapsedTicks > oldest.elapsedTicksAtChange)
                        {
                            for (int i = newestBufferIndex; i <= oldestBufferIndex; ++i)
                            {
                                if (valueBuffer[i].elapsedTicksAtChange <= atElapsedTicks)
                                {
                                    iBase = i;
                                    break;
                                }
                            }
                        }

                        NumericValueChangeSnapshot baseSnap = valueBuffer[iBase];
                        int valueCountUsable;

                        // Determine usable value count
                        if (iBase == newestBufferIndex && valueBuffer[newestBufferIndex].elapsedTicksAtChange > atElapsedTicks)
                        {
                            valueCountUsable = 0;
                        }
                        else
                        {
                            valueCountUsable = oldestBufferIndex - iBase + 1;
                        }

                        bool isEnoughInfoToExtrapolate = valueCountUsable >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE;

                        if (isEnoughInfoToExtrapolate)
                        {
                            if (valueCountUsable > 2)
                            {
                                blendedValue = GetQuaternionAccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, iBase, baseSnap);
                            }
                            else
                            {
                                // Simple extrapolation for 2 values
                                NumericValueChangeSnapshot justBeforeBase = valueBuffer[iBase + 1];
                                Quaternion baseValue = baseSnap.numericValue.UnityEngine_Quaternion;
                                Quaternion justBeforeValue = justBeforeBase.numericValue.UnityEngine_Quaternion;

                                Quaternion diffRotation = baseValue * Quaternion.Inverse(justBeforeValue);
                                float interpolationTime = (atElapsedTicks - baseSnap.elapsedTicksAtChange) /
                                                        (float)(baseSnap.elapsedTicksAtChange - justBeforeBase.elapsedTicksAtChange);
                                Quaternion extrapolatedRotation = baseValue * diffRotation;
                                blendedValue = QuaternionUtils.SlerpUnclamped(
                                    ref baseValue,
                                    ref extrapolatedRotation,
                                    interpolationTime);
                            }

                            // Only true when we're extrapolating past ALL known data
                            didExtrapolatePastMostRecentChanges = valueCountUsable == valueCount;
                        }
                        else
                        {
                            blendedValue = baseSnap.numericValue.UnityEngine_Quaternion;
                        }
                    }
                    else if (atElapsedTicks <= oldest.elapsedTicksAtChange)
                    {
                        // Time is before our oldest known value
                        blendedValue = oldest.numericValue.UnityEngine_Quaternion;
                    }
                    else
                    {
                        // Normal interpolation between known values
                        bool didInterpolate = false;
                        for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                        {
                            NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                            if (atElapsedTicks <= newer.elapsedTicksAtChange)
                            {
                                NumericValueChangeSnapshot older = valueBuffer[i];

                                float interpolationTime = (atElapsedTicks - older.elapsedTicksAtChange) /
                                                        (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
                                blendedValue = Quaternion.Slerp(
                                    older.numericValue.UnityEngine_Quaternion,
                                    newer.numericValue.UnityEngine_Quaternion,
                                    interpolationTime);
                                didInterpolate = true;
                                break;
                            }
                        }

                        if (!didInterpolate)
                        {
                            GONetLog.Debug("Failed to interpolate quaternion - using newest value");
                            blendedValue = newest.numericValue.UnityEngine_Quaternion;
                        }
                    }

                    // Apply conditional smoothing after all interpolation/extrapolation
                    ValueBlendUtils.Quaternion_ApplySmoothing_IfAppropriate(ref valueBuffer, valueCount, ref blendedValue);
                }
                else
                {
                    // Data is too old - stop processing
                    return false;
                }

                return true;
            }

            return false;
        }
    }
    public class GONetValueBlending_Vector3_GrokOut : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "What Grok 3 beta implemented for us to try.";

        private readonly struct State
        {
            public readonly Vector3 PreviousPosition;
            public readonly Vector3 PreviousVelocity;

            public State(Vector3 previousPosition, Vector3 previousVelocity)
            {
                PreviousPosition = previousPosition;
                PreviousVelocity = previousVelocity;
            }
        }

        private readonly Dictionary<NumericValueChangeSnapshot[], State> stateCache = new Dictionary<NumericValueChangeSnapshot[], State>();
        private readonly object lockObject = new object();

        private const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;
        private const float MAX_EXTRAPOLATION_TIME = 0.2f; // Limit extrapolation to 200ms
        private const float MIN_DELTA_TIME = 0.01f; // Prevent division by near-zero
        private const float MAX_VELOCITY = 50f; // Cap velocity (adjust for game)
        private const float POSITION_SMOOTH_FACTOR = 0.5f; // Lerp factor for position smoothing
        private const float VELOCITY_SMOOTH_FACTOR = 0.3f; // Lerp factor for velocity smoothing
        private const float MAX_POSITION_DELTA = 0.5f; // Max position change per frame (adjust for game)

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolate)
        {
            blendedValue = default;
            didExtrapolate = IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH;

            // Retrieve or initialize state using valueBuffer as key
            State state;
            lock (lockObject)
            {
                if (!stateCache.TryGetValue(valueBuffer, out state))
                {
                    state = new State(Vector3.zero, Vector3.zero);
                    stateCache[valueBuffer] = state;
                }
            }

            Vector3 previousPosition = state.PreviousPosition;
            Vector3 previousVelocity = state.PreviousVelocity;

            if (valueCount < 2)
            {
                if (valueCount == 1)
                {
                    Vector3 newPosition = valueBuffer[0].numericValue.UnityEngine_Vector3;
                    newPosition = Vector3.Lerp(previousPosition, newPosition, POSITION_SMOOTH_FACTOR);
                    blendedValue.UnityEngine_Vector3 = newPosition;

                    lock (lockObject)
                    {
                        stateCache[valueBuffer] = new State(newPosition, Vector3.zero);
                    }
                    return true;
                }
                return false;
            }

            NumericValueChangeSnapshot latestSnapshot = valueBuffer[0];
            NumericValueChangeSnapshot prevSnapshot = valueBuffer[1];

            if (latestSnapshot.elapsedTicksAtChange == atElapsedTicks)
            {
                Vector3 newPosition = latestSnapshot.numericValue.UnityEngine_Vector3;
                newPosition = Vector3.Lerp(previousPosition, newPosition, POSITION_SMOOTH_FACTOR);
                blendedValue.UnityEngine_Vector3 = newPosition;

                lock (lockObject)
                {
                    stateCache[valueBuffer] = new State(newPosition, Vector3.zero);
                }
                return true;
            }

            // Interpolation when allowed and possible
            if (!IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH &&
                atElapsedTicks > prevSnapshot.elapsedTicksAtChange &&
                atElapsedTicks < latestSnapshot.elapsedTicksAtChange)
            {
                float t = (atElapsedTicks - prevSnapshot.elapsedTicksAtChange) /
                          (float)(latestSnapshot.elapsedTicksAtChange - prevSnapshot.elapsedTicksAtChange);
                Vector3 newPosition = Vector3.Lerp(
                    prevSnapshot.numericValue.UnityEngine_Vector3,
                    latestSnapshot.numericValue.UnityEngine_Vector3,
                    t);
                newPosition = Vector3.Lerp(previousPosition, newPosition, POSITION_SMOOTH_FACTOR);

                Vector3 velocity = (latestSnapshot.numericValue.UnityEngine_Vector3 - prevSnapshot.numericValue.UnityEngine_Vector3) /
                                  ((latestSnapshot.elapsedTicksAtChange - prevSnapshot.elapsedTicksAtChange) / (float)TimeSpan.TicksPerSecond);
                velocity = Vector3.Lerp(previousVelocity, velocity, VELOCITY_SMOOTH_FACTOR);

                blendedValue.UnityEngine_Vector3 = newPosition;
                didExtrapolate = false;

                lock (lockObject)
                {
                    stateCache[valueBuffer] = new State(newPosition, velocity);
                }
                return true;
            }

            // Extrapolation
            float deltaTime = (latestSnapshot.elapsedTicksAtChange - prevSnapshot.elapsedTicksAtChange) / (float)TimeSpan.TicksPerSecond;
            float extrapolateTime = (atElapsedTicks - latestSnapshot.elapsedTicksAtChange) / (float)TimeSpan.TicksPerSecond;

            deltaTime = Mathf.Max(deltaTime, MIN_DELTA_TIME);
            extrapolateTime = Mathf.Min(extrapolateTime, MAX_EXTRAPOLATION_TIME);

            Vector3 rawVelocity = (latestSnapshot.numericValue.UnityEngine_Vector3 - prevSnapshot.numericValue.UnityEngine_Vector3) / deltaTime;
            Vector3 smoothedVelocity = Vector3.Lerp(previousVelocity, rawVelocity, VELOCITY_SMOOTH_FACTOR);

            float velocityMagnitude = smoothedVelocity.magnitude;
            if (velocityMagnitude > MAX_VELOCITY)
            {
                smoothedVelocity = smoothedVelocity.normalized * MAX_VELOCITY;
            }

            Vector3 newPosition1 = latestSnapshot.numericValue.UnityEngine_Vector3 + smoothedVelocity * extrapolateTime;
            newPosition1 = Vector3.Lerp(previousPosition, newPosition1, POSITION_SMOOTH_FACTOR);

            Vector3 delta = newPosition1 - previousPosition;
            float deltaMagnitude = delta.magnitude;
            if (deltaMagnitude > MAX_POSITION_DELTA)
            {
                newPosition1 = previousPosition + delta.normalized * MAX_POSITION_DELTA;
            }

            blendedValue.UnityEngine_Vector3 = newPosition1;

            lock (lockObject)
            {
                stateCache[valueBuffer] = new State(newPosition1, smoothedVelocity);
            }

            return true;
        }

        // Clean up state for a specific valueBuffer (e.g., when object is destroyed)
        public void RemoveBufferState(NumericValueChangeSnapshot[] valueBuffer)
        {
            lock (lockObject)
            {
                stateCache.Remove(valueBuffer);
            }
        }
    }

    public class GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter : IGONetAutoMagicalSync_CustomValueBlending
    {
        public static bool ShouldLog = false;

        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "Provides a good blending solution for Vector3s with component values that change with linear velocity and/or fixed acceleration.  Will not perform as well for jittery or somewhat chaotic value changes.";

        public static void LogValueBufferEntries(NumericValueChangeSnapshot[] valueBuffer, int valueCount)
        {
            if (ShouldLog)
            {
                string logMessage = $"ValueBuffer Contents (count: {valueCount}):\n";
                for (int i = 0; i < valueCount; ++i)
                {
                    NumericValueChangeSnapshot snapshot = valueBuffer[i];
                    logMessage += $"[{i}] ticks: {snapshot.elapsedTicksAtChange} (ms: {TimeSpan.FromTicks(snapshot.elapsedTicksAtChange).TotalMilliseconds:F2}), value: {snapshot.numericValue.UnityEngine_Vector3}\n";
                }
                GONetLog.Debug(logMessage);
            }
        }

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (ShouldLog)
            {
                GONetLog.Debug($"VECTOR3 buffer value caount: {valueCount}, atElapsedTicks (as ms): {TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds}, game time elapsed seconds: {GONetMain.Time.ElapsedSeconds}, game time elapsed seconds (client sim): {GONetMain.Time.ElapsedSeconds_ClientSimulation}");
                LogValueBufferEntries(valueBuffer, valueCount);
            }
            if (valueCount > 0)
            {
                { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                    int newestBufferIndex = 0;
                    NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];

                    if (float.IsNaN(newest.numericValue.UnityEngine_Vector3.x) ||
                        float.IsNaN(newest.numericValue.UnityEngine_Vector3.y) ||
                        float.IsNaN(newest.numericValue.UnityEngine_Vector3.z))
                    {
                        if (ShouldLog) GONetLog.Warning("Input data contains NaN value(s) for newest value in buffer.");
                        return false;
                    }

                    int oldestBufferIndex = valueCount - 1;
                    NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];

                    bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) < GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;
                    if (isNewestRecentEnoughToProcess)
                    {
                        Vector3 newestValue = newest.numericValue.UnityEngine_Vector3;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            //if (ShouldLog) GONetLog.Debug("values: " + valueCount + ", atElapsedTicks: " + atElapsedTicks + ", bufferHas: " + valueBuffer.GetHashCode() + ' ' + string.Join('-', valueBuffer.Select(x => string.Concat('{', 't', ':', x.elapsedTicksAtChange , ',', x.numericValue.ToString(), '}'))));

                            const bool IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH = true;
                            if (IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                //float average, min, max;
                                //DetermineTimeBetweenStats(valueBuffer, valueCount, out min, out average, out max);
                                //GONetLog.Debug($"millis between snaps: min: {min} avg: {average} max: {max}");

                                // 1) find the “base” snapshot to extrapolate from:
                                //    - if target is at or after newest, baseIndex = newestIndex
                                //    - else if it's between oldest and newest, pick the closest one <= atElapsedTicks
                                int iBase = newestBufferIndex;
                                if (atElapsedTicks < newest.elapsedTicksAtChange && atElapsedTicks > oldest.elapsedTicksAtChange)
                                {
                                    for (int i = newestBufferIndex; i <= oldestBufferIndex; ++i)
                                    {
                                        if (valueBuffer[i].elapsedTicksAtChange <= atElapsedTicks)
                                        {
                                            iBase = i;
                                            break;
                                        }
                                    }
                                }
                                NumericValueChangeSnapshot baseSnap = valueBuffer[iBase];
                                Vector3 baseValue = baseSnap.numericValue.UnityEngine_Vector3;
                                int valueCountUsable;
                                // If iBase is still at newestBufferIndex and the newest is newer than target,
                                // then no entries are usable
                                if (iBase == newestBufferIndex && valueBuffer[newestBufferIndex].elapsedTicksAtChange > atElapsedTicks)
                                {
                                    valueCountUsable = 0;
                                }
                                else
                                {
                                    valueCountUsable = oldestBufferIndex - iBase + 1;
                                }

                                if (ShouldLog) GONetLog.Debug("if EXTRAPO, value count usable: " + valueCountUsable);
                                bool isEnoughInfoToExtrapolate = valueCountUsable >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    if (valueCountUsable > 3)
                                    {
                                        Vector3 averageAcceleration;
                                        int countToUse = valueCountUsable < 4 ? valueCountUsable : 4;
                                        if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, countToUse, out averageAcceleration, iBase))
                                        {
                                            //GONetLog.Debug($"avg accel: {averageAcceleration}");
                                            blendedValue = baseSnap.numericValue.UnityEngine_Vector3 + averageAcceleration * (float)TimeSpan.FromTicks(atElapsedTicks - baseSnap.elapsedTicksAtChange).TotalSeconds;
                                        }
                                    }
                                    else if (valueCountUsable > 2)
                                    {
                                        Vector3 acceleration;
                                        blendedValue = ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(valueBuffer, atElapsedTicks, iBase, baseSnap, out acceleration);
                                    }
                                    else
                                    {
                                        // Get direct pointer access to Vector3 components - no struct copies
                                        unsafe
                                        {
                                            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
                                            {
                                                // Direct pointer to justBeforeNewest Vector3 components
                                                float* justBefore_components = (float*)((byte*)&bufferPtr[iBase + 1].numericValue + 1);
                                                float* base_components = (float*)((byte*)&baseSnap.numericValue + 1);

                                                // Direct component access - no Vector3 struct operations
                                                float justBefore_x = justBefore_components[0];
                                                float justBefore_y = justBefore_components[1];
                                                float justBefore_z = justBefore_components[2];

                                                float base_x = base_components[0];
                                                float base_y = base_components[1];
                                                float base_z = base_components[2];

                                                // Component-wise difference calculation
                                                float diff_x = base_x - justBefore_x;
                                                float diff_y = base_y - justBefore_y;
                                                float diff_z = base_z - justBefore_z;

                                                // Direct tick access
                                                long ticksBetweenLastTwo = baseSnap.elapsedTicksAtChange - bufferPtr[iBase + 1].elapsedTicksAtChange;
                                                long atMinusNewestTicks = atElapsedTicks - baseSnap.elapsedTicksAtChange;

                                                // Guard against zero/negative time differences
                                                if (ticksBetweenLastTwo <= 0)
                                                {
                                                    blendedValue = new Vector3(base_x, base_y, base_z);
                                                    if (ShouldLog) GONetLog.Debug("Zero/negative time diff, using base value");
                                                }
                                                else
                                                {
                                                    // Optimized extrapolation calculation
                                                    int extrapolationSections = (int)Math.Ceiling(atMinusNewestTicks / (float)ticksBetweenLastTwo);
                                                    if (extrapolationSections < 0) extrapolationSections = -extrapolationSections;

                                                    if (extrapolationSections == 0)
                                                    {
                                                        blendedValue = new Vector3(base_x, base_y, base_z);
                                                        if (ShouldLog) GONetLog.Debug("Zero extrapolation sections, using base value");
                                                    }
                                                    else
                                                    {
                                                        long extrapolated_TicksAtChange = baseSnap.elapsedTicksAtChange + (ticksBetweenLastTwo * extrapolationSections);

                                                        // Component-wise extrapolated value calculation (no Vector3 operators)
                                                        float extrapolated_x = base_x + (diff_x * extrapolationSections);
                                                        float extrapolated_y = base_y + (diff_y * extrapolationSections);
                                                        float extrapolated_z = base_z + (diff_z * extrapolationSections);

                                                        long denominator = extrapolated_TicksAtChange - baseSnap.elapsedTicksAtChange;
                                                        if (denominator <= 0)
                                                        {
                                                            blendedValue = new Vector3(base_x, base_y, base_z);
                                                            if (ShouldLog) GONetLog.Debug("Invalid denominator, using base value");
                                                        }
                                                        else
                                                        {
                                                            float interpolationTime = atMinusNewestTicks / (float)denominator;
                                                            float oneSectionPercentage = 1f / (extrapolationSections + 1);
                                                            float remainingSectionPercentage = 1f - oneSectionPercentage;
                                                            float bezierTime = oneSectionPercentage + (interpolationTime * remainingSectionPercentage);

                                                            // Final NaN check before bezier calculation
                                                            if (float.IsNaN(bezierTime) || float.IsInfinity(bezierTime))
                                                            {
                                                                blendedValue = new Vector3(base_x, base_y, base_z);
                                                                if (ShouldLog) GONetLog.Warning($"Invalid bezierTime: {bezierTime}, using base value");
                                                            }
                                                            else
                                                            {
                                                                // Create Vector3 structs only for the bezier call (unavoidable)
                                                                Vector3 justBeforeVec = new Vector3(justBefore_x, justBefore_y, justBefore_z);
                                                                Vector3 baseVec = new Vector3(base_x, base_y, base_z);
                                                                Vector3 extrapolatedVec = new Vector3(extrapolated_x, extrapolated_y, extrapolated_z);

                                                                blendedValue = ValueBlendUtils.GetQuadraticBezierValue(justBeforeVec, baseVec, extrapolatedVec, bezierTime);

                                                                // Check result for NaN (direct component access)
                                                                Vector3 result = blendedValue.UnityEngine_Vector3;
                                                                if (float.IsNaN(result.x) || float.IsNaN(result.y) || float.IsNaN(result.z))
                                                                {
                                                                    blendedValue = new Vector3(base_x, base_y, base_z);
                                                                    if (ShouldLog) GONetLog.Warning("Bezier result contained NaN, using base value");
                                                                }
                                                                else
                                                                {
                                                                    if (ShouldLog) GONetLog.Debug("extroip'd....p0: " + justBeforeVec + " p1: " + baseVec + " p2: " + extrapolatedVec + " blended: " + result + " t: " + bezierTime);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    // BELOW: maybe better to use time `atElapsedTicks > valueBuffer[0].elapsedTicksAtChange`, but this is (currently) accurate and faster comparison
                                    didExtrapolatePastMostRecentChanges = valueCountUsable == valueCount;
                                }
                                else
                                {
                                    blendedValue = baseValue; // was: newestValue;
                                    if (ShouldLog) GONetLog.Debug("VECTOR3 new new beast");
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

                            ValueBlendUtils.Vector3_ApplySmoothing_IfAppropriate(ref valueBuffer, valueCount, ref blendedValue);
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

    public class GONetValueBlending_Vector3_HighPerformance : IGONetAutoMagicalSync_CustomValueBlending
    {
        public static bool ShouldLog = false;

        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "High performance testing.";

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out GONetSyncableValue blendedValue, out bool didExtrapolate)
        {
            if (valueCount == 0)
            {
                blendedValue = Vector3.zero;
                didExtrapolate = false;
                return false;
            }

            blendedValue = valueBuffer[0].numericValue; // choose most recent value
            didExtrapolate = false;
            return true;
        }
    }

    #endregion
}
