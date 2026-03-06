using System;
using System.Linq;

namespace Lxdn.Core.Basics
{
    public class Hash
    {
        public static readonly Func<int, int, int> Combine = (hash1, hash2)
            => unchecked((hash1 * 16777619) ^ hash2); // inspired by https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode

        public static int Of(params object[] objects)
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            if (objects.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(objects), "Zero count of arguments");

            if (objects.Length == 1) 
            {
                if (objects[0] is null)
                    throw new ArgumentException("The single argument cannot be null", nameof(objects));

                return objects[0].GetHashCode();
            }
            // or conditionally use HashCode.Combine
            return objects.Select(x => x?.GetHashCode() ?? 0).Aggregate(Combine);
        }
    }
}
