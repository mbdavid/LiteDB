using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB.Engine;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resources. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        #region Properties

        private readonly Lazy<LiteEngine> _engine = null;
        private BsonMapper _mapper = BsonMapper.Global;
        private ConnectionString _connectionString = null;

        /// <summary>
        /// Get current instance of BsonMapper used in this database instance (can be BsonMapper.Global)
        /// </summary>
        public BsonMapper Mapper { get { return _mapper; } }

        /// <summary>
        /// Get current database engine instance. Engine is lower data layer that works with BsonDocuments only (no mapper, no LINQ)
        /// </summary>
        public LiteEngine Engine { get { return _engine.Value; } }

        #endregion

        #region Ctor

        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LiteDatabase(string connectionString, BsonMapper mapper = null)
            : this(new ConnectionString(connectionString), mapper)
        {
        }

        /// <summary>
        /// Starts LiteDB database using a connection string for file system database
        /// </summary>
        public LiteDatabase(ConnectionString connectionString, BsonMapper mapper = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            _mapper = mapper ?? BsonMapper.Global;

            _engine = new Lazy<LiteEngine>(() =>
            {
                var settings = new EngineSettings
                {
                    FileName = connectionString.FileName,
                    Password = connectionString.Password,
                    InitialSize = connectionString.InitialSize,
                    LimitSize = connectionString.LimitSize,
                    UtcDate = connectionString.UtcDate,
                    Timeout = connectionString.Timeout,
                    LogLevel = connectionString.Log
                };

                return new LiteEngine(settings);
            });
        }

        /// <summary>
        /// Starts LiteDB database using a Stream disk
        /// </summary>
        public LiteDatabase(Stream stream, BsonMapper mapper = null, string password = null, bool disposeStream = false)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _mapper = mapper ?? BsonMapper.Global;

            _engine = new Lazy<LiteEngine>(() =>
            {
                var settings = new EngineSettings
                {
                    DataStream = stream,
                };

                return new LiteEngine(settings);
            });
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
        {
            return new LiteCollection<T>(name, _engine, _mapper);
        }

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        public LiteCollection<T> GetCollection<T>()
        {
            return this.GetCollection<T>(null);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new LiteCollection<BsonDocument>(name, _engine, _mapper);
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.
        /// Return true if transaction was created or false if current thread already in a transaction.
        /// </summary>
        public bool BeginTrans() => _engine.Value.BeginTrans();

        /// <summary>
        /// Commit current transaction
        /// </summary>
        public bool Commit() => _engine.Value.Commit();

        /// <summary>
        /// Rollback current transaction
        /// </summary>
        public bool Rollback() => _engine.Value.Rollback();

        #endregion

        #region FileStorage

        // private LiteStorage _fs = null;
        // 
        // /// <summary>
        // /// Returns a special collection for storage files/stream inside datafile
        // /// </summary>
        // public LiteStorage FileStorage
        // {
        //     get { return _fs ?? (_fs = new LiteStorage(_engine.Value)); }
        // }

        #endregion

        #region Shortcut

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            return _engine.Value.GetCollectionNames();
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case insensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return _engine.Value.GetCollectionNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return _engine.Value.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            if (oldName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(oldName));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));

            return _engine.Value.RenameCollection(oldName, newName);
        }

        #endregion

        #region Execute SQL

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        public BsonDataReader Execute(string command, BsonDocument parameters = null)
        {
            return _engine.Value.Execute(command, parameters);
        }

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        public BsonDataReader Execute(string command, params BsonValue[] args)
        {
            return _engine.Value.Execute(command, args);
        }

        #endregion

        #region Shrink

        /// <summary>
        /// Reduce disk size re-arranging unused spaces.
        /// </summary>
        public long Shrink()
        {
            return this.Shrink(_connectionString == null ? null : _connectionString.Password);
        }

        /// <summary>
        /// Reduce disk size re-arranging unused space. Can change password. If a temporary disk was not provided, use MemoryStream temp disk
        /// </summary>
        public long Shrink(string password)
        {
            return _engine.Value.Shrink(password);
        }

        #endregion

        public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}
