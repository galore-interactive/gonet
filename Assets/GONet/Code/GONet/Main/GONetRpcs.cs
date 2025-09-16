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

using MemoryPack;
using System;
using System.Threading.Tasks;

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
    public enum RpcType
    {
        ServerRpc,
        ClientRpc,
        TargetRpc
    }

    public enum RpcTarget
    {
        Owner,
        Others,
        All
    }

    public class RpcMetadata
    {
        public RpcType Type { get; set; }
        public bool IsReliable { get; set; }
        public bool IsMineRequired { get; set; }
        public RpcTarget Target { get; set; } // For TargetRpc
    }

    /// <summary>
    /// Context information provided to RPC methods when they include this as a parameter.
    /// </summary>
    public struct GONetRpcContext
    {
        public readonly GONetEventEnvelope Envelope;

        // For local execution without envelope
        public readonly ushort SourceAuthorityId;
        public readonly bool IsSourceRemote;
        public readonly bool IsFromMe;
        public readonly bool IsReliable;
        public readonly uint GONetParticipantId;

        // Constructor from envelope (for real RPC calls)
        internal GONetRpcContext(GONetEventEnvelope envelope)
        {
            Envelope = envelope;
            SourceAuthorityId = envelope.SourceAuthorityId;
            IsSourceRemote = envelope.IsSourceRemote;
            IsFromMe = envelope.IsFromMe;
            IsReliable = envelope.IsReliable;
            GONetParticipantId = envelope.GONetParticipant?.GONetId ?? 0;
        }

        // Constructor for local execution (no envelope)
        internal GONetRpcContext(ushort sourceAuthorityId, bool isReliable, uint gonetParticipantId)
        {
            Envelope = null;
            SourceAuthorityId = sourceAuthorityId;
            IsSourceRemote = false;
            IsFromMe = true;
            IsReliable = isReliable;
            GONetParticipantId = gonetParticipantId;
        }
    }

    /// <summary>
    /// Interface for generated RPC dispatchers. Provides type-safe dispatch methods
    /// for different parameter counts without array allocations.
    /// </summary>
    public interface IRpcDispatcher
    {
        // Synchronous dispatch methods (existing)
        void Dispatch0(object instance, string methodName);
        void Dispatch1<T1>(object instance, string methodName, T1 arg1);
        void Dispatch2<T1, T2>(object instance, string methodName, T1 arg1, T2 arg2);
        void Dispatch3<T1, T2, T3>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3);
        void Dispatch4<T1, T2, T3, T4>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        void Dispatch5<T1, T2, T3, T4, T5>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        void Dispatch6<T1, T2, T3, T4, T5, T6>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        void Dispatch7<T1, T2, T3, T4, T5, T6, T7>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        void Dispatch8<T1, T2, T3, T4, T5, T6, T7, T8>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);

        // Async dispatch methods with return types (new)
        Task<TResult> DispatchAsync0<TResult>(object instance, string methodName);
        Task<TResult> DispatchAsync1<TResult, T1>(object instance, string methodName, T1 arg1);
        Task<TResult> DispatchAsync2<TResult, T1, T2>(object instance, string methodName, T1 arg1, T2 arg2);
        Task<TResult> DispatchAsync3<TResult, T1, T2, T3>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3);
        Task<TResult> DispatchAsync4<TResult, T1, T2, T3, T4>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
        Task<TResult> DispatchAsync5<TResult, T1, T2, T3, T4, T5>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        Task<TResult> DispatchAsync6<TResult, T1, T2, T3, T4, T5, T6>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        Task<TResult> DispatchAsync7<TResult, T1, T2, T3, T4, T5, T6, T7>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        Task<TResult> DispatchAsync8<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(object instance, string methodName, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
    }

    [MemoryPackable]
    public partial struct RpcData1<T1>
    {
        public T1 Arg1 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData2<T1, T2>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData3<T1, T2, T3>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData4<T1, T2, T3, T4>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
        public T4 Arg4 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData5<T1, T2, T3, T4, T5>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
        public T4 Arg4 { get; set; }
        public T5 Arg5 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData6<T1, T2, T3, T4, T5, T6>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
        public T4 Arg4 { get; set; }
        public T5 Arg5 { get; set; }
        public T6 Arg6 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData7<T1, T2, T3, T4, T5, T6, T7>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
        public T4 Arg4 { get; set; }
        public T5 Arg5 { get; set; }
        public T6 Arg6 { get; set; }
        public T7 Arg7 { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcData8<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        public T1 Arg1 { get; set; }
        public T2 Arg2 { get; set; }
        public T3 Arg3 { get; set; }
        public T4 Arg4 { get; set; }
        public T5 Arg5 { get; set; }
        public T6 Arg6 { get; set; }
        public T7 Arg7 { get; set; }
        public T8 Arg8 { get; set; }
    }
}