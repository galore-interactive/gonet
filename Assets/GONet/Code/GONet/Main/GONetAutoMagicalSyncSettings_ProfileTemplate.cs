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

        #region Velocity Quantization Settings (NEW - Velocity Augmented Sync System)

        /// <summary>
        /// Enable velocity-augmented sync for this value. When enabled, GONet will alternate between sending
        /// VALUE packets (quantized positions) and VELOCITY packets (rates of change) to eliminate sub-quantization jitter.
        ///
        /// IMPORTANT: Only enable for values that change smoothly and predictably (positions, rotations).
        /// DO NOT enable for discrete/step-change values (health, ammo count, etc.).
        ///
        /// RECOMMENDED: Enable for Transform.position/rotation on moving objects for smoother interpolation.
        /// </summary>
        [Space(10)]
        [Header("Velocity-Augmented Sync (Eliminates Sub-Quantization Jitter)")]
        [Tooltip("Enable velocity-augmented sync to eliminate sub-quantization jitter on moving objects.\n\n" +
                 "When enabled, GONet alternates between VALUE packets (positions) and VELOCITY packets (rates of change).\n\n" +
                 "IMPORTANT: Only enable for smooth/predictable values (positions, rotations).\n" +
                 "DO NOT enable for discrete values (health, ammo, etc.).\n\n" +
                 "RECOMMENDED: Enable for Transform.position/rotation on moving objects.")]
        public bool IsVelocityEligible = false;

        /// <summary>
        /// VELOCITY-AUGMENTED SYNC: If true, velocity quantization bounds are calculated automatically
        /// from VALUE quantization settings. If false, uses manually configured VelocityQuantize* settings below.
        ///
        /// AUTO-CALCULATION (default, recommended):
        /// - Calculates velocity bounds from VALUE precision / sync interval
        /// - Ensures VELOCITY bundles are sent only for sub-quantization motion
        /// - Bit count matches VALUE bit count for consistency
        /// - VelocityQuantize* fields below are IGNORED when this is true
        ///
        /// MANUAL CONFIGURATION (advanced):
        /// - Set to false to use custom VelocityQuantize* settings below
        /// - Useful for fine-tuning bandwidth vs quality trade-offs
        /// - Requires understanding of quantization math
        ///
        /// Default: true (auto-calculate)
        /// </summary>
        [Tooltip("AUTO-CALCULATE (default): Velocity bounds calculated from VALUE precision / sync interval.\n" +
                 "VelocityQuantize* fields below are IGNORED when enabled.\n\n" +
                 "MANUAL CONFIG (advanced): Uncheck to use custom VelocityQuantize* settings below.\n" +
                 "Requires understanding of quantization math.")]
        public bool AutoCalculateVelocityQuantization = true;

        /// <summary>
        /// Velocity/delta quantization lower bound. Used when alternating value/velocity packets enabled.
        /// Represents expected minimum rate of change (units per second).
        ///
        /// Only applies when <see cref="IsVelocityEligible"/> is true.
        ///
        /// RECOMMENDED: Set to negative of expected maximum velocity (e.g., -20 for objects moving up to 20 units/sec)
        /// </summary>
        [Tooltip("Lower bound for velocity/delta quantization when alternating value/velocity packets enabled.\n" +
                 "Represents expected minimum rate of change (units per second).\n\n" +
                 "Only applies when 'Is Velocity Eligible' is enabled.\n\n" +
                 "RECOMMENDED: Set to negative of expected maximum velocity\n" +
                 "Example: -20 for objects moving up to 20 units/sec")]
        public float VelocityQuantizeLowerBound = -20f;

        /// <summary>
        /// Velocity/delta quantization upper bound. Used when alternating value/velocity packets enabled.
        /// Represents expected maximum rate of change (units per second).
        ///
        /// Only applies when <see cref="IsVelocityEligible"/> is true.
        ///
        /// RECOMMENDED: Set to expected maximum velocity (e.g., 20 for objects moving up to 20 units/sec)
        /// </summary>
        [Tooltip("Upper bound for velocity/delta quantization when alternating value/velocity packets enabled.\n" +
                 "Represents expected maximum rate of change (units per second).\n\n" +
                 "Only applies when 'Is Velocity Eligible' is enabled.\n\n" +
                 "RECOMMENDED: Set to expected maximum velocity\n" +
                 "Example: 20 for objects moving up to 20 units/sec")]
        public float VelocityQuantizeUpperBound = 20f;

        /// <summary>
        /// Bit count for velocity quantization. Can often be lower than position bit count since velocity
        /// range is typically smaller and more predictable than position range.
        ///
        /// Only applies when <see cref="IsVelocityEligible"/> is true.
        ///
        /// RECOMMENDED: 8-10 bits for most use cases (provides 0.039-0.156 units/sec resolution for ±20 units/sec range)
        /// </summary>
        [Tooltip("Bit count for velocity quantization. Can be lower than position bits since velocity range is typically smaller.\n\n" +
                 "Only applies when 'Is Velocity Eligible' is enabled.\n\n" +
                 "RECOMMENDED: 8-10 bits for most use cases\n" +
                 "8 bits = 0.156 units/sec resolution (256 values)\n" +
                 "10 bits = 0.039 units/sec resolution (1024 values)")]
        [Range(1, 31)]
        public byte VelocityQuantizeDownToBitCount = 10;

        /// <summary>
        /// READONLY: Velocity quantization resulting precision (calculated from above settings)
        /// </summary>
        [Tooltip("READONLY: Velocity quantization precision (calculated from above settings)")]
        public string VelocityQuantizationResultingPrecision;

        #endregion

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

        /// <summary>
        /// SERVER PHYSICS SYNC ONLY: How often to sync physics state (position/rotation from Rigidbody) for objects using this profile.
        /// Only applies to GONetParticipants with Rigidbody components and IsRigidBodyOwnerOnlyControlled enabled.
        ///
        /// Value of 1 = sync every FixedUpdate (60 Hz at 0.0167s timestep)
        /// Value of 2 = sync every 2nd FixedUpdate (30 Hz)
        /// Value of 3 = sync every 3rd FixedUpdate (20 Hz)
        /// Value of 4 = sync every 4th FixedUpdate (15 Hz)
        ///
        /// PERFORMANCE: Higher values reduce bandwidth and CPU overhead but increase interpolation lag on clients.
        /// RECOMMENDED: 1 for fast-moving objects (projectiles), 2-3 for normal physics objects, 4 for slow/static objects.
        /// </summary>
        [Space(10)]
        [Tooltip("SERVER PHYSICS SYNC: How often to sync physics state for Rigidbody objects using this profile.\n\n" +
                 "1 = Every FixedUpdate (60 Hz) - Best for projectiles/fast objects\n" +
                 "2 = Every 2nd FixedUpdate (30 Hz) - Good for normal physics\n" +
                 "3 = Every 3rd FixedUpdate (20 Hz) - Balanced performance\n" +
                 "4 = Every 4th FixedUpdate (15 Hz) - Best for slow/static objects\n\n" +
                 "Only applies to GONetParticipants with IsRigidBodyOwnerOnlyControlled enabled.\n" +
                 "Higher values reduce bandwidth but increase interpolation lag.")]
        [Range(1, 4)]
        public int PhysicsUpdateInterval = 1;

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
            // Position quantization precision
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

            // Velocity quantization precision
            if (VelocityQuantizeDownToBitCount == 0)
            {
                VelocityQuantizationResultingPrecision = "<full precision - no quantization will occur>";
            }
            else
            {
                float velocityRange = VelocityQuantizeUpperBound - VelocityQuantizeLowerBound;
                float velocityPrecision = velocityRange / (float)Math.Pow(2.0, VelocityQuantizeDownToBitCount);
                VelocityQuantizationResultingPrecision = string.Concat(velocityPrecision.ToString("F6"), " units/sec");
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
