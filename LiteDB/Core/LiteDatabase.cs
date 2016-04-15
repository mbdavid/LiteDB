using System;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        private LazyLoad<DbEngine> _engine;

        private BsonMapper _mapper;

        private Logger _log = new Logger();

        public ushort Version { get; private set; }

        public Logger Log { get { return _log; } }

        /// <summary>
        /// Starts LiteDB database using a connection string for filesystem database
        /// </summary>
        public LiteDatabase(string connectionString)
        {
            var conn = new ConnectionString(connectionString);
            var version = conn.GetValue<ushort>("version", 0);
            var encrypted = !StringExtensions.IsNullOrWhiteSpace(conn.GetValue<string>("password", null));

            _engine = new LazyLoad<DbEngine>(
                () => new DbEngine(encrypted ? new EncryptedDiskService(conn, _log) : new FileDiskService(conn, _log), _log),
                () => this.InitializeMapper(),
                () => this.UpdateDbVersion(version));
        }

        /// <summary>
        /// Initialize database using any read/write Stream (like MemoryStream)
        /// </summary>
        public LiteDatabase(Stream stream, ushort version = 0)
        {
            _engine = new LazyLoad<DbEngine>(
                () => new DbEngine(new StreamDiskService(stream), _log),
                () => this.InitializeMapper(),
                () => this.UpdateDbVersion(version));
        }

        /// <summary>
        /// Starts LiteDB database using full parameters
        /// </summary>
        public LiteDatabase(IDiskService diskService, ushort version = 0)
        {
            _engine = new LazyLoad<DbEngine>(
                () => new DbEngine(diskService, _log),
                () => this.InitializeMapper(),
                () => this.UpdateDbVersion(version));
        }

        public void Dispose()
        {
            if (_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}