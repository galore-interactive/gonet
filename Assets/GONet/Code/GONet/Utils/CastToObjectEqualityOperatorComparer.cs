/* Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System.Collections.Generic;

namespace GONet.Utils
{
    /// <summary>
    /// This is almost solely available to use to ensure Unity3D object == operator is NOT used (i.e., crossing the managed-native barrier) just to compare reference equality.
    /// Hence, the cast to <see cref="object"/>.
    /// </summary>
    public class CastToObjectEqualityOperatorComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            return (object)x == (object)y;
        }

        public int GetHashCode(T obj)
        {
            return (object)obj == null ? 0 : obj.GetHashCode();
        }
    }
}
