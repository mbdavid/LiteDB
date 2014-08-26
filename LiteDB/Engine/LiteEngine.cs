using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The LiteDB engine. Used for create a LiteDB instance and use all resoures
    /// </summary>
    public partial class LiteEngine : IDisposable
    {
        public ConnectionString ConnectionString { get; private set; }

        internal RecoveryService Recovery { get; private set; }

        internal CacheService Cache { get; private set; }

        internal DiskService Disk { get; private set; }

        internal PageService Pager { get; private set; }

        internal RedoService Redo { get; private set; }

        internal TransactionService Transaction { get; private set; }

        internal IndexService Indexer { get; private set; }

        internal DataService Data { get; private set; }

        internal CollectionService Collections { get; private set; }

        /// <summary>
        /// Constructor - Start all classes services
        /// </summary>
        public LiteEngine(string connectionString)
        {
            this.ConnectionString = new ConnectionString(connectionString);

            if (!File.Exists(ConnectionString.Filename))
                CreateNewDatabase(ConnectionString);

            this.Recovery = new RecoveryService(this.ConnectionString);

            this.Recovery.TryRecovery();

            this.Disk = new DiskService(this.ConnectionString);

            this.Cache = new CacheService(this.Disk);

            this.Pager = new PageService(this.Disk, this.Cache, this.ConnectionString);

            this.Redo = new RedoService(this.Recovery, this.Cache, this.ConnectionString.JournalEnabled);

            this.Indexer = new IndexService(this.Cache, this.Pager);

            this.Transaction = new TransactionService(this.Disk, this.Cache, this.Redo);

            this.Data = new DataService(this.Disk, this.Cache, this.Pager);

            this.Collections = new CollectionService(this.Pager, this.Indexer);
        }

        #region Collections

        public Collection<T> GetCollection<T>(string name)
            where T : new()
        {
            return new Collection<T>(this, name);
        }

        public Collection<BsonDocument> GetCollection(string name)
        {
            return new Collection<BsonDocument>(this, name);
        }

        public bool DropCollection(string name)
        {
            return this.Collections.Drop(name);
        }

        #endregion

        #region UserVersion

        public int UserVersion
        {
            get { return this.Cache.Header.UserVersion; }
            set
            {
                if (this.Cache.Header.UserVersion != value)
                {
                    this.Transaction.Begin();

                    try
                    {
                        this.Cache.Header.UserVersion = value;
                        this.Cache.Header.IsDirty = true;

                        this.Transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        this.Transaction.Rollback();
                        throw ex;
                    }
                }
            }
        }

        #endregion

        #region File Storage

        private FilesCollection _files = null;

        /// <summary>
        /// Returns a special collection for storage files inside datafile
        /// </summary>
        public FilesCollection Files
        {
            get { return _files ?? (_files = new FilesCollection(this)); }
        }

        #endregion

        #region Transaction

        public void BeginTrans()
        {
            this.Transaction.Begin();
        }

        public void Commit()
        {
            this.Transaction.Commit();
        }

        public void Rollback()
        {
            this.Transaction.Rollback();
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
