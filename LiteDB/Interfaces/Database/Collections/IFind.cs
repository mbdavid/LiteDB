using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Find documents inside a collection using Query object.
        /// </summary>
        IEnumerable<T> Find(Query query, int skip = 0, int limit = int.MaxValue);

        /// <summary>
        /// Find documents inside a collection using Linq expression. Must have indexes in linq expression
        /// </summary>
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = int.MaxValue);

        /// <summary>
        /// Find a document using Document Id. Returns null if not found.
        /// </summary>
        T FindById(BsonValue id);

        /// <summary>
        /// Find the first document using Query object. Returns null if not found. Must have index on query expression.
        /// </summary>
        T FindOne(Query query);

        /// <summary>
        /// Find the first document using Linq expression. Returns null if not found. Must have indexes on predicate.
        /// </summary>
        T FindOne(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Returns all documents inside collection order by _id index.
        /// </summary>
        IEnumerable<T> FindAll();
    }
}
