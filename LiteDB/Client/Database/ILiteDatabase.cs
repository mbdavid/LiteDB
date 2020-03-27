using System;
using System.Collections.Generic;
using System.IO;
using LiteDB.Engine;

namespace LiteDB
{
    public interface ILiteDatabase : IDisposable
    {
        /// <summary>
        /// Get current instance of BsonMapper used in this database instance (can be BsonMapper.Global)
        /// </summary>
        BsonMapper Mapper { get; }

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile. Use _files and _chunks collection names. FileId is implemented as string. Use "GetStorage" for custom options
        /// </summary>
        ILiteStorage<string> FileStorage { get; }

        /// <summary>
        /// Get/Set database user version - use this version number to control database change model
        /// </summary>
        int UserVersion { get; set; }

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        /// <param name="autoId">Define autoId data type (when object contains no id field)</param>
        ILiteCollection<T> GetCollection<T>(string name, BsonAutoId autoId = BsonAutoId.ObjectId);

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        ILiteCollection<T> GetCollection<T>();

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        ILiteCollection<T> GetCollection<T>(BsonAutoId autoId);

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        /// <param name="autoId">Define autoId data type (when document contains no _id field)</param>
        ILiteCollection<BsonDocument> GetCollection(string name, BsonAutoId autoId = BsonAutoId.ObjectId);

        /// <summary>
        /// Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.
        /// Return true if transaction was created or false if current thread already in a transaction.
        /// </summary>
        bool BeginTrans();

        /// <summary>
        /// Commit current transaction
        /// </summary>
        bool Commit();

        /// <summary>
        /// Rollback current transaction
        /// </summary>
        bool Rollback();

        /// <summary>
        /// Get new instance of Storage using custom FileId type, custom "_files" collection name and custom "_chunks" collection. LiteDB support multiples file storages (using different files/chunks collection names)
        /// </summary>
        ILiteStorage<TFileId> GetStorage<TFileId>(string filesCollection = "_files", string chunksCollection = "_chunks");

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        IEnumerable<string> GetCollectionNames();

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case insensitive
        /// </summary>
        bool CollectionExists(string name);

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        bool DropCollection(string name);

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        bool RenameCollection(string oldName, string newName);

        /// <summary>
        /// Execute SQL commands and return as data reader.
        /// </summary>
        IBsonDataReader Execute(TextReader commandReader, BsonDocument parameters = null);

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        IBsonDataReader Execute(string command, BsonDocument parameters = null);

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        IBsonDataReader Execute(string command, params BsonValue[] args);

        /// <summary>
        /// Do database checkpoint. Copy all commited transaction from log file into datafile.
        /// </summary>
        void Checkpoint();

        /// <summary>
        /// Rebuild all database to remove unused pages - reduce data file
        /// </summary>
        long Rebuild(RebuildOptions options = null);

        /// <summary>
        /// Get value from internal engine variables
        /// </summary>
        BsonValue Pragma(string name);

        /// <summary>
        /// Set new value to internal engine variables
        /// </summary>
        BsonValue Pragma(string name, BsonValue value);
    }
}