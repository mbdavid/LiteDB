using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Manage all transaction and garantee concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        private IDiskService _disk;
        private AesEncryption _crypto;
        private PageService _pager;
        private Logger _log;
        private int _cacheSize;

        internal TransactionService(IDiskService disk, AesEncryption crypto, PageService pager, int cacheSize, Logger log)
        {
            _disk = disk;
            _crypto = crypto;
            _pager = pager;
            _cacheSize = cacheSize;
            _log = log;
        }

        /// <summary>
        /// Checkpoint is a safe point to clear cache pages without loose pages references.
        /// Is called after each document insert/update/deleted/indexed/fetch from query
        /// </summary>
        public void CheckPoint()
        {
            // works only when journal are enabled
            if (_disk.IsJournalEnabled && _pager.CachePageCount >= _cacheSize)
            {
                _log.Write(Logger.CACHE, "cache checkpoint reached at {0} pages in cache", _pager.CachePageCount);

                // write all dirty pages in data file (journal 
                foreach (var page in _pager.GetDirtyPages())
                {
                    // first write in journal file original data
                    _disk.WriteJournal(page.PageID, page.DiskData);
                
                    // then writes no datafile new changed pages
                    _disk.WritePage(page.PageID, page.WritePage());
                }
                
                // empty all cache pages
                _pager.ClearCache();
            }
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
                // page.WritePage() updated DiskData with new rendered buffer
                _disk.WritePage(page.PageID, _crypto == null || page.PageID == 0 ? page.WritePage() : _crypto.Encrypt(page.WritePage()));

                // mark page as clean (is now saved in disk)
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
            _pager.ClearCache();

            // recovery (if exists) journal file
            this.Recovery();
        }

        /// <summary>
        /// Try recovery journal file (if exists). Restore original datafile
        /// Journal file are NOT encrypted (even when datafile are encrypted)
        /// </summary>
        public void Recovery()
        {
            var fileSize = _disk.FileLength;

            // read all journal pages
            foreach (var buffer in _disk.ReadJournal())
            {
                // read pageID (first 4 bytes)
                var pageID = BitConverter.ToUInt32(buffer, 0);

                _log.Write(Logger.RECOVERY, "recover page #{0:0000}", pageID);

                // if header, read all byte (to get original filesize)
                if (pageID == 0)
                {
                    var header = (HeaderPage)BasePage.ReadPage(buffer);

                    fileSize = BasePage.GetSizeOfPages(header.LastPageID + 1);
                }

                // write in stream (encrypt if datafile is encrypted)
                _disk.WritePage(pageID, _crypto == null || pageID == 0 ? buffer : _crypto.Encrypt(buffer));
            }

            _log.Write(Logger.RECOVERY, "resize datafile to {0} bytes", fileSize);

            // redim filesize if grow more than original before rollback
            _disk.SetLength(fileSize);

            // empty journal file
            _disk.ClearJournal();
        }
    }
}