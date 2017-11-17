using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Remove all document based on a Query object. Returns removed document counts
        /// </summary>
        int Delete(Query query);

        /// <summary>
        /// Remove all document based on a LINQ query. Returns removed document counts
        /// </summary>
        int Delete(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Remove an document in collection using Document Id - returns false if not found document
        /// </summary>
        bool Delete(BsonValue id);
    }
}
