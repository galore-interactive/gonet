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
    /// This is akin to [SyncVar] from old/legacy unity networking.....change sent at end of frame; HOWEVER, changes are monitored on both server (like [SyncVar]) ***AND*** client (UNlike [SyncVar]).
    /// TODO have to figure out network ownership and whatnot!
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
