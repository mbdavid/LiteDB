using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Class with static methods for serialize/deserialize a object <=> byte[]
    /// </summary>
    public class BsonSerializer
    {
        static BsonSerializer()
        {
            fastBinaryJSON.BJSON.Parameters.UseExtensions = false;
            fastBinaryJSON.BJSON.Parameters.IgnoreAttributes.Add(typeof(BsonIgnoreAttribute));
        }

        public static byte[] Serialize(object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            if (obj is BsonDocument)
            {
                var doc = (BsonDocument)obj;

                return fastBinaryJSON.BJSON.ToBJSON(doc.RawValue);
            }
            else
            {
                return fastBinaryJSON.BJSON.ToBJSON(obj);
            }
        }

        /// <summary>
        /// Convert byte array in a object - use fastBinaryJson or IBinarySerializable if implemented
        /// </summary>
        public static T Deserialize<T>(byte[] data)
            where T : new()
        {
            if (data == null || data.Length == 0) throw new ArgumentNullException("data");

            if (typeof(T) == typeof(BsonDocument))
            {
                var dict = fastBinaryJSON.BJSON.Parse(data);

                object doc = new BsonDocument((Dictionary<string, object>)dict);

                return (T)doc;
            }
            else
            {
                return fastBinaryJSON.BJSON.ToObject<T>(data);
            }
        }

        /// <summary>
        /// Get a value of a field in a object
        /// </summary>
        public static object GetValueField(object obj, string fieldName)
        {
            if (obj.GetType() == typeof(BsonDocument))
            {
                var doc = (BsonDocument)obj;

                return doc[fieldName].RawValue;
            }
            else
            {
                var p = obj.GetType().GetProperty(fieldName);

                return p == null ? null : p.GetValue(obj, null);
            }
        }
    }
}
