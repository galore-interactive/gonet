using GONet.PluginAPI;
using System;
using TypeReferences;
using UnityEngine;

namespace GONet
{
    public enum SyncChangesTimeUOM : byte
    {
        TimesPerSecond,
        TimesPerMinute,
        TimesPerHour,
        TimesPerDay,
    }

    public class GONetAutoMagicalSyncSettings_ProfileTemplate : ScriptableObject, ISerializationCallbackReceiver
    {
        [Tooltip("*If checked, any changes to the value will be networked to other/interested parties as soon as GONet can make that happen (i.e., end of frame in which the change occurs).\n*If unchecked, the values of both "+nameof(SyncChangesFrequencyOccurrences)+" (which in this case must be greater than 0) and "+nameof(SyncChangesFrequencyUnitOfTime)+" will be used to determine the frequency at which any changes will be acted upon.")]
        public bool SyncChangesASAP = false;

        [Range(0, 1000)]
        public ushort SyncChangesFrequencyOccurrences = 24;

        public SyncChangesTimeUOM SyncChangesFrequencyUnitOfTime = SyncChangesTimeUOM.TimesPerSecond;

        [Space(10)]
        [Tooltip("Indicates the reliability settings to use when sending the changed value across the network to other/interested parties.")]
        public AutoMagicalSyncReliability SendViaReliability = AutoMagicalSyncReliability.Unreliable;

        /// <summary>
        /// Indicates whether or not the receiver of value changes of the property/field should be interpolated or extrapolated between the actual values received.
        /// This is good for smoothing value changes out over time, especially when using a higher value for <see cref="SyncChangesEverySeconds"/> and even moreso 
        /// when <see cref="Reliability"/> is set to <see cref="AutoMagicalSyncReliability.Unreliable"/>.
        /// 
        /// IMPORTANT: This is ONLY applicable for numeric value types (and likely only floats initially during development).
        /// </summary>
        [Tooltip("Indicates whether or not the receiver of value changes of the property/field should be interpolated or extrapolated between the actual values received.\n*This is good for smoothing value changes out over time, especially when using a higher value for "+nameof(SyncChangesFrequencyOccurrences)+" and even moreso when "+nameof(SendViaReliability)+" is set to "+nameof(AutoMagicalSyncReliability.Unreliable)+".\n*IMPORTANT: This is ONLY applicable for numeric value types (and likely only floats initially during development).")]
        public bool ShouldBlendBetweenValuesReceived = false;

        /// <summary>
        /// Only applicable to primitive numeric data types, currently float and Vector2/3/4.
        /// If value is 0, no quantizing will occur; otherwise, value MUST be less than 32.
        /// If value is 1, the result will be the quantized value can only represnt <see cref="QuantizeLowerBound"/> or <see cref="QuantizeUpperBound"/> and the one represented will be dictated by which of the two the original value is closest to.
        /// </summary>
        [Space(10)]
        [Tooltip("Only applicable to primitive numeric data types, currently float and Vector2/3/4.\n*MUST be set less than 32.\n*If set to 0, no quantizing will occur.\n*If set to 1, the result will be the quantized value can only represnt " + nameof(QuantizeLowerBound)+" or "+nameof(QuantizeUpperBound)+" and the one represented will be dictated by which of the two the original value is closest to.")]
        [Range(0, 31)]
        public byte QuantizeDownToBitCount = 0;

        /// <summary>
        /// Only used/applied when <see cref="QuantizeDownToBitCount"/> greater than 0.
        ///
        /// This is the known/expected lowest value to likely be encountered.  HOWEVER, there is a really neat GONet feature
        /// (auto adjusted baseline value - see <see cref="GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.baselineValue_current"/>)
        /// that essentially allows defining a (potentially) small value range with this and <see cref="QuantizeUpperBound"/> and the end 
        /// result is actually a semi-infinite acceptable values that can go anywhere outside the bounds defined here.  Sounds crazy, but it was an afterthought, post v1.0,
        /// that turns these bounds into a range that dictates when to auto adjust the baseline value (i.e., a mandatory reliable message to all parties).
        /// 
        /// IMPORTANT: 
        /// PRE: Must be less than <see cref="QuantizeUpperBound"/>.
        /// 
        /// WARNING: 
        /// Do not make the range smaller than the expected rate of change of the values to which this will apply or else the 'auto adjust'
        /// baseline logic will be applied every value change and that will yield bad looking results for non-owners!
        /// </summary>
        [Tooltip("This is the known/expected lowest value to likely be encountered.  HOWEVER, there is a really neat GONet feature (auto adjusted baseline value - see "+nameof(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue) +"." + nameof(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.baselineValue_current) + ") that essentially allows defining a (potentially) small value range with this and " + nameof(QuantizeUpperBound) + " and the end result is actually a semi-infinite acceptable values that can go anywhere outside the bounds defined here.  Sounds crazy, but it was an afterthought, post v1.0, that turns these bounds into a range that dictates when to auto adjust the baseline value (i.e., a mandatory reliable message to all parties).\n*Only used/applied when " + nameof(QuantizeDownToBitCount) + " greater than 0.\n*Must be less than " + nameof(QuantizeUpperBound) + ".\n\nWARNING: Do not make the range smaller than the expected rate of change of the values to which this will apply or else the 'auto adjust' baseline logic will be applied every value change and that will yield bad looking results for non-owners!")]
        //[Range(float.MinValue / 2f, (float.MaxValue / 2f) - 1)]
        public float QuantizeLowerBound = float.MinValue / 2f;

        /// <summary>
        /// Only used/applied when <see cref="QuantizeDownToBitCount"/> greater than 0.
        ///
        /// This is the known/expected highest value to likely be encountered.  HOWEVER, there is a really neat GONet feature
        /// (auto adjusted baseline value - see <see cref="GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.baselineValue_current"/>)
        /// that essentially allows defining a (potentially) small value range with this and <see cref="QuantizeLowerBound"/> and the end 
        /// result is actually a semi-infinite acceptable values that can go anywhere outside the bounds defined here.  Sounds crazy, but it was an afterthought, post v1.0,
        /// that turns these bounds into a range that dictates when to auto adjust the baseline value (i.e., a mandatory reliable message to all parties).
        /// 
        /// IMPORTANT: 
        /// PRE: Must be greater than <see cref="QuantizeLowerBound"/>.
        /// 
        /// WARNING: 
        /// Do not make the range smaller than the expected rate of change of the values to which this will apply or else the 'auto adjust'
        /// baseline value logic will be applied every value change and that will yield bad looking results for non-owners!
        /// </summary>
        [Tooltip("This is the known/expected highest value to likely be encountered.  HOWEVER, there is a really neat GONet feature (auto adjusted baseline value - see " + nameof(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue) + "." + nameof(GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.baselineValue_current) + ") that essentially allows defining a (potentially) small value range with this and " + nameof(QuantizeLowerBound) + " and the end result is actually a semi-infinite acceptable values that can go anywhere outside the bounds defined here.  Sounds crazy, but it was an afterthought, post v1.0, that turns these bounds into a range that dictates when to auto adjust the baseline value (i.e., a mandatory reliable message to all parties).\n*Only used/applied when " + nameof(QuantizeDownToBitCount) + " greater than 0.\n*Must be less than " + nameof(QuantizeLowerBound) + ".\n\nWARNING: Do not make the range smaller than the expected rate of change of the values to which this will apply or else the 'auto adjust' baseline logic will be applied every value change and that will yield bad looking results for non-owners!")]
        //[Range((float.MinValue / 2f) + 1, float.MaxValue / 2f)]
        public float QuantizeUpperBound = float.MaxValue / 2f;

        /// <summary>
        /// This is a very helpful READONLY readout of what the above quantization settings will yield for the resulting precision.
        /// This value is the same unit of measure of whatever is source value being quantized represents.
        /// For example: a component of a position would be meters, which is common and that why we put the more useful millimeters
        /// conversion in parenthesis for convenience (in which case sub-millimeter precision is great).
        /// 
        /// IMPORTANT: If there is a custom serializer associated with certain Value Type(s), it is possible these quantization settings are ignored (see <see cref="SyncValueTypeSerializerOverrides"/>).
        /// </summary>
        [Tooltip("This is a very helpful READONLY readout of what the above quantization settings will yield for the resulting precision.\nThis value is the same unit of measure of whatever the source value being quantized represents.\nFor example: a component of a position would be meters, which is common and that's why we put the more useful millimeters conversion in parenthesis for convenience (in which case sub-millimeter precision is great).\n\nIMPORTANT: If there is a custom serializer associated with certain Value Type(s), it is possible these quantization settings are ignored (see 'Sync Value Type Serializer Overrides' section).")]
        public string QuantizationResultingPrecision;

        /// <summary>
        /// Helps identify the order in which this single value change will be processed in a group of auto-magical value changes.
        /// Leave this alone for normal priority.
        /// 
        /// NOTE: The higher the number, the higher the priority and the sooner a change of value will be processed in a group of changes being processed at once.
        /// </summary>
        [Space(10)]
        [Tooltip("Helps identify the order in which this single value change will be processed in a group of auto-magical value changes.\n*Leave this alone for normal priority.\n*The higher the number, the higher the priority and the sooner a change of value will be processed in a group of changes being processed at once.")]
        [Range(-255, 255)]
        public int ProcessingPriority = 0;

        /// <summary>
        /// GONet optimizes processing by using multiple threads (as possible) when processing value sync'ing.
        /// Some things just cannot be done outside the main unity thread.
        /// Therefore, if you know for certain that the value to sync being decorated with this attribute cannot
        /// run outside unity main thread, set this to true and GONet will ensure it is so.
        /// </summary>
        [Tooltip("GONet optimizes processing by using multiple threads (as possible) when processing value sync'ing.\nSome things just cannot be done outside the main Unity thread.\nTherefore, if you know for certain that the value to sync being decorated with this attribute cannot run outside unity main thread, set this to true and GONet will ensure it is so.")]
        public bool MustRunOnUnityMainThread = false;

        [Space(10)]
        [Tooltip("*If this is left empty, the GONet default serialization will be applied to any/all value types associated with this sync template/profile.\n*If this is populated, then any/all value types included herein will have its corresponding custom serializer applied when preparing to send over the network.\n*NOTE: The Custom Serializer Type needs to be public and implement GONet.IGONetAutoMagicalSync_CustomSerializer.\n*WARNING: Do NOT have multiple entries for the same GONet Value Type or else GONet will get confused.")]
        public SyncType_CustomSerializer_Pair[] SyncValueTypeSerializerOverrides;

        [Tooltip("*If this is left empty, the GONet default value blending will be applied to any/all value types associated with this sync template/profile.\n*If this is populated, then any/all value types included herein will have its corresponding custom value blending technique applied on non-owners of data when determining current value between values auto-magically sync'd from the owner at the configured regular frequency.\n*NOTE: The implementation Type needs to be public and implement GONet.PluginAPI.IGONetAutoMagicalSync_CustomValueBlending.\n*WARNING: Do NOT have multiple entries for the same GONet Value Type or else GONet will get confused.")]
        public SyncType_CustomValueBlending_Pair[] SyncValueTypeValueBlendingOverrides;

        /// <summary>
        /// ***Do NOT change this!  Thanks.
        /// --GONet Team
        /// </summary>
        [HideInInspector]
        [Tooltip("***Do NOT change this!  Thanks.\n\n--GONet Team")]
        public GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId ShouldSkipSyncRegistrationId;

        public void OnAfterDeserialize()
        {
            UpdateResultingPrecision();
        }

        public void OnBeforeSerialize()
        {
            UpdateResultingPrecision();
        }

        private void UpdateResultingPrecision()
        {
            if (QuantizeDownToBitCount == 0)
            {
                QuantizationResultingPrecision = "<full precision - no quantization will occur>";
            }
            else
            {
                float range = QuantizeUpperBound - QuantizeLowerBound;
                float precision = range / (float)Math.Pow(2.0, QuantizeDownToBitCount);
                QuantizationResultingPrecision = string.Concat(precision.ToString(), " (i.e., ", string.Format("{0:##,##0.###}", precision * 1000f), " millimeters)");
            }
        }
    }

    [Serializable]
    public struct SyncType_CustomSerializer_Pair
    {
        public GONetSyncableValueTypes ValueType;

        [Tooltip("NOTE: Any selection needs to be public and implement GONet.IGONetAutoMagicalSync_CustomSerializer.")]
        [ClassImplements(typeof(IGONetAutoMagicalSync_CustomSerializer))]
        public ClassTypeReference CustomSerializerType;
    }

    [Serializable]
    public struct SyncType_CustomValueBlending_Pair
    {
        public GONetSyncableValueTypes ValueType;

        [Tooltip("NOTE: Any selection needs to be public and implement GONet.PluginAPI.IGONetAutoMagicalSync_CustomValueBlending.")]
        [ClassImplements(typeof(IGONetAutoMagicalSync_CustomValueBlending))]
        public ClassTypeReference CustomValueBlendingType;
    }
}
