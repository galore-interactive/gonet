/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace GONet
{
    #region base stuffs

    /// <summary>
    /// This alone does not mean much.  Implement either <see cref="ITransientEvent"/> or <see cref="IPersistentEvent"/>.
    /// </summary>
    public partial interface IGONetEvent
    {
        long OccurredAtElapsedTicks { get; }
    }

    /// <summary>
    /// Implement this to this indicates the information herein is only relevant while it is happening and while subscribers are notified and NOT to be passed along to newly connecting clients and can safely be skipped over during replay skip-ahead or fast-forward.
    /// </summary>
    public partial interface ITransientEvent : IGONetEvent { }

    /// <summary>
    /// Implement this for persistent events..opposite of extending <see cref="ITransientEvent"/> (see the comments there for more).
    /// </summary>
    public partial interface IPersistentEvent : IGONetEvent { }

    /// <summary>
    /// Tack this on to any event type to ensure calls to <see cref="GONetEventBus.Publish{T}(T, uint?)"/> only publish locally (i.e., not sent across the network to anyone else)
    /// </summary>
    public interface ILocalOnlyPublish { }

    #endregion

    public struct AutoMagicalSync_AllCurrentValues_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    public struct ServerSaysClientInitializationCompletion : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new NotImplementedException();
    }

    public struct AutoMagicalSync_ValueChanges_Message : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    public struct OwnerAuthorityIdAssignmentEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }

    [MessagePackObject]
    public struct RequestMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public long UID;

        public RequestMessage(long occurredAtElapsedTicks)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;

            UID = GUID.Generate().AsInt64();
        }
    }

    [MessagePackObject]
    public struct ResponseMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public long CorrelationRequestUID;

        public ResponseMessage(long occurredAtElapsedTicks, long correlationRequestUID)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            CorrelationRequestUID = correlationRequestUID;
        }
    }

    [MessagePackObject]
    public struct InstantiateGONetParticipantEvent : IPersistentEvent
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// this is the information necessary to lookup the source <see cref="UnityEngine.GameObject"/> from which to use as the template in order to call <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// TODO add the persisted int->string lookup table that is updated each time a new design time location is encountered (at design time...duh)..so this can be an int!
        /// </summary>
        [Key(0)]
        public string DesignTimeLocation;

        [Key(1)]
        public uint GONetId;

        [Key(2)]
        public uint OwnerAuthorityId;

        [Key(3)]
        public Vector3 Position;

        [Key(4)]
        public Quaternion Rotation;

        [Key(5)]
        public string InstanceName;

        internal static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = gonetParticipant.designTimeLocation;

            @event.GONetId = gonetParticipant.GONetId;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }

        internal static InstantiateGONetParticipantEvent Create_WithNonAuthorityInfo(GONetParticipant gonetParticipant, string nonAuthorityAlternate_designTimeLocation)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = nonAuthorityAlternate_designTimeLocation;

            @event.GONetId = gonetParticipant.GONetId;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;

            @event.Position = gonetParticipant.transform.position;
            @event.Rotation = gonetParticipant.transform.rotation;

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }
    }

    [MessagePackObject]
    public struct DestroyGONetParticipantEvent : IPersistentEvent
    {
        [IgnoreMember]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(0)]
        public uint GONetId;
    }

    [MessagePackObject]
    public struct PersistentEvents_Bundle : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public Queue<IPersistentEvent> PersistentEvents;

        public PersistentEvents_Bundle(long occurredAtElapsedTicks, Queue<IPersistentEvent> persistentEvents) : this()
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            PersistentEvents = persistentEvents;
        }
    }

    [MessagePackObject]
    public struct ClientTypeFlagsChangedEvent : ITransientEvent
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public uint ClientAuthorityId { get; set; }

        [Key(2)]
        public ClientTypeFlags FlagsPrevious { get; set; }

        [Key(3)]
        public ClientTypeFlags FlagsNow { get; set; }

        public ClientTypeFlagsChangedEvent(long occurredAtElapsedTicks, uint myAuthorityId, ClientTypeFlags flagsPrevious, ClientTypeFlags flagsNow)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            ClientAuthorityId = myAuthorityId;
            FlagsPrevious = flagsPrevious;
            FlagsNow = flagsNow;
        }
    }

    public enum SyncEvent_ValueChangeProcessedExplanation : byte
    {
        OutboundToOthers = 1,

        InboundFromOther,

        BlendingBetweenInboundValuesFromOther,
    }

    /// <summary>
    /// Once this event is sent through <see cref="GONetEventBus.Publish{T}(T, uint?)"/>, it will automatically have <see cref="Return"/> called on it.
    /// At time of writing, this is to support (automatic) object pool usage for better memory/garbage/GC performance.
    /// </summary>
    public interface ISelfReturnEvent
    {
        void Return();
    }

    /// <summary>
    /// This represents that a sync value change has been processed locally.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MessagePackObject]
    public abstract partial class SyncEvent_ValueChangeProcessed : ITransientEvent, ILocalOnlyPublish, ISelfReturnEvent
    {
        [Key(0)] public double OccurredAtElapsedSeconds { get => TimeSpan.FromTicks(OccurredAtElapsedTicks).TotalSeconds; set { OccurredAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember] public long OccurredAtElapsedTicks { get; set; }

        [Key(1)] public double ProcessedAtElapsedSeconds { get => TimeSpan.FromTicks(ProcessedAtElapsedTicks).TotalSeconds; set { ProcessedAtElapsedTicks = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember] public long ProcessedAtElapsedTicks;

        [Key(2)] public uint RelatedOwnerAuthorityId;
        [Key(3)] public uint GONetId;

        [IgnoreMember] public byte CodeGenerationId;

        [Key(4)] public byte SyncMemberIndex;
        [Key(5)] public SyncEvent_ValueChangeProcessedExplanation Explanation;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// </summary>
        public SyncEvent_ValueChangeProcessed() { }

        public abstract void Return();
    }

    [MessagePackObject]
    public class SyncEvent_PersistenceBundle
    {
        [Key(0)] public Queue<SyncEvent_ValueChangeProcessed> bundle;

        public static readonly SyncEvent_PersistenceBundle Instance = new SyncEvent_PersistenceBundle();
    }

    /// <summary>
    /// This represents that a sync value change has been processed.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// </summary>
    [MessagePackObject]
    public sealed class SyncEvent_Time_ElapsedTicks_SetFromAuthority : SyncEvent_ValueChangeProcessed
    {
        [Key(6)]        public double ElapsedSeconds_Previous { get => TimeSpan.FromTicks(ElapsedTicks_Previous).TotalSeconds; set { ElapsedTicks_Previous = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember]  public long ElapsedTicks_Previous { get; private set; }

        [Key(7)]        public double ElapsedSeconds_New { get => TimeSpan.FromTicks(ElapsedTicks_New).TotalSeconds; set { ElapsedTicks_New = TimeSpan.FromSeconds(value).Ticks; } }
        [IgnoreMember]  public long ElapsedTicks_New { get; private set; }

        [Key(8)]        public double RoundTripSeconds_Latest { get; set; }
        [Key(9)]        public double RoundTripSeconds_RecentAverage { get; set; }
        [Key(10)]       public float RoundTripMilliseconds_LowLevelTransportProtocol { get; set; }

        static readonly ObjectPool<SyncEvent_Time_ElapsedTicks_SetFromAuthority> pool = new ObjectPool<SyncEvent_Time_ElapsedTicks_SetFromAuthority>(5, 1);
        static readonly ConcurrentQueue<SyncEvent_Time_ElapsedTicks_SetFromAuthority> returnQueue_onceOnBorrowThread = new ConcurrentQueue<SyncEvent_Time_ElapsedTicks_SetFromAuthority>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, long, long)"/>.
        /// </summary>
        public SyncEvent_Time_ElapsedTicks_SetFromAuthority() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_Time_ElapsedTicks_SetFromAuthority)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_Time_ElapsedTicks_SetFromAuthority Borrow(long elapsedTicks_previous, long elapsedTicks_new, float roundTripSeconds_latest, float roundTripSeconds_recentAverage, float roundTripMilliseconds_LowLevelTransportProtocol)
        {
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_Time_ElapsedTicks_SetFromAuthority autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();

            @event.RoundTripSeconds_Latest = roundTripSeconds_latest;
            @event.RoundTripSeconds_RecentAverage = roundTripSeconds_recentAverage;
            @event.RoundTripMilliseconds_LowLevelTransportProtocol = roundTripMilliseconds_LowLevelTransportProtocol;

            @event.Explanation = SyncEvent_ValueChangeProcessedExplanation.InboundFromOther;
            @event.OccurredAtElapsedTicks = elapsedTicks_previous;
            @event.RelatedOwnerAuthorityId = GONetMain.OwnerAuthorityId_Server;

            { // meaningless for this event:
                @event.GONetId = GONetParticipant.GONetId_Unset;
                @event.CodeGenerationId = 0;
                @event.SyncMemberIndex = 0;
            }

            @event.ElapsedTicks_Previous = elapsedTicks_previous;
            @event.ElapsedTicks_New = elapsedTicks_new;

            return @event;
        }

        public override void Return()
        {
            Return(this);
        }

        public static void Return(SyncEvent_Time_ElapsedTicks_SetFromAuthority borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }
}
