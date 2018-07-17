using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Return new QueryBuilder to run search over collection
        /// </summary>
        public QueryBuilder Query(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentNullException(nameof(collection));

            // test if is an system collection
            if (collection.StartsWith("$") && _systemCollections.TryGetValue(collection, out var factory))
            {
                return new QueryBuilder(collection, this, factory());
            }

            return new QueryBuilder(collection, this);
        }

        /// <summary>
        /// Return new QueryBuilder based on external data source
        /// </summary>
        public QueryBuilder Query(IFileCollection collection)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));

            return new QueryBuilder(collection.Name, this, collection.Input());
        }
    }
}