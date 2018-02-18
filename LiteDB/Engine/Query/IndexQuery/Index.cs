using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Class that implement higher level of index search operations (equals, greater, less, ...)
    /// </summary>
    public abstract class Index
    {
        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; private set; }

        internal Index(string name)
        {
            this.Name = name;
        }

        #region Static Methods

        /// <summary>
        /// Returns all documents using _id index order
        /// </summary>
        public static Index All(int order = Query.Ascending)
        {
            return new IndexAll("_id", order);
        }

        /// <summary>
        /// Returns all documents using index order
        /// </summary>
        public static Index All(string name, int order = Query.Ascending)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexAll(name, order);
        }

        /// <summary>
        /// Returns all documents that value are equals to value (=)
        /// </summary>
        public static Index EQ(string name, BsonValue value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexEquals(name, value ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that value are less than value (&lt;)
        /// </summary>
        public static Index LT(string name, BsonValue value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexLess(name, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are less than or equals value (&lt;=)
        /// </summary>
        public static Index LTE(string name, BsonValue value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexLess(name, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that value are greater than value (&gt;)
        /// </summary>
        public static Index GT(string name, BsonValue value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexGreater(name, value ?? BsonValue.Null, false);
        }

        /// <summary>
        /// Returns all documents that value are greater than or equals value (&gt;=)
        /// </summary>
        public static Index GTE(string name, BsonValue value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexGreater(name, value ?? BsonValue.Null, true);
        }

        /// <summary>
        /// Returns all document that values are between "start" and "end" values (BETWEEN)
        /// </summary>
        public static Index Between(string name, BsonValue start, BsonValue end)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new IndexBetween(name, start ?? BsonValue.Null, end ?? BsonValue.Null);
        }

        /// <summary>
        /// Returns all documents that "Sql Like" with value
        /// </summary>
        public static Index Like(string name, string value)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (value.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(value));

            return new IndexLike(name, value);
        }

        /// <summary>
        /// Returns all documents that startsWith value [index scan]
        /// </summary>
        public static Index StartsWith(string name, string value, int order = Query.Ascending)
        {
            return Like(name, value + "%");
        }

        /// <summary>
        /// Apply a predicate function in an index result (index scan). Execute full index scan but it's faster then runs over deserialized document.
        /// </summary>
        public static Index Scan(string name, Func<BsonValue, bool> predicate, int order = Query.Ascending)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return new IndexScan(name, predicate, order);
        }

        /// <summary>
        /// Returns all documents that contains value [index scan]
        /// </summary>
        public static Index Contains(string name, string value, int order = Query.Ascending)
        {
            return Scan(name, x => x.IsString && x.AsString.Contains(value ?? BsonValue.Null), order);
        }

        /// <summary>
        /// Returns all documents that ends with string [index scan]
        /// </summary>
        public static Index EndsWith(string name, string value, int order = Query.Ascending)
        {
            return Scan(name, x => x.IsString && x.AsString.EndsWith(value ?? BsonValue.Null), order);
        }

        /// <summary>
        /// Returns all documents that are not equals to value (not equals) [index scan]
        /// </summary>
        public static Index Not(string name, BsonValue value, int order = Query.Ascending)
        {
            return Scan(name, x => x.CompareTo(value ?? BsonValue.Null) != 0, order);
        }

        #endregion

        #region Executing Index Search

        /// <summary>
        /// Calculate score based on type/value/collection - Number are from 1 (best) to 0 (worst)
        /// It will be used to decide best index to use
        /// </summary>
        internal abstract double GetScore(CollectionIndex index);

        /// <summary>
        /// Abstract method that must be implement for index seek/scan - Returns IndexNodes that match with index
        /// </summary>
        internal abstract IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index);

        /// <summary>
        /// Find witch index will be used and run Execute method
        /// </summary>
        internal virtual IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            // get index for this query
            var index = col.GetIndex(this.Name);

            if (index == null) throw LiteException.IndexNotFound(col.CollectionName, this.Name);

            // execute query to get all IndexNodes
            return this.Execute(indexer, index);
        }

        #endregion
    }
}