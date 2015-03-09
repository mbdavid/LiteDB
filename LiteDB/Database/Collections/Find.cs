using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        #region Find

        /// <summary>
        /// Find documents inside a collection using Query object. Must have indexes in query expression 
        /// </summary>
        public IEnumerable<T> Find(Query query, int skip = 0, int limit = 0)
        {
            if (query == null) throw new ArgumentNullException("query");

            var nodes = query.Run<T>(this);

            // skip/limit results direct in index nodes - before deserialize objects
            if (skip > 0) nodes = nodes.Skip(skip);

            if (limit > 0) nodes = nodes.Take(limit);

            foreach (var node in nodes)
            {
                var dataBlock = this.Database.Data.Read(node.DataBlock, true);

                var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

                // get object from BsonDocument
                var obj = this.Database.Mapper.ToObject<T>(doc);

                foreach (var action in _includes)
                {
                    action(obj);
                }

                yield return obj;
            }
        }

        /// <summary>
        /// Find documents inside a collection using Linq expression. Must have indexes in linq expression 
        /// </summary>
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return this.Find(_visitor.Visit(predicate));
        }

        #endregion

        #region FindById + One + All

        /// <summary>
        /// Find a document using Document Id. Returns null if not found.
        /// </summary>
        public T FindById(BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            return this.Find(Query.EQ("_id", id)).SingleOrDefault();
        }

        /// <summary>
        /// Find the first document using Query object. Returns null if not found. Must have index on query expression.
        /// </summary>
        public T FindOne(Query query)
        {
            return this.Find(query).FirstOrDefault();
        }

        /// <summary>
        /// Find the first document using Linq expression. Returns null if not found. Must have indexes on predicate.
        /// </summary>
        public T FindOne(Expression<Func<T, bool>> predicate)
        {
            return this.Find(_visitor.Visit(predicate)).FirstOrDefault();
        }

        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        public IEnumerable<T> FindAll()
        {
            return this.Find(Query.All());
        }

        #endregion

        #region Count/Exits

        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        public int Count()
        {
            var col = this.GetCollectionPage(false);

            if (col == null) return 0;

            return Convert.ToInt32(col.DocumentCount);
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            return query.Run<T>(this).Count();
        }

        /// <summary>
        /// Count documnets with a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public int Count(Expression<Func<T, bool>> predicate)
        {
            return this.Count(_visitor.Visit(predicate));
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            return query.Run<T>(this).FirstOrDefault() != null;
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return this.Exists(_visitor.Visit(predicate));
        }

        #endregion

        #region Min/Max

        /// <summary>
        /// Returns the first/min value from a index field
        /// </summary>
        public BsonValue Min(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var node = Query.Min(field).Run(this).SingleOrDefault();

            return node == null ? BsonValue.MinValue : node.Value;
        }

        /// <summary>
        /// Returns the first/min _id field
        /// </summary>
        public BsonValue Min()
        {
            return this.Min("_id");
        }

        /// <summary>
        /// Returns the first/min field using a linq expression
        /// </summary>
        public BsonValue Min<K>(Expression<Func<T, K>> property)
        {
            var field = _visitor.GetBsonField(property);

            return this.Min(field);
        }

        /// <summary>
        /// Returns the last/max value from a index field
        /// </summary>
        public BsonValue Max(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");

            var node = Query.Max(field).Run(this).SingleOrDefault();

            return node == null ? BsonValue.MaxValue : node.Value;
        }

        /// <summary>
        /// Returns the last/max _id field
        /// </summary>
        public BsonValue Max()
        {
            return this.Max("_id");
        }

        /// <summary>
        /// Returns the last/max field using a linq expression
        /// </summary>
        public BsonValue Max<K>(Expression<Func<T, K>> property)
        {
            var field = _visitor.GetBsonField(property);

            return this.Max(field);
        }

        #endregion
    }
}
