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
        private IDiskService _disk;
        private CacheService _cache;

        private int _level = 0;

        internal TransactionService(IDiskService disk, CacheService cache)
        {
            _disk = disk;
            _cache = cache;

            // _cache.OnPageChange((page) => _disk.PageChange(page.PageID, page.OriginalBuffer);
        }

        public bool IsInTransaction { get { return _level > 0; } }

        /// <summary>
        /// Starts a new transaction - lock database to garantee that only one processes is in a transaction
        /// </summary>
        public void Begin()
        {
            if (_level == 0)
            {
                this.AvoidDirtyRead();

                // lock (or try to) datafile
                _disk.Lock();
            }

            _level++;
        }

        /// <summary>
        /// Abort a transaction is used when begin and has no changes yet - no writes, no checks (it's simple than rollback)
        /// </summary>
        public void Abort()
        {
            if (_level == 0) return;

            if (_level == 1)
            {
                _disk.Unlock();

                _level = 0;
            }
            else
            {
                _level--;
            }
        }

        /// <summary>
        /// Commit the transaction - increese 
        /// </summary>
        public void Commit()
        {
            if (_level == 0) return;

            if (_level == 1)
            {
                if (_cache.HasDirtyPages)
                {
                    var header = _cache.GetPage<HeaderPage>(0);

                    // increase file changeID (back to 0 when overflow)
                    header.ChangeID = header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(header.ChangeID + (ushort)1);

                    _cache.AddPage(header, true);

                    _disk.StartWrite();

                    foreach(var page in _cache.GetDirtyPages())
                    {
                        Console.WriteLine("save dirty page " + page.PageID);
                        _disk.WritePage(page.PageID, page.WritePage());
                    }

                    _disk.EndWrite();

                    _cache.ClearDirty();
                }

                // unlock datafile
                _disk.Unlock();

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

            // clear all pages from memory
            _cache.Clear(); 

            // tell disk service there is no more writes
            _disk.EndWrite();

            // unlock datafile
            _disk.Unlock();

            _level = 0;
        }

        /// <summary>
        /// This method must be called before read/write operation to avoid dirty reads.
        /// It's occurs when my cache contains pages that was changed in another process
        /// </summary>
        public void AvoidDirtyRead()
        {
            // if is in transaction pages are not dirty (begin trans was checked)
            if(this.IsInTransaction == true) return;

            var cache = _cache.GetPage<HeaderPage>(0);

            if (cache == null) return;

            // read change direct from disk
            var change = _disk.GetChangeID();

            // if changeID was changed, file was changed by another process - clear all cache
            if (cache.ChangeID != change)
            {
                _cache.Clear();
            }
        }
    }
}
