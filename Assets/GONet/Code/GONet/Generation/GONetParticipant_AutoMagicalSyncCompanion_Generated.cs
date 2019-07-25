﻿/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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

using GONet.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet.Generation
{
    /// <summary>
    /// TODO: make the main dll internals visible to editor dll so this can be made internal again
    /// </summary>
    public abstract class GONetParticipant_AutoMagicalSyncCompanion_Generated
    {
        internal GONetParticipant gonetParticipant;

        const int EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MIN = 5;
        const int EXPECTED_AUTO_SYNC_MEMBER_COUNT_PER_GONetParticipant_MAX = 30;

        protected static readonly ArrayPool<bool> lastKnownValuesChangedArrayPool = 
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

        internal GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue[] valuesChangesSupport;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;
        }

        ~GONetParticipant_AutoMagicalSyncCompanion_Generated()
        {
            lastKnownValuesChangedArrayPool.Return(lastKnownValueChangesSinceLastCheck);

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

        /// <summary>
        /// POST: lastKnownValueChangesSinceLastCheck updated with true of false to indicate which value indices inside <see cref="lastKnownValues"/> represent new/changed values.
        /// </summary>
        internal bool HaveAnyValuesChangedSinceLastCheck(GONetMain.SyncBundleUniqueGrouping? onlyMatchIfUniqueGroupingMatches = default)
        {
            bool hasChange = false;
            for (int i = 0; i < valuesCount; ++i)
            {
                GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[i];
                if (DoesMatchUniqueGrouping(valueChangeSupport, onlyMatchIfUniqueGroupingMatches) &&
                    !Equals(valueChangeSupport.lastKnownValue, valueChangeSupport.lastKnownValue_previous) &&
                    !ShouldSkipSync(valueChangeSupport, i)) // TODO examine eval order and performance...should this be first or last?
                {
                    lastKnownValueChangesSinceLastCheck[i] = true;
                    hasChange = true;
                }
            }
            return hasChange;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldSkipSync(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport, int index)
        {
            return valueChangeSupport.syncAttribute_ShouldSkipSync != null && valueChangeSupport.syncAttribute_ShouldSkipSync(valueChangeSupport, index);
        }

        internal void OnValueChangeCheck_Reset(GONetMain.SyncBundleUniqueGrouping? onlyMatchIfUniqueGroupingMatches = default)
        {
            if (!onlyMatchIfUniqueGroupingMatches.HasValue)
            {
                Array.Clear(lastKnownValueChangesSinceLastCheck, 0, valuesCount);
            }
            else
            {
                for (int i = 0; i < valuesCount; ++i)
                {
                    if (DoesMatchUniqueGrouping(valuesChangesSupport[i], onlyMatchIfUniqueGroupingMatches))
                    {
                        lastKnownValueChangesSinceLastCheck[i] = false;
                    }
                }
            }
        }

        internal void SerializeSingleQuantized(Utils.BitStream bitStream_appendTo, byte singleIndex, object value)
        {
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[singleIndex];
            float valueAsFloat = (float)value;//valueChangeSupport.lastKnownValue // TODO maybe use this instead of accepting in value
            QuantizerSettingsGroup quantizeSettings = valueChangeSupport.syncAttribute_QuantizerSettingsGroup;
            uint valueQuantized = Quantizer.LookupQuantizer(quantizeSettings).Quantize(valueAsFloat);
            bitStream_appendTo.WriteUInt(valueQuantized, quantizeSettings.quantizeToBitCount);
        }

        internal object DeserializeSingleQuantized(Utils.BitStream bitStream_readFrom, byte singleIndex)
        {
            QuantizerSettingsGroup quantizeSettings = valuesChangesSupport[singleIndex].syncAttribute_QuantizerSettingsGroup;
            uint valueQuantized;
            bitStream_readFrom.ReadUInt(out valueQuantized, quantizeSettings.quantizeToBitCount);
            float valueUnquantized = Quantizer.LookupQuantizer(quantizeSettings).Unquantize(valueQuantized);
            return valueUnquantized;
        }

        internal abstract void SetAutoMagicalSyncValue(byte index, object value);

        internal abstract object GetAutoMagicalSyncValue(byte index);

        /// <summary>
        /// Serializes all values of appropriaate member variables internally to <paramref name="bitStream_appendTo"/>.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal abstract void SerializeAll(Utils.BitStream bitStream_appendTo);

        internal abstract void SerializeSingle(Utils.BitStream bitStream_appendTo, byte singleIndex);

        /// <summary>
        /// Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal abstract void DeserializeInitAll(Utils.BitStream bitStream_readFrom, long assumedElapsedTicksAtChange);

        /// <summary>
        ///  Deserializes a ginel value (using <paramref name="singleIndex"/> to know which) from <paramref name="bitStream_readFrom"/>
        ///  and uses them to modify appropriate member variables internally.
        /// </summary>
        internal abstract void DeserializeInitSingle(Utils.BitStream bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange);

        internal abstract void UpdateLastKnownValues(GONetMain.SyncBundleUniqueGrouping? onlyMatchIfUniqueGroupingMatches = default);

        internal void AppendListWithChangesSinceLastCheck(List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend, GONetMain.SyncBundleUniqueGrouping? onlyMatchIfUniqueGroupingMatches = default)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool DoesMatchUniqueGrouping(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport, GONetMain.SyncBundleUniqueGrouping? onlyMatchIfUniqueGroupingMatches)
        {
            return !onlyMatchIfUniqueGroupingMatches.HasValue ||
                (onlyMatchIfUniqueGroupingMatches.Value.scheduleFrequency == valueChangeSupport.syncAttribute_SyncChangesEverySeconds &&
                 onlyMatchIfUniqueGroupingMatches.Value.reliability == valueChangeSupport.syncAttribute_Reliability);
        }

        internal void AppendListWithAllValues(List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                syncValuesToSend.Add(valuesChangesSupport[i]);
            }
        }
    }

    internal static class GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory
    {
        internal delegate GONetParticipant_AutoMagicalSyncCompanion_Generated GONetParticipant_AutoMagicalSyncCompanion_Generated_FactoryDelegate(GONetParticipant gonetParticipant);
        internal static GONetParticipant_AutoMagicalSyncCompanion_Generated_FactoryDelegate theRealness = delegate (GONetParticipant gonetParticipant) 
        {
            throw new System.Exception("Run code generation or else the correct generated instance cannot be created.");
        };
        /// <summary>
        /// Order of operations in static processing, this needs to come after the declaration of <see cref="theRealness"/>.
        /// </summary>
        private static readonly BobWad theBobber = new BobWad();

        internal static GONetParticipant_AutoMagicalSyncCompanion_Generated CreateInstance(GONetParticipant gonetParticipant)
        {
            return theRealness(gonetParticipant);
        }
    }

    /// <summary>
    /// TODO: make the main dll internals visible to editor dll so this can be made internal again
    /// </summary>
    public partial class BobWad
    {
        internal BobWad() { }
    }
}