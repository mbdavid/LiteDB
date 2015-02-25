using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Creates a field that is a reference for another document from another collection. T is another type
    /// </summary>
    public class LiteDBRef<T>
        where T : new()
    {
        public LiteDBRef()
        {
        }

        /// <summary>
        /// Initialize using reference collection name and collection Id
        /// </summary>
        public LiteDBRef(string collection, BsonValue id)
        {
            if (string.IsNullOrEmpty(collection)) throw new ArgumentNullException("collection");
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            this.Collection = collection;
            this.Id = id ?? BsonValue.Null;
        }

        /// <summary>
        /// Initialize using reference collection name and collection Id
        /// </summary>
        public LiteDBRef(LiteCollection<T> collection, BsonValue id)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            this.Collection = collection.Name;
            this.Id = id;
        }

        [BsonField("$ref")]
        public string Collection { get; set; }

        [BsonField("$id")]
        public BsonValue Id { get; set; }

        [BsonIgnore]
        public T Item { get; private set; }

        /// <summary>
        /// Fetch document reference return them. After fetch, you can use "Item" proerty do get ref document
        /// </summary>
        public T Fetch(LiteDatabase db)
        {
            if (this.Item == null)
            {
                this.Item = db.GetCollection<T>(this.Collection).FindById(this.Id);
            }

            return this.Item;
        }
    }
}
