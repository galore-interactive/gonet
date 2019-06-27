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

        public static InstantiateGONetParticipantEvent Create(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event = new InstantiateGONetParticipantEvent();

            @event.DesignTimeLocation = gonetParticipant.designTimeLocation;
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
}
