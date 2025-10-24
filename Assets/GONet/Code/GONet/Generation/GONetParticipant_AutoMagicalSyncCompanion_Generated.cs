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

#undef GONET_VELOCITY_SYNC_DEBUG // Disable velocity sync debug logging (massive spam in logs)

using GONet.PluginAPI;
using GONet.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GONet.Generation
{
    /// <summary>
    /// TODO: make the main dll internals visible to editor dll so this can be made internal again
    /// </summary>
    public abstract class GONetParticipant_AutoMagicalSyncCompanion_Generated : IDisposable
    {
        internal GONetParticipant gonetParticipant;

        const int EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN = 5;
        const int EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX = 30;

        protected static readonly ArrayPool<bool> lastKnownValuesChangedArrayPool =
            new ArrayPool<bool>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        protected static readonly ArrayPool<byte> lastKnownValueAtRestBitsArrayPool =
            new ArrayPool<byte>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        protected static readonly ArrayPool<long> lastKnownValueChangedAtElapsedTicksArrayPool =
            new ArrayPool<long>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        protected static readonly ArrayPool<bool> doesBaselineValueNeedAdjustingArrayPool =
            new ArrayPool<bool>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        /// <summary>
        /// Look for true values inside <see cref="lastKnownValueChangesSinceLastCheck"/> to know which indexes/instances herein represent real changes since last check.
        /// </summary>
        internal static readonly ArrayPool<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> valuesChangesSupportArrayPool = 
            new ArrayPool<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        internal static readonly ObjectPool<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> valueChangeSupportArrayPool =
            new ObjectPool<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(10000, 100);

        /// <summary>
        /// NOTE: Set inside generated constructor
        /// </summary>
        internal byte valuesCount;

        protected bool[] lastKnownValueChangesSinceLastCheck;
        protected long[] lastKnownValueChangedAtElapsedTicks;
        protected byte[] lastKnownValueAtRestBits;
        protected bool[] doesBaselineValueNeedAdjusting;

        protected const byte LAST_KNOWN_VALUE_NOT_AT_REST = 0;
        protected const byte LAST_KNOWN_VALUE_IS_AT_REST_NEEDS_TO_BROADCAST = 1;
        protected const byte LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED = byte.MaxValue;

        internal GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue[] valuesChangesSupport;

        protected static readonly ArrayPool<IGONetAutoMagicalSync_CustomSerializer> cachedCustomSerializersArrayPool =
            new ArrayPool<IGONetAutoMagicalSync_CustomSerializer>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);
        /// <summary>
        /// DEPRECATED: Use cachedValueSerializers instead. This field exists for backward compatibility only.
        /// Will be removed when all generated code is migrated to two-serializer architecture.
        /// </summary>
        protected IGONetAutoMagicalSync_CustomSerializer[] cachedCustomSerializers;

        /// <summary>
        /// Velocity-augmented sync: Serializers initialized with VALUE quantization settings.
        /// Used when sending/receiving VALUE packets (actual position/rotation data).
        /// </summary>
        protected IGONetAutoMagicalSync_CustomSerializer[] cachedValueSerializers;

        /// <summary>
        /// Velocity-augmented sync: Serializers initialized with VELOCITY quantization settings.
        /// Used when sending/receiving VELOCITY packets (velocity/angular velocity data).
        /// </summary>
        protected IGONetAutoMagicalSync_CustomSerializer[] cachedVelocitySerializers;

        protected static readonly ArrayPool<IGONetAutoMagicalSync_CustomValueBlending> cachedCustomValueBlendingsArrayPool =
            new ArrayPool<IGONetAutoMagicalSync_CustomValueBlending>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);
        protected IGONetAutoMagicalSync_CustomValueBlending[] cachedCustomValueBlendings;

        protected static readonly ArrayPool<IGONetAutoMagicalSync_CustomVelocityBlending> cachedCustomVelocityBlendingsArrayPool =
            new ArrayPool<IGONetAutoMagicalSync_CustomVelocityBlending>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);
        protected IGONetAutoMagicalSync_CustomVelocityBlending[] cachedCustomVelocityBlendings;

        protected static readonly ConcurrentDictionary<Thread, byte[]> valueDeserializeByteArrayByThreadMap = new ConcurrentDictionary<Thread, byte[]>(5, 5);

        /// <summary>
        /// Velocity-augmented sync: Tracks sync count PER-VALUE for velocity frequency logic.
        /// Used with VelocityFrequency to determine when to send VELOCITY vs VALUE packets.
        /// Example: syncCounter[i] % VelocityFrequency == 0 means send VELOCITY this tick.
        /// Array indexed by singleIndex (same as valuesChangesSupport indices).
        /// </summary>
        protected int[] syncCounter;

        protected static readonly ArrayPool<int> syncCounterArrayPool =
            new ArrayPool<int>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);

        internal abstract byte CodeGenerationId { get; }

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;
        }

        public void Dispose()
        {
            lastKnownValueAtRestBitsArrayPool.Return(lastKnownValueAtRestBits);
            lastKnownValuesChangedArrayPool.Return(lastKnownValueChangesSinceLastCheck);
            lastKnownValueChangedAtElapsedTicksArrayPool.Return(lastKnownValueChangedAtElapsedTicks);

            doesBaselineValueNeedAdjustingArrayPool.Return(doesBaselineValueNeedAdjusting);
            cachedCustomSerializersArrayPool.Return(cachedCustomSerializers);
            cachedCustomSerializersArrayPool.Return(cachedValueSerializers);
            cachedCustomSerializersArrayPool.Return(cachedVelocitySerializers);
            cachedCustomValueBlendingsArrayPool.Return(cachedCustomValueBlendings);
            cachedCustomVelocityBlendingsArrayPool.Return(cachedCustomVelocityBlendings);
            syncCounterArrayPool.Return(syncCounter);

            for (int i = 0; i < valuesCount; ++i)
            {
                GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                if (valueChangeSupport.mostRecentChanges != null)
                {
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Return(valueChangeSupport.mostRecentChanges);
                    valueChangeSupport.mostRecentChanges = null;
                    valueChangeSupport.mostRecentChanges_capacitySize = 0;
                    valueChangeSupport.mostRecentChanges_usedSize = 0;
                }
                valueChangeSupportArrayPool.Return(valueChangeSupport); // all of these calls should come prior to returning valuesChangesSupport inside which these object reside currently

            }
            valuesChangesSupportArrayPool.Return(valuesChangesSupport);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected byte[] GetMyValueDeserializeByteArray()
        {
            byte[] mine;
            if (!valueDeserializeByteArrayByThreadMap.TryGetValue(Thread.CurrentThread, out mine))
            {
                mine = new byte[8];
                valueDeserializeByteArrayByThreadMap[Thread.CurrentThread] = mine;
            }

            return mine;
        }

        public bool IsValueAtRest(byte index)
        {
            return lastKnownValueAtRestBits[index] != LAST_KNOWN_VALUE_NOT_AT_REST;
        }

        internal void IndicateAtRestBroadcasted(byte index)
        {
            lastKnownValueAtRestBits[index] = LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED;
        }

        /// <summary>
        /// <para>POST: <see cref="lastKnownValueChangesSinceLastCheck"/> updated with true of false to indicate which value indices inside <see cref="lastKnownValues"/> represent new/changed values.</para>
        /// <para>POST: <see cref="lastKnownValueAtRestBits"/> updated with one of the following: <see cref="LAST_KNOWN_VALUE_NOT_AT_REST"/>, <see cref="LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED"/> or <see cref="LAST_KNOWN_VALUE_IS_AT_REST_NEEDS_TO_BROADCAST"/></para>
        /// <para>IMPORTANT: If <see cref="gonetParticipant"/> has a value of false for <see cref="GONetParticipant.IsOKToStartAutoMagicalProcessing"/>, then this will return false no matter what!</para>
        /// </summary>
        internal bool HaveAnyValuesChangedSinceLastCheck_AppendNewlyAtRest(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches, long nowElapsedTicks, List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> valuesAtRestToBroadcast)
        {
            bool hasChange = false;

            //GONetLog.Debug($"gnp.name: {gonetParticipant.name} IsOKToStartAutoMagicalProcessing: {gonetParticipant.IsOKToStartAutoMagicalProcessing}");
            if (gonetParticipant.IsOKToStartAutoMagicalProcessing)
            {
                for (int i = 0; i < valuesCount; ++i)
                {
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                    if (DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches) &&
                        !ShouldSkipSync(valueChangeSupport, i)) // TODO examine eval order and performance...should this be first or last?
                    {
                        if (valueChangeSupport.lastKnownValue == valueChangeSupport.lastKnownValue_previous)
                        {
                            //Debug.Log($"AT REST possible @ index: {valueChangeSupport.index} this.type: {GetType().Name}");

                            //bool doesAtRestEvenApply = false; // TODO FIXME put the real processing of at rest back: valueChangeSupport.syncAttribute_ShouldBlendBetweenValuesReceived;
                            bool doesAtRestEvenApply = valueChangeSupport.syncAttribute_ShouldBlendBetweenValuesReceived;
                            if (doesAtRestEvenApply)
                            { // if the value is the same and our at rest stuff applies here, we need to check if this is (newly) considered 'at rest' or not and act accordingly (i.e., signal that further action needs to be taken to tell others)
                                bool isConsideredAtRest =
                                    (nowElapsedTicks - lastKnownValueChangedAtElapsedTicks[i])
                                    >
                                    (TimeSpan.FromSeconds(valueChangeSupport.syncAttribute_SyncChangesEverySeconds).Ticks << 1); // TODO make this configurable and even if not still need to precalculate (via adding new value set during generation)

                                if (isConsideredAtRest)
                                {
                                    lastKnownValueAtRestBits[i] |= LAST_KNOWN_VALUE_IS_AT_REST_NEEDS_TO_BROADCAST; // or in this value instead of assign because it might already be the value of LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED and we do not want to change that!
                                    if (lastKnownValueAtRestBits[i] == LAST_KNOWN_VALUE_IS_AT_REST_NEEDS_TO_BROADCAST)
                                    {
                                        valuesAtRestToBroadcast.Add(valueChangeSupport);
                                        //GONetLog.Debug($"I now recognize as at rest.  index: {valueChangeSupport.index}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            // need to consider quantization here so we do not consider the value changed if the quanitized value is the same even though the actual value is changed
                            if (!AreEqualConsideringQuantization(valueChangeSupport, valueChangeSupport.lastKnownValue, valueChangeSupport.lastKnownValue_previous))
                            {
                                if (lastKnownValueAtRestBits[i] == LAST_KNOWN_VALUE_IS_AT_REST_NEEDS_TO_BROADCAST)
                                {
                                    GONetLog.Warning("Value was 'At Rest' but it was not broadcasted!  And now the value has changed so that last at rest will not get broadcast, which is really probably will not be noticed, but it should have been broadcast...why not?  hmmmm...");
                                }

                                lastKnownValueAtRestBits[i] = LAST_KNOWN_VALUE_NOT_AT_REST;
                                lastKnownValueChangesSinceLastCheck[i] = true;
                                lastKnownValueChangedAtElapsedTicks[i] = nowElapsedTicks;
                                hasChange = true;
                            }
                        }
                    }
                    else
                    {
                        //GONetLog.Debug($"skip wads @ index: {valueChangeSupport.index} this.type: {GetType().Name}, does match? {DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches)}");
                    }
                }
            }

            return hasChange;
        }

        private bool AreEqualConsideringQuantization(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport, GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            bool areEqual = valueA == valueB;
            if (!areEqual // if they are equal unquantized, then ASSume they will also be the same after quantization since that process is supposed to be deterministic!
                /* && valueChangeSupport.syncAttribute_QuantizerSettingsGroup.CanBeUsedForQuantization */) // IMPORTANT: we had to remove this since the custom serializer ones would have this as false!
            {
                areEqual = AreEqualQuantized(valueChangeSupport.index, valueA, valueB);
            }
            return areEqual;
        }

        /// <summary>
        /// PRE: value at <paramref name="singleIndex"/> is known to be configured to be quantized
        /// NOTE: This is only virtual to avoid upgrading customers prior to this being added having compilation issues when upgrading from a previous version of GONet
        /// </summary>
        protected virtual bool AreEqualQuantized(byte singleIndex, GONetSyncableValue valueA, GONetSyncableValue valueB) { return false; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldSkipSync(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport, int index)
        {
            return valueChangeSupport.syncAttribute_ShouldSkipSync != null && valueChangeSupport.syncAttribute_ShouldSkipSync(valueChangeSupport, index);
        }

        internal void OnValueChangeCheck_Reset(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                if (DoesMatchUniqueGrouping(valuesChangesSupport[i], onlyMatchIfUniqueGroupingMatches))
                {
                    lastKnownValueChangesSinceLastCheck[i] = false;
                }
            }
        }

        internal uint QuantizeSingle(byte singleIndex, GONetSyncableValue value)
        {
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[singleIndex];
            float valueAsFloat = value.System_Single;//valueChangeSupport.lastKnownValue // TODO maybe use this instead of accepting in value
            valueAsFloat -= valuesChangesSupport[singleIndex].baselineValue_current.System_Single;
            QuantizerSettingsGroup quantizeSettings = valueChangeSupport.syncAttribute_QuantizerSettingsGroup;
            return Quantizer.LookupQuantizer(quantizeSettings).Quantize(valueAsFloat);
        }

        internal void SerializeSingleQuantized(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex, GONetSyncableValue value)
        {
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[singleIndex];
            QuantizerSettingsGroup quantizeSettings = valueChangeSupport.syncAttribute_QuantizerSettingsGroup;

            // SUB-QUANTIZATION DIAGNOSTIC: Check delta-from-baseline before quantizing
            float currentValue = value.System_Single;
            float baselineValue = valuesChangesSupport[singleIndex].baselineValue_current.System_Single;
            float deltaFromBaseline = currentValue - baselineValue;
            Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(
                gonetParticipant.GONetId,
                valueChangeSupport.memberName,
                deltaFromBaseline,
                quantizeSettings,
                null); // No custom serializer for plain float quantization

            uint valueQuantized = QuantizeSingle(singleIndex, value);
            bitStream_appendTo.WriteUInt(valueQuantized, quantizeSettings.quantizeToBitCount);
        }

        internal GONetSyncableValue DeserializeSingleQuantized(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex)
        {
            QuantizerSettingsGroup quantizeSettings = valuesChangesSupport[singleIndex].syncAttribute_QuantizerSettingsGroup;
            uint valueQuantized;
            bitStream_readFrom.ReadUInt(out valueQuantized, quantizeSettings.quantizeToBitCount);
            float valueUnquantized = Quantizer.LookupQuantizer(quantizeSettings).Unquantize(valueQuantized);
            return valueUnquantized + valuesChangesSupport[singleIndex].baselineValue_current.System_Single;
        }

        /// <summary>
        /// Calculates velocity (delta/time) for a value based on previous snapshot.
        /// Used when serializing velocity packets in alternating value/velocity system.
        /// </summary>
        protected GONetSyncableValue CalculateVelocity(byte singleIndex, GONetSyncableValue currentValue, long currentElapsedTicks)
        {
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[singleIndex];

            // Get previous snapshot
            int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;
            if (mostRecentChangesIndex < 0 || valueChangeSupport.mostRecentChanges_usedSize == 0)
            {
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] CalculateVelocity: No previous snapshot, returning zero velocity");
#endif
                // No previous value, velocity is zero
                if (currentValue.GONetSyncType == GONetSyncableValueTypes.System_Single)
                    return 0f;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2)
                    return UnityEngine.Vector2.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3)
                    return UnityEngine.Vector3.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4)
                    return UnityEngine.Vector4.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion)
                    return UnityEngine.Quaternion.identity; // Angular velocity handling will be added later

                return currentValue; // Fallback
            }

            PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
            long deltaTimeTicks = currentElapsedTicks - previousSnapshot.elapsedTicksAtChange;

            if (deltaTimeTicks <= 0)
            {
                // No time elapsed, cannot calculate velocity
                if (currentValue.GONetSyncType == GONetSyncableValueTypes.System_Single)
                    return 0f;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2)
                    return UnityEngine.Vector2.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3)
                    return UnityEngine.Vector3.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4)
                    return UnityEngine.Vector4.zero;
                else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion)
                    return UnityEngine.Quaternion.identity;

                return currentValue;
            }

            float deltaTimeSeconds = (float)deltaTimeTicks / System.Diagnostics.Stopwatch.Frequency;

            // Calculate velocity based on type
            if (currentValue.GONetSyncType == GONetSyncableValueTypes.System_Single)
            {
                float delta = currentValue.System_Single - previousSnapshot.numericValue.System_Single;
                float velocity = delta / deltaTimeSeconds;
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] CalculateVelocity[{singleIndex}]: Float current={currentValue.System_Single}, prev={previousSnapshot.numericValue.System_Single}, delta={delta}, Δt={deltaTimeSeconds}s, velocity={velocity}");
#endif
                return velocity;
            }
            else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector2)
            {
                UnityEngine.Vector2 delta = currentValue.UnityEngine_Vector2 - previousSnapshot.numericValue.UnityEngine_Vector2;
                UnityEngine.Vector2 velocity = delta / deltaTimeSeconds;
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] CalculateVelocity[{singleIndex}]: Vector2 delta={delta}, Δt={deltaTimeSeconds}s, velocity={velocity}");
#endif
                return velocity;
            }
            else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3)
            {
                UnityEngine.Vector3 delta = currentValue.UnityEngine_Vector3 - previousSnapshot.numericValue.UnityEngine_Vector3;
                UnityEngine.Vector3 velocity = delta / deltaTimeSeconds;
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] CalculateVelocity[{singleIndex}]: Vector3 current={currentValue.UnityEngine_Vector3}, prev={previousSnapshot.numericValue.UnityEngine_Vector3}, delta={delta}, Δt={deltaTimeSeconds}s, velocity={velocity}");
#endif
                return velocity;
            }
            else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector4)
            {
                UnityEngine.Vector4 delta = currentValue.UnityEngine_Vector4 - previousSnapshot.numericValue.UnityEngine_Vector4;
                return delta / deltaTimeSeconds;
            }
            else if (currentValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion)
            {
                // Calculate angular velocity (omega) from quaternion delta
                UnityEngine.Quaternion q0 = previousSnapshot.numericValue.UnityEngine_Quaternion;
                UnityEngine.Quaternion q1 = currentValue.UnityEngine_Quaternion;
                UnityEngine.Vector3 omega = CalculateAngularVelocity(q0, q1, deltaTimeSeconds);
                return omega; // Store as Vector3 (axis * radians/sec)
            }

            return currentValue; // Fallback
        }

        /// <summary>
        /// Calculates angular velocity (omega) from two quaternions.
        /// Returns a Vector3 representing the axis of rotation scaled by angular speed (radians/second).
        /// Formula: omega = 2 * axis * angle / deltaTime
        /// where (axis, angle) = ToAxisAngle(q1 * q0^-1)
        /// </summary>
        protected UnityEngine.Vector3 CalculateAngularVelocity(UnityEngine.Quaternion q0, UnityEngine.Quaternion q1, float deltaTime)
        {
            if (deltaTime <= 0f)
                return UnityEngine.Vector3.zero;

            // Calculate relative rotation: q_delta = q1 * q0^-1
            UnityEngine.Quaternion q0Inverse = UnityEngine.Quaternion.Inverse(q0);
            UnityEngine.Quaternion qDelta = q1 * q0Inverse;

            // Ensure shortest path (quaternion double-cover: q and -q represent same rotation)
            if (qDelta.w < 0f)
            {
                qDelta.x = -qDelta.x;
                qDelta.y = -qDelta.y;
                qDelta.z = -qDelta.z;
                qDelta.w = -qDelta.w;
            }

            // Extract axis-angle representation
            // For quaternion q = (cos(angle/2), sin(angle/2) * axis)
            // angle = 2 * acos(w)
            // axis = (x, y, z) / sin(angle/2)

            float angle = 2f * UnityEngine.Mathf.Acos(UnityEngine.Mathf.Clamp(qDelta.w, -1f, 1f));

            // Handle near-zero rotation (avoid division by zero)
            float sinHalfAngle = UnityEngine.Mathf.Sin(angle * 0.5f);
            if (UnityEngine.Mathf.Abs(sinHalfAngle) < 1e-6f)
            {
                return UnityEngine.Vector3.zero;
            }

            // Extract axis
            UnityEngine.Vector3 axis = new UnityEngine.Vector3(
                qDelta.x / sinHalfAngle,
                qDelta.y / sinHalfAngle,
                qDelta.z / sinHalfAngle
            );

            // Angular velocity = axis * angle / deltaTime
            UnityEngine.Vector3 omega = axis * (angle / deltaTime);

            return omega;
        }

        internal abstract void SetAutoMagicalSyncValue(byte index, GONetSyncableValue value);

        internal abstract GONetSyncableValue GetAutoMagicalSyncValue(byte index);

        /// <summary>
        /// Serializes all values of appropriaate member variables internally to <paramref name="bitStream_appendTo"/>.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal abstract void SerializeAll(Utils.BitByBitByteArrayBuilder bitStream_appendTo);

        internal abstract void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex, bool isVelocityBundle = false);

        public bool TryGetIndexByMemberName(string memberName, out byte index)
        {
            var valueChangeSupport = valuesChangesSupport.FirstOrDefault(x => x != null && x.memberName == memberName);
            index = valueChangeSupport != null ? valueChangeSupport.index : default;
            return valueChangeSupport != null;
        }

        /// <summary>
        /// VELOCITY-AUGMENTED SYNC: Checks if the calculated velocity for a value fits within its quantization range.
        /// Used to decide if value should be sent in VELOCITY bundle or fallback to VALUE bundle.
        /// </summary>
        internal bool IsVelocityWithinQuantizationRange(byte valueIndex)
        {
            var changesSupport = valuesChangesSupport[valueIndex];

            // Non-velocity-eligible values always return false
            if (!changesSupport.isVelocityEligible)
            {
                return false;
            }

            // CRITICAL FIX: Use lastKnownValue (authority's actual transform) NOT mostRecentChanges (client-received snapshots)!
            // mostRecentChanges is for VALUE BLENDING on clients, not for authority velocity calculation!
            var current = changesSupport.lastKnownValue;
            var previous = changesSupport.lastKnownValue_previous;

            // Check if we have previous value (if current == previous, no change has occurred yet)
            if (current.GONetSyncType != previous.GONetSyncType)
            {
                return false; // Types don't match - not initialized yet
            }

            // OPTIMIZATION: Use pre-calculated per-sync-interval bounds (no division required)
            // These were calculated at initialization: bounds_per_second * deltaTime
            float lowerBoundPerInterval = changesSupport.velocityQuantizeLowerBound_PerSyncInterval;
            float upperBoundPerInterval = changesSupport.velocityQuantizeUpperBound_PerSyncInterval;

            // Calculate raw delta (no division) and compare against per-interval bounds
            switch (changesSupport.codeGenerationMemberType)
            {
                case GONetSyncableValueTypes.UnityEngine_Vector3:
                    {
                        var positionDelta = current.UnityEngine_Vector3 - previous.UnityEngine_Vector3;
                        // Check each component against per-interval bounds
                        return positionDelta.x >= lowerBoundPerInterval && positionDelta.x <= upperBoundPerInterval &&
                               positionDelta.y >= lowerBoundPerInterval && positionDelta.y <= upperBoundPerInterval &&
                               positionDelta.z >= lowerBoundPerInterval && positionDelta.z <= upperBoundPerInterval;
                    }

                case GONetSyncableValueTypes.UnityEngine_Quaternion:
                    {
                        // For quaternions, check angular velocity magnitude
                        // TODO: Implement proper angular velocity check if needed
                        return true; // For now, assume quaternions always fit (rotation is typically slow)
                    }

                case GONetSyncableValueTypes.System_Single:
                    {
                        var valueDelta = current.System_Single - previous.System_Single;
                        return valueDelta >= lowerBoundPerInterval && valueDelta <= upperBoundPerInterval;
                    }

                default:
                    return false; // Unknown type, use VALUE bundle
            }
        }

        /// <summary>
        /// Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal abstract void DeserializeInitAll(Utils.BitByBitByteArrayBuilder bitStream_readFrom, long assumedElapsedTicksAtChange);

        /// <summary>
        ///  Deserializes a single value (using <paramref name="singleIndex"/> to know which) from <paramref name="bitStream_readFrom"/>
        ///  and uses them to modify appropriate member variables internally.
        /// </summary>
        internal virtual void DeserializeInitSingle(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange, bool useVelocitySerializer = false)
        {
            //GONetLog.Debug($"********************should be doing some init single index: {singleIndex}");
            GONetSyncableValue value = DeserializeInitSingle_ReadOnlyNotApply(bitStream_readFrom, singleIndex, useVelocitySerializer);
            InitSingle(value, singleIndex, assumedElapsedTicksAtChange);
        }

        /// <summary>
        /// NOTE: This is only virtual to avoid upgrading customers prior to this being added having compilation issues when upgrading from a previous version of GONet.
        /// </summary>
        /// <param name="bitStream_readFrom">The bit stream to deserialize from</param>
        /// <param name="singleIndex">The index of the value to deserialize</param>
        /// <param name="useVelocitySerializer">Velocity-augmented sync: If true, uses cachedVelocitySerializers; if false, uses cachedValueSerializers. Default false for backward compatibility.</param>
        /// <returns>The deserialized value (either VALUE or VELOCITY depending on useVelocitySerializer parameter)</returns>
        internal virtual GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, bool useVelocitySerializer = false)
        {
            // NOTE: this return here is dummy and overrides in child classes will NOT call base.DeserializeInitSingle_ReadOnlyNotApply()
            return default;
        }

        /// <summary>
        /// At time of writing, this is here to take the output from <see cref="DeserializeInitSingle_ReadOnlyNotApply(BitByBitByteArrayBuilder, byte)"/>
        /// as the input argument <paramref name="value"/> at some delayed time for whatever reason 
        /// (e.g., after <see cref="GONetMain.valueBlendingBufferLeadSeconds"/> has transpired).
        /// </summary>
        internal abstract void InitSingle(GONetSyncableValue value, byte singleIndex, long assumedElapsedTicksAtChange);

        internal abstract void UpdateLastKnownValues(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches);

        internal void AppendListWithChangesSinceLastCheck(List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend, GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                if (lastKnownValueChangesSinceLastCheck[i] && DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches))
                {
                    syncValuesToSend.Add(valueChangeSupport);
                }
            }
        }

        /// <summary>
        /// PRE: <see cref="lastKnownValueChangesSinceLastCheck"/> still has true values in it and has not been reset via <see cref="OnValueChangeCheck_Reset(GONetMain.SyncBundleUniqueGrouping)"/>
        /// POST: <see cref="doesBaselineValueNeedAdjusting"/> is updated with true values for the baseline values needing adjustment, but adjustment is not applied until <see cref="ApplyAnnotatedBaselineValueAdjustments"/> is called later
        /// POST: for the values where <see cref="lastKnownValueChangesSinceLastCheck"/> is true, calls to update min and max if we went past the previous min/max (e.g., <see cref="GONetSyncableValue.UpdateMinimumEncountered_IfApppropriate"/>)
        /// </summary>
        internal void AnnotateMyBaselineValuesNeedingAdjustment()
        {
            if (gonetParticipant.IsMine)
            {
                for (int i = 0; i < valuesCount; ++i)
                {
                    if (lastKnownValueChangesSinceLastCheck[i])
                    {
                        GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];

                        bool isAppropriate =
                            valueChangeSupport.syncAttribute_QuantizerSettingsGroup.quantizeToBitCount > 0 &&
                            IsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange((byte)i, valueChangeSupport);

                        if (isAppropriate)
                        {
                            doesBaselineValueNeedAdjusting[i] = true;
                        }

                        GONetSyncableValue.UpdateMinimumEncountered_IfApppropriate(ref valueChangeSupport.valueLimitEncountered_min, valueChangeSupport.lastKnownValue);
                        GONetSyncableValue.UpdateMaximumEncountered_IfApppropriate(ref valueChangeSupport.valueLimitEncountered_max, valueChangeSupport.lastKnownValue);
                    }
                }
            }
        }

        /// <summary>
        /// POST: If appropriate, baseline values updated and <paramref name="baselineAdjustmentsEventQueue"/> has one of 
        ///       each of the following enqueued for sending later at right time/thread: 
        ///       1) <see cref="ValueMonitoringSupport_BaselineExpiredEvent"/>
        ///       2) <see cref="ValueMonitoringSupport_NewBaselineEvent"/>
        /// </summary>
        /// <param name="baselineAdjustmentsEventQueue"></param>
        internal void ApplyAnnotatedBaselineValueAdjustments(Queue<IGONetEvent> baselineAdjustmentsEventQueue)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                if (doesBaselineValueNeedAdjusting[i])
                {
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                    valueChangeSupport.baselineValue_current = valueChangeSupport.lastKnownValue;

                    { // queue up for later (i.e., in proper thread): fire reliable events to everyone that the baseline changed so everyone adjusts accordingly so the networking will work 
                        var expirationEvent = new ValueMonitoringSupport_BaselineExpiredEvent() { GONetId = gonetParticipant.GONetId, ValueIndex = (byte)i };
                        baselineAdjustmentsEventQueue.Enqueue(expirationEvent); //used to directly do the following, but cannot because of threading limitations and now will delay to do this until in proper thread: GONetMain.EventBus.Publish(expirationEvent);

                        ValueMonitoringSupport_NewBaselineEvent newBaselineEvent = CreateNewBaselineValueEvent(gonetParticipant.GONetId, (byte)i, valueChangeSupport.baselineValue_current);
                        baselineAdjustmentsEventQueue.Enqueue(newBaselineEvent); //used to directly do the following, but cannot because of threading limitations and now will delay to do this until in proper thread: GONetMain.EventBus.Publish(newBaselineEvent);
                    }

                    doesBaselineValueNeedAdjusting[i] = false;
                }
            }
        }

        /// <summary>
        /// The only reason this method is virtual instead of abstract is due to how the upgrade from 1.0.3 to 1.0.4 will break people's project (i.e., not compile without some fixing)...so there.
        /// </summary>
        internal virtual bool IsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange(byte index, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport) { return default; }

        /// <summary>
        /// The only reason this method is virtual instead of abstract is due to how the upgrade from 1.0.3 to 1.0.4 will break people's project (i.e., not compile without some fixing)...so there.
        /// </summary>
        internal virtual ValueMonitoringSupport_NewBaselineEvent CreateNewBaselineValueEvent(uint gonetId, byte index, GONetSyncableValue newBaselineValue) { return default; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DoesMatchUniqueGrouping(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport, GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)
        {
            // PHYSICS SYNC SPECIAL CASE: Physics sync grouping (mustRunOnUnityMainThread=true, END_OF_FRAME frequency)
            // bypasses frequency matching and processes ALL members. This is because:
            // 1. Physics objects are filtered at participant level (IsRigidBodyOwnerOnlyControlled check in Process() loop)
            // 2. Physics sync runs at 50Hz (via WaitForFixedUpdate), not based on member frequency
            // 3. Member frequencies (like 24Hz for Transform.position/rotation) don't apply to physics pipeline
            bool isPhysicsSyncGrouping = onlyMatchIfUniqueGroupingMatches.mustRunOnUnityMainThread &&
                                         onlyMatchIfUniqueGroupingMatches.scheduleFrequency == AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS;
            if (isPhysicsSyncGrouping)
            {
                // Physics sync: Process all members (frequency check bypassed)
                return onlyMatchIfUniqueGroupingMatches.reliability == valueChangeSupport.syncAttribute_Reliability;
            }

            // Regular sync: Match both frequency and reliability
            return onlyMatchIfUniqueGroupingMatches.scheduleFrequency == valueChangeSupport.syncAttribute_SyncChangesEverySeconds &&
                   onlyMatchIfUniqueGroupingMatches.reliability == valueChangeSupport.syncAttribute_Reliability;
        }

        internal void AppendListWithAllValues(List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                syncValuesToSend.Add(valuesChangesSupport[i]);
            }
        }

        internal void ResetAtRestValues(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                if (!(lastKnownValueAtRestBits[i] != LAST_KNOWN_VALUE_NOT_AT_REST) && DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches))
                {
                    lastKnownValueAtRestBits[i] = LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED;
                }
            }
        }

        internal bool TryGetBlendedValue(
            byte index,
            NumericValueChangeSnapshot[] valueBuffer,
            int valueCount,
            long atElapsedTicks,
            out GONetSyncableValue blendedValue,
            out bool didExtrapolatePastMostRecentChanges)
        {
            // Check if velocity-aware blending should be used
            bool hasVelocityData = false;
            if (valueCount > 0)
            {
                // Check most recent snapshot for velocity data
                NumericValueChangeSnapshot mostRecent = valueBuffer[valueCount - 1];
                // Velocity data exists if: flagged as synthesized OR velocity type matches value type
                hasVelocityData = mostRecent.wasSynthesizedFromVelocity ||
                                  (mostRecent.velocity.GONetSyncType == mostRecent.numericValue.GONetSyncType) ||
                                  (mostRecent.numericValue.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Quaternion &&
                                   mostRecent.velocity.GONetSyncType == GONetSyncableValueTypes.UnityEngine_Vector3); // Angular velocity stored as Vector3
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] TryGetBlendedValue[{index}]: hasVelocityData={hasVelocityData}, wasSynthesized={mostRecent.wasSynthesizedFromVelocity}, velocityType={mostRecent.velocity.GONetSyncType}, valueType={mostRecent.numericValue.GONetSyncType}");
#endif
            }

            // Use velocity blending if available and velocity data exists
            if (hasVelocityData)
            {
                IGONetAutoMagicalSync_CustomVelocityBlending customVelocityBlending = cachedCustomVelocityBlendings[index];
                if (customVelocityBlending != null)
                {
#if GONET_VELOCITY_SYNC_DEBUG
                    GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] TryGetBlendedValue[{index}]: Using velocity-aware extrapolation with {customVelocityBlending.GetType().Name}");
#endif
                    // Use velocity-aware extrapolation
                    blendedValue = customVelocityBlending.ExtrapolateWithVelocityContext(
                        valueBuffer,
                        valueCount,
                        atElapsedTicks,
                        gonetParticipant);

#if GONET_VELOCITY_SYNC_DEBUG
                    GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] TryGetBlendedValue[{index}]: Extrapolated value={blendedValue}");
#endif
                    // Velocity extrapolation always extrapolates (uses velocity data)
                    didExtrapolatePastMostRecentChanges = true;
                    return true;
                }
#if GONET_VELOCITY_SYNC_DEBUG
                else
                {
                    GONetLog.Warning($"[VelocitySync][{gonetParticipant.GONetId}] TryGetBlendedValue[{index}]: hasVelocityData=true but customVelocityBlending is NULL! Falling back to standard blending.");
                }
#endif
            }

            // Fall back to standard value blending
            IGONetAutoMagicalSync_CustomValueBlending customValueBlending = cachedCustomValueBlendings[index];
            //GONetLog.Debug($"grease null blender? {(customValueBlending == null)} @ index: {index}");
            if (customValueBlending != null)
            {
                return customValueBlending.TryGetBlendedValue(valueBuffer, valueCount, atElapsedTicks, out blendedValue, out didExtrapolatePastMostRecentChanges);
            }

            didExtrapolatePastMostRecentChanges = false;
            blendedValue = default;
            return false;
        }
    }

    internal static class GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory
    {
        internal delegate GONetParticipant_AutoMagicalSyncCompanion_Generated GONetParticipant_AutoMagicalSyncCompanion_Generated_FactoryDelegate(GONetParticipant gonetParticipant);
        internal static GONetParticipant_AutoMagicalSyncCompanion_Generated_FactoryDelegate theRealness = delegate (GONetParticipant gonetParticipant) 
        {
            throw new System.Exception("Run code generation or else the correct generated instance cannot be created.");
        };

        internal delegate HashSet<QuantizerSettingsGroup> GetQuantizerSettingsDelegate();
        internal static GetQuantizerSettingsDelegate theRealness_quantizerSettings = delegate ()
        {
            throw new System.Exception("Run code generation or else the correct QuaniterSettingsGroup values cannot be identified.");
        };

        /// <summary>
        /// Order of operations in static processing, this needs to come after the declaration of <see cref="theRealness"/>.
        /// </summary>
        private static readonly BobWad theBobber = new BobWad();

        internal static GONetParticipant_AutoMagicalSyncCompanion_Generated CreateInstance(GONetParticipant gonetParticipant)
        {
            return theRealness(gonetParticipant);
        }

        internal static HashSet<QuantizerSettingsGroup> GetAllPossibleUniqueQuantizerSettingsGroups()
        {
            return theRealness_quantizerSettings();
        }
    }

    /// <summary>
    /// TODO: make the main dll internals visible to editor dll so this can be made internal again
    /// </summary>
    public partial class BobWad
    {
        internal BobWad() { }
    }

    internal static class GONet_SyncEvent_ValueChangeProcessed_Generated_Factory
    {
        internal delegate SyncEvent_ValueChangeProcessed GONet_SyncValueChangeProcessedEvent_Generated_FactoryDelegate(SyncEvent_ValueChangeProcessedExplanation explanation, long elapsedTicks, ushort filterUsingOwnerAuthorityId, GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion, byte index);
        internal static GONet_SyncValueChangeProcessedEvent_Generated_FactoryDelegate theRealness = delegate (SyncEvent_ValueChangeProcessedExplanation explanation, long elapsedTicks, ushort filterUsingOwnerAuthorityId, GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion, byte index)
        {
            throw new System.Exception("Run code generation or else the correct generated instance cannot be created.");
        };

        internal delegate SyncEvent_ValueChangeProcessed GONet_SyncValueChangeProcessedEvent_Generated_FactoryDelegate_Copy(SyncEvent_ValueChangeProcessed original);
        internal static GONet_SyncValueChangeProcessedEvent_Generated_FactoryDelegate_Copy theRealness_copy = delegate (SyncEvent_ValueChangeProcessed original)
        {
            throw new System.Exception("Run code generation or else the correct generated instance cannot be created.");
        };

        internal static List<Type> allUniqueSyncEventTypes;

        /// <summary>
        /// Order of operations in static processing, this needs to come after the declaration of <see cref="theRealness"/>.
        /// </summary>
        private static readonly BobWad theBobber = new BobWad();

        internal static SyncEvent_ValueChangeProcessed CreateInstance(SyncEvent_ValueChangeProcessedExplanation explanation, long elapsedTicks, ushort filterUsingOwnerAuthorityId, GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion, byte syncMemberIndex)
        {
            var instance = theRealness(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion, syncMemberIndex);
            instance.ProcessedAtElapsedTicks = 0;
            return instance;
        }

        internal static SyncEvent_ValueChangeProcessed CreateCopy(SyncEvent_ValueChangeProcessed original)
        {
            var copy = theRealness_copy(original);
            copy.ProcessedAtElapsedTicks = original.ProcessedAtElapsedTicks;
            return copy;
        }

        internal static IEnumerable<Type> GetAllUniqueSyncEventTypes()
        {
            return allUniqueSyncEventTypes;
        }
    }
}
