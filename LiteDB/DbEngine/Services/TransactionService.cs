using System;
using LiteDB.Interfaces;

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
        private int _level = 0;

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
        public void Begin(bool readOnly)
        {
            _level++;

            _disk.Open(readOnly);
        }

        /// <summary>
        /// Complete transaction commit dirty pages and closing data file
        /// </summary>
        public void Complete()
        {
            if (--_level > 0) return;

            if (_cache.HasDirtyPages)
            {
                // save dirty pages
                this.Save();

                // delete journal file - datafile is consist here
                _disk.DeleteJournal();
            }

            // clear all pages in cache
            _cache.Clear();

            // close datafile
            _disk.Close();
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

        /// <summary>
        /// Stop transaction, discard journal file and close database
        /// </summary>
        public void Abort()
        {
            _level = 0;

            // clear all pages from memory (return true if has dirty pages on cache)
            if (_cache.Clear())
            {
                // if has dirty page, has journal file - delete it (is not valid)
                _disk.DeleteJournal();
            }

            // release datafile
            _disk.Close();
        }
    }
}