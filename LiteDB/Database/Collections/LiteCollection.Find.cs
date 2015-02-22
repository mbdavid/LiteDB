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
        /// <summary>
        /// Find a document using Document Id. Returns null if not found.
        /// </summary>
        public T FindById(BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            var col = this.GetCollectionPage(false);

            if (col == null) return default(T);

            var node = this.Database.Indexer.FindOne(col.PK, id.RawValue);

            if (node == null) return default(T);

            var dataBlock = this.Database.Data.Read(node.DataBlock, true);

            var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

            var obj = this.Database.Mapper.ToObject<T>(doc);

            foreach (var action in _includes)
            {
                action(obj);
            }

            return obj;
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
        /// Find documents inside a collection using Query object. Must have indexes in query expression 
        /// </summary>
        public IEnumerable<T> Find(Query query)
        {
            if (query == null) throw new ArgumentNullException("query");

            var col = this.GetCollectionPage(false);

            if (col == null) yield break;

            var nodes = query.Run(this.Database, col);

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

        /// <summary>
        /// Returns all documents inside collection.
        /// </summary>
        public IEnumerable<T> FindAll()
        {
            return this.Find(Query.All());
        }

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

            var col = this.GetCollectionPage(false);

            if (col == null) return 0;

            return query.Run(this.Database, col).Count();
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

            var col = this.GetCollectionPage(false);

            if (col == null) return false;

            return query.Run(this.Database, col).FirstOrDefault() != null;
        }

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        public bool Exists(Expression<Func<T, bool>> predicate)
        {
            return this.Exists(_visitor.Visit(predicate));
        }
    }
}
