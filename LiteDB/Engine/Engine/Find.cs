using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
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
        /// Find first or default document based in collection using expression predicate
        /// </summary>
        public BsonDocument FindOne(string collection, BsonExpression predicate)
        {
            return this.Query(collection)
                .Where(predicate)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find first or default document based in collection using expression predicate
        /// </summary>
        public BsonDocument FindOne(string collection, string predicate, BsonDocument parameters)
        {
            return this.Query(collection)
                .Where(predicate, parameters)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find first or default document based in collection using expression predicate
        /// </summary>
        public BsonDocument FindOne(string collection, string predicate, params BsonValue[] args)
        {
            return this.Query(collection)
                .Where(predicate, args)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find first or default document based in _id field
        /// </summary>
        public BsonDocument FindById(string collection, BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            return this.Query(collection)
                .SingleById(id);
        }

        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        public IEnumerable<BsonDocument> FindAll(string collection)
        {
            return this.Query(collection)
                .ToEnumerable();
        }
    }
}