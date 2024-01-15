using GONet.PluginAPI;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet.Utils
{
    [TestFixture]
    public class ValueBlendUtilsTests
    {
        [Test]
        public void BlendExtrapolatedQuaternionsSmoothly()
        {
            GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue support4 = new GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue();
            Quaternion startingRotation = Quaternion.Euler(90, 0, 0);
            Quaternion degrees5 = Quaternion.Euler(5, 0, 0);
            Quaternion degrees10 = Quaternion.Euler(10, 0, 0);
            Quaternion degrees15 = Quaternion.Euler(15, 0, 0);


            {  // NOTE: taken  from GONetParticipant_AutoMagicalSyncCompanion_Generated_1
                support4.baselineValue_current.UnityEngine_Quaternion = startingRotation;
                support4.lastKnownValue.UnityEngine_Quaternion = startingRotation;
                support4.lastKnownValue_previous.UnityEngine_Quaternion = startingRotation;
                support4.valueLimitEncountered_min.UnityEngine_Quaternion = startingRotation;
                support4.valueLimitEncountered_max.UnityEngine_Quaternion = startingRotation;
                support4.syncCompanion = null; // not needed for test so null is OK
                support4.memberName = "rotation";
                support4.index = 4;
                support4.syncAttribute_MustRunOnUnityMainThread = true;
                support4.syncAttribute_ProcessingPriority = 0;
                support4.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
                support4.syncAttribute_SyncChangesEverySeconds = 0.05f;
                support4.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
                support4.syncAttribute_ShouldBlendBetweenValuesReceived = true;
                GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((0, 1), out support4.syncAttribute_ShouldSkipSync);
                support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

                // cachedCustomSerializers[4] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);

                int support4_mostRecentChanges_calcdSize = support4.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support4.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
                support4.mostRecentChanges_capacitySize = Math.Max(support4_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
                support4.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support4.mostRecentChanges_capacitySize);
            }

            support4.mostRecentChanges_usedSize = 4;

            DateTime oldestTime = DateTime.Today;

            NumericValueChangeSnapshot value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.018).Ticks;
            value.numericValue = startingRotation * degrees15;
            support4.mostRecentChanges[0] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.015).Ticks;
            value.numericValue = startingRotation * degrees10;
            support4.mostRecentChanges[1] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.AddSeconds(0.01).Ticks;
            value.numericValue = startingRotation * degrees5;
            support4.mostRecentChanges[2] = value;

            value = new NumericValueChangeSnapshot();
            value.elapsedTicksAtChange = oldestTime.Ticks;
            value.numericValue = startingRotation;
            support4.mostRecentChanges[3] = value;

            GONetSyncableValue blendedValue;
            bool isGrande = ValueBlendUtils.TryGetBlendedValue(support4, oldestTime.AddSeconds(0.0195).Ticks, out blendedValue, out bool didExtrapolate);

            Debug.Log("blendedValue: " + blendedValue.UnityEngine_Quaternion.eulerAngles);
        }
    }
}
