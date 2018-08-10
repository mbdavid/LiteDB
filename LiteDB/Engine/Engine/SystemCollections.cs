using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private Dictionary<string, SystemCollection> _systemCollections = new Dictionary<string, SystemCollection>();

        /// <summary>
        /// Get name of all system collections (returns only non-function collections)
        /// </summary>
        public IEnumerable<string> GetSystemCollections()
        {
            foreach(var item in _systemCollections)
            {
                if (item.Value.IsFunction) continue;

                yield return item.Key;
            }
        }

        /// <summary>
        /// Get registered system collection
        /// </summary>
        internal SystemCollection GetSystemCollection(string name)
        {
            if (_systemCollections.TryGetValue(name, out var sys))
            {
                return sys;
            }

            throw new LiteException(0, $"System collection '{name}' are not registered as system collection");
        }

        /// <summary>
        /// Register a new system collection that can be used in query for input/output data
        /// Collection name must starts with $
        /// </summary>
        public void RegisterSystemCollection(SystemCollection systemCollection)
        {
            if (systemCollection == null) throw new ArgumentNullException(nameof(systemCollection));

            _systemCollections[systemCollection.Name] = systemCollection;
        }

        /// <summary>
        /// Register a new system collection that can be used in query for input data
        /// Collection name must starts with $
        /// </summary>
        public void RegisterSystemCollection(string collectionName, Func<BsonValue, IEnumerable<BsonDocument>> factory)
        {
            if (collectionName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collectionName));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _systemCollections[collectionName] = new SystemCollection(collectionName, factory);
        }
    }
}