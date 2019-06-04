using GONet.Utils;
using System.Collections.Generic;

namespace GONet
{
    #region base stuffs

    /// <summary>
    /// This alone does not mean much.  Implement either <see cref="ITransientEvent"/> or <see cref="IPersistentEvent"/>.
    /// </summary>
    public interface IGONetEvent
    {
        long OccurredAtElapsedTicks { get; }
    }

    /// <summary>
    /// Implement this to this indicates the information herein is only relevant while it is happening and while subscribers are notified and NOT to be passed along to newly connecting clients and can safely be skipped over during replay skip-ahead or fast-forward.
    /// </summary>
    public interface ITransientEvent : IGONetEvent { }

    /// <summary>
    /// Implement this for persistent events..opposite of extending <see cref="ITransientEvent"/> (see the comments there for more).
    /// </summary>
    public interface IPersistentEvent : IGONetEvent { }

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

    public struct RequestMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        public long OccurredAtElapsedTicks { get; set; }
        public readonly long UID;

        public RequestMessage(long occurredAtElapsedTicks)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;

            UID = GUID.Generate().AsInt64();
        }
    }

    public struct ResponseMessage : ITransientEvent // TODO probably not always going to be considered transient
    {
        public long OccurredAtElapsedTicks { get; set; }
        public readonly long CorrelationRequestUID;

        public ResponseMessage(long occurredAtElapsedTicks, long correlationRequestUID)
        {
            OccurredAtElapsedTicks = occurredAtElapsedTicks;
            CorrelationRequestUID = correlationRequestUID;
        }
    }

    public struct InstantiateGONetParticipantEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks { get; set; }

        /// <summary>
        /// this is the information necessary to lookup the source <see cref="UnityEngine.GameObject"/> from which to use as the template in order to call <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// TODO add the persisted int->string lookup table that is updated each time a new design time location is encountered (at design time...duh)..so this can be an int!
        /// </summary>
        public string DesignTimeLocation;

        public uint GONetId;

        public uint OwnerAuthorityId;

        public InstantiateGONetParticipantEvent(GONetParticipant gonetParticipant)
        {
            DesignTimeLocation = gonetParticipant.designTimeLocation;
            GONetId = gonetParticipant.GONetId;
            OwnerAuthorityId = gonetParticipant.OwnerAuthorityId;

            OccurredAtElapsedTicks = default;
        }
    }

    public struct PersistentEvents_Bundle : ITransientEvent
    {
        public long OccurredAtElapsedTicks => throw new System.NotImplementedException();
    }
}
