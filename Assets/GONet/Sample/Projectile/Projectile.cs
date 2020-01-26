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
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GONet.Sample
{
    [RequireComponent(typeof(GONetParticipant))]
    public class Projectile : GONetParticipantCompanionBehaviour
    {
        public float speed = 2;

        TextMeshProUGUI text;

        protected override void Awake()
        {
            base.Awake();

            text = GetComponentInChildren<TextMeshProUGUI>();

            InitSutffForSupportingHoveringDuplicate();
        }

        private void Update()
        {
            if (gonetParticipant.IsMine)
            {
                const string MINE = "MINE";
                text.text = MINE;
                text.color = Color.green;

                hoveringDuplicateChild.gameObject.GetComponent<Renderer>().enabled = true;
                SimulateNonAuthorityInterpolation(hoveringDuplicateChild);
            }
            else
            {
                const string WHO = "?";
                text.text = WHO;
                text.color = Color.red;

                hoveringDuplicateChild.gameObject.GetComponent<Renderer>().enabled = false;
            }
        }

        #region stuff for supporting hovering duplicate (for GONet internal reasons):

        Transform hoveringDuplicateChild;

        GONetMain.SecretaryOfTemporalAffairs simulatedNonAuthorityTime;
        readonly Queue<Vector3> sentPositionBuffer = new Queue<Vector3>();
        readonly Queue<long> sentTimeInTicksBuffer = new Queue<long>();
        private static readonly int VALUE_COUNT_NEEDED_TO_EXTRAPOLATE = 2;
        int buffer_capacitySize;

        private void InitSutffForSupportingHoveringDuplicate()
        {
            hoveringDuplicateChild = transform.Find("HoveringDuplicate");
            simulatedNonAuthorityTime = new GONetMain.SecretaryOfTemporalAffairs(GONetMain.Time);
            GONetMain.Time.TimeSetFromAuthority += Time_TimeSetFromAuthority;
            float syncPositionEverySeconds = 1 / 30f;
            int buffer_calcdSize = syncPositionEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / syncPositionEverySeconds) * 2.5f) : 0;
            buffer_capacitySize = Math.Max(buffer_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
            GONetLog.Debug("buffer capacity: " + buffer_capacitySize);
        }

        protected override void Start()
        {
            base.Start();

            GONetMain.EventBus.Subscribe<SyncEvent_Transform_position>(OnSendingMyTransform, e => !e.IsSourceRemote && e.GONetParticipant == gonetParticipant);
        }

        private void Time_TimeSetFromAuthority(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks)
        {
            simulatedNonAuthorityTime.SetFromAuthority(toElapsedTicks);
        }

        private void OnSendingMyTransform(GONetEventEnvelope<SyncEvent_Transform_position> eventEnvelope)
        {
            //GONetLog.Debug("sending my transform: " + eventEnvelope.Event.valueNew + " time: " + eventEnvelope.Event.OccurredAtElapsedSeconds);

            sentPositionBuffer.Enqueue(eventEnvelope.Event.valueNew);
            sentTimeInTicksBuffer.Enqueue(eventEnvelope.Event.OccurredAtElapsedTicks);

            if (sentPositionBuffer.Count > buffer_capacitySize)
            {
                sentPositionBuffer.Dequeue();
                sentTimeInTicksBuffer.Dequeue();
            }
        }

        private void SimulateNonAuthorityInterpolation(Transform applySimulationToTransform)
        {
            simulatedNonAuthorityTime.Update();

            Vector3 interpolatedPosition;
            if (GetBlendedValue_Vector3(
                    sentPositionBuffer.Reverse().ToArray(), 
                    sentTimeInTicksBuffer.Reverse().ToArray(), 
                    sentPositionBuffer.Count,
                    simulatedNonAuthorityTime.ElapsedTicks - GONetMain.valueBlendingBufferLeadTicks, 
                    out interpolatedPosition))
            {
                applySimulationToTransform.position = new Vector3(
                    interpolatedPosition.x, 
                    applySimulationToTransform.position.y, // keep old y to maintain the "hovering" word to be accurate and keep it out of the way to be able to see both
                    interpolatedPosition.z);
            }
            else
            {
                //const string NO = "no interpolation!!!";
                //GONetLog.Debug(NO);
            }
        }

        bool GetBlendedValue_Vector3(Vector3[] sentPositionBuffer, long[] sentTimeInTicksBuffer, int valueCount, long atElapsedTicks, out Vector3 blendedValue)
        {
            blendedValue = default;

            if (valueCount > 0)
            {
                { // use buffer to determine the actual value that we think is most appropriate for this moment in time
                    int newestBufferIndex = 0;
                    Vector3 newest_numericValue = sentPositionBuffer[newestBufferIndex];
                    long newest_elapsedTicksAtChange = sentTimeInTicksBuffer[newestBufferIndex];
                    int oldestBufferIndex = valueCount - 1;
                    Vector3 oldest_numericValue = sentPositionBuffer[oldestBufferIndex];
                    long oldest_elapsedTicksAtChange = sentTimeInTicksBuffer[oldestBufferIndex];
                    bool isNewestRecentEnoughToProcess = (atElapsedTicks - newest_elapsedTicksAtChange) < GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS;
                    if (isNewestRecentEnoughToProcess)
                    {
                        Vector3 newestValue = newest_numericValue;
                        bool shouldAttemptInterExtraPolation = true; // default to true since only float is supported and we can definitely interpolate/extrapolate floats!
                        if (shouldAttemptInterExtraPolation)
                        {
                            if (atElapsedTicks >= newest_elapsedTicksAtChange) // if the adjustedTime is newer than our newest time in buffer, just set the transform to what we have as newest
                            {
                                bool isEnoughInfoToExtrapolate = valueCount >= VALUE_COUNT_NEEDED_TO_EXTRAPOLATE; // this is the fastest way to check if newest is different than oldest....in which case we do have two distinct snapshots...from which to derive last velocity
                                if (isEnoughInfoToExtrapolate)
                                {
                                    Vector3 justBeforenewest_numericValue = sentPositionBuffer[newestBufferIndex + 1];
                                    long justBeforenewest_elapsedTicksAtChange = sentTimeInTicksBuffer[newestBufferIndex + 1];
                                    Vector3 valueDiffBetweenLastTwo = newestValue - justBeforenewest_numericValue;
                                    long ticksBetweenLastTwo = newest_elapsedTicksAtChange - justBeforenewest_elapsedTicksAtChange;

                                    long extrapolated_TicksAtSend = newest_elapsedTicksAtChange + ticksBetweenLastTwo;
                                    Vector3 extrapolated_ValueNew = newestValue + valueDiffBetweenLastTwo;
                                    float interpolationTime = (atElapsedTicks - newest_elapsedTicksAtChange) / (float)(extrapolated_TicksAtSend - newest_elapsedTicksAtChange);
                                    blendedValue = Vector3.Lerp(newestValue, extrapolated_ValueNew, interpolationTime);
                                    //GONetLog.Debug("extroip'd....newest: " + TimeSpan.FromTicks(newest_elapsedTicksAtChange).TotalSeconds + " extrap'd: " + TimeSpan.FromTicks(extrapolated_TicksAtSend).TotalSeconds + " interpolationPERCENTAGE: " + interpolationTime);
                                }
                                else
                                {
                                    blendedValue = newestValue;
                                    //GONetLog.Debug("VECTOR3 new new beast");
                                }
                            }
                            else if (atElapsedTicks <= oldest_elapsedTicksAtChange) // if the adjustedTime is older than our oldest time in buffer, just set the transform to what we have as oldest
                            {
                                blendedValue = oldest_numericValue;
                                //GONetLog.Debug("VECTOR3 went old school on 'eem..... blendedValue: " + blendedValue + " valueCount: " + valueCount + " oldest.seconds: " + TimeSpan.FromTicks(oldest_elapsedTicksAtChange).TotalSeconds);
                            }
                            else // this is the normal case where we can apply interpolation if the settings call for it!
                            {
                                bool didWeLoip = false;
                                for (int i = oldestBufferIndex; i > newestBufferIndex; --i)
                                {
                                    Vector3 newer_numericValue = sentPositionBuffer[i - 1];
                                    long newer_elapsedTicksAtChange = sentTimeInTicksBuffer[i - 1];

                                    if (atElapsedTicks <= newer_elapsedTicksAtChange)
                                    {
                                        Vector3 older_numericValue = sentPositionBuffer[i];
                                        long older_elapsedTicksAtChange = sentTimeInTicksBuffer[i];

                                        float interpolationTime = (atElapsedTicks - older_elapsedTicksAtChange) / (float)(newer_elapsedTicksAtChange - older_elapsedTicksAtChange);
                                        blendedValue = Vector3.Lerp(
                                            older_numericValue,
                                            newer_numericValue,
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
                        //GONetLog.Debug("data is too old....  now - newest (ms): " + TimeSpan.FromTicks(simulatedNonAuthorityTime.ElapsedTicks - newest_elapsedTicksAtChange).TotalMilliseconds);
                        return false; // data is too old...stop processing for now....we do this as we believe we have already processed the latest data and further processing is unneccesary additional resource usage
                    }
                }

                return true;
            }

            return false;
        }

        #endregion

    }
}
