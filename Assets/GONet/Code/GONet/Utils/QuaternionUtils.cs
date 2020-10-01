/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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

using System.Runtime.CompilerServices;
using UnityEngine;

namespace GONet.Utils
{
    public static class QuaternionUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /// <summary>
        /// Gets the square of the quaternion length (magnitude).
        /// </summary>
        public static float LengthSquared(this ref Quaternion a)
        {
            return a.x * a.x + a.y * a.y + a.z * a.z + a.w * a.w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 xyz(this ref Quaternion a)
        {
            return new Vector3(a.x, a.y, a.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void xyz(this ref Quaternion a, ref Vector3 xyz)
        {
            a.x = xyz.x;
            a.y = xyz.y;
            a.z = xyz.z;
        }

        public static GONetSyncableValue SlerpUnclamped(ref Quaternion a, ref Quaternion b, float t)
        {
            // if either input is zero, return the other.
            if (a.LengthSquared() == 0.0f)
            {
                if (b.LengthSquared() == 0.0f)
                {
                    return Quaternion.identity;
                }
                return b;
            }
            else if (b.LengthSquared() == 0.0f)
            {
                return a;
            }


            float cosHalfAngle = a.w * b.w + Vector3.Dot(a.xyz(), b.xyz());

            if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f)
            {
                // angle = 0.0f, so just return one input.
                return a;
            }
            else if (cosHalfAngle < 0.0f)
            {
                Vector3 negBxyz = -b.xyz();
                b.xyz(ref negBxyz);
                b.w = -b.w;
                cosHalfAngle = -cosHalfAngle;
            }

            float blendA;
            float blendB;
            if (cosHalfAngle < 0.99f)
            {
                // do proper slerp for big angles
                float halfAngle = (float)System.Math.Acos(cosHalfAngle);
                float sinHalfAngle = (float)System.Math.Sin(halfAngle);
                float oneOverSinHalfAngle = 1.0f / sinHalfAngle;
                blendA = (float)System.Math.Sin(halfAngle * (1.0f - t)) * oneOverSinHalfAngle;
                blendB = (float)System.Math.Sin(halfAngle * t) * oneOverSinHalfAngle;
            }
            else
            {
                // do lerp if angle is really small.
                blendA = 1.0f - t;
                blendB = t;
            }

            Vector3 resultXYZ = blendA * a.xyz() + blendB * b.xyz();
            Quaternion result = new Quaternion(resultXYZ.x, resultXYZ.y, resultXYZ.z, blendA * a.w + blendB * b.w);

            if (result.LengthSquared() > 0.0f)
            {
                return result.normalized;
            }
            else
            {
                return Quaternion.identity;
            }
        }
    }
}
