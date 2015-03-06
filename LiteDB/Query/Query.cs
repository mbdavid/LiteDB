using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create query using indexes in database. All methods are statics
    /// </summary>
    public abstract class Query
    {
        internal Func<string, CollectionIndex> FindIndexAttribute = null;

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
        public static Query LT(string field, BsonValue value, int order = Ascending)
        {
            return new QueryLess(field, value ?? BsonValue.Null, false, order);
        }

        /// <summary>
        /// Returns all documents that value are less than or equals value (&lt;=)
        /// </summary>
        public static Query LTE(string field, BsonValue value, int order = Ascending)
        {
            return new QueryLess(field, value ?? BsonValue.Null, true, order);
        }

        /// <summary>
        /// Returns all document that value are greater than value (&gt;)
        /// </summary>
        public static Query GT(string field, BsonValue value, int order = Ascending)
        {
            return new QueryGreater(field, value ?? BsonValue.Null, false, order);
        }

        /// <summary>
        /// Returns all documents that value are greater than or equals value (&gt;=)
        /// </summary>
        public static Query GTE(string field, BsonValue value, int order = Ascending)
        {
            return new QueryGreater(field, value ?? BsonValue.Null, true, order);
        }

        /// <summary>
        /// Returns all document that values are between "start" and "end" values (BETWEEN)
        /// </summary>
        public static Query Between(string field, BsonValue start, BsonValue end)
        {
            return new QueryBetween(field, start, end);
        }

        /// <summary>
        /// Returns all documents that starts with value (LIKE)
        /// </summary>
        public static Query StartsWith(string field, string value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value");

            return new QueryStartsWith(field ?? BsonValue.Null, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that are not equals to value
        /// </summary>
        public static Query Not(string field, BsonValue value, int order = Ascending)
        {
            return new QueryNot(field, value ?? BsonValue.Null, order);
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
        /// Returns the document with min index value
        /// </summary>
        public static Query Min(string field)
        {
            return new QueryMin(field);
        }

        /// <summary>
        /// Returns the document with max index value
        /// </summary>
        public static Query Max(string field)
        {
            return new QueryMax(field);
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

        // used for execute in results (AND/OR)
        internal abstract IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index);

        internal virtual IEnumerable<IndexNode> Run<T>(LiteCollection<T> collection)
            where T : new()
        {
            // get collection page - no collection, no results
            var col = collection.GetCollectionPage(false);

            if (col == null) yield break;

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

            if (index == null) throw new LiteException(string.Format("Index '{0}.{1}' not found. Use EnsureIndex to create a new index.", col.CollectionName, this.Field));

            // execute query to get all IndexNodes
            var nodes = this.Execute(collection.Database.Indexer, index);

            foreach (var node in nodes)
            {
                yield return node;
            }
        }

        #endregion
    }
}
