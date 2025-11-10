using GONet;
using MemoryPack;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    #region Custom Types for Testing (MUST be at namespace level for MemoryPack)

    public enum TestEnum : byte
    {
        ValueA = 0,
        ValueB = 1,
        ValueC = 2
    }

    [MemoryPackable]
    public partial struct TestStruct
    {
        public int id;
        public float value;
        public string name;
    }

    [MemoryPackable]
    public partial struct NestedStruct
    {
        public TestStruct inner;
        public Vector3 position;
        public TestEnum enumValue;
    }

    // Non-MemoryPackable struct (should FAIL)
    public struct NonMemoryPackableStruct
    {
        public int id;
        public float value;
    }

    #endregion

    /// <summary>
    /// TEST FILE: Validates which types are supported by GONet RPC MemoryPack serialization.
    ///
    /// PURPOSE:
    /// - Prove what types actually work through testing, not assumptions
    /// - Document MemoryPack's capabilities for GONet RPC parameters
    /// - Provide reference examples for users
    ///
    /// TESTING STRATEGY:
    /// 1. Create RPCs with various parameter types
    /// 2. Trigger code generation (right-click file → Reimport)
    /// 3. Check Unity Console for errors
    /// 4. Test at runtime with Shift+M (MemoryPack types test)
    ///
    /// EXPECTED RESULTS:
    /// - Built-in types (primitives, string, Vector3, Quaternion): ✅ Should work
    /// - Enums: ✅ Should work (MemoryPack supports enums)
    /// - Custom [MemoryPackable] structs: ✅ Should work (MUST be at namespace level, not nested)
    /// - Arrays: ✅ Should work (MemoryPack supports arrays)
    /// - Lists/Dictionaries: ✅ Should work (MemoryPack supports collections)
    /// - Color: ❓ Need to test (Unity type, not in GONetSyncableValueTypes)
    /// - Non-MemoryPackable custom types: ❌ Should fail
    ///
    /// IMPORTANT: MemoryPackable types MUST NOT be nested inside classes (MEMPACK002 error)
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcMemoryPackTypesTest : GONetParticipantCompanionBehaviour
    {

        #region Test Results Tracking

        private List<string> testResults = new List<string>();

        private void LogTestResult(string testName, bool success, string message = "")
        {
            string result = success ? "✅ PASS" : "❌ FAIL";
            string log = $"{result}: {testName}";
            if (!string.IsNullOrEmpty(message))
                log += $" - {message}";

            testResults.Add(log);
            GONetLog.Info(log);
        }

        private void PrintTestSummary()
        {
            GONetLog.Info("========== MEMORYPACK RPC TYPE TEST SUMMARY ==========");
            foreach (var result in testResults)
            {
                GONetLog.Info(result);
            }
            GONetLog.Info($"Total tests: {testResults.Count}");
            GONetLog.Info("======================================================");
        }

        #endregion

        #region Built-in Types (Expected: ✅ All should work)

        [ServerRpc]
        internal void Test_Primitives(int intVal, float floatVal, bool boolVal, double doubleVal, long longVal, byte byteVal)
        {
            LogTestResult("Primitives", true, $"int={intVal}, float={floatVal}, bool={boolVal}, double={doubleVal}, long={longVal}, byte={byteVal}");
        }

        [ServerRpc]
        internal void Test_String(string message)
        {
            LogTestResult("String", true, $"message='{message}'");
        }

        [ServerRpc]
        internal void Test_Vector3(Vector3 position)
        {
            LogTestResult("Vector3", true, $"position={position}");
        }

        [ServerRpc]
        internal void Test_Quaternion(Quaternion rotation)
        {
            LogTestResult("Quaternion", true, $"rotation={rotation}");
        }

        [ServerRpc]
        internal void Test_Vector2(Vector2 uv)
        {
            LogTestResult("Vector2", true, $"uv={uv}");
        }

        // Vector4 - NOT SUPPORTED by MemoryPack (validation error)
        // [ServerRpc]
        // void Test_Vector4(Vector4 plane)
        // {
        //     LogTestResult("Vector4", true, $"plane={plane}");
        // }

        #endregion

        #region Enums (Expected: ✅ Should work)

        [ServerRpc]
        internal void Test_Enum(TestEnum enumValue)
        {
            LogTestResult("Enum", true, $"enumValue={enumValue}");
        }

        [ServerRpc]
        internal void Test_EnumAsInt(int enumAsInt)
        {
            TestEnum enumValue = (TestEnum)enumAsInt;
            LogTestResult("EnumAsInt workaround", true, $"enumValue={enumValue}");
        }

        #endregion

        #region Color (Result: ❌ NOT SUPPORTED by MemoryPack)

        // Color - NOT SUPPORTED by MemoryPack (validation error)
        // ERROR: parameter 'color' type 'UnityEngine.Color' not serializable
        // WORKAROUND: Break into RGBA floats
        // [ServerRpc]
        // void Test_Color(Color color)
        // {
        //     LogTestResult("Color", true, $"color={color}");
        // }

        // Color32 - NOT SUPPORTED by MemoryPack (validation error)
        // ERROR: parameter 'color32' type 'UnityEngine.Color32' not serializable
        // WORKAROUND: Break into RGBA bytes
        // [ServerRpc]
        // void Test_Color32(Color32 color32)
        // {
        //     LogTestResult("Color32", true, $"color32={color32}");
        // }

        // Workaround: Send Color as separate RGBA components
        [ServerRpc]
        internal void Test_ColorAsFloats(float r, float g, float b, float a)
        {
            Color reconstructed = new Color(r, g, b, a);
            LogTestResult("Color as RGBA floats (workaround)", true, $"color={reconstructed}");
        }

        #endregion

        #region Custom MemoryPackable Structs (Expected: ✅ Should work)

        [ServerRpc]
        internal void Test_CustomStruct(TestStruct data)
        {
            LogTestResult("Custom [MemoryPackable] struct", true, $"id={data.id}, value={data.value}, name='{data.name}'");
        }

        [ServerRpc]
        internal void Test_NestedStruct(NestedStruct nested)
        {
            LogTestResult("Nested [MemoryPackable] struct", true, $"inner.id={nested.inner.id}, position={nested.position}, enumValue={nested.enumValue}");
        }

        #endregion

        #region Arrays (Expected: ✅ Should work)

        [ServerRpc]
        internal void Test_IntArray(int[] numbers)
        {
            LogTestResult("int[]", true, $"length={numbers?.Length ?? 0}, first={(numbers != null && numbers.Length > 0 ? numbers[0].ToString() : "null")}");
        }

        [ServerRpc]
        internal void Test_Vector3Array(Vector3[] positions)
        {
            LogTestResult("Vector3[]", true, $"length={positions?.Length ?? 0}");
        }

        [ServerRpc]
        internal void Test_StringArray(string[] messages)
        {
            LogTestResult("string[]", true, $"length={messages?.Length ?? 0}");
        }

        [ServerRpc]
        internal void Test_CustomStructArray(TestStruct[] data)
        {
            LogTestResult("TestStruct[]", true, $"length={data?.Length ?? 0}");
        }

        #endregion

        #region Collections (Expected: ✅ Should work)

        [ServerRpc]
        internal void Test_ListInt(List<int> numbers)
        {
            LogTestResult("List<int>", true, $"count={numbers?.Count ?? 0}");
        }

        [ServerRpc]
        internal void Test_ListString(List<string> messages)
        {
            LogTestResult("List<string>", true, $"count={messages?.Count ?? 0}");
        }

        [ServerRpc]
        internal void Test_ListVector3(List<Vector3> positions)
        {
            LogTestResult("List<Vector3>", true, $"count={positions?.Count ?? 0}");
        }

        [ServerRpc]
        internal void Test_Dictionary(Dictionary<int, string> data)
        {
            LogTestResult("Dictionary<int, string>", true, $"count={data?.Count ?? 0}");
        }

        #endregion

        #region Non-MemoryPackable Types (Expected: ❌ Should fail at code generation)

        // UNCOMMENT TO TEST FAILURE CASE:
        /*
        [ServerRpc]
        internal void Test_NonMemoryPackableStruct(NonMemoryPackableStruct data)
        {
            // This should FAIL code generation with error message about missing [MemoryPackable]
            LogTestResult("Non-MemoryPackable struct", false, "Should not compile!");
        }
        */

        #endregion

        #region Runtime Test Execution (Shift+M)

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.LeftShift))
            {
                RunAllTests();
            }
        }

        private void RunAllTests()
        {
            testResults.Clear();
            GONetLog.Info("========== STARTING MEMORYPACK RPC TYPE TESTS ==========");

            if (GONetMain.IsClient && !GONetMain.IsServer)
            {
                // Built-in types
                CallRpc(nameof(Test_Primitives), 42, 3.14f, true, 2.71828, 12345678901234L, (byte)255);
                CallRpc(nameof(Test_String), "Hello from MemoryPack!");
                CallRpc(nameof(Test_Vector3), new Vector3(1, 2, 3));
                CallRpc(nameof(Test_Quaternion), Quaternion.Euler(45, 90, 180));
                CallRpc(nameof(Test_Vector2), new Vector2(0.5f, 0.75f));
                // Vector4 NOT supported - commented out

                // Enums
                CallRpc(nameof(Test_Enum), TestEnum.ValueB);
                CallRpc(nameof(Test_EnumAsInt), (int)TestEnum.ValueC);

                // Color workaround (Color/Color32 NOT supported directly)
                Color testColor = Color.red;
                CallRpc(nameof(Test_ColorAsFloats), testColor.r, testColor.g, testColor.b, testColor.a);

                // Custom structs
                var testStruct = new TestStruct { id = 100, value = 99.9f, name = "Test" };
                CallRpc(nameof(Test_CustomStruct), testStruct);

                var nestedStruct = new NestedStruct
                {
                    inner = testStruct,
                    position = new Vector3(5, 10, 15),
                    enumValue = TestEnum.ValueA
                };
                CallRpc(nameof(Test_NestedStruct), nestedStruct);

                // Arrays
                CallRpc(nameof(Test_IntArray), new int[] { 1, 2, 3, 4, 5 });
                CallRpc(nameof(Test_Vector3Array), new Vector3[] { Vector3.up, Vector3.forward, Vector3.right });
                CallRpc(nameof(Test_StringArray), new string[] { "one", "two", "three" });
                CallRpc(nameof(Test_CustomStructArray), new TestStruct[] { testStruct, testStruct });

                // Collections
                CallRpc(nameof(Test_ListInt), new List<int> { 10, 20, 30 });
                CallRpc(nameof(Test_ListString), new List<string> { "alpha", "beta", "gamma" });
                CallRpc(nameof(Test_ListVector3), new List<Vector3> { Vector3.zero, Vector3.one });
                CallRpc(nameof(Test_Dictionary), new Dictionary<int, string> { { 1, "one" }, { 2, "two" } });

                GONetLog.Info("Sent all test RPCs from client");
            }
            else
            {
                GONetLog.Warning("This test must be run from a client (not server). Press Shift+M on a client instance.");
            }
        }

        private void OnApplicationQuit()
        {
            if (testResults.Count > 0)
            {
                PrintTestSummary();
            }
        }

        #endregion
    }
}
