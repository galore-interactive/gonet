using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GONet.Sample
{
    /// <summary>
    /// Executes test scripts defined in YAML-like simple text format.
    ///
    /// Test Script Format (.gotest files):
    ///
    /// # Comments start with #
    /// name: Spawn and Scene Change Test
    /// description: Tests spawn sync across scene changes
    /// require_clients: 2
    /// despawn_wait: 40
    ///
    /// # Test steps (one per line)
    /// wait_clients: 2
    /// spawn_server: 3
    /// wait: 2
    /// verify_beacons: all
    /// scene_change: ProjectileTest
    /// wait: 3
    /// spawn_server: 2
    /// spawn_client: 1, count=2
    /// wait: 2
    /// verify_beacons: all
    /// wait_despawn: 40
    /// verify_despawned: all
    /// human_action: Start Client3
    /// wait_client: 3
    /// wait: 3
    /// verify_beacons: all
    ///
    /// </summary>
    [System.Serializable]
    public class GONetTestScript
    {
        public string name = "Unnamed Test";
        public string description = "";
        public int requireClients = 2;
        public float despawnWaitTime = 40f;
        public string preCondition = "";  // Pre-test setup instructions for human operator
        public List<TestStep> steps = new List<TestStep>();

        [System.Serializable]
        public class TestStep
        {
            public TestStepType type;
            public Dictionary<string, string> parameters = new Dictionary<string, string>();

            public string GetParam(string key, string defaultValue = "")
            {
                return parameters.TryGetValue(key, out string value) ? value : defaultValue;
            }

            public int GetParamInt(string key, int defaultValue = 0)
            {
                if (parameters.TryGetValue(key, out string value) && int.TryParse(value, out int result))
                    return result;
                return defaultValue;
            }

            public float GetParamFloat(string key, float defaultValue = 0f)
            {
                if (parameters.TryGetValue(key, out string value) && float.TryParse(value, out float result))
                    return result;
                return defaultValue;
            }
        }

        public enum TestStepType
        {
            WaitClients,        // wait_clients: 2
            SpawnServer,        // spawn_server: 3
            SpawnClient,        // spawn_client: 1, count=2
            SpawnAllClients,    // spawn_all_clients: 2
            Wait,               // wait: 2.5
            VerifyBeacons,      // verify_beacons: all  OR  verify_beacons: 1,2,3
            VerifyDespawned,    // verify_despawned: all
            VerifyCount,        // verify_count: 5
            SceneChange,        // scene_change: ProjectileTest
            WaitDespawn,        // wait_despawn: 40
            HumanAction,        // human_action: Start Client3
            WaitClient,         // wait_client: 3
            Log                 // log: Custom message here
        }

        /// <summary>
        /// Parse a test script from a .gotest file
        /// </summary>
        public static GONetTestScript ParseFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                GONetLog.Error($"[TestScript] File not found: {filePath}");
                return null;
            }

            string[] lines = File.ReadAllLines(filePath);
            return ParseFromLines(lines);
        }

        /// <summary>
        /// Parse a test script from string content
        /// </summary>
        public static GONetTestScript ParseFromString(string content)
        {
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return ParseFromLines(lines);
        }

        private static GONetTestScript ParseFromLines(string[] lines)
        {
            var script = new GONetTestScript();
            int lineNumber = 0;

            foreach (string rawLine in lines)
            {
                lineNumber++;
                string line = rawLine.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                // Parse metadata
                if (line.StartsWith("name:"))
                {
                    script.name = line.Substring(5).Trim();
                    continue;
                }
                if (line.StartsWith("description:"))
                {
                    script.description = line.Substring(12).Trim();
                    continue;
                }
                if (line.StartsWith("require_clients:"))
                {
                    if (int.TryParse(line.Substring(16).Trim(), out int clients))
                        script.requireClients = clients;
                    continue;
                }
                if (line.StartsWith("despawn_wait:"))
                {
                    if (float.TryParse(line.Substring(13).Trim(), out float wait))
                        script.despawnWaitTime = wait;
                    continue;
                }
                if (line.StartsWith("pre_condition:"))
                {
                    script.preCondition = line.Substring(14).Trim();
                    continue;
                }

                // Parse step
                TestStep step = ParseStep(line, lineNumber);
                if (step != null)
                    script.steps.Add(step);
            }

            GONetLog.Info($"[TestScript] Parsed '{script.name}' with {script.steps.Count} steps");
            return script;
        }

        private static TestStep ParseStep(string line, int lineNumber)
        {
            // Split by colon to get command and params
            int colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
            {
                GONetLog.Warning($"[TestScript] Line {lineNumber}: Invalid step format (missing colon): {line}");
                return null;
            }

            string command = line.Substring(0, colonIndex).Trim();
            string paramString = line.Substring(colonIndex + 1).Trim();

            var step = new TestStep();

            // Convert command to PascalCase for enum parsing (e.g., "wait_clients" -> "WaitClients")
            string commandPascalCase = ConvertToPascalCase(command);

            // Parse command type
            if (!Enum.TryParse(commandPascalCase, true, out TestStepType stepType))
            {
                GONetLog.Warning($"[TestScript] Line {lineNumber}: Unknown command '{command}' (tried '{commandPascalCase}')");
                return null;
            }

            step.type = stepType;

            // Parse parameters based on step type
            switch (stepType)
            {
                case TestStepType.WaitClients:
                    step.parameters["count"] = paramString;
                    break;

                case TestStepType.SpawnServer:
                    step.parameters["count"] = paramString;
                    break;

                case TestStepType.SpawnClient:
                    // Format: "1, count=2" or just "1"
                    var parts = paramString.Split(',');
                    step.parameters["client"] = parts[0].Trim();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var kvp = parts[i].Split('=');
                        if (kvp.Length == 2)
                            step.parameters[kvp[0].Trim()] = kvp[1].Trim();
                    }
                    break;

                case TestStepType.SpawnAllClients:
                    step.parameters["count"] = paramString;
                    break;

                case TestStepType.Wait:
                    step.parameters["seconds"] = paramString;
                    break;

                case TestStepType.VerifyBeacons:
                case TestStepType.VerifyDespawned:
                    step.parameters["beacons"] = paramString; // "all" or "1,2,3"
                    break;

                case TestStepType.VerifyCount:
                    step.parameters["expected"] = paramString;
                    break;

                case TestStepType.SceneChange:
                    step.parameters["scene"] = paramString;
                    break;

                case TestStepType.WaitDespawn:
                    step.parameters["seconds"] = paramString;
                    break;

                case TestStepType.HumanAction:
                    step.parameters["instruction"] = paramString;
                    break;

                case TestStepType.WaitClient:
                    step.parameters["client"] = paramString;
                    break;

                case TestStepType.Log:
                    step.parameters["message"] = paramString;
                    break;
            }

            return step;
        }

        /// <summary>
        /// Converts snake_case or kebab-case to PascalCase.
        /// Examples: "wait_clients" -> "WaitClients", "scene-change" -> "SceneChange"
        /// </summary>
        private static string ConvertToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var parts = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpper(part[0]));
                    if (part.Length > 1)
                        result.Append(part.Substring(1).ToLower());
                }
            }

            return result.ToString();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Test: {name}");
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine($"  Description: {description}");
            sb.AppendLine($"  Require Clients: {requireClients}");
            sb.AppendLine($"  Steps: {steps.Count}");
            foreach (var step in steps)
            {
                sb.Append($"    - {step.type}");
                if (step.parameters.Count > 0)
                {
                    sb.Append($" ({string.Join(", ", step.parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))})");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
