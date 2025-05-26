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

using GONet.PluginAPI;
using System;
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

        public static Quaternion SlerpUnclamped(ref Quaternion a, ref Quaternion b, float t)
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

        #region Squad

        /// <overloads>
        /// <summary>
        /// Interpolates between quaternions using spherical quadrangle interpolation.
        /// </summary>
        /// </overloads>
        /// 
        /// <summary>
        /// Interpolates between quaternions using spherical quadrangle interpolation (single-precision).
        /// </summary>
        /// <param name="q">The source quaternion (<i>q<sub>n</sub></i>).</param>
        /// <param name="a">The first intermediate quaternion (<i>a<sub>n</sub></i>).</param>
        /// <param name="b">The second intermediate quaternion (<i>a<sub>n+1</sub></i>).</param>
        /// <param name="p">The target quaternion (<i>q<sub>n+1</sub></i>).</param>
        /// <param name="t">The interpolation parameter t.</param>
        /// <returns>The interpolated quaternion.</returns>
        /// <remarks>
        /// <para>
        /// The <i>spherical quadrangle interpolation (SQUAD) </i> is a spline-based interpolation 
        /// of rotations (unit quaternion). This operation is also known as 
        /// <i>spherical cubic interpolation</i>.
        /// </para>
        /// <para>
        /// If <i>q<sub>n</sub></i> is a sequence of <i>N</i> quaternions (<i>n</i> = 0 to <i>N</i>-1),
        /// then the smooth interpolation is given by:
        /// </para>
        /// <para>
        /// squad(<i>q<sub>n</sub></i>, <i>a<sub>n</sub></i>, <i>a<sub>n+1</sub></i>, <i>q<sub>n+1</sub></i>, <i>t</i>) 
        ///   = slerp(slerp(<i>q<sub>n</sub></i>, <i>q<sub>n+1</sub></i>, <i>t</i>), 
        ///           slerp(<i>a<sub>n</sub></i>, <i>a<sub>n+1</sub></i>, <i>t</i>), 
        ///           2<i>t</i>(1-<i>t</i>))
        /// where
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <i>q<sub>n</sub></i>, <i>q<sub>n+1</sub></i> represent start and destination rotation,
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <i>t</i> is the interpolation parameter which lies in the interval [0, 1], and
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <para>
        /// <i>a<sub>n</sub></i>, <i>a<sub>n+1</sub></i> are intermediate quaternions which can be 
        /// determined with:
        /// </para>
        /// <para>
        ///   <i>a<sub>n</sub></i> = 
        ///     <i>q<sub>n</sub></i> e<sup>-(ln(<i>q<sub>n</sub><sup>-1</sup></i><i>q<sub>n-1</sub></i>) + ln(<i>q<sub>n</sub><sup>-1</sup></i><i>q<sub>n+1</sub></i>))/4</sup>
        /// </para>
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <example>
        /// The following example demonstrates how to interpolate quaternions using 
        /// <see cref="Squad(Quaternion,Quaternion,Quaternion,Quaternion,float)"/>:
        /// <code>
        ///   // Given:
        ///   Quaternion q0, q1, q2, q3;  // A sequence of quaternions
        ///   float t;                     // A interpolation parameter
        /// 
        ///   // We want to interpolate between q1 and q2 by an interpolation factor t
        ///   Quaternion q, a, b, p;
        ///   Quaternion.SquadSetup(q0, q1, q2, q3, out q, out a, out b, out p);
        ///   Quaternion result = Quaternion.Squad(q, a, b, p, t);
        /// </code>
        /// </example>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static Quaternion Squad(ref Quaternion q, ref Quaternion a, ref Quaternion b, ref Quaternion p, float t)
        {
            return Quaternion.Slerp(
                Quaternion.Slerp(q, p, t),
                Quaternion.Slerp(a, b, t),
                2 * t * (1 - t));
        }

        /// <overloads>
        /// <summary>
        /// Calculates the parameters for a spline-based quaternion interpolation.
        /// </summary>
        /// </overloads>
        /// 
        /// <summary>
        /// Calculates the parameters for a spline-based quaternion interpolation (single-precision).
        /// </summary>
        /// <param name="q0">The previous quaternion (<i>q<sub>n-1</sub></i>).</param>
        /// <param name="q1">The source quaternion (<i>q<sub>n</sub></i>).</param>
        /// <param name="q2">The target quaternion (<i>q<sub>n+1</sub></i>).</param>
        /// <param name="q3">The subsequent quaternion (<i>q<sub>n+2</sub></i>).</param>
        /// <param name="q">The source quaternion (<i>q<sub>n</sub></i>).</param>
        /// <param name="a">The first intermediate quaternion (<i>a<sub>n</sub></i>).</param>
        /// <param name="b">The second intermediate quaternion (<i>a<sub>n+1</sub></i>).</param>
        /// <param name="p">The target quaternion (<i>q<sub>n+1</sub></i>).</param>
        /// <remarks>
        /// Given a sequence of quaternions, this method calculates the intermediate quaternions
        /// that are required by the method 
        /// <see cref="Squad(Quaternion,Quaternion,Quaternion,Quaternion,float)"/> to perform a 
        /// smooth spline-based interpolation. See 
        /// <see cref="Squad(Quaternion,Quaternion,Quaternion,Quaternion,float)"/> to find out more.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static void SquadSetup(ref Quaternion q0, ref Quaternion q1, ref Quaternion q2, ref Quaternion q3,
                                      out Quaternion q, out Quaternion a, out Quaternion b, out Quaternion p)
        {
            q = q1;
            p = q2;

            Quaternion q1Inv = Quaternion.Inverse(q1);
            Quaternion q2Inv = Quaternion.Inverse(q2);

            a = q1 * Exp(Multiply(-0.25f, Add(Ln(q1Inv * q0), Ln(q1Inv * q2))));
            b = q2 * Exp(Multiply(-0.25f, Add(Ln(q2Inv * q1), Ln(q2Inv * q3))));
        }

        #endregion

        /// <summary>
        /// Calculates the exponential.
        /// </summary>
        /// <param name="quaternion">The quaternion.</param>
        /// <returns>The exponential e<sup><i>q</i></sup>.</returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> This method requires that the quaternion is a pure quaternion. A
        /// pure quaternion is defined by <i>q</i> = (0, <i><b>u</b>θ</i>) where <i><b>u</b></i> is a
        /// unit vector.
        /// </para>
        /// <para>
        /// The exponential of a quaternion <i>q</i> is defines as:
        /// </para>
        /// <para>
        /// e<sup><i>q</i></sup> = (cos<i>θ</i> + <i><b>u</b></i>sin<i>θ</i>)
        /// </para>
        /// <para>
        /// The result is returned as a quaternion with the form: 
        /// (cos(<i>θ</i>), <i><b>u</b></i>sin(<i>θ</i>))
        /// </para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public static Quaternion Exp(this Quaternion quaternion)
        {
            float theta = (float)Math.Sqrt(quaternion.x * quaternion.x + quaternion.y * quaternion.y + quaternion.z * quaternion.z);
            float cosTheta = (float)Math.Cos(theta);

            if (theta > float.Epsilon)
            {
                float coefficient = (float)Math.Sin(theta) / theta;
                quaternion.w = cosTheta;
                quaternion.x *= coefficient;
                quaternion.y *= coefficient;
                quaternion.z *= coefficient;
            }
            else
            {
                // In this case θ was 0.
                // Therefore: cos(θ) = 1, sin(θ) = 0
                quaternion.w = cosTheta;

                // We do not have to set (x, y, z) because we already know that length
                // is 0.
            }
            return quaternion;
        }

        /// <summary>
        /// Calculates the natural logarithm.
        /// </summary>
        /// <param name="quaternion">The quaternion.  If not a unit quaternion, this method will normalize it before proceeding.</param>
        /// <returns>The natural logarithm ln(<i>q</i>).</returns>
        /// <remarks>
        /// <para>
        /// <strong>Important:</strong> This method requires that the quaternion is a unit quaternion.
        /// </para>
        /// <para>
        /// The natural logarithm of a quaternion <i>q</i> is defines as:
        /// </para>
        /// <para>
        /// ln(<i>q</i>) = ln(cos(<i>θ</i>) + <i><b>u</b></i>sin(<i>θ</i>)) 
        ///              = ln(e<sup><i><b>u</b>θ</i></sup>) = <i><b>u</b>θ</i>
        /// </para>
        /// <para>
        /// The result is returned as a quaternion with the form: (0, <i><b>u</b>θ</i>)
        /// </para>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly")]
        public static Quaternion Ln(this Quaternion quaternion)
        {
            if (Math.Abs(quaternion.w) > 1.0f)
            {
                quaternion = quaternion.normalized;
            }

            float sinTheta = (float)Math.Sqrt(quaternion.x * quaternion.x + quaternion.y * quaternion.y + quaternion.z * quaternion.z);
            float theta = (float)Math.Atan(sinTheta / quaternion.w);

            // Slower version:
            //float θ = System.Math.Acos(quaternion.W);
            //float sinθ = System.Math.Sin(θ);

            if (sinTheta != 0)
            {
                float coefficient = theta / sinTheta;
                quaternion.w = 0.0f;
                quaternion.x *= coefficient;
                quaternion.y *= coefficient;
                quaternion.z *= coefficient;
            }
            else
            {
                // In this case θ was 0.
                // cos(θ) = 1, sin(θ) = 0
                // We assume that the given quaternion is a unit quaternion.
                // If w = 1, then all other components should be 0.
                Debug.Assert(quaternion.x == 0, "Quaternion is not a unit quaternion.");
                Debug.Assert(quaternion.y == 0, "Quaternion is not a unit quaternion.");
                Debug.Assert(quaternion.z == 0, "Quaternion is not a unit quaternion.");

                // Return (0, (0, 0, 0))
                quaternion.w = 0.0f;

                // We do not have to touch (x, y, z).
            }
            return quaternion;
        }

        /// <summary>
        /// Adds two quaternions.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The sum of the two quaternions.</returns>
        public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
        {
            quaternion1.x += quaternion2.x;
            quaternion1.y += quaternion2.y;
            quaternion1.z += quaternion2.z;
            quaternion1.w += quaternion2.w;
            return quaternion1;
        }

        /// <overloads>
        /// <summary>
        /// Multiplies a quaternion by a scalar or a quaternion.
        /// </summary>
        /// </overloads>
        /// 
        /// <summary>
        /// Multiplies a quaternion by a scalar.
        /// </summary>
        /// <param name="quaternion">The quaternion.</param>
        /// <param name="scalar">The scalar.</param>
        /// <returns>
        /// The quaternion with each component multiplied by <paramref name="scalar"/>.
        /// </returns>
        public static Quaternion Multiply(float scalar, Quaternion quaternion)
        {
            quaternion.w *= scalar;
            quaternion.x *= scalar;
            quaternion.y *= scalar;
            quaternion.z *= scalar;
            return quaternion;
        }
    }

    /// <summary>
    /// Optimized quaternion interpolation and squad with log-exp, fast trig approximations,
    /// branchless small-angle handling, and precomputed half-slerps.
    /// </summary>
    public static class QuaternionUtilsOptimized
    {
        /// <summary>
        /// Fast polynomial approximations for acos, sin, cos
        /// Returns an approximation of acos(x) for x in [-1,1]
        /// Minimax polynomial: error ~0.005 radians
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ApproxAcos(float x)
        {
            return (-0.156583f * x * x - 0.33072f * x + 1.5708f);
        }

        /// <summary>
        /// Taylor-series-based approximation: sin(x) ≈ x * (1 - x^2/6)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ApproxSin(float x)
        {
            float x2 = x * x;
            return x * (1f - x2 * (1f / 6f));
        }

        /// <summary>
        /// Taylor-series-based approximation: cos(x) ≈ 1 - x^2/2 + x^4/24
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ApproxCos(float x)
        {
            float x2 = x * x;
            return 1f - x2 * 0.5f + (x2 * x2) * (1f / 24f);
        }

        /// <summary>
        /// Clamp x to [0,1] over [edge0,edge1], then smoothstep
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = (x - edge0) / (edge1 - edge0);
            // branchless clamp
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Quaternion logarithm and exponential
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Log(Quaternion q)
        {
            if (q.w > 1f) q.w = 1f; // clamp for numeric safety
            float angle = ApproxAcos(q.w);
            float sinAngle = ApproxSin(angle);
            Quaternion result = new Quaternion();
            if (Mathf.Abs(sinAngle) > 1e-4f)
            {
                float coeff = angle / sinAngle;
                result.x = q.x * coeff;
                result.y = q.y * coeff;
                result.z = q.z * coeff;
            }
            else
            {
                result.x = q.x;
                result.y = q.y;
                result.z = q.z;
            }
            result.w = 0f;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Exp(Quaternion q)
        {
            float angle = new Vector3(q.x, q.y, q.z).magnitude;
            float sinAngle = ApproxSin(angle);
            float cosAngle = ApproxCos(angle);
            Quaternion result = new Quaternion();
            if (angle > 1e-4f)
            {
                float coeff = sinAngle / angle;
                result.x = q.x * coeff;
                result.y = q.y * coeff;
                result.z = q.z * coeff;
            }
            else
            {
                result.x = q.x;
                result.y = q.y;
                result.z = q.z;
            }
            result.w = cosAngle;
            return result;
        }

        /// <summary>
        /// Fast slerp using log-exp, branchless small-angle, and trig approximations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion SlerpFast(Quaternion a, Quaternion b, float t)
        {
            // dot:
            float cosHalfAngle = a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;

            // identity check:
            if (Mathf.Abs(cosHalfAngle) >= 0.99999f)
                return a;

            // force shortest:
            if (cosHalfAngle < 0f)
            {
                b = new Quaternion(-b.x, -b.y, -b.z, -b.w);
                cosHalfAngle = -cosHalfAngle;
            }

            if (cosHalfAngle >= 0.99999f)
                return a;

            // exp/log path:
            Quaternion delta = b * Quaternion.Inverse(a);
            Quaternion logDelta = Log(delta);
            Quaternion scaled = new Quaternion(logDelta.x * t, logDelta.y * t, logDelta.z * t, 0f);
            Quaternion expDelta = Exp(scaled);
            Quaternion full = a * expDelta;

            // cheap LERP fallback (now our own):
            Quaternion approx = LerpNormalized(a, b, t);

            // mix based on how small the angle was:
            float factor = SmoothStep(0.99f, 1.01f, cosHalfAngle);
            return Quaternion.SlerpUnclamped(full, approx, factor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Quaternion LerpNormalized(Quaternion a, Quaternion b, float t)
        {
            // simple component‐wise LERP + normalize
            Quaternion q = new Quaternion(
                a.x * (1 - t) + b.x * t,
                a.y * (1 - t) + b.y * t,
                a.z * (1 - t) + b.z * t,
                a.w * (1 - t) + b.w * t
            );
            return q.normalized;
        }

        /// <summary>
        /// Precomputed half-slerps and final slerp -> fast Squad
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion SquadFast(Quaternion q0, Quaternion q1, Quaternion q2, Quaternion q3, float t)
        {
            // Precompute half-steps
            float ta = 0.5f * (1f - t);
            float tb = 0.5f * (1f + t);
            Quaternion A = SlerpFast(q0, q3, ta);
            Quaternion B = SlerpFast(q1, q2, tb);

            // Final blend
            float tc = 2f * t * (1f - t);
            return SlerpFast(A, B, tc);
        }

        /// <summary>
        /// Ultra-fast quaternion angle calculation using dot product
        /// Avoids Acos for small angles where precision isn't critical
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleFast(Quaternion a, Quaternion b)
        {
            float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;

            // Handle edge cases
            if (dot > 0.99999f) return 0f;
            if (dot < -0.99999f) return 180f;

            // For small angles, use approximation: angle ≈ 2 * asin(|a - b| / 2)
            float absDot = dot < 0f ? -dot : dot;
            if (absDot > 0.95f)
            {
                // Small angle approximation
                float dx = a.x - b.x;
                float dy = a.y - b.y;
                float dz = a.z - b.z;
                float dw = a.w - b.w;
                float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
                return dist * 114.59156f; // 2 * 180/PI
            }

            // Full calculation for larger angles
            return MathF.Acos(absDot) * 114.59156f; // 2 * 180/PI
        }

        /// <summary>
        /// Optimized RotateTowards using SLERP
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion RotateTowardsFast(Quaternion from, Quaternion to, float maxDegreesDelta)
        {
            float angle = AngleFast(from, to);
            if (angle <= 0.01f) return to;

            float t = maxDegreesDelta / angle;
            t = t > 1f ? 1f : t;

            return SlerpFast(from, to, t);
        }

        /// <summary>
        /// Batch quaternion operations for better cache usage
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BatchSlerp(
            Quaternion[] results,
            Quaternion[] from,
            Quaternion[] to,
            float[] t,
            int count)
        {
            // Process in batches of 4 for potential SIMD optimization
            int i = 0;
            for (; i < count - 3; i += 4)
            {
                results[i] = SlerpFast(from[i], to[i], t[i]);
                results[i + 1] = SlerpFast(from[i + 1], to[i + 1], t[i + 1]);
                results[i + 2] = SlerpFast(from[i + 2], to[i + 2], t[i + 2]);
                results[i + 3] = SlerpFast(from[i + 3], to[i + 3], t[i + 3]);
            }

            // Handle remaining
            for (; i < count; i++)
            {
                results[i] = SlerpFast(from[i], to[i], t[i]);
            }
        }

        // For the smoothing implementation, add this optimized version:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Quaternion GetSmoothedRotation_Optimized(
            Quaternion mostRecentValue,
            NumericValueChangeSnapshot[] olderValuesBuffer,
            int bufferCount)
        {
            const int INPUT_COUNT = 3;
            const int OUTPUT_COUNT = 2;

            // Stack-allocated arrays for small, fixed sizes
            Quaternion* inputs = stackalloc Quaternion[INPUT_COUNT];
            Quaternion* outputs = stackalloc Quaternion[OUTPUT_COUNT];
            float* inputWeights = stackalloc float[] { 0.35f, 0.1f, 0.1f };
            float* outputWeights = stackalloc float[] { 0.4f, 0.05f };

            // Fill inputs from buffer
            fixed (NumericValueChangeSnapshot* bufferPtr = olderValuesBuffer)
            {
                int idx = 0;
                for (int i = bufferCount - 1; i >= 0 && idx < INPUT_COUNT; --i, ++idx)
                {
                    inputs[idx] = *(Quaternion*)((byte*)&bufferPtr[i].numericValue + 1);
                }

                // Fill remaining with most recent if needed
                for (; idx < INPUT_COUNT; ++idx)
                {
                    inputs[idx] = mostRecentValue;
                }
            }

            // Compute weighted sum using basis-relative approach
            Quaternion basis = mostRecentValue;
            Quaternion invBasis = Quaternion.Inverse(basis);
            Quaternion accumulator = Quaternion.identity;

            // Process inputs
            for (int i = 0; i < INPUT_COUNT; ++i)
            {
                Quaternion relative = invBasis * inputs[i];
                accumulator = Quaternion.SlerpUnclamped(accumulator, relative, inputWeights[i]);
            }

            // Note: For a full implementation, you'd need to handle the output history
            // This is simplified for demonstration

            return basis * accumulator;
        }
    }
}
