using System.Collections.Generic;

namespace LiteDB
{
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Get free index based on dictionary count number (if is in use, move to next number)
        /// </summary>
        public static ushort NextIndex<T>(this Dictionary<ushort, T> dict)
        {
            if (dict.Count == 0) return 0;

            var next = (ushort)dict.Count;

            while (dict.ContainsKey(next))
            {
                next++;
            }

            return next;
        }

        public static T GetOrDefault<K, T>(this IDictionary<K, T> dict, K key, T defaultValue = default(T))
        {
            if (dict.TryGetValue(key, out T result))
            {
                return result;
            }

            return defaultValue;
        }
    }
}