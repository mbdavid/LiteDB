using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public enum TransactionMode { Read, Write, WriteHeader }

    /// <summary>
    /// Represent a single transaction service. Need a new instance for each transaction
    /// </summary>
    internal class TransactionService : IDisposable
    {
        // instances from Engine
        private WalService _wal;
        private Logger _log;

        // new instances
        private PageService _pager;
        private IndexService _index;
        private CollectionService _collection;
        private DataService _data;

        // transaction controls
        private TransactionMode _mode;
        private LockReadWrite _lockReadWrite;
        private CollectionPage _collectionPage;
        private bool _commited = false;

        // expose services
        public PageService Pager => _pager;
        public CollectionService Collection => _collection;
        public IndexService Indexer => _index;
        public DataService Data => _data;
        public CollectionPage CollectionPage => _collectionPage;

        /// <summary>
        /// Create new Transaction - if collection == null is read transaction
        /// </summary>
        public TransactionService(TransactionMode mode, string collection, bool addIfNotExists, LockService locker, WalService wal, FileService datafile, FileService walfile, Logger log)
        {
            _mode = mode;
            _wal = wal;
            _log = log;

            // load services
            _pager = new PageService(wal, datafile, walfile, _log);
            _data = new DataService(_pager, _log);
            _index = new IndexService(_pager, _log);
            _collection = new CollectionService(_pager, _index, _data, _log);

            // need lock before get current read version
            _lockReadWrite = locker.Read();

            // if is write transaction, lock collection name
            if ((mode == TransactionMode.Write || mode == TransactionMode.WriteHeader) && collection != null)
            {
                locker.Write(_lockReadWrite, collection);
            }

            // if need lock header
            if (mode == TransactionMode.WriteHeader)
            {
                locker.Header(_lockReadWrite);
            }

            // start transaction and get current read version
            _pager.Initialize();

            // load collection page (or create one)
            if (collection != null)
            {
                _collectionPage = _collection.Get(collection);

                // if need create collection, must lock header too
                if (_collectionPage == null && addIfNotExists)
                {
                    locker.Header(_lockReadWrite);

                    // also, need clear loaded pages and reset read version (to garantee that we are with lastest header version)
                    _pager.Initialize();

                    // add new collection
                    _collectionPage = _collection.Add(collection);
                }
            }
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index
        /// </summary>
        public void Commit()
        {
            _pager.PersistTransaction();

            _pager.WalCommit();

            _commited = true;
        }

        /// <summary>
        /// Transaction runs rollback if is a write-transaction with no commit (should be an error during transaction execution)
        /// </summary>
        private void Rollback()
        {

        }

        public void Dispose()
        {
            // if is a write transaction and had no commit, do rollback 
            if (_commited == false && _mode != TransactionMode.Read)
            {
                this.Rollback();
            }

            // release lock
            _lockReadWrite.Dispose();
        }
    }
}