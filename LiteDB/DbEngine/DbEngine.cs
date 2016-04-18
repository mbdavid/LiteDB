using System;

namespace LiteDB
{
    /// <summary>
    /// A internal class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only
    /// </summary>
    internal partial class DbEngine : IDisposable
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

        private object _locker = new object();

        public DbEngine(IDiskService disk, Logger log)
        {
            // initialize disk service and check if database exists
            var isNew = disk.Initialize();

            // new database? create new database
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

            // check user verion
        }

        #endregion Services instances

        /// <summary>
        /// Get the collection page only when nedded. Gets from pager always to garantee that wil be the last (in case of clear cache will get a new one - pageID never changes)
        /// </summary>
        private CollectionPage GetCollectionPage(string name, bool addIfNotExits)
        {
            // before get a collection, avoid dirty reads
            _transaction.AvoidDirtyRead();

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
        /// Encapsulate all transaction commands in same data structure
        /// </summary>
        private T Transaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            lock (_locker)
            {
                try
                {
                    _transaction.Begin();

                    var col = this.GetCollectionPage(colName, addIfNotExists);

                    var result = action(col);

                    _transaction.Commit();

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);
                    _transaction.Rollback();
                    throw;
                }
            }
        }

        public void Dispose()
        {
            _disk.Dispose();
        }
    }
}