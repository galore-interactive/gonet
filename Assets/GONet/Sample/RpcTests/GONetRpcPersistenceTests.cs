using GONet;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Tests for RPC persistence system.
    /// Covers: IsPersistent flag, late-joiner delivery, persistent TargetRpc/ClientRpc/ServerRpc
    ///
    /// Invoked via Shift+P keyboard shortcut.
    ///
    /// Testing Strategy:
    /// - Mark RPCs as persistent (IsPersistent = true)
    /// - Invoke RPCs before late-joiner connects
    /// - Verify late-joiner receives queued persistent RPCs on connect
    /// - Manual testing required: Run server, invoke persistent RPCs, THEN connect client
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcPersistenceTests : GONetParticipantCompanionBehaviour
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
                // No executions recorded - don't create log file
                return;
            }

            EnsureLoggingProfileRegistered();

            var grouped = rpcExecutionLog.GroupBy(entry => entry.Split('|')[0])
                                          .OrderBy(g => g.Key);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("========== RPC PERSISTENCE TEST EXECUTION SUMMARY ==========");

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
        private bool isLoggingProfileRegistered = false;

        /// <summary>
        /// Lazy initialization: only register logging profile when we actually need to log something.
        /// This prevents empty log files from being created when tests are not run.
        /// </summary>
        private void EnsureLoggingProfileRegistered()
        {
            if (!isLoggingProfileRegistered)
            {
                myRpcLogTelemetryProfile = $"RpcPersistence-{(GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}")}";
                GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(myRpcLogTelemetryProfile, outputToSeparateFile: true));
                isLoggingProfileRegistered = true;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Logging profile registration deferred until first actual log write
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

            // Shift+P: Run ALL persistence tests
            if (shiftPressed && Input.GetKeyDown(KeyCode.P))
            {
                EnsureLoggingProfileRegistered();
                GONetLog.Info("[GONetRpcPersistenceTests] Running ALL persistence tests (Shift+P)...", myRpcLogTelemetryProfile);
                InvokeTest_TargetRpc_Persistent();
                InvokeTest_ClientRpc_Persistent();
                InvokeTest_ServerRpc_Persistent();
                InvokeTest_TargetRpc_NonPersistent();
                InvokeTest_ClientRpc_NonPersistent();
                InvokeTest_ServerRpc_NonPersistent();
                GONetLog.Info("[GONetRpcPersistenceTests] Completed ALL persistence tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
                GONetLog.Info("[GONetRpcPersistenceTests] For late-joiner testing: Run server, press Shift+P, THEN connect client and check if persistent RPCs execute.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test 1: TargetRpc Persistent (Late-Joiner Should Receive)

        [TargetRpc(RpcTarget.All, IsPersistent = true)]
        internal void TargetRpc_Persistent(string message)
        {
            LogRpcExecution("TargetRpc_Persistent", message);
            GONetLog.Debug($"[RpcPersistence] Persistent TargetRpc executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_TargetRpc_Persistent()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-PersistentTargetRpc from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(TargetRpc_Persistent), msg);

            GONetLog.Info($"[RpcPersistence] Persistent TargetRpc invoked - late-joiners SHOULD receive this", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 2: ClientRpc Persistent (Late-Joiner Should Receive)

        [ClientRpc(IsPersistent = true)]
        internal void ClientRpc_Persistent(string message)
        {
            LogRpcExecution("ClientRpc_Persistent", message);
            GONetLog.Debug($"[RpcPersistence] Persistent ClientRpc executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ClientRpc_Persistent()
        {
            if (!IsServer)
            {
                GONetLog.Info("[RpcPersistence] ClientRpc_Persistent test skipped - only executable from server", myRpcLogTelemetryProfile);
                return;
            }

            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-PersistentClientRpc from Server";
            CallRpc(nameof(ClientRpc_Persistent), msg);

            GONetLog.Info($"[RpcPersistence] Persistent ClientRpc invoked - late-joiners SHOULD receive this", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 3: ServerRpc Persistent (NOT Applicable - ServerRpc Doesn't Support Persistence)

        // NOTE: ServerRpc does NOT support IsPersistent flag (makes no sense - client→server is one-time request)
        // This test verifies that IsPersistent is ignored for ServerRpc (no compile error, just no effect)

        [ServerRpc(IsPersistent = true)] // Should compile but have no effect
        internal void ServerRpc_Persistent(string message)
        {
            LogRpcExecution("ServerRpc_Persistent", message);
            GONetLog.Debug($"[RpcPersistence] ServerRpc with IsPersistent flag executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ServerRpc_Persistent()
        {
            if (!IsClient)
            {
                GONetLog.Info("[RpcPersistence] ServerRpc_Persistent test skipped - only executable from clients", myRpcLogTelemetryProfile);
                return;
            }

            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ServerRpc (IsPersistent ignored) from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(ServerRpc_Persistent), msg);

            GONetLog.Info($"[RpcPersistence] ServerRpc with IsPersistent flag invoked - flag should be IGNORED (ServerRpc doesn't support persistence)", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 4: TargetRpc Non-Persistent (Late-Joiner Should NOT Receive)

        [TargetRpc(RpcTarget.All, IsPersistent = false)] // Explicit non-persistent
        internal void TargetRpc_NonPersistent(string message)
        {
            LogRpcExecution("TargetRpc_NonPersistent", message);
            GONetLog.Debug($"[RpcPersistence] Non-Persistent TargetRpc executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_TargetRpc_NonPersistent()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-NonPersistentTargetRpc from {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            CallRpc(nameof(TargetRpc_NonPersistent), msg);

            GONetLog.Info($"[RpcPersistence] Non-Persistent TargetRpc invoked - late-joiners should NOT receive this", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 5: ClientRpc Non-Persistent (Late-Joiner Should NOT Receive)

        [ClientRpc(IsPersistent = false)] // Explicit non-persistent
        internal void ClientRpc_NonPersistent(string message)
        {
            LogRpcExecution("ClientRpc_NonPersistent", message);
            GONetLog.Debug($"[RpcPersistence] Non-Persistent ClientRpc executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ClientRpc_NonPersistent()
        {
            if (!IsServer)
            {
                GONetLog.Info("[RpcPersistence] ClientRpc_NonPersistent test skipped - only executable from server", myRpcLogTelemetryProfile);
                return;
            }

            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-NonPersistentClientRpc from Server";
            CallRpc(nameof(ClientRpc_NonPersistent), msg);

            GONetLog.Info($"[RpcPersistence] Non-Persistent ClientRpc invoked - late-joiners should NOT receive this", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 6: ServerRpc Non-Persistent (Default Behavior)

        [ServerRpc] // Default: IsPersistent = false
        internal void ServerRpc_NonPersistent(string message)
        {
            LogRpcExecution("ServerRpc_NonPersistent", message);
            GONetLog.Debug($"[RpcPersistence] Non-Persistent ServerRpc executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ServerRpc_NonPersistent()
        {
            if (!IsClient)
            {
                GONetLog.Info("[RpcPersistence] ServerRpc_NonPersistent test skipped - only executable from clients", myRpcLogTelemetryProfile);
                return;
            }

            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-NonPersistentServerRpc from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(ServerRpc_NonPersistent), msg);

            GONetLog.Info($"[RpcPersistence] Non-Persistent ServerRpc invoked (default behavior)", myRpcLogTelemetryProfile);
        }

        #endregion
    }
}
