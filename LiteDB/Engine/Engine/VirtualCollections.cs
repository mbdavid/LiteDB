using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteEngine
    {
        private Dictionary<string, Func<IEnumerable<BsonDocument>>> _virtualCollections = new Dictionary<string, Func<IEnumerable<BsonDocument>>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Register a new virtual collection that can be used in query (used for system information)
        /// Collection name must stasts with $
        /// </summary>
        public void RegisterVirtualCollection(string collectionName, Func<IEnumerable<BsonDocument>> factory)
        {
            if (collectionName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collectionName));
            if (!collectionName.StartsWith("$")) throw new ArgumentException("Virtual collection name must starts with $");
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _virtualCollections[collectionName] = factory;
        }
    }
}