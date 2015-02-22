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
            this.Collection = collection;
            this.Id = id;
        }

        /// <summary>
        /// Initialize using reference collection name and collection Id
        /// </summary>
        public LiteDBRef(LiteCollection<T> collection, BsonValue id)
        {
            this.Collection = collection.Name;
            this.Id = id;
        }

        [BsonField("$ref")]
        public string Collection { get; set; }

        [BsonField("$id")]
        public BsonValue Id { get; set; }

        [BsonIgnore]
        public T Item { get; private set; }

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
