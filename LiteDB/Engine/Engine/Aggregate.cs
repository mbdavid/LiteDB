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
            return (int)this.LongCount(collection);
        }

        /// <summary>
        /// Get collection counter
        /// </summary>
        public long LongCount(string collection)
        {
            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(SnapshotMode.Read, collection, false);

                return snapshot.CollectionPage?.DocumentCount ?? 0;
            });
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public int Count(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select(true)
                .Count();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long LongCount(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select(true)
                .LongCount();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public int Count(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Count();
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long LongCount(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .LongCount();
        }

        /// <summary>
        /// Check if has at least one node in a query execution - do not deserialize documents to check
        /// </summary>
        public bool Exists(string collection, Index index)
        {
            return this.Query(collection)
                .Index(index)
                .Select(true)
                .Exists();
        }

        /// <summary>
        /// Check if has at least one node in a query execution - do not deserialize documents to check
        /// </summary>
        public bool Exists(string collection, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Exists();
        }

        /// <summary>
        /// Apply aggregation expression over collection. eg: db.Aggregate("col", "SUM(total)")
        /// </summary>
        public BsonValue Aggregate(string collection, BsonExpression expr)
        {
            return this.Query(collection)
                .Select(expr)
                .Aggregate();
        }

        /// <summary>
        /// Apply aggregation expression over collection. eg: db.Aggregate("col", "SUM(total)", "_id > 10")
        /// </summary>
        public BsonValue Aggregate(string collection, BsonExpression expr, BsonExpression query)
        {
            return this.Query(collection)
                .Where(query)
                .Select(expr)
                .Aggregate();
        }
    }
}