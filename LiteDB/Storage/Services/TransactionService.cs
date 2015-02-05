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
        private JournalService _journal;

        private int _level = 0;
        private DateTime _lastFileCheck = DateTime.Now;

        internal TransactionService(DiskService disk, CacheService cache, JournalService journal)
        {
            _disk = disk;
            _cache = cache;
            _journal = journal;
        }

        public bool IsInTransaction { get { return _level > 0; } }

        /// <summary>
        /// Starts a new transaction - lock database to garantee that only one processes is in a transaction
        /// </summary>
        public void Begin()
        {
            if (_level == 0)
            {
                // lock (or try to) datafile
                _disk.Lock();

                // get header page from DISK to check changeID
                var header = _disk.ReadPage<HeaderPage>(0);

                // if changeID was changed, file was changed by another process
                if (header.ChangeID != _cache.Header.ChangeID)
                {
                    _cache.Clear(header);
                }
            }

            _level++;
        }

        /// <summary>
        /// Abort a transaction is used when begin and has no changes yet - no writes, no checks
        /// </summary>
        public void Abort()
        {
            if (_level == 0) return;

            if (_level == 1)
            {
                _disk.UnLock();

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
                if (_cache.HasDirtyPages())
                {
                    // increase file changeID (back to 0 when overflow)
                    _cache.Header.ChangeID = _cache.Header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(_cache.Header.ChangeID + (ushort)1);

                    _cache.Header.IsDirty = true;

                    // first i will save all dirty pages to a recovery file
                    _journal.CreateJournalFile(() =>
                    {
                        // [transaction are now acepted] (all dirty pages are in journal file)
                        // journal file still open in exlcusive mode, let's persist pages
                        // if occurs a failure during "PersistDirtyPages" database will be recovered on next open

                        // inside this file will be all locked for avoid inconsistent reads
                        _disk.ProtectWriteFile(() =>
                        {
                            //command below can be run in an async task
                            //System.Threading.Tasks.Task.Factory.StartNew(() =>
                            //{
                                // persist all dirty pages
                                _cache.PersistDirtyPages();

                                // unlock datafile
                                _disk.UnLock();
                            //});
                        });
                    });
                }
                else
                {
                    // if not dirty pages, just unlock datafile
                    _disk.UnLock();
                }

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
            _cache.Clear(null); 

            // Unlock datafile
            _disk.UnLock();

            _level = 0;
        }

        /// <summary>
        /// This method must be called before read operation to avoid dirty reads.
        /// It's occurs when my cache contains pages that was changed in another process
        /// </summary>
        public void AvoidDirtyRead()
        {
            // if is in transaction pages are not dirty (begin trans was checked)
            if(this.IsInTransaction == true) return;

            // check if file changed only after 1 second from last check
            if (DateTime.Now.Subtract(_lastFileCheck).TotalMilliseconds < 1000) return;

            _lastFileCheck = DateTime.Now;

            // get header page from DISK to check changeID
            var header = _disk.ReadPage<HeaderPage>(0);

            // if changeID was changed, file was changed by another process
            if (header.ChangeID != _cache.Header.ChangeID)
            {
                _cache.Clear(header);
            }
        }
    }
}
