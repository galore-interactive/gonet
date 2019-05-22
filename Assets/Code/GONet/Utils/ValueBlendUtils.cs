using System.Collections.Generic;
using UnityEngine;

namespace GONet.Utils
{
    internal static class ValueBlendUtils
    {
        internal static bool TryGetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueMonitoringSupport, long atElapsedTicks, out object blendedValue)
        {
            if (valueMonitoringSupport.mostRecentChanges_usedSize > 0)
            {
                GetBlendedValue getBlendedValue;
                if (getBlendedValues_byValueType.TryGetValue(valueMonitoringSupport.mostRecentChanges[0].valueType, out getBlendedValue))
                {
                    return getBlendedValue(valueMonitoringSupport.mostRecentChanges, valueMonitoringSupport.mostRecentChanges_usedSize, atElapsedTicks, out blendedValue);
                }
            }

            blendedValue = null;
            return false;
        }

        static readonly Dictionary<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.GONetBlendyValueType, GetBlendedValue> getBlendedValues_byValueType = 
            new Dictionary<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.GONetBlendyValueType, GetBlendedValue>(8)
        {
            { GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.GONetBlendyValueType.Float, GetBlendedValue_Float },
            { GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.GONetBlendyValueType.Quaternion, GetBlendedValue_Quaternion }
        };

        delegate bool GetBlendedValue(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out object blendedValue);

        static bool GetBlendedValue_Float(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out object blendedValue)
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
                        float newestValue = newest.value_float;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            long adjustedTicks = atElapsedTicks - GONetMain.BLENDING_BUFFER_LEAD_TICKS;
                            if (adjustedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount > 1; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    float valueDiffBetweenLastTwo = newestValue - justBeforeNewest.value_float;
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
                                blendedValue = oldest.value_float;
                                //GONetLog.Debug("went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " mostRecentChanges_capacitySize: " + mostRecentChanges_capacitySize);
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
                                            older.value_float,
                                            newer.value_float,
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

        static bool GetBlendedValue_Quaternion(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot[] valueBuffer, int valueCount, long atElapsedTicks, out object blendedValue)
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
                        Quaternion newestValue = newest.value_Quaternion;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            long adjustedTicks = atElapsedTicks - GONetMain.BLENDING_BUFFER_LEAD_TICKS;
                            if (adjustedTicks >= newest.elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount > 1; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.NumericValueChangeSnapshot justBeforeNewest = valueBuffer[newestBufferIndex + 1];
                                    Quaternion valueDiffBetweenLastTwo = justBeforeNewest.value_Quaternion * Quaternion.Inverse(newestValue);
                                    long ticksBetweenLastTwo = newest.elapsedTicksAtChange - justBeforeNewest.elapsedTicksAtChange;

                                    long extrapolated_TicksAtSend = newest.elapsedTicksAtChange + ticksBetweenLastTwo;
                                    Quaternion extrapolated_ValueNew = newestValue * valueDiffBetweenLastTwo;
                                    float interpolationTime = (adjustedTicks - newest.elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest.elapsedTicksAtChange);
                                    blendedValue = Quaternion.Slerp(newestValue, extrapolated_ValueNew, interpolationTime);
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
                                blendedValue = oldest.value_Quaternion;
                                //GONetLog.Debug("went old school on 'eem..... adjusted seconds: " + TimeSpan.FromTicks(adjustedTicks).TotalSeconds + " blendedValue: " + blendedValue + " mostRecentChanges_capacitySize: " + mostRecentChanges_capacitySize);
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
                                            older.value_Quaternion,
                                            newer.value_Quaternion,
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
    }
}
