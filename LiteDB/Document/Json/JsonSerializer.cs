using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Static class for serialize/deserialize BsonDocuments into json extended format
    /// </summary>
    public class JsonSerializer
    {
        #region Serialize

        /// <summary>
        /// Json serialize a BsonValue into a String
        /// </summary>
        public static string Serialize(BsonValue value, bool pretty = false, bool writeBinary = true)
        {
            var sb = new StringBuilder();

            using (var w = new StringWriter(sb))
            {
                Serialize(value ?? BsonValue.Null, w, pretty, writeBinary);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Json serialize a BsonValue into a TextWriter
        /// </summary>
        public static void Serialize(BsonValue value, TextWriter writer, bool pretty = false, bool writeBinary = true)
        {
            var w = new JsonWriter(writer);
            w.Pretty = pretty;
            w.WriteBinary = writeBinary;
            w.Serialize(value ?? BsonValue.Null);
        }

        #endregion

        #region Deserialize

        /// <summary>
        /// Deserialize a Json string into a BsonValue
        /// </summary>
        public static BsonValue Deserialize(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using (var sr = new StringReader(json))
            {
                var reader = new JsonReader(sr);

                return reader.Deserialize();
            }
        }

        /// <summary>
        /// Deserialize a Json TextReader into a BsonValue
        /// </summary>
        public static BsonValue Deserialize(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var jr = new JsonReader(reader);

            return jr.Deserialize();
        }

        /// <summary>
        /// Deserialize a json using a StringScanner and returns BsonValue
        /// </summary>
        public static BsonValue Deserialize(StringScanner s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            if (s.HasTerminated) return BsonValue.Null;

            using (var sr = new StringReader(s.ToString()))
            {
                var reader = new JsonReader(sr);

                var value = reader.Deserialize();

                s.Seek((int)(reader.Position - 1));

                return value;
            }
        }

        /// <summary>
        /// Deserialize a json array as an IEnumerable of BsonValue
        /// </summary>
        public static IEnumerable<BsonValue> DeserializeArray(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            var sr = new StringReader(json);
            var reader = new JsonReader(sr);
            return reader.DeserializeArray();
        }

        /// <summary>
        /// Deserialize a json array as an IEnumerable of BsonValue reading on demand TextReader
        /// </summary>
        public static IEnumerable<BsonValue> DeserializeArray(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var jr = new JsonReader(reader);

            return jr.DeserializeArray();
        }

        #endregion
    }
}