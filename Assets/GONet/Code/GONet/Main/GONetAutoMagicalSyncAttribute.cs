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

using GONet.Utils;
using GONet.PluginAPI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GONet
{
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

        /// <summary>
        /// Key = (int codeGenerationId, int singleIndex)
        /// </summary>
        internal static readonly Dictionary<(int, int), Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool>> ShouldSkipSyncByRegistrationIdMap = 
            new Dictionary<(int, int), Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool>>(2);
        
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

        #region custom serializer stuffs
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

        #endregion

        #region custom value blending stuffs
        /// <summary>
        /// OPTIONAL.
        /// You can use this when there is a special case for serialization (e.g., when not blittable or not GONet natively support type).
        /// IMPORTANT: Due to C# attribute parameter type limitations (see https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/attributes),
        ///            the type here is <see cref="Type"/> when it really needs to be an instance of <see cref="IGONetAutoMagicalSync_CustomValueBlending"/>.
        ///            An instance of this type will be created and cast to <see cref="IGONetAutoMagicalSync_CustomValueBlending"/> and 
        ///            set on <see cref="CustomValueBlending_Instance"/> and that is what will be used.
        /// </summary>
        public Type CustomValueBlending_Type = null;

        /// <summary>
        /// Only here to support serialization of <see cref="CustomValueBlending_Type"/>
        /// </summary>
        public string CustomValueBlending_Type_AsString
        {
            get { return CustomValueBlending_Type != null ? CustomValueBlending_Type.AssemblyQualifiedName : null; }

            set
            {
                if (value == null)
                {
                    CustomValueBlending_Type = null;
                }
                else
                {
                    CustomValueBlending_Type = Type.GetType(value);
                }
            }
        }

        static readonly Dictionary<Type, IGONetAutoMagicalSync_CustomValueBlending> customValueBlendingInstanceByTypeMap = new Dictionary<Type, IGONetAutoMagicalSync_CustomValueBlending>(25);
        IGONetAutoMagicalSync_CustomValueBlending customValueBlending_Instance = null;
        /// <summary>
        /// IMPORTANT: Do NOT use this.
        /// TODO: make the main dll internals visible to editor dll so this can be made internal again
        /// This will only be non-null when <see cref="CustomValueBlending_Type"/> is set to a non-abstract type
        /// that implements <see cref="IGONetAutoMagicalSync_CustomValueBlending"/>.
        /// </summary>
        public IGONetAutoMagicalSync_CustomValueBlending CustomValueBlending_Instance
        {
            get
            {
                if (CustomValueBlending_Type != null &&
                    customValueBlending_Instance == null &&
                    TypeUtils.IsTypeAInstanceOfTypeB(CustomValueBlending_Type, typeof(IGONetAutoMagicalSync_CustomValueBlending)) &&
                    !CustomValueBlending_Type.IsAbstract)
                { // if in here, we know we need to lookup or perhaps create a new instance of this class and it should work fine
                    if (!customValueBlendingInstanceByTypeMap.TryGetValue(CustomValueBlending_Type, out customValueBlending_Instance))
                    {
                        customValueBlending_Instance = (IGONetAutoMagicalSync_CustomValueBlending)Activator.CreateInstance(CustomValueBlending_Type);
                        customValueBlendingInstanceByTypeMap[CustomValueBlending_Type] = customValueBlending_Instance;
                    }
                }
                return customValueBlending_Instance;
            }
        }

        public static T GetCustomValueBlending<T>() where T : IGONetAutoMagicalSync_CustomValueBlending
        {
            T customValueBlending = default(T);
            Type typeofT = typeof(T);
            if (!typeofT.IsAbstract)
            { // if in here, we know we need to lookup or perhaps create a new instance of this class and it should work fine
                IGONetAutoMagicalSync_CustomValueBlending customValueBlending_raw;
                if (customValueBlendingInstanceByTypeMap.TryGetValue(typeofT, out customValueBlending_raw))
                {
                    if (customValueBlending_raw.GetType() == typeofT) // just double checking...probabaly overkill since we ensure this is true through other steps
                    {
                        customValueBlending = (T)customValueBlending_raw;
                    }
                }
                else
                {
                    customValueBlending = Activator.CreateInstance<T>();
                    customValueBlendingInstanceByTypeMap[typeofT] = customValueBlending;
                }
            }
            return customValueBlending;
        }

        #endregion

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
}
