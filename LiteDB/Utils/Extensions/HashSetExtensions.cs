using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal static class HashSetExtensions
    {
        public static HashSet<T> AddRange<T>(this HashSet<T> hash, IEnumerable<T> items)
        {
            foreach(var item in items)
            {
                hash.Add(item);
            }

            return hash;
        }
    }
}