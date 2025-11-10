using GONet;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Comprehensive integration test for all RPC types (ServerRpc, ClientRpc, TargetRpc).
    /// Tests real-world patterns: request-response, async/await, exception handling, nested calls.
    ///
    /// Invoked via Shift+A keyboard shortcut.
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcAllTypesIntegrationTest : GONetParticipantCompanionBehaviour
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
            sb.AppendLine("========== ALL RPC TYPES INTEGRATION TEST SUMMARY ==========");

            foreach (var group in grouped)
            {
                string variant = group.Key;
                var machines = group.Select(entry => entry.Split('|')[1]).Distinct().OrderBy(m => m).ToList();
                int count = group.Count();
                sb.AppendLine($"{variant}: {count} executions on [{string.Join(", ", machines)}]");
            }

            sb.AppendLine($"Total executions: {rpcExecutionLog.Count}");
            sb.AppendLine("=============================================================");

            GONetLog.Info(sb.ToString(), myRpcLogTelemetryProfile);
        }

        string myRpcLogTelemetryProfile;
        private void InitTelemetryLogging()
        {
            myRpcLogTelemetryProfile = $"RpcAllTypes-{(GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}")}";
            GONetLog.RegisterLoggingProfile(new GONetLog.LoggingProfile(myRpcLogTelemetryProfile, outputToSeparateFile: true));
        }

        #endregion

        #region Unity Lifecycle

        protected override void Start()
        {
            base.Start();
         
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

            // Shift+A: Run ALL RPC integration tests
            if (shiftPressed && Input.GetKeyDown(KeyCode.A))
            {
                GONetLog.Info("[GONetRpcAllTypesIntegrationTest] Running ALL RPC integration tests (Shift+A)...", myRpcLogTelemetryProfile);
                RunAllTests();
                GONetLog.Info("[GONetRpcAllTypesIntegrationTest] Completed ALL tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test Orchestration

        private void RunAllTests()
        {
            if (GONetMain.IsClient && !GONetMain.IsServer)
            {
                // Client initiates tests
                InvokeTest_ServerRpc_Basic();
                InvokeTest_ServerRpc_Async();
                InvokeTest_ServerRpc_CallsClientRpc();
                InvokeTest_ClientRpc_BroadcastToAll();
                InvokeTest_TargetRpc_ToOwner();
                InvokeTest_TargetRpc_ToSpecificClient();
                InvokeTest_NestedRpcCalls();
                InvokeTest_MixedReliableUnreliable();
                InvokeTest_ExceptionHandling();
            }
            else if (GONetMain.IsServer)
            {
                GONetLog.Info("[RpcAllTypes] Server ready for tests. Press Shift+A from CLIENT to initiate.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test 1: Basic ServerRpc (Client → Server)

        [ServerRpc]
        internal void ServerRpc_Basic(string message)
        {
            LogRpcExecution("ServerRpc_Basic", message);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_Basic executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ServerRpc_Basic()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ServerRpc_Basic from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(ServerRpc_Basic), msg);
        }

        #endregion

        #region Test 2: ServerRpc with Async Return Value

        [ServerRpc]
        internal async Task<int> ServerRpc_AsyncResponse(string request, int value)
        {
            LogRpcExecution("ServerRpc_AsyncResponse", request);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_AsyncResponse executing: {request}, value={value}", myRpcLogTelemetryProfile);

            // Simulate async work
            await Task.Delay(10);

            int result = value * 2;
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_AsyncResponse returning: {result}", myRpcLogTelemetryProfile);
            return result;
        }

        public async void InvokeTest_ServerRpc_Async()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ServerRpc_Async from Client:{GONetMain.MyAuthorityId}";

            int result = await CallRpcAsync<int, string, int>(nameof(ServerRpc_AsyncResponse), msg, 42);
            GONetLog.Info($"[RpcAllTypes] ServerRpc_Async returned: {result} (expected 84)", myRpcLogTelemetryProfile);

            if (result == 84)
            {
                GONetLog.Info("✅ PASS: ServerRpc async return value correct", myRpcLogTelemetryProfile);
            }
            else
            {
                GONetLog.Error($"❌ FAIL: ServerRpc async return value incorrect. Expected 84, got {result}", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test 3: ServerRpc Calls ClientRpc (Request-Response Broadcast Pattern)

        [ServerRpc]
        internal void ServerRpc_ThenBroadcast(string request)
        {
            LogRpcExecution("ServerRpc_ThenBroadcast", request);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_ThenBroadcast received: {request}", myRpcLogTelemetryProfile);

            // Server processes request, then broadcasts result to all clients
            string response = $"Server processed: {request}";
            CallRpc(nameof(ClientRpc_BroadcastResponse), response);
        }

        [ClientRpc]
        internal void ClientRpc_BroadcastResponse(string response)
        {
            LogRpcExecution("ClientRpc_BroadcastResponse", response);
            GONetLog.Debug($"[RpcAllTypes] ClientRpc_BroadcastResponse executed: {response}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ServerRpc_CallsClientRpc()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ServerRpc_CallsClientRpc from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(ServerRpc_ThenBroadcast), msg);
        }

        #endregion

        #region Test 4: ClientRpc Direct Broadcast (Server → All Clients)

        [ClientRpc]
        internal void ClientRpc_DirectBroadcast(string message, int data)
        {
            LogRpcExecution("ClientRpc_DirectBroadcast", message);
            GONetLog.Debug($"[RpcAllTypes] ClientRpc_DirectBroadcast executed: {message}, data={data}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ClientRpc_BroadcastToAll()
        {
            if (GONetMain.IsServer)
            {
                int correlationId = UnityEngine.Random.Range(100, 999);
                currentTestId = correlationId;
                string msg = $"{correlationId}-ClientRpc_DirectBroadcast from Server";
                CallRpc(nameof(ClientRpc_DirectBroadcast), msg, 999);
            }
            else
            {
                // Client triggers server to broadcast via ServerRpc
                CallRpc(nameof(ServerRpc_TriggerClientBroadcast));
            }
        }

        [ServerRpc(IsMineRequired = false)] // Allow any client to call
        internal void ServerRpc_TriggerClientBroadcast()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-ClientRpc_DirectBroadcast triggered by client";
            CallRpc(nameof(ClientRpc_DirectBroadcast), msg, 888);
        }

        #endregion

        #region Test 5: TargetRpc to Owner

        [TargetRpc(RpcTarget.Owner)]
        internal void TargetRpc_ToOwner(string message)
        {
            LogRpcExecution("TargetRpc_ToOwner", message);
            GONetLog.Debug($"[RpcAllTypes] TargetRpc_ToOwner executed: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_TargetRpc_ToOwner()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-TargetRpc_ToOwner from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(TargetRpc_ToOwner), msg);
        }

        #endregion

        #region Test 6: TargetRpc to Specific Client

        [TargetRpc(RpcTarget.SpecificAuthority)]
        internal void TargetRpc_ToSpecificClient(ushort targetClientId, string message)
        {
            LogRpcExecution("TargetRpc_ToSpecificClient", message);
            GONetLog.Debug($"[RpcAllTypes] TargetRpc_ToSpecificClient executed: {message} (target was {targetClientId})", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_TargetRpc_ToSpecificClient()
        {
            // Send message to Client:1 specifically
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-TargetRpc_ToSpecificClient from Client:{GONetMain.MyAuthorityId} targeting Client:1";
            CallRpc(nameof(TargetRpc_ToSpecificClient), (ushort)1, msg);
        }

        #endregion

        #region Test 7: Nested RPC Calls (ServerRpc → ClientRpc → TargetRpc chain)

        [ServerRpc]
        internal void ServerRpc_StartChain(string message)
        {
            LogRpcExecution("ServerRpc_StartChain", message);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_StartChain executing: {message}", myRpcLogTelemetryProfile);

            // Call ClientRpc from ServerRpc
            CallRpc(nameof(ClientRpc_MiddleChain), $"{message} -> ClientRpc");
        }

        [ClientRpc]
        internal void ClientRpc_MiddleChain(string message)
        {
            LogRpcExecution("ClientRpc_MiddleChain", message);
            GONetLog.Debug($"[RpcAllTypes] ClientRpc_MiddleChain executing: {message}", myRpcLogTelemetryProfile);

            // Call TargetRpc from ClientRpc (only on client, not server)
            if (GONetMain.IsClient && !GONetMain.IsServer)
            {
                CallRpc(nameof(TargetRpc_EndChain), $"{message} -> TargetRpc");
            }
        }

        [TargetRpc(RpcTarget.Owner)]
        internal void TargetRpc_EndChain(string message)
        {
            LogRpcExecution("TargetRpc_EndChain", message);
            GONetLog.Debug($"[RpcAllTypes] TargetRpc_EndChain executing: {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_NestedRpcCalls()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string msg = $"{correlationId}-NestedRpcCalls from Client:{GONetMain.MyAuthorityId}";
            CallRpc(nameof(ServerRpc_StartChain), msg);
        }

        #endregion

        #region Test 8: Mixed Reliable/Unreliable RPCs

        [ServerRpc(IsReliable = true)]
        internal void ServerRpc_Reliable(string message)
        {
            LogRpcExecution("ServerRpc_Reliable", message);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_Reliable executed (reliable): {message}", myRpcLogTelemetryProfile);
        }

        [ServerRpc(IsReliable = false)]
        internal void ServerRpc_Unreliable(string message)
        {
            LogRpcExecution("ServerRpc_Unreliable", message);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_Unreliable executed (unreliable): {message}", myRpcLogTelemetryProfile);
        }

        [ClientRpc(IsReliable = false)]
        internal void ClientRpc_Unreliable(string message)
        {
            LogRpcExecution("ClientRpc_Unreliable", message);
            GONetLog.Debug($"[RpcAllTypes] ClientRpc_Unreliable executed (unreliable): {message}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_MixedReliableUnreliable()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            // Test reliable ServerRpc
            CallRpc(nameof(ServerRpc_Reliable), $"{correlationId}-Reliable from Client:{GONetMain.MyAuthorityId}");

            // Test unreliable ServerRpc
            CallRpc(nameof(ServerRpc_Unreliable), $"{correlationId}-Unreliable from Client:{GONetMain.MyAuthorityId}");

            // Trigger unreliable ClientRpc via ServerRpc
            CallRpc(nameof(ServerRpc_TriggerUnreliableClient));
        }

        [ServerRpc(IsMineRequired = false)]
        internal void ServerRpc_TriggerUnreliableClient()
        {
            int correlationId = currentTestId;
            CallRpc(nameof(ClientRpc_Unreliable), $"{correlationId}-UnreliableClient from Server");
        }

        #endregion

        #region Test 9: Exception Handling in RPC Methods

        [ServerRpc]
        internal void ServerRpc_ThrowsException(string message)
        {
            LogRpcExecution("ServerRpc_ThrowsException_BEFORE", message);
            GONetLog.Debug($"[RpcAllTypes] ServerRpc_ThrowsException executing: {message}", myRpcLogTelemetryProfile);

            // Intentionally throw exception
            throw new System.InvalidOperationException($"Test exception in ServerRpc: {message}");
        }

        [ClientRpc]
        internal void ClientRpc_ThrowsException(string message)
        {
            LogRpcExecution("ClientRpc_ThrowsException_BEFORE", message);
            GONetLog.Debug($"[RpcAllTypes] ClientRpc_ThrowsException executing: {message}", myRpcLogTelemetryProfile);

            // Intentionally throw exception
            throw new System.InvalidOperationException($"Test exception in ClientRpc: {message}");
        }

        public void InvokeTest_ExceptionHandling()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;

            GONetLog.Info($"[RpcAllTypes] Testing exception handling - expect error logs below (this is intentional)", myRpcLogTelemetryProfile);

            // Test ServerRpc exception
            try
            {
                CallRpc(nameof(ServerRpc_ThrowsException), $"{correlationId}-ExceptionTest from Client:{GONetMain.MyAuthorityId}");
            }
            catch (System.Exception ex)
            {
                GONetLog.Info($"✅ PASS: Caught exception from ServerRpc: {ex.Message}", myRpcLogTelemetryProfile);
            }

            // Trigger ClientRpc exception via ServerRpc
            CallRpc(nameof(ServerRpc_TriggerExceptionClient));
        }

        [ServerRpc(IsMineRequired = false)]
        internal void ServerRpc_TriggerExceptionClient()
        {
            int correlationId = currentTestId;
            try
            {
                CallRpc(nameof(ClientRpc_ThrowsException), $"{correlationId}-ClientExceptionTest from Server");
            }
            catch (System.Exception ex)
            {
                GONetLog.Info($"✅ PASS: Caught exception from ClientRpc: {ex.Message}", myRpcLogTelemetryProfile);
            }
        }

        #endregion
    }
}
