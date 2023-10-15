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
}
