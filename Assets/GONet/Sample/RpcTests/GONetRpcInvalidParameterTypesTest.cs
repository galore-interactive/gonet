using GONet;
using System.Collections.Generic;
using UnityEngine;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// TEST FILE: Contains intentionally invalid RPC parameter types to verify code generation validation.
    ///
    /// EXPECTED: This file should cause compilation errors with helpful error messages
    /// explaining which parameter types are invalid and how to fix them.
    ///
    /// PURPOSE: Validates that GONet's RPC parameter type validation is working correctly.
    ///
    /// TO TEST:
    /// 1. Uncomment one of the invalid RPC methods below
    /// 2. Right-click this file → Reimport (triggers code generation)
    /// 3. Check Unity Console for validation errors
    /// 4. Verify error messages are helpful and suggest fixes
    /// 5. Re-comment the method and reimport to clear errors
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class GONetRpcInvalidParameterTypesTest : GONetParticipantCompanionBehaviour
    {
        // Custom types for testing
        public enum TestEnum { A, B, C }
        public struct TestStruct { public int id; public float value; }

        #region TEST 1: Enum Parameter (INVALID - should fail validation)

        [ServerRpc]
        void SendEnum(TestEnum enumValue)
        {
            // EXPECTED ERROR:
            // ERROR: SendEnum parameter 'enumValue' has unsupported type 'TestEnum'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for enum: Cast to int before sending:
            //     CallRpc(nameof(MethodName), (int)myEnumValue);
        }

        #endregion

        #region TEST 2: Custom Struct (INVALID - should fail validation)
        /*
        [ServerRpc]
        void SendStruct(TestStruct data)
        {
            // EXPECTED ERROR:
            // ERROR: SendStruct parameter 'data' has unsupported type 'TestStruct'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for custom struct: Break into primitive parameters:
            //     CallRpc(nameof(SendData), myStruct.field1, myStruct.field2, ...);
        }
        */
        #endregion

        #region TEST 3: Color Parameter (INVALID - should fail validation)
        /*
        [ServerRpc]
        void SendColor(Color color)
        {
            // EXPECTED ERROR:
            // ERROR: SendColor parameter 'color' has unsupported type 'Color'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for Color: Break into r,g,b,a floats:
            //     CallRpc(nameof(SetColor), color.r, color.g, color.b, color.a);
        }
        */
        #endregion

        #region TEST 4: Array Parameter (INVALID - should fail validation)
        /*
        [ServerRpc]
        void SendArray(int[] numbers)
        {
            // EXPECTED ERROR:
            // ERROR: SendArray parameter 'numbers' has unsupported type 'Int32[]'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for array: Send elements one at a time:
            //     for (int i = 0; i < array.Length; i++)
            //         CallRpc(nameof(SendElement), array[i], i);
            //   OR use [GONetAutoMagicalSync] instead of RPC for array sync
        }
        */
        #endregion

        #region TEST 5: List Parameter (INVALID - should fail validation)
        /*
        [ServerRpc]
        void SendList(List<int> numbers)
        {
            // EXPECTED ERROR:
            // ERROR: SendList parameter 'numbers' has unsupported type 'List`1'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for List/Dictionary: Use [GONetAutoMagicalSync] for collection sync
        }
        */
        #endregion

        #region TEST 6: Dictionary Parameter (INVALID - should fail validation)
        /*
        [ServerRpc]
        void SendDictionary(Dictionary<int, string> data)
        {
            // EXPECTED ERROR:
            // ERROR: SendDictionary parameter 'data' has unsupported type 'Dictionary`2'
            //   GONet RPCs only support types from GONetSyncableValueTypes
            //   FIX for List/Dictionary: Use [GONetAutoMagicalSync] for collection sync
        }
        */
        #endregion

        #region VALID EXAMPLES (These should work correctly)

        [ServerRpc]
        void ValidRpc_Primitives(int id, float value, string name, bool flag)
        {
            // ✅ Valid - all parameters are supported types
        }

        [ServerRpc]
        void ValidRpc_UnityMath(Vector3 position, Quaternion rotation, Vector2 uv)
        {
            // ✅ Valid - Unity math types are supported
        }

        [ServerRpc]
        void ValidRpc_EnumAsInt(int enumValue)
        {
            // ✅ Valid workaround for enum: cast to int before sending
            // Usage: CallRpc(nameof(ValidRpc_EnumAsInt), (int)myEnumValue);
        }

        [ServerRpc]
        void ValidRpc_ColorAsFloats(float r, float g, float b, float a)
        {
            // ✅ Valid workaround for Color: break into components
            // Usage: CallRpc(nameof(ValidRpc_ColorAsFloats), color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}
