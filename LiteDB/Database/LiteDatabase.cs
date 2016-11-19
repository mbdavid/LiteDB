using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        #region Properties

        private LazyLoad<LiteEngine> _engine;
        private BsonMapper _mapper;
        private Logger _log = new Logger();

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
        /// Starts LiteDB database using a connection string for filesystem database
        /// </summary>
        public LiteDatabase(string connectionString, BsonMapper mapper = null)
        {
            var conn = new ConnectionString(connectionString);

            _mapper = mapper ?? BsonMapper.Global;

            var options = new FileOptions
            {
                InitialSize = conn.InitialSize,
                LimitSize = conn.LimitSize,
                Journal = conn.Journal,
                Timeout = conn.Timeout
            };

            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(new FileDiskService(conn.Filename, options), conn.Password, conn.Timeout, conn.AutoCommit, conn.CacheSize, _log));
        }

        /// <summary>
        /// Starts LiteDB database using a Stream disk
        /// </summary>
        public LiteDatabase(Stream stream, BsonMapper mapper = null, string password = null)
        {
            _mapper = mapper ?? BsonMapper.Global;
            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(new StreamDiskService(stream), password: password, log: _log));
        }

        /// <summary>
        /// Starts LiteDB database using a custom IDiskService with all parameters avaiable
        /// </summary>
        /// <param name="diskService">Custom implementation of persist data layer</param>
        /// <param name="mapper">Instance of BsonMapper that map poco classes to document</param>
        /// <param name="password">Password to encrypt you datafile</param>
        /// <param name="timeout">Locker timeout for concurrent access</param>
        /// <param name="autocommit">If auto commit after any write operation</param>
        /// <param name="cacheSize">Max memory pages used before flush data in Journal file (when avaiable)</param>
        /// <param name="log">Custom log implementation</param>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper = null, string password = null, TimeSpan? timeout = null, bool autocommit = true, int cacheSize = 5000, Logger log = null)
        {
            _mapper = mapper ?? BsonMapper.Global;
            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(diskService, password: password, timeout: timeout, autocommit: autocommit, cacheSize: cacheSize, log: _log ));
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Starts new transaction
        /// </summary>
        public LiteTransaction BeginTrans()
        {
            _engine.Value.BeginTrans();

            return new LiteTransaction(_engine.Value.Commit, _engine.Value.Rollback);
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
        {
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException("name");

            return new LiteCollection<T>(name, _engine, _mapper, _log);
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

        public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}