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
    ///
    /// <para><b>Supported RPC Parameter Types (RUNTIME VALIDATED):</b></para>
    /// <para>
    /// GONet RPCs use MemoryPack serialization. The following types are fully supported:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Primitives:</b> bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double</description></item>
    /// <item><description><b>String:</b> string (reference type)</description></item>
    /// <item><description><b>Unity Math:</b> Vector2, Vector3, Quaternion</description></item>
    /// <item><description><b>Enums:</b> All enum types (serialized as underlying type)</description></item>
    /// <item><description><b>Custom Types:</b> [MemoryPackable] partial struct/class (MUST be at namespace level, not nested)</description></item>
    /// <item><description><b>Arrays:</b> T[] for any supported type T (int[], Vector3[], string[], custom struct arrays)</description></item>
    /// <item><description><b>Collections:</b> List&lt;T&gt;, Dictionary&lt;K,V&gt; for any supported K,V</description></item>
    /// </list>
    ///
    /// <para><b>NOT Supported (Unity Types Not Recognized by MemoryPack):</b></para>
    /// <list type="bullet">
    /// <item><description><b>UnityEngine.Vector4</b> - Use 4 float parameters (x, y, z, w)</description></item>
    /// <item><description><b>UnityEngine.Color</b> - Use 4 float parameters (r, g, b, a)</description></item>
    /// <item><description><b>UnityEngine.Color32</b> - Use 4 byte parameters (r, g, b, a)</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>MemoryPack Restrictions:</b>
    /// </para>
    /// <list type="bullet">
    /// <item><description>[MemoryPackable] types MUST be at namespace level (not nested inside classes) - causes MEMPACK002 error</description></item>
    /// <item><description>[MemoryPackable] types MUST be declared 'partial'</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Example Workarounds:</b>
    /// </para>
    /// <code>
    /// // ✅ VALID - Custom MemoryPackable struct (at namespace level)
    /// [MemoryPackable]
    /// public partial struct PlayerData { public int id; public float health; }
    ///
    /// [ServerRpc]
    /// void UpdatePlayer(PlayerData data) { } // Works!
    ///
    /// // ✅ VALID - Arrays and collections
    /// [ServerRpc]
    /// void SendPositions(Vector3[] positions) { } // Works!
    ///
    /// [ServerRpc]
    /// void SendData(List&lt;int&gt; values) { } // Works!
    ///
    /// // ❌ INVALID - Color not supported
    /// [ServerRpc]
    /// void SetColor(Color color) { } // FAILS
    ///
    /// // ✅ CORRECT - Break Color into RGBA components
    /// [ServerRpc]
    /// void SetColor(float r, float g, float b, float a) { }
    ///
    /// // Usage: CallRpc(nameof(SetColor), color.r, color.g, color.b, color.a);
    /// </code>
    ///
    /// <para><b>RPC Parameter Count Limit:</b> Maximum 8 parameters per RPC method.</para>
    /// <para><b>Validation:</b> See GONetRpcMemoryPackTypesTest.cs for comprehensive runtime-validated examples.</para>
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
    /// Marks a method as a Server RPC that executes on the server.
    /// Can be called by clients (via network) or by the server itself (direct execution with RunLocally=true).
    /// </summary>
    /// <remarks>
    /// <para><b>Basic Server RPC Usage (Client → Server):</b></para>
    /// <code>
    /// [ServerRpc]
    /// async Task&lt;ClaimResult&gt; RequestClaim()
    /// {
    ///     // Runs on server (called by client OR server itself)
    ///     var context = GONetEventBus.GetCurrentRpcContext();
    ///
    ///     // Manual validation in method body
    ///     if (IsAlreadyClaimed)
    ///         return new ClaimResult { Success = false };
    ///
    ///     ClaimedBy = context.SourceAuthorityId;
    ///
    ///     // Broadcast result to all clients
    ///     BroadcastClaimChanged();
    ///
    ///     return new ClaimResult { Success = true };
    /// }
    ///
    /// [ClientRpc]
    /// void BroadcastClaimChanged()
    /// {
    ///     UpdateVisuals(); // Runs on all clients
    /// }
    /// </code>
    ///
    /// <para><b>Server-Side Execution (RunLocally = true, default):</b></para>
    /// <code>
    /// // Server can call ServerRpc directly (no network overhead)
    /// void OnServerStartup()
    /// {
    ///     if (GONetMain.IsServer)
    ///     {
    ///         // Executes locally on server (no RPC sent)
    ///         var result = await CallRpcAsync&lt;ClaimResult&gt;(nameof(RequestClaim));
    ///     }
    /// }
    /// </code>
    ///
    /// <para><b>Validation:</b></para>
    /// <para>ServerRpc does NOT have a built-in validation pipeline. For validation with parameter
    /// modification and selective targeting, use <see cref="TargetRpcAttribute"/> instead, which
    /// supports the <c>validationMethod</c> parameter.</para>
    ///
    /// <para><b>Broadcasting to Clients:</b></para>
    /// <para>To notify other clients about ServerRpc results, manually call <see cref="ClientRpcAttribute"/>
    /// or <see cref="TargetRpcAttribute"/> from within your ServerRpc method. This is the industry-standard
    /// pattern used by Mirror, Fish-Net, and Unity Netcode for GameObjects.</para>
    ///
    /// <para><b>Security:</b></para>
    /// <para>ServerRpcs have <c>IsMineRequired = true</c> by default. Clients can only call ServerRpcs
    /// on objects they own unless explicitly set to <c>false</c>.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRpcAttribute : GONetRpcAttribute
    {
        /// <summary>
        /// Gets or sets whether the server should execute this RPC locally when called on the server.
        /// Default: true (matches Unity Netcode for GameObjects behavior).
        ///
        /// When true:
        /// - Server calls to this ServerRpc execute directly (no network overhead)
        /// - Client calls to this ServerRpc route via network as normal
        ///
        /// When false:
        /// - Server calls will throw InvalidOperationException (strict client-to-server only)
        /// - Client calls route via network as normal
        ///
        /// Set to false only if you want to enforce strict client-to-server semantics
        /// (preventing server from accidentally calling its own ServerRpcs).
        /// </summary>
        public bool RunLocally { get; set; } = true;

        /// <summary>
        /// Initializes a new ServerRpcAttribute with secure defaults.
        /// Sets IsMineRequired = true to prevent unauthorized access to other players' objects.
        /// Sets RunLocally = true to allow server-side execution (matches industry standard).
        /// </summary>
        public ServerRpcAttribute()
        {
            IsMineRequired = true; // Safe default - clients can only call on objects they own
            RunLocally = true; // Allow server to call directly without network overhead
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
    /// <item><description>Does NOT support validation - use <see cref="TargetRpcAttribute"/> if you need validation</description></item>
    /// </list>
    ///
    /// <para><b>For Selective Targeting or Validation:</b></para>
    /// <para>If you need to target specific clients or validate messages before delivery, use
    /// <see cref="TargetRpcAttribute"/> instead, which supports validation methods and flexible targeting.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : GONetRpcAttribute { }

    /// <summary>
    /// Marks a method as a Target RPC that routes messages to specific recipients with optional server-side validation.
    /// TargetRpc is the ONLY RPC type with validation support for message filtering and parameter modification.
    /// </summary>
    /// <remarks>
    /// <para><b>Basic Targeting:</b></para>
    /// <code>
    /// [TargetRpc(RpcTarget.All)]
    /// void BroadcastMessage(string msg) { }
    ///
    /// [TargetRpc(RpcTarget.Owner)]
    /// void NotifyOwner(string msg) { }
    ///
    /// // Property-based targeting
    /// public ushort TargetPlayerId;
    /// [TargetRpc(nameof(TargetPlayerId))]
    /// void SendToPlayer(string msg) { }
    ///
    /// // Multiple targets
    /// public List&lt;ushort&gt; TeamMembers;
    /// [TargetRpc(nameof(TeamMembers), isMultipleTargets: true)]
    /// void SendToTeam(string msg) { }
    /// </code>
    ///
    /// <para><b>Validation (Profanity Filtering, Access Control, etc.):</b></para>
    /// <code>
    /// [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateMessage))]
    /// void Chat(string message) { }
    ///
    /// // Sync validator (use ref for parameter modification)
    /// RpcValidationResult ValidateMessage(ref string message)
    /// {
    ///     var ctx = GONetMain.EventBus.GetValidationContext();
    ///     var result = ctx.HasValue ? ctx.Value.GetValidationResult() : RpcValidationResult.CreatePreAllocated(1);
    ///     message = FilterProfanity(message); // Modify ref parameter
    ///     result.AllowAll(); // Or filter targets: result.AllowTarget(authorityId)
    ///     return result;
    /// }
    ///
    /// // Async validator (NO ref - use SetValidatedOverride)
    /// async Task&lt;RpcValidationResult&gt; ValidateAsync(string message)
    /// {
    ///     var filtered = await WebAPI.FilterProfanity(message);
    ///     var result = GetValidationResult();
    ///     if (filtered != message) result.SetValidatedOverride(0, filtered);
    ///     return result;
    /// }
    /// </code>
    ///
    /// <para><b>Delivery Confirmation:</b></para>
    /// <code>
    /// [TargetRpc(RpcTarget.All)]
    /// async Task&lt;RpcDeliveryReport&gt; SendConfirmed(string msg)
    /// {
    ///     // Framework populates report with delivery results
    ///     return await Task.CompletedTask;
    /// }
    ///
    /// var report = await SendConfirmed("Hello");
    /// if (report.FailedDelivery?.Length > 0)
    ///     Debug.Log($"Failed: {report.FailureReason}");
    /// </code>
    ///
    /// <para><b>Persistent RPCs - ⚠️ Limitation:</b> Late-joiners only receive if targeted by RpcTarget.All or if your targeting property includes them.</para>
    /// <para><b>See Also:</b> GONetRpcValidationTests.cs for runtime validation test suite (press Shift+V). See class code comments below for comprehensive examples.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : GONetRpcAttribute
    {
        public RpcTarget Target { get; }
        public string TargetPropertyName { get; }
        public bool IsMultipleTargets { get; }
        public string ValidationMethodName { get; }

        public TargetRpcAttribute(RpcTarget target = RpcTarget.All, string validationMethod = null)
        {
            Target = target;
            ValidationMethodName = validationMethod;
        }

        // Single constructor for property-based targeting
        public TargetRpcAttribute(string targetPropertyName, bool isMultipleTargets = false, string validationMethod = null)
        {
            Target = isMultipleTargets ? RpcTarget.MultipleAuthorities : RpcTarget.SpecificAuthority;
            TargetPropertyName = targetPropertyName;
            IsMultipleTargets = isMultipleTargets;
            ValidationMethodName = validationMethod;
        }

        /* ========================================
         * TARGETRPC COMPREHENSIVE USAGE GUIDE
         * ========================================
         *
         * TargetRpc is the ONLY GONet RPC type that supports:
         * - Server-side validation
         * - Parameter modification (profanity filtering, clamping, sanitization)
         * - Selective targeting (control exactly who receives messages)
         * - Delivery confirmation reports
         *
         * ---------------------------------------
         * WHEN TO USE TARGETRPC
         * ---------------------------------------
         *
         * Use TargetRpc when you need:
         * - Profanity filtering (modify chat messages before delivery)
         * - Anti-cheat validation (clamp damage values, validate positions)
         * - Access control (verify sender/receiver permissions)
         * - Team/proximity-based messaging (send only to nearby players)
         * - Delivery confirmation (know if message was received)
         *
         * Use ServerRpc for: Client → Server requests (no validation needed)
         * Use ClientRpc for: Server → All clients broadcasts (no validation needed)
         *
         * ---------------------------------------
         * BASIC TARGETING PATTERNS
         * ---------------------------------------
         *
         * 1. BROADCAST TO ALL:
         *
         *    [TargetRpc(RpcTarget.All)]
         *    void BroadcastEvent(string eventName)
         *    {
         *        HandleEvent(eventName);
         *    }
         *
         * 2. SEND TO OWNER ONLY:
         *
         *    [TargetRpc(RpcTarget.Owner)]
         *    void NotifyOwner(string message)
         *    {
         *        ShowNotification(message);
         *    }
         *
         * 3. PROPERTY-BASED TARGETING (SINGLE):
         *
         *    public ushort TargetPlayerId { get; set; }
         *
         *    [TargetRpc(nameof(TargetPlayerId))]
         *    void SendToPlayer(string message)
         *    {
         *        DisplayMessage(message);
         *    }
         *
         *    // Usage:
         *    TargetPlayerId = 5; // Set before calling
         *    CallRpc(nameof(SendToPlayer), "Private message");
         *
         * 4. PROPERTY-BASED TARGETING (MULTIPLE):
         *
         *    public List<ushort> TeamMembers { get; set; }
         *
         *    [TargetRpc(nameof(TeamMembers), isMultipleTargets: true)]
         *    void SendToTeam(string message)
         *    {
         *        DisplayTeamMessage(message);
         *    }
         *
         *    // Usage:
         *    TeamMembers = new List<ushort> { 1, 3, 5 };
         *    CallRpc(nameof(SendToTeam), "Team objective complete!");
         *
         * 5. PARAMETER-BASED TARGETING:
         *
         *    [TargetRpc(RpcTarget.SpecificAuthority)]
         *    void SendToPlayer(ushort targetId, string message)
         *    {
         *        // First parameter specifies target
         *        DisplayMessage(message);
         *    }
         *
         *    // Usage:
         *    CallRpc(nameof(SendToPlayer), (ushort)3, "You won!");
         *
         * ---------------------------------------
         * VALIDATION: SYNC VS ASYNC
         * ---------------------------------------
         *
         * SYNC VALIDATOR (use ref for parameter modification):
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateChat))]
         *    void Chat(string message)
         *    {
         *        DisplayChatMessage(message);
         *    }
         *
         *    RpcValidationResult ValidateChat(ref string message)
         *    {
         *        var ctx = GONetMain.EventBus.GetValidationContext();
         *        var result = ctx.HasValue ?
         *            ctx.Value.GetValidationResult() :
         *            RpcValidationResult.CreatePreAllocated(1);
         *
         *        // Modify parameter directly with ref
         *        message = FilterProfanity(message);
         *
         *        result.AllowAll(); // Or filter targets
         *        return result;
         *    }
         *
         * ASYNC VALIDATOR (NO ref - use SetValidatedOverride):
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateChatAsync))]
         *    void Chat(string message)
         *    {
         *        DisplayChatMessage(message);
         *    }
         *
         *    async Task<RpcValidationResult> ValidateChatAsync(string message)
         *    {
         *        var ctx = GONetMain.EventBus.GetValidationContext();
         *        var result = ctx.Value.GetValidationResult();
         *
         *        // Async operations allowed
         *        string filtered = await WebAPI.FilterProfanity(message);
         *
         *        // Can't use ref with async - use SetValidatedOverride instead
         *        if (filtered != message)
         *            result.SetValidatedOverride(0, filtered); // Param index 0
         *
         *        result.AllowAll();
         *        return result;
         *    }
         *
         * VALIDATION PARAMETERS MUST MATCH RPC PARAMETERS:
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate))]
         *    void SendData(string msg, int value, Vector3 pos) { }
         *
         *    // CORRECT - parameters match RPC signature
         *    RpcValidationResult Validate(ref string msg, ref int value, ref Vector3 pos)
         *    {
         *        // Same types, same order
         *    }
         *
         * ---------------------------------------
         * ADVANCED VALIDATION PATTERNS
         * ---------------------------------------
         *
         * PROFANITY FILTERING:
         *
         *    RpcValidationResult ValidateChat(ref string message)
         *    {
         *        var result = GetValidationResult();
         *        message = message.Replace("badword", "***");
         *        result.AllowAll();
         *        return result;
         *    }
         *
         * ANTI-CHEAT (CLAMP VALUES):
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateDamage))]
         *    void ReportDamage(int damage) { }
         *
         *    RpcValidationResult ValidateDamage(ref int damage)
         *    {
         *        var result = GetValidationResult();
         *
         *        // Clamp to prevent cheating
         *        int original = damage;
         *        damage = Mathf.Clamp(damage, 0, 100);
         *
         *        if (damage != original)
         *            GONetLog.Warning($"Clamped damage from {original} to {damage}");
         *
         *        result.AllowAll();
         *        return result;
         *    }
         *
         * DENY ALL (BLOCK EVERYONE):
         *
         *    RpcValidationResult ValidateRestricted(ref string action)
         *    {
         *        var ctx = GONetMain.EventBus.GetValidationContext();
         *        var result = ctx.Value.GetValidationResult();
         *
         *        // Check permissions
         *        if (!HasPermission(ctx.Value.SourceAuthorityId, action))
         *        {
         *            result.DenyAll(); // Block message entirely
         *            return result;
         *        }
         *
         *        result.AllowAll();
         *        return result;
         *    }
         *
         * SELECTIVE TARGETING (TEAM-ONLY):
         *
         *    public List<ushort> TeamMembers { get; set; }
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateTeam))]
         *    void SendTeamMessage(string msg) { }
         *
         *    RpcValidationResult ValidateTeam(ref string msg)
         *    {
         *        var ctx = GONetMain.EventBus.GetValidationContext();
         *        var result = ctx.Value.GetValidationResult();
         *
         *        // Filter targets to team members only
         *        for (int i = 0; i < ctx.Value.TargetCount; i++)
         *        {
         *            ushort targetId = ctx.Value.TargetAuthorityIds[i];
         *            result.AllowedTargets[i] = TeamMembers.Contains(targetId);
         *        }
         *
         *        return result;
         *    }
         *
         * PROXIMITY-BASED TARGETING:
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateProximity))]
         *    void BroadcastLocalEvent(string eventName, Vector3 pos) { }
         *
         *    RpcValidationResult ValidateProximity(ref string eventName, ref Vector3 pos)
         *    {
         *        var ctx = GONetMain.EventBus.GetValidationContext();
         *        var result = ctx.Value.GetValidationResult();
         *
         *        const float maxDistance = 50f;
         *
         *        // Only send to clients within 50 units
         *        for (int i = 0; i < ctx.Value.TargetCount; i++)
         *        {
         *            ushort targetId = ctx.Value.TargetAuthorityIds[i];
         *            Vector3 targetPos = GetPlayerPosition(targetId);
         *            float distance = Vector3.Distance(pos, targetPos);
         *            result.AllowedTargets[i] = (distance <= maxDistance);
         *        }
         *
         *        return result;
         *    }
         *
         * ---------------------------------------
         * DELIVERY CONFIRMATION
         * ---------------------------------------
         *
         * BASIC DELIVERY REPORT:
         *
         *    [TargetRpc(RpcTarget.All)]
         *    async Task<RpcDeliveryReport> SendConfirmed(string msg)
         *    {
         *        DisplayMessage(msg);
         *        return await Task.CompletedTask; // Framework populates report
         *    }
         *
         *    // Usage:
         *    var report = await SendConfirmed("Important message");
         *    if (report.FailedDelivery?.Length > 0)
         *    {
         *        Debug.LogError($"Failed to deliver to: {string.Join(", ", report.FailedDelivery)}");
         *        Debug.LogError($"Reason: {report.FailureReason}");
         *    }
         *
         * ---------------------------------------
         * PERSISTENT RPCs - IMPORTANT LIMITATIONS
         * ---------------------------------------
         *
         * Persistent TargetRPCs store ORIGINAL target authority IDs.
         * Late-joiners may NOT receive them if their ID wasn't in the original list!
         *
         * ✅ SAFE - RpcTarget.All:
         *
         *    [TargetRpc(RpcTarget.All, IsPersistent = true)]
         *    void AnnounceGamePhase(GamePhase phase)
         *    {
         *        // Late-joiners WILL receive this
         *        currentPhase = phase;
         *    }
         *
         * ❌ PROBLEMATIC - Dynamic target list:
         *
         *    public List<ushort> ActivePlayers { get; set; }
         *
         *    [TargetRpc(nameof(ActivePlayers), isMultipleTargets: true, IsPersistent = true)]
         *    void SendRoundResults(ScoreData scores)
         *    {
         *        // Late-joiners WON'T receive this (not in original ActivePlayers list)
         *        DisplayResults(scores);
         *    }
         *
         * RECOMMENDATION: Use RpcTarget.All for persistent state that all clients need.
         *
         * ---------------------------------------
         * VALIDATION CONTEXT API
         * ---------------------------------------
         *
         * RpcValidationContext properties:
         *
         *    var ctx = GONetMain.EventBus.GetValidationContext();
         *    if (ctx.HasValue)
         *    {
         *        ushort senderId = ctx.Value.SourceAuthorityId; // Who sent RPC
         *        int targetCount = ctx.Value.TargetCount;       // How many targets
         *        ushort[] targets = ctx.Value.TargetAuthorityIds; // Target IDs
         *
         *        // Get pre-allocated validation result (preferred - no GC)
         *        RpcValidationResult result = ctx.Value.GetValidationResult();
         *    }
         *
         * RpcValidationResult API:
         *
         *    result.AllowAll();              // Allow all targets
         *    result.DenyAll();               // Block all targets
         *    result.AllowTarget(authorityId); // Allow specific target
         *    result.AllowedTargets[i] = true; // Manually set allowed targets
         *    result.SetValidatedOverride(0, newValue); // Modify param (async only)
         *
         * ---------------------------------------
         * COMMON PITFALLS
         * ---------------------------------------
         *
         * ❌ WRONG - Missing ref keyword (sync validators):
         *
         *    RpcValidationResult Validate(string msg)
         *    {
         *        msg = "modified"; // This change is LOCAL only!
         *        return result;
         *    }
         *
         * ✅ CORRECT - Use ref keyword:
         *
         *    RpcValidationResult Validate(ref string msg)
         *    {
         *        msg = "modified"; // This propagates to RPC
         *        return result;
         *    }
         *
         * ❌ WRONG - Using ref with async:
         *
         *    async Task<RpcValidationResult> Validate(ref string msg) // COMPILE ERROR!
         *
         * ✅ CORRECT - Use SetValidatedOverride:
         *
         *    async Task<RpcValidationResult> Validate(string msg)
         *    {
         *        string modified = await FilterAsync(msg);
         *        result.SetValidatedOverride(0, modified);
         *        return result;
         *    }
         *
         * ❌ WRONG - Parameter mismatch:
         *
         *    [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate))]
         *    void Send(string msg, int value) { }
         *
         *    RpcValidationResult Validate(ref string msg) // Missing ref int value!
         *
         * ✅ CORRECT - Parameters MUST match:
         *
         *    RpcValidationResult Validate(ref string msg, ref int value)
         *
         * ---------------------------------------
         * TESTING & EXAMPLES
         * ---------------------------------------
         *
         * Test File: Assets/GONet/Sample/RpcTests/GONetRpcValidationTests.cs
         *
         * How to run:
         * 1. Start server + 2 clients
         * 2. Press Shift+V from any client to run ALL validation tests
         * 3. Press Shift+K to dump execution summary
         *
         * Tests included:
         * - Sync Validator - AllowAll
         * - Sync Validator - DenyAll
         * - Sync Validator - Allow Specific Targets (Client:1 only)
         * - Async Validator - AllowAll
         * - Async Validator - DenyAll
         * - Validator with Parameter Modification
         * - Validator with Selective Targeting
         *
         * ======================================== */
    }

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
        /// Tracks validated parameter overrides for async validators.
        /// Dictionary maps parameter index to validated value.
        /// Used when async validators cannot use 'ref' parameters (C# language limitation).
        /// The framework serializes all params (original + overrides) into ModifiedData after validation completes.
        /// </summary>
        private Dictionary<int, object> _validatedParamOverrides;

        /// <summary>
        /// Sets a validated override for the parameter at the specified index.
        /// This is the primary API for async validators to modify RPC parameters.
        /// </summary>
        /// <remarks>
        /// <para><b>Usage in Async Validators:</b></para>
        /// <code>
        /// internal async Task&lt;RpcValidationResult&gt; ValidateMessageAsync(
        ///     string content,        // [0] - No 'ref' keyword (async limitation)
        ///     string channelName,    // [1]
        ///     ChatType messageType)  // [2]
        /// {
        ///     var result = GetValidationContext().GetValidationResult();
        ///
        ///     // Perform async validation
        ///     string filteredContent = await FilterProfanityAsync(content);
        ///
        ///     // Set validated override for parameter at index 0
        ///     if (filteredContent != content)
        ///     {
        ///         result.SetValidatedOverride(0, filteredContent);
        ///     }
        ///
        ///     result.AllowAll();
        ///     return result;
        /// }
        /// </code>
        ///
        /// <para><b>Performance Notes:</b></para>
        /// <list type="bullet">
        /// <item><description>Boxing occurs for value types (~0.1ms overhead per override)</description></item>
        /// <item><description>Framework serializes ALL params (original + overrides) after validation</description></item>
        /// <item><description>Overhead is negligible compared to eliminating 2000ms blocking from sync validators</description></item>
        /// </list>
        /// </remarks>
        /// <param name="paramIndex">Zero-based index of the parameter to override</param>
        /// <param name="validatedValue">The validated value to use instead of the original</param>
        /// <exception cref="ObjectDisposedException">Thrown if the validation result has been disposed</exception>
        public void SetValidatedOverride(int paramIndex, object validatedValue)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RpcValidationResult));

            if (_validatedParamOverrides == null)
            {
                _validatedParamOverrides = new Dictionary<int, object>();
            }

            _validatedParamOverrides[paramIndex] = validatedValue; // Boxes value types (acceptable cost)
            WasModified = true; // Auto-set modification flag
        }

        /// <summary>
        /// Internal: Gets the validated overrides for framework serialization.
        /// Used by GONetEventBus_Rpc to apply parameter overrides after async validation completes.
        /// </summary>
        internal Dictionary<int, object> GetValidatedOverrides() => _validatedParamOverrides;

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

            // Clear validated param overrides dictionary
            _validatedParamOverrides?.Clear();
            _validatedParamOverrides = null;
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
        public ushort OriginatorAuthorityId { get; set; } // Authority that initiated the RPC call
        public bool HasValidation { get; set; } // Whether this RPC has a validation method

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
            // NOTE: We DO NOT return evt.Data to the byte array pool here because:
            // 1. The Data array may be a shared reference from persistent events (see HandlePersistentRpcForMe)
            // 2. The byte array lifecycle is managed separately from the event object lifecycle
            // 3. Byte arrays are returned at deserialization boundaries, not at event pooling boundaries
            // Attempting to return Data here causes "array not borrowed from pool" exceptions.

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
            // NOTE: We DO NOT return evt.Data to the byte array pool here because:
            // 1. The Data array may be a shared reference or borrowed from elsewhere in the pipeline
            // 2. The byte array lifecycle is managed separately from the event object lifecycle
            // 3. Byte arrays are returned at deserialization boundaries, not at event pooling boundaries
            // Attempting to return Data here causes "array not borrowed from pool" exceptions.

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
        public ushort OriginatorAuthorityId { get; set; } // Authority that initiated the RPC call
        public bool HasValidation { get; set; } // Whether this RPC has a validation method
        public bool ShouldExpandToAllClients { get; set; } // Server should expand target list to include all connected clients

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
            // NOTE: We DO NOT return evt.Data to the byte array pool here because:
            // 1. The Data array may be a shared reference from persistent events (see HandlePersistentRpcForMe)
            // 2. The byte array lifecycle is managed separately from the event object lifecycle
            // 3. Byte arrays are returned at deserialization boundaries, not at event pooling boundaries
            // Attempting to return Data here causes "array not borrowed from pool" exceptions.

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

        // Constructor for synthetic execution context (e.g., server-side ServerRpc with RunLocally=true)
        internal GONetRpcContext(ushort sourceAuthorityId, bool isReliable, uint gonetParticipantId, bool isSourceRemote)
        {
            Envelope = null;
            SourceAuthorityId = sourceAuthorityId;
            IsSourceRemote = isSourceRemote;
            IsFromMe = !isSourceRemote; // If remote, not from me; if local, from me
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