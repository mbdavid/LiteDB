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
    public class BsonSerializer2
    {
        public static byte[] Serialize(BsonDocument value)
        {
            if (value == null) throw new ArgumentNullException("value");

            var count = value.GetBytesCount(true);
            var writer = new ByteWriter(count);
            var bson = new BsonWriter2();

            bson.WriteDocument(writer, value);

            return writer.Buffer;
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
