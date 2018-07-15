using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns minimal/first value from expression
        /// </summary>
        public BsonValue Min(string collection, BsonExpression expression)
        {
            return this.Query(collection)
                .OrderBy(expression, LiteDB.Query.Ascending)
                .Select(expression)
                .Limit(1)
                .ToValues()
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns max/last value from expression
        /// </summary>
        public BsonValue Max(string collection, BsonExpression expression)
        {
            return this.Query(collection)
                .OrderBy(expression, LiteDB.Query.Descending)
                .Select(expression)
                .Limit(1)
                .ToValues()
                .FirstOrDefault();
        }

        /// <summary>
        /// Get collection counter
        /// </summary>
        public long Count(string collection)
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
        public int Count(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Select("_id")
                .Count();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long LongCount(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
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
        public bool Exists(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Select("_id")
                .Exists();
        }

        /// <summary>
        /// Apply aggregation expression over collection. eg: db.Aggregate("col", "SUM(total)")
        /// </summary>
        public BsonValue Aggregate(string collection, BsonExpression select)
        {
            return this.Query(collection)
                .Aggregate(select);
        }

        /// <summary>
        /// Apply aggregation expression over collection. eg: db.Aggregate("col", "SUM(total)", "_id > 10")
        /// </summary>
        public BsonValue Aggregate(string collection, BsonExpression select, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Aggregate(select);
        }
    }
}