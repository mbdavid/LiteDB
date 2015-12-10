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
            if (query == null) throw new ArgumentNullException("query");

            // keep trying execute query to auto-create indexes when not found
            while (true)
            {
                try
                {
                    return _engine.Delete(_name, query);
                }
                catch (IndexNotFoundException ex)
                {
                    // if query returns this exception, let's auto create using mapper (or using default options)
                    var options = _mapper.GetIndexFromMapper<T>(ex.Field) ?? new IndexOptions();

                    _engine.EnsureIndex(ex.Collection, ex.Field, options);
                }
            }
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
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            return this.Delete(Query.EQ("_id", id)) > 0;
        }
    }
}