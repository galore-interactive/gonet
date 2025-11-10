using GONet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Tests for RPC complex parameter types.
    /// Covers: Vector3, Quaternion, Color, structs, enums, arrays, lists, dictionaries
    ///
    /// Invoked via Shift+X keyboard shortcut.
    ///
    /// Testing Strategy:
    /// - Send complex Unity types (Vector3, Quaternion, Color)
    /// - Send custom structs and enums
    /// - Send collections (arrays, lists)
    /// - Verify serialization/deserialization correctness
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcComplexTypesTests : GONetParticipantCompanionBehaviour
    {
        #region Custom Types for Testing

        public enum TestEnum
        {
            None = 0,
            Alpha = 1,
            Beta = 2,
            Gamma = 3
        }

        [Serializable]
        public struct TestStruct
        {
            public int id;
            public float value;
            public string name;

            public override string ToString()
            {
                return $"TestStruct(id={id}, value={value}, name={name})";
            }

            public override bool Equals(object obj)
            {
                if (!(obj is TestStruct)) return false;
                TestStruct other = (TestStruct)obj;
                return id == other.id && Mathf.Approximately(value, other.value) && name == other.name;
            }

            public override int GetHashCode()
            {
                return id.GetHashCode() ^ value.GetHashCode() ^ (name != null ? name.GetHashCode() : 0);
            }
        }

        #endregion

        #region RPC Execution Tracker

        private static readonly ConcurrentBag<string> rpcExecutionLog = new ConcurrentBag<string>();
        private static int currentTestId = -1;

        private static void LogRpcExecution(string rpcVariant, string details = null)
        {
            if (currentTestId == -1) return;

            string machine = GONetMain.IsServer ? "Server" : (GONetMain.MyAuthorityId == 1 ? "Client:1" : $"Client:{GONetMain.MyAuthorityId}");
            string entry = details != null ? $"{currentTestId}-{rpcVariant}|{machine}|{details}" : $"{currentTestId}-{rpcVariant}|{machine}";
            rpcExecutionLog.Add(entry);
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
            sb.AppendLine("========== RPC COMPLEX TYPES TEST EXECUTION SUMMARY ==========");

            foreach (var group in grouped)
            {
                string variant = group.Key;
                var machines = group.Select(entry => entry.Split('|')[1]).Distinct().OrderBy(m => m).ToList();
                int count = group.Count();
                sb.AppendLine($"{variant}: {count} executions on [{string.Join(", ", machines)}]");

                // Show parameter details for first execution
                var firstExecution = group.First();
                var parts = firstExecution.Split('|');
                if (parts.Length > 2)
                {
                    sb.AppendLine($"    Sample: {parts[2]}");
                }
            }

            sb.AppendLine($"Total executions: {rpcExecutionLog.Count}");
            sb.AppendLine("=============================================================");

            GONetLog.Info(sb.ToString(), myRpcLogTelemetryProfile);
        }

        string myRpcLogTelemetryProfile;
        private void InitTelemetryLogging()
        {
            myRpcLogTelemetryProfile = $"RpcComplexTypes-{(GONetMain.IsServer ? "Server" : $"Client{GONetMain.MyAuthorityId}")}";
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

            // Shift+X: Run ALL complex types tests
            if (shiftPressed && Input.GetKeyDown(KeyCode.X))
            {
                GONetLog.Info("[GONetRpcComplexTypesTests] Running ALL complex types tests (Shift+X)...", myRpcLogTelemetryProfile);
                InvokeTest_Vector3Parameter();
                InvokeTest_QuaternionParameter();
                InvokeTest_ColorParameter();
                InvokeTest_StructParameter();
                InvokeTest_EnumParameter();
                InvokeTest_IntArrayParameter();
                InvokeTest_Vector3ArrayParameter();
                InvokeTest_MultipleComplexParameters();
                InvokeTest_NullableParameter();
                InvokeTest_StringArrayParameter();
                InvokeTest_MixedTypesParameter();
                InvokeTest_LargeStructParameter();
                GONetLog.Info("[GONetRpcComplexTypesTests] Completed ALL complex types tests. Press Shift+K to dump summary.", myRpcLogTelemetryProfile);
            }
        }

        #endregion

        #region Test 1: Vector3 Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Vector3(Vector3 position)
        {
            LogRpcExecution("TargetRpc_Vector3", $"pos={position}");
            GONetLog.Debug($"[RpcComplexTypes] Vector3 RPC executed: {position}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_Vector3Parameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            Vector3 testPosition = new Vector3(123.456f, 789.012f, 345.678f);
            CallRpc(nameof(TargetRpc_Vector3), testPosition);

            GONetLog.Info($"[RpcComplexTypes] Vector3 test invoked: {testPosition}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 2: Quaternion Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Quaternion(Quaternion rotation)
        {
            LogRpcExecution("TargetRpc_Quaternion", $"rot={rotation.eulerAngles}");
            GONetLog.Debug($"[RpcComplexTypes] Quaternion RPC executed: {rotation.eulerAngles}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_QuaternionParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            Quaternion testRotation = Quaternion.Euler(45f, 90f, 180f);
            CallRpc(nameof(TargetRpc_Quaternion), testRotation);

            GONetLog.Info($"[RpcComplexTypes] Quaternion test invoked: {testRotation.eulerAngles}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 3: Color Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Color(Color color)
        {
            LogRpcExecution("TargetRpc_Color", $"color=({color.r},{color.g},{color.b},{color.a})");
            GONetLog.Debug($"[RpcComplexTypes] Color RPC executed: {color}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_ColorParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            Color testColor = new Color(0.5f, 0.75f, 1.0f, 0.8f);
            CallRpc(nameof(TargetRpc_Color), testColor);

            GONetLog.Info($"[RpcComplexTypes] Color test invoked: {testColor}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 4: Struct Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Struct(TestStruct data)
        {
            LogRpcExecution("TargetRpc_Struct", $"struct={data}");
            GONetLog.Debug($"[RpcComplexTypes] Struct RPC executed: {data}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_StructParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            TestStruct testStruct = new TestStruct { id = 42, value = 3.14159f, name = "TestData" };
            CallRpc(nameof(TargetRpc_Struct), testStruct);

            GONetLog.Info($"[RpcComplexTypes] Struct test invoked: {testStruct}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 5: Enum Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Enum(TestEnum enumValue)
        {
            LogRpcExecution("TargetRpc_Enum", $"enum={enumValue}");
            GONetLog.Debug($"[RpcComplexTypes] Enum RPC executed: {enumValue}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_EnumParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            TestEnum testEnum = TestEnum.Beta;
            CallRpc(nameof(TargetRpc_Enum), testEnum);

            GONetLog.Info($"[RpcComplexTypes] Enum test invoked: {testEnum}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 6: Int Array Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_IntArray(int[] numbers)
        {
            LogRpcExecution("TargetRpc_IntArray", $"array=[{string.Join(",", numbers)}]");
            GONetLog.Debug($"[RpcComplexTypes] IntArray RPC executed: [{string.Join(", ", numbers)}]", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_IntArrayParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            int[] testArray = new int[] { 10, 20, 30, 40, 50 };
            CallRpc(nameof(TargetRpc_IntArray), testArray);

            GONetLog.Info($"[RpcComplexTypes] IntArray test invoked: [{string.Join(", ", testArray)}]", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 7: Vector3 Array Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Vector3Array(Vector3[] positions)
        {
            LogRpcExecution("TargetRpc_Vector3Array", $"array.Length={positions.Length}");
            GONetLog.Debug($"[RpcComplexTypes] Vector3Array RPC executed: {positions.Length} positions", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_Vector3ArrayParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            Vector3[] testPositions = new Vector3[]
            {
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6),
                new Vector3(7, 8, 9)
            };
            CallRpc(nameof(TargetRpc_Vector3Array), testPositions);

            GONetLog.Info($"[RpcComplexTypes] Vector3Array test invoked: {testPositions.Length} positions", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 8: Multiple Complex Parameters

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_MultipleComplex(Vector3 position, Quaternion rotation, Color color, int id)
        {
            LogRpcExecution("TargetRpc_MultipleComplex", $"pos={position}, rot={rotation.eulerAngles}, color={color}, id={id}");
            GONetLog.Debug($"[RpcComplexTypes] MultipleComplex RPC executed: pos={position}, rot={rotation.eulerAngles}, color={color}, id={id}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_MultipleComplexParameters()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            Vector3 pos = new Vector3(10, 20, 30);
            Quaternion rot = Quaternion.Euler(45, 90, 135);
            Color col = Color.cyan;
            int id = 123;
            CallRpc(nameof(TargetRpc_MultipleComplex), pos, rot, col, id);

            GONetLog.Info($"[RpcComplexTypes] MultipleComplex test invoked: pos={pos}, rot={rot.eulerAngles}, color={col}, id={id}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 9: Nullable Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_Nullable(int? nullableValue)
        {
            LogRpcExecution("TargetRpc_Nullable", $"value={nullableValue}");
            GONetLog.Debug($"[RpcComplexTypes] Nullable RPC executed: {nullableValue}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_NullableParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            int? testValue = 42;
            CallRpc(nameof(TargetRpc_Nullable), testValue);

            GONetLog.Info($"[RpcComplexTypes] Nullable test invoked: {testValue}", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 10: String Array Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_StringArray(string[] messages)
        {
            LogRpcExecution("TargetRpc_StringArray", $"array=[{string.Join(",", messages)}]");
            GONetLog.Debug($"[RpcComplexTypes] StringArray RPC executed: [{string.Join(", ", messages)}]", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_StringArrayParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            string[] testMessages = new string[] { "Hello", "World", "RPC", "Test" };
            CallRpc(nameof(TargetRpc_StringArray), testMessages);

            GONetLog.Info($"[RpcComplexTypes] StringArray test invoked: [{string.Join(", ", testMessages)}]", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 11: Mixed Types Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_MixedTypes(int intValue, float floatValue, bool boolValue, string stringValue, Vector3 vectorValue)
        {
            LogRpcExecution("TargetRpc_MixedTypes", $"int={intValue}, float={floatValue}, bool={boolValue}, string={stringValue}, vec={vectorValue}");
            GONetLog.Debug($"[RpcComplexTypes] MixedTypes RPC executed: int={intValue}, float={floatValue}, bool={boolValue}, string={stringValue}, vec={vectorValue}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_MixedTypesParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            CallRpc(nameof(TargetRpc_MixedTypes), 42, 3.14159f, true, "MixedTest", new Vector3(1, 2, 3));

            GONetLog.Info($"[RpcComplexTypes] MixedTypes test invoked", myRpcLogTelemetryProfile);
        }

        #endregion

        #region Test 12: Large Struct Parameter

        [TargetRpc(RpcTarget.All)]
        internal void TargetRpc_LargeStruct(TestStruct data1, TestStruct data2, TestStruct data3)
        {
            LogRpcExecution("TargetRpc_LargeStruct", $"3 structs: {data1}, {data2}, {data3}");
            GONetLog.Debug($"[RpcComplexTypes] LargeStruct RPC executed: {data1}, {data2}, {data3}", myRpcLogTelemetryProfile);
        }

        public void InvokeTest_LargeStructParameter()
        {
            int correlationId = UnityEngine.Random.Range(100, 999);
            currentTestId = correlationId;
            TestStruct s1 = new TestStruct { id = 1, value = 1.1f, name = "First" };
            TestStruct s2 = new TestStruct { id = 2, value = 2.2f, name = "Second" };
            TestStruct s3 = new TestStruct { id = 3, value = 3.3f, name = "Third" };
            CallRpc(nameof(TargetRpc_LargeStruct), s1, s2, s3);

            GONetLog.Info($"[RpcComplexTypes] LargeStruct test invoked: {s1}, {s2}, {s3}", myRpcLogTelemetryProfile);
        }

        #endregion
    }
}
