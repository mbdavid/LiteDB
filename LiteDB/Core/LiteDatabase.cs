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
        private Lazy<DbEngine> _engine;

        private BsonMapper _mapper;

        private Logger _log = new Logger();

        public BsonMapper Mapper { get { return _mapper; } }

        public Logger Log { get { return _log; } }

        /// <summary>
        /// Starts LiteDB database using a connectionString for filesystem database
        /// </summary>
        public LiteDatabase(string connectionString)
        {
            _mapper = BsonMapper.Global;
            _engine = new Lazy<DbEngine>(
                () => new DbEngine(new FileDiskService(connectionString, _log), _log), 
                false);
        }

        /// <summary>
        /// Initialize database using any read/write Stream (like MemoryStream)
        /// </summary>
        public LiteDatabase(Stream stream)
        {
            _mapper = BsonMapper.Global;
            _engine = new Lazy<DbEngine>(
                () => new DbEngine(new StreamDiskService(stream), _log),
                false);
        }

        /// <summary>
        /// Starts LiteDB database using full parameters
        /// </summary>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper)
        {
            _mapper = mapper;
            _engine = new Lazy<DbEngine>(
                () => new DbEngine(diskService, _log),
                false);
        }

        #region Collections

        /// <summary>
        /// Get a collection using a entity class as strong typed document. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<T> GetCollection<T>(string name)
            where T : new()
        {
            return new LiteCollection<T>(name, _engine.Value, _mapper, _log);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            return new LiteCollection<BsonDocument>(name, _engine.Value, _mapper, _log);
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
            return _engine.Value.GetCollectionNames().Contains(name);
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            return _engine.Value.DropCollection(name);
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            return _engine.Value.RenameCollection(oldName, newName);
        }

        #endregion

        #region FileStorage

        private LiteFileStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteFileStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteFileStorage(_engine.Value)); }
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
            return _engine.Value.DumpPages(startPage, endPage).ToString();
        }

        internal string DumpIndex(string colName, string field)
        {
            return _engine.Value.DumpIndex(colName, field).ToString();
        }

        #endregion

        /// <summary>
        /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced. Use an In-memory database as temporary area.
        /// </summary>
        public int Shrink()
        {
            return _engine.Value.Shrink(new StreamDiskService(new MemoryStream()));
        }

        /// <summary>
        /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced. Use a temp file as temporary database
        /// </summary>
        public int Shrink(string tempFilename)
        {
            if (string.IsNullOrEmpty(tempFilename)) throw new ArgumentNullException("tempFilename");
            if (File.Exists(tempFilename)) throw new ArgumentException("tempFilename already exists");

            try
            {
                var diff = _engine.Value.Shrink(new FileDiskService("filename=" + tempFilename + ";journal=false", new Logger()));

                return diff;
            }
            finally
            {
                File.Delete(tempFilename);
            }
        }

        public void Dispose()
        {
            if(_engine.IsValueCreated) _engine.Value.Dispose();
        }
    }
}
