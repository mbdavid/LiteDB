using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Class to call method for convert BsonDocument to/from byte[] - based on http://bsonspec.org/spec.html
    /// </summary>
    public class BsonSerializer
    {
        public static byte[] Serialize(BsonDocument value)
        {
            if (value == null) throw new ArgumentNullException("value");

            using (var mem = new MemoryStream())
            {
                var writer = new BsonWriter();
                writer.Serialize(mem, value);

                return mem.ToArray();
            }
        }

        public static BsonDocument Deserialize(byte[] bson)
        {
            if (bson == null || bson.Length == 0) throw new ArgumentNullException("bson");

            using (var mem = new MemoryStream(bson))
            {
                var reader = new BsonReader();

                return reader.Deserialize(mem);
            }
        }
    }
}
