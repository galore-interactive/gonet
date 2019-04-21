using GONet.Utils;
using System;
using System.Collections.Generic;

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
        protected byte valuesCount;

        protected bool[] lastKnownValueChangesSinceLastCheck;

        internal GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue[] valuesChangesSupport;

        /// <summary>
        /// POST: lastKnownValueChangesSinceLastCheck updated with true of false to indicate which value indices inside <see cref="lastKnownValues"/> represent new/changed values.
        /// </summary>
        internal bool HaveAnyValuesChangedSinceLastCheck()
        {
            bool hasChange = false;
            for (int i = 0; i < valuesCount; ++i)
            {
                if (!Equals(valuesChangesSupport[i].lastKnownValue, valuesChangesSupport[i].lastKnownValue_previous))
                {
                    lastKnownValueChangesSinceLastCheck[i] = true;
                    hasChange = true;
                }
            }
            return hasChange;
        }

        internal void OnValueChangeCheck_Reset()
        {
            Array.Clear(lastKnownValueChangesSinceLastCheck, 0, valuesCount);
        }

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated(GONetParticipant gonetParticipant)
        {
            this.gonetParticipant = gonetParticipant;
        }

        ~GONetParticipant_AutoMagicalSyncCompanion_Generated()
        {
            lastKnownValuesChangedArrayPool.Return(lastKnownValueChangesSinceLastCheck);

            for (int i = 0; i < valuesCount; ++i)
            {
                valueChangeSupportArrayPool.Return(valuesChangesSupport[i]); // all of these calls should come prior to returning valuesChangesSupport inside which these object reside currently
            }
            valuesChangesSupportArrayPool.Return(valuesChangesSupport);
        }

        internal abstract void SetAutoMagicalSyncValue(byte index, object value);

        internal abstract object GetAutoMagicalSyncValue(byte index);

        internal abstract void SerializeAll(BitStream bitStream_appendTo);

        internal abstract void SerializeSingle(BitStream bitStream_appendTo, byte singleIndex);

        /// <summary>
        ///  Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// </summary>
        internal abstract void DeserializeInitAll(BitStream bitStream_readFrom);

        /// <summary>
        ///  Deserializes a ginel value (using <paramref name="singleIndex"/> to know which) from <paramref name="bitStream_readFrom"/>
        ///  and uses them to modify appropriate member variables internally.
        /// </summary>
        internal abstract void DeserializeInitSingle(BitStream bitStream_readFrom, byte singleIndex);

        internal abstract void UpdateLastKnownValues();

        internal void AppendListWithChangesSinceLastCheck(List<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend)
        {
            for (int i = 0; i < valuesCount; ++i)
            {
                if (lastKnownValueChangesSinceLastCheck[i])
                {
                    syncValuesToSend.Add(valuesChangesSupport[i]);
                }
            }
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
            throw new System.Exception("You need to run code generation or else the correct generated instance cannot be created");
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
