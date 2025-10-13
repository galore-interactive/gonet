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
using System.Text;
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

        public string Description => "Provides a good blending solution for floats that change with linear velocity and/or fixed acceleration. Will not perform as well for jittery or somewhat chaotic value changes.";

        // TODO once at min Unity version that allows this: [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks,
            out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (valueCount <= 0)
                return false;

            fixed (NumericValueChangeSnapshot* bufferPtr = valueBuffer)
            {
                int newestBufferIndex = 0;
                int oldestBufferIndex = valueCount - 1;

                long newestTicks = bufferPtr[newestBufferIndex].elapsedTicksAtChange;
                long ticksSinceNewest = atElapsedTicks - newestTicks;

                // Early exit if data is too old
                if (ticksSinceNewest >= GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS)
                    return false;

                // Fast path for single value
                if (valueCount == 1)
                {
                    blendedValue = bufferPtr[0].numericValue.System_Single;
                    return true;
                }

                long oldestTicks = bufferPtr[oldestBufferIndex].elapsedTicksAtChange;

                if (ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newestTicks)
                {
                    // Use optimized method with unsafe pointer
                    int valueCountUsable = DetermineUsableValueCountForForcedExtrapolation_Unsafe(
                        bufferPtr, valueCount, atElapsedTicks, newestBufferIndex, oldestBufferIndex, out int iBase);

                    float baseValue = bufferPtr[iBase].numericValue.System_Single;

                    if (valueCountUsable < ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE)
                    {
                        blendedValue = baseValue;
                        return true;
                    }

                    // Branch-free selection of extrapolation method
                    if (valueCountUsable > 3)
                    {
                        blendedValue = GetFloatAccelerationBasedExtrapolation_Unsafe(
                            bufferPtr, valueCountUsable, atElapsedTicks, iBase);
                    }
                    else if (valueCountUsable > 2)
                    {
                        blendedValue = GetFloatSimpleAccelerationExtrapolation_Unsafe(
                            bufferPtr, atElapsedTicks, iBase);
                    }
                    else
                    {
                        blendedValue = GetFloatBezierExtrapolation_Unsafe(
                            bufferPtr, atElapsedTicks, iBase);
                    }

                    didExtrapolatePastMostRecentChanges = valueCountUsable == valueCount;
                }
                else if (atElapsedTicks <= oldestTicks)
                {
                    blendedValue = bufferPtr[oldestBufferIndex].numericValue.System_Single;
                }
                else
                {
                    // Binary search for interpolation
                    blendedValue = InterpolateFloat_BinarySearch(
                        bufferPtr, newestBufferIndex, oldestBufferIndex, atElapsedTicks);
                }

                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int DetermineUsableValueCountForForcedExtrapolation_Unsafe(
            NumericValueChangeSnapshot* bufferPtr,
            int valueCount,
            long atElapsedTicks,
            int newestBufferIndex,
            int oldestBufferIndex,
            out int baseIndex)
        {
            long newestTicks = bufferPtr[newestBufferIndex].elapsedTicksAtChange;

            if (atElapsedTicks >= newestTicks)
            {
                baseIndex = newestBufferIndex;
                return valueCount;
            }

            long oldestTicks = bufferPtr[oldestBufferIndex].elapsedTicksAtChange;

            if (atElapsedTicks <= oldestTicks)
            {
                baseIndex = oldestBufferIndex;
                return 1;
            }

            // Binary search
            int left = newestBufferIndex;
            int right = oldestBufferIndex;

            while (left < right)
            {
                int mid = left + ((right - left) >> 1);

                if (bufferPtr[mid].elapsedTicksAtChange > atElapsedTicks)
                    left = mid + 1;
                else
                    right = mid;
            }

            baseIndex = left;
            return oldestBufferIndex - left + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float GetFloatAccelerationBasedExtrapolation_Unsafe(
            NumericValueChangeSnapshot* bufferPtr, int valueCountUsable, long atElapsedTicks, int baseIndex)
        {
            const float TICKS_TO_SECONDS = 1e-7f;

            // Use double for accumulation to reduce floating point error
            double totalVelocity = 0.0;
            double totalSeconds = 0.0;

            int endIndex = baseIndex + valueCountUsable - 1;

            // Unroll by 2 for better performance
            int i = baseIndex;
            for (; i < endIndex - 1; i += 2)
            {
                // First pair
                float v1_0 = bufferPtr[i].numericValue.System_Single;
                float v2_0 = bufferPtr[i + 1].numericValue.System_Single;
                long dt_0 = bufferPtr[i].elapsedTicksAtChange - bufferPtr[i + 1].elapsedTicksAtChange;

                // Second pair
                float v1_1 = bufferPtr[i + 1].numericValue.System_Single;
                float v2_1 = bufferPtr[i + 2].numericValue.System_Single;
                long dt_1 = bufferPtr[i + 1].elapsedTicksAtChange - bufferPtr[i + 2].elapsedTicksAtChange;

                double seconds_0 = dt_0 * TICKS_TO_SECONDS;
                double seconds_1 = dt_1 * TICKS_TO_SECONDS;

                totalVelocity += (v1_0 - v2_0) + (v1_1 - v2_1);
                totalSeconds += seconds_0 + seconds_1;
            }

            // Handle remaining
            for (; i < endIndex; ++i)
            {
                float v1 = bufferPtr[i].numericValue.System_Single;
                float v2 = bufferPtr[i + 1].numericValue.System_Single;
                long dt = bufferPtr[i].elapsedTicksAtChange - bufferPtr[i + 1].elapsedTicksAtChange;

                totalVelocity += v1 - v2;
                totalSeconds += dt * TICKS_TO_SECONDS;
            }

            if (totalSeconds <= 0.0)
                return bufferPtr[baseIndex].numericValue.System_Single;

            float averageVelocity = (float)(totalVelocity / totalSeconds);
            float baseValue = bufferPtr[baseIndex].numericValue.System_Single;
            float timeSinceBase = (atElapsedTicks - bufferPtr[baseIndex].elapsedTicksAtChange) * TICKS_TO_SECONDS;

            return baseValue + (averageVelocity * timeSinceBase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float GetFloatSimpleAccelerationExtrapolation_Unsafe(
            NumericValueChangeSnapshot* bufferPtr, long atElapsedTicks, int baseIndex)
        {
            const float TICKS_TO_SECONDS = 1e-7f;

            // Direct pointer access
            float v0 = bufferPtr[baseIndex + 2].numericValue.System_Single;
            float v1 = bufferPtr[baseIndex + 1].numericValue.System_Single;
            float v2 = bufferPtr[baseIndex].numericValue.System_Single;

            long dt1_ticks = bufferPtr[baseIndex + 1].elapsedTicksAtChange - bufferPtr[baseIndex + 2].elapsedTicksAtChange;
            long dt2_ticks = bufferPtr[baseIndex].elapsedTicksAtChange - bufferPtr[baseIndex + 1].elapsedTicksAtChange;

            // Early exit if invalid time deltas
            if (dt1_ticks <= 0 || dt2_ticks <= 0)
                return v2;

            // Convert to seconds and calculate reciprocals
            float dt1 = dt1_ticks * TICKS_TO_SECONDS;
            float dt2 = dt2_ticks * TICKS_TO_SECONDS;
            float inv_dt1 = 1f / dt1;
            float inv_dt2 = 1f / dt2;

            // Calculate velocities and acceleration
            float vel1 = (v1 - v0) * inv_dt1;
            float vel2 = (v2 - v1) * inv_dt2;
            float accel = (vel2 - vel1) * inv_dt2;

            // Extrapolate
            float timeSinceBase = (atElapsedTicks - bufferPtr[baseIndex].elapsedTicksAtChange) * TICKS_TO_SECONDS;
            float timeSinceBase_squared = timeSinceBase * timeSinceBase;

            return v2 + (vel2 * timeSinceBase) + (0.5f * accel * timeSinceBase_squared);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float GetFloatBezierExtrapolation_Unsafe(
            NumericValueChangeSnapshot* bufferPtr, long atElapsedTicks, int baseIndex)
        {
            float baseValue = bufferPtr[baseIndex].numericValue.System_Single;
            float justBeforeValue = bufferPtr[baseIndex + 1].numericValue.System_Single;

            float valueDiff = baseValue - justBeforeValue;
            long ticksBetween = bufferPtr[baseIndex].elapsedTicksAtChange - bufferPtr[baseIndex + 1].elapsedTicksAtChange;

            if (ticksBetween <= 0)
                return baseValue;

            long atMinusBase = atElapsedTicks - bufferPtr[baseIndex].elapsedTicksAtChange;

            // Optimized section calculation
            float ratio = atMinusBase / (float)ticksBetween;
            int sections = (int)Math.Ceiling(ratio);
            sections = sections < 0 ? -sections : sections;

            if (sections == 0)
                return baseValue;

            // Pre-calculate values
            float extrapolatedValue = baseValue + (valueDiff * sections);
            float invSectionsPlusOne = 1f / (sections + 1);
            float bezierTime = invSectionsPlusOne + (ratio / sections) * (1f - invSectionsPlusOne);

            // Inline Bezier calculation
            float u = 1f - bezierTime;
            float u_squared = u * u;
            float t_squared = bezierTime * bezierTime;
            float two_u_t = 2f * u * bezierTime;

            return (u_squared * justBeforeValue) + (two_u_t * baseValue) + (t_squared * extrapolatedValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float InterpolateFloat_BinarySearch(
            NumericValueChangeSnapshot* bufferPtr, int newestIndex, int oldestIndex, long atElapsedTicks)
        {
            // Binary search for the interpolation bracket
            int left = newestIndex;
            int right = oldestIndex;

            while (left < right - 1)
            {
                int mid = left + ((right - left) >> 1);

                if (bufferPtr[mid].elapsedTicksAtChange > atElapsedTicks)
                    left = mid;
                else
                    right = mid;
            }

            // Now interpolate between left and right
            long t0 = bufferPtr[right].elapsedTicksAtChange;
            long t1 = bufferPtr[left].elapsedTicksAtChange;
            float v0 = bufferPtr[right].numericValue.System_Single;
            float v1 = bufferPtr[left].numericValue.System_Single;

            float t = (atElapsedTicks - t0) / (float)(t1 - t0);
            return v0 + (v1 - v0) * t; // Faster than Mathf.Lerp
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
                    if (ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange)
                    {
                        int valueCountUsable =
                            ValueBlendUtils.DetermineUsableValueCountForForcedExtrapolation(
                                valueBuffer, valueCount, atElapsedTicks, newestBufferIndex, oldestBufferIndex, out int iBase);

                        NumericValueChangeSnapshot baseSnap = valueBuffer[iBase];

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

    public sealed class GONetValueBlending_Quaternion_HermiteOptimized : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Quaternion;

        public string Description => "High-performance quaternion blending using angular velocity matching. Provides smooth rotation without overshooting.";

        // Constants
        private const float TICKS_TO_SECONDS = 1e-7f;
        private const float MAX_ANGULAR_ACCELERATION = 720f; // degrees/s²
        private const float MAX_ANGULAR_VELOCITY = 360f; // degrees/s

        // Per-object state for temporal coherence
        private struct RotationState
        {
            public Quaternion lastSmoothed;
            public Vector3 angularVelocity; // rad/s
            public long lastUpdateTicks;
        }

        private static readonly System.Collections.Generic.Dictionary<int, RotationState> objectStates =
            new System.Collections.Generic.Dictionary<int, RotationState>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (valueCount == 0) return false;

            // Single value case
            if (valueCount == 1)
            {
                blendedValue = valueBuffer[0].numericValue.UnityEngine_Quaternion;
                return true;
            }

            /* // DEBUG: Check the entire buffer right at the start
            bool allIdentical = true;
            Quaternion firstQ = valueBuffer[0].numericValue.UnityEngine_Quaternion;
            for (int i = 1; i < Math.Min(10, valueCount); i++)
            {
                Quaternion q = valueBuffer[i].numericValue.UnityEngine_Quaternion;
                float angle = Quaternion.Angle(firstQ, q);
                if (angle > 0.001f) // More than 0.001 degrees different
                {
                    allIdentical = false;
                    break;
                }
            }

            if (allIdentical)
            {
                GONetLog.Warning($"[QUATERNION DEBUG] ALL buffer values are identical!");
                GONetLog.Warning($"  Buffer size: {valueCount}");
                GONetLog.Warning($"  First 5 quaternions:");
                for (int i = 0; i < Math.Min(5, valueCount); i++)
                {
                    Quaternion q = valueBuffer[i].numericValue.UnityEngine_Quaternion;
                    GONetLog.Warning($"    [{i}]: ({q.x:F8}, {q.y:F8}, {q.z:F8}, {q.w:F8})");
                }
                GONetLog.Warning($"  This means the server is sending identical rotations - check quantization/compression!");
            }
            */

            // Find base index for forced extrapolation
            int baseIndex = 0;
            int usableCount = valueCount;

            if (ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH)
            {
                // Find the two points just before atElapsedTicks
                for (int i = 0; i < valueCount - 1; i++)
                {
                    if (atElapsedTicks >= valueBuffer[i].elapsedTicksAtChange)
                    {
                        baseIndex = 0;
                        didExtrapolatePastMostRecentChanges = true;
                        break;
                    }
                    else if (atElapsedTicks >= valueBuffer[i + 1].elapsedTicksAtChange)
                    {
                        baseIndex = i + 1;
                        usableCount = valueCount - baseIndex;
                        break;
                    }
                }
            }

            // Need at least 2 values to work with
            if (usableCount < 2)
            {
                blendedValue = valueBuffer[baseIndex].numericValue.UnityEngine_Quaternion;
                return true;
            }

            // Extract rotations and times
            Quaternion q0 = valueBuffer[baseIndex].numericValue.UnityEngine_Quaternion;
            Quaternion q1 = valueBuffer[baseIndex + 1].numericValue.UnityEngine_Quaternion;
            long t0 = valueBuffer[baseIndex].elapsedTicksAtChange;
            long t1 = valueBuffer[baseIndex + 1].elapsedTicksAtChange;

            /* // DEBUG: Log what we're about to calculate velocity from
            float angleBeforeVelocityCalc = Quaternion.Angle(q0, q1);
            GONetLog.Debug($"[QUATERNION DEBUG] About to calculate velocity:");
            GONetLog.Debug($"  q0 (base): ({q0.x:F8}, {q0.y:F8}, {q0.z:F8}, {q0.w:F8})");
            GONetLog.Debug($"  q1 (prev): ({q1.x:F8}, {q1.y:F8}, {q1.z:F8}, {q1.w:F8})");
            GONetLog.Debug($"  Angle between: {angleBeforeVelocityCalc:F8}°");
            GONetLog.Debug($"  Time delta: {(t0 - t1) * TICKS_TO_SECONDS:F6}s");
            */

            // Calculate angular velocity between q0 and q1
            Vector3 angularVel = CalculateAngularVelocityPrecise(q1, q0, t1, t0);

            /* // DEBUG: Log the result
            GONetLog.Debug($"[QUATERNION DEBUG] Angular velocity result: ({angularVel.x:F8}, {angularVel.y:F8}, {angularVel.z:F8})");
            GONetLog.Debug($"  Magnitude: {angularVel.magnitude * Mathf.Rad2Deg:F8}°/s");
            */

            /* // DEBUG: Log if angular velocity is suspiciously low
            float angVelMagnitude = angularVel.magnitude * Mathf.Rad2Deg;
            if (angVelMagnitude > 0 && angVelMagnitude < 0.1f) // Less than 0.1 deg/s
            {
                GONetLog.Debug($"[QUATERNION DEBUG] Very low angular velocity detected: {angVelMagnitude:F6}°/s");
                GONetLog.Debug($"  q0: ({q0.x:F6}, {q0.y:F6}, {q0.z:F6}, {q0.w:F6})");
                GONetLog.Debug($"  q1: ({q1.x:F6}, {q1.y:F6}, {q1.z:F6}, {q1.w:F6})");
                GONetLog.Debug($"  Time delta: {(t0 - t1) * TICKS_TO_SECONDS:F6}s");
            }
            */

            // Get angular acceleration if we have 3+ points
            Vector3 angularAccel = Vector3.zero;
            if (usableCount >= 3)
            {
                Quaternion q2 = valueBuffer[baseIndex + 2].numericValue.UnityEngine_Quaternion;
                long t2 = valueBuffer[baseIndex + 2].elapsedTicksAtChange;

                Vector3 angularVel2 = CalculateAngularVelocityPrecise(q2, q1, t2, t1);
                float dt = (t1 - t2) * TICKS_TO_SECONDS;

                if (dt > 0)
                {
                    angularAccel = (angularVel - angularVel2) / dt;

                    // Cap acceleration
                    float accelMag = angularAccel.magnitude;
                    float maxAccelRad = MAX_ANGULAR_ACCELERATION * Mathf.Deg2Rad;
                    if (accelMag > maxAccelRad)
                    {
                        angularAccel = (angularAccel / accelMag) * maxAccelRad;
                    }
                }
            }

            // Calculate extrapolation time
            float deltaTime = (atElapsedTicks - t0) * TICKS_TO_SECONDS;

            // Apply rotation with angular velocity and acceleration
            Quaternion result;

            if (deltaTime <= 0)
            {
                result = q0;
            }
            else
            {
                // Calculate the angular change
                Vector3 angularDelta = angularVel * deltaTime + 0.5f * angularAccel * deltaTime * deltaTime;

                // Hybrid approach for forced extrapolation
                if (baseIndex > 0 && !didExtrapolatePastMostRecentChanges)
                {
                    // We're ignoring a future value - blend toward it slightly
                    Quaternion futureRot = valueBuffer[baseIndex - 1].numericValue.UnityEngine_Quaternion;

                    // Apply the rotation (no threshold check - apply even tiny rotations)
                    if (!float.IsNaN(angularDelta.x) && angularDelta.sqrMagnitude > 0)
                    {
                        float angle = angularDelta.magnitude;

                        if (angle < 0.001f) // Very small rotation in radians (~0.057 degrees)
                        {
                            // Use first-order approximation for tiny rotations
                            Quaternion deltaRotation = new Quaternion(
                                angularDelta.x * 0.5f,
                                angularDelta.y * 0.5f,
                                angularDelta.z * 0.5f,
                                1f
                            );
                            deltaRotation = deltaRotation.normalized;
                            Quaternion extrapolated = deltaRotation * q0;

                            // Blend 15% toward known future to reduce error
                            result = Quaternion.Slerp(extrapolated, futureRot, 0.15f);
                        }
                        else
                        {
                            // Normal rotation for larger angles
                            Vector3 axis = angularDelta.normalized;
                            float angleDeg = angle * Mathf.Rad2Deg;
                            Quaternion deltaRotation = Quaternion.AngleAxis(angleDeg, axis);
                            Quaternion extrapolated = deltaRotation * q0;

                            // Blend 15% toward known future to reduce error
                            result = Quaternion.Slerp(extrapolated, futureRot, 0.15f);
                        }
                    }
                    else
                    {
                        result = q0;
                    }
                }
                else
                {
                    // True extrapolation (no future value to blend with)

                    // Cap total rotation for stability
                    float angle = angularDelta.magnitude;
                    float maxAngleRad = (MAX_ANGULAR_VELOCITY * Mathf.Deg2Rad) * deltaTime;
                    if (angle > maxAngleRad)
                    {
                        angularDelta = angularDelta.normalized * maxAngleRad;
                        angle = maxAngleRad;
                    }

                    // Apply rotation (no threshold - even tiny rotations)
                    if (!float.IsNaN(angularDelta.x) && angularDelta.sqrMagnitude > 0)
                    {
                        if (angle < 0.001f) // Very small rotation in radians
                        {
                            // Use first-order approximation for tiny rotations
                            Quaternion deltaRotation = new Quaternion(
                                angularDelta.x * 0.5f,
                                angularDelta.y * 0.5f,
                                angularDelta.z * 0.5f,
                                1f
                            );
                            deltaRotation = deltaRotation.normalized;
                            result = deltaRotation * q0;
                        }
                        else
                        {
                            // Normal rotation for larger angles
                            Vector3 axis = angularDelta.normalized;
                            float angleDeg = angle * Mathf.Rad2Deg;
                            Quaternion deltaRotation = Quaternion.AngleAxis(angleDeg, axis);
                            result = deltaRotation * q0;
                        }
                    }
                    else
                    {
                        result = q0;

                        /* debug
                        if (angularDelta.sqrMagnitude == 0)
                        {
                            StringBuilder bufferDebug = new StringBuilder();
                            bufferDebug.AppendLine("[QUATERNION DEBUG] No rotation applied - angular delta is zero");
                            bufferDebug.AppendLine($"  Buffer contains {valueCount} values:");

                            // Show first 10 values with full precision
                            for (int i = 0; i < Math.Min(10, valueCount); i++)
                            {
                                Quaternion q = valueBuffer[i].numericValue.UnityEngine_Quaternion;
                                long t = valueBuffer[i].elapsedTicksAtChange;
                                bufferDebug.AppendLine($"    [{i}]: ({q.x:F8}, {q.y:F8}, {q.z:F8}, {q.w:F8}) @ t={t}");

                                // Compare to first value
                                if (i > 0)
                                {
                                    Quaternion first = valueBuffer[0].numericValue.UnityEngine_Quaternion;
                                    float angle2 = Quaternion.Angle(first, q);
                                    float dotProduct = Quaternion.Dot(first, q);
                                    bufferDebug.AppendLine($"         Angle from [0]: {angle2:F8}°, Dot: {dotProduct:F8}");

                                    // Show component-level differences
                                    float dx = q.x - first.x;
                                    float dy = q.y - first.y;
                                    float dz = q.z - first.z;
                                    float dw = q.w - first.w;
                                    bufferDebug.AppendLine($"         Component diffs from [0]: x={dx:F10}, y={dy:F10}, z={dz:F10}, w={dw:F10}");
                                }
                            }

                            GONetLog.Debug(bufferDebug.ToString());
                        }
                        */
                    }
                }
            }

            // Apply temporal smoothing for consistency
            Quaternion beforeSmoothing = result;
            result = ApplyTemporalSmoothing(result, q0, deltaTime);

            /* // DEBUG: Check if smoothing killed the rotation
            float angleAfterSmoothing = Quaternion.Angle(q0, result);
            float angleBeforeSmoothing = Quaternion.Angle(q0, beforeSmoothing);
            if (angleBeforeSmoothing > 0.01f && angleAfterSmoothing < 0.01f)
            {
                GONetLog.Debug($"[QUATERNION DEBUG] Temporal smoothing eliminated rotation!");
                GONetLog.Debug($"  Before smoothing: {angleBeforeSmoothing:F6}°");
                GONetLog.Debug($"  After smoothing: {angleAfterSmoothing:F6}°");
            }
            */

            blendedValue = result;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 CalculateAngularVelocity(Quaternion from, Quaternion to, long fromTime, long toTime)
        {
            float dt = (fromTime - toTime) * TICKS_TO_SECONDS;
            if (dt <= 0) return Vector3.zero;

            // Get rotation difference
            Quaternion deltaRot = to * Quaternion.Inverse(from);

            // CRITICAL: Use quaternion components directly for small rotations
            // ToAngleAxis loses precision for small angles

            // Check if it's a small rotation (close to identity)
            float dotProduct = Mathf.Abs(deltaRot.w);

            if (dotProduct > 0.9999f) // Very small rotation
            {
                // Use small angle approximation
                // For small angles: sin(θ/2) ≈ θ/2
                // Quaternion ≈ [θx/2, θy/2, θz/2, 1]

                Vector3 angularVel = new Vector3(
                    deltaRot.x * 2f / dt,
                    deltaRot.y * 2f / dt,
                    deltaRot.z * 2f / dt
                );

                return angularVel;
            }
            else
            {
                // Normal angle-axis for larger rotations
                float angle;
                Vector3 axis;
                deltaRot.ToAngleAxis(out angle, out axis);

                if (angle > 180f)
                {
                    angle = 360f - angle;
                    axis = -axis;
                }

                float angleRad = angle * Mathf.Deg2Rad;
                return axis * (angleRad / dt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 CalculateAngularVelocityPrecise(Quaternion from, Quaternion to, long fromTime, long toTime)
        {
            float dt = (fromTime - toTime) * TICKS_TO_SECONDS;
            if (dt <= 0) return Vector3.zero;

            // Special handling for single-axis rotations with quantization artifacts
            // This handles the case where compression causes some components to stick
            const float COMPONENT_EPSILON = 0.001f;

            bool xStuck = Mathf.Abs(from.x - to.x) < COMPONENT_EPSILON;
            bool yStuck = Mathf.Abs(from.y - to.y) < COMPONENT_EPSILON;
            bool zStuck = Mathf.Abs(from.z - to.z) < COMPONENT_EPSILON;

            // Y-axis rotation (X and Z stuck)
            if (xStuck && zStuck && !yStuck)
            {
                float fromAngle = 2.0f * Mathf.Atan2(from.y, from.w);
                float toAngle = 2.0f * Mathf.Atan2(to.y, to.w);

                float deltaAngle = toAngle - fromAngle;
                if (deltaAngle > Mathf.PI) deltaAngle -= 2 * Mathf.PI;
                if (deltaAngle < -Mathf.PI) deltaAngle += 2 * Mathf.PI;

                return new Vector3(0, deltaAngle / dt, 0);
            }

            // X-axis rotation (Y and Z stuck)
            if (yStuck && zStuck && !xStuck)
            {
                float fromAngle = 2.0f * Mathf.Atan2(from.x, from.w);
                float toAngle = 2.0f * Mathf.Atan2(to.x, to.w);

                float deltaAngle = toAngle - fromAngle;
                if (deltaAngle > Mathf.PI) deltaAngle -= 2 * Mathf.PI;
                if (deltaAngle < -Mathf.PI) deltaAngle += 2 * Mathf.PI;

                return new Vector3(deltaAngle / dt, 0, 0);
            }

            // Z-axis rotation (X and Y stuck)
            if (xStuck && yStuck && !zStuck)
            {
                float fromAngle = 2.0f * Mathf.Atan2(from.z, from.w);
                float toAngle = 2.0f * Mathf.Atan2(to.z, to.w);

                float deltaAngle = toAngle - fromAngle;
                if (deltaAngle > Mathf.PI) deltaAngle -= 2 * Mathf.PI;
                if (deltaAngle < -Mathf.PI) deltaAngle += 2 * Mathf.PI;

                return new Vector3(0, 0, deltaAngle / dt);
            }

            // General case for multi-axis or no stuck components
            Quaternion deltaRot = to * Quaternion.Inverse(from);

            // Check if it's a very small rotation
            float dotProduct = Mathf.Abs(deltaRot.w);

            if (dotProduct > 0.9999f) // Very small rotation (< ~1 degree)
            {
                // Small angle approximation
                Vector3 angularVel = new Vector3(
                    deltaRot.x * 2f / dt,
                    deltaRot.y * 2f / dt,
                    deltaRot.z * 2f / dt
                );
                return angularVel;
            }

            // Standard angle-axis extraction for larger rotations
            float angle;
            Vector3 axis;
            deltaRot.ToAngleAxis(out angle, out axis);

            // Handle 180-degree ambiguity
            if (angle > 180f)
            {
                angle = 360f - angle;
                axis = -axis;
            }

            float angleRad = angle * Mathf.Deg2Rad;
            return axis * (angleRad / dt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Quaternion ApplyTemporalSmoothing(Quaternion predicted, Quaternion lastKnown, float deltaTime)
        {
            // Measure the angular difference
            float angleDiff = Quaternion.Angle(predicted, lastKnown);

            // Dynamic smoothing based on angular difference
            // Large differences = more smoothing to prevent pops
            float smoothingFactor;

            if (angleDiff < 5f)
            {
                smoothingFactor = 1.0f; // No smoothing for small changes
            }
            else if (angleDiff < 30f)
            {
                smoothingFactor = 0.8f; // Light smoothing
            }
            else if (angleDiff < 90f)
            {
                smoothingFactor = 0.5f; // Moderate smoothing
            }
            else
            {
                // Cap maximum rotation per frame
                float maxAnglePerSecond = 180f;
                float maxAngle = maxAnglePerSecond * deltaTime;

                if (angleDiff > maxAngle)
                {
                    // Rotate toward target but cap the speed
                    return Quaternion.RotateTowards(lastKnown, predicted, maxAngle);
                }

                smoothingFactor = 0.3f; // Heavy smoothing for large changes
            }

            return Quaternion.Slerp(lastKnown, predicted, smoothingFactor);
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
            didExtrapolate = ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH;

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
            if (!ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH &&
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

    public partial class GONetValueBlending_Vector3_ExtrapolateWithLowPassSmoothingFilter : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "Provides a good blending solution for Vector3s with component values that change with linear velocity and/or fixed acceleration. Will not perform as well for jittery or somewhat chaotic value changes.";

        // Add static counter for object ID tracking (temporary solution)
        private static int objectIdCounter = 0;
        private static readonly System.Collections.Generic.Dictionary<NumericValueChangeSnapshot[], int> bufferToObjectId = new System.Collections.Generic.Dictionary<NumericValueChangeSnapshot[], int>();

        public static void LogValueBufferEntries(NumericValueChangeSnapshot[] valueBuffer, int valueCount)
        {
            if (ValueBlendUtils.ShouldLog)
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
            NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks,
            out GONetSyncableValue blendedValue, out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            // Track object ID for this buffer
            int objectId;
            if (!bufferToObjectId.TryGetValue(valueBuffer, out objectId))
            {
                objectId = ++objectIdCounter;
                bufferToObjectId[valueBuffer] = objectId;
            }

            // Variables for analysis
            Vector3 rawBlendedValue = Vector3.zero;
            Vector3 preSmoothedValue = Vector3.zero;
            bool didApplySmoothing = false;
            int valueCountUsable = valueCount;
            string smoothingReason = "none";
            float smoothingStrength = 0f;

            if (ValueBlendUtils.ShouldLog)
            {
                GONetLog.Debug($"VECTOR3 buffer value count: {valueCount}, atElapsedTicks (as ms): {TimeSpan.FromTicks(atElapsedTicks).TotalMilliseconds}, game time elapsed seconds: {GONetMain.Time.ElapsedSeconds}, game time elapsed seconds (client sim): {GONetMain.Time.ElapsedSeconds_ClientSimulation}");
                LogValueBufferEntries(valueBuffer, valueCount);
            }

            if (valueCount > 0)
            {
                int newestBufferIndex = 0;
                NumericValueChangeSnapshot newest = valueBuffer[newestBufferIndex];

                if (float.IsNaN(newest.numericValue.UnityEngine_Vector3.x) ||
                    float.IsNaN(newest.numericValue.UnityEngine_Vector3.y) ||
                    float.IsNaN(newest.numericValue.UnityEngine_Vector3.z))
                {
                    if (ValueBlendUtils.ShouldLog) GONetLog.Warning("Input data contains NaN value(s) for newest value in buffer.");
                    return false;
                }

                int oldestBufferIndex = valueCount - 1;
                NumericValueChangeSnapshot oldest = valueBuffer[oldestBufferIndex];

                bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest.elapsedTicksAtChange) <
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;

                if (isNewestRecentEnoughToProcess)
                {
                    Vector3 newestValue = newest.numericValue.UnityEngine_Vector3;
                    bool shouldAttemptInterExtraPolation = true;

                    if (shouldAttemptInterExtraPolation)
                    {
                        if (ValueBlendUtils.IS_FORCING_ALWAYS_EXTRAP_TO_AVOID_THE_UGLY_DATA_SWITCH || atElapsedTicks >= newest.elapsedTicksAtChange)
                        {
                            valueCountUsable = ValueBlendUtils.DetermineUsableValueCountForForcedExtrapolation(
                                valueBuffer, valueCount, atElapsedTicks, newestBufferIndex, oldestBufferIndex, out int iBase);

                            NumericValueChangeSnapshot baseSnap = valueBuffer[iBase];
                            Vector3 baseValue = baseSnap.numericValue.UnityEngine_Vector3;

                            if (ValueBlendUtils.ShouldLog) GONetLog.Debug("if EXTRAPO, value count usable: " + valueCountUsable);

                            bool isEnoughInfoToExtrapolate = valueCountUsable >= ValueBlendUtils.VALUE_COUNT_NEEDED_TO_EXTRAPOLATE;

                            if (isEnoughInfoToExtrapolate)
                            {
                                // Store raw value before any processing
                                rawBlendedValue = baseValue;

                                if (valueCountUsable > 3)
                                {
                                    Vector3 averageAcceleration;
                                    int countToUse = valueCountUsable < 4 ? valueCountUsable : 4;
                                    if (ValueBlendUtils.TryDetermineAverageAccelerationPerSecond(valueBuffer, countToUse, out averageAcceleration, iBase))
                                    {
                                        blendedValue = baseSnap.numericValue.UnityEngine_Vector3 +
                                            averageAcceleration * (float)TimeSpan.FromTicks(atElapsedTicks - baseSnap.elapsedTicksAtChange).TotalSeconds;
                                        rawBlendedValue = blendedValue.UnityEngine_Vector3;
                                    }
                                }
                                else if (valueCountUsable > 2)
                                {
                                    Vector3 acceleration;
                                    blendedValue = ValueBlendUtils.GetVector3AccelerationBasedExtrapolation(
                                        valueBuffer, atElapsedTicks, iBase, baseSnap, out acceleration);
                                    rawBlendedValue = blendedValue.UnityEngine_Vector3;
                                }
                                else
                                {
                                    // Linear extrapolation with 2 values
                                    NumericValueChangeSnapshot justBeforeBase = valueBuffer[iBase + 1];
                                    Vector3 justBeforeBaseValue = justBeforeBase.numericValue.UnityEngine_Vector3;

                                    float elapsedTimeSinceBase = (float)TimeSpan.FromTicks(atElapsedTicks - baseSnap.elapsedTicksAtChange).TotalSeconds;
                                    float timeBetweenSnapshots = (float)TimeSpan.FromTicks(baseSnap.elapsedTicksAtChange - justBeforeBase.elapsedTicksAtChange).TotalSeconds;

                                    if (timeBetweenSnapshots > 0)
                                    {
                                        Vector3 velocity = (baseValue - justBeforeBaseValue) / timeBetweenSnapshots;
                                        blendedValue = baseValue + velocity * elapsedTimeSinceBase;
                                        rawBlendedValue = blendedValue.UnityEngine_Vector3;
                                    }
                                    else
                                    {
                                        // No time between snapshots - use base value
                                        blendedValue = baseValue;
                                        rawBlendedValue = baseValue;
                                    }
                                }

                                didExtrapolatePastMostRecentChanges = valueCountUsable == valueCount;

                                // Store pre-smoothed value
                                preSmoothedValue = blendedValue.UnityEngine_Vector3;

                                // Apply smoothing and track if it was applied
                                GONetSyncableValue originalValue = blendedValue;
                                didApplySmoothing = ValueBlendUtils.Vector3_ApplySmoothing_IfAppropriate(
                                    ref valueBuffer, valueCount, ref blendedValue);

                                // Analyze why smoothing was applied
                                if (didApplySmoothing)
                                {
                                    AnalyzeSmoothingReason(
                                        valueBuffer, valueCount, originalValue.UnityEngine_Vector3,
                                        blendedValue.UnityEngine_Vector3, out smoothingReason, out smoothingStrength);
                                }
                            }
                            else
                            {
                                blendedValue = baseValue;
                                rawBlendedValue = baseValue;
                                if (ValueBlendUtils.ShouldLog) GONetLog.Debug("VECTOR3 new new beast");
                            }
                        }
                        else if (atElapsedTicks <= oldest.elapsedTicksAtChange)
                        {
                            if (ValueBlendUtils.ShouldLog) GONetLog.Debug("\telse if YOU OLD DEVIL YOU");
                            blendedValue = oldest.numericValue.UnityEngine_Vector3;
                            rawBlendedValue = oldest.numericValue.UnityEngine_Vector3;
                        }
                        else
                        {
                            // Interpolation case
                            if (ValueBlendUtils.ShouldLog) GONetLog.Debug("\telse LOIP!");
                            bool didWeLoip = false;

                            for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                            {
                                NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                if (atElapsedTicks <= newer.elapsedTicksAtChange)
                                {
                                    NumericValueChangeSnapshot older = valueBuffer[i];

                                    float interpolationTime = (atElapsedTicks - older.elapsedTicksAtChange) /
                                        (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);

                                    blendedValue = Vector3.Lerp(
                                        older.numericValue.UnityEngine_Vector3,
                                        newer.numericValue.UnityEngine_Vector3,
                                        interpolationTime);

                                    rawBlendedValue = blendedValue.UnityEngine_Vector3;
                                    didWeLoip = true;
                                    break;
                                }
                            }

                            if (!didWeLoip)
                            {
                                if (ValueBlendUtils.ShouldLog) GONetLog.Debug("NEVER NEVER in life did we loip 'eem");
                            }
                            else
                            {
                                // Apply smoothing for interpolated values too
                                preSmoothedValue = blendedValue.UnityEngine_Vector3;
                                GONetSyncableValue originalValue = blendedValue;
                                didApplySmoothing = ValueBlendUtils.Vector3_ApplySmoothing_IfAppropriate(
                                    ref valueBuffer, valueCount, ref blendedValue);

                                if (didApplySmoothing)
                                {
                                    AnalyzeSmoothingReason(
                                        valueBuffer, valueCount, originalValue.UnityEngine_Vector3,
                                        blendedValue.UnityEngine_Vector3, out smoothingReason, out smoothingStrength);
                                }
                            }
                        }
                    }
                    else
                    {
                        blendedValue = newestValue;
                        rawBlendedValue = newestValue;
                        if (ValueBlendUtils.ShouldLog) GONetLog.Debug("not a vector3?");
                    }

                    // Log the complete blending analysis
                    Vector3BlendingQualityAnalyzer.LogBlendingResult(
                        objectId,
                        valueBuffer,
                        valueCount,
                        atElapsedTicks,
                        rawBlendedValue,
                        blendedValue.UnityEngine_Vector3,
                        didApplySmoothing,
                        didExtrapolatePastMostRecentChanges,
                        valueCountUsable,
                        smoothingReason,
                        smoothingStrength
                    );
                }
                else
                {
                    if (ValueBlendUtils.ShouldLog)
                        GONetLog.Debug("data is too old.... now - newest (ms): " +
                            TimeSpan.FromTicks(GONetMain.Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                    return false;
                }

                return true;
            }

            if (ValueBlendUtils.ShouldLog) GONetLog.Debug($"NO VALUES!!! hash: {valueBuffer.GetHashCode()}");
            return false;
        }

        private void AnalyzeSmoothingReason(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            Vector3 originalValue,
            Vector3 smoothedValue,
            out string reason,
            out float strength)
        {
            reason = "unknown";
            strength = 1.0f;

            // Calculate the smoothing delta
            float delta = (smoothedValue - originalValue).magnitude;

            // Low value count
            if (valueCount <= 3)
            {
                reason = "low-value-count";
                strength = 1.0f;
                return;
            }

            // Check for at-rest
            if (valueCount >= 2)
            {
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
                            float deltaTime = (float)(timeDelta * 1e-7);
                            float velocitySqr = distSqr / (deltaTime * deltaTime);

                            if (velocitySqr < 0.01f) // 0.1 units/second squared
                            {
                                reason = "at-rest";
                                strength = 0.8f;
                                return;
                            }
                        }
                    }
                }
            }

            // Check for direction change
            if (valueCount >= 3)
            {
                float directionChange = CalculateDirectionChange(valueBuffer);
                if (directionChange > 90f)
                {
                    reason = "direction-change";
                    strength = directionChange / 180f;
                    return;
                }
            }

            // Default to jitter
            reason = "jitter";
            strength = Mathf.Clamp01(delta / 0.2f);
        }

        private float CalculateDirectionChange(NumericValueChangeSnapshot[] valueBuffer)
        {
            if (valueBuffer.Length < 3) return 0f;

            Vector3 v1 = valueBuffer[0].numericValue.UnityEngine_Vector3 - valueBuffer[1].numericValue.UnityEngine_Vector3;
            Vector3 v2 = valueBuffer[1].numericValue.UnityEngine_Vector3 - valueBuffer[2].numericValue.UnityEngine_Vector3;

            if (v1.magnitude < 0.001f || v2.magnitude < 0.001f) return 0f;

            float dot = Vector3.Dot(v1.normalized, v2.normalized);
            return Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
        }
    }

    public sealed class GONetValueBlending_Vector3_HermiteSpline : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "High-quality Hermite spline interpolation with velocity matching. Provides the smoothest movement by considering both position and velocity at sample points. Best for scenarios requiring maximum visual quality.";

        // Constants for performance
        private const float TICKS_TO_SECONDS = 1e-7f;
        private const float MAX_ACCELERATION = 100f;
        private const float VELOCITY_SMOOTHING = 0.3f;
        private const int MIN_VALUES_FOR_VELOCITY = 2;
        private const int MIN_VALUES_FOR_ACCELERATION = 3;

        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (valueCount == 0) return false;

            // With forced extrapolation, we need to find the right "base" point
            // This is the point just BEFORE atElapsedTicks that we'll extrapolate from

            int baseIndex = 0;
            int valueCountUsable = valueCount;

            // Find where atElapsedTicks falls in our buffer
            for (int i = 0; i < valueCount - 1; i++)
            {
                if (atElapsedTicks >= valueBuffer[i].elapsedTicksAtChange)
                {
                    // We're at or past the newest value - true extrapolation
                    baseIndex = 0;
                    valueCountUsable = valueCount;
                    didExtrapolatePastMostRecentChanges = true;
                    break;
                }
                else if (atElapsedTicks >= valueBuffer[i + 1].elapsedTicksAtChange)
                {
                    // We're between two values
                    // WITH FORCED EXTRAP: Use i+1 as base and extrapolate forward
                    // This ignores value at index i (which is in the future)
                    baseIndex = i + 1;
                    valueCountUsable = valueCount - baseIndex;
                    didExtrapolatePastMostRecentChanges = false; // Not past ALL known values
                    break;
                }
            }

            // Need at least 2 points from base to extrapolate
            if (valueCountUsable < 2)
            {
                blendedValue = valueBuffer[baseIndex].numericValue.UnityEngine_Vector3;
                return true;
            }

            // Get our extrapolation points (going backwards from base)
            Vector3 p0 = valueBuffer[baseIndex].numericValue.UnityEngine_Vector3;
            Vector3 p1 = valueBuffer[baseIndex + 1].numericValue.UnityEngine_Vector3;
            long t0 = valueBuffer[baseIndex].elapsedTicksAtChange;
            long t1 = valueBuffer[baseIndex + 1].elapsedTicksAtChange;

            // Calculate velocity
            float dt = (t0 - t1) * TICKS_TO_SECONDS;
            if (dt <= 0) return false;

            Vector3 velocity = (p0 - p1) / dt;

            // Calculate how far to extrapolate
            float extrapolateTime = (atElapsedTicks - t0) * TICKS_TO_SECONDS;

            // SMOOTHING TRICK: If we're ignoring future data, blend slightly toward it
            // This reduces the error while maintaining consistency
            if (baseIndex > 0 && !didExtrapolatePastMostRecentChanges)
            {
                // We have a future value we're ignoring
                Vector3 futurePos = valueBuffer[baseIndex - 1].numericValue.UnityEngine_Vector3;

                // Calculate what pure extrapolation would give
                Vector3 extrapolatedPos = p0 + velocity * extrapolateTime;

                // Blend slightly toward the known future (10-20% influence)
                // This reduces error without causing visible pops
                float blendFactor = 0.15f;
                blendedValue = Vector3.Lerp(extrapolatedPos, futurePos, blendFactor);
            }
            else
            {
                // True extrapolation - no future data to blend with
                if (valueCountUsable >= 3)
                {
                    // Use acceleration if available
                    Vector3 p2 = valueBuffer[baseIndex + 2].numericValue.UnityEngine_Vector3;
                    long t2 = valueBuffer[baseIndex + 2].elapsedTicksAtChange;

                    Vector3 v1 = (p1 - p2) / ((t1 - t2) * TICKS_TO_SECONDS);
                    Vector3 accel = (velocity - v1) / dt;

                    // Cap acceleration
                    if (accel.magnitude > MAX_ACCELERATION)
                        accel = accel.normalized * MAX_ACCELERATION;

                    blendedValue = p0 + velocity * extrapolateTime +
                                  0.5f * accel * extrapolateTime * extrapolateTime;
                }
                else
                {
                    // Linear extrapolation
                    blendedValue = p0 + velocity * extrapolateTime;
                }
            }

            /*
            // Log the complete blending analysis
            Vector3BlendingQualityAnalyzer.LogBlendingResult(
                valueBuffer.GetHashCode(),
                valueBuffer,
                valueCount,
                atElapsedTicks,
                rawBlendedValue,
                blendedValue.UnityEngine_Vector3,
                didApplySmoothing,
                didExtrapolatePastMostRecentChanges,
                valueCountUsable,
                smoothingReason,
                smoothingStrength
            );
            */

            return true;
        }
    }

    public sealed class GONetValueBlending_Vector3_CatmullRom : IGONetAutoMagicalSync_CustomValueBlending
    {
        public GONetSyncableValueTypes AppliesOnlyToGONetType => GONetSyncableValueTypes.UnityEngine_Vector3;

        public string Description => "Catmull-Rom spline interpolation using only position data. Faster than Hermite but may overshoot on sharp turns. Best for high object counts or smooth trajectories.";

        private const float TICKS_TO_SECONDS = 1e-7f;
        private const float EXTRAPOLATION_DAMPING = 0.95f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBlendedValue(
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolatePastMostRecentChanges)
        {
            blendedValue = default;
            didExtrapolatePastMostRecentChanges = false;

            if (valueCount == 0) return false;

            Vector3 p1 = valueBuffer[0].numericValue.UnityEngine_Vector3;

            // Quick NaN check
            if (float.IsNaN(p1.x) || float.IsNaN(p1.y) || float.IsNaN(p1.z)) return false;

            // Need at least 2 points for any interpolation
            if (valueCount < 2)
            {
                blendedValue = p1;
                return true;
            }

            long t1 = valueBuffer[0].elapsedTicksAtChange;
            Vector3 p2 = valueBuffer[1].numericValue.UnityEngine_Vector3;
            long t2 = valueBuffer[1].elapsedTicksAtChange;

            // Check if extrapolating
            if (atElapsedTicks >= t1)
            {
                didExtrapolatePastMostRecentChanges = true;

                // For extrapolation, use velocity from recent points
                float dt = (t1 - t2) * TICKS_TO_SECONDS;
                if (dt <= 0)
                {
                    blendedValue = p1;
                    return true;
                }

                Vector3 velocity = (p1 - p2) / dt;
                float extrapolateTime = (atElapsedTicks - t1) * TICKS_TO_SECONDS;

                // Apply damping to extrapolation to prevent runaway
                float damping = (float)Math.Pow(EXTRAPOLATION_DAMPING, extrapolateTime);
                blendedValue = p1 + velocity * extrapolateTime * damping;
                return true;
            }

            // For Catmull-Rom, we need 4 points ideally
            Vector3 p0, p3;

            if (valueCount >= 4)
            {
                // We have all 4 points
                p0 = valueBuffer[3].numericValue.UnityEngine_Vector3;
                p3 = valueBuffer[2].numericValue.UnityEngine_Vector3;
            }
            else if (valueCount == 3)
            {
                // Missing one endpoint
                p3 = valueBuffer[2].numericValue.UnityEngine_Vector3;
                // Extrapolate p0 by reflecting p2 through p1
                p0 = p1 + (p1 - p2);
            }
            else // valueCount == 2
            {
                // Only have 2 points, extrapolate both ends
                p0 = p1 + (p1 - p2);
                p3 = p2 + (p2 - p1);
            }

            // Find which segment we're in
            if (atElapsedTicks <= t2)
            {
                // Before oldest point
                blendedValue = p2;
                return true;
            }

            // Calculate t parameter for Catmull-Rom
            float segmentTime = (t1 - t2) * TICKS_TO_SECONDS;
            float currentTime = (atElapsedTicks - t2) * TICKS_TO_SECONDS;
            float t = currentTime / segmentTime;

            // Catmull-Rom spline calculation
            float t2_calc = t * t;
            float t3 = t2_calc * t;

            // Catmull-Rom basis
            blendedValue = 0.5f * (
                2f * p2 +
                (-p0 + p1) * t +
                (2f * p0 - 5f * p2 + 4f * p1 - p3) * t2_calc +
                (-p0 + 3f * p2 - 3f * p1 + p3) * t3
            );

            return true;
        }
    }

    public class GONetValueBlending_Vector3_HighPerformance : IGONetAutoMagicalSync_CustomValueBlending
    {
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
