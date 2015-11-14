using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB database. Used for create a LiteDB instance and use all storage resoures. It's the database connection
    /// </summary>
    public partial class LiteDatabase : IDisposable
    {
        private DbEngine _engine;

        private BsonMapper _mapper;

        private Logger _log = new Logger();

        public BsonMapper Mapper { get { return _mapper; } }

        public Logger Log { get { return _log; } }

        /// <summary>
        /// Starts LiteDB database using a connectionString
        /// </summary>
        public LiteDatabase(string connectionString)
        {
            var str = new ConnectionString(connectionString);

            var filename = str.GetValue<string>("filename", "");
            var journal = str.GetValue<bool>("journal", true);
            var timeout = str.GetValue<TimeSpan>("timeout", new TimeSpan(0, 1, 0));
            var readOnly = str.GetValue<bool>("readonly", false);
            var password = str.GetValue<string>("password", null);

            if(string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException("filename");

            // initialize engine creating a new FileDiskService for data access
            _engine = new DbEngine(new FileDiskService(filename, journal, timeout, readOnly, password, _log));
            _mapper = BsonMapper.Global;
        }

        public LiteDatabase(Stream stream)
        {
            // initialize engine using StreamDisk
            _engine = new DbEngine(new StreamDiskService(stream));
            _mapper = BsonMapper.Global;
        }

        /// <summary>
        /// Starts LiteDB database using full parameters
        /// </summary>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper)
        {
            _engine = new DbEngine(diskService);
            _mapper = mapper;
        }

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
            where T : new()
        {
            return new LiteCollection<T>(name, _engine, _mapper, _log);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            return new LiteCollection<BsonDocument>(name, _engine, _mapper, _log);
        }

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            return _engine.GetCollectionNames();
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case unsensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            return _engine.GetCollectionNames().Contains(name);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            return _engine.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            return _engine.RenameCollection(oldName, newName);
        }

        #endregion

        #region FileStorage

        private LiteFileStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteFileStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteFileStorage(_engine)); }
        }

        #endregion

        #region Shell

        private LiteShell _shell = null;

        /// <summary>
        /// Run a shell command in current database. Returns a BsonValue as result
        /// </summary>
        public BsonValue RunCommand(string command)
        {
            if (_shell == null)
            {
                _shell = new LiteShell(this);
            }
            return _shell.Run(command);
        }

        #endregion

        #region Dump

        internal string DumpPages(uint startPage = 0, uint endPage = uint.MaxValue)
        {
            return _engine.DumpPages(startPage, endPage).ToString();
        }

        internal string DumpIndex(string colName, string field)
        {
            return _engine.DumpIndex(colName, field).ToString();
        }

        #endregion

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}
