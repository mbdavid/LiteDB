using System;
using LiteDB.Interfaces;

namespace LiteDB
{
    /// <summary>
    /// A internal class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only
    /// </summary>
    public partial class DbEngine : IDisposable
    {
        #region Services instances

        private Logger _log;

        private CacheService _cache;

        private IDiskService _disk;

        private PageService _pager;

        private TransactionService _transaction;

        private IndexService _indexer;

        private DataService _data;

        private CollectionService _collections;

        public DbEngine(IDiskService disk, Logger log)
        {
            // initialize disk service and check if database exists
            var isNew = disk.Initialize();

            // new database? create new datafile
            if (isNew)
            {
                disk.CreateNew();
            }

            _log = log;
            _disk = disk;

            // initialize all services
            _cache = new CacheService();
            _pager = new PageService(_disk, _cache);
            _indexer = new IndexService(_pager);
            _data = new DataService(_pager);
            _collections = new CollectionService(_pager, _indexer, _data);
            _transaction = new TransactionService(_disk, _pager, _cache);
        }

        #endregion Services instances

        /// <summary>
        /// Get the collection page only when nedded. Gets from pager always to garantee that wil be the last (in case of clear cache will get a new one - pageID never changes)
        /// </summary>
        private CollectionPage GetCollectionPage(string name, bool addIfNotExits)
        {
            // search my page on collection service
            var col = _collections.Get(name);

            if (col == null && addIfNotExits)
            {
                _log.Write(Logger.COMMAND, "create new collection '{0}'", name);

                col = _collections.Add(name);
            }

            return col;
        }

        /// <summary>
        /// Starts a new write exclusive transaction
        /// </summary>
        public void BeginTrans()
        {
            _transaction.Begin(false);
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        public void Commit()
        {
            _transaction.Complete();
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void Rollback()
        {
            _transaction.Abort();
        }

        /// <summary>
        /// Encapsulate a read only transaction
        /// </summary>
        private T ReadTransaction<T>(string colName, Func<CollectionPage, T> action)
        {
            try
            {
                _transaction.Begin(true);

                var col = this.GetCollectionPage(colName, false);

                var result = action(col);

                _transaction.Complete();

                return result;
            }
            catch (Exception ex)
            {
                _log.Write(Logger.ERROR, ex.Message);
                _transaction.Abort();
                throw;
            }
        }

        /// <summary>
        /// Encapsulate read/write transaction
        /// </summary>
        private T WriteTransaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            try
            {
                _transaction.Begin(false);

                var col = this.GetCollectionPage(colName, addIfNotExists);

                var result = action(col);

                _transaction.Complete();

                return result;
            }
            catch (Exception ex)
            {
                _log.Write(Logger.ERROR, ex.Message);
                _transaction.Abort();
                throw;
            }
        }

        public void Dispose()
        {
            _disk.Dispose();
        }
    }
}