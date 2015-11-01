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
    public class DbRef<T>
        where T : new()
    {
        /// <summary>
        /// Used only for serialization/deserialize
        /// </summary>
        public DbRef()
        {
        }

        /// <summary>
        /// Initialize using reference collection name and collection Id
        /// </summary>
        public DbRef(string collection, BsonValue id)
        {
            if (string.IsNullOrEmpty(collection)) throw new ArgumentNullException("collection");
            if (id == null || id.IsNull || id.IsMinValue || id.IsMaxValue) throw new ArgumentNullException("id");

            this.Collection = collection;
            this.Id = id;
        }

        /// <summary>
        /// Initialize using reference collection name and collection Id
        /// </summary>
        public DbRef(LiteCollection<T> collection, BsonValue id)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (id == null || id.IsNull || id.IsMinValue || id.IsMaxValue) throw new ArgumentNullException("id");

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
