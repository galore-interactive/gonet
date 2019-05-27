using GONet.Utils;
using System.Collections.Generic;

namespace GONet
{
    #region base stuffs

    public interface IGONetEvent
    {
        /// <summary>
        /// If true, this indicates the information herein is only relevant while it is happening and while subscribers are notified and NOT to be passed along to newly connecting clients and can safely be skipped over during replay skip-ahead or fast-forward.
        /// If false, the term used is persistent.
        /// </summary>
        bool IsTransient { get; }

        long OccurredAtElapsedTicks { get; }
    }

    public abstract class TransientEvent : IGONetEvent
    {
        public bool IsTransient { get; private set; }  = true;

        public long OccurredAtElapsedTicks { get; }
    }

    public abstract class PersistentEvent : IGONetEvent
    {
        public bool IsTransient { get; private set; } = false;

        public long OccurredAtElapsedTicks { get; set; }
    }

    #endregion

    public class AutoMagicalSync_AllCurrentValues_Message : TransientEvent { }

    public class AutoMagicalSync_ValueChanges_Message : TransientEvent { }

    public class OwnerAuthorityIdAssignmentEvent : PersistentEvent { }

    public class RequestMessage : TransientEvent // TODO probably not always going to be considered transient
    {
        public readonly long UID;

        /// <summary>
        /// TODO replace this with (new) <see cref="IGONetEvent.OccurredAtElapsedTicks"/>
        /// </summary>
        public long ElapsedTicksAtSend { get; internal set; }

        public RequestMessage()
        {
            UID = GUID.Generate().AsInt64();
        }

        public RequestMessage(long uid)
        {
            UID = uid;
        }
    }

    public class ResponseMessage : TransientEvent // TODO probably not always going to be considered transient
    {
        public readonly long RequestUID;

        public ResponseMessage(long requestUID)
        {
            RequestUID = requestUID;
        }
    }

    public class InstantiateGONetParticipantEvent : PersistentEvent
    {
        /// <summary>
        /// this is the information necessary to lookup the source <see cref="UnityEngine.GameObject"/> from which to use as the template in order to call <see cref="UnityEngine.Object.Instantiate(UnityEngine.Object)"/>.
        /// TODO add the persisted int->string lookup table that is updated each time a new design time location is encountered (at design time...duh)..so this can be an int!
        /// </summary>
        public string DesignTimeLocation;

        public uint GONetId;

        public uint OwnerAuthorityId;
    }
}
