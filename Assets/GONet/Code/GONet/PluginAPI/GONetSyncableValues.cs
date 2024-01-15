/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace GONet
{
    public enum GONetSyncableValueTypes : byte
    {
        System_Boolean,
        System_Byte,
        System_SByte,
        System_Int16,
        System_UInt16,
        System_Int32,
        System_UInt32,
        System_Int64,
        System_UInt64,
        System_Single,
        System_Double,
        // ?? TODO reference type support: System_String,
        UnityEngine_Vector2,
        UnityEngine_Vector3,
        UnityEngine_Vector4,
        UnityEngine_Quaternion
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GONetSyncableValue
    {
        [FieldOffset(0)] public GONetSyncableValueTypes GONetSyncType;

        [FieldOffset(1)] bool system_Boolean;
        public bool System_Boolean { get => system_Boolean; set { system_Boolean = value; GONetSyncType = GONetSyncableValueTypes.System_Boolean; } }

        [FieldOffset(1)] byte system_Byte;
        public byte System_Byte { get => system_Byte; set { system_Byte = value; GONetSyncType = GONetSyncableValueTypes.System_Byte; } }

        [FieldOffset(1)] sbyte system_SByte;
        public sbyte System_SByte { get => system_SByte; set { system_SByte = value; GONetSyncType = GONetSyncableValueTypes.System_SByte; } }

        [FieldOffset(1)] short system_Int16;
        public short System_Int16 { get => system_Int16; set { system_Int16 = value; GONetSyncType = GONetSyncableValueTypes.System_Int16; } }

        [FieldOffset(1)] ushort system_UInt16;
        public ushort System_UInt16 { get => system_UInt16; set { system_UInt16 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt16; } }

        [FieldOffset(1)] int system_Int32;
        public int System_Int32 { get => system_Int32; set { system_Int32 = value; GONetSyncType = GONetSyncableValueTypes.System_Int32; } }

        [FieldOffset(1)] uint system_UInt32;
        public uint System_UInt32 { get => system_UInt32; set { system_UInt32 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt32; } }

        [FieldOffset(1)] long system_Int64;
        public long System_Int64 { get => system_Int64; set { system_Int64 = value; GONetSyncType = GONetSyncableValueTypes.System_Int64; } }

        [FieldOffset(1)] ulong system_UInt64;
        public ulong System_UInt64 { get => system_UInt64; set { system_UInt64 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt64; } }

        [FieldOffset(1)] float system_Single;
        public float System_Single { get => system_Single; set { system_Single = value; GONetSyncType = GONetSyncableValueTypes.System_Single; } }

        [FieldOffset(1)] double system_Double;
        public double System_Double { get => system_Double; set { system_Double = value; GONetSyncType = GONetSyncableValueTypes.System_Double; } }

        [FieldOffset(1)] Vector2 unityEngine_Vector2;
        public Vector2 UnityEngine_Vector2 { get => unityEngine_Vector2; set { unityEngine_Vector2 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector2; } }

        [FieldOffset(1)] Vector3 unityEngine_Vector3;
        public Vector3 UnityEngine_Vector3 { get => unityEngine_Vector3; set { unityEngine_Vector3 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector3; } }

        [FieldOffset(1)] Vector4 unityEngine_Vector4;
        public Vector4 UnityEngine_Vector4 { get => unityEngine_Vector4; set { unityEngine_Vector4 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector4; } }

        [FieldOffset(1)] Quaternion unityEngine_Quaternion;
        public Quaternion UnityEngine_Quaternion { get => unityEngine_Quaternion; set { unityEngine_Quaternion = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Quaternion; } }

        #region operators == and !=

        public static bool operator ==(GONetSyncableValue left, GONetSyncableValue right)
        {
            bool areValuesEqual = false;
            switch (left.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: areValuesEqual = left.system_Boolean == right.system_Boolean; break;
                case GONetSyncableValueTypes.System_Byte: areValuesEqual = left.system_Byte == right.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: areValuesEqual = left.system_Double == right.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: areValuesEqual = left.system_Int16 == right.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: areValuesEqual = left.system_Int32 == right.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: areValuesEqual = left.system_Int64 == right.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: areValuesEqual = left.system_SByte == right.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: areValuesEqual = left.system_Single == right.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: areValuesEqual = left.system_UInt16 == right.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: areValuesEqual = left.system_UInt32 == right.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: areValuesEqual = left.system_UInt64 == right.system_UInt64; break;

                case GONetSyncableValueTypes.UnityEngine_Quaternion:
                    { // the following method is used to compare orientation equality (since quat == quat is pure equality) in a faster way than quat.eulerAngles == quat.eulerAngles
                        float angleDiff = Quaternion.Angle(left.unityEngine_Quaternion, right.unityEngine_Quaternion);
                        angleDiff = angleDiff < 0 ? -angleDiff : angleDiff;
                        areValuesEqual = angleDiff < 1e-3f;
                        break;
                    }

                case GONetSyncableValueTypes.UnityEngine_Vector2: areValuesEqual = left.unityEngine_Vector2 == right.unityEngine_Vector2; break;
                case GONetSyncableValueTypes.UnityEngine_Vector3: areValuesEqual = left.unityEngine_Vector3 == right.unityEngine_Vector3; break;
                case GONetSyncableValueTypes.UnityEngine_Vector4: areValuesEqual = left.unityEngine_Vector4 == right.unityEngine_Vector4; break;
            }

            return left.GONetSyncType == right.GONetSyncType && areValuesEqual;
        }

        public static bool operator !=(GONetSyncableValue left, GONetSyncableValue right)
        {
            bool areValuesEqual = false;
            switch (left.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: areValuesEqual = left.system_Boolean == right.system_Boolean; break;
                case GONetSyncableValueTypes.System_Byte: areValuesEqual = left.system_Byte == right.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: areValuesEqual = left.system_Double == right.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: areValuesEqual = left.system_Int16 == right.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: areValuesEqual = left.system_Int32 == right.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: areValuesEqual = left.system_Int64 == right.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: areValuesEqual = left.system_SByte == right.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: areValuesEqual = left.system_Single == right.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: areValuesEqual = left.system_UInt16 == right.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: areValuesEqual = left.system_UInt32 == right.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: areValuesEqual = left.system_UInt64 == right.system_UInt64; break;

                case GONetSyncableValueTypes.UnityEngine_Quaternion:
                    { // the following method is used to compare orientation equality (since quat == quat is pure equality) in a faster way than quat.eulerAngles == quat.eulerAngles
                        float angleDiff = Quaternion.Angle(left.unityEngine_Quaternion, right.unityEngine_Quaternion);
                        angleDiff = angleDiff < 0 ? -angleDiff : angleDiff;
                        areValuesEqual = angleDiff < 1e-3f;
                        break;
                    }

                case GONetSyncableValueTypes.UnityEngine_Vector2: areValuesEqual = left.unityEngine_Vector2 == right.unityEngine_Vector2; break;
                case GONetSyncableValueTypes.UnityEngine_Vector3: areValuesEqual = left.unityEngine_Vector3 == right.unityEngine_Vector3; break;
                case GONetSyncableValueTypes.UnityEngine_Vector4: areValuesEqual = left.unityEngine_Vector4 == right.unityEngine_Vector4; break;
            }

            return left.GONetSyncType != right.GONetSyncType || !areValuesEqual;
        }

        [Conditional("GONET_MEASURE_VALUES_MIN_MAX")]
        internal static void UpdateMinimumEncountered_IfApppropriate(ref GONetSyncableValue minimum, GONetSyncableValue value)
        {
            switch (minimum.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: break;
                case GONetSyncableValueTypes.UnityEngine_Quaternion: break;

                case GONetSyncableValueTypes.System_Byte: if (value.system_Byte < minimum.system_Byte) minimum.system_Byte = value.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: if (value.system_Double < minimum.system_Double) minimum.system_Double = value.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: if (value.system_Int16 < minimum.system_Int16) minimum.system_Int16 = value.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: if (value.system_Int32 < minimum.system_Int32) minimum.system_Int32 = value.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: if (value.system_Int64 < minimum.system_Int64) minimum.system_Int64 = value.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: if (value.system_SByte < minimum.system_SByte) minimum.system_SByte = value.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: if (value.system_Single < minimum.system_Single) minimum.system_Single = value.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: if (value.system_UInt16 < minimum.system_UInt16) minimum.system_UInt16 = value.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: if (value.system_UInt32 < minimum.system_UInt32) minimum.system_UInt32 = value.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: if (value.system_UInt64 < minimum.system_UInt64) minimum.system_UInt64 = value.system_UInt64; break;

                case GONetSyncableValueTypes.UnityEngine_Vector2:
                    if (value.unityEngine_Vector2.x < minimum.unityEngine_Vector2.x) minimum.unityEngine_Vector2.x = value.unityEngine_Vector2.x;
                    if (value.unityEngine_Vector2.y < minimum.unityEngine_Vector2.y) minimum.unityEngine_Vector2.y = value.unityEngine_Vector2.y;
                    break;

                case GONetSyncableValueTypes.UnityEngine_Vector3:
                    if (value.unityEngine_Vector3.x < minimum.unityEngine_Vector3.x) minimum.unityEngine_Vector3.x = value.unityEngine_Vector3.x;
                    if (value.unityEngine_Vector3.y < minimum.unityEngine_Vector3.y) minimum.unityEngine_Vector3.y = value.unityEngine_Vector3.y;
                    if (value.unityEngine_Vector3.z < minimum.unityEngine_Vector3.z) minimum.unityEngine_Vector3.z = value.unityEngine_Vector3.z;
                    break;

                case GONetSyncableValueTypes.UnityEngine_Vector4:
                    if (value.unityEngine_Vector4.x < minimum.unityEngine_Vector4.x) minimum.unityEngine_Vector4.x = value.unityEngine_Vector4.x;
                    if (value.unityEngine_Vector4.y < minimum.unityEngine_Vector4.y) minimum.unityEngine_Vector4.y = value.unityEngine_Vector4.y;
                    if (value.unityEngine_Vector4.z < minimum.unityEngine_Vector4.z) minimum.unityEngine_Vector4.z = value.unityEngine_Vector4.z;
                    if (value.unityEngine_Vector4.w < minimum.unityEngine_Vector4.w) minimum.unityEngine_Vector4.w = value.unityEngine_Vector4.w;
                    break;
            }
        }

        public override string ToString()
        {
            switch (GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: return system_Boolean.ToString();
                case GONetSyncableValueTypes.System_Byte: return system_Byte.ToString();
                case GONetSyncableValueTypes.System_Double: return system_Double.ToString();
                case GONetSyncableValueTypes.System_Int16: return system_Int16.ToString();
                case GONetSyncableValueTypes.System_Int32: return system_Int32.ToString();
                case GONetSyncableValueTypes.System_Int64: return system_Int64.ToString();
                case GONetSyncableValueTypes.System_SByte: return system_SByte.ToString();
                case GONetSyncableValueTypes.System_Single: return system_Single.ToString();
                case GONetSyncableValueTypes.System_UInt16: return system_UInt16.ToString();
                case GONetSyncableValueTypes.System_UInt32: return system_UInt32.ToString();
                case GONetSyncableValueTypes.System_UInt64: return system_UInt64.ToString();
                case GONetSyncableValueTypes.UnityEngine_Quaternion: return unityEngine_Quaternion.ToString();
                case GONetSyncableValueTypes.UnityEngine_Vector2: return unityEngine_Vector2.ToString();
                case GONetSyncableValueTypes.UnityEngine_Vector3: return ToStringFull(unityEngine_Vector3);
                case GONetSyncableValueTypes.UnityEngine_Vector4: return unityEngine_Vector4.ToString();
            }

            return base.ToString();
        }

        public static string ToStringFull(Vector3 vector3)
        {
            const string RP = ")";
            const string LP = "(";
            const string CS = ", ";
            return string.Concat(LP, vector3.x, CS, vector3.y, CS, vector3.z, RP);
        }

        [Conditional("GONET_MEASURE_VALUES_MIN_MAX")]
        internal static void UpdateMaximumEncountered_IfApppropriate(ref GONetSyncableValue maximum, GONetSyncableValue value)
        {
            switch (maximum.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: break;
                case GONetSyncableValueTypes.UnityEngine_Quaternion: break;

                case GONetSyncableValueTypes.System_Byte: if (value.system_Byte > maximum.system_Byte) maximum.system_Byte = value.system_Byte; break;
                case GONetSyncableValueTypes.System_Double: if (value.system_Double > maximum.system_Double) maximum.system_Double = value.system_Double; break;
                case GONetSyncableValueTypes.System_Int16: if (value.system_Int16 > maximum.system_Int16) maximum.system_Int16 = value.system_Int16; break;
                case GONetSyncableValueTypes.System_Int32: if (value.system_Int32 > maximum.system_Int32) maximum.system_Int32 = value.system_Int32; break;
                case GONetSyncableValueTypes.System_Int64: if (value.system_Int64 > maximum.system_Int64) maximum.system_Int64 = value.system_Int64; break;
                case GONetSyncableValueTypes.System_SByte: if (value.system_SByte > maximum.system_SByte) maximum.system_SByte = value.system_SByte; break;
                case GONetSyncableValueTypes.System_Single: if (value.system_Single > maximum.system_Single) maximum.system_Single = value.system_Single; break;
                case GONetSyncableValueTypes.System_UInt16: if (value.system_UInt16 > maximum.system_UInt16) maximum.system_UInt16 = value.system_UInt16; break;
                case GONetSyncableValueTypes.System_UInt32: if (value.system_UInt32 > maximum.system_UInt32) maximum.system_UInt32 = value.system_UInt32; break;
                case GONetSyncableValueTypes.System_UInt64: if (value.system_UInt64 > maximum.system_UInt64) maximum.system_UInt64 = value.system_UInt64; break;

                case GONetSyncableValueTypes.UnityEngine_Vector2:
                    if (value.unityEngine_Vector2.x > maximum.unityEngine_Vector2.x) maximum.unityEngine_Vector2.x = value.unityEngine_Vector2.x;
                    if (value.unityEngine_Vector2.y > maximum.unityEngine_Vector2.y) maximum.unityEngine_Vector2.y = value.unityEngine_Vector2.y;
                    break;

                case GONetSyncableValueTypes.UnityEngine_Vector3:
                    if (value.unityEngine_Vector3.x > maximum.unityEngine_Vector3.x) maximum.unityEngine_Vector3.x = value.unityEngine_Vector3.x;
                    if (value.unityEngine_Vector3.y > maximum.unityEngine_Vector3.y) maximum.unityEngine_Vector3.y = value.unityEngine_Vector3.y;
                    if (value.unityEngine_Vector3.z > maximum.unityEngine_Vector3.z) maximum.unityEngine_Vector3.z = value.unityEngine_Vector3.z;
                    break;

                case GONetSyncableValueTypes.UnityEngine_Vector4:
                    if (value.unityEngine_Vector4.x > maximum.unityEngine_Vector4.x) maximum.unityEngine_Vector4.x = value.unityEngine_Vector4.x;
                    if (value.unityEngine_Vector4.y > maximum.unityEngine_Vector4.y) maximum.unityEngine_Vector4.y = value.unityEngine_Vector4.y;
                    if (value.unityEngine_Vector4.z > maximum.unityEngine_Vector4.z) maximum.unityEngine_Vector4.z = value.unityEngine_Vector4.z;
                    if (value.unityEngine_Vector4.w > maximum.unityEngine_Vector4.w) maximum.unityEngine_Vector4.w = value.unityEngine_Vector4.w;
                    break;
            }
        }

        #endregion

        #region implicit conversions from primitive values wrapped herein

        public static implicit operator GONetSyncableValue(bool value) { return new GONetSyncableValue() { System_Boolean = value }; }
        public static implicit operator GONetSyncableValue(byte value) { return new GONetSyncableValue() { System_Byte = value }; }
        public static implicit operator GONetSyncableValue(double value) { return new GONetSyncableValue() { System_Double = value }; }
        public static implicit operator GONetSyncableValue(short value) { return new GONetSyncableValue() { System_Int16 = value }; }
        public static implicit operator GONetSyncableValue(int value) { return new GONetSyncableValue() { System_Int32 = value }; }
        public static implicit operator GONetSyncableValue(long value) { return new GONetSyncableValue() { System_Int64 = value }; }
        public static implicit operator GONetSyncableValue(sbyte value) { return new GONetSyncableValue() { System_SByte = value }; }
        public static implicit operator GONetSyncableValue(float value) { return new GONetSyncableValue() { System_Single = value }; }
        public static implicit operator GONetSyncableValue(ushort value) { return new GONetSyncableValue() { System_UInt16 = value }; }
        public static implicit operator GONetSyncableValue(uint value) { return new GONetSyncableValue() { System_UInt32 = value }; }
        public static implicit operator GONetSyncableValue(ulong value) { return new GONetSyncableValue() { System_UInt64 = value }; }
        public static implicit operator GONetSyncableValue(Quaternion value) { return new GONetSyncableValue() { UnityEngine_Quaternion = value }; }
        public static implicit operator GONetSyncableValue(Vector2 value) { return new GONetSyncableValue() { UnityEngine_Vector2 = value }; }
        public static implicit operator GONetSyncableValue(Vector3 value) { return new GONetSyncableValue() { UnityEngine_Vector3 = value }; }
        public static implicit operator GONetSyncableValue(Vector4 value) { return new GONetSyncableValue() { UnityEngine_Vector4 = value }; }

        #endregion
    }
}
