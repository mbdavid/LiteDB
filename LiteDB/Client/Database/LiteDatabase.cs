using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resources. It's the database connection
    /// </summary>
    public partial class LiteDatabase : ILiteDatabase
    {
        #region Properties

        private readonly ILiteEngine _engine;
        private readonly BsonMapper _mapper;

        /// <summary>
        /// Get current instance of BsonMapper used in this database instance (can be BsonMapper.Global)
        /// </summary>
        public BsonMapper Mapper => _mapper;

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
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            if (connectionString.Upgrade)
            {
                // try upgrade if need
                LiteEngine.Upgrade(connectionString.Filename, connectionString.Password);
            }

            _engine = connectionString.CreateEngine();
            _mapper = mapper ?? BsonMapper.Global;
        }

        /// <summary>
        /// Starts LiteDB database using a generic Stream implementation (mostly MemoryStrem).
        /// Use another MemoryStrem as LOG file.
        /// </summary>
        public LiteDatabase(Stream stream, BsonMapper mapper = null)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var settings = new EngineSettings
            {
                DataStream = stream
            };

            _engine = new LiteEngine(settings);
            _mapper = mapper ?? BsonMapper.Global;
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public ILiteCollection<T> GetCollection<T>(string name)
        {
            return new LiteCollection<T>(name, BsonAutoId.ObjectId, _engine, _mapper);
        }

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        public ILiteCollection<T> GetCollection<T>()
        {
            return this.GetCollection<T>(null);
        }

        /// <summary>
        /// Get a collection using a name based on typeof(T).Name (BsonMapper.ResolveCollectionName function)
        /// </summary>
        public ILiteCollection<T> GetCollection<T>(BsonAutoId autoId)
        {
            return this.GetCollection<T>(null);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exists, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case-insensitive)</param>
        /// <param name="autoId">Define autoId data type (when document contains no _id field)</param>
        public ILiteCollection<BsonDocument> GetCollection(string name, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return new LiteCollection<BsonDocument>(name, autoId, _engine, _mapper);
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.
        /// Return true if transaction was created or false if current thread already in a transaction.
        /// </summary>
        public bool BeginTrans() => _engine.BeginTrans();

        /// <summary>
        /// Commit current transaction
        /// </summary>
        public bool Commit() => _engine.Commit();

        /// <summary>
        /// Rollback current transaction
        /// </summary>
        public bool Rollback() => _engine.Rollback();

        #endregion

        #region FileStorage

        private ILiteStorage<string> _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile. Use _files and _chunks collection names. FileId is implemented as string. Use "GetStorage" for custom options
        /// </summary>
        public ILiteStorage<string> FileStorage
        {
            get { return _fs ?? (_fs = this.GetStorage<string>()); }
        }

        /// <summary>
        /// Get new instance of Storage using custom FileId type, custom "_files" collection name and custom "_chunks" collection. LiteDB support multiples file storages (using different files/chunks collection names)
        /// </summary>
        public ILiteStorage<TFileId> GetStorage<TFileId>(string filesCollection = "_files", string chunksCollection = "_chunks")
        {
            return new LiteStorage<TFileId>(this, filesCollection, chunksCollection);
        }

        #endregion

        #region Shortcut

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            // use $cols system collection with type = user only
            var cols = this.GetCollection("$cols")
                .Query()
                .Where("type = 'user'")
                .ToDocuments()
                .Select(x => x["name"].AsString)
                .ToArray();

            return cols;
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case insensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return this.GetCollectionNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            return _engine.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            if (oldName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(oldName));
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(newName));

            return _engine.RenameCollection(oldName, newName);
        }

        #endregion

        #region Execute SQL

        /// <summary>
        /// Execute SQL commands and return as data reader.
        /// </summary>
        public IBsonDataReader Execute(TextReader commandReader, BsonDocument parameters = null)
        {
            if (commandReader == null) throw new ArgumentNullException(nameof(commandReader));

            var tokenizer = new Tokenizer(commandReader);
            var sql = new SqlParser(_engine, tokenizer, parameters);
            var reader = sql.Execute();

            return reader;
        }

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        public IBsonDataReader Execute(string command, BsonDocument parameters = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            var tokenizer = new Tokenizer(command);
            var sql = new SqlParser(_engine, tokenizer, parameters);
            var reader = sql.Execute();

            return reader;
        }

        /// <summary>
        /// Execute SQL commands and return as data reader
        /// </summary>
        public IBsonDataReader Execute(string command, params BsonValue[] args)
        {
            var p = new BsonDocument();
            var index = 0;

            foreach (var arg in args)
            {
                p[index.ToString()] = arg;
                index++;
            }

            return this.Execute(command, p);
        }

        #endregion

        #region Analyze/Checkpoint/Shrink/UserVersion

        /// <summary>
        /// Do database checkpoint. Copy all committed transaction from log file into datafile.
        /// </summary>
        public void Checkpoint()
        {
            _engine.Checkpoint();
        }

        /// <summary>
        /// Rebuild all database to remove unused pages - reduce data file
        /// </summary>
        public long Rebuild(RebuildOptions options = null)
        {
            return _engine.Rebuild(options);
        }

        /// <summary>
        /// Get value from internal engine variables
        /// </summary>
        public BsonValue Pragma(string name)
        {
            return _engine.Pragma(name);
        }

        /// <summary>
        /// Set new value to internal engine variables
        /// </summary>
        public BsonValue Pragma(string name, BsonValue value)
        {
            return _engine.Pragma(name, value);
        }

        /// <summary>
        /// Get/Set database user version - use this version number to control database change model
        /// </summary>
        public int UserVersion
        {
            get => _engine.Pragma(Pragmas.USER_VERSION);
            set => _engine.Pragma(Pragmas.USER_VERSION, value);
        }

        #endregion

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~LiteDatabase()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _engine.Dispose();
            }
        }
    }
}
