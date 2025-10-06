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
    /// <summary>
    /// Base class for all GONet RPC attributes. Provides common configuration options for RPC behavior.
    /// </summary>
    /// <remarks>
    /// This abstract class defines the fundamental properties that control how RPCs are transmitted and processed:
    /// <list type="bullet">
    /// <item><description><b>IsMineRequired</b>: Whether the RPC can only be called on objects owned by the caller</description></item>
    /// <item><description><b>IsReliable</b>: Whether the RPC uses reliable UDP transmission (guaranteed delivery)</description></item>
    /// <item><description><b>IsPersistent</b>: Whether the RPC is stored and sent to late-joining clients</description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class GONetRpcAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether this RPC can only be called on objects owned by the caller.
        /// When true, prevents clients from calling RPCs on objects they don't own.
        /// Default: false (allows calls on any object)
        /// </summary>
        public bool IsMineRequired { get; set; } = false;

        /// <summary>
        /// Gets or sets whether this RPC uses reliable UDP transmission.
        /// Reliable RPCs are guaranteed to arrive but may have higher latency.
        /// Default: true (reliable transmission)
        /// </summary>
        public bool IsReliable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether this RPC is stored and automatically sent to late-joining clients.
        /// Useful for state-setting RPCs that new players need to receive.
        /// Default: false (not persistent)
        /// </summary>
        public bool IsPersistent { get; set; } = false;
    }

    /// <summary>
    /// Marks a method as a Server RPC that can be called by clients to execute on the server.
    /// Optionally supports relaying the call to other clients after server processing.
    /// </summary>
    /// <remarks>
    /// <para><b>Basic Server RPC (Client → Server only):</b></para>
    /// <code>
    /// [ServerRpc]
    /// void RequestPickupItem(int itemId)
    /// {
    ///     // This runs on the server when called by a client
    ///     if (CanPickupItem(itemId))
    ///     {
    ///         GiveItemToPlayer(itemId);
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Server RPC with Relay (Client → Server → All Clients):</b></para>
    /// <code>
    /// [ServerRpc(Relay = RelayMode.All)]
    /// void BroadcastPlayerAction(string action)
    /// {
    ///     // Server validates, then relays to all clients
    ///     if (IsValidAction(action))
    ///     {
    ///         // This will be sent to all clients after server processing
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Security Note:</b></para>
    /// <para>ServerRpcs have <c>IsMineRequired = true</c> by default for security.
    /// Clients can only call ServerRpcs on objects they own unless explicitly disabled.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : GONetRpcAttribute
    {
        /// <summary>
        /// Gets or sets whether the server should relay this RPC to other clients after processing.
        /// Default: RelayMode.None (server only, no relay to clients)
        /// </summary>
        public RelayMode Relay { get; set; } = RelayMode.None;

        /// <summary>
        /// Initializes a new ServerRpcAttribute with secure defaults.
        /// Sets IsMineRequired = true to prevent unauthorized access to other players' objects.
        /// </summary>
        public ServerRpcAttribute()
        {
            IsMineRequired = true; // Safe default - clients can only call on objects they own
        }
    }

    /// <summary>
    /// Marks a method as a Client RPC that can be called by the server to execute on all connected clients.
    /// Client RPCs are typically used to update client state, play effects, or synchronize game events.
    /// </summary>
    /// <remarks>
    /// <para><b>Basic Client RPC Usage:</b></para>
    /// <code>
    /// [ClientRpc]
    /// void ShowExplosionEffect(Vector3 position, float radius)
    /// {
    ///     // This runs on all clients when called by the server
    ///     PlayExplosionAnimation(position, radius);
    ///     PlayExplosionSound(position);
    /// }
    ///
    /// // Call from server code:
    /// CallRpc(nameof(ShowExplosionEffect), explosionPos, blastRadius);
    /// </code>
    ///
    /// <para><b>State Synchronization Example:</b></para>
    /// <code>
    /// [ClientRpc]
    /// void UpdatePlayerHealth(int newHealth, int maxHealth)
    /// {
    ///     // Update UI on all clients
    ///     healthBar.SetValue(newHealth, maxHealth);
    ///
    ///     if (newHealth &lt;= 0)
    ///         ShowDeathEffect();
    /// }
    /// </code>
    ///
    /// <para><b>Persistent Client RPCs for Late-Joining Clients:</b></para>
    /// <code>
    /// [ClientRpc(IsPersistent = true)]
    /// void UpdateGameState(GameState newState)
    /// {
    ///     // This RPC is stored and automatically sent to late-joining clients
    ///     currentGameState = newState;
    ///     RefreshUI();
    /// }
    /// </code>
    ///
    /// <para><b>Key Characteristics:</b></para>
    /// <list type="bullet">
    /// <item><description>Only the server can call Client RPCs</description></item>
    /// <item><description>Sent to all connected clients automatically</description></item>
    /// <item><description>Ideal for visual effects, UI updates, and state synchronization</description></item>
    /// <item><description>Uses reliable transmission by default (IsReliable = true)</description></item>
    /// <item><description>Set IsPersistent = true to automatically deliver to late-joining clients</description></item>
    /// </list>
    /// </remarks>
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
    ///
    /// <para><b>Persistent Target RPCs - ⚠️ IMPORTANT LIMITATIONS:</b></para>
    /// <para>Persistent TargetRPCs store original recipient authority IDs. Late-joining clients may not receive them if their ID wasn't in the original target list.</para>
    /// <code>
    /// // ✅ GOOD - All clients receive persistent state
    /// [TargetRpc(RpcTarget.All, IsPersistent = true)]
    /// void AnnounceGamePhaseChange(GamePhase newPhase)
    /// {
    ///     // Late-joiners will receive this persistent state update
    ///     currentGamePhase = newPhase;
    /// }
    ///
    /// // ❌ PROBLEMATIC - Late-joiners may be excluded
    /// [TargetRpc(nameof(ActivePlayerIds), isMultipleTargets: true, IsPersistent = true)]
    /// void SendRoundResults(ScoreData scores)
    /// {
    ///     // Late-joiners won't be in the original ActivePlayerIds list!
    /// }
    ///
    /// // ⚠️ USE WITH CAUTION - Depends on property logic
    /// [TargetRpc(nameof(TeamLeaderId), IsPersistent = true)]
    /// void NotifyTeamLeader(string message)
    /// {
    ///     // Only works if TeamLeaderId property includes late-joiners appropriately
    /// }
    /// </code>
    /// <para><b>Recommendation:</b> For persistent TargetRPCs, prefer RpcTarget.All or ensure your targeting logic accounts for late-joining clients.</para>
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
        public bool IsPersistent { get; set; } // Whether RPC should persist for late-joining clients
        public RpcTarget Target { get; set; } // For TargetRpc
        public string TargetPropertyName { get; set; } // For TargetRpc
        public bool IsMultipleTargets { get; set; } // For TargetRpc
        public string ValidationMethodName { get; set; } // For TargetRpc
        public bool ExpectsDeliveryReport { get; set; }
    }

    /// <summary>
    /// Contains delivery status information for TargetRpc calls with async return types.
    /// Provides detailed feedback about which recipients received the RPC and any failures that occurred.
    /// </summary>
    /// <remarks>
    /// This structure is returned when calling TargetRpc methods with Task&lt;RpcDeliveryReport&gt; return type:
    /// <code>
    /// [TargetRpc(nameof(TeamMembers), isMultipleTargets: true)]
    /// async Task&lt;RpcDeliveryReport&gt; SendToTeam(string message)
    /// {
    ///     DisplayMessage(message);
    ///     return default; // Framework populates the actual report
    /// }
    ///
    /// // Usage:
    /// var report = await SendToTeam("Hello!");
    /// if (report.FailedDelivery?.Length > 0)
    /// {
    ///     Debug.LogWarning($"Failed to deliver to {report.FailedDelivery.Length} recipients");
    ///     Debug.LogWarning($"Reason: {report.FailureReason}");
    /// }
    /// </code>
    /// </remarks>
    [MemoryPackable]
    public partial struct RpcDeliveryReport
    {
        /// <summary>
        /// Array of authority IDs that successfully received the RPC.
        /// Null if no recipients or if delivery tracking was not requested.
        /// </summary>
        public ushort[] DeliveredTo { get; set; }

        /// <summary>
        /// Array of authority IDs that failed to receive the RPC.
        /// Common causes include disconnected clients, validation failures, or network issues.
        /// </summary>
        public ushort[] FailedDelivery { get; set; }

        /// <summary>
        /// Human-readable description of why delivery failed for some recipients.
        /// Examples: "Target disconnected", "Validation denied", "Network timeout"
        /// </summary>
        public string FailureReason { get; set; }

        /// <summary>
        /// Indicates whether the RPC data was modified by validation before delivery.
        /// True when validation methods modify ref parameters (e.g., content filtering).
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Unique identifier for retrieving detailed validation report if available.
        /// Can be used with GetFullRpcValidationReport() for debugging complex validation scenarios.
        /// Zero if no detailed report is available.
        /// </summary>
        public ulong ValidationReportId { get; set; }

        /// <summary>
        /// <para>Indicates that the RPC was validated but requires asynchronous processing before completion.</para>
        /// <para>When true, the caller should expect a follow-on response RPC from the target.</para>
        ///
        /// <para><b>Use Case - Async Approval Pattern:</b></para>
        /// <para>This flag enables server-side approval workflows where validation allows the RPC through,
        /// but actual execution is deferred until manual approval (e.g., admin confirmation, user interaction).</para>
        ///
        /// <para><b>Example - Scene Change Approval:</b></para>
        /// <code>
        /// // Client requests scene change
        /// var report = await RPC_RequestLoadScene("NewScene");
        /// if (report.ExpectFollowOnResponse)
        /// {
        ///     // Server is processing request asynchronously
        ///     // Wait for RPC_SceneRequestResponse RPC with approval/denial
        /// }
        ///
        /// // Server validation
        /// RpcValidationResult Validate_RequestLoadScene(...)
        /// {
        ///     result.AllowAll(); // Let RPC through
        ///     result.ExpectFollowOnResponse = true; // Signal async processing
        ///     return result;
        /// }
        ///
        /// // Server RPC handler shows approval UI, then sends response
        /// void RPC_RequestLoadScene(...)
        /// {
        ///     ShowApprovalDialog();
        /// }
        ///
        /// // After approval, server sends response
        /// void RPC_SceneRequestResponse(ushort clientId, bool approved, ...)
        /// {
        ///     // Client receives definitive approval/denial
        /// }
        /// </code>
        ///
        /// <para>See GONet scene management sample for complete implementation.</para>
        /// </summary>
        public bool ExpectFollowOnResponse { get; set; }
    }

    /// <summary>
    /// Represents the result of RPC validation, including which targets are allowed/denied.
    /// Implements IDisposable to properly return pooled arrays to avoid memory leaks.
    /// </summary>
    public struct RpcValidationResult : IDisposable
    {
        /// <summary>
        /// Parallel array to ValidationContext.TargetAuthorities.
        /// Array indicating which targets are allowed (true) or denied (false).
        /// This array is pre-allocated by the framework to match TargetCount.
        /// IMPORTANT: This array is pooled and will be automatically returned when disposed.
        /// </summary>
        public bool[] AllowedTargets { get; internal set; }

        /// <summary>
        /// Number of valid targets in the AllowedTargets array
        /// </summary>
        public int TargetCount { get; internal set; }

        /// <summary>
        /// Optional reason for any denials. Used for logging and delivery reports.
        /// </summary>
        public string DenialReason { get; set; }

        /// <summary>
        /// Whether the validator modified any parameters.
        /// When true, the framework will use ModifiedData for the RPC call.
        /// </summary>
        public bool WasModified { get; set; }

        /// <summary>
        /// Internal field for framework use only - contains serialized modified parameters
        /// </summary>
        internal byte[] ModifiedData { get; set; }

        /// <summary>
        /// <para>Signals that this RPC requires asynchronous processing and the caller should expect a follow-on response.</para>
        /// <para>When set to true, this flag is automatically propagated to the RpcDeliveryReport.</para>
        ///
        /// <para><b>Usage in Validators:</b></para>
        /// <code>
        /// RpcValidationResult Validate_MyAsyncRpc(ref string data)
        /// {
        ///     var result = context.GetValidationResult();
        ///     result.AllowAll(); // Allow RPC through
        ///     result.ExpectFollowOnResponse = true; // Signal async processing to caller
        ///     return result;
        /// }
        /// </code>
        ///
        /// <para>The caller will receive this flag in the delivery report and can await a separate response RPC.</para>
        /// <para>See RpcDeliveryReport.ExpectFollowOnResponse for complete example.</para>
        /// </summary>
        public bool ExpectFollowOnResponse { get; set; }

        /// <summary>
        /// Internal flag to track disposal state and prevent double-disposal
        /// </summary>
        private bool _disposed;


        /// <summary>
        /// Internal factory method for creating pre-allocated validation results.
        /// Used by the GONet framework to create validation results with pooled arrays.
        /// </summary>
        /// <param name="targetCount">Number of targets to validate (must be positive and within limits)</param>
        /// <returns>A new RpcValidationResult with pre-allocated arrays</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if targetCount is invalid</exception>
        internal static RpcValidationResult CreatePreAllocated(int targetCount)
        {
            if (targetCount < 0)
                throw new ArgumentOutOfRangeException(nameof(targetCount), "Target count cannot be negative.");
            if (targetCount > GONetEventBus.MAX_RPC_TARGETS)
                throw new ArgumentOutOfRangeException(nameof(targetCount), $"Target count {targetCount} exceeds maximum allowed ({GONetEventBus.MAX_RPC_TARGETS}).");

            var allowedTargets = RpcValidationArrayPool.BorrowAllowedTargets();
            if (allowedTargets == null)
            {
                throw new InvalidOperationException("Failed to borrow array from pool. Array pool may be exhausted.");
            }

            // Clear the array to ensure clean state
            for (int i = 0; i < Math.Min(targetCount, allowedTargets.Length); i++)
            {
                allowedTargets[i] = false;
            }

            return new RpcValidationResult
            {
                AllowedTargets = allowedTargets,
                TargetCount = targetCount,
                _disposed = false
            };
        }

        /// <summary>
        /// Disposes the validation result, returning pooled arrays to prevent memory leaks.
        /// This is automatically called by the framework after validation processing.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed && AllowedTargets != null)
            {
                RpcValidationArrayPool.ReturnAllowedTargets(AllowedTargets);
                AllowedTargets = null;
                _disposed = true;
            }
        }

        /// <summary>
        /// Sets all targets as allowed
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the validation result has been disposed</exception>
        /// <exception cref="InvalidOperationException">Thrown if AllowedTargets array is null or invalid</exception>
        public void AllowAll()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));
            if (AllowedTargets == null) throw new InvalidOperationException("AllowedTargets array is null. Validation result may not be properly initialized.");
            if (TargetCount < 0 || TargetCount > AllowedTargets.Length)
                throw new InvalidOperationException($"Invalid TargetCount {TargetCount}. Must be between 0 and {AllowedTargets?.Length ?? 0}.");

            for (int i = 0; i < TargetCount; i++)
                AllowedTargets[i] = true;
            DenialReason = null;
        }

        /// <summary>
        /// Sets all targets as denied
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the validation result has been disposed</exception>
        /// <exception cref="InvalidOperationException">Thrown if AllowedTargets array is null or invalid</exception>
        public void DenyAll()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));
            if (AllowedTargets == null) throw new InvalidOperationException("AllowedTargets array is null. Validation result may not be properly initialized.");
            if (TargetCount < 0 || TargetCount > AllowedTargets.Length)
                throw new InvalidOperationException($"Invalid TargetCount {TargetCount}. Must be between 0 and {AllowedTargets?.Length ?? 0}.");

            for (int i = 0; i < TargetCount; i++)
                AllowedTargets[i] = false;
        }

        /// <summary>
        /// Sets all targets as denied with a reason
        /// </summary>
        /// <param name="reason">Reason for denial, used in logging and delivery reports</param>
        public void DenyAll(string reason)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));

            DenyAll();
            DenialReason = reason;
        }

        /// <summary>
        /// Helper to allow specific targets by index
        /// </summary>
        /// <param name="index">Index in the TargetAuthorities array to allow</param>
        /// <exception cref="ObjectDisposedException">Thrown if the validation result has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is out of valid range</exception>
        /// <exception cref="InvalidOperationException">Thrown if AllowedTargets array is null</exception>
        public void AllowTarget(int index)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));
            if (AllowedTargets == null) throw new InvalidOperationException("AllowedTargets array is null. Validation result may not be properly initialized.");
            if (index < 0 || index >= TargetCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {TargetCount - 1}.");

            AllowedTargets[index] = true;
        }

        /// <summary>
        /// Helper to deny specific targets by index
        /// </summary>
        /// <param name="index">Index in the TargetAuthorities array to deny</param>
        /// <param name="reason">Optional reason for denial</param>
        /// <exception cref="ObjectDisposedException">Thrown if the validation result has been disposed</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if index is out of valid range</exception>
        /// <exception cref="InvalidOperationException">Thrown if AllowedTargets array is null</exception>
        public void DenyTarget(int index, string reason = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));
            if (AllowedTargets == null) throw new InvalidOperationException("AllowedTargets array is null. Validation result may not be properly initialized.");
            if (index < 0 || index >= TargetCount)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range is 0 to {TargetCount - 1}.");

            AllowedTargets[index] = false;
            if (!string.IsNullOrEmpty(reason))
                DenialReason = reason;
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

    /// <summary>
    /// Internal array pool for RPC validation bool arrays to reduce garbage collection.
    /// Automatically manages allocation and deallocation of bool arrays used in validation results.
    /// </summary>
    internal static class RpcValidationArrayPool
    {
        /// <summary>
        /// Pool of bool arrays sized for maximum RPC targets.
        /// Configured with reasonable defaults for typical RPC validation scenarios.
        /// </summary>
        private static readonly ArrayPool<bool> boolArrayPool = new(10, 2, GONetEventBus.MAX_RPC_TARGETS, GONetEventBus.MAX_RPC_TARGETS);

        /// <summary>
        /// Borrows a bool array from the pool for use in RpcValidationResult.
        /// The array is automatically cleared and ready for use.
        /// IMPORTANT: Always return the array via ReturnAllowedTargets when done.
        /// </summary>
        /// <returns>A clean bool array sized for maximum RPC targets</returns>
        internal static bool[] BorrowAllowedTargets()
        {
            return boolArrayPool.Borrow();
        }

        /// <summary>
        /// Returns a bool array to the pool for reuse.
        /// Called automatically by RpcValidationResult.Dispose().
        /// </summary>
        /// <param name="array">The array to return to the pool</param>
        internal static void ReturnAllowedTargets(bool[] array)
        {
            boolArrayPool.Return(array);
        }
    }

    #region event Support

    // ========================================
    // RPC EVENT ARCHITECTURE DOCUMENTATION
    // ========================================
    //
    // GONet implements a dual-architecture RPC event system to handle both transient and persistent RPCs:
    //
    // TRANSIENT EVENTS (IsPersistent = false):
    // - RpcEvent: Standard RPCs, implements ITransientEvent + ISelfReturnEvent
    // - RoutedRpcEvent: TargetRPC routing, implements ITransientEvent + ISelfReturnEvent
    // - Uses object pooling for high-performance, low-GC operation
    // - Events are processed immediately and returned to pool
    // - Not stored for late-joining clients
    //
    // PERSISTENT EVENTS (IsPersistent = true):
    // - PersistentRpcEvent: Persistent RPCs, implements IPersistentEvent ONLY
    // - PersistentRoutedRpcEvent: Persistent TargetRPC routing, implements IPersistentEvent ONLY
    // - INTENTIONALLY does NOT use pooling to prevent data corruption
    // - Events are stored by reference for late-joining clients
    // - Data integrity prioritized over memory efficiency
    // - ⚠️ TARGET RPC LIMITATION: Late-joining clients may not receive TargetRPCs if their
    //   authority ID wasn't in the original TargetAuthorities[] array when the RPC was sent
    //
    // CRITICAL DESIGN CONSTRAINT FOR FUTURE DEVELOPERS:
    // Persistent events must NOT implement ISelfReturnEvent because GONet stores direct
    // references to these events for late-joining client delivery (see GONet.cs:651
    // persistentEventsThisSession and OnPersistentEvent_KeepTrack:1549). If pooled:
    //
    //   DISASTER SCENARIO:
    //   1. Persistent RPC created with critical state (e.g., player team assignment)
    //   2. Event stored by reference in persistentEventsThisSession for late-joiners
    //   3. Event.Return() called → data cleared/corrupted, returned to pool
    //   4. Pool reuses object for different RPC → overwrites stored reference's data
    //   5. Late-joiner connects minutes/hours later
    //   6. Server serializes persistentEventsThisSession to sync late-joiner
    //   7. CATASTROPHIC: Late-joiner receives corrupted/wrong data from stored reference
    //   8. Result: Game-breaking bugs (wrong teams, missing state, crashes)
    //
    // Memory cost is acceptable (~48 bytes per persistent RPC, typically 1-10 KB per session)
    // for the guarantee of data integrity. Persistent RPCs are infrequent (setup/config)
    // where safety dramatically outweighs the tiny memory cost.
    //
    // This pattern applies to ALL persistent events in GONet:
    // - PersistentRpcEvent (this file)
    // - InstantiateGONetParticipantEvent (spawn events)
    // - DespawnGONetParticipantEvent (despawn with cancellation)
    // - SceneLoadEvent (scene transitions)
    //
    // DO NOT add ISelfReturnEvent to any class that implements IPersistentEvent!
    // ========================================

    /// <summary>
    /// Transient RPC event with object pooling for high-frequency operations.
    ///
    /// POOLING RATIONALE: Safe to pool because these events are NEVER stored.
    /// They execute immediately and return to the pool within milliseconds.
    ///
    /// Performance characteristics:
    /// - Frequency: 100-1000+ RPCs per second (movement, combat, frequent actions)
    /// - Lifetime: Single frame (borrowed → executed → returned in ~1-5ms)
    /// - Memory impact WITHOUT pooling: 5-50 MB/sec of GC pressure
    /// - Memory impact WITH pooling: ~5-10 KB pooled (100 objects * ~50 bytes)
    ///
    /// Contrast with PersistentRpcEvent (no pooling):
    /// - Frequency: 1-10 RPCs per minute (setup, config, rare state changes)
    /// - Lifetime: Entire session (stored for late-joiners, hours)
    /// - Memory cost: 1-10 KB per session (acceptable for data integrity)
    /// </summary>
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

    /// <summary>
    /// Persistent version of RpcEvent that gets stored and sent to late-joining clients.
    /// Used for ClientRpc calls with IsPersistent = true.
    ///
    /// ⚠️  CRITICAL: NO OBJECT POOLING - DO NOT add ISelfReturnEvent interface!
    ///
    /// WHY NO POOLING:
    /// This class is intentionally NOT pooled to prevent catastrophic data corruption.
    /// GONet stores these events by REFERENCE in persistentEventsThisSession (GONet.cs:651)
    /// for the entire session duration. When late-joining clients connect (minutes or hours
    /// later), these stored references are serialized and transmitted.
    ///
    /// If this class were pooled (like RpcEvent):
    ///   1. Event created: { RpcId=0x12345, GONetId=3071, Data="TeamRed" }
    ///   2. Stored by reference in persistentEventsThisSession
    ///   3. Event.Return() called → data cleared: { RpcId=0, GONetId=0, Data=null }
    ///   4. Pool reuses object for different RPC → overwrites: { RpcId=0xABCDE, Data="TeamBlue" }
    ///   5. Late-joiner connects 30 minutes later
    ///   6. Server serializes persistentEventsThisSession (includes corrupted reference)
    ///   7. Late-joiner receives WRONG data: TeamBlue instead of TeamRed
    ///   8. RESULT: Game-breaking state desynchronization, invisible bugs, crashes
    ///
    /// MEMORY COST vs BENEFIT:
    /// - Cost: ~48 bytes per persistent RPC × 10-200 events = 1-10 KB per session
    /// - Benefit: 100% guarantee of data integrity for late-joining clients
    /// - Trade-off: Trivial memory cost for critical correctness
    ///
    /// USAGE GUIDANCE FOR DEVELOPERS:
    /// - Use persistent RPCs for: Setup, configuration, rare state changes that late-joiners need
    /// - DON'T use for: High-frequency updates (movement, combat) - use regular RpcEvent
    /// - Typical frequency: 1-10 persistent RPCs per minute vs 100-1000 transient RPCs per second
    ///
    /// This pattern is consistent across ALL persistent events in GONet:
    /// - PersistentRpcEvent (this class)
    /// - PersistentRoutedRpcEvent (TargetRpc variant)
    /// - InstantiateGONetParticipantEvent (spawn events)
    /// - DespawnGONetParticipantEvent (despawn with cancellation logic)
    /// - SceneLoadEvent (networked scene management)
    ///
    /// IF YOU'RE TEMPTED TO ADD POOLING: Don't! The memory savings are negligible (~10 KB)
    /// and the risk of data corruption is catastrophic. This design choice has been thoroughly
    /// validated through production use and is architecturally required by GONet's persistence
    /// mechanism. See GONet.cs:OnPersistentEvent_KeepTrack() for the storage implementation.
    /// </summary>
    [MemoryPackable]
    public partial class PersistentRpcEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks { get; set; }
        public uint RpcId { get; set; }
        public uint GONetId { get; set; }
        public byte[] Data { get; set; }
        public long CorrelationId { get; set; }
        public bool IsSingularRecipientOnly { get; set; }

        /// <summary>
        /// Original target specification for validation when sending to late-joining clients
        /// </summary>
        public RpcTarget OriginalTarget { get; set; } = RpcTarget.All;

        /// <summary>
        /// Source authority that originally sent this RPC
        /// </summary>
        public ushort SourceAuthorityId { get; set; }
    }

    /// <summary>
    /// Represents a persistent RPC event that targets specific authorities.
    /// Used when TargetRpc calls are marked as persistent (IsPersistent = true) to ensure
    /// late-joining clients receive targeted messages appropriately.
    ///
    /// ⚠️  CRITICAL LIMITATION FOR LATE-JOINING CLIENTS:
    /// Persistent TargetRPCs store the exact authority IDs that were valid when originally sent.
    /// Late-joining clients will receive the persistent event, but their authority ID won't be
    /// in the original TargetAuthorities[] array, so they may not process the RPC.
    ///
    /// RECOMMENDED PATTERNS FOR PERSISTENT TARGET RPCS:
    /// ✅ Use RpcTarget.All for truly persistent state that all clients need
    /// ✅ Use property-based targeting where the property naturally includes late-joiners
    /// ❌ Avoid parameter-based specific authority targeting with persistence
    /// ❌ Avoid targeting lists that exclude future clients
    ///
    /// EXAMPLE - GOOD (All clients should know about team changes):
    /// [TargetRpc(RpcTarget.All, IsPersistent = true)]
    /// void AnnounceTeamFormation(string teamName) { }
    ///
    /// EXAMPLE - PROBLEMATIC (Late-joiners won't be in original team member list):
    /// [TargetRpc(nameof(TeamMemberIds), isMultipleTargets: true, IsPersistent = true)]
    /// void SendTeamStrategy(string strategy) { } // Late-joiners excluded!
    ///
    /// IMPORTANT: This class intentionally does NOT implement ISelfReturnEvent or use pooling.
    ///
    /// GONet's persistence mechanism stores references to persistent events for late-joining
    /// clients (see OnPersistentEvent_KeepTrack in GONet.cs). If these events were pooled
    /// and returned after execution, their data would be cleared/corrupted when sent to
    /// late-joining clients, causing critical data integrity issues.
    ///
    /// This design choice prioritizes data safety over memory efficiency. When creating
    /// persistent RPCs, the cost of allocating new objects is acceptable given the relative
    /// infrequency of persistent RPC usage compared to standard transient RPCs.
    ///
    /// For performance-critical scenarios, consider:
    /// - Using transient RPCs where persistence is not required
    /// - Limiting the frequency of persistent RPC calls
    /// - Implementing custom cleanup logic if memory usage becomes problematic
    ///
    /// This pattern aligns with other persistent events in GONet (e.g., InstantiateGONetParticipantEvent)
    /// which also avoid pooling to maintain data integrity.
    /// </summary>
    [MemoryPackable]
    public partial class PersistentRoutedRpcEvent : IPersistentEvent
    {
        public long OccurredAtElapsedTicks { get; set; }
        public uint RpcId { get; set; }
        public uint GONetId { get; set; }
        public ushort[] TargetAuthorities { get; set; } = new ushort[64];
        public int TargetCount { get; set; }
        public byte[] Data { get; set; }
        public long CorrelationId { get; set; }

        /// <summary>
        /// Original target specification for validation when sending to late-joining clients
        /// </summary>
        public RpcTarget OriginalTarget { get; set; }

        /// <summary>
        /// Source authority that originally sent this RPC
        /// </summary>
        public ushort SourceAuthorityId { get; set; }

        /// <summary>
        /// Property name used for original targeting (for property-based TargetRpc)
        /// </summary>
        public string TargetPropertyName { get; set; }
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
    /// Context information available during RPC validation, providing access to source and target authority data.
    /// This struct is populated by the GONet framework before calling validation methods.
    /// </summary>
    /// <remarks>
    /// <para><b>Usage in Validation Methods:</b></para>
    /// <code>
    /// internal RpcValidationResult ValidateMessage(ref string content, ref string channel)
    /// {
    ///     var context = GONetMain.EventBus.GetValidationContext().Value;
    ///     var result = context.GetValidationResult();
    ///
    ///     // Check if sender is authorized for this channel
    ///     if (!IsAuthorizedForChannel(context.SourceAuthorityId, channel))
    ///     {
    ///         result.DenyAll("Not authorized for channel");
    ///         return result;
    ///     }
    ///
    ///     // Filter recipients based on permissions
    ///     for (int i = 0; i &lt; context.TargetCount; i++)
    ///     {
    ///         ushort targetId = context.TargetAuthorityIds[i];
    ///         result.AllowedTargets[i] = CanReceiveMessage(targetId, content);
    ///     }
    ///
    ///     return result;
    /// }
    /// </code>
    /// </remarks>
    public struct RpcValidationContext
    {
        /// <summary>
        /// Authority ID of the client/server that initiated the RPC call.
        /// For client-to-server RPCs, this is the originating client's authority ID.
        /// For server-to-client RPCs, this is the server's authority ID.
        /// </summary>
        public ushort SourceAuthorityId { get; set; }

        /// <summary>
        /// Array of target authority IDs that the RPC is being sent to.
        /// The length of this array may be larger than TargetCount; only the first TargetCount elements are valid.
        /// Use in conjunction with TargetCount to determine valid recipients.
        /// </summary>
        public ushort[] TargetAuthorityIds { get; set; }

        /// <summary>
        /// Number of valid targets in the TargetAuthorityIds array.
        /// Always use this value instead of TargetAuthorityIds.Length for iteration.
        /// </summary>
        public int TargetCount { get; set; }

        /// <summary>
        /// Internal pre-allocated validation result. Use GetValidationResult() to access.
        /// </summary>
        internal RpcValidationResult PreAllocatedResult { get; set; }

        /// <summary>
        /// Gets the pre-allocated validation result for modification.
        /// The AllowedTargets bool array is already sized to TargetCount and initialized to false.
        /// Modify this result to control which targets should receive the RPC.
        /// </summary>
        /// <returns>RpcValidationResult with a pre-allocated bool array ready for modification</returns>
        /// <example>
        /// <code>
        /// var result = context.GetValidationResult();
        /// result.AllowAll(); // Allow all targets
        /// // OR
        /// result.AllowTarget(0); // Allow only first target
        /// result.DenyTarget(1, "Not authorized"); // Deny second target with reason
        /// return result;
        /// </code>
        /// </example>
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