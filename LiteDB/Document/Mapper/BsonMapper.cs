using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Class that converts POCO class to/from BsonDocument
    /// </summary>
    public class BsonMapper
    {
        public T ToObject<T>(BsonDocument doc)
            where T : new()
        {
            var type = typeof(T);

            // if T is BsonDocument, just return them
            if (type == typeof(BsonDocument)) return (T)(object)doc;


            return default(T);
        }

        public BsonDocument ToDocument(object obj)
        {
            // if object is BsonDocument, just return them
            if (obj is BsonDocument) return (BsonDocument)obj;


            return null;
        }
    }
}
