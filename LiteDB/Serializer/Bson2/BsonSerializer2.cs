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
        public static byte[] Serialize(BsonDocument doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            var writer = new BsonWriter2();

            return writer.Serialize(doc);
        }

        public static BsonDocument Deserialize(byte[] bson)
        {
            if (bson == null || bson.Length == 0) throw new ArgumentNullException("bson");

            var reader = new BsonReader2();

            return reader.Deserialize(bson);
        }
    }
}
