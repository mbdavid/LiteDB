using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns min value from _id key index
        /// </summary>
        public BsonValue Min(string collection)
        {
            return this.Query(collection)
                .Index(Index.All("_id", LiteDB.Query.Ascending))
                .Select("_id")
                .ExecuteScalar();
        }

        /// <summary>
        /// Returns min value from expression
        /// </summary>
        public BsonValue Min(string collection, BsonExpression keySelector)
        {
            return this.Query(collection)
                .OrderBy(keySelector, LiteDB.Query.Ascending)
                .Select(keySelector)
                .ExecuteScalar();
        }

        /// <summary>
        /// Returns max value from _id key index
        /// </summary>
        public BsonValue Max(string collection)
        {
            return this.Query(collection)
                .Index(Index.All("_id", LiteDB.Query.Descending))
                .Select("_id")
                .ExecuteScalar();
        }

        /// <summary>
        /// Returns max value from expression
        /// </summary>
        public BsonValue Max(string collection, BsonExpression keySelector)
        {
            return this.Query(collection)
                .OrderBy(keySelector, LiteDB.Query.Descending)
                .Select(keySelector)
                .ExecuteScalar();
        }

        /// <summary>
        /// Get collection counter
        /// </summary>
        public int Count(string collection)
        {
            return this.Query(collection)
                .Select("_id")
                .Count();
        }

        /// <summary>
        /// Get collection counter
        /// </summary>
        public long LongCount(string collection)
        {
            return this.Query(collection)
                .Select("_id")
                .LongCount();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public int Count(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select("_id")
                .Count();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long LongCount(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select("_id")
                .LongCount();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public int Count(string collection, BsonExpression predicate)
        {
            return this.Query(collection)
                .Where(predicate)
                .Select("_id")
                .Count();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long LongCount(string collection, BsonExpression predicate)
        {
            return this.Query(collection)
                .Where(predicate)
                .Select("_id")
                .LongCount();
        }

        /// <summary>
        /// Check if has at least one node in a query execution - do not deserialize documents to check
        /// </summary>
        public bool Exists(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select("_id")
                .Exists();
        }

        /// <summary>
        /// Check if has at least one node in a query execution - do not deserialize documents to check
        /// </summary>
        public bool Exists(string collection, BsonExpression predicate)
        {
            return this.Query(collection)
                .Where(predicate)
                .Select("_id")
                .Exists();
        }
    }
}