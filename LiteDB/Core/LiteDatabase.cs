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
        #region Properties + Ctor

        internal CacheService Cache { get; private set; }

        internal IDiskService Disk { get; private set; }

        internal PageService Pager { get; private set; }

        internal TransactionService Transaction { get; private set; }

        internal IndexService Indexer { get; private set; }

        internal DataService Data { get; private set; }

        internal CollectionService Collections { get; private set; }

        public BsonMapper Mapper { get; private set; }

        private LiteDatabase()
        {
            this.Mapper = BsonMapper.Global;
        }

        /// <summary>
        /// Starts LiteDB database using a connectionString
        /// </summary>
        public LiteDatabase(string connectionString)
            : this()
        {
            var str = new ConnectionString(connectionString);

            var filename = str.GetValue<string>("filename", "");
            var journal = str.GetValue<bool>("journal", true);
            var timeout = str.GetValue<TimeSpan>("timeout", new TimeSpan(0, 1, 0));
            var readOnly = str.GetValue<bool>("readonly", false);
            var password = str.GetValue<string>("password", null);

            if(string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException("filename");

            this.Disk = new FileDiskService(filename, journal, timeout, readOnly, password);

            this.Initialize();
        }

        /// <summary>
        /// Starts LiteDB database using full parameters
        /// </summary>
        public LiteDatabase(IDiskService diskService, BsonMapper mapper)
        {
            this.Disk = diskService;
            this.Mapper = mapper;
        }

        /// <summary>
        /// Initialize database engine - starts all services and open datafile
        /// </summary>
        private void Initialize()
        {
            var isNew = this.Disk.Initialize();

            if(isNew)
            {
                this.Disk.WritePage(0, new HeaderPage().WritePage());
            }

            this.Cache = new CacheService();

            this.Pager = new PageService(this.Disk, this.Cache);

            this.Indexer = new IndexService(this.Pager);

            this.Data = new DataService(this.Pager);

            this.Collections = new CollectionService(this.Pager, this.Indexer, this.Data);

            this.Transaction = new TransactionService(this.Disk, this.Cache);
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
            return new LiteCollection<T>(this, name);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public LiteCollection<BsonDocument> GetCollection(string name)
        {
            return new LiteCollection<BsonDocument>(this, name);
        }

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public IEnumerable<string> GetCollectionNames()
        {
            this.Transaction.AvoidDirtyRead();

            return this.Collections.GetAll().Select(x => x.CollectionName);
        }

        /// <summary>
        /// Checks if a collection exists on database. Collection name is case unsensitive
        /// </summary>
        public bool CollectionExists(string name)
        {
            this.Transaction.AvoidDirtyRead();

            return this.Collections.Get(name) != null;
        }

        /// <summary>
        /// Drop a collection and all data + indexes
        /// </summary>
        public bool DropCollection(string name)
        {
            return this.GetCollection(name).Drop();
        }

        /// <summary>
        /// Rename a collection. Returns false if oldName does not exists or newName already exists
        /// </summary>
        public bool RenameCollection(string oldName, string newName)
        {
            return this.GetCollection(oldName).Rename(newName);
        }

        #endregion

        #region FileStorage

        private LiteFileStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteFileStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteFileStorage(this)); }
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

        public void Dispose()
        {
            this.Disk.Dispose();
            this.Cache.Dispose();
        }
    }
}
