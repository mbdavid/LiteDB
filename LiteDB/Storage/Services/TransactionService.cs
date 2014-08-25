using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Manage all transaction and garantee concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        private DiskService _disk;
        private CacheService _cache;
        private RedoService _redo;

        private int _level = 0;

        internal TransactionService(DiskService disk, CacheService cache, RedoService redo)
        {
            _disk = disk;
            _cache = cache;
            _redo = redo;
        }

        public bool IsInTransaction { get { return _level > 0; } }

        /// <summary>
        /// Starts a new transaction - lock database to garantee that only one processes is in a transaction
        /// Returns true if cache clears
        /// </summary>
        public bool Begin()
        {
            var ret = false;

            if (_level == 0)
            {
                // Get header page from DISK to check changeID
                var header = _disk.ReadPage<HeaderPage>(0);

                // If versionID was changed, clear cache
                if (header.ChangeID != _cache.Header.ChangeID)
                {
                    _cache.Clear();
                    ret = true;
                }

                // Lock (or try to) datafile
                _disk.Lock();

                _redo.CheckRedoFile(_disk);
            }

            _level++;

            return ret;
        }

        /// <summary>
        /// Commit the transaction - increese 
        /// </summary>
        public void Commit()
        {
            if (_level == 0) return;

            if (_level == 1)
            {
                // Increase file changeID (back to 0 with overflow)
                _cache.Header.ChangeID = _cache.Header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(_cache.Header.ChangeID + (ushort)1);

                _cache.Header.IsDirty = true;

                // first i will save all dirty pages to a recovery file
                _redo.CreateRedoFile();

                // Before complete redo file, transaction will be rollback in case of failure.
                // At this point, transaction are acepted! if failure during write pages on
                // disk, recovery mode will ajust data file
                // The ideal is return now to user with "complete transaction", but it will be
                // a problem in subsequente transaction.

                // Persist all dirty pages
                _cache.PersistDirtyPages();

                // Delete redo file
                _redo.DeleteRedoFile();

                // Unlock datafile
                _disk.UnLock();

                _level = 0;
            }
            else
            {
                _level--;
            }
        }

        public void Rollback()
        {
            if (_level == 0) return;

            // Clear all pages from memory
            _cache.Clear(); 

            // Unlock datafile
            _disk.UnLock();

            _level = 0;
        }
    }
}
