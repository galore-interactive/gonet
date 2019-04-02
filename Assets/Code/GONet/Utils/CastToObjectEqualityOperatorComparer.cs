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
