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

    public class GONetAutoMagicalSyncSettings_ProfileTemplate : ScriptableObject
    {
        [Tooltip("*If checked, any changes to the value will be networked to other/interested parties as soon as GONet can make that happen (i.e., end of frame in which the change occurs).\n*If unchecked, the values of both "+nameof(SyncChangesFrequencyOccurrences)+" (which in this case must be greater than 0) and "+nameof(SyncChangesFrequencyUnitOfTime)+" will be used to determine the frequency at which any changes will be acted upon.")]
        public bool SyncChangesASAP = false;

        [Range(0, 1000)]
        public ushort SyncChangesFrequencyOccurrences = 24;

        public SyncChangesTimeUOM SyncChangesFrequencyUnitOfTime = SyncChangesTimeUOM.TimesPerSecond;

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
        [Tooltip("Only applicable to primitive numeric data types, currently float and Vector2/3/4.\n*MUST be set less than 32.\n*If set to 0, no quantizing will occur.\n*If set to 1, the result will be the quantized value can only represnt " + nameof(QuantizeLowerBound)+" or "+nameof(QuantizeUpperBound)+" and the one represented will be dictated by which of the two the original value is closest to.")]
        [Range(0, 31)]
        public byte QuantizeDownToBitCount = 0;

        /// <summary>
        /// Only used/applied when <see cref="QuantizeDownToBitCount"/> greater than 0.
        ///
        /// This is the known/expected lowest value possible.
        /// IMPORTANT: 
        /// PRE: Must be less than <see cref="QuantizeUpperBound"/>.
        /// </summary>
        [Tooltip("This is the known/expected lowest value possible.\n*Only used/applied when " + nameof(QuantizeDownToBitCount) + " greater than 0.\n*Must be less than " + nameof(QuantizeUpperBound) + ".")]
        //[Range(float.MinValue / 2f, (float.MaxValue / 2f) - 1)]
        public float QuantizeLowerBound = float.MinValue / 2f;

        [Tooltip("This is the known/expected highest value possible.\n*Only used/applied when " + nameof(QuantizeDownToBitCount) + " greater than 0.\n*Must be greater than " + nameof(QuantizeLowerBound) + ".")]
        //[Range((float.MinValue / 2f) + 1, float.MaxValue / 2f)]
        public float QuantizeUpperBound = float.MaxValue / 2f;

        /// <summary>
        /// Helps identify the order in which this single value change will be processed in a group of auto-magical value changes.
        /// Leave this alone for normal priority.
        /// 
        /// NOTE: The higher the number, the higher the priority and the sooner a change of value will be processed in a group of changes being processed at once.
        /// </summary>
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

        [Tooltip("*If this is left empty, the GONet default serialization will be applied to any/all value types associated with this sync template/profile.\n*If this is populated, then any/all value types included herein will have its corresponding custom serializer applied when preparing to send over the network.\n*NOTE: The Custom Serializer Type needs to implement GONet.IGONetAutoMagicalSync_CustomSerializer.")]
        public SyncType_CustomSerializer_Pair[] SyncValueTypeSerializerOverrides;

        /// <summary>
        /// ***Do NOT change this!  Thanks.
        /// --GONet Team
        /// </summary>
        [HideInInspector]
        [Tooltip("***Do NOT change this!  Thanks.\n\n--GONet Team")]
        public GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId ShouldSkipSyncRegistrationId;
    }

    [Serializable]
    public struct SyncType_CustomSerializer_Pair
    {
        public GONetSyncableValueTypes ValueType;

        [Tooltip("NOTE: Any selection needs to implement GONet.IGONetAutoMagicalSync_CustomSerializer.")]
        [ClassImplements(typeof(IGONetAutoMagicalSync_CustomSerializer))]
        public ClassTypeReference CustomSerializerType;
    }
}
