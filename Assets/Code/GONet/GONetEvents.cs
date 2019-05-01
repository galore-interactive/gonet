using GONet.Utils;
using System.Collections.Generic;

namespace GONet
{
    public interface IGONetEvent
    {
    }

    public class AutoMagicalSync_ValueChangesMessage : IGONetEvent
    {
    }

    public class OwnerAuthorityIdAssignmentMessage : IGONetEvent
    {
    }

    public class RequestMessage : IGONetEvent
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

    public class ResponseMessage : IGONetEvent
    {
        public readonly long RequestUID;

        public ResponseMessage(long requestUID)
        {
            RequestUID = requestUID;
        }
    }
}
