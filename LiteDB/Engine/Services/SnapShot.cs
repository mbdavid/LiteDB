using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Represent a single snapshot
    /// </summary>
    internal class Snapshot : IDisposable
    {
        // instances from Engine
        private LockService _locker;
        private WalService _wal;
        private FileService _dataFile;
        private FileService _walFile;
        private Logger _log;

        // new instances
        private PageService _pager;
        private IndexService _index;
        private CollectionService _collection;
        private DataService _data;

        // snapshot controls
        private SnapshotMode _mode;
        private CollectionPage _collectionPage;
        private string _collectionName;
        private bool _collectionPageLocked = false;

        // expose services
        public PageService Pager => _pager;
        public CollectionService Collection => _collection;
        public IndexService Indexer => _index;
        public DataService Data => _data;
        public CollectionPage CollectionPage => _collectionPage;

        public Snapshot(string collectionName, HeaderPage header, TransactionPages transPages, LockService locker, WalService wal, FileService dataFile, FileService walFile, Logger log)
        {
            _locker = locker;
            _wal = wal;
            _dataFile = dataFile;
            _walFile = walFile;
            _log = log;

            // always init a snapshot as read mode
            _mode = SnapshotMode.Read;

            // load services
            _pager = new PageService(header, transPages, wal, dataFile, walFile, log);
            _data = new DataService(_pager, log);
            _index = new IndexService(_pager, log);
            _collection = new CollectionService(_pager, _index, _data, log);

            // initialize pager and get read version
            _pager.Initialize();

            // get collection page (or null if collection not exists)
            _collectionPage = _collection.Get(collectionName);
        }

        /// <summary>
        /// Enter snapshot in write mode (if not already in write mode)
        /// </summary>
        public void WriteMode(bool addIfNotExits)
        {
            if (_mode == SnapshotMode.Read)
            {
                _locker.EnterReserved(_collectionName);

                _mode = SnapshotMode.Write;
            }

            if (_collectionPage == null && addIfNotExits)
            {
                // use '#collection_page' name for collection page lock
                _locker.EnterReserved("#collection_page");

                _collectionPageLocked = true;

                _pager.Initialize();

                _collectionPage = _collection.Add(_collectionName);
            }
        }

        /// <summary>
        /// Unlock all collections locks on dispose
        /// </summary>
        public void Dispose()
        {
            if (_collectionPageLocked)
            {
                _locker.ExitReserved("#collection_page");
            }

            if (_mode == SnapshotMode.Write)
            {
                _locker.ExitReserved(_collectionName);
            }

            _locker.ExitRead(_collectionName);
        }
    }
}