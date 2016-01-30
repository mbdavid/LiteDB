using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
            where T : new()
        {
            if (String.IsNullOrEmpty(name) || name.Trim().Length == 0) throw new ArgumentNullException("name");

            return new LiteCollection<T>(name, _engine.Value, _mapper, _log);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            if (String.IsNullOrEmpty(name) || name.Trim().Length == 0) throw new ArgumentNullException("name");

            return new LiteCollection<BsonDocument>(name, _engine.Value, _mapper, _log);
        }

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            return _engine.Value.GetCollectionNames();
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case unsensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            if (String.IsNullOrEmpty(name) || name.Trim().Length == 0) throw new ArgumentNullException("name");

            return _engine.Value.GetCollectionNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            if (name == null || name.Length <= 0) throw new ArgumentNullException("name");

            return _engine.Value.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            if (String.IsNullOrEmpty(oldName) || oldName.Trim().Length == 0) throw new ArgumentNullException("oldName");
            if (String.IsNullOrEmpty(newName) || newName.Trim().Length == 0) throw new ArgumentNullException("newName");

            return _engine.Value.RenameCollection(oldName, newName);
        }
    }
}