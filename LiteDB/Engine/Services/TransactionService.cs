using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Manage all transaction and garantee concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        /// <summary>
        /// Max cache pages size - read or dirty. If Count pass this value cache will be clear on next checkpoint
        /// </summary>
        public const int MAX_CACHE_SIZE = 5000;

        private IDiskService _disk;
        private PageService _pager;
        private Logger _log;

        internal TransactionService(IDiskService disk, PageService pager, Logger log)
        {
            _disk = disk;
            _pager = pager;
            _log = log;
        }

        /// <summary>
        /// Checkpoint is a safe point to clear cache pages without loose pages references.
        /// Is called after each document insert/update/deleted/indexed/fetch from query
        /// </summary>
        public bool CheckPoint()
        {
            // works only when journal are enabled
            if (_disk.IsJournalEnabled && _pager.CacheSize >= MAX_CACHE_SIZE)
            {
                _log.Write(Logger.CACHE, "cache checkpoint reached at {0} pages in cache", _pager.CacheSize);

                // write all dirty pages in data file (journal 
                foreach (var page in _pager.GetDirtyPages())
                {
                    // first write in journal file original data
                    _disk.WriteJournal(page.PageID, page.DiskData);

                    // then writes no datafile new changed pages
                    _disk.WritePage(page.PageID, page.WritePage());
                }
                
                // empty all cache pages
                _pager.ClearCache(false);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Save all dirty pages to disk
        /// </summary>
        public void Commit()
        {
            // get header page
            var header = _pager.GetPage<HeaderPage>(0);

            // set final datafile length (optimize page writes)
            _disk.SetLength(BasePage.GetSizeOfPages(header.LastPageID + 1));

            // write all dirty pages in data file
            foreach (var page in _pager.GetDirtyPages())
            {
                // first write in journal file original data
                _disk.WriteJournal(page.PageID, page.DiskData);

                // then writes no datafile new changed pages
                _disk.WritePage(page.PageID, page.WritePage());

                // mark page as clear (is now saved in disk)
                page.IsDirty = false;
            }

            // discard journal file
            _disk.ClearJournal();
        }

        /// <summary>
        /// Clear cache, discard journal file
        /// </summary>
        public void Rollback()
        {
            // clear all dirty pages from memory
            _pager.ClearCache(false);

            // recovery (if exists) journal file
            _disk.Recovery();
        }
    }
}