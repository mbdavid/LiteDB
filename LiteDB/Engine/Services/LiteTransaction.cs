using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Represent a single transaction service. Need a new instance for each transaction
    /// </summary>
    public class LiteTransaction : IDisposable
    {
        // instances from Engine
        internal HeaderPage _header;
        internal WalService _wal;
        internal LockService _locker;
        internal FileService _dataFile;
        internal FileService _walFile;
        internal Logger _log;

        // transaction controls
        private Guid _transactionID;
        private ConcurrentDictionary<string, SnapShot> _snapshots = new ConcurrentDictionary<string, SnapShot>();

        // handle created pages during transaction (for rollback)
        private List<uint> _newPages = new List<uint>();

        // first sequence deleted page (FreePageID) 
        private HeaderPage _delHeaderPage = new HeaderPage();
        private uint _delLastPageID = uint.MaxValue;

        /// <summary>
        /// </summary>
        internal LiteTransaction(HeaderPage header, LockService locker, WalService wal, FileService dataFile, FileService walFile, Logger log)
        {
            _transactionID = Guid.NewGuid();

            _wal = wal;
            _log = log;

            // retain instances
            _header = header;
            _locker = locker;
            _wal = wal;
            _dataFile = dataFile;
            _walFile = walFile;
        }

        internal SnapShot CreateSnapshot(string collectionName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write pages into disk and confirm transaction in wal-index
        /// </summary>
        public void Commit()
        {
        }

        /// <summary>
        /// Transaction runs rollback if is a write-transaction with no commit (should be an error during transaction execution)
        /// </summary>
        public void Rollback()
        {
        }

        public void Dispose()
        {
        }
    }
}