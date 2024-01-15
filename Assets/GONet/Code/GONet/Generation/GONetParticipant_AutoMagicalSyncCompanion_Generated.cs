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
        protected IGONetAutoMagicalSync_CustomSerializer[] cachedCustomSerializers;

        protected static readonly ArrayPool<IGONetAutoMagicalSync_CustomValueBlending> cachedCustomValueBlendingsArrayPool =
            new ArrayPool<IGONetAutoMagicalSync_CustomValueBlending>(1000, 10, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN, EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX);
        protected IGONetAutoMagicalSync_CustomValueBlending[] cachedCustomValueBlendings;

        protected static readonly ConcurrentDictionary<Thread, byte[]> valueDeserializeByteArrayByThreadMap = new ConcurrentDictionary<Thread, byte[]>(5, 5);

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
            cachedCustomValueBlendingsArrayPool.Return(cachedCustomValueBlendings);

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
                        //Debug.Log($"skip wads @ index: {valueChangeSupport.index} this.type: {GetType().Name}, does match? {DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches)}");
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

        internal abstract void SetAutoMagicalSyncValue(byte index, GONetSyncableValue value);

        internal abstract GONetSyncableValue GetAutoMagicalSyncValue(byte index);

        /// <summary>
        /// Serializes all values of appropriaate member variables internally to <paramref name="bitStream_appendTo"/>.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal abstract void SerializeAll(Utils.BitByBitByteArrayBuilder bitStream_appendTo);

        internal abstract void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex);

        public bool TryGetIndexByMemberName(string memberName, out byte index)
        {
            var valueChangeSupport = valuesChangesSupport.FirstOrDefault(x => x != null && x.memberName == memberName);
            index = valueChangeSupport != null ? valueChangeSupport.index : default;
            return valueChangeSupport != null;
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
        internal void DeserializeInitSingle(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange)
        {
            GONetSyncableValue value = DeserializeInitSingle_ReadOnlyNotApply(bitStream_readFrom, singleIndex);
            InitSingle(value, singleIndex, assumedElapsedTicksAtChange);
        }

        /// <summary>
        /// NOTE: This is only virtual to avoid upgrading customers prior to this being added having compilation issues when upgrading from a previous version of GONet
        /// </summary>
        internal virtual GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex)
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
            return 
                onlyMatchIfUniqueGroupingMatches.scheduleFrequency == valueChangeSupport.syncAttribute_SyncChangesEverySeconds &&
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
            out bool didExtrapolate)
        {
            IGONetAutoMagicalSync_CustomValueBlending customValueBlending = cachedCustomValueBlendings[index];
            if (customValueBlending != null)
            {
                return customValueBlending.TryGetBlendedValue(valueBuffer, valueCount, atElapsedTicks, out blendedValue, out didExtrapolate);
            }

            didExtrapolate = false;
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
