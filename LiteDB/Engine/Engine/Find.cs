using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Find documents in collection using Index filter only
        /// </summary>
        public IEnumerable<BsonDocument> Find(string collection, Index index, int offset = 0, int limit = int.MaxValue)
        {
            return this.Query(collection)
                .Index(index)
                .Offset(offset)
                .Limit(limit)
                .ToEnumerable();
        }

        /// <summary>
        /// Find documents in collection using expression as filter/index
        /// </summary>
        public IEnumerable<BsonDocument> Find(string collection, BsonExpression predicate, int offset = 0, int limit = int.MaxValue)
        {
            return this.Query(collection)
                .Where(predicate)
                .Offset(offset)
                .Limit(limit)
                .ToEnumerable();
        }

        /// <summary>
        /// Find first or default document based in collection based on Query filter
        /// </summary>
        public BsonDocument FindOne(string collection, BsonExpression predicate)
        {
            return this.Find(collection, predicate).FirstOrDefault();
        }

        /// <summary>
        /// Find first or default document based in _id field
        /// </summary>
        public BsonDocument FindById(string collection, BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            return this.Find(collection, Index.EQ("_id", id)).FirstOrDefault();
        }

        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        public IEnumerable<BsonDocument> FindAll(string collection)
        {
            return this.Find(collection, Index.All());
        }
    }
}