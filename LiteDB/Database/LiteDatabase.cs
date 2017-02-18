using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resources. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        #region Properties

        private LazyLoad<LiteEngine> _engine = null;
        private BsonMapper _mapper = BsonMapper.Global;
        private Logger _log = new Logger();
        private ConnectionString _connectionString = null;

        /// <summary>
        /// Get logger class instance
        /// </summary>
        public Logger Log { get { return _log; } }

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
            if (connectionString == null) throw new ArgumentNullException("connectionString");

            _connectionString = connectionString;
            _log.Level = _connectionString.Log;

            if (_connectionString.Upgrade)
            {
                LiteEngine.Upgrade(_connectionString.Filename, _connectionString.Password);
            }

            _mapper = mapper ?? BsonMapper.Global;

            var options = new FileOptions
            {
                InitialSize = _connectionString.InitialSize,
                LimitSize = _connectionString.LimitSize,
                Journal = _connectionString.Journal
            };

            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(new FileDiskService(_connectionString.Filename, options), _connectionString.Password, _connectionString.Timeout, _connectionString.CacheSize, _log));
        }

        /// <summary>
        /// Starts LiteDB database using a Stream disk
        /// </summary>
        public LiteDatabase(Stream stream, BsonMapper mapper = null, string password = null)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            _mapper = mapper ?? BsonMapper.Global;

            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(new StreamDiskService(stream), password: password, log: _log));
        }

        /// <summary>
        /// Starts LiteDB database using a custom IDiskService with all parameters available
        /// </summary>
        /// <param name="diskService">Custom implementation of persist data layer</param>
        /// <param name="mapper">Instance of BsonMapper that map poco classes to document</param>
        /// <param name="password">Password to encrypt you datafile</param>
        /// <param name="timeout">Locker timeout for concurrent access</param>
        /// <param name="cacheSize">Max memory pages used before flush data in Journal file (when available)</param>
        /// <param name="log">Custom log implementation</param>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper = null, string password = null, TimeSpan? timeout = null, int cacheSize = 5000, Logger log = null)
        {
            if (diskService == null) throw new ArgumentNullException("diskService");

            _mapper = mapper ?? BsonMapper.Global;

            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(diskService, password: password, timeout: timeout, cacheSize: cacheSize, log: _log ));
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Starts new transaction
        /// </summary>
        public LiteTransaction BeginTrans()
        {
            _engine.Value.BeginTrans();

            return new LiteTransaction(() => _engine.Value.Commit(), _engine.Value.Rollback);
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
        {
            return new LiteCollection<T>(name, _engine, _mapper, _log);
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
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

            return new LiteCollection<BsonDocument>(name, _engine, _mapper, _log);
        }

        #endregion

        #region FileStorage

        private LiteStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteStorage(_engine.Value)); }
        }

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
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

            return _engine.Value.GetCollectionNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

            return _engine.Value.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            if (oldName.IsNullOrWhiteSpace()) throw new ArgumentNullException("oldName");
            if (newName.IsNullOrWhiteSpace()) throw new ArgumentNullException("newName");

            return _engine.Value.RenameCollection(oldName, newName);
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
            // if has connection string, use same path
            if (_connectionString != null)
            {
                // get temp file ("-temp" suffix)
                var tempFile = FileHelper.GetTempFile(_connectionString.Filename);

                // get temp disk based on temp file
                var tempDisk = new FileDiskService(tempFile, false);

                var reduced = _engine.Value.Shrink(password);

                // delete temp file
                File.Delete(tempFile);

                return reduced;
            }
            else
            {
                return _engine.Value.Shrink(password);
            }
        }

        #endregion

        public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}