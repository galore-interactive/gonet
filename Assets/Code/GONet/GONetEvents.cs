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
    }

    public abstract class TransientEvent : IGONetEvent
    {
        public bool IsTransient { get; private set; }  = true;
    }

    public abstract class PersistentEvent : IGONetEvent
    {
        public bool IsTransient { get; private set; } = false;
    }

    #endregion

    public class AutoMagicalSync_ValueChangesMessage : TransientEvent
    {
    }

    public class OwnerAuthorityIdAssignmentMessage : PersistentEvent
    {
    }

    public class RequestMessage : TransientEvent // TODO probably not always going to be considered transient
    {
        public readonly long UID;

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
}
