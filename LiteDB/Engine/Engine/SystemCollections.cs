using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private Dictionary<string, Func<IEnumerable<BsonDocument>>> _systemCollections = new Dictionary<string, Func<IEnumerable<BsonDocument>>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Get name of all system collections
        /// </summary>
        public IEnumerable<string> GetSystemCollections() => _systemCollections.Keys;

        /// <summary>
        /// Register a new system collection that can be used in query (used for system information)
        /// Collection name must stasts with $
        /// </summary>
        public void RegisterSystemCollection(string collectionName, Func<IEnumerable<BsonDocument>> factory)
        {
            if (collectionName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collectionName));
            if (!collectionName.StartsWith("$")) throw new ArgumentException("System collection name must starts with $");

            _systemCollections[collectionName] = factory ?? throw new ArgumentNullException(nameof(factory));
        }
    }
}