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

using GONet.Utils;
using MemoryPack;
using System;
using System.Collections.Generic;
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
    /// Marks a method as a Target RPC that can be called from client or server to route messages to specific recipients.
    /// Target RPCs support validation, message transformation, and optional delivery confirmation.
    /// </summary>
    /// <remarks>
    /// <para><b>Basic Usage:</b></para>
    /// <code>
    /// [TargetRpc(RpcTarget.Owner)]
    /// void NotifyOwner(string message) { }
    /// 
    /// [TargetRpc(RpcTarget.All)]
    /// void BroadcastToAll(string message) { }
    /// </code>
    /// 
    /// <para><b>Property-based Targeting:</b></para>
    /// <para>Target specific authorities using a property value:</para>
    /// <code>
    /// public ushort TargetPlayerId { get; set; }
    /// 
    /// [TargetRpc(nameof(TargetPlayerId))]
    /// void SendToSpecificPlayer(string message) { }
    /// </code>
    /// 
    /// <para><b>Multiple Targets:</b></para>
    /// <code>
    /// public List&lt;ushort&gt; TeamMembers { get; set; }
    /// 
    /// [TargetRpc(nameof(TeamMembers), isMultipleTargets: true)]
    /// void SendToTeam(string message) { }
    /// </code>
    /// 
    /// <para><b>Validation Methods:</b></para>
    /// <para>Add server-side validation to filter recipients and optionally transform messages:</para>
    /// <code>
    /// [TargetRpc(nameof(TeamMembers), isMultipleTargets: true, validationMethod: nameof(ValidateTeamMessage))]
    /// void SendToTeam(string message) { }
    /// 
    /// // Validation method signatures:
    /// 
    /// // Option 1: Simple bool validator (single target)
    /// private bool ValidateTeamMessage(ushort sourceAuthority, ushort targetAuthority)
    /// {
    ///     return IsTeamMember(targetAuthority);
    /// }
    /// 
    /// // Option 2: Full validation with filtering and transformation
    /// private RpcValidationResult ValidateTeamMessage(ushort sourceAuthority, ushort[] targets, int count, byte[] messageData)
    /// {
    ///     // Filter targets
    ///     var allowed = targets.Where(t => IsTeamMember(t)).ToArray();
    ///     
    ///     // Optionally modify message
    ///     var modifiedData = TransformMessage(messageData);
    ///     
    ///     return new RpcValidationResult
    ///     {
    ///         AllowedTargets = allowed,
    ///         AllowedCount = allowed.Length,
    ///         DeniedTargets = targets.Except(allowed).ToArray(),
    ///         DeniedCount = targets.Length - allowed.Length,
    ///         DenialReason = "Target is not a team member",
    ///         ModifiedData = modifiedData  // Optional
    ///     };
    /// }
    /// </code>
    /// 
    /// <para><b>Delivery Confirmation:</b></para>
    /// <para>Get confirmation of who received the RPC by using Task&lt;RpcDeliveryReport&gt; return type:</para>
    /// <code>
    /// [TargetRpc(nameof(TeamMembers), isMultipleTargets: true, validationMethod: nameof(ValidateTeam))]
    /// async Task&lt;RpcDeliveryReport&gt; SendToTeamConfirmed(string message)
    /// {
    ///     DisplayMessage(message);
    ///     return await Task.CompletedTask; // Framework handles the actual report
    /// }
    /// 
    /// // Usage:
    /// var report = await SendToTeamConfirmed("Hello team!");
    /// if (report.FailedDelivery?.Length > 0)
    /// {
    ///     Debug.Log($"Failed to deliver to: {string.Join(", ", report.FailedDelivery)}");
    ///     Debug.Log($"Reason: {report.FailureReason}");
    ///     
    ///     // Optionally get full validation details
    ///     if (report.ValidationReportId != 0)
    ///     {
    ///         var fullReport = await GetFullRpcValidationReport(report.ValidationReportId);
    ///     }
    /// }
    /// </code>
    /// 
    /// <para><b>Parameter-based Targeting:</b></para>
    /// <para>Pass target as first parameter instead of using property:</para>
    /// <code>
    /// [TargetRpc(RpcTarget.SpecificAuthority)]
    /// void SendToPlayer(ushort targetPlayerId, string message) { }
    /// 
    /// [TargetRpc(RpcTarget.MultipleAuthorities)]
    /// void SendToPlayers(List&lt;ushort&gt; targetPlayerIds, string message) { }
    /// </code>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : GONetRpcAttribute
    {
        public RpcTarget Target { get; }
        public string TargetPropertyName { get; }
        public bool IsMultipleTargets { get; }
        public string ValidationMethodName { get; }

        public TargetRpcAttribute(RpcTarget target = RpcTarget.All)
        {
            Target = target;
        }

        // Single constructor for property-based targeting
        public TargetRpcAttribute(string targetPropertyName, bool isMultipleTargets = false, string validationMethod = null)
        {
            Target = isMultipleTargets ? RpcTarget.MultipleAuthorities : RpcTarget.SpecificAuthority;
            TargetPropertyName = targetPropertyName;
            IsMultipleTargets = isMultipleTargets;
            ValidationMethodName = validationMethod;
        }
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
        All,
        SpecificAuthority,  // Single target
        MultipleAuthorities // List of targets
    }

    public class RpcMetadata
    {
        public RpcType Type { get; set; }
        public bool IsReliable { get; set; }
        public bool IsMineRequired { get; set; }
        public RpcTarget Target { get; set; } // For TargetRpc
        public string TargetPropertyName { get; set; } // For TargetRpc
        public bool IsMultipleTargets { get; set; } // For TargetRpc
        public string ValidationMethodName { get; set; } // For TargetRpc
        public bool ExpectsDeliveryReport { get; set; }
    }

    [MemoryPackable]
    public partial struct RpcDeliveryReport
    {
        public ushort[] DeliveredTo { get; set; }
        public ushort[] FailedDelivery { get; set; }
        public string FailureReason { get; set; }
        public bool WasModified { get; set; } // was the message modified before delivery
        public ulong ValidationReportId { get; set; }  // for retrieving full details, if applicable
    }

    public struct RpcValidationResult
    {
        /// <summary>
        /// Parallel array to ValidationContext.TargetAuthorities.
        /// Array indicating which targets are allowed (true) or denied (false).
        /// This array is pre-allocated by the framework to match TargetCount.
        /// </summary>
        public bool[] AllowedTargets { get; internal set; }

        /// <summary>
        /// Number of valid targets in the AllowedTargets array
        /// </summary>
        public int TargetCount { get; internal set; }

        /// <summary>
        /// Optional reason for any denials
        /// </summary>
        public string DenialReason { get; set; }

        /// <summary>
        /// Whether the validator modified any parameters
        /// </summary>
        public bool WasModified { get; set; }

        // Internal field for framework use only
        internal byte[] ModifiedData { get; set; }


        // Internal factory for framework use
        internal static RpcValidationResult CreatePreAllocated(int targetCount)
        {
            return new RpcValidationResult
            {
                AllowedTargets = RpcValidationArrayPool.BorrowAllowedTargets(),
                TargetCount = targetCount
            };
        }

        /// <summary>
        /// Sets all targets as allowed
        /// </summary>
        public void AllowAll()
        {
            for (int i = 0; i < TargetCount; i++)
                AllowedTargets[i] = true;
            DenialReason = null;
        }

        /// <summary>
        /// Sets all targets as denied
        /// </summary>
        public void DenyAll()
        {
            for (int i = 0; i < TargetCount; i++)
                AllowedTargets[i] = false;
        }

        /// <summary>
        /// Sets all targets as denied with a reason
        /// </summary>
        public void DenyAll(string reason)
        {
            DenyAll();
            DenialReason = reason;
        }

        /// <summary>
        /// Helper to allow specific targets by index
        /// </summary>
        public void AllowTarget(int index)
        {
            if (index >= 0 && index < TargetCount)
                AllowedTargets[index] = true;
        }

        /// <summary>
        /// Helper to deny specific targets by index
        /// </summary>
        public void DenyTarget(int index, string reason = null)
        {
            if (index >= 0 && index < TargetCount)
            {
                AllowedTargets[index] = false;
                if (!string.IsNullOrEmpty(reason))
                    DenialReason = reason;
            }
        }

        /// <summary>
        /// Converts bool array results to allowed targets list for delivery reports
        /// </summary>
        internal ushort[] GetAllowedTargetsList(ushort[] sourceTargets)
        {
            var allowedList = new List<ushort>();
            for (int i = 0; i < TargetCount && i < sourceTargets.Length; i++)
            {
                if (AllowedTargets[i])
                    allowedList.Add(sourceTargets[i]);
            }
            return allowedList.ToArray();
        }

        /// <summary>
        /// Converts bool array results to denied targets list for delivery reports
        /// </summary>
        internal ushort[] GetDeniedTargetsList(ushort[] sourceTargets)
        {
            var deniedList = new List<ushort>();
            for (int i = 0; i < TargetCount && i < sourceTargets.Length; i++)
            {
                if (!AllowedTargets[i])
                    deniedList.Add(sourceTargets[i]);
            }
            return deniedList.ToArray();
        }
    }

    internal static class RpcValidationArrayPool
    {
        private static readonly ArrayPool<bool> boolArrayPool = new(10, 2, GONetEventBus.MAX_RPC_TARGETS, GONetEventBus.MAX_RPC_TARGETS);

        internal static bool[] BorrowAllowedTargets()
        {
            return boolArrayPool.Borrow();
        }

        internal static void ReturnAllowedTargets(bool[] array)
        {
            boolArrayPool.Return(array);
        }
    }

    #region event Support

    [MemoryPackable]
    public partial class RpcEvent : ITransientEvent, ISelfReturnEvent
    {
        private static readonly ObjectPool<RpcEvent> rpcEventPool = new(100, 10);
        public long OccurredAtElapsedTicks { get; set; }
        public uint RpcId { get; set; }
        public uint GONetId { get; set; }
        public byte[] Data { get; set; }
        public long CorrelationId { get; set; } // For request-response
        public bool IsSingularRecipientOnly { get; set; }

        internal static RpcEvent Borrow()
        {
            return rpcEventPool.Borrow();
        }

        public void Return()
        {
            Return(this);
        }

        private static void Return(RpcEvent evt)
        {
            SerializationUtils.TryReturnByteArray(evt.Data); // may or may not be borrowed from there

            evt.OccurredAtElapsedTicks = 0;
            evt.RpcId = 0;
            evt.GONetId = 0;
            evt.Data = null;
            evt.CorrelationId = 0;
            evt.IsSingularRecipientOnly = false;

            rpcEventPool.TryReturn(evt);  // using try return here because stuff coming over the network is created fresh from message pack or memory pack (they just know it up, they don't borrow it). Some of these are just not going to be able to be returned successfully when they're coming across the wire as opposed to when we're controlling it by creating the new event and then publishing it initially.
        }
    }

    [MemoryPackable]
    public partial class RpcResponseEvent : ITransientEvent, ISelfReturnEvent
    {
        private static readonly ObjectPool<RpcResponseEvent> rpcResponsePool = new(100, 10);
        public long OccurredAtElapsedTicks { get; set; }
        public long CorrelationId { get; set; }
        public byte[] Data { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSingularRecipientOnly => true;

        internal static RpcResponseEvent Borrow()
        {
            return rpcResponsePool.Borrow();
        }

        public void Return()
        {
            Return(this);
        }

        private static void Return(RpcResponseEvent evt)
        {
            SerializationUtils.TryReturnByteArray(evt.Data); // may or may not be borrowed from there

            evt.OccurredAtElapsedTicks = 0;
            evt.CorrelationId = 0;
            evt.Data = null;
            evt.Success = false;
            evt.ErrorMessage = null;

            rpcResponsePool.TryReturn(evt);  // using try return here because stuff coming over the network is created fresh from message pack or memory pack (they just know it up, they don't borrow it). Some of these are just not going to be able to be returned successfully when they're coming across the wire as opposed to when we're controlling it by creating the new event and then publishing it initially.
        }
    }

    [MemoryPackable]
    public partial class RoutedRpcEvent : ITransientEvent, ISelfReturnEvent
    {
        private static readonly ObjectPool<RoutedRpcEvent> routedRpcEventPool = new(50, 10);

        public long OccurredAtElapsedTicks { get; set; }
        public uint RpcId { get; set; }
        public uint GONetId { get; set; }
        public ushort[] TargetAuthorities { get; set; } = new ushort[64];
        public int TargetCount { get; set; }
        public byte[] Data { get; set; }
        public long CorrelationId { get; set; }

        internal static RoutedRpcEvent Borrow()
        {
            return routedRpcEventPool.Borrow();
        }

        public void Return()
        {
            Return(this);
        }

        private static void Return(RoutedRpcEvent evt)
        {
            SerializationUtils.TryReturnByteArray(evt.Data);

            evt.OccurredAtElapsedTicks = 0;
            evt.RpcId = 0;
            evt.GONetId = 0;
            evt.TargetCount = 0;
            evt.Data = null;
            evt.CorrelationId = 0;
            // Don't null TargetAuthorities array, just reset count

            routedRpcEventPool.TryReturn(evt);  // using try return here because stuff coming over the network is created fresh from message pack or memory pack (they just know it up, they don't borrow it). Some of these are just not going to be able to be returned successfully when they're coming across the wire as opposed to when we're controlling it by creating the new event and then publishing it initially.
        }
    }

    [MemoryPackable]
    public partial class RpcDeliveryReportEvent : ITransientEvent, ISelfReturnEvent
    {
        private static readonly ObjectPool<RpcDeliveryReportEvent> pool = new(50, 10);

        public long OccurredAtElapsedTicks { get; set; }
        public RpcDeliveryReport Report { get; set; }
        public long CorrelationId { get; set; }

        internal static RpcDeliveryReportEvent Borrow()
        {
            return pool.Borrow();
        }

        public void Return()
        {
            Return(this);
        }

        private static void Return(RpcDeliveryReportEvent evt)
        {
            evt.OccurredAtElapsedTicks = 0;
            evt.Report = default;
            evt.CorrelationId = 0;
            pool.Return(evt);
        }
    }

    #endregion

    /// <summary>
    /// Context information available to RPC methods through GONetEventBus.CurrentRpcContext.
    /// Contains information about the RPC caller, reliability, and correlation.
    /// </summary>
    /// <remarks>
    /// Access this in your RPC method like:
    /// <code>
    /// [ServerRpc]
    /// void MyMethod()
    /// {
    ///     var context = GONetEventBus.CurrentRpcContext;
    ///     if (context.HasValue)
    ///     {
    ///         var callerAuthority = context.Value.SourceAuthorityId;
    ///         // Use context information
    ///     }
    /// }
    /// </code>
    /// Note: GONetRpcContext should NOT be included as an RPC method parameter!
    /// </remarks>
    public struct GONetRpcContext
    {
        public readonly GONetEventEnvelope Envelope;

        // For local execution without envelope
        public readonly ushort SourceAuthorityId;
        public readonly bool IsSourceRemote;
        public readonly bool IsFromMe;
        public readonly bool IsReliable;
        public readonly uint GONetParticipantId;
        public RpcValidationContext ValidationContext { get; internal set; }

        // Constructor from envelope (for real RPC calls)
        internal GONetRpcContext(GONetEventEnvelope envelope)
        {
            Envelope = envelope;
            SourceAuthorityId = envelope.SourceAuthorityId;
            IsSourceRemote = envelope.IsSourceRemote;
            IsFromMe = envelope.IsFromMe;
            IsReliable = envelope.IsReliable;
            GONetParticipantId = envelope.GONetParticipant?.GONetId ?? 0;
            ValidationContext = default;
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
            ValidationContext = default;
        }
    }

    /// <summary>
    /// Context available during RPC validation containing source and target information.
    /// Access via GONetEventBus.CurrentRpcContext.Value.ValidationContext
    /// </summary>
    public struct RpcValidationContext
    {
        public ushort SourceAuthorityId { get; set; }
        public ushort[] TargetAuthorityIds { get; set; }
        public int TargetCount { get; set; }
        internal RpcValidationResult PreAllocatedResult { get; set; } // Internal only

        /// <summary>
        /// Gets the pre-allocated validation result for modification.
        /// The bool array is already sized to TargetCount.
        /// </summary>
        public RpcValidationResult GetValidationResult()
        {
            return PreAllocatedResult;
        }
    }

    internal interface IResponseHandler
    {
        void HandleResponse(RpcResponseEvent response);
    }

    internal class DeliveryReportHandler : IResponseHandler
    {
        private readonly TaskCompletionSource<RpcDeliveryReport> tcs;

        public DeliveryReportHandler(TaskCompletionSource<RpcDeliveryReport> tcs)
        {
            this.tcs = tcs;
        }

        public void HandleResponse(RpcResponseEvent response)
        {
            // This shouldn't be used for delivery reports
            // They come through RpcDeliveryReportEvent instead
            tcs.TrySetException(new Exception("Unexpected response type for delivery report"));
        }

        public void HandleDeliveryReport(RpcDeliveryReportEvent evt)
        {
            tcs.TrySetResult(evt.Report);
        }
    }

    internal class ResponseHandler<T> : IResponseHandler
    {
        private readonly TaskCompletionSource<T> tcs;

        public ResponseHandler(TaskCompletionSource<T> tcs)
        {
            this.tcs = tcs;
        }

        public void HandleResponse(RpcResponseEvent response)
        {
            if (response.Success && response.Data != null)
            {
                var result = SerializationUtils.DeserializeFromBytes<T>(response.Data);
                tcs.TrySetResult(result);
            }
            else
            {
                tcs.TrySetException(new Exception($"RPC failed: {response.ErrorMessage ?? "Unknown error"}"));
            }
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