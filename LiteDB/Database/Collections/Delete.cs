using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Remove all document based on a Query object. Returns removed document counts
        /// </summary>
        public int Delete(Query query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            return _engine.Value.Delete(_name, query);
        }

        /// <summary>
        /// Remove all document based on a LINQ query. Returns removed document counts
        /// </summary>
        public int Delete(Expression<Func<T, bool>> predicate)
        {
            return this.Delete(_visitor.Visit(predicate));
        }

        /// <summary>
        /// Remove an document in collection using Document Id - returns false if not found document
        /// </summary>
        public bool Delete(BsonValue id)
        {
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            return this.Delete(Query.EQ("_id", id)) > 0;
        }
    }
}