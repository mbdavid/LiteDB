using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Static class for serialize/deserialize BsonDocuments into JsonEx (extended json format)
    /// </summary>
    public class JsonEx
    {
        /// <summary>
        /// Serialize a BsonDocument (or any BsonValue) into a JsonEx string
        /// </summary>
        public static string Serialize(BsonValue value, bool pretty = false, bool showbinary = true)
        {
            var writer = new JsonWriter(pretty, showbinary);

            return writer.Serialize(value);
        }

        /// <summary>
        /// Convert a JsonEx string into a BsonValue
        /// </summary>
        public static BsonValue Deserialize(string json)
        {
            var reader = new JsonReader();

            return reader.Deserialize(json);
        }

        /// <summary>
        /// Convert a JsonEx string into a BsonValue based type (BsonObject, BsonArray or BsonDocument)
        /// </summary>
        public static T Deserialize<T>(string json)
            where T : BsonValue
        {
            var value = Deserialize(json);

            if (typeof(T) == typeof(BsonDocument))
            {
                return (T)(object)new BsonDocument(value);
            }
            else if (typeof(T) == typeof(BsonObject))
            {
                return (T)(object)value.AsObject;
            }
            else if (typeof(T) == typeof(BsonArray))
            {
                return (T)(object)value.AsArray;
            }
            else
            {
                throw new ArgumentException("Unknow T BsonValue type");
            }
        }
    }
}
