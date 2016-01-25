using System;

namespace LiteDB
{
    /// <summary>
    /// Manage all transaction and garantee concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        private IDiskService _disk;
        private PageService _pager;
        private CacheService _cache;
        private bool _trans = false;

        internal TransactionService(IDiskService disk, PageService pager, CacheService cache)
        {
            _disk = disk;
            _pager = pager;
            _cache = cache;
            _cache.MarkAsDirtyAction = (page) => _disk.WriteJournal(page.PageID, page.DiskData);
            _cache.DirtyRecicleAction = () => this.Save();
        }

        /// <summary>
        /// Starts a new transaction - lock database to garantee that only one processes is in a transaction
        /// </summary>
        public void Begin()
        {
            if (_trans == true) throw new Exception("Begin transaction already exists");

            // lock (or try to) datafile
            _disk.Lock();

            _trans = true;
        }

        /// <summary>
        /// Commit the transaction - increese
        /// </summary>
        public void Commit()
        {
            if (_trans == false) throw new Exception("No begin transaction");

            if (_cache.HasDirtyPages)
            {
                // save dirty pages
                this.Save();

                // clear all pages in cache
                _cache.Clear();
            }

            // delete journal file - datafile is consist here
            _disk.DeleteJournal();

            // unlock datafile
            _disk.Unlock();

            _trans = false;
        }

        /// <summary>
        /// Save all dirty pages to disk - do not touch on lock disk
        /// </summary>
        private void Save()
        {
            // get header and mark as dirty
            var header = _pager.GetPage<HeaderPage>(0, true);

            // increase file changeID (back to 0 when overflow)
            header.ChangeID = header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(header.ChangeID + (ushort)1);

            // set final datafile length (optimize page writes)
            _disk.SetLength(BasePage.GetSizeOfPages(header.LastPageID + 1));

            // write all dirty pages in data file
            foreach (var page in _cache.GetDirtyPages())
            {
                _disk.WritePage(page.PageID, page.WritePage());
            }
        }

        public void Rollback()
        {
            if (_trans == false) return;

            // clear all pages from memory (return true if has dirty pages on cache)
            if (_cache.Clear())
            {
                // if has dirty page, has journal file - delete it (is not valid)
                _disk.DeleteJournal();
            }

            // unlock datafile
            _disk.Unlock();

            _trans = false;
        }

        /// <summary>
        /// This method must be called before read/write operation to avoid dirty reads.
        /// It's occurs when my cache contains pages that was changed in another process
        /// </summary>
        public bool AvoidDirtyRead()
        {
            var cache = (HeaderPage)_cache.GetPage(0);

            if (cache == null) return false;

            // read change direct from disk
            var change = _disk.GetChangeID();

            // if changeID was changed, file was changed by another process - clear all cache
            if (cache.ChangeID != change)
            {
                _cache.Clear();
                return true;
            }

            return false;
        }
    }
}