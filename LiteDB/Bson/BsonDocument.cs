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
    /// Represent a document schemeless to use in collections. Based on Dictionary<string, object>
    /// </summary>
    public class BsonDocument : BsonValue
    {
        public const int MAX_DOCUMENT_SIZE = 256 * 124;

        public BsonDocument()
            : base(new Dictionary<string, object>())
        {
        }

        public BsonDocument(Dictionary<string, object> value)
            : base(value)
        {
        }

        public BsonDocument(object anonymousObject)
            : this()
        {
            this.Append(anonymousObject);
        }

        public T ConvertTo<T>()
            where T : new()
        {
            return BsonSerializer.Deserialize<T>(BsonSerializer.Serialize(this));
        }

        public static BsonDocument ConvertFrom(object value)
        {
            return BsonSerializer.Deserialize<BsonDocument>(BsonSerializer.Serialize(value));
        }
    }
}
