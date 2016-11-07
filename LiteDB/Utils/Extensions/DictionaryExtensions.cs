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
            var inClosure = false;
            var inValue = false;
            var key = string.Empty;
            var value = string.Empty;

            for (var i = 0; i < connectionString.Length; i++)
            {
                if (inValue)
                {
                    if (inClosure)
                    {
                        if (connectionString[i] == '"' && !(i > 0 && connectionString[i - 1] == '\\'))
                        {
                            inClosure = false;
                        }
                        else
                        {
                            value += connectionString[i];
                        }
                    }
                    else
                    {
                        if (connectionString[i] == '"' && !(i > 0 && connectionString[i - 1] == '\\'))
                        {
                            inClosure = true;
                        }
                        else if (connectionString[i] == ';')
                        {
                            dict.Add(key.Trim(), value);
                            key = string.Empty;
                            value = string.Empty;
                            inValue = false;
                        }
                        else
                        {
                            value += connectionString[i];
                        }
                    }
                }
                else
                {
                    if (connectionString[i] == '=')
                    {
                        inValue = true;
                    }
                    else if (connectionString[i] == ';')
                    {
                        dict.Add(key.Trim(), string.Empty);
                        key = string.Empty;
                    }
                    else
                    {
                        key += connectionString[i];
                    }
                }
            }

            if (!string.IsNullOrEmpty(key.Trim()))
            {
                dict.Add(key.Trim(), value);
            }
        }
    }
}