using System;
using UnityEngine;

namespace GONet
{
    public enum AutoMagicalSyncSchedule : byte
    {
        EndOfEveryFrame,

        /// <summary>
        /// Less often than every frame...maybe once a second
        /// 
        /// TODO: have to define where this is configured!
        /// </summary>
        OnConfigurableFrequency
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
    public class GONetAutoMagicalSyncAttribute : Attribute
    {
        public AutoMagicalSyncSchedule Schedule = AutoMagicalSyncSchedule.EndOfEveryFrame;

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
    }
}
