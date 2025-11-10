using GONet;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Comprehensive test suite for GONet RPC system.
/// Tests all three RPC types (TargetRpc, ServerRpc, ClientRpc) across all parameter counts (0-8).
///
/// Test Triggers:
/// - Shift+A: Run ALL tests applicable to this machine (auto-detect)
/// - Shift+L: TargetRpc tests (broadcast to all machines)
/// - Shift+C: ServerRpc tests (RunLocally=true allows server and clients to invoke)
/// - Shift+S: ClientRpc tests (server → all clients only, SERVER ONLY)
/// - Shift+K: Dump execution summary
///
/// Expected Results:
/// - TargetRpc: All machines execute RPCs from all initiators
/// - ServerRpc: Only server executes, clients receive async responses
/// - ClientRpc: Only clients execute (server broadcasts without local execution in dedicated mode)
/// </summary>
[RequireComponent(typeof(GONetParticipant))]
public class GONetRpcComprehensiveTests : GONetParticipantCompanionBehaviour
{
    #region RPC Execution Tracker

    private static readonly ConcurrentBag<string> rpcExecutionLog = new ConcurrentBag<string>();
    private static int currentTestId = -1;

    /// <summary>
    /// Record an RPC execution for later summary output (avoids log interleaving issues).
    /// Extracts test ID from the RPC message parameter if available.
    /// </summary>
    private static void LogRpcExecution(string rpcVariant, string messageWithTestId = null)
    {
        // Try to extract test ID from message first (format: "537-1p-nvs message...")
        int testId = ExtractTestIdFromMessage(messageWithTestId);

        // If no message or couldn't extract, use currentTestId
        if (testId == -1)
        {
            testId = currentTestId;
        }

        // If still no test ID, can't log
        if (testId == -1) return;

        // Auto-set currentTestId from first message we see (enables tracking even if we didn't initiate)
        if (currentTestId == -1)
        {
            currentTestId = testId;
        }

        string machine = GONetMain.IsServer ? "Server" : (GONetMain.MyAuthorityId == 1 ? "Client:1" : "Client:2");
        rpcExecutionLog.Add($"{testId}-{rpcVariant}|{machine}");
    }

    /// <summary>
    /// Extracts test ID from message like "537-1p-nvs message..." or "340-2p-Vs message...".
    /// Returns -1 if no test ID found.
    /// </summary>
    private static int ExtractTestIdFromMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return -1;

        // Find first dash (test ID ends there)
        int dashIndex = message.IndexOf('-');
        if (dashIndex == -1 || dashIndex == 0) return -1;

        string testIdStr = message.Substring(0, dashIndex);
        if (int.TryParse(testIdStr, out int testId))
        {
            return testId;
        }

        return -1;
    }

    /// <summary>
    /// Dump all collected RPC executions in one log entry (prevents interleaving).
    /// Call this with Shift+K after test completes.
    /// </summary>
    private void DumpRpcExecutionSummary()
    {
        if (rpcExecutionLog.Count == 0)
        {
            GONetLog.Info("No RPC executions recorded");
            return;
        }

        var grouped = rpcExecutionLog.GroupBy(entry => entry.Split('|')[0])
                                      .OrderBy(g => g.Key);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("========== RPC EXECUTION SUMMARY ==========");

        foreach (var group in grouped)
        {
            string variant = group.Key;
            var machines = group.Select(entry => entry.Split('|')[1]).Distinct().OrderBy(m => m).ToList();
            int count = group.Count();
            sb.AppendLine($"{variant}: {count} executions on [{string.Join(", ", machines)}]");
        }

        sb.AppendLine($"Total executions: {rpcExecutionLog.Count}");
        sb.AppendLine("==========================================");

        // Log to machine-specific file
        GONetLog.Info(sb.ToString(), myRpcLogTelemetryProfile);
    }

    string myRpcLogTelemetryProfile;
    private void InitTelemetryLogging()
    {
        // Register a separate log file per machine to avoid log interleaving
        myRpcLogTelemetryProfile = string.Concat("RpcTelemetry-", GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}");
        GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(myRpcLogTelemetryProfile, outputToSeparateFile: true));
    }

    private void OnApplicationQuit()
    {
        DumpRpcExecutionSummary();
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitTelemetryLogging();
        RegisterTestsWithUI();
    }

    #endregion

    #region TargetRpc Test Methods (36 rpc methods: 9 param counts × 4 variants each)

    // ========== 0-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_0Params_NotValidated()
    {
        LogRpcExecution("0p-nvs");
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 0-params (sync)"), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_0Params))]
    internal void LogOnAllMachines_0Params_Validated()
    {
        LogRpcExecution("0p-Vs");
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 0-params (validated)"), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_0Params_NotValidatedAsync()
    {
        await Task.CompletedTask;
        LogRpcExecution("0p-nvA");
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 0-params (async)"), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_0Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_0Params_ValidatedAsync()
    {
        await Task.CompletedTask;
        LogRpcExecution("0p-VA");
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 0-params (validated async)"), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_0Params()
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 1-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_NotValidated(string message)
    {
        LogRpcExecution("1p-nvs", message);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), ' ', message), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator))]
    internal void LogOnAllMachines_Validated(string message)
    {
        LogRpcExecution("1p-Vs", message);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), ' ', message), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_NotValidatedAsync(string message)
    {
        await Task.CompletedTask;
        LogRpcExecution("1p-nvA", message);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), ' ', message), myRpcLogTelemetryProfile);

        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_ValidatedAsync(string message)
    {
        await Task.CompletedTask;
        LogRpcExecution("1p-VA", message);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), ' ', message), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator(ref string message)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 2-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_2Params_NotValidated(string msg, int value)
    {
        LogRpcExecution("2p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 2-params: ", msg, ", ", value), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_2Params))]
    internal void LogOnAllMachines_2Params_Validated(string msg, int value)
    {
        LogRpcExecution("2p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 2-params (validated): ", msg, ", ", value), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_2Params_NotValidatedAsync(string msg, int value)
    {
        await Task.CompletedTask;
        LogRpcExecution("2p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 2-params (async): ", msg, ", ", value), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_2Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_2Params_ValidatedAsync(string msg, int value)
    {
        await Task.CompletedTask;
        LogRpcExecution("2p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 2-params (validated async): ", msg, ", ", value), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_2Params(ref string msg, ref int value)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 3-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_3Params_NotValidated(string msg, int value, float f)
    {
        LogRpcExecution("3p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 3-params: ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_3Params))]
    internal void LogOnAllMachines_3Params_Validated(string msg, int value, float f)
    {
        LogRpcExecution("3p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 3-params (validated): ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_3Params_NotValidatedAsync(string msg, int value, float f)
    {
        await Task.CompletedTask;
        LogRpcExecution("3p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 3-params (async): ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_3Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_3Params_ValidatedAsync(string msg, int value, float f)
    {
        await Task.CompletedTask;
        LogRpcExecution("3p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 3-params (validated async): ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_3Params(ref string msg, ref int value, ref float f)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 4-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_4Params_NotValidated(string msg, int value, float f, bool b)
    {
        LogRpcExecution("4p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 4-params: ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_4Params))]
    internal void LogOnAllMachines_4Params_Validated(string msg, int value, float f, bool b)
    {
        LogRpcExecution("4p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 4-params (validated): ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_4Params_NotValidatedAsync(string msg, int value, float f, bool b)
    {
        await Task.CompletedTask;
        LogRpcExecution("4p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 4-params (async): ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_4Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_4Params_ValidatedAsync(string msg, int value, float f, bool b)
    {
        await Task.CompletedTask;
        LogRpcExecution("4p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 4-params (validated async): ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_4Params(ref string msg, ref int value, ref float f, ref bool b)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 5-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_5Params_NotValidated(string msg, int value, float f, bool b, double d)
    {
        LogRpcExecution("5p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 5-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_5Params))]
    internal void LogOnAllMachines_5Params_Validated(string msg, int value, float f, bool b, double d)
    {
        LogRpcExecution("5p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 5-params (validated): ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_5Params_NotValidatedAsync(string msg, int value, float f, bool b, double d)
    {
        await Task.CompletedTask;
        LogRpcExecution("5p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 5-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_5Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_5Params_ValidatedAsync(string msg, int value, float f, bool b, double d)
    {
        await Task.CompletedTask;
        LogRpcExecution("5p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 5-params (validated async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_5Params(ref string msg, ref int value, ref float f, ref bool b, ref double d)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 6-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_6Params_NotValidated(string msg, int value, float f, bool b, double d, long l)
    {
        LogRpcExecution("6p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 6-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_6Params))]
    internal void LogOnAllMachines_6Params_Validated(string msg, int value, float f, bool b, double d, long l)
    {
        LogRpcExecution("6p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 6-params (validated): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_6Params_NotValidatedAsync(string msg, int value, float f, bool b, double d, long l)
    {
        await Task.CompletedTask;
        LogRpcExecution("6p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 6-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_6Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_6Params_ValidatedAsync(string msg, int value, float f, bool b, double d, long l)
    {
        await Task.CompletedTask;
        LogRpcExecution("6p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 6-params (validated async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_6Params(ref string msg, ref int value, ref float f, ref bool b, ref double d, ref long l)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 7-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_7Params_NotValidated(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        LogRpcExecution("7p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 7-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_7Params))]
    internal void LogOnAllMachines_7Params_Validated(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        LogRpcExecution("7p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 7-params (validated): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_7Params_NotValidatedAsync(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        await Task.CompletedTask;
        LogRpcExecution("7p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 7-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_7Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_7Params_ValidatedAsync(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        await Task.CompletedTask;
        LogRpcExecution("7p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 7-params (validated async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_7Params(ref string msg, ref int value, ref float f, ref bool b, ref double d, ref long l, ref byte bt)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    // ========== 8-parameter TargetRpc tests ==========
    [TargetRpc]
    internal void LogOnAllMachines_8Params_NotValidated(string msg, int value, float f, bool b, double d, long l, byte bt, short s)
    {
        LogRpcExecution("8p-nvs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 8-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_8Params))]
    internal void LogOnAllMachines_8Params_Validated(string msg, int value, float f, bool b, double d, long l, byte bt, short s)
    {
        LogRpcExecution("8p-Vs", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 8-params (validated): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
    }

    [TargetRpc]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_8Params_NotValidatedAsync(string msg, int value, float f, bool b, double d, long l, byte bt, short s)
    {
        await Task.CompletedTask;
        LogRpcExecution("8p-nvA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 8-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
        return default;
    }

    [TargetRpc(validationMethod: nameof(AlwaysAllowValidator_8Params))]
    internal async Task<RpcDeliveryReport> LogOnAllMachines_8Params_ValidatedAsync(string msg, int value, float f, bool b, double d, long l, byte bt, short s)
    {
        await Task.CompletedTask;
        LogRpcExecution("8p-VA", msg);
        GONetLog.Debug(string.Concat(nameof(TargetRpcAttribute), " 8-params (validated async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
        return default;
    }

    internal RpcValidationResult AlwaysAllowValidator_8Params(ref string msg, ref int value, ref float f, ref bool b, ref double d, ref long l, ref byte bt, ref short s)
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    #endregion

    #region ServerRpc Test Methods (18 methods: 9 param counts × 2 variants)

    // ========== ServerRpc Tests: 0-parameter ==========
    [ServerRpc]
    internal void ServerRpc_0Params_Sync()
    {
        LogRpcExecution("SRpc-0p-s");
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 0-params (sync)"), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_0Params_Async()
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-0p-A");
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 0-params (async)"), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 1-parameter ==========
    [ServerRpc]
    internal void ServerRpc_1Param_Sync(string message)
    {
        LogRpcExecution("SRpc-1p-s", message);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 1-param (sync): ", message), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_1Param_Async(string message)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-1p-A", message);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 1-param (async): ", message), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 2-parameter ==========
    [ServerRpc]
    internal void ServerRpc_2Params_Sync(string msg, int value)
    {
        LogRpcExecution("SRpc-2p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 2-params (sync): ", msg, ", ", value), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_2Params_Async(string msg, int value)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-2p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 2-params (async): ", msg, ", ", value), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 3-parameter ==========
    [ServerRpc]
    internal void ServerRpc_3Params_Sync(string msg, int value, float f)
    {
        LogRpcExecution("SRpc-3p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 3-params: ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_3Params_Async(string msg, int value, float f)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-3p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 3-params (async): ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 4-parameter ==========
    [ServerRpc]
    internal void ServerRpc_4Params_Sync(string msg, int value, float f, bool b)
    {
        LogRpcExecution("SRpc-4p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 4-params: ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_4Params_Async(string msg, int value, float f, bool b)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-4p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 4-params (async): ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 5-parameter ==========
    [ServerRpc]
    internal void ServerRpc_5Params_Sync(string msg, int value, float f, bool b, double d)
    {
        LogRpcExecution("SRpc-5p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 5-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_5Params_Async(string msg, int value, float f, bool b, double d)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-5p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 5-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 6-parameter ==========
    [ServerRpc]
    internal void ServerRpc_6Params_Sync(string msg, int value, float f, bool b, double d, long l)
    {
        LogRpcExecution("SRpc-6p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 6-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_6Params_Async(string msg, int value, float f, bool b, double d, long l)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-6p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 6-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 7-parameter ==========
    [ServerRpc]
    internal void ServerRpc_7Params_Sync(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        LogRpcExecution("SRpc-7p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 7-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_7Params_Async(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-7p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 7-params (async): ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
        return default;
    }

    // ========== ServerRpc Tests: 8-parameter ==========
    [ServerRpc]
    internal void ServerRpc_8Params_Sync(string msg, int v1, float f, bool b, double d, long l, byte bt, short s)
    {
        LogRpcExecution("SRpc-8p-s", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 8-params: ", msg, ", ", v1, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
    }

    [ServerRpc]
    internal async Task<RpcDeliveryReport> ServerRpc_8Params_Async(string msg, int v1, float f, bool b, double d, long l, byte bt, short s)
    {
        await Task.CompletedTask;
        LogRpcExecution("SRpc-8p-A", msg);
        GONetLog.Debug(string.Concat(nameof(ServerRpcAttribute), " 8-params (async): ", msg, ", ", v1, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
        return default;
    }

    #endregion

    #region ClientRpc Test Methods (9 methods: 9 param counts, sync only)

    // ========== ClientRpc Tests: 0-parameter ==========
    [ClientRpc]
    internal void ClientRpc_0Params()
    {
        LogRpcExecution("CRpc-0p");
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 0-params"), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 1-parameter ==========
    [ClientRpc]
    internal void ClientRpc_1Param(string message)
    {
        LogRpcExecution("CRpc-1p", message);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 1-param: ", message), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 2-parameter ==========
    [ClientRpc]
    internal void ClientRpc_2Params(string msg, int value)
    {
        LogRpcExecution("CRpc-2p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 2-params: ", msg, ", ", value), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 3-parameter ==========
    [ClientRpc]
    internal void ClientRpc_3Params(string msg, int value, float f)
    {
        LogRpcExecution("CRpc-3p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 3-params: ", msg, ", ", value, ", ", f), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 4-parameter ==========
    [ClientRpc]
    internal void ClientRpc_4Params(string msg, int value, float f, bool b)
    {
        LogRpcExecution("CRpc-4p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 4-params: ", msg, ", ", value, ", ", f, ", ", b), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 5-parameter ==========
    [ClientRpc]
    internal void ClientRpc_5Params(string msg, int value, float f, bool b, double d)
    {
        LogRpcExecution("CRpc-5p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 5-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 6-parameter ==========
    [ClientRpc]
    internal void ClientRpc_6Params(string msg, int value, float f, bool b, double d, long l)
    {
        LogRpcExecution("CRpc-6p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 6-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 7-parameter ==========
    [ClientRpc]
    internal void ClientRpc_7Params(string msg, int value, float f, bool b, double d, long l, byte bt)
    {
        LogRpcExecution("CRpc-7p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 7-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt), myRpcLogTelemetryProfile);
    }

    // ========== ClientRpc Tests: 8-parameter ==========
    [ClientRpc]
    internal void ClientRpc_8Params(string msg, int value, float f, bool b, double d, long l, byte bt, short s)
    {
        LogRpcExecution("CRpc-8p", msg);
        GONetLog.Debug(string.Concat(nameof(ClientRpcAttribute), " 8-params: ", msg, ", ", value, ", ", f, ", ", b, ", ", d, ", ", l, ", ", bt, ", ", s), myRpcLogTelemetryProfile);
    }

    #endregion

    #region Test Input Handling

    internal override void UpdateAfterGONetReady()
    {
        base.UpdateAfterGONetReady();

        /////////////////////////////////////////////////////////////////////////////////////////////////
        if (!Input.GetKey(KeyCode.LeftShift)) return; // left shift must be down for any of the rest!
        /////////////////////////////////////////////////////////////////////////////////////////////////


        // TargetRpc property for routing (needed by TargetRpc system)
        CurrentMessageTargets = new System.Collections.Generic.List<ushort> { GONetMain.OwnerAuthorityId_Server, 1, 2 };

        // Unique correlation ID per RPC invocation (for tracking)
        int correlationId = UnityEngine.Random.Range(111, 666); // used for correlation in logs

        const string INIT = "INITIATOR DREETSi";
        const string ASYNC_DONE = "ASYNC DONE DREETSi";

        // Shift+K: Dump RPC execution summary
        if (Input.GetKeyDown(KeyCode.K))
        {
            DumpRpcExecutionSummary();
        }

        // NOTE: Shift+A disabled - conflicts with GONetRpcAllTypesIntegrationTest
        // Use Shift+L (TargetRpc), Shift+S (ServerRpc), Shift+C (ClientRpc) instead
        /*
        // Shift+A: Run ALL tests applicable to this machine (auto-detect)
        if (Input.GetKeyDown(KeyCode.A))
        {
            GONetLog.Info("[GONetRpcComprehensiveTests] Running ALL applicable tests (Shift+A)...", myRpcLogTelemetryProfile);

            // TargetRpc tests (applicable to ALL machines)
            InvokeTest_TargetRpc_AllParamCounts();

            // ServerRpc tests (RunLocally=true allows server and clients to invoke)
            InvokeTest_ServerRpc_AllParamCounts();

            // ClientRpc tests (SERVER ONLY - only server can invoke ClientRpc)
            if (IsServer)
            {
                InvokeTest_ClientRpc_AllParamCounts();
            }

            GONetLog.Info("[GONetRpcComprehensiveTests] Completed ALL applicable tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
            return; // Early exit to prevent duplicate execution if L/C/S also pressed
        }
        */

        // Shift+L: TargetRpc comprehensive test (broadcasts to all machines)
        if (Input.GetKeyDown(KeyCode.L))
        {
            // Start tracking - use FIRST test ID encountered (stays set for all subsequent tests)
            if (currentTestId == -1)
            {
                currentTestId = correlationId;
            }
            GONetLog.Debug(string.Concat(correlationId, ' ', INIT), myRpcLogTelemetryProfile);

            const string MSG = "DREETSi Paul Blart logged everywhere via default TargetRpc setting of RpcTarget.All";

            // ========== 1-parameter tests (FIRST so remote machines can extract test ID and auto-set currentTestId) ==========
            CallRpc(nameof(LogOnAllMachines_NotValidated), string.Concat(correlationId, "-1p-nvs", ' ', MSG));
            CallRpc(nameof(LogOnAllMachines_Validated), string.Concat(correlationId, "-1p-Vs", ' ', MSG));
            CallRpcAsync<RpcDeliveryReport, string>(
                nameof(LogOnAllMachines_NotValidatedAsync),
                string.Concat(correlationId, "-1p-nvA", ' ', MSG))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-1p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string>(
                nameof(LogOnAllMachines_ValidatedAsync),
                string.Concat(correlationId, "-1p-VA", ' ', MSG))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-1p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 2-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_2Params_NotValidated), string.Concat(correlationId, "-2p", ' ', MSG), 42);
            CallRpc(nameof(LogOnAllMachines_2Params_Validated), string.Concat(correlationId, "-2p", ' ', MSG), 42);
            CallRpcAsync<RpcDeliveryReport, string, int>(
                nameof(LogOnAllMachines_2Params_NotValidatedAsync),
                string.Concat(correlationId, "-2p-nvA", ' ', MSG), 42)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-2p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int>(
                nameof(LogOnAllMachines_2Params_ValidatedAsync),
                string.Concat(correlationId, "-2p-VA", ' ', MSG), 42)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-2p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 3-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_3Params_NotValidated), string.Concat(correlationId, "-3p", ' ', MSG), 42, 3.14f);
            CallRpc(nameof(LogOnAllMachines_3Params_Validated), string.Concat(correlationId, "-3p", ' ', MSG), 42, 3.14f);
            CallRpcAsync<RpcDeliveryReport, string, int, float>(
                nameof(LogOnAllMachines_3Params_NotValidatedAsync),
                string.Concat(correlationId, "-3p-nvA", ' ', MSG), 42, 3.14f)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-3p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float>(
                nameof(LogOnAllMachines_3Params_ValidatedAsync),
                string.Concat(correlationId, "-3p-VA", ' ', MSG), 42, 3.14f)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-3p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 4-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_4Params_NotValidated), string.Concat(correlationId, "-4p", ' ', MSG), 42, 3.14f, true);
            CallRpc(nameof(LogOnAllMachines_4Params_Validated), string.Concat(correlationId, "-4p", ' ', MSG), 42, 3.14f, true);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool>(
                nameof(LogOnAllMachines_4Params_NotValidatedAsync),
                string.Concat(correlationId, "-4p-nvA", ' ', MSG), 42, 3.14f, true)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-4p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool>(
                nameof(LogOnAllMachines_4Params_ValidatedAsync),
                string.Concat(correlationId, "-4p-VA", ' ', MSG), 42, 3.14f, true)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-4p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 5-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_5Params_NotValidated), string.Concat(correlationId, "-5p", ' ', MSG), 42, 3.14f, true, 2.718);
            CallRpc(nameof(LogOnAllMachines_5Params_Validated), string.Concat(correlationId, "-5p", ' ', MSG), 42, 3.14f, true, 2.718);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double>(
                nameof(LogOnAllMachines_5Params_NotValidatedAsync),
                string.Concat(correlationId, "-5p-nvA", ' ', MSG), 42, 3.14f, true, 2.718)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-5p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double>(
                nameof(LogOnAllMachines_5Params_ValidatedAsync),
                string.Concat(correlationId, "-5p-VA", ' ', MSG), 42, 3.14f, true, 2.718)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-5p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 6-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_6Params_NotValidated), string.Concat(correlationId, "-6p", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
            CallRpc(nameof(LogOnAllMachines_6Params_Validated), string.Concat(correlationId, "-6p", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long>(
                nameof(LogOnAllMachines_6Params_NotValidatedAsync),
                string.Concat(correlationId, "-6p-nvA", ' ', MSG), 42, 3.14f, true, 2.718, 999L)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-6p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long>(
                nameof(LogOnAllMachines_6Params_ValidatedAsync),
                string.Concat(correlationId, "-6p-VA", ' ', MSG), 42, 3.14f, true, 2.718, 999L)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-6p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 7-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_7Params_NotValidated), string.Concat(correlationId, "-7p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
            CallRpc(nameof(LogOnAllMachines_7Params_Validated), string.Concat(correlationId, "-7p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte>(
                nameof(LogOnAllMachines_7Params_NotValidatedAsync),
                string.Concat(correlationId, "-7p-nvA", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-7p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte>(
                nameof(LogOnAllMachines_7Params_ValidatedAsync),
                string.Concat(correlationId, "-7p-VA", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-7p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 8-parameter tests ==========
            CallRpc(nameof(LogOnAllMachines_8Params_NotValidated), string.Concat(correlationId, "-8p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);
            CallRpc(nameof(LogOnAllMachines_8Params_Validated), string.Concat(correlationId, "-8p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte, short>(
                nameof(LogOnAllMachines_8Params_NotValidatedAsync),
                string.Concat(correlationId, "-8p-nvA", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-8p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte, short>(
                nameof(LogOnAllMachines_8Params_ValidatedAsync),
                string.Concat(correlationId, "-8p-VA", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-8p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));

            // ========== 0-parameter tests (LAST so currentTestId is already set from 1-param RPCs above) ==========
            CallRpc(nameof(LogOnAllMachines_0Params_NotValidated));
            CallRpc(nameof(LogOnAllMachines_0Params_Validated));
            CallRpcAsync<RpcDeliveryReport>(nameof(LogOnAllMachines_0Params_NotValidatedAsync))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-0p-nvA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport>(nameof(LogOnAllMachines_0Params_ValidatedAsync))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-0p-VA", ' ', ASYNC_DONE), myRpcLogTelemetryProfile));
        }

        // Shift+C: ServerRpc comprehensive test (RunLocally=true allows server and clients to invoke)
        if (Input.GetKeyDown(KeyCode.C))
        {
            // Start tracking - use FIRST test ID encountered (stays set for all subsequent tests)
            if (currentTestId == -1)
            {
                currentTestId = correlationId;
            }
            GONetLog.Debug(string.Concat(correlationId, " INITIATOR ServerRpc Test"), myRpcLogTelemetryProfile);

            const string MSG = "ServerRpc test message from client to server";

            // ========== 1-parameter tests (FIRST so server can extract test ID and auto-set currentTestId) ==========
            CallRpc(nameof(ServerRpc_1Param_Sync), string.Concat(correlationId, "-SRpc-1p-nvs", ' ', MSG));
            CallRpc(nameof(ServerRpc_1Param_Sync), string.Concat(correlationId, "-SRpc-1p-Vs", ' ', MSG));
            CallRpcAsync<RpcDeliveryReport, string>(
                nameof(ServerRpc_1Param_Async),
                string.Concat(correlationId, "-SRpc-1p-nvA", ' ', MSG))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-1p-nvA", " ASYNC DONE"), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string>(
                nameof(ServerRpc_1Param_Async),
                string.Concat(correlationId, "-SRpc-1p-VA", ' ', MSG))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-1p-VA", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 2-parameter tests ==========
            CallRpc(nameof(ServerRpc_2Params_Sync), string.Concat(correlationId, "-SRpc-2p-nvs", ' ', MSG), 42);
            CallRpc(nameof(ServerRpc_2Params_Sync), string.Concat(correlationId, "-SRpc-2p-Vs", ' ', MSG), 42);
            CallRpcAsync<RpcDeliveryReport, string, int>(
                nameof(ServerRpc_2Params_Async),
                string.Concat(correlationId, "-SRpc-2p-nvA", ' ', MSG), 42)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-2p-nvA", " ASYNC DONE"), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int>(
                nameof(ServerRpc_2Params_Async),
                string.Concat(correlationId, "-SRpc-2p-VA", ' ', MSG), 42)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-2p-VA", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 3-parameter tests ==========
            CallRpc(nameof(ServerRpc_3Params_Sync), string.Concat(correlationId, "-SRpc-3p-nvs", ' ', MSG), 42, 3.14f);
            CallRpc(nameof(ServerRpc_3Params_Sync), string.Concat(correlationId, "-SRpc-3p-Vs", ' ', MSG), 42, 3.14f);
            CallRpcAsync<RpcDeliveryReport, string, int, float>(
                nameof(ServerRpc_3Params_Async),
                string.Concat(correlationId, "-SRpc-3p-nvA", ' ', MSG), 42, 3.14f)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-3p-nvA", " ASYNC DONE"), myRpcLogTelemetryProfile));
            CallRpcAsync<RpcDeliveryReport, string, int, float>(
                nameof(ServerRpc_3Params_Async),
                string.Concat(correlationId, "-SRpc-3p-VA", ' ', MSG), 42, 3.14f)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-3p-VA", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 4-parameter tests ==========
            CallRpc(nameof(ServerRpc_4Params_Sync), string.Concat(correlationId, "-SRpc-4p-s", ' ', MSG), 42, 3.14f, true);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool>(
                nameof(ServerRpc_4Params_Async),
                string.Concat(correlationId, "-SRpc-4p-A", ' ', MSG), 42, 3.14f, true)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-4p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 5-parameter tests ==========
            CallRpc(nameof(ServerRpc_5Params_Sync), string.Concat(correlationId, "-SRpc-5p-s", ' ', MSG), 42, 3.14f, true, 2.718);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double>(
                nameof(ServerRpc_5Params_Async),
                string.Concat(correlationId, "-SRpc-5p-A", ' ', MSG), 42, 3.14f, true, 2.718)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-5p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 6-parameter tests ==========
            CallRpc(nameof(ServerRpc_6Params_Sync), string.Concat(correlationId, "-SRpc-6p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long>(
                nameof(ServerRpc_6Params_Async),
                string.Concat(correlationId, "-SRpc-6p-A", ' ', MSG), 42, 3.14f, true, 2.718, 999L)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-6p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 7-parameter tests ==========
            CallRpc(nameof(ServerRpc_7Params_Sync), string.Concat(correlationId, "-SRpc-7p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte>(
                nameof(ServerRpc_7Params_Async),
                string.Concat(correlationId, "-SRpc-7p-A", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-7p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 8-parameter tests ==========
            CallRpc(nameof(ServerRpc_8Params_Sync), string.Concat(correlationId, "-SRpc-8p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);
            CallRpcAsync<RpcDeliveryReport, string, int, float, bool, double, long, byte, short>(
                nameof(ServerRpc_8Params_Async),
                string.Concat(correlationId, "-SRpc-8p-A", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767)
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-8p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));

            // ========== 0-parameter tests (LAST so currentTestId is already set from 1-param RPCs above) ==========
            CallRpc(nameof(ServerRpc_0Params_Sync));
            CallRpcAsync<RpcDeliveryReport>(nameof(ServerRpc_0Params_Async))
                .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-0p-A", " ASYNC DONE"), myRpcLogTelemetryProfile));
        }

        // Shift+S: ClientRpc comprehensive test (server broadcasts to all clients)
        if (IsServer && Input.GetKeyDown(KeyCode.S))
        {
            // Start tracking - use FIRST test ID encountered (stays set for all subsequent tests)
            if (currentTestId == -1)
            {
                currentTestId = correlationId;
            }
            GONetLog.Debug(string.Concat(correlationId, " INITIATOR ClientRpc Test"), myRpcLogTelemetryProfile);

            const string MSG = "ClientRpc test message from server to all clients";

            // ========== 1-parameter test ==========
            CallRpc(nameof(ClientRpc_1Param), string.Concat(correlationId, "-CRpc-1p", ' ', MSG));

            // ========== 2-parameter test ==========
            CallRpc(nameof(ClientRpc_2Params), string.Concat(correlationId, "-CRpc-2p", ' ', MSG), 42);

            // ========== 3-parameter test ==========
            CallRpc(nameof(ClientRpc_3Params), string.Concat(correlationId, "-CRpc-3p", ' ', MSG), 42, 3.14f);

            // ========== 4-parameter test ==========
            CallRpc(nameof(ClientRpc_4Params), string.Concat(correlationId, "-CRpc-4p", ' ', MSG), 42, 3.14f, true);

            // ========== 5-parameter test ==========
            CallRpc(nameof(ClientRpc_5Params), string.Concat(correlationId, "-CRpc-5p", ' ', MSG), 42, 3.14f, true, 2.718);

            // ========== 6-parameter test ==========
            CallRpc(nameof(ClientRpc_6Params), string.Concat(correlationId, "-CRpc-6p", ' ', MSG), 42, 3.14f, true, 2.718, 999L);

            // ========== 7-parameter test ==========
            CallRpc(nameof(ClientRpc_7Params), string.Concat(correlationId, "-CRpc-7p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);

            // ========== 8-parameter test ==========
            CallRpc(nameof(ClientRpc_8Params), string.Concat(correlationId, "-CRpc-8p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);

            // ========== 0-parameter test (LAST so currentTestId is already set from 1-param RPCs above) ==========
            CallRpc(nameof(ClientRpc_0Params));
        }
    }

    // TargetRpc property (required for TargetRpc routing)
    public System.Collections.Generic.List<ushort> CurrentMessageTargets { get; set; } = new System.Collections.Generic.List<ushort>();

    #endregion

    #region UI Test Registration (NEW - for RpcTestRunnerUI)

    private void RegisterTestsWithUI()
    {
        // Register TargetRpc comprehensive test (all param counts with RpcTarget.All)
        GONet.Sample.RpcTests.RpcTestRegistry.RegisterTest(
            GONet.Sample.RpcTests.RpcTestRegistry.TestCategory.TargetRpc_Targeting,
            new GONet.Sample.RpcTests.RpcTestRegistry.TestDescriptor
            {
                Name = "TargetRpc - All Param Counts (Comprehensive)",
                Description = "Tests TargetRpc with RpcTarget.All (default) across all parameter counts (0-8).\n\n" +
                             "Includes 4 variants per param count:\n" +
                             "• NotValidated (sync)\n" +
                             "• Validated (sync with AlwaysAllow validator)\n" +
                             "• NotValidatedAsync (async RpcDeliveryReport)\n" +
                             "• ValidatedAsync (async RpcDeliveryReport)\n\n" +
                             "Total: 36 RPC methods executed.",
                ExpectedResult = "All machines execute all TargetRpc calls (36 total).\n\n" +
                                "Each machine should log execution for:\n" +
                                "• 0-8 param counts\n" +
                                "• All 4 variants per count\n" +
                                "• Total 36 executions per machine",
                ApplicableMachines = GONet.Sample.RpcTests.RpcTestRegistry.MachineRequirement.All,
                InvokeTest = InvokeTest_TargetRpc_AllParamCounts
            }
        );

        // Register ServerRpc comprehensive test (all param counts, RunLocally=true default)
        GONet.Sample.RpcTests.RpcTestRegistry.RegisterTest(
            GONet.Sample.RpcTests.RpcTestRegistry.TestCategory.ServerRpc_Execution,
            new GONet.Sample.RpcTests.RpcTestRegistry.TestDescriptor
            {
                Name = "ServerRpc - All Param Counts (RunLocally=true)",
                Description = "Tests ServerRpc with RunLocally=true (default) across all parameter counts (0-8).\n\n" +
                             "Includes 2 variants per param count:\n" +
                             "• Sync (void return)\n" +
                             "• Async (Task<RpcDeliveryReport> return)\n\n" +
                             "Total: 18 RPC methods executed.\n\n" +
                             "NOTE: Server executes ServerRpc locally using reflection when called on server.\n" +
                             "Clients send ServerRpc to server via network.",
                ExpectedResult = "Server executes all ServerRpc calls (18 total).\n" +
                                "When called on server: Executes locally via reflection.\n" +
                                "When called on client: Routes to server, server executes.\n\n" +
                                "Clients receive async responses for async variants.",
                ApplicableMachines = GONet.Sample.RpcTests.RpcTestRegistry.MachineRequirement.All,
                InvokeTest = InvokeTest_ServerRpc_AllParamCounts
            }
        );

        // Register ClientRpc comprehensive test (all param counts)
        GONet.Sample.RpcTests.RpcTestRegistry.RegisterTest(
            GONet.Sample.RpcTests.RpcTestRegistry.TestCategory.TargetRpc_Targeting,
            new GONet.Sample.RpcTests.RpcTestRegistry.TestDescriptor
            {
                Name = "ClientRpc - All Param Counts",
                Description = "Tests ClientRpc across all parameter counts (0-8).\n\n" +
                             "Server broadcasts to all clients.\n" +
                             "Total: 9 RPC methods executed.\n\n" +
                             "NOTE: Only server can call ClientRpc.",
                ExpectedResult = "All clients execute ClientRpc calls (9 total).\n" +
                                "Server does NOT execute locally in dedicated server mode.\n\n" +
                                "Each client should log 9 executions (0-8 param counts).",
                ApplicableMachines = GONet.Sample.RpcTests.RpcTestRegistry.MachineRequirement.ServerOnly,
                InvokeTest = InvokeTest_ClientRpc_AllParamCounts
            }
        );
    }

    /// <summary>
    /// UI wrapper for TargetRpc comprehensive test (Shift+L equivalent).
    /// </summary>
    private void InvokeTest_TargetRpc_AllParamCounts()
    {
        // Setup
        CurrentMessageTargets = new System.Collections.Generic.List<ushort> { GONetMain.OwnerAuthorityId_Server, 1, 2 };
        int correlationId = UnityEngine.Random.Range(111, 666);
        if (currentTestId == -1) currentTestId = correlationId;

        GONetLog.Info($"[UI TEST START] TargetRpc All Param Counts (ID: {correlationId})", myRpcLogTelemetryProfile);

        const string MSG = "UI-invoked TargetRpc test (RpcTarget.All)";

        // Execute same logic as Shift+L (copy from UpdateAfterGONetReady)
        // ========== 1-parameter tests (FIRST so remote machines can extract test ID) ==========
        CallRpc(nameof(LogOnAllMachines_NotValidated), string.Concat(correlationId, "-1p-nvs", ' ', MSG));
        CallRpc(nameof(LogOnAllMachines_Validated), string.Concat(correlationId, "-1p-Vs", ' ', MSG));
        CallRpcAsync<RpcDeliveryReport, string>(
            nameof(LogOnAllMachines_NotValidatedAsync),
            string.Concat(correlationId, "-1p-nvA", ' ', MSG))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-1p-nvA ASYNC DONE"), myRpcLogTelemetryProfile));
        CallRpcAsync<RpcDeliveryReport, string>(
            nameof(LogOnAllMachines_ValidatedAsync),
            string.Concat(correlationId, "-1p-VA", ' ', MSG))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-1p-VA ASYNC DONE"), myRpcLogTelemetryProfile));

        // ========== 2-parameter tests ==========
        CallRpc(nameof(LogOnAllMachines_2Params_NotValidated), string.Concat(correlationId, "-2p", ' ', MSG), 42);
        CallRpc(nameof(LogOnAllMachines_2Params_Validated), string.Concat(correlationId, "-2p", ' ', MSG), 42);
        CallRpcAsync<RpcDeliveryReport, string, int>(
            nameof(LogOnAllMachines_2Params_NotValidatedAsync),
            string.Concat(correlationId, "-2p-nvA", ' ', MSG), 42)
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-2p-nvA ASYNC DONE"), myRpcLogTelemetryProfile));
        CallRpcAsync<RpcDeliveryReport, string, int>(
            nameof(LogOnAllMachines_2Params_ValidatedAsync),
            string.Concat(correlationId, "-2p-VA", ' ', MSG), 42)
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-2p-VA ASYNC DONE"), myRpcLogTelemetryProfile));

        // Continue with remaining param counts (3-8)...
        CallRpc(nameof(LogOnAllMachines_3Params_NotValidated), string.Concat(correlationId, "-3p", ' ', MSG), 42, 3.14f);
        CallRpc(nameof(LogOnAllMachines_4Params_NotValidated), string.Concat(correlationId, "-4p", ' ', MSG), 42, 3.14f, true);
        CallRpc(nameof(LogOnAllMachines_5Params_NotValidated), string.Concat(correlationId, "-5p", ' ', MSG), 42, 3.14f, true, 2.718);
        CallRpc(nameof(LogOnAllMachines_6Params_NotValidated), string.Concat(correlationId, "-6p", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
        CallRpc(nameof(LogOnAllMachines_7Params_NotValidated), string.Concat(correlationId, "-7p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
        CallRpc(nameof(LogOnAllMachines_8Params_NotValidated), string.Concat(correlationId, "-8p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);

        // ========== 0-parameter tests (LAST) ==========
        CallRpc(nameof(LogOnAllMachines_0Params_NotValidated));
        CallRpc(nameof(LogOnAllMachines_0Params_Validated));
        CallRpcAsync<RpcDeliveryReport>(nameof(LogOnAllMachines_0Params_NotValidatedAsync))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-0p-nvA ASYNC DONE"), myRpcLogTelemetryProfile));
        CallRpcAsync<RpcDeliveryReport>(nameof(LogOnAllMachines_0Params_ValidatedAsync))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-0p-VA ASYNC DONE"), myRpcLogTelemetryProfile));

        GONetLog.Info($"[UI TEST END] TargetRpc All Param Counts invoked. Check logs (Shift+K for summary).", myRpcLogTelemetryProfile);
    }

    /// <summary>
    /// UI wrapper for ServerRpc comprehensive test (Shift+C equivalent).
    /// Tests ServerRpc with RunLocally=true (default) - can be invoked from clients OR server.
    /// </summary>
    private void InvokeTest_ServerRpc_AllParamCounts()
    {
        int correlationId = UnityEngine.Random.Range(111, 666);
        if (currentTestId == -1) currentTestId = correlationId;

        GONetLog.Info($"[UI TEST START] ServerRpc All Param Counts (ID: {correlationId})", myRpcLogTelemetryProfile);

        const string MSG = "UI-invoked ServerRpc test";

        // Execute same logic as Shift+C
        CallRpc(nameof(ServerRpc_1Param_Sync), string.Concat(correlationId, "-SRpc-1p-s", ' ', MSG));
        CallRpcAsync<RpcDeliveryReport, string>(
            nameof(ServerRpc_1Param_Async),
            string.Concat(correlationId, "-SRpc-1p-A", ' ', MSG))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-1p-A ASYNC DONE"), myRpcLogTelemetryProfile));

        CallRpc(nameof(ServerRpc_2Params_Sync), string.Concat(correlationId, "-SRpc-2p-s", ' ', MSG), 42);
        CallRpc(nameof(ServerRpc_3Params_Sync), string.Concat(correlationId, "-SRpc-3p-s", ' ', MSG), 42, 3.14f);
        CallRpc(nameof(ServerRpc_4Params_Sync), string.Concat(correlationId, "-SRpc-4p-s", ' ', MSG), 42, 3.14f, true);
        CallRpc(nameof(ServerRpc_5Params_Sync), string.Concat(correlationId, "-SRpc-5p-s", ' ', MSG), 42, 3.14f, true, 2.718);
        CallRpc(nameof(ServerRpc_6Params_Sync), string.Concat(correlationId, "-SRpc-6p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
        CallRpc(nameof(ServerRpc_7Params_Sync), string.Concat(correlationId, "-SRpc-7p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
        CallRpc(nameof(ServerRpc_8Params_Sync), string.Concat(correlationId, "-SRpc-8p-s", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);

        CallRpc(nameof(ServerRpc_0Params_Sync));
        CallRpcAsync<RpcDeliveryReport>(nameof(ServerRpc_0Params_Async))
            .ContinueWith(task => GONetLog.Debug(string.Concat(correlationId, "-SRpc-0p-A ASYNC DONE"), myRpcLogTelemetryProfile));

        GONetLog.Info($"[UI TEST END] ServerRpc All Param Counts invoked. Server should execute all RPCs.", myRpcLogTelemetryProfile);
    }

    /// <summary>
    /// UI wrapper for ClientRpc comprehensive test (Shift+S equivalent).
    /// </summary>
    private void InvokeTest_ClientRpc_AllParamCounts()
    {
        if (!IsServer)
        {
            GONetLog.Warning("[UI TEST] ClientRpc tests can only be invoked from server");
            return;
        }

        int correlationId = UnityEngine.Random.Range(111, 666);
        if (currentTestId == -1) currentTestId = correlationId;

        GONetLog.Info($"[UI TEST START] ClientRpc All Param Counts (ID: {correlationId})", myRpcLogTelemetryProfile);

        const string MSG = "UI-invoked ClientRpc test";

        // Execute same logic as Shift+S
        CallRpc(nameof(ClientRpc_1Param), string.Concat(correlationId, "-CRpc-1p", ' ', MSG));
        CallRpc(nameof(ClientRpc_2Params), string.Concat(correlationId, "-CRpc-2p", ' ', MSG), 42);
        CallRpc(nameof(ClientRpc_3Params), string.Concat(correlationId, "-CRpc-3p", ' ', MSG), 42, 3.14f);
        CallRpc(nameof(ClientRpc_4Params), string.Concat(correlationId, "-CRpc-4p", ' ', MSG), 42, 3.14f, true);
        CallRpc(nameof(ClientRpc_5Params), string.Concat(correlationId, "-CRpc-5p", ' ', MSG), 42, 3.14f, true, 2.718);
        CallRpc(nameof(ClientRpc_6Params), string.Concat(correlationId, "-CRpc-6p", ' ', MSG), 42, 3.14f, true, 2.718, 999L);
        CallRpc(nameof(ClientRpc_7Params), string.Concat(correlationId, "-CRpc-7p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255);
        CallRpc(nameof(ClientRpc_8Params), string.Concat(correlationId, "-CRpc-8p", ' ', MSG), 42, 3.14f, true, 2.718, 999L, (byte)255, (short)32767);
        CallRpc(nameof(ClientRpc_0Params));

        GONetLog.Info($"[UI TEST END] ClientRpc All Param Counts invoked. Clients should execute all RPCs.", myRpcLogTelemetryProfile);
    }

    #endregion
}
