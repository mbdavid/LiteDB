using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Create complex query and execute with QueryBuilder
        /// </summary>
        public QueryBuilder Query(string collection, LiteTransaction transaction)
        {
            return new QueryBuilder(collection, this, transaction, _bsonReader);
        }
    }
}