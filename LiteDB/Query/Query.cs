using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create query using indexes in database. All methods are statics.
    /// Queries can be executed in 2 ways: Index Seek (fast), Index Scan (good)
    /// </summary>
    public abstract class Query
    {
        public string Field { get; private set; }

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
            return new QueryAll(field, order);
        }

        /// <summary>
        /// Returns all documents that value are equals to value (=)
        /// </summary>
        public static Query EQ(string field, BsonValue value)
        {
            return new QueryEquals(field, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that value are less than value (&lt;)
        /// </summary>
        public static Query LT(string field, BsonValue value)
        {
            return new QueryLess(field, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are less than or equals value (&lt;=)
        /// </summary>
        public static Query LTE(string field, BsonValue value)
        {
            return new QueryLess(field, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that value are greater than value (&gt;)
        /// </summary>
        public static Query GT(string field, BsonValue value)
        {
            return new QueryGreater(field, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are greater than or equals value (&gt;=)
        /// </summary>
        public static Query GTE(string field, BsonValue value)
        {
            return new QueryGreater(field, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that values are between "start" and "end" values (BETWEEN)
        /// </summary>
        public static Query Between(string field, BsonValue start, BsonValue end)
        {
            return new QueryBetween(field, start ?? BsonValue.Null, end ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that starts with value (LIKE)
        /// </summary>
        public static Query StartsWith(string field, string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            return new QueryStartsWith(field, value);
        }

        /// <summary>
        /// Returns all documents that contains value (CONTAINS)
        /// </summary>
        public static Query Contains(string field, string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            return new QueryContains(field, value);
        }

        /// <summary>
        /// Returns all documents that are not equals to value
        /// </summary>
        public static Query Not(string field, BsonValue value)
        {
            return new QueryNot(field, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that has value in values list (IN)
        /// </summary>
        public static Query In(string field, BsonArray value)
        {
            if (value == null) throw new ArgumentNullException("value");

            return new QueryIn(field, value.RawValue);
        }

        /// <summary>
        /// Returns all documents that has value in values list (IN)
        /// </summary>
        public static Query In(string field, params BsonValue[] values)
        {
            if (values == null) throw new ArgumentNullException("values");

            return new QueryIn(field, values);
        }

        /// <summary>
        /// Returns document that exists in BOTH queries results (Intersect).
        /// </summary>
        public static Query And(Query left, Query right)
        {
            return new QueryAnd(left, right);
        }

        /// <summary>
        /// Returns documents that exists in ANY queries results (Union).
        /// </summary>
        public static Query Or(Query left, Query right)
        {
            return new QueryOr(left, right);
        }

        #endregion Static Methods

        #region Execute Query

        /// <summary>
        /// Abstract method that must be implement for index seek/scan - Returns IndexNodes that match with index
        /// </summary>
        internal abstract IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index);

        /// <summary>
        /// Find witch index will be used and run Execute method
        /// </summary>
        internal virtual IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // get index for this query
            var index = col.GetIndex(this.Field);

            // no index? throw an index not found exception to auto-create in LiteDatabse
            if (index == null) throw new IndexNotFoundException(col.CollectionName, this.Field);

            // execute query to get all IndexNodes
            return this.ExecuteIndex(indexer, index);
        }

        #endregion Execute Query
    }
}