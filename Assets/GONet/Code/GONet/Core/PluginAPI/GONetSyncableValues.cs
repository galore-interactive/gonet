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

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        public bool System_Boolean
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Boolean;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Boolean = value; GONetSyncType = GONetSyncableValueTypes.System_Boolean; }
        }

        [FieldOffset(1)] byte system_Byte;
        public byte System_Byte
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Byte;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Byte = value; GONetSyncType = GONetSyncableValueTypes.System_Byte; }
        }

        [FieldOffset(1)] sbyte system_SByte;
        public sbyte System_SByte
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_SByte;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_SByte = value; GONetSyncType = GONetSyncableValueTypes.System_SByte; }
        }

        [FieldOffset(1)] short system_Int16;
        public short System_Int16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Int16;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Int16 = value; GONetSyncType = GONetSyncableValueTypes.System_Int16; }
        }

        [FieldOffset(1)] ushort system_UInt16;
        public ushort System_UInt16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_UInt16;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_UInt16 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt16; }
        }

        [FieldOffset(1)] int system_Int32;
        public int System_Int32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Int32;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Int32 = value; GONetSyncType = GONetSyncableValueTypes.System_Int32; }
        }

        [FieldOffset(1)] uint system_UInt32;
        public uint System_UInt32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_UInt32;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_UInt32 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt32; }
        }

        [FieldOffset(1)] long system_Int64;
        public long System_Int64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Int64;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Int64 = value; GONetSyncType = GONetSyncableValueTypes.System_Int64; }
        }

        [FieldOffset(1)] ulong system_UInt64;
        public ulong System_UInt64
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_UInt64;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_UInt64 = value; GONetSyncType = GONetSyncableValueTypes.System_UInt64; }
        }

        [FieldOffset(1)] float system_Single;
        public float System_Single
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Single;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Single = value; GONetSyncType = GONetSyncableValueTypes.System_Single; }
        }

        [FieldOffset(1)] double system_Double;
        public double System_Double
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => system_Double;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { system_Double = value; GONetSyncType = GONetSyncableValueTypes.System_Double; }
        }

        [FieldOffset(1)] Vector2 unityEngine_Vector2;
        public Vector2 UnityEngine_Vector2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unityEngine_Vector2;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unityEngine_Vector2 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector2; }
        }

        [FieldOffset(1)] Vector3 unityEngine_Vector3;
        public Vector3 UnityEngine_Vector3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unityEngine_Vector3;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unityEngine_Vector3 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector3; }
        }

        [FieldOffset(1)] Vector4 unityEngine_Vector4;
        public Vector4 UnityEngine_Vector4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unityEngine_Vector4;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unityEngine_Vector4 = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector4; }
        }

        [FieldOffset(1)] Quaternion unityEngine_Quaternion;
        public Quaternion UnityEngine_Quaternion
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => unityEngine_Quaternion;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { unityEngine_Quaternion = value; GONetSyncType = GONetSyncableValueTypes.UnityEngine_Quaternion; }
        }

        #region Ultra High-Performance Unsafe Accessors, Direct pointer access methods - fastest possible

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector3* GetVector3Ptr()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return (Vector3*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float* GetVector3ComponentsPtr()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return (float*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Quaternion* GetQuaternionPtr()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return (Quaternion*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector2* GetVector2Ptr()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return (Vector2*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector4* GetVector4Ptr()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return (Vector4*)((byte*)ptr + 1);
            }
        }

        // Fastest component-wise Vector3 access
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void GetVector3Components(out float x, out float y, out float z)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                float* components = (float*)((byte*)ptr + 1);
                x = components[0];
                y = components[1];
                z = components[2];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetVector3Components(float x, float y, float z)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                float* components = (float*)((byte*)ptr + 1);
                components[0] = x;
                components[1] = y;
                components[2] = z;
                ptr->GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector3;
            }
        }
        #endregion

        #region Unsafe High-Performance Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool GetBooleanUnsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(bool*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetBooleanUnsafe(bool value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(bool*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.System_Boolean;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe float GetSingleUnsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(float*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetSingleUnsafe(float value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(float*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.System_Single;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe double GetDoubleUnsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(double*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetDoubleUnsafe(double value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(double*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.System_Double;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector2 GetVector2Unsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(Vector2*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetVector2Unsafe(Vector2 value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(Vector2*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector2;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector3 GetVector3Unsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(Vector3*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetVector3Unsafe(Vector3 value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(Vector3*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector3;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Vector4 GetVector4Unsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(Vector4*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetVector4Unsafe(Vector4 value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(Vector4*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.UnityEngine_Vector4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Quaternion GetQuaternionUnsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(Quaternion*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetQuaternionUnsafe(Quaternion value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(Quaternion*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.UnityEngine_Quaternion;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int GetInt32Unsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(int*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetInt32Unsafe(int value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(int*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.System_Int32;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe long GetInt64Unsafe()
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                return *(long*)((byte*)ptr + 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void SetInt64Unsafe(long value)
        {
            fixed (GONetSyncableValue* ptr = &this)
            {
                *(long*)((byte*)ptr + 1) = value;
                ptr->GONetSyncType = GONetSyncableValueTypes.System_Int64;
            }
        }

        #endregion

        #region operators == and !=

        public static bool operator ==(GONetSyncableValue left, GONetSyncableValue right)
        {
            if (left.GONetSyncType != right.GONetSyncType) return false;

            switch (left.GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: return left.system_Boolean == right.system_Boolean;
                case GONetSyncableValueTypes.System_Byte: return left.system_Byte == right.system_Byte;
                case GONetSyncableValueTypes.System_Double: return left.system_Double == right.system_Double;
                case GONetSyncableValueTypes.System_Int16: return left.system_Int16 == right.system_Int16;
                case GONetSyncableValueTypes.System_Int32: return left.system_Int32 == right.system_Int32;
                case GONetSyncableValueTypes.System_Int64: return left.system_Int64 == right.system_Int64;
                case GONetSyncableValueTypes.System_SByte: return left.system_SByte == right.system_SByte;
                case GONetSyncableValueTypes.System_Single: return left.system_Single == right.system_Single;
                case GONetSyncableValueTypes.System_UInt16: return left.system_UInt16 == right.system_UInt16;
                case GONetSyncableValueTypes.System_UInt32: return left.system_UInt32 == right.system_UInt32;
                case GONetSyncableValueTypes.System_UInt64: return left.system_UInt64 == right.system_UInt64;
                case GONetSyncableValueTypes.UnityEngine_Vector2: return left.unityEngine_Vector2 == right.unityEngine_Vector2;
                case GONetSyncableValueTypes.UnityEngine_Vector3: return left.unityEngine_Vector3 == right.unityEngine_Vector3;
                case GONetSyncableValueTypes.UnityEngine_Vector4: return left.unityEngine_Vector4 == right.unityEngine_Vector4;
                case GONetSyncableValueTypes.UnityEngine_Quaternion:
                    // Optimized quaternion comparison using dot product
                    float dot = Quaternion.Dot(left.unityEngine_Quaternion, right.unityEngine_Quaternion);
                    return dot > Utils.QuaternionUtils.DOT_PRODUCT_SMALL_ROTATION_THRESHOLD; // ~0.026 degree threshold (see QuaternionUtils for history)
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(GONetSyncableValue left, GONetSyncableValue right)
        {
            return !(left == right);
        }

        #endregion

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

        public override bool Equals(object obj)
        {
            return obj is GONetSyncableValue other && this == other;
        }

        public override int GetHashCode()
        {
            switch (GONetSyncType)
            {
                case GONetSyncableValueTypes.System_Boolean: return HashCode.Combine(GONetSyncType, system_Boolean);
                case GONetSyncableValueTypes.System_Byte: return HashCode.Combine(GONetSyncType, system_Byte);
                case GONetSyncableValueTypes.System_Double: return HashCode.Combine(GONetSyncType, system_Double);
                case GONetSyncableValueTypes.System_Int16: return HashCode.Combine(GONetSyncType, system_Int16);
                case GONetSyncableValueTypes.System_Int32: return HashCode.Combine(GONetSyncType, system_Int32);
                case GONetSyncableValueTypes.System_Int64: return HashCode.Combine(GONetSyncType, system_Int64);
                case GONetSyncableValueTypes.System_SByte: return HashCode.Combine(GONetSyncType, system_SByte);
                case GONetSyncableValueTypes.System_Single: return HashCode.Combine(GONetSyncType, system_Single);
                case GONetSyncableValueTypes.System_UInt16: return HashCode.Combine(GONetSyncType, system_UInt16);
                case GONetSyncableValueTypes.System_UInt32: return HashCode.Combine(GONetSyncType, system_UInt32);
                case GONetSyncableValueTypes.System_UInt64: return HashCode.Combine(GONetSyncType, system_UInt64);
                case GONetSyncableValueTypes.UnityEngine_Vector2: return HashCode.Combine(GONetSyncType, unityEngine_Vector2);
                case GONetSyncableValueTypes.UnityEngine_Vector3: return HashCode.Combine(GONetSyncType, unityEngine_Vector3);
                case GONetSyncableValueTypes.UnityEngine_Vector4: return HashCode.Combine(GONetSyncType, unityEngine_Vector4);
                case GONetSyncableValueTypes.UnityEngine_Quaternion: return HashCode.Combine(GONetSyncType, unityEngine_Quaternion);
                default: return GONetSyncType.GetHashCode();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
