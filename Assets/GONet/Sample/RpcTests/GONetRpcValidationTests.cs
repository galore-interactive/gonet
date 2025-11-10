using GONet;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Tests for RPC validation system.
    /// Covers: Sync validators, async validators, parameter modification, DenyAll patterns.
    ///
    /// Invoked via Shift+V keyboard shortcut.
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcValidationTests : GONetParticipantCompanionBehaviour
    {
        #region RPC Execution Tracker

        private static readonly ConcurrentBag<string> rpcExecutionLog = new ConcurrentBag<string>();
        private static int currentTestId = -1;

        private static void LogRpcExecution(string rpcVariant, string messageWithTestId = null)
        {
            int testId = ExtractTestIdFromMessage(messageWithTestId);

            if (testId == -1)
            {
                testId = currentTestId;
            }

            if (testId == -1) return;

            if (currentTestId == -1)
            {
                currentTestId = testId;
            }

            string machine = GONetMain.IsServer ? "Server" : (GONetMain.MyAuthorityId == 1 ? "Client:1" : $"Client:{GONetMain.MyAuthorityId}");
            rpcExecutionLog.Add($"{testId}-{rpcVariant}|{machine}");
        }

        private static int ExtractTestIdFromMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return -1;

            int dashIndex = message.IndexOf('-');
            if (dashIndex == -1 || dashIndex == 0) return -1;

            string testIdStr = message.Substring(0, dashIndex);
            if (int.TryParse(testIdStr, out int testId))
            {
                return testId;
            }

            return -1;
        }

        private void DumpRpcExecutionSummary()
        {
            if (rpcExecutionLog.Count == 0)
            {
                GONetLog.Info("No RPC executions recorded", myRpcLogTelemetryProfile);
                return;
            }

            var grouped = rpcExecutionLog.GroupBy(entry => entry.Split('|')[0])
                                          .OrderBy(g => g.Key);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("========== RPC VALIDATION TEST EXECUTION SUMMARY ==========");

            foreach (var group in grouped)
            {
                string variant = group.Key;
                var machines = group.Select(entry => entry.Split('|')[1]).Distinct().OrderBy(m => m).ToList();
                int count = group.Count();
                sb.AppendLine($"{variant}: {count} executions on [{string.Join(", ", machines)}]");
            }

            sb.AppendLine($"Total executions: {rpcExecutionLog.Count}");
            sb.AppendLine("===========================================================");

            GONetLog.Info(sb.ToString(), myRpcLogTelemetryProfile);
        }

        string myRpcLogTelemetryProfile;
        private void InitTelemetryLogging()
        {
            myRpcLogTelemetryProfile = $"RpcValidation-{(GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}")}";
            GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(myRpcLogTelemetryProfile, outputToSeparateFile: true));
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitTelemetryLogging();
        }

        private void OnApplicationQuit()
        {
            DumpRpcExecutionSummary();
        }

        internal override void UpdateAfterGONetReady()
        {
            base.UpdateAfterGONetReady();

            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Shift+K: Dump execution summary
            if (shiftPressed && Input.GetKeyDown(KeyCode.K))
            {
                DumpRpcExecutionSummary();
            }

            // Shift+V: Run ALL validation tests
            if (shiftPressed && Input.GetKeyDown(KeyCode.V))
            {
                GONetLog.Info("[GONetRpcValidationTests] Running ALL validation tests (Shift+V)...", myRpcLogTelemetryProfile);
                InvokeTest_SyncValidator_AllowAll();
                InvokeTest_SyncValidator_DenyAll();
                InvokeTest_SyncValidator_AllowSpecific();
                InvokeTest_AsyncValidator_AllowAll();
                InvokeTest_AsyncValidator_DenyAll();
                InvokeTest_Validator_ParameterModification();
                InvokeTest_Validator_SelectiveTargeting();
                GONetLog.Info("[GONetRpcValidationTests] Completed ALL validation tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test 1: Sync Validator - AllowAll

        [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate_AllowAll))]
        internal void ValidatedRpc_AllowAll(string message)
        {
            LogRpcExecution("ValidatedRpc_AllowAll", message);
            GONetLog.Debug($"[RpcValidation] AllowAll RPC executed: {message}", myRpcLogTelemetryProfile);
        }

        internal RpcValidationResult Validate_AllowAll()
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

        public void InvokeTest_SyncValidator_AllowAll()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-AllowAll test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpc_AllowAll), msg);
        }

        #endregion

        #region Test 2: Sync Validator - DenyAll

        [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate_DenyAll))]
        internal void ValidatedRpc_DenyAll(string message)
        {
            LogRpcExecution("ValidatedRpc_DenyAll", message);
            GONetLog.Debug($"[RpcValidation] DenyAll RPC executed (should NOT happen): {message}", myRpcLogTelemetryProfile);
        }

        internal RpcValidationResult Validate_DenyAll()
        {
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                var result = RpcValidationResult.CreatePreAllocated(1);
                result.DenyAll();
                return result;
            }

            var validationResult = context.Value.GetValidationResult();
            validationResult.DenyAll();
            return validationResult;
        }

        public void InvokeTest_SyncValidator_DenyAll()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-DenyAll test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpc_DenyAll), msg);
            GONetLog.Info($"[RpcValidation] DenyAll test invoked - RPC should be blocked, check execution count is 0", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 3: Sync Validator - Allow Specific Targets

        [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate_AllowClient1Only))]
        internal void ValidatedRpc_AllowClient1Only(string message)
        {
            LogRpcExecution("ValidatedRpc_AllowClient1Only", message);
            GONetLog.Debug($"[RpcValidation] AllowClient1Only RPC executed: {message}", myRpcLogTelemetryProfile);
        }

        internal RpcValidationResult Validate_AllowClient1Only()
        {
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                var result = RpcValidationResult.CreatePreAllocated(1);
                result.AllowTarget(1); // Only Client:1
                return result;
            }

            var validationResult = context.Value.GetValidationResult();
            validationResult.AllowTarget(1); // Only Client:1
            return validationResult;
        }

        public void InvokeTest_SyncValidator_AllowSpecific()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-AllowSpecific test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpc_AllowClient1Only), msg);
        }

        #endregion

        #region Test 4: Async Validator - AllowAll

        [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateAsync_AllowAll))]
        internal void ValidatedRpcAsync_AllowAll(string message)
        {
            LogRpcExecution("ValidatedRpcAsync_AllowAll", message);
            GONetLog.Debug($"[RpcValidation] Async AllowAll RPC executed: {message}", myRpcLogTelemetryProfile);
        }

        internal async Task<RpcValidationResult> ValidateAsync_AllowAll()
        {
            await Task.Delay(10); // Simulate async work (e.g., database lookup)

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

        public void InvokeTest_AsyncValidator_AllowAll()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-AsyncAllowAll test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpcAsync_AllowAll), msg);
        }

        #endregion

        #region Test 5: Async Validator - DenyAll

        [TargetRpc(RpcTarget.All, validationMethod: nameof(ValidateAsync_DenyAll))]
        internal void ValidatedRpcAsync_DenyAll(string message)
        {
            LogRpcExecution("ValidatedRpcAsync_DenyAll", message);
            GONetLog.Debug($"[RpcValidation] Async DenyAll RPC executed (should NOT happen): {message}", myRpcLogTelemetryProfile);
        }

        internal async Task<RpcValidationResult> ValidateAsync_DenyAll()
        {
            await Task.Delay(10); // Simulate async work (e.g., profanity check API)

            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                var result = RpcValidationResult.CreatePreAllocated(1);
                result.DenyAll();
                return result;
            }

            var validationResult = context.Value.GetValidationResult();
            validationResult.DenyAll();
            return validationResult;
        }

        public void InvokeTest_AsyncValidator_DenyAll()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-AsyncDenyAll test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpcAsync_DenyAll), msg);
            GONetLog.Info($"[RpcValidation] AsyncDenyAll test invoked - RPC should be blocked, check execution count is 0", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 6: Validator with Parameter Modification

        [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate_ModifyParameter))]
        internal void ValidatedRpc_WithModification(string message, int value)
        {
            LogRpcExecution("ValidatedRpc_WithModification", message);
            GONetLog.Debug($"[RpcValidation] Modified RPC executed: {message}, value={value}", myRpcLogTelemetryProfile);
        }

        internal RpcValidationResult Validate_ModifyParameter(string message, int value)
        {
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                var result = RpcValidationResult.CreatePreAllocated(1);
                result.AllowAll();
                return result;
            }

            var validationResult = context.Value.GetValidationResult();

            // Modify parameter (e.g., profanity filtering: replace bad words with ***)
            var modifiedMessage = message.Replace("badword", "***");
            if (modifiedMessage != message)
            {
                validationResult.SetValidatedOverride(0, modifiedMessage);
            }

            // Clamp value parameter (e.g., prevent cheating: max value = 100)
            var clampedValue = Mathf.Clamp(value, 0, 100);
            if (clampedValue != value)
            {
                validationResult.SetValidatedOverride(1, clampedValue);
            }

            validationResult.AllowAll();
            return validationResult;
        }

        public void InvokeTest_Validator_ParameterModification()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ParamMod test with badword from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpc_WithModification), msg, 9999); // 9999 should be clamped to 100
        }

        #endregion

        #region Test 7: Validator with Selective Targeting (AllowTarget + DenyTarget)

        [TargetRpc(RpcTarget.All, validationMethod: nameof(Validate_SelectiveTargeting))]
        internal void ValidatedRpc_SelectiveTargeting(string message)
        {
            LogRpcExecution("ValidatedRpc_SelectiveTargeting", message);
            GONetLog.Debug($"[RpcValidation] Selective RPC executed: {message}", myRpcLogTelemetryProfile);
        }

        internal RpcValidationResult Validate_SelectiveTargeting()
        {
            var context = GONetMain.EventBus.GetValidationContext();
            if (!context.HasValue)
            {
                var result = RpcValidationResult.CreatePreAllocated(3);
                result.AllowTarget(GONetMain.OwnerAuthorityId_Server); // Allow server
                result.AllowTarget(1); // Allow Client:1
                // Client:2 NOT allowed (not explicitly added)
                return result;
            }

            var validationResult = context.Value.GetValidationResult();
            validationResult.AllowTarget(GONetMain.OwnerAuthorityId_Server); // Allow server
            validationResult.AllowTarget(1); // Allow Client:1
            // Client:2 NOT allowed
            return validationResult;
        }

        public void InvokeTest_Validator_SelectiveTargeting()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-Selective test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(ValidatedRpc_SelectiveTargeting), msg);
        }

        #endregion
    }
}
