using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

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

        public static object Get(this Dictionary<string, object> dict, string name)
        {
            object res;
            if (dict.TryGetValue(name, out res))
                return res;
            
            return null;
        }

        public static BsonValue Get(this Dictionary<string, BsonValue> dict, string name)
        {
            BsonValue res;
            if (dict.TryGetValue(name, out res))
                return res;
            
            return BsonValue.Null;
        }
    }
}
