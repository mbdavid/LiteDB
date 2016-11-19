using System;
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

        public static void ParseKeyValue(this IDictionary<string, string> dict, string connectionString)
        {
            var s = new StringScanner(connectionString);

            while(!s.HasTerminated)
            {
                var key = s.Scan(@"(.*?)=", 1).Trim();
                var value = "";
                s.Scan(@"\s*");

                if (s.Match("\""))
                {
                    // read a value inside an string " (remove escapes)
                    value = s.Scan(@"""((?:\\""|.)*?)""", 1).Replace("\\\"", "\"");
                    s.Scan(@"\s*;?\s*");
                }
                else
                {
                    // read value
                    value = s.Scan(@"(.*?);\s*", 1).Trim();

                    // read last part
                    if (value.Length == 0)
                    {
                        value = s.Scan(".*").Trim();
                    }
                }

                dict[key] = value;
            }
        }
    }
}