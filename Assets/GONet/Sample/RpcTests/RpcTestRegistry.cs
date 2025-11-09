using System;
using System.Collections.Generic;
using GONet;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Central registry for RPC tests.
    /// Provides test discovery, machine-aware filtering, and invocation.
    /// </summary>
    public static class RpcTestRegistry
    {
        /// <summary>
        /// Test category groupings for organizational purposes.
        /// </summary>
        public enum TestCategory
        {
            TargetRpc_Targeting,     // RpcTarget.Owner, Others, SpecificAuthority, etc.
            TargetRpc_Validation,    // Validators, parameter modification, async validation
            ServerRpc_Relay,         // RelayMode.None, All, Others, Owner
            Persistence,             // IsPersistent, late-joiner delivery
            ComplexTypes,            // Unity types (Vector3, Quaternion), structs, collections
            Lifecycle,               // Deferred execution, handler registration timing
            ErrorHandling,           // Null handling, exceptions, timeouts
            Stress                   // Concurrent RPCs, max targets, large payloads
        }

        /// <summary>
        /// Defines which machines can run a particular test.
        /// Used for UI filtering - only show applicable tests.
        /// </summary>
        public enum MachineRequirement
        {
            All,                // Any machine can run this test
            ServerOnly,         // Only server can initiate (e.g., ClientRpc tests)
            ClientOnly,         // Only clients can initiate (e.g., ServerRpc tests)
            Client1Only,        // Specific client (for deterministic multi-client tests)
            MultipleClients     // Requires 2+ clients connected
        }

        /// <summary>
        /// Metadata and invocation delegate for a single test.
        /// </summary>
        public class TestDescriptor
        {
            /// <summary>
            /// Display name shown in UI dropdown (e.g., "RpcTarget.Owner (0+1 params)").
            /// </summary>
            public string Name;

            /// <summary>
            /// Human-readable description of what this test validates.
            /// </summary>
            public string Description;

            /// <summary>
            /// Expected behavior when test executes (shown in UI).
            /// </summary>
            public string ExpectedResult;

            /// <summary>
            /// Which machines can run this test.
            /// UI will only show tests matching current machine type.
            /// </summary>
            public MachineRequirement ApplicableMachines;

            /// <summary>
            /// Delegate to invoke the test method.
            /// Points to a public method on the test class (e.g., InvokeTest_TargetRpc_Owner).
            /// </summary>
            public Action InvokeTest;
        }

        // Storage: TestCategory → List of TestDescriptors
        private static readonly Dictionary<TestCategory, List<TestDescriptor>> registry = new Dictionary<TestCategory, List<TestDescriptor>>();

        /// <summary>
        /// Registers a test with the global registry.
        /// Called by test classes during Start() or Awake().
        /// </summary>
        /// <param name="category">Which category this test belongs to</param>
        /// <param name="test">Test metadata and invocation delegate</param>
        public static void RegisterTest(TestCategory category, TestDescriptor test)
        {
            if (!registry.ContainsKey(category))
            {
                registry[category] = new List<TestDescriptor>();
            }

            registry[category].Add(test);
        }

        /// <summary>
        /// Gets ALL tests for a category, regardless of machine applicability.
        /// </summary>
        /// <param name="category">Test category</param>
        /// <returns>List of all tests in category (empty if none registered)</returns>
        public static List<TestDescriptor> GetTestsForCategory(TestCategory category)
        {
            return registry.ContainsKey(category) ? registry[category] : new List<TestDescriptor>();
        }

        /// <summary>
        /// Gets tests for a category that are applicable to the CURRENT machine.
        /// Filters based on IsServer, MyAuthorityId, and connected client count.
        /// </summary>
        /// <param name="category">Test category</param>
        /// <returns>List of tests this machine can run</returns>
        public static List<TestDescriptor> GetApplicableTests(TestCategory category)
        {
            var allTests = GetTestsForCategory(category);
            var applicable = new List<TestDescriptor>();

            foreach (var test in allTests)
            {
                if (IsTestApplicable(test.ApplicableMachines))
                {
                    applicable.Add(test);
                }
            }

            return applicable;
        }

        /// <summary>
        /// Checks if a test with the given machine requirement is applicable to the current machine.
        /// </summary>
        /// <param name="requirement">Machine requirement to check</param>
        /// <returns>True if current machine matches requirement</returns>
        private static bool IsTestApplicable(MachineRequirement requirement)
        {
            switch (requirement)
            {
                case MachineRequirement.All:
                    return true;

                case MachineRequirement.ServerOnly:
                    return GONetMain.IsServer;

                case MachineRequirement.ClientOnly:
                    return !GONetMain.IsServer;

                case MachineRequirement.Client1Only:
                    return !GONetMain.IsServer && GONetMain.MyAuthorityId == 1;

                case MachineRequirement.MultipleClients:
                    if (GONetMain.IsServer)
                    {
                        // Server: Check if at least 2 clients are connected
                        return GONetMain.gonetServer != null && GONetMain.gonetServer.remoteClients.Count >= 2;
                    }
                    else
                    {
                        // Clients can initiate multi-client tests, but server will validate
                        return true;
                    }

                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets human-readable display name for a test category.
        /// Used by UI dropdowns.
        /// </summary>
        /// <param name="category">Category enum value</param>
        /// <returns>Formatted display string</returns>
        public static string GetCategoryDisplayName(TestCategory category)
        {
            switch (category)
            {
                case TestCategory.TargetRpc_Targeting:
                    return "TargetRpc - Targeting Modes";
                case TestCategory.TargetRpc_Validation:
                    return "TargetRpc - Validation";
                case TestCategory.ServerRpc_Relay:
                    return "ServerRpc - Relay Modes";
                case TestCategory.Persistence:
                    return "Persistence & Late-Joiners";
                case TestCategory.ComplexTypes:
                    return "Complex Parameter Types";
                case TestCategory.Lifecycle:
                    return "Lifecycle & Timing";
                case TestCategory.ErrorHandling:
                    return "Error Handling";
                case TestCategory.Stress:
                    return "Stress & Performance";
                default:
                    return category.ToString();
            }
        }

        /// <summary>
        /// Clears all registered tests.
        /// Useful for testing or when reloading test classes.
        /// </summary>
        public static void ClearRegistry()
        {
            registry.Clear();
        }

        /// <summary>
        /// Gets total count of registered tests across all categories.
        /// </summary>
        public static int GetTotalTestCount()
        {
            int count = 0;
            foreach (var tests in registry.Values)
            {
                count += tests.Count;
            }
            return count;
        }
    }
}
