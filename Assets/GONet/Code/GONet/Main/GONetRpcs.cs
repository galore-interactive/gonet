/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;

namespace GONet
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class GONetRpcAttribute : Attribute
    {
        public bool IsMineRequired { get; set; } = false;
        public bool IsReliable { get; set; } = true;
        public bool IsPersistent { get; set; } = false;
    }

    /// <summary>
    /// Client to server RPC with possible relay to other clients as well.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : GONetRpcAttribute
    {
        public RelayMode Relay { get; set; } = RelayMode.None;

        public ServerRpcAttribute()
        {
            IsMineRequired = true; // Safe default
        }
    }

    /// <summary>
    /// Server to all clients RPC
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : GONetRpcAttribute { }

    /// <summary>
    /// Server to specific target client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : GONetRpcAttribute
    {
        public RpcTarget Target { get; set; } = RpcTarget.Owner;
    }

    public enum RelayMode { None, Others, All, Owner }
    public enum RpcTarget { Owner, Others, All }

    /// <summary>
    /// Context information provided to RPC methods when they include this as a parameter.
    /// </summary>
    public struct GONetRpcContext
    {
        public readonly GONetEventEnvelope Envelope;

        internal GONetRpcContext(GONetEventEnvelope envelope)
        {
            Envelope = envelope;
        }

        # region Convenience properties
        public ushort SourceAuthorityId => Envelope.SourceAuthorityId;
        public ushort TargetClientAuthorityId => Envelope.TargetClientAuthorityId;
        public bool IsReliable => Envelope.IsReliable;
        public bool IsSourceRemote => Envelope.IsSourceRemote;
        public bool IsFromMe => Envelope.IsFromMe;
        public GONetParticipant GONetParticipant => Envelope.GONetParticipant;
        #endregion
    }
}