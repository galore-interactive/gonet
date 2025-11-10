using GONet;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Tests for TargetRpc targeting modes.
    /// Covers: RpcTarget.Owner, RpcTarget.Others, RpcTarget.SpecificAuthority, RpcTarget.MultipleAuthorities,
    /// and property-based targeting (single + multiple).
    ///
    /// Invoked via RpcTestRunnerUI (Shift+R).
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcTargetingTests : GONetParticipantCompanionBehaviour
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
            sb.AppendLine("========== RPC TARGETING TEST EXECUTION SUMMARY ==========");

            foreach (var group in grouped)
            {
                string variant = group.Key;
                var machines = group.Select(entry => entry.Split('|')[1]).Distinct().OrderBy(m => m).ToList();
                int count = group.Count();
                sb.AppendLine($"{variant}: {count} executions on [{string.Join(", ", machines)}]");
            }

            sb.AppendLine($"Total executions: {rpcExecutionLog.Count}");
            sb.AppendLine("==========================================================");

            GONetLog.Info(sb.ToString(), myRpcLogTelemetryProfile);
        }

        string myRpcLogTelemetryProfile;
        private void InitTelemetryLogging()
        {
            myRpcLogTelemetryProfile = $"RpcTargeting-{(GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}")}";
            GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(myRpcLogTelemetryProfile, outputToSeparateFile: true));
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitTelemetryLogging();
            RegisterTestsWithUI();
        }

        private void OnApplicationQuit()
        {
            DumpRpcExecutionSummary();
        }

        internal override void UpdateAfterGONetReady()
        {
            base.UpdateAfterGONetReady();

            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Shift+K: Dump execution summary (same as GONetRpcComprehensiveTests)
            if (shiftPressed && Input.GetKeyDown(KeyCode.K))
            {
                DumpRpcExecutionSummary();
            }

            // Shift+T: Run ALL targeting mode tests (applicable to all machines)
            if (shiftPressed && Input.GetKeyDown(KeyCode.T))
            {
                GONetLog.Info("[GONetRpcTargetingTests] Running ALL targeting mode tests (Shift+T)...", myRpcLogTelemetryProfile);
                InvokeTest_TargetRpc_Owner();
                InvokeTest_TargetRpc_Others();
                InvokeTest_TargetRpc_SpecificAuthority();
                InvokeTest_TargetRpc_MultipleAuthorities();
                InvokeTest_TargetRpc_PropertySingle();
                InvokeTest_TargetRpc_PropertyMultiple();
                GONetLog.Info("[GONetRpcTargetingTests] Completed ALL targeting mode tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test Registration

        private void RegisterTestsWithUI()
        {
            // RpcTarget.Owner tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "RpcTarget.Owner (0+1 params)",
                    Description = "Tests RpcTarget.Owner targeting mode. Only the owner of this GONetParticipant should receive the RPC.",
                    ExpectedResult = "• Owner machine: Executes RPC (logs execution)\n• Non-owner machines: Do NOT execute\n\nNote: Ownership is assigned when object is spawned. This test object is owned by the machine that created the scene.",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_Owner
                }
            );

            // RpcTarget.Others tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "RpcTarget.Others (0+1 params)",
                    Description = "Tests RpcTarget.Others targeting mode. All machines EXCEPT the caller should receive the RPC.",
                    ExpectedResult = "• Caller machine: Does NOT execute\n• All other machines: Execute RPC\n\nExample: If Client:1 calls the RPC, Server and Client:2 execute, but Client:1 does NOT.",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_Others
                }
            );

            // RpcTarget.SpecificAuthority tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "RpcTarget.SpecificAuthority (1+2 params)",
                    Description = "Tests RpcTarget.SpecificAuthority (parameter-based single target). First parameter specifies target authority ID.",
                    ExpectedResult = "• Target machine (specified in first parameter): Executes RPC\n• All other machines: Do NOT execute\n\nThis test sends to Client:1 (authority ID 1).",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_SpecificAuthority
                }
            );

            // RpcTarget.MultipleAuthorities tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "RpcTarget.MultipleAuthorities (1+2 params)",
                    Description = "Tests RpcTarget.MultipleAuthorities (parameter-based multiple targets). First parameter is List<ushort> of target authority IDs.",
                    ExpectedResult = "• Target machines (specified in first parameter list): Execute RPC\n• All other machines: Do NOT execute\n\nThis test sends to Server + Client:1 (IDs 1023 and 1).",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_MultipleAuthorities
                }
            );

            // Property-based single target tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "Property-based single target (1 param)",
                    Description = "Tests property-based single target (via targetPropertyName). The TargetPlayerId property specifies the recipient.",
                    ExpectedResult = "• Target machine (specified in TargetPlayerId property): Executes RPC\n• All other machines: Do NOT execute\n\nThis test sets TargetPlayerId=1 (Client:1) before calling RPC.",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_PropertySingle
                }
            );

            // Property-based multiple targets tests
            RpcTestRegistry.RegisterTest(
                RpcTestRegistry.TestCategory.TargetRpc_Targeting,
                new RpcTestRegistry.TestDescriptor
                {
                    Name = "Property-based multiple targets (1 param)",
                    Description = "Tests property-based multiple targets (via targetPropertyName + isMultipleTargets=true). The TeamMembers property (List<ushort>) specifies recipients.",
                    ExpectedResult = "• Target machines (specified in TeamMembers list): Execute RPC\n• All other machines: Do NOT execute\n\nThis test sets TeamMembers=[Server, Client:1] before calling RPC.",
                    ApplicableMachines = RpcTestRegistry.MachineRequirement.All,
                    InvokeTest = InvokeTest_TargetRpc_PropertyMultiple
                }
            );
        }

        #endregion

        #region TargetRpc Test Methods

        // ========== RpcTarget.Owner tests ==========
        [TargetRpc(RpcTarget.Owner)]
        internal void NotifyOwner_0Params()
        {
            LogRpcExecution("Owner-0p");
            GONetLog.Debug("TargetRpc RpcTarget.Owner 0-params executed", myRpcLogTelemetryProfile);
        }

        [TargetRpc(RpcTarget.Owner)]
        internal void NotifyOwner_1Param(string message)
        {
            LogRpcExecution("Owner-1p", message);
            GONetLog.Debug($"TargetRpc RpcTarget.Owner 1-param: {message}", myRpcLogTelemetryProfile);
        }

        // ========== RpcTarget.Others tests ==========
        [TargetRpc(RpcTarget.Others)]
        internal void NotifyOthers_0Params()
        {
            LogRpcExecution("Others-0p");
            GONetLog.Debug("TargetRpc RpcTarget.Others 0-params executed", myRpcLogTelemetryProfile);
        }

        [TargetRpc(RpcTarget.Others)]
        internal void NotifyOthers_1Param(string message)
        {
            LogRpcExecution("Others-1p", message);
            GONetLog.Debug($"TargetRpc RpcTarget.Others 1-param: {message}", myRpcLogTelemetryProfile);
        }

        // ========== RpcTarget.SpecificAuthority tests (parameter-based single target) ==========
        [TargetRpc(RpcTarget.SpecificAuthority)]
        internal void SendToPlayer_1Param(ushort targetPlayerId, string message)
        {
            LogRpcExecution("SpecificAuth-1p", message);
            GONetLog.Debug($"TargetRpc RpcTarget.SpecificAuthority 1-param (target={targetPlayerId}): {message}", myRpcLogTelemetryProfile);
        }

        [TargetRpc(RpcTarget.SpecificAuthority)]
        internal void SendToPlayer_2Params(ushort targetPlayerId, string message, int value)
        {
            LogRpcExecution("SpecificAuth-2p", message);
            GONetLog.Debug($"TargetRpc RpcTarget.SpecificAuthority 2-params (target={targetPlayerId}): {message}, {value}", myRpcLogTelemetryProfile);
        }

        // ========== RpcTarget.MultipleAuthorities tests (parameter-based multiple targets) ==========
        [TargetRpc(RpcTarget.MultipleAuthorities)]
        internal void SendToTeam_1Param(List<ushort> teamIds, string message)
        {
            LogRpcExecution("MultiAuth-1p", message);
            string teamList = string.Join(", ", teamIds);
            GONetLog.Debug($"TargetRpc RpcTarget.MultipleAuthorities 1-param (targets=[{teamList}]): {message}", myRpcLogTelemetryProfile);
        }

        [TargetRpc(RpcTarget.MultipleAuthorities)]
        internal void SendToTeam_2Params(List<ushort> teamIds, string message, int value)
        {
            LogRpcExecution("MultiAuth-2p", message);
            string teamList = string.Join(", ", teamIds);
            GONetLog.Debug($"TargetRpc RpcTarget.MultipleAuthorities 2-params (targets=[{teamList}]): {message}, {value}", myRpcLogTelemetryProfile);
        }

        // ========== Property-based single target ==========
        public ushort TargetPlayerId { get; set; }

        [TargetRpc(nameof(TargetPlayerId))]
        internal void SendToPropertySingle(string message)
        {
            LogRpcExecution("PropSingle", message);
            GONetLog.Debug($"TargetRpc Property-based single (target={TargetPlayerId}): {message}", myRpcLogTelemetryProfile);
        }

        // ========== Property-based multiple targets ==========
        public List<ushort> TeamMembers { get; set; } = new List<ushort>();

        [TargetRpc(nameof(TeamMembers), isMultipleTargets: true)]
        internal void SendToPropertyMultiple(string message)
        {
            LogRpcExecution("PropMulti", message);
            string teamList = string.Join(", ", TeamMembers);
            GONetLog.Debug($"TargetRpc Property-based multiple (targets=[{teamList}]): {message}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Public Test Invocation API (called by RpcTestRunnerUI)

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "RpcTarget.Owner" test.
        /// </summary>
        public void InvokeTest_TargetRpc_Owner()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            string msg = $"{correlationId}-Owner test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";

            GONetLog.Info($"[TEST START] RpcTarget.Owner test (ID: {correlationId})", myRpcLogTelemetryProfile);

            CallRpc(nameof(NotifyOwner_0Params));
            CallRpc(nameof(NotifyOwner_1Param), msg);

            GONetLog.Info($"[TEST END] RpcTarget.Owner test invoked. Check logs for execution (Shift+K to dump summary).", myRpcLogTelemetryProfile);
        }

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "RpcTarget.Others" test.
        /// </summary>
        public void InvokeTest_TargetRpc_Others()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            string msg = $"{correlationId}-Others test from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";

            GONetLog.Info($"[TEST START] RpcTarget.Others test (ID: {correlationId})", myRpcLogTelemetryProfile);

            CallRpc(nameof(NotifyOthers_0Params));
            CallRpc(nameof(NotifyOthers_1Param), msg);

            GONetLog.Info($"[TEST END] RpcTarget.Others test invoked. Caller should NOT execute, all others should.", myRpcLogTelemetryProfile);
        }

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "RpcTarget.SpecificAuthority" test.
        /// </summary>
        public void InvokeTest_TargetRpc_SpecificAuthority()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            // Send to Client:1 (authority ID 1)
            ushort targetClient = 1;
            string msg = $"{correlationId}-SpecificAuthority to Client:{targetClient}";

            GONetLog.Info($"[TEST START] RpcTarget.SpecificAuthority test (ID: {correlationId}), targeting Client:{targetClient}", myRpcLogTelemetryProfile);

            CallRpc(nameof(SendToPlayer_1Param), targetClient, msg);
            CallRpc(nameof(SendToPlayer_2Params), targetClient, msg, 42);

            GONetLog.Info($"[TEST END] RpcTarget.SpecificAuthority test invoked. Only Client:{targetClient} should execute.", myRpcLogTelemetryProfile);
        }

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "RpcTarget.MultipleAuthorities" test.
        /// </summary>
        public void InvokeTest_TargetRpc_MultipleAuthorities()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            // Send to Server + Client:1
            List<ushort> teamIds = new List<ushort> { GONetMain.OwnerAuthorityId_Server, 1 };
            string msg = $"{correlationId}-MultipleAuthorities to [Server, Client:1]";

            GONetLog.Info($"[TEST START] RpcTarget.MultipleAuthorities test (ID: {correlationId}), targeting Server + Client:1", myRpcLogTelemetryProfile);

            CallRpc(nameof(SendToTeam_1Param), teamIds, msg);
            CallRpc(nameof(SendToTeam_2Params), teamIds, msg, 99);

            GONetLog.Info($"[TEST END] RpcTarget.MultipleAuthorities test invoked. Only Server and Client:1 should execute.", myRpcLogTelemetryProfile);
        }

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "Property-based single target" test.
        /// </summary>
        public void InvokeTest_TargetRpc_PropertySingle()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            // Set target via property
            TargetPlayerId = 1; // Client:1

            string msg = $"{correlationId}-PropertySingle to Client:1 (via TargetPlayerId property)";

            GONetLog.Info($"[TEST START] Property-based single target test (ID: {correlationId}), TargetPlayerId={TargetPlayerId}", myRpcLogTelemetryProfile);

            CallRpc(nameof(SendToPropertySingle), msg);

            GONetLog.Info($"[TEST END] Property-based single target test invoked. Only Client:1 should execute.", myRpcLogTelemetryProfile);
        }

        /// <summary>
        /// Invoked by RpcTestRunnerUI when user selects "Property-based multiple targets" test.
        /// </summary>
        public void InvokeTest_TargetRpc_PropertyMultiple()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            // Set targets via property
            TeamMembers = new List<ushort> { GONetMain.OwnerAuthorityId_Server, 1 }; // Server + Client:1

            string msg = $"{correlationId}-PropertyMultiple to [Server, Client:1] (via TeamMembers property)";

            GONetLog.Info($"[TEST START] Property-based multiple targets test (ID: {correlationId}), TeamMembers=[Server, Client:1]", myRpcLogTelemetryProfile);

            CallRpc(nameof(SendToPropertyMultiple), msg);

            GONetLog.Info($"[TEST END] Property-based multiple targets test invoked. Server and Client:1 should execute.", myRpcLogTelemetryProfile);
        }

        #endregion
    }
}
