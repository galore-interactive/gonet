/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.Utils
{
    internal static class ValueBlendUtils
    {
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
                            long adjustedTicks = atElapsedTicks - GONetMain.valueBlendingBufferLeadTicks;
                            if (adjustedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount > 1; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    float valueDiffBetweenLastTwo = newestValue - justBeforeNewest.numericValue.System_Single;
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                    long extrapolated_TicksAtSend = newest.elapsedTicksAtChange + ticksBetweenLastTwo;
                                    float extrapolated_ValueNew = newestValue + valueDiffBetweenLastTwo;
                                    float interpolationTime = (adjustedTicks - newest.elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest.elapsedTicksAtChange);
                                    blendedValue = Mathf.Lerp(newestValue, extrapolated_ValueNew, interpolationTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("new new beast");
                                }
                            }
                            else if (adjustedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
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

                                    if (adjustedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (adjustedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
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
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(Time.ElapsedTicks - newest.elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                return true;
            }

            return false;
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
                            long adjustedTicks = atElapsedTicks - GONetMain.valueBlendingBufferLeadTicks;
                            if (adjustedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount > 1; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;
                                    long extrapolated_TicksAtSend = newest.elapsedTicksAtChange + ticksBetweenLastTwo;


                                    /* option 1:
                                    Quaternion valueDiffBetweenLastTwo = justBeforeNewest.numericValue.UnityEngine_Quaternion * Quaternion.Inverse(newestValue);
                                    Quaternion extrapolated_ValueNew = newestValue * valueDiffBetweenLastTwo;
                                    float interpolationTime = (adjustedTicks - newest.elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest.elapsedTicksAtChange);
                                    blendedValue = Quaternion.Slerp(newestValue, extrapolated_ValueNew, interpolationTime);
                                    */


                                    //* option 2:
                                    var rot = newestValue * Quaternion.Inverse(justBeforeNewest.numericValue.UnityEngine_Quaternion); // rot is the rotation from t1 to t2
                                    var dt = (extrapolated_TicksAtSend - justBeforeNewest.elapsedTicksAtChange) / (float)(newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange); // dt = extrapolation factor
                                    float ang;
                                    Vector3 axis;
                                    rot.ToAngleAxis(out ang, out axis); // find axis-angle representation
                                    if (ang > 180) ang -= 360;  // assume the shortest path
                                    ang = ang * dt % 360; // multiply angle by the factor
                                    blendedValue = Quaternion.AngleAxis(ang, axis) * justBeforeNewest.numericValue.UnityEngine_Quaternion; // combine with first rotation
                                    //*/


                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("QUAT new new beast");
                                }
                            }
                            else if (adjustedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest.numericValue.UnityEngine_Quaternion;
                                //GONetLog.Debug("QUAT went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest.elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                bool didWeLoip = false;
                                for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot newer = valueBuffer[i - 1];

                                    if (adjustedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (adjustedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
                                        blendedValue = Quaternion.Slerp(
                                            older.numericValue.UnityEngine_Quaternion,
                                            newer.numericValue.UnityEngine_Quaternion,
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
                            long adjustedTicks = atElapsedTicks - GONetMain.valueBlendingBufferLeadTicks;
                            if (adjustedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount > 1; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    Vector3 valueDiffBetweenLastTwo = newestValue - justBeforeNewest.numericValue.UnityEngine_Vector3;
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                    long extrapolated_TicksAtSend = newest.elapsedTicksAtChange + ticksBetweenLastTwo;
                                    Vector3 extrapolated_ValueNew = newestValue + valueDiffBetweenLastTwo;
                                    float interpolationTime = (adjustedTicks - newest.elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest.elapsedTicksAtChange);
                                    blendedValue = Vector3.Lerp(newestValue, extrapolated_ValueNew, interpolationTime);
                                    //GONetLog.Debug("extroip'd....newest: " + newestValue + " extrap'd: " + extrapolated_ValueNew);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR3 new new beast");
                                }
                            }
                            else if (adjustedTicks <= oldest.elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
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

                                    if (adjustedTicks <= newer.elapsedTicksAtChange)
                                    {
                                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot older = valueBuffer[i];

                                        float interpolationTime = (adjustedTicks - older.elapsedTicksAtChange) / (float)(newer.elapsedTicksAtChange - older.elapsedTicksAtChange);
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
}
