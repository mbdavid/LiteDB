using System;
using System.Collections.Generic;

namespace LiteDB
{
    public interface ILiteDatabase : IDisposable
    {
        /// <summary>
        /// Get logger class instance
        /// </summary>
        Logger Log { get; }

        /// <summary>
        /// Get current instance of BsonMapper used in this database instance (can be BsonMapper.Global)
        /// </summary>
        BsonMapper Mapper { get; }

        /// <summary>
        /// Get current database engine instance. Engine is lower data layer that works with BsonDocuments only (no mapper, no LINQ)
        /// </summary>
        LiteEngine Engine { get; }

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        LiteStorage FileStorage { get; }



        /// <summary>
        /// Checks if a collection exists on database. Collection name is case insensitive
        /// </summary>
        bool CollectionExists(string name);

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        bool DropCollection(string name);

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        LiteCollection<BsonDocument> GetCollection(string name);

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        LiteCollection<T> GetCollection<T>();

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        LiteCollection<T> GetCollection<T>(string name);

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        IEnumerable<string> GetCollectionNames();

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        bool RenameCollection(string oldName, string newName);

        /// <summary>
        /// Reduce disk size re-arranging unused spaces.
        /// </summary>
        long Shrink();

        /// <summary>
        /// Reduce disk size re-arranging unused space. Can change password. If a temporary disk was not provided, use MemoryStream temp disk
        /// </summary>
        long Shrink(string password);
    }
}