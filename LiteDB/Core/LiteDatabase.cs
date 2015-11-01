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

        public BsonMapper Mapper { get; set; }

        /// <summary>
        /// Starts LiteDB database. Open database file or create a new one if not exits
        /// </summary>
        public LiteDatabase(string filename)
        {
            this.Disk = new FileDiskService(filename);

            this.Disk.Initialize();

            this.Mapper = BsonMapper.Global;

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
            this.Transaction.Begin();

            try
            {
                var col = this.Collections.Get(oldName);

                if (col == null || this.CollectionExists(newName))
                {
                    this.Transaction.Abort();
                    return false;
                }

                col.CollectionName = newName;
                col.IsDirty = true;

                this.Transaction.Commit();
            }
            catch
            {
                this.Transaction.Rollback();
                throw;
            }

            return true;
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

        #region Transaction

        /// <summary>
        /// Starts a new transaction. After this command, all write operations will be first in memory and will persist on disk
        /// only when call Commit() method. If any error occurs, a Rollback() method will run.
        /// </summary>
        public void BeginTrans()
        {
            this.Transaction.Begin();
        }

        /// <summary>
        /// Persist all changes on disk. Always use this method to finish your changes on database
        /// </summary>
        public void Commit()
        {
            this.Transaction.Commit();
        }

        /// <summary>
        /// Cancel all write operations and keep datafile as is before BeginTrans() called.
        /// Rollback are implicit on a database operation error, so you do not need call for database errors (only on business rules).
        /// </summary>
        public void Rollback()
        {
            this.Transaction.Rollback();
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
