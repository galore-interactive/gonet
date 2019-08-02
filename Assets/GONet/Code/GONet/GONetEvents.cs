/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
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
using System.Collections.Generic;

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
        public string InstanceName;

        internal static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.InstanceName = gonetParticipant.gameObject.name;
            @event.DesignTimeLocation = gonetParticipant.designTimeLocation;
            @event.GONetId = gonetParticipant.GONetId;
            @event.OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;

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

            @event.OccurredAtElapsedTicks = default;

            return @event;
        }
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

    /// <summary>
    /// This represents that a sync value change has been processed.  Two major occassions:
    /// 1) For an outbound change being sent to others (in which case, this event is published AFTER the change has been sent to remote sources)
    /// 2) For an inbound change received from other (in which case, this event is published AFTER the change has been applied)
    /// 
    /// Be aware, this class has some TODO FIXME object type BOXING GC
    /// </summary>
    [MessagePackObject]
    public class SyncValueChangeProcessedEvent : ITransientEvent, ILocalOnlyPublish
    {
        public enum ProcessedExplanation : byte
        {
            OutboundToOthers = 1,

            InboundFromOther,

            BlendingBetweenInboundValuesFromOther,
        }

        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public uint RelatedOwnerAuthorityId;

        [Key(2)]
        public uint GONetId;

        [Key(3)]
        public byte index;

        /// <summary>
        /// TODO FIXME object type BOXING GC
        /// </summary>
        [Key(4)]
        public object valuePrevious;

        /// <summary>
        /// TODO FIXME object type BOXING GC
        /// </summary>
        [Key(5)]
        public object valueNew;

        [Key(6)]
        public ProcessedExplanation Explanation;

        /// <summary>
        /// Be aware, this class has some TODO FIXME object type BOXING GC
        /// </summary>
        public SyncValueChangeProcessedEvent(ProcessedExplanation explanation, long occurredAtElapsedTicks, uint relatedOwnerAuthorityId, uint gonetId, byte index, object valuePrevious, object valueNew)
        {
            Explanation = explanation;
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            GONetId = gonetId;

            this.index = index;
            this.valuePrevious = valuePrevious;
            this.valueNew = valueNew;
        }
    }

    /*
    /// <summary>
    /// TODO probably want to consolidate...well, we did already
    /// Be aware, this class has some TODO FIXME object type BOXING GC
    /// </summary>
    [MessagePackObject]
    public struct SyncValueChangeProcessedFromOtherEvent : ITransientEvent, ILocalOnlyPublish
    {
        [Key(0)]
        public long OccurredAtElapsedTicks { get; set; }

        [Key(1)]
        public uint GONetId;

        [Key(2)]
        public byte index;

        /// <summary>
        /// TODO FIXME object type BOXING GC
        /// </summary>
        [Key(3)]
        public object valuePrevious;

        /// <summary>
        /// TODO FIXME object type BOXING GC
        /// </summary>
        [Key(4)]
        public object valueNew;

        /// <summary>
        /// Be aware, this class has some TODO FIXME object type BOXING GC
        /// </summary>
        public SyncValueChangeProcessedFromOtherEvent(long occurredAtElapsedTicks, uint gONetId, byte index, object valuePrevious, object valueNew)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            GONetId = gONetId;

            this.index = index;
            this.valuePrevious = valuePrevious;
            this.valueNew = valueNew;
        }
    }
    */
}
