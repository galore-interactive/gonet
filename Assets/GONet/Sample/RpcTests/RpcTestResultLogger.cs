using GONet;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Structured test result logging for automated validation.
    ///
    /// Logs test results in JSON format with [RPC-TEST-RESULT] prefix
    /// for easy parsing by Python validation scripts.
    ///
    /// Usage:
    /// 1. Create RpcTestResult at test start
    /// 2. Track RPC executions during test
    /// 3. Set validation criteria (expected vs actual)
    /// 4. Finalize and log result (PASS/FAIL)
    /// </summary>
    public static class RpcTestResultLogger
    {
        private const string LOG_PREFIX = "[RPC-TEST-RESULT]";

        /// <summary>
        /// Logs a complete test result in structured JSON format.
        /// </summary>
        public static void LogTestResult(RpcTestResult result)
        {
            string json = SerializeToJson(result);

            // Log with appropriate level based on result
            if (result.Result == TestResultStatus.PASS)
            {
                GONetLog.Info($"{LOG_PREFIX} {json}");
            }
            else if (result.Result == TestResultStatus.FAIL)
            {
                GONetLog.Error($"{LOG_PREFIX} {json}");
            }
            else
            {
                GONetLog.Warning($"{LOG_PREFIX} {json}");
            }
        }

        /// <summary>
        /// Creates a simple JSON representation (no external dependencies).
        /// </summary>
        private static string SerializeToJson(RpcTestResult result)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");

            // Test identification
            AppendJsonString(sb, "TestClass", result.TestClass);
            sb.Append(", ");
            AppendJsonString(sb, "TestName", result.TestName);
            sb.Append(", ");
            AppendJsonNumber(sb, "CorrelationId", result.CorrelationId);
            sb.Append(", ");
            AppendJsonString(sb, "ExecutingMachine", result.ExecutingMachine);
            sb.Append(", ");
            AppendJsonString(sb, "Timestamp", result.Timestamp);
            sb.Append(", ");

            // RPC executions array
            sb.Append("\"RpcExecutions\": [");
            for (int i = 0; i < result.RpcExecutions.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                SerializeRpcExecution(sb, result.RpcExecutions[i]);
            }
            sb.Append("], ");

            // Expected behavior
            AppendJsonString(sb, "ExpectedBehavior", result.ExpectedBehavior);
            sb.Append(", ");

            // Validation criteria
            sb.Append("\"ValidationCriteria\": {");
            bool first = true;
            foreach (var kvp in result.ValidationCriteria)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append($"\"{kvp.Key}\": {{\"Expected\": {kvp.Value.Expected}, \"Actual\": {kvp.Value.Actual}}}");
            }
            sb.Append("}, ");

            // Result status
            AppendJsonString(sb, "Result", result.Result.ToString());
            sb.Append(", ");
            AppendJsonString(sb, "ErrorMessage", result.ErrorMessage ?? "null");

            sb.Append(" }");
            return sb.ToString();
        }

        private static void SerializeRpcExecution(StringBuilder sb, RpcExecutionRecord record)
        {
            sb.Append("{");
            AppendJsonString(sb, "Method", record.Method);
            sb.Append(", ");
            AppendJsonString(sb, "ExecutedOn", record.ExecutedOn);
            sb.Append(", ");
            AppendJsonBool(sb, "Expected", record.Expected);
            sb.Append(", ");
            AppendJsonBool(sb, "Actual", record.Actual);
            sb.Append("}");
        }

        private static void AppendJsonString(StringBuilder sb, string key, string value)
        {
            sb.Append($"\"{key}\": \"{EscapeJson(value)}\"");
        }

        private static void AppendJsonNumber(StringBuilder sb, string key, int value)
        {
            sb.Append($"\"{key}\": {value}");
        }

        private static void AppendJsonBool(StringBuilder sb, string key, bool value)
        {
            sb.Append($"\"{key}\": {value.ToString().ToLower()}");
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }

    /// <summary>
    /// Test result data structure.
    /// </summary>
    public class RpcTestResult
    {
        public string TestClass { get; set; }
        public string TestName { get; set; }
        public int CorrelationId { get; set; }
        public string ExecutingMachine { get; set; }
        public string Timestamp { get; set; }
        public List<RpcExecutionRecord> RpcExecutions { get; set; }
        public string ExpectedBehavior { get; set; }
        public Dictionary<string, ValidationCriterion> ValidationCriteria { get; set; }
        public TestResultStatus Result { get; set; }
        public string ErrorMessage { get; set; }

        public RpcTestResult()
        {
            RpcExecutions = new List<RpcExecutionRecord>();
            ValidationCriteria = new Dictionary<string, ValidationCriterion>();
            Timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            ExecutingMachine = GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}";
        }
    }

    /// <summary>
    /// Single RPC execution record.
    /// </summary>
    public class RpcExecutionRecord
    {
        public string Method { get; set; }
        public string ExecutedOn { get; set; }
        public bool Expected { get; set; } // Was execution expected on this machine?
        public bool Actual { get; set; }   // Did execution actually occur?
    }

    /// <summary>
    /// Validation criterion (expected vs actual count/value).
    /// </summary>
    public class ValidationCriterion
    {
        public int Expected { get; set; }
        public int Actual { get; set; }

        public bool IsValid => Expected == Actual;
    }

    /// <summary>
    /// Test result status.
    /// </summary>
    public enum TestResultStatus
    {
        PASS,
        FAIL,
        SKIPPED,
        ERROR
    }
}
