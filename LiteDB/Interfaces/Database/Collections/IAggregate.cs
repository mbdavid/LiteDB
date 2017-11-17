using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        int Count();

        /// <summary>
        /// Count documents matching a query. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        int Count(Query query);

        /// <summary>
        /// Count documents matching a query. This method does not deserialize any documents. Needs indexes on query expression
        /// </summary>
        int Count(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        long LongCount();

        /// <summary>
        /// Count documents matching a query. This method does not deserialize any documents. Needs indexes on query expression
        /// </summary>
        long LongCount(Query query);

        /// <summary>
        /// Count documents matching a query. This method does not deserialize any documents. Needs indexes on query expression
        /// </summary>
        long LongCount(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        bool Exists(Query query);

        /// <summary>
        /// Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression
        /// </summary>
        bool Exists(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Returns the first/min value from a index field
        /// </summary>
        BsonValue Min(string field);

        /// <summary>
        /// Returns the first/min _id field
        /// </summary>
        BsonValue Min();

        /// <summary>
        /// Returns the first/min field using a linq expression
        /// </summary>
        BsonValue Min<K>(Expression<Func<T, K>> property);

        /// <summary>
        /// Returns the last/max value from a index field
        /// </summary>
        BsonValue Max(string field);

        /// <summary>
        /// Returns the last/max _id field
        /// </summary>
        BsonValue Max();

        /// <summary>
        /// Returns the last/max field using a linq expression
        /// </summary>
        BsonValue Max<K>(Expression<Func<T, K>> property);
    }
}
