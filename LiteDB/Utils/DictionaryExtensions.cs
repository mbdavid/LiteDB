using System.Collections.Generic;

namespace LiteDB
{
    internal static class DictionaryExtensions
    {
        public static ushort NextIndex<T>(this Dictionary<ushort, T> dict)
        {
            ushort next = 0;

            while (dict.ContainsKey(next))
            {
                next++;
            }

            return next;
        }

        public static T GetOrDefault<K, T>(this IDictionary<K, T> dict, K key, T defaultValue = default(T))
        {
            T result;

            if (dict.TryGetValue(key, out result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}