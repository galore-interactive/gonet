using System;
using NetcodeIO.NET;
using ReliableNetcode;

namespace GONet
{
    public class GONetConnection_ServerToClient : ReliableEndpoint
    {
        private RemoteClient remoteClient;

        public GONetConnection_ServerToClient(RemoteClient remoteClient)
        {
            this.remoteClient = remoteClient;

            TransmitCallback = SendToMyClient_AnyLittleThingTheProtocolLayerDeemsNecessary;
        }

        private void SendToMyClient_AnyLittleThingTheProtocolLayerDeemsNecessary(byte[] payloadBytes, int payloadSize)
        {
            remoteClient.SendPayload(payloadBytes, payloadSize);
        }
    }
}