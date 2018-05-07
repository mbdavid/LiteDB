using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Get value from dictionary converting datatype T
        /// </summary>
        public static T GetValue<T>(this Dictionary<string, string> dict, string key, T defaultValue)
        {
            try
            {
                string value;

                if (dict.TryGetValue(key, out value) == false) return defaultValue;

                if (typeof(T) == typeof(TimeSpan))
                {
                    return (T)(object)TimeSpan.Parse(value);
                }
                else if (typeof(T).GetTypeInfo().IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), value, true);
                }
                else
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception)
            {
                throw new LiteException("Invalid connection string value type for [" + key + "]");
            }
        }

        /// <summary>
        /// Get a value from a key converted in file size format: "1gb", "10 mb", "80000"
        /// </summary>
        public static long GetFileSize(this Dictionary<string, string> dict, string key, long defaultValue)
        {
            var size = dict.GetValue<string>(key, null);

            if (size == null) return defaultValue;

            var match = Regex.Match(size, @"^(\d+)\s*([tgmk])?(b|byte|bytes)?$", RegexOptions.IgnoreCase);

            if (!match.Success) return 0;

            var num = Convert.ToInt64(match.Groups[1].Value);

            switch (match.Groups[2].Value.ToLower())
            {
                case "t": return num * 1024L * 1024L * 1024L * 1024L;
                case "g": return num * 1024L * 1024L * 1024L;
                case "m": return num * 1024L * 1024L;
                case "k": return num * 1024L;
                case "": return num;
            }

            return 0;
        }
    }
}