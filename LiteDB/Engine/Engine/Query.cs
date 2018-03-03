using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Return new QueryBuilder to run search over collection
        /// </summary>
        public QueryBuilder Query(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentNullException(nameof(collection));

            return new QueryBuilder(collection, this);
        }
    }
}