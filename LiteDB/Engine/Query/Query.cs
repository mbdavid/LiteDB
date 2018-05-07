using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create query using indexes in database. All methods are statics.
    /// Queries can be executed in 3 ways: Index Seek (fast), Index Scan (good), Full Scan (slow)
    /// </summary>
    public abstract class Query
    {
        public string Field { get; private set; }

        internal BsonExpression Expression { get; set; }
        internal virtual bool UseIndex { get; set; }
        internal virtual bool UseFilter { get; set; }

        internal Query(string field)
        {
            this.Field = field;
        }

        #region Static Methods

        /// <summary>
        /// Indicate when a query must execute in ascending order
        /// </summary>
        public const int Ascending = 1;

        /// <summary>
        /// Indicate when a query must execute in descending order
        /// </summary>
        public const int Descending = -1;

        /// <summary>
        /// Returns all documents using _id index order
        /// </summary>
        public static Query All(int order = Ascending)
        {
            return new QueryAll("_id", order);
        }

        /// <summary>
        /// Returns all documents using field index order
        /// </summary>
        public static Query All(string field, int order = Ascending)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryAll(field, order);
        }

        /// <summary>
        /// Returns all documents that value are equals to value (=)
        /// </summary>
        public static Query EQ(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryEquals(field, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that value are less than value (&lt;)
        /// </summary>
        public static Query LT(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryLess(field, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are less than or equals value (&lt;=)
        /// </summary>
        public static Query LTE(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryLess(field, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that value are greater than value (&gt;)
        /// </summary>
        public static Query GT(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryGreater(field, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are greater than or equals value (&gt;=)
        /// </summary>
        public static Query GTE(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryGreater(field, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that values are between "start" and "end" values (BETWEEN)
        /// </summary>
        public static Query Between(string field, BsonValue start, BsonValue end, bool startEquals = true, bool endEquals = true)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryBetween(field, start ?? BsonValue.Null, end ?? BsonValue.Null, startEquals, endEquals);
        }

        /// <summary>
        /// Returns all documents that starts with value (LIKE)
        /// </summary>
        public static Query StartsWith(string field, string value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (value.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(value));

            return new QueryStartsWith(field, value);
        }

        /// <summary>
        /// Returns all documents that contains value (CONTAINS)
        /// </summary>
        public static Query Contains(string field, string value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (value.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(value));

            return new QueryContains(field, value);
        }

        /// <summary>
        /// Returns all documents that are not equals to value (not equals)
        /// </summary>
        public static Query Not(string field, BsonValue value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            return new QueryNotEquals(field, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that in query result (not result)
        /// </summary>
        public static Query Not(Query query, int order = Query.Ascending)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            return new QueryNot(query, order);
        }

        /// <summary>
        /// Returns all documents that has value in values list (IN)
        /// </summary>
        public static Query In(string field, BsonArray value)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return new QueryIn(field, value.RawValue);
        }

        /// <summary>
        /// Returns all documents that has value in values list (IN)
        /// </summary>
        public static Query In(string field, params BsonValue[] values)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (values == null) throw new ArgumentNullException(nameof(values));

            return new QueryIn(field, values);
        }

        /// <summary>
        /// Returns all documents that has value in values list (IN)
        /// </summary>
        public static Query In(string field, IEnumerable<BsonValue> values)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (values == null) throw new ArgumentNullException(nameof(values));

            return new QueryIn(field, values);
        }

        /// <summary>
        /// Apply a predicate function in an index result. Execute full index scan but it's faster then runs over deserialized document.
        /// </summary>
        public static Query Where(string field, Func<BsonValue, bool> predicate, int order = Query.Ascending)
        {
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new QueryWhere(field, predicate, order);
        }

        /// <summary>
        /// Returns document that exists in BOTH queries results. If both queries has indexes, left query has index preference (other side will be run in full scan)
        /// </summary>
        public static Query And(Query left, Query right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            // test if can use QueryBetween because it's more efficient
            if (left is QueryGreater && right is QueryLess && left.Field == right.Field)
            {
                var l = left as QueryGreater;
                var r = right as QueryLess;

                return Between(l.Field, l.Value, r.Value, l.IsEquals, r.IsEquals);
            }

            return new QueryAnd(left, right);
        }

        /// <summary>
        /// Returns document that exists in ALL queries results.
        /// </summary>
        public static Query And(params Query[] queries)
        {
            if (queries == null || queries.Length < 2) throw new ArgumentException("At least two Query should be passed");

            var left = queries[0];

            for (int i = 1; i < queries.Length; i++)
            {
                left = And(left, queries[i]);
            }
            return left;
        }

        /// <summary>
        /// Returns documents that exists in ANY queries results (Union).
        /// </summary>
        public static Query Or(Query left, Query right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            return new QueryOr(left, right);
        }

        /// <summary>
        /// Returns document that exists in ANY queries results (Union).
        /// </summary>
        public static Query Or(params Query[] queries)
        {
            if (queries == null || queries.Length < 2) throw new ArgumentException("At least two Query should be passed");

            var left = queries[0];

            for (int i = 1; i < queries.Length; i++)
            {
                left = Or(left, queries[i]);
            }
            return left;
        }

        #endregion

        #region Executing Query

        /// <summary>
        /// Find witch index will be used and run Execute method
        /// </summary>
        internal virtual IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // get index for this query
            var index = col.GetIndex(this.Field);

            // if index not found, must use Filter (full scan)
            if (index == null)
            {
                this.UseFilter = true;

                // create expression based on Field (if field contains '$' or '(' is already an expression)
                var expr = this.Field.StartsWith("$") || this.Field.IndexOf("(") > 0 ? 
                    this.Field : "$." + this.Field;

                this.Expression = new BsonExpression(expr);

                // returns all index nodes - (will use Filter method later)
                return indexer.FindAll(col.PK, Query.Ascending);
            }
            else
            {
                this.UseIndex = true;

                // create expression from index
                this.Expression = new BsonExpression(index.Expression);

                // execute query to get all IndexNodes
                // do DistinctBy datablock to not duplicate same document in results
                return this.ExecuteIndex(indexer, index)
                    .DistinctBy(x => x.DataBlock, null);
            }
        }

        /// <summary>
        /// Abstract method that must be implement for index seek/scan - Returns IndexNodes that match with index
        /// </summary>
        internal abstract IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index);

        /// <summary>
        /// Abstract method that must implement full scan - will be called for each document and need
        /// returns true if condition was satisfied
        /// </summary>
        internal abstract bool FilterDocument(BsonDocument doc);

        #endregion
    }
}