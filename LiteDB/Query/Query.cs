using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal enum QueryExecuteMode { IndexSeek, FullScan }

    /// <summary>
    /// Class helper to create query using indexes in database. All methods are statics.
    /// Queries can be executed in 3 ways: Index Seek (fast), Index Scan (good), Full Scan (slow)
    /// </summary>
    public abstract class Query
    {
        public string Field { get; private set; }

        /// <summary>
        /// Indicate that query need to run under full scan (there is not index)
        /// </summary>
        internal QueryExecuteMode ExecuteMode { get; set; }

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

        #endregion

        #region Execute Query

        /// <summary>
        /// Abstract method that must be implement for index seek/scan - Returns IndexNodes that match with index
        /// </summary>
        internal abstract IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index);

        /// <summary>
        /// Abstract method to normalize values before run full scan
        /// </summary>
        internal abstract void NormalizeValues(IndexOptions options);

        /// <summary>
        /// Abstract method that must implement full scan - will be called for each document and need
        /// returns true if condition was satisfied
        /// </summary>
        internal abstract bool ExecuteFullScan(BsonDocument doc, IndexOptions options);

        /// <summary>
        /// Find witch index will be used and run Execute method - define ExecuteMode here
        /// </summary>
        internal virtual IEnumerable<IndexNode> Run<T>(LiteCollection<T> collection)
            where T : new()
        {
            // get collection page - no collection, no results
            var col = collection.GetCollectionPage(false);

            // no collection just returns an empty list of indexnode
            if (col == null) return new List<IndexNode>();

            // get index
            var index = col.GetIndex(this.Field);

            // if index not found, lets check if type T has [BsonIndex]
            if (index == null && typeof(T) != typeof(BsonDocument))
            {
                var options = collection.Database.Mapper.GetIndexFromAttribute<T>(this.Field);

                // create a new index
                if (options != null)
                {
                    collection.EnsureIndex(this.Field, options);

                    index = col.GetIndex(this.Field);
                }
            }

            if (index == null)
            {
                this.ExecuteMode = QueryExecuteMode.FullScan;

                // normalize query values before run full scan
                this.NormalizeValues(new IndexOptions());

                // if there is no index, returns all index nodes - will be used Full Scan
                return collection.Database.Indexer.FindAll(col.PK, Query.Ascending);
            }
            else
            {
                this.ExecuteMode = QueryExecuteMode.IndexSeek;

                // execute query to get all IndexNodes
                return this.ExecuteIndex(collection.Database.Indexer, index);
            }
        }

        #endregion
    }
}
