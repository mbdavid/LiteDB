using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Run query over collection using a query definition
        /// </summary>
        public BsonDataReader Query(string collection, QueryDefinition query)
        {
            if (string.IsNullOrWhiteSpace(collection)) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            IEnumerable<BsonDocument> source = null;

            // test if is an system collection
            if (collection.StartsWith("$") && _systemCollections.TryGetValue(collection, out var factory))
            {
                source = factory();
            }

            var exec = new QueryExecutor(this, collection, query, source);

            return exec.ExecuteQuery();
        }

        /// <summary>
        /// Execute query using custom collection implementation (external data source)
        /// </summary>
        public BsonDataReader Query(IFileCollection collection, QueryDefinition query)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            var exec = new QueryExecutor(this, collection.Name, query, collection.Input());

            return exec.ExecuteQuery();
        }
    }
}