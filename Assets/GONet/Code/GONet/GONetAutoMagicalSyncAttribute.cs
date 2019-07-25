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
using MessagePack;
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
    /// IMPORTANT: Ensure this is placed on a field on a <see cref="MonoBehaviour"/>
    ///            and the aforementioned <see cref="MonoBehaviour"/> is placed on a 
    ///            <see cref="GameObject"/> with a <see cref="GONetParticipant"/>
    ///            placed on it as well!
    /// 
    /// This is akin to [SyncVar] from old/legacy unity networking.....change sent at end of frame; HOWEVER, changes are
    /// monitored on both server (like [SyncVar]) ***AND*** client (UNlike [SyncVar]).
    /// TODO have to figure out network ownership and whatnot!
    /// 
    /// As a reminder, here is the Unity 2017.4 documentation for SyncVar (taken from: https://docs.unity3d.com/2017.4/Documentation/ScriptReference/Networking.SyncVarAttribute.html):
    ///     [SyncVar] is an attribute that can be put on member variables of NetworkBehaviour classes. These variables will
    ///     have their values sychronized from the server to clients in the game that are in the ready state.
    ///     Setting the value of a[SyncVar] marks it as dirty, so it will be sent to clients at the end of the current frame.
    ///     Only simple values can be marked as [SyncVars]. The type of the SyncVar variable cannot be from an external DLL or assembly.
    ///     The allowed SyncVar types are;
    ///     • Basic type(byte, int, float, string, UInt64, etc)
    ///     • Built-in Unity math type(Vector3, Quaternion, etc), 
    ///     • Structs containing allowable types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class GONetAutoMagicalSyncAttribute : Attribute
    {
        public override object TypeId => base.TypeId;

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
        public string SettingsProfileTemplateName;

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
        /// Only applicable to primitive numeric data types, mainly float.
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

        static readonly Dictionary<Type, IGONetAutoMagicalSync_CustomSerializer> customSerializerInstanceByTypeMap = new Dictionary<Type, IGONetAutoMagicalSync_CustomSerializer>(25);
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
                    if (!customSerializerInstanceByTypeMap.TryGetValue(CustomSerialize_Type, out customSerialize_Instance))
                    {
                        customSerialize_Instance = (IGONetAutoMagicalSync_CustomSerializer)Activator.CreateInstance(CustomSerialize_Type);
                        customSerializerInstanceByTypeMap[CustomSerialize_Type] = customSerialize_Instance;
                    }
                }
                return customSerialize_Instance;
            }
        }

        public static T GetCustomSerializer<T>() where T : IGONetAutoMagicalSync_CustomSerializer
        {
            T customSerializer = default(T);
            Type typeofT = typeof(T);
            if (!typeofT.IsAbstract)
            { // if in here, we know we need to lookup or perhaps create a new instance of this class and it should work fine
                IGONetAutoMagicalSync_CustomSerializer customSerializer_raw;
                if (customSerializerInstanceByTypeMap.TryGetValue(typeofT, out customSerializer_raw))
                {
                    if (customSerializer_raw.GetType() == typeofT) // just double checking...probabaly overkill since we ensure this is true through other steps
                    {
                        customSerializer = (T)customSerializer_raw;
                    }
                }
                else
                {
                    customSerializer = Activator.CreateInstance<T>();
                    customSerializerInstanceByTypeMap[typeofT] = customSerializer;
                }
            }
            return customSerializer;
        }

        public GONetAutoMagicalSyncAttribute() { }

        public GONetAutoMagicalSyncAttribute(string profileTemplateName)
        {
            SettingsProfileTemplateName = profileTemplateName;
        }
    }

    public interface IGONetAutoMagicalSync_CustomSerializer
    {
        /// <param name="gonetParticipant">here for reference in case that helps to serialize properly</param>
        void Serialize(Utils.BitStream bitStream_appendTo, GONetParticipant gonetParticipant, object value);

        object Deserialize(Utils.BitStream bitStream_readFrom);
    }

    public class Vector3Serializer : IGONetAutoMagicalSync_CustomSerializer
    {
        public const byte DEFAULT_BITS_PER_COMPONENT = 32;
        public const float DEFAULT_MAX_VALUE = 100f;
        public const float DEFAULT_MIN_VALUE = -DEFAULT_MAX_VALUE;

        readonly Quantizer quantizer;
        byte bitsPerComponent;

        public Vector3Serializer() : this(DEFAULT_MIN_VALUE, DEFAULT_MAX_VALUE, DEFAULT_BITS_PER_COMPONENT) { }

        public Vector3Serializer(float minValue, float maxValue, byte bitsPerComponent)
        {
            this.bitsPerComponent = bitsPerComponent;
            quantizer = new Quantizer(minValue, maxValue, bitsPerComponent, true);
        }

        public object Deserialize(Utils.BitStream bitStream_readFrom)
        {
            uint x;
            bitStream_readFrom.ReadUInt(out x, bitsPerComponent);
            uint y;
            bitStream_readFrom.ReadUInt(out y, bitsPerComponent);
            uint z;
            bitStream_readFrom.ReadUInt(out z, bitsPerComponent);

            return new Vector3(quantizer.Unquantize(x), quantizer.Unquantize(y), quantizer.Unquantize(z));
        }

        public void Serialize(Utils.BitStream bitStream_appendTo, GONetParticipant gonetParticipant, object value)
        {
            Vector3 vector3 = (Vector3)value;

            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.x), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.y), bitsPerComponent);
            bitStream_appendTo.WriteUInt(quantizer.Quantize(vector3.z), bitsPerComponent);
        }
    }

    public class QuaternionSerializer : IGONetAutoMagicalSync_CustomSerializer
    {
        static readonly float SQUARE_ROOT_OF_2 = Mathf.Sqrt(2.0f);
        static readonly float QuatValueMinimum = -1.0f / SQUARE_ROOT_OF_2;
        static readonly float QuatValueMaximum = +1.0f / SQUARE_ROOT_OF_2;

        Quantizer quantizer;
        byte bitsPerSmallestThreeItem;

        public const byte DEFAULT_BITS_PER_SMALLEST_THREE = 9;

        public QuaternionSerializer() : this(DEFAULT_BITS_PER_SMALLEST_THREE) { }

        public QuaternionSerializer(byte bitsPerSmallestThreeItem)
        {
            this.bitsPerSmallestThreeItem = bitsPerSmallestThreeItem;
            quantizer = new Quantizer(QuatValueMinimum, QuatValueMaximum, bitsPerSmallestThreeItem, true);
        }

        /// <returns>a <see cref="Quaternion"/></returns>
        public object Deserialize(Utils.BitStream bitStream_readFrom)
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

        public void Serialize(Utils.BitStream bitStream_appendTo, GONetParticipant gonetParticipant, object value)
        {
            uint LargestIndex;
            uint SmallestA;
            uint SmallestB;
            uint SmallestC;

            Quaternion quattie = (Quaternion)value;
            float x = quattie.x;
            float y = quattie.y;
            float z = quattie.z;
            float w = quattie.w;

            float xABS = Math.Abs(x);
            float yABS = Math.Abs(y);
            float zABS = Math.Abs(z);
            float wABS = Math.Abs(w);

            LargestIndex = 0;
            float largestValue = xABS;

            if (yABS > largestValue)
            {
                LargestIndex = 1;
                largestValue = yABS;
            }

            if (zABS > largestValue)
            {
                LargestIndex = 2;
                largestValue = zABS;
            }

            if (wABS > largestValue)
            {
                LargestIndex = 3;
                largestValue = wABS;
            }

            float a = 0f;
            float b = 0f;
            float c = 0f;

            switch (LargestIndex)
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


            SmallestA = quantizer.Quantize(a);
            SmallestB = quantizer.Quantize(b);
            SmallestC = quantizer.Quantize(c);

            bitStream_appendTo.WriteUInt(LargestIndex, 2);
            bitStream_appendTo.WriteUInt(SmallestA, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestB, bitsPerSmallestThreeItem);
            bitStream_appendTo.WriteUInt(SmallestC, bitsPerSmallestThreeItem);
        }
    }
}