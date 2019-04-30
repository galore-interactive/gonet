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
        /// Use this if tevery chang should be processed at the end of the game update frame in which the change occurred.
        /// This is the fastest GONet can deliver results. 
        /// Try not to use this for everything or else the network performance will be negatively affected with what is likely traffic that is not all vital.
        /// </summary>
        public const float END_OF_FRAME_IN_WHICH_CHANGE_OCCURS = 0f;

        /// <summary>
        /// This is a great default value for most data items that is not considered absolutely vital to arrive ASAP.
        /// </summary>
        public const float _24_Hz = 1f / 24f;
    }

    public enum AutoMagicalSyncReliability : byte
    {
        /// <summary>
        /// Every time the value changes, it is sent reliably
        /// </summary>
        Reliable,

        /// <summary>
        /// Expectation is that the value changes frequently and should be sent as an unreliable stream of changes, using interpolation/extrapolation
        /// </summary>
        UnreliableStream
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
    [MessagePackObject]
    public class GONetAutoMagicalSyncAttribute : Attribute
    {
        public override object TypeId => base.TypeId;

        /// <summary>
        /// How often (in seconds) the system will check the field/property value for a change and send it across the network if it did change.
        /// <see cref="AutoMagicalSyncFrequencies"/> for some standard options to use here.
        /// </summary>
        public float SyncChangesEverySeconds = AutoMagicalSyncFrequencies._24_Hz;

        public AutoMagicalSyncReliability Reliability = AutoMagicalSyncReliability.Reliable;

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
        /// </summary>
        public int QuantizeDownToBitCount = 0;

        #region GONet internal only

        /// <summary>
        /// DO NOT use this!  It is used internally within GONet only.
        /// If you use this, results of entire GONet could be compromised and unpredictable....why would you want that?
        /// </summary>
        public int ProcessingPriority_GONetInternalOverride = 0;

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
    }

    public interface IGONetAutoMagicalSync_CustomSerializer
    {
        /// <param name="gonetParticipant">here for reference in case that helps to serialize properly</param>
        void Serialize(Utils.BitStream bitStream_appendTo, GONetParticipant gonetParticipant, object value);

        object Deserialize(Utils.BitStream bitStream_readFrom);
    }
}
