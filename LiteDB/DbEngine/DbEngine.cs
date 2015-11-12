using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// A internal class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only
    /// </summary>
    internal partial class DbEngine : IDisposable
    {
        #region Services instances

        private CacheService _cache;

        private IDiskService _disk;

        private PageService _pager;

        private TransactionService _transaction;

        private IndexService _indexer;

        private DataService _data;

        private CollectionService _collections;

        private Dictionary<string, uint> _collectionPages = new Dictionary<string, uint>();

        private object _locker = new object();

        public DbEngine(IDiskService disk)
        {
            _disk = disk;

            var isNew = _disk.Initialize();

            if(isNew)
            {
                _disk.WritePage(0, new HeaderPage().WritePage());
            }

            _cache = new CacheService();
            _pager = new PageService(_disk, _cache);
            _indexer = new IndexService(_pager);
            _data = new DataService(_pager);
            _collections = new CollectionService(_pager, _indexer, _data);
            _transaction = new TransactionService(_disk, _pager, _cache);
        }

        #endregion

        /// <summary>
        /// Get the collection page only when nedded. Gets from pager always to garantee that wil be the last (in case of clear cache will get a new one - pageID never changes)
        /// </summary>
        private CollectionPage GetCollectionPage(string name, bool addIfNotExits)
        {
            uint pageID;

            // read if data file was changed by another process - if changed, reset my collection pageId cache
            if(_transaction.AvoidDirtyRead())
            {
                _collectionPages = new Dictionary<string, uint>();
            }

            // check if pageID is in my dictionary
            if(_collectionPages.TryGetValue(name, out pageID))
            {
                return _pager.GetPage<CollectionPage>(pageID);
            }
            else 
            {
                // search my page on collection service
                var col = _collections.Get(name);

                if(col == null && addIfNotExits)
                {
                    col = _collections.Add(name);
                }

                if(col != null)
                {
                    _collectionPages.Add(name, col.PageID);

                    return _pager.GetPage<CollectionPage>(col.PageID);
                }
            }

            return null;
        }

        /// <summary>
        /// Encapsulate all transaction commands in same data structure
        /// </summary>
        private T Transaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            lock(_locker)
            try
            {
                _transaction.Begin();

                var col = this.GetCollectionPage(colName, addIfNotExists);

                var result = action(col);

                _transaction.Commit();

                return result;
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
        }

        public void Dispose()
        {
            _disk.Dispose();
        }
    }
}
