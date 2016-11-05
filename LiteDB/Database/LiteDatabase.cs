using System;
using System.Collections.Generic;
using System.IO;

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
                Timeout = conn.Timeout,
                Password = conn.Password
            };

            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(new FileDiskService(conn.Filename, options), conn.Timeout, conn.CacheSize, conn.AutoCommit, _log));
        }

        /// <summary>
        /// Starts LiteDB database using a custom IDiskService
        /// </summary>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper = null)
        {
            _mapper = mapper ?? BsonMapper.Global;
            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(diskService, TimeSpan.FromMinutes(1), log: _log ));
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
            where T : new()
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