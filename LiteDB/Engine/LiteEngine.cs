using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB engine. Used for create a LiteDB instance and use all storage resoures. It's the database connection engine.
    /// </summary>
    public partial class LiteEngine : IDisposable
    {
        #region Properties + Ctor

        public ConnectionString ConnectionString { get; private set; }

        internal RecoveryService Recovery { get; private set; }

        internal CacheService Cache { get; private set; }

        internal DiskService Disk { get; private set; }

        internal PageService Pager { get; private set; }

        internal JournalService Journal { get; private set; }

        internal TransactionService Transaction { get; private set; }

        internal IndexService Indexer { get; private set; }

        internal DataService Data { get; private set; }

        internal CollectionService Collections { get; private set; }

        /// <summary>
        /// Starts LiteDB engine. Open database file or create a new one if not exits
        /// </summary>
        /// <param name="connectionString">Full filename or connection string</param>
        public LiteEngine(string connectionString)
        {
            this.ConnectionString = new ConnectionString(connectionString);

            if (!File.Exists(ConnectionString.Filename))
            {
                CreateNewDatabase(ConnectionString);
            }

            this.Recovery = new RecoveryService(this.ConnectionString);

            this.Recovery.TryRecovery();

            this.Disk = new DiskService(this.ConnectionString);

            this.Cache = new CacheService(this.Disk);

            this.Pager = new PageService(this.Disk, this.Cache);

            this.Journal = new JournalService(this.ConnectionString, this.Cache);

            this.Indexer = new IndexService(this.Cache, this.Pager);

            this.Transaction = new TransactionService(this.Disk, this.Cache, this.Journal);

            this.Data = new DataService(this.Disk, this.Cache, this.Pager);

            this.Collections = new CollectionService(this.Pager, this.Indexer);

            this.UpdateDatabaseVersion();
        }

        #endregion

        #region Collections

        /// <summary>
        /// Get a collection using a strong typed POCO class. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public Collection<T> GetCollection<T>(string name)
            where T : new()
        {
            return new Collection<T>(this, name);
        }

        /// <summary>
        /// Get a collection using a generic BsonDocument. If collection does not exits, create a new one.
        /// </summary>
        /// <param name="name">Collection name (case insensitive)</param>
        public Collection<BsonDocument> GetCollection(string name)
        {
            return new Collection<BsonDocument>(this, name);
        }

        /// <summary>
        /// Get all collections name inside this database.
        /// </summary>
        public string[] GetCollections()
        {
            this.Transaction.AvoidDirtyRead();

            return this.Collections.GetAll().Select(x => x.CollectionName).ToArray();
        }

        #endregion

        #region UserVersion

        /// <summary>
        /// Virtual method for update database when a new version (from coneection string) was setted
        /// </summary>
        /// <param name="newVersion">The new database version</param>
        protected virtual void OnVersionUpdate(int newVersion)
        {
        }

        /// <summary>
        /// Update database version, when necessary
        /// </summary>
        private void UpdateDatabaseVersion()
        {
            // not necessary "AvoidDirtyRead" because its calls from ctor
            var current = this.Cache.Header.UserVersion;
            var recent = this.ConnectionString.UserVersion;

            // there is no updates
            if (current == recent) return;

            // start a transaction
            this.Transaction.Begin();

            try
            {
                for (var newVersion = current + 1; newVersion <= recent; newVersion++)
                {
                    OnVersionUpdate(newVersion);

                    this.Cache.Header.UserVersion = newVersion;
                }

                this.Cache.Header.IsDirty = true;
                this.Transaction.Commit();

            }
            catch (Exception ex)
            {
                this.Transaction.Rollback();
                throw ex;
            }
        }

        #endregion

        #region MaxFileLength

        /// <summary>
        /// Set database max datafile length. Minumum is 256Kb. Default is long.MaxValue.
        /// </summary>
        public void SetMaxFileLength(long size)
        {
            if (size < (256 * 1024)) throw new ArgumentException("MaxFileLength must be bigger than 262.144 (256Kb)");

            // start transcation - all data are garanteed
            this.Transaction.Begin();

            if (this.Cache.Header.MaxFileLength != size)
            {
                this.Transaction.Abort();
                return;
            }

            try
            {
                this.Cache.Header.MaxFileLength = size;
                this.Cache.Header.IsDirty = true;

                if (this.Cache.Header.MaxPageID > this.Cache.Header.LastPageID) throw new ArgumentException("File size is bigger than " + size);

                this.Transaction.Commit();
            }
            catch (Exception ex)
            {
                this.Transaction.Rollback();
                throw ex;
            }
        }

        #endregion

        #region Files Storage

        private FileStorage _files = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public FileStorage FileStorage
        {
            get { return _files ?? (_files = new FileStorage(this)); }
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

        #region Database info

        public BsonObject GetDatabaseInfo()
        {
            this.Transaction.AvoidDirtyRead();

            var info = new BsonObject();

            info["filename"] = this.ConnectionString.Filename;
            info["journal"] = this.ConnectionString.JournalEnabled;
            info["timeout"] = this.ConnectionString.Timeout.TotalSeconds;
            info["version"] = this.Cache.Header.UserVersion;
            info["changeID"] = this.Cache.Header.ChangeID;
            info["maxFileLength"] = this.Cache.Header.MaxFileLength == long.MaxValue ? BsonValue.Null : new BsonValue(this.Cache.Header.MaxFileLength);
            info["fileLength"] = this.Cache.Header.LastPageID * BasePage.PAGE_SIZE;
            info["lastPageID"] = this.Cache.Header.LastPageID;
            info["pagesInCache"] = this.Cache.PagesInCache;
            info["dirtyPages"] = this.Cache.HasDirtyPages();

            return info;
        }

        #endregion

        #region Statics methods

        /// <summary>
        /// Create a empty database ready to be used using connectionString as parameters
        /// </summary>
        private static void CreateNewDatabase(ConnectionString connectionString)
        {
            using (var stream = File.Create(connectionString.Filename))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // creating header + master collection
                    DiskService.WritePage(writer, new HeaderPage { PageID = 0, LastPageID = 1 });
                    DiskService.WritePage(writer, new CollectionPage { PageID = 1, CollectionName = "_master" });
                }
            }
        }

        #endregion

        public void Dispose()
        {
            Disk.Dispose();
        }
    }
}
