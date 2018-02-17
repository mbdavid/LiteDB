using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Return IEnumerable of documents using QueryBuilder class to search
        /// </summary>
        public IEnumerable<BsonDocument> Find(string collection, BsonExpression query, LiteTransaction transaction)
        {
            //return query.Run(collection, transaction, this);
            return new QueryBuilder(collection, transaction, this)
                .Where(query)
                .ToEnumerable();
        }

        /// <summary>
        /// Return new QueryBuilder to run search over collection
        /// </summary>
        public QueryBuilder Query(string collection, LiteTransaction transaction)
        {
            return new QueryBuilder(collection, transaction, this);

        }
    }
}