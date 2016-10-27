using System;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
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

            _engine = new LazyLoad<LiteEngine>(() =>
            {
                var disk = conn.Password == null ? new FileDiskService(conn.Filename, options) : new EncryptedDiskService(conn.Filename, conn.Password, options);

                return new LiteEngine(disk, conn.Timeout, conn.CacheSize, conn.AutoCommit, _log);
            });
        }

        /// <summary>
        /// Starts LiteDB database using a custom IDiskService
        /// </summary>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper = null)
        {
            _mapper = mapper ?? BsonMapper.Global;
            _engine = new LazyLoad<LiteEngine>(() => new LiteEngine(diskService, TimeSpan.FromMinutes(1), log: _log ));
        }

        public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}