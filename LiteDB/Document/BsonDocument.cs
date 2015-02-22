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
    /// Represent a document schemeless to use in collections.
    /// </summary>
    public class BsonDocument : BsonObject
    {
        public const int MAX_DOCUMENT_SIZE = 256 * BasePage.PAGE_AVAILABLE_BYTES; // limits in 1.044.224b max document size to avoid large documents, memory usage and slow performance

        public BsonDocument()
            : base()
        {
        }

        public BsonDocument(BsonObject value)
            : base(value.RawValue)
        {
            if (!this.HasKey("_id")) throw new ArgumentException("BsonDocument must have an _id key");
        }

        public BsonDocument(BsonObject value, BsonValue id)
            : base(value.RawValue)
        {
            this.Id = id;
        }

        public BsonValue Id 
        { 
            get { return this["_id"]; }
            set { this["_id"] = value; } 
        }
    }
}
