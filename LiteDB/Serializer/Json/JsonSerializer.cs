using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Static class for serialize/deserialize BsonDocuments into json extended format
    /// </summary>
    public class JsonSerializer
    {
        /// <summary>
        /// Serialize a BsonObject into a json string
        /// </summary>
        public static string Serialize(BsonValue value, bool pretty = false, bool writeBinary = true)
        {
            var sb = new StringBuilder();

            using (var w = new StringWriter(sb))
            {
                var writer = new JsonWriter(w);
                writer.Pretty = pretty;
                writer.WriteBinary = writeBinary;
                writer.Serialize(value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert a Json string into a BsonValue
        /// </summary>
        public static BsonValue Deserialize(string json)
        {
            var reader = new JsonReader();

            return reader.Deserialize(json);
        }

        /// <summary>
        /// Deserialize a Json as an IEnumerable of BsonValue based class
        /// </summary>
        public static IEnumerable<BsonValue> DeserializeArray(string json)
        {
            var reader = new JsonReader();

            return reader.ReadEnumerable(json);
        }
    }
}
