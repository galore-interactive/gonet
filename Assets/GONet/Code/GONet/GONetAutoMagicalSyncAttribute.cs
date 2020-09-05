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

using GONet.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GONet
{
    public enum GONetSyncableValueTypes : byte
    {
        System_Boolean,
        System_Byte,
        System_SByte,
        System_Int16,
        System_UInt16,
        System_Int32,
        System_UInt32,
        System_Int64,
        System_UInt64,
        System_Single,
        System_Double,
        // ?? TODO reference type support: System_String,
        UnityEngine_Vector2,
        UnityEngine_Vector3,
        UnityEngine_Vector4,
        UnityEngine_Quaternion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GONetSyncableValue
    {
        [FieldOffset(0)] public GONetSyncableValueTypes GONetSyncType;

        [FieldOffset(1)] bool system_Boolean;
        public bool System_Boolean { get => system_Boolean; set { system_Boolean = value; GONetSyncType = GONetSyncableValueTypes.System_Boolean; } }

        [FieldOffset(1)] byte system_Byte;
        public byte System_Byte { get => system_Byte; set { system_Byte = value; GONetSyncType = GONetSyncableValueTypes.System_Byte; } }

        [FieldOffset(1)] sbyte system_SByte;
        public sbyte System_SByte { get => system_SByte; set { system_SByte = value; GONetSyncType = GONetSyncableValueTypes.System_SByte; } }

        [FieldOffset(1)] short system_Int16;
        public short System_Int16 { get => system_Int16; set { system_Int16 = value; GONetSyncType = GONetSyncableValueTypes.System_Int16; } }

        [FieldOffset(1)] ushort system_UInt16;
        public ushort System_UInt16 { get => system_UInt16; set { system_UInt16 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt16; } }

        [FieldOffset(1)] int system_Int32;
        public int System_Int32 { get => system_Int32; set { system_Int32 = value; GONetSyncType = GONetSyncableValueTypes.System_Int32; } }

        [FieldOffset(1)] uint system_UInt32;
        public uint System_UInt32 { get => system_UInt32; set { system_UInt32 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt32; } }

        [FieldOffset(1)] long system_Int64;
        public long System_Int64 { get => system_Int64; set { system_Int64 = value; GONetSyncType = GONetSyncableValueTypes.System_Int64; } }

        [FieldOffset(1)] ulong system_UInt64;
        public ulong System_UInt64 { get => system_UInt64; set { system_UInt64 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt64; } }

        [FieldOffset(1)] float system_Single;
        public float System_Single { get => system_Single; set { system_Single = value; GONetSyncType = GONetSyncableValueTypes.System_Single; } }

        [FieldOffset(1)] double system_Double;
        public double System_Double { get => system_Double; set { system_Double = value; GONetSyncType = GONetSyncableValueTypes.System_Double; } }

        [FieldOffset(1)] Vector2 unityEngine_Vector2;
        public Vector2 UnityEngine_Vector2 { get => unityEngine_Vector2; set { unityEngine_Vector2 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector2; } }

        [FieldOffset(1)] Vector3 unityEngine_Vector3;
        public Vector3 UnityEngine_Vector3 { get => unityEngine_Vector3; set { unityEngine_Vector3 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector3; } }

        [FieldOffset(1)] Vector4 unityEngine_Vector4;
        public Vector4 UnityEngine_Vector4 { get => unityEngine_Vector4; set { unityEngine_Vector4 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector4; } }

        [FieldOffset(1)] Quaternion unityEngine_Quaternion;
        public Quaternion UnityEngine_Quaternion { get => unityEngine_Quaternion; set { unityEngine_Quaternion = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Quaternion; } }

        #region operators == and !=

        public static bool operator ==(GONetSyncableValue left, GONetSyncableValue right)
        {
            bool areValuesEqual = false;
            switch (left.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: areValuesEqual = left.system_Boolean == right.system_Boolean; break;
                case GONetSyncableValueTypes.System_Byte: areValuesEqual = left.system_Byte == right.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: areValuesEqual = left.system_Double == right.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: areValuesEqual = left.system_Int16 == right.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: areValuesEqual = left.system_Int32 == right.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: areValuesEqual = left.system_Int64 == right.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: areValuesEqual = left.system_SByte == right.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: areValuesEqual = left.system_Single == right.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: areValuesEqual = left.system_UInt16 == right.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: areValuesEqual = left.system_UInt32 == right.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: areValuesEqual = left.system_UInt64 == right.system_UInt64; break;
                case GONetSyncableValueTypes.UnityEngine_Quaternion: areValuesEqual = left.unityEngine_Quaternion.eulerAngles == right.unityEngine_Quaternion.eulerAngles; break;
                case GONetSyncableValueTypes.UnityEngine_Vector2: areValuesEqual = left.unityEngine_Vector2 == right.unityEngine_Vector2; break;
                case GONetSyncableValueTypes.UnityEngine_Vector3: areValuesEqual = left.unityEngine_Vector3 == right.unityEngine_Vector3; break;
                case GONetSyncableValueTypes.UnityEngine_Vector4: areValuesEqual = left.unityEngine_Vector4 == right.unityEngine_Vector4; break;
            }

            return left.GONetSyncType == right.GONetSyncType && areValuesEqual;
        }

        public static bool operator !=(GONetSyncableValue left, GONetSyncableValue right)
        {
            bool areValuesEqual = false;
            switch (left.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: areValuesEqual = left.system_Boolean == right.system_Boolean; break;
                case GONetSyncableValueTypes.System_Byte: areValuesEqual = left.system_Byte == right.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: areValuesEqual = left.system_Double == right.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: areValuesEqual = left.system_Int16 == right.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: areValuesEqual = left.system_Int32 == right.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: areValuesEqual = left.system_Int64 == right.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: areValuesEqual = left.system_SByte == right.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: areValuesEqual = left.system_Single == right.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: areValuesEqual = left.system_UInt16 == right.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: areValuesEqual = left.system_UInt32 == right.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: areValuesEqual = left.system_UInt64 == right.system_UInt64; break;
                case GONetSyncableValueTypes.UnityEngine_Quaternion: areValuesEqual = left.unityEngine_Quaternion == right.unityEngine_Quaternion; break;
                case GONetSyncableValueTypes.UnityEngine_Vector2: areValuesEqual = left.unityEngine_Vector2 == right.unityEngine_Vector2; break;
                case GONetSyncableValueTypes.UnityEngine_Vector3: areValuesEqual = left.unityEngine_Vector3 == right.unityEngine_Vector3; break;
                case GONetSyncableValueTypes.UnityEngine_Vector4: areValuesEqual = left.unityEngine_Vector4 == right.unityEngine_Vector4; break;
            }

            return left.GONetSyncType != right.GONetSyncType || !areValuesEqual;
        }

        #endregion

        #region implicit conversions from primitive values wrapped herein

        public static implicit operator GONetSyncableValue(bool value) { return new GONetSyncableValue() { System_Boolean = value }; }
        public static implicit operator GONetSyncableValue(byte value) { return new GONetSyncableValue() { System_Byte = value }; }
        public static implicit operator GONetSyncableValue(double value) { return new GONetSyncableValue() { System_Double = value }; }
        public static implicit operator GONetSyncableValue(short value) { return new GONetSyncableValue() { System_Int16 = value }; }
        public static implicit operator GONetSyncableValue(int value) { return new GONetSyncableValue() { System_Int32 = value }; }
        public static implicit operator GONetSyncableValue(long value) { return new GONetSyncableValue() { System_Int64 = value }; }
        public static implicit operator GONetSyncableValue(sbyte value) { return new GONetSyncableValue() { System_SByte = value }; }
        public static implicit operator GONetSyncableValue(float value) { return new GONetSyncableValue() { System_Single = value }; }
        public static implicit operator GONetSyncableValue(ushort value) { return new GONetSyncableValue() { System_UInt16 = value }; }
        public static implicit operator GONetSyncableValue(uint value) { return new GONetSyncableValue() { System_UInt32 = value }; }
        public static implicit operator GONetSyncableValue(ulong value) { return new GONetSyncableValue() { System_UInt64 = value }; }
        public static implicit operator GONetSyncableValue(Quaternion value) { return new GONetSyncableValue() { UnityEngine_Quaternion = value }; }
        public static implicit operator GONetSyncableValue(Vector2 value) { return new GONetSyncableValue() { UnityEngine_Vector2 = value }; }
        public static implicit operator GONetSyncableValue(Vector3 value) { return new GONetSyncableValue() { UnityEngine_Vector3 = value }; }
        public static implicit operator GONetSyncableValue(Vector4 value) { return new GONetSyncableValue() { UnityEngine_Vector4 = value }; }

        #endregion
    }

    /// <summary>
    /// This class only holds constant value fields that can be used as <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/> values.
    /// </summary>
    public static class AutoMagicalSyncFrequencies
    {
        /// <summary>
        /// Use this if every change should be processed at the end of the game update frame in which the change occurred.
        /// This is the fastest GONet can deliver results. 
        /// Try not to use this for everything or else the network performance will be negatively affected with what is likely traffic that is not all vital.
        /// </summary>
        public const float END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS = 0f;

        /// <summary>
        /// This is a great default value for most data items that is not considered absolutely vital to arrive ASAP.
        /// </summary>
        public const float _24_Hz = 1f / 24f;
    }

    public enum AutoMagicalSyncReliability : byte
    {
        Reliable,

        Unreliable
    }

    /// <summary>
    /// <para>
    /// IMPORTANT: Ensure this is placed on a public field/property on a <see cref="MonoBehaviour"/>
    ///            and the aforementioned <see cref="MonoBehaviour"/> is placed on a 
    ///            <see cref="GameObject"/> with a <see cref="GONetParticipant"/>
    ///            placed on it as well!
    /// </para>
    /// <para>
    /// IMPORTANT: There is a hard GONet limit of 256 of these maximum for a single <see cref="GONetParticipant"/> and that is collectively
    ///            from *ALL* the <see cref="MonoBehaviour"/> instances installed on the same <see cref="GameObject"/> as the GNP.
    /// </para>
    /// <para>
    /// This is akin to [SyncVar] from old/legacy unity networking.....change sent at end of frame; HOWEVER, changes are
    /// monitored on both server (like [SyncVar]) ***AND*** client (UNlike [SyncVar]).
    /// </para>
    /// <para>
    /// See <see cref="GONetSyncableValueTypes"/> for a list of supported types this attribute can be placed on.
    /// </para>
    /// <para>
    /// Also, as a reminder, here is the Unity 2017.4 documentation for SyncVar (taken from: https://docs.unity3d.com/2017.4/Documentation/ScriptReference/Networking.SyncVarAttribute.html):
    ///     [SyncVar] is an attribute that can be put on member variables of NetworkBehaviour classes. These variables will
    ///     have their values sychronized from the server to clients in the game that are in the ready state.
    ///     Setting the value of a[SyncVar] marks it as dirty, so it will be sent to clients at the end of the current frame.
    ///     Only simple values can be marked as [SyncVars]. The type of the SyncVar variable cannot be from an external DLL or assembly.
    ///     The allowed SyncVar types are;
    ///     • Basic type(byte, int, float, string, UInt64, etc)
    ///     • Built-in Unity math type(Vector3, Quaternion, etc), 
    ///     • Structs containing allowable types.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GONetAutoMagicalSyncAttribute : Attribute
    {
        public override object TypeId => base.TypeId;

        /// <summary>
        /// This is a special value that indicates to GONet internals to take the values directly off the properties in the/this attribute instance
        /// instead of from the corresponding <see cref="GONetAutoMagicalSyncSettings_ProfileTemplate"/> asset of the matching name provided in this value.
        /// </summary>
        public const string PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY = "";
        public const string PROFILE_TEMPLATE_NAME___DEFAULT = "__GONet_DEFAULT";
        public const string PROFILE_TEMPLATE_NAME___TRANSFORM_ROTATION = "_GONet_Transform_Rotation";
        public const string PROFILE_TEMPLATE_NAME___TRANSFORM_POSITION = "_GONet_Transform_Position";
        public const string PROFILE_TEMPLATE_NAME___ANIMATOR_CONTROLLER_PARAMETERS = "_GONet_Animator_Controller_Parameters";

        /// <summary>
        /// <para>This is the main way in which the runtime settings (represented by the placement of an instance of <see cref="GONetAutoMagicalSyncAttribute"/> on <see cref="MonoBehaviour"/> fields for syncing data) are derived/looked up.</para>
        /// <para>This is the 1-1 direct correlation with the name of one of the <see cref="GONetAutoMagicalSyncSettings_ProfileTemplate"/> asset instances in the "Resources/GONet/SyncSettingsProfiles" directory in your project (without the ".asset" file extension included).</para>
        /// <para>Check out the public const string definitions on <see cref="GONetAutoMagicalSyncAttribute"/> that start with the prefix "PROFILE_TEMPLATE_NAME___".</para>
        /// <para>IMPORTANT: If this is provided, the settings defined on the corresponding profile/template will be used and NOT the settings on the attribute!</para>
        /// </summary>
        public string SettingsProfileTemplateName = PROFILE_TEMPLATE_NAME___DEFAULT;

        /// <summary>
        /// GONet optimizes processing by using multiple threads (as possible) when processing value sync'ing.
        /// Some things just cannot be done outside the main unity thread.
        /// Therefore, if you know for certain that the value to sync being decorated with this attribute cannot
        /// run outside unity main thread, set this to true and GONet will ensure it is so.
        /// </summary>
        public bool MustRunOnUnityMainThread = false;

        /// <summary>
        /// How often (in seconds) the system will check the field/property value for a change and send it across the network if it did change.
        /// <see cref="AutoMagicalSyncFrequencies"/> for some standard options to use here.
        /// </summary>
        public float SyncChangesEverySeconds = AutoMagicalSyncFrequencies._24_Hz;

        public AutoMagicalSyncReliability Reliability = AutoMagicalSyncReliability.Reliable;

        /// <summary>
        /// Indicates whether or not the receiver of value changes of the property/field should be interpolated or extrapolated between the actual values received.
        /// This is good for smoothing value changes out over time, especially when using a higher value for <see cref="SyncChangesEverySeconds"/> and even moreso 
        /// when <see cref="Reliability"/> is set to <see cref="AutoMagicalSyncReliability.Unreliable"/>.
        /// 
        /// IMPORTANT: This is ONLY applicable for numeric value types (and likely only floats initially during development).
        /// </summary>
        public bool ShouldBlendBetweenValuesReceived = false;

        /// <summary>
        /// Helps identify the order in which this single value change will be processed in a group of auto-magical value changes.
        /// Leave this alone for normal priority.
        /// 
        /// NOTE: The higher the number, the higher the priority and the sooner a change of value will be processed in a group of changes being processed at once.
        /// </summary>
        public int ProcessingPriority = 0;

        public bool IsReadonlyInEditor = false;

        /// <summary>
        /// Only applicable to primitive numeric data types, currently float and Vector2/3/4.
        /// If value is 0, no quantizing will occur; otherwise, value MUST be less than 32.
        /// If value is 1, the result will be the quantized value can only represnt <see cref="QuantizeLowerBound"/> or <see cref="QuantizeUpperBound"/> and the one represented will be dictated by which of the two the original value is closest to.
        /// </summary>
        public byte QuantizeDownToBitCount = 0;

        /// <summary>
        /// Only used/applied when <see cref="QuantizeDownToBitCount"/> greater than 0.
        ///
        /// This is the known/expected lowest value possible.
        /// IMPORTANT: 
        /// PRE: Must be less than <see cref="QuantizeUpperBound"/>.
        /// </summary>
        public float QuantizeLowerBound = float.MinValue / 2f;
        public float QuantizeUpperBound = float.MaxValue / 2f;

        #region GONet internal only

        /// <summary>
        /// DO NOT use this!  It is used internally within GONet only.
        /// If you use this, results of entire GONet could be compromised and unpredictable....why would you want that?
        /// </summary>
        public int ProcessingPriority_GONetInternalOverride = 0;

        internal static readonly Dictionary<int, Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool>> ShouldSkipSyncByRegistrationIdMap = new Dictionary<int, Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool>>(2);
        /// <summary>
        /// public: Do NOT use this!
        /// TODO make this internal..only public due to reference inside editor-based code gen tt
        /// </summary>
        public int ShouldSkipSync_RegistrationId;

        /// <summary>
        /// TODO make this internal once internals visible updated
        /// public: do NOT use this
        /// </summary>
        public enum ShouldSkipSyncRegistrationId : int
        {
            // IMPORTANT: Leave 0 empty
            GONetParticipant_IsRotationSyncd = 1,
            GONetParticipant_IsPositionSyncd,
        }

        #endregion

        /// <summary>
        /// OPTIONAL.
        /// You can use this when there is a special case for serialization (e.g., when not blittable or not GONet natively support type).
        /// IMPORTANT: Due to C# attribute parameter type limitations (see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes),
        ///            the type here is <see cref="Type"/> when it really needs to be an instance of <see cref="IGONetAutoMagicalSync_CustomSerializer"/>.
        ///            An instance of this type will be created and cast to <see cref="IGONetAutoMagicalSync_CustomSerializer"/> and 
        ///            set on <see cref="CustomSerialize_Instance"/> and that is what will be used.
        /// </summary>
        public Type CustomSerialize_Type = null;

        /// <summary>
        /// Only here to support serialization of <see cref="CustomSerialize_Type"/>
        /// </summary>
        public string CustomSerialize_Type_AsString
        {
            get { return CustomSerialize_Type != null ? CustomSerialize_Type.AssemblyQualifiedName : null; }

            set
            {
                if (value == null)
                {
                    CustomSerialize_Type = null;
                }
                else
                {
                    CustomSerialize_Type = Type.GetType(value);
                }
            }
        }

        internal struct CustomSerializerLookupKey : IEquatable<CustomSerializerLookupKey>
        {
            internal Type serializedType;

            //{ // what we want, but in this block is what is easiest for now: QuantizerSettingsGroup quantizationSettings;
            internal byte quantizeDownToBitCount;
            internal float quantizeLowerBound;
            internal float quantizeUpperBound;
            //}

            internal CustomSerializerLookupKey(Type serializedType, byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
            {
                this.serializedType = serializedType;
                this.quantizeDownToBitCount = quantizeDownToBitCount;
                this.quantizeLowerBound = quantizeLowerBound;
                this.quantizeUpperBound = quantizeUpperBound;
            }

            public bool Equals(CustomSerializerLookupKey other)
            {
                return serializedType == other.serializedType
                    && quantizeDownToBitCount == other.quantizeDownToBitCount
                    && quantizeLowerBound == other.quantizeLowerBound
                    && quantizeUpperBound == other.quantizeUpperBound;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CustomSerializerLookupKey))
                {
                    return false;
                }

                var key = (CustomSerializerLookupKey)obj;
                return serializedType == key.serializedType
                    && quantizeDownToBitCount == key.quantizeDownToBitCount
                    && quantizeLowerBound == key.quantizeLowerBound
                    && quantizeUpperBound == key.quantizeUpperBound;
            }

            public override int GetHashCode()
            {
                var hashCode = -1886448105;
                hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(serializedType);
                hashCode = hashCode * -1521134295 + quantizeDownToBitCount.GetHashCode();
                hashCode = hashCode * -1521134295 + quantizeLowerBound.GetHashCode();
                hashCode = hashCode * -1521134295 + quantizeUpperBound.GetHashCode();
                return hashCode;
            }
        }

        static readonly Dictionary<CustomSerializerLookupKey, IGONetAutoMagicalSync_CustomSerializer> customSerializerInstanceByTypeMap = new Dictionary<CustomSerializerLookupKey, IGONetAutoMagicalSync_CustomSerializer>(25);
        IGONetAutoMagicalSync_CustomSerializer customSerialize_Instance = null;
        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// This will only be non-null when <see cref="CustomSerialize_Type"/> is set to a non-abstract type
        /// that implements <see cref="IGONetAutoMagicalSync_CustomSerializer"/>.
        /// </summary>
        public IGONetAutoMagicalSync_CustomSerializer CustomSerialize_Instance
        {
            get
            {
                if (CustomSerialize_Type != null &&
                    customSerialize_Instance == null &&
                    TypeUtils.IsTypeAInstanceOfTypeB(CustomSerialize_Type, typeof(IGONetAutoMagicalSync_CustomSerializer)) &&
                    !CustomSerialize_Type.IsAbstract)
                { // if in here, we know we need to lookup or perhaps create a new instance of this class and it should work fine
                    CustomSerializerLookupKey key = new CustomSerializerLookupKey(CustomSerialize_Type, QuantizeDownToBitCount, QuantizeLowerBound, QuantizeUpperBound);
                    if (!customSerializerInstanceByTypeMap.TryGetValue(key, out customSerialize_Instance))
                    {
                        customSerialize_Instance = (IGONetAutoMagicalSync_CustomSerializer)Activator.CreateInstance(CustomSerialize_Type);
                        customSerialize_Instance.InitQuantizationSettings(key.quantizeDownToBitCount, key.quantizeLowerBound, key.quantizeUpperBound);

                        customSerializerInstanceByTypeMap[key] = customSerialize_Instance;
                    }
                }
                return customSerialize_Instance;
            }
        }

        /// <summary>
        /// Get/Create the "No quantization" option
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetCustomSerializer<T>() where T : IGONetAutoMagicalSync_CustomSerializer
        {
            return GetCustomSerializer<T>(0, 0, 0); // the first 0 for quantize down to bit count causes no quantization
        }

        public static T GetCustomSerializer<T>(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound) where T : IGONetAutoMagicalSync_CustomSerializer
        {
            T customSerializer = default(T);
            Type typeofT = typeof(T);
            if (!typeofT.IsAbstract)
            { // if in here, we know we need to lookup or perhaps create a new instance of this class and it should work fine
                IGONetAutoMagicalSync_CustomSerializer customSerializer_raw;
                CustomSerializerLookupKey key = new CustomSerializerLookupKey(typeofT, quantizeDownToBitCount, quantizeLowerBound, quantizeUpperBound);
                if (customSerializerInstanceByTypeMap.TryGetValue(key, out customSerializer_raw))
                {
                    if (customSerializer_raw.GetType() == typeofT) // just double checking...probabaly overkill since we ensure this is true through other steps
                    {
                        customSerializer = (T)customSerializer_raw;
                    }
                }
                else
                {
                    customSerializer = Activator.CreateInstance<T>();
                    customSerializer.InitQuantizationSettings(key.quantizeDownToBitCount, key.quantizeLowerBound, key.quantizeUpperBound);

                    customSerializerInstanceByTypeMap[key] = customSerializer;
                }
            }
            return customSerializer;
        }

        public GONetAutoMagicalSyncAttribute() { }

        /// <param name="profileTemplateName">
        /// <para>This is the main way in which the runtime settings (represented by the placement of an instance of <see cref="GONetAutoMagicalSyncAttribute"/> on <see cref="MonoBehaviour"/> fields for syncing data) are derived/looked up.</para>
        /// <para>This is the 1-1 direct correlation with the name of one of the <see cref="GONetAutoMagicalSyncSettings_ProfileTemplate"/> asset instances in the "Resources/GONet/SyncSettingsProfiles" directory in your project (without the ".asset" file extension included).</para>
        /// <para>Check out the public const string definitions on <see cref="GONetAutoMagicalSyncAttribute"/> that start with the prefix "PROFILE_TEMPLATE_NAME___".</para>
        /// <para>IMPORTANT: If this is provided, the settings defined on the corresponding profile/template will be used and NOT the settings on the attribute!</para>
        /// </param>
        public GONetAutoMagicalSyncAttribute(string profileTemplateName)
        {
            SettingsProfileTemplateName = profileTemplateName;
        }
    }

    public interface IGONetAutoMagicalSync_CustomSerializer
    {
        void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound);

        /// <summary>
        /// Since the way this custom serializer may or may not perform quantization during <see cref="Serialize(BitByBitByteArrayBuilder, GONetParticipant, GONetSyncableValue)"/>, 
        /// this method is helpful to know if two values are considered the same *IF* quantization is part of the equation.
        /// </summary>
        bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB);
        
        /// <param name="gonetParticipant">here for reference in case that helps to serialize properly</param>
        void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value);

        GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom);
    }

    public class Vector2Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector2Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);

                isQuantizationInitialized = true;
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            uint x;
            bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
            uint y;
            bitStream_readFrom.ReadUInt(out y, bitsPerComponent);

            return new Vector2(quantizer.Unquantize(x), quantizer.Unquantize(y));
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector2 vector2 = value.UnityEngine_Vector2;

            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector2.x), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector2.y), bitsPerComponent);
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            Vector2 vector2A = valueA.UnityEngine_Vector2;
            Vector2 vector2B = valueB.UnityEngine_Vector2;

            return
                quantizer.Quantize(vector2A.x) == quantizer.Quantize(vector2B.x) &&
                quantizer.Quantize(vector2A.y) == quantizer.Quantize(vector2B.y);
        }
    }

    public class Vector3Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector3Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            bool areFloatsFullSized = bitsPerComponent == 32;
            if (areFloatsFullSized) // i.e., nothing to unquantize
            {
                float x;
                bitStream_readFrom.ReadFloat(out x);
                float y;
                bitStream_readFrom.ReadFloat(out y);
                float z;
                bitStream_readFrom.ReadFloat(out z);

                return new Vector3(x, y, z);
            }
            else
            {
                uint x;
                bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
                uint y;
                bitStream_readFrom.ReadUInt(out y, bitsPerComponent);
                uint z;
                bitStream_readFrom.ReadUInt(out z, bitsPerComponent);

                return new Vector3(quantizer.Unquantize(x), quantizer.Unquantize(y), quantizer.Unquantize(z));
            }
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector3 vector3 = value.UnityEngine_Vector3;

            bool areFloatsFullSized = bitsPerComponent == 32;
            if (areFloatsFullSized) // i.e., nothing to quantize
            {
                bitStream_appendTo.WriteFloat(vector3.x);
                bitStream_appendTo.WriteFloat(vector3.y);
                bitStream_appendTo.WriteFloat(vector3.z);
            }
            else
            {
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.x), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.y), bitsPerComponent);
                bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.z), bitsPerComponent);
            }
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            Vector3 vector3A = valueA.UnityEngine_Vector3;
            Vector3 vector3B = valueB.UnityEngine_Vector3;

            return
                quantizer.Quantize(vector3A.x) == quantizer.Quantize(vector3B.x) &&
                quantizer.Quantize(vector3A.y) == quantizer.Quantize(vector3B.y) &&
                quantizer.Quantize(vector3A.z) == quantizer.Quantize(vector3B.z);
        }
    }

    public class Vector4Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 10000f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerComponent = DEFAULT_BITS_PER_COMPONENT;

        public Vector4Serializer() { }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerComponent = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerComponent, true);
            }
        }

        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            uint x;
            bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
            uint y;
            bitStream_readFrom.ReadUInt(out y, bitsPerComponent);
            uint z;
            bitStream_readFrom.ReadUInt(out z, bitsPerComponent);
            uint w;
            bitStream_readFrom.ReadUInt(out w, bitsPerComponent);

            return new Vector4(quantizer.Unquantize(x), quantizer.Unquantize(y), quantizer.Unquantize(z), quantizer.Unquantize(w));
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            Vector4 vector4 = value.UnityEngine_Vector4;

            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.x), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.y), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.z), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector4.w), bitsPerComponent);
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            Vector4 vector4A = valueA.UnityEngine_Vector4;
            Vector4 vector4B = valueB.UnityEngine_Vector4;

            return
                quantizer.Quantize(vector4A.x) == quantizer.Quantize(vector4B.x) &&
                quantizer.Quantize(vector4A.y) == quantizer.Quantize(vector4B.y) &&
                quantizer.Quantize(vector4A.z) == quantizer.Quantize(vector4B.z) &&
                quantizer.Quantize(vector4A.w) == quantizer.Quantize(vector4B.w);
        }
    }

    public class QuaternionSerializer : IGONetAutoMagicalSync_CustomSerializer
    {
        static readonly float SQUARE_ROOT_OF_2 = Mathf.Sqrt(2.0f);
        static readonly float QuatValueMinimum = -1.0f / SQUARE_ROOT_OF_2;
        static readonly float QuatValueMaximum = +1.0f / SQUARE_ROOT_OF_2;

        bool isQuantizationInitialized = false;
        Quantizer quantizer;
        byte bitsPerSmallestThreeItem = DEFAULT_BITS_PER_SMALLEST_THREE;

        public const byte DEFAULT_BITS_PER_SMALLEST_THREE = 9;

        public QuaternionSerializer() : this(DEFAULT_BITS_PER_SMALLEST_THREE) { }

        public QuaternionSerializer(byte bitsPerSmallestThreeItem)
        {
            this.bitsPerSmallestThreeItem = bitsPerSmallestThreeItem;
            quantizer = new Quantizer(QuatValueMinimum, QuatValueMaximum, bitsPerSmallestThreeItem, true);
        }

        public void InitQuantizationSettings(byte quantizeDownToBitCount, float quantizeLowerBound, float quantizeUpperBound)
        {
            if (quantizeDownToBitCount > 0)
            {
                if (isQuantizationInitialized)
                {
                    throw new InvalidOperationException("Quantization is already initialized for this custom serializer.");
                }

                bitsPerSmallestThreeItem = quantizeDownToBitCount;
                quantizer = new Quantizer(quantizeLowerBound, quantizeUpperBound, bitsPerSmallestThreeItem, true);
            }
        }

        /// <returns>a <see cref="Quaternion"/></returns>
        public GONetSyncableValue Deserialize(Utils.BitByBitByteArrayBuilder bitStream_readFrom)
        {
            uint LargestIndex;
            bitStream_readFrom.ReadUInt(out LargestIndex, 2);
            uint SmallestA;
            bitStream_readFrom.ReadUInt(out SmallestA, bitsPerSmallestThreeItem);
            uint SmallestB;
            bitStream_readFrom.ReadUInt(out SmallestB, bitsPerSmallestThreeItem);
            uint SmallestC;
            bitStream_readFrom.ReadUInt(out SmallestC, bitsPerSmallestThreeItem);

            float a = quantizer.Unquantize(SmallestA);
            float b = quantizer.Unquantize(SmallestB);
            float c = quantizer.Unquantize(SmallestC);

            float x, y, z, w;

            switch (LargestIndex)
            {
                case 0:
                    {
                        x = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        y = a;
                        z = b;
                        w = c;
                    }
                    break;

                case 1:
                    {
                        x = a;
                        y = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        z = b;
                        w = c;
                    }
                    break;

                case 2:
                    {
                        x = a;
                        y = b;
                        z = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                        w = c;
                    }
                    break;

                case 3:
                    {
                        x = a;
                        y = b;
                        z = c;
                        w = (float)Math.Sqrt(1f - a * a - b * b - c * c); // calculated the largest value based on the smallest 3 values
                    }
                    break;

                default:
                    {
                        Debug.Assert(false);
                        x = 0F;
                        y = 0F;
                        z = 0F;
                        w = 1F;
                    }
                    break;
            }

            return new Quaternion(x, y, z, w);
        }

        public void Serialize(Utils.BitByBitByteArrayBuilder bitStream_appendTo, GONetParticipant gonetParticipant, GONetSyncableValue value)
        {
            uint LargestIndex;
            uint SmallestA;
            uint SmallestB;
            uint SmallestC;

            Quantize(value, out LargestIndex, out SmallestA, out SmallestB, out SmallestC);

            bitStream_appendTo.WriteUInt(LargestIndex, 2);
            bitStream_appendTo.WriteUInt(SmallestA, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestB, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestC, bitsPerSmallestThreeItem);
        }

        private void Quantize(GONetSyncableValue value, out uint largestIndex, out uint smallestA, out uint smallestB, out uint smallestC)
        {
            Quaternion quattie = value.UnityEngine_Quaternion;
            float x = quattie.x;
            float y = quattie.y;
            float z = quattie.z;
            float w = quattie.w;

            float xABS = Math.Abs(x);
            float yABS = Math.Abs(y);
            float zABS = Math.Abs(z);
            float wABS = Math.Abs(w);

            largestIndex = 0;
            float largestValue = xABS;

            if (yABS > largestValue)
            {
                largestIndex = 1;
                largestValue = yABS;
            }

            if (zABS > largestValue)
            {
                largestIndex = 2;
                largestValue = zABS;
            }

            if (wABS > largestValue)
            {
                largestIndex = 3;
                largestValue = wABS;
            }

            float a = 0f;
            float b = 0f;
            float c = 0f;

            switch (largestIndex)
            {
                case 0:
                    if (x >= 0)
                    {
                        a = y;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -y;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 1:
                    if (y >= 0)
                    {
                        a = x;
                        b = z;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -z;
                        c = -w;
                    }
                    break;

                case 2:
                    if (z >= 0)
                    {
                        a = x;
                        b = y;
                        c = w;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -w;
                    }
                    break;

                case 3:
                    if (w >= 0)
                    {
                        a = x;
                        b = y;
                        c = z;
                    }
                    else
                    {
                        a = -x;
                        b = -y;
                        c = -z;
                    }
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }


            smallestA = quantizer.Quantize(a);
            smallestB = quantizer.Quantize(b);
            smallestC = quantizer.Quantize(c);
        }

        public bool AreEqualConsideringQuantization(GONetSyncableValue valueA, GONetSyncableValue valueB)
        {
            uint LargestIndex_A, LargestIndex_B;
            uint SmallestA_A, SmallestA_B;
            uint SmallestB_A, SmallestB_B;
            uint SmallestC_A, SmallestC_B;

            Quantize(valueA, out LargestIndex_A, out SmallestA_A, out SmallestB_A, out SmallestC_A);
            Quantize(valueB, out LargestIndex_B, out SmallestA_B, out SmallestB_B, out SmallestC_B);

            return
                LargestIndex_A == LargestIndex_B &&
                SmallestA_A == SmallestA_B &&
                SmallestB_A == SmallestB_B &&
                SmallestC_A == SmallestC_B;
        }
    }
}
