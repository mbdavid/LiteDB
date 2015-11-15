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
        public IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue)
        {
            if (query == null) throw new ArgumentNullException("query");

            IEnumerable<BsonDocument> docs;

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    docs = _engine.Find(_name, query, skip, limit);
                    break;
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }

            foreach (var doc in docs)
            {
                // executing all includes in BsonDocument
                foreach (var action in _includes)
                {
                    action(doc);
                }

                // get object from BsonDocument
                var obj = _mapper.ToObject<T>(doc);

                yield return obj;
            }
        }

        /// <summary>
        /// Find documents inside a collection using Linq expression. Must have indexes in linq expression 
        /// </summary>
        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue)
        {
            return this.Find(_visitor.Visit(predicate), skip, limit);
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
    }
}
