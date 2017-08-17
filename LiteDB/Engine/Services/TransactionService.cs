using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Manages all transactions and grantees concurrency and recovery
    /// </summary>
    internal class TransactionService
    {
        private IDiskService _disk;
        private AesEncryption _crypto;
        private LockService _locker;
        private PageService _pager;
        private CacheService _cache;
        private Logger _log;
        private int _cacheSize;

        internal TransactionService(IDiskService disk, AesEncryption crypto, PageService pager, LockService locker, CacheService cache, int cacheSize, Logger log)
        {
            _disk = disk;
            _crypto = crypto;
            _cache = cache;
            _locker = locker;
            _pager = pager;
            _cacheSize = cacheSize;
            _log = log;
        }

        /// <summary>
        /// Checkpoint is a safe point to clear cache pages without loose pages references.
        /// Is called after each document insert/update/deleted/indexed/fetch from query
        /// Clear only clean pages - do not clear dirty pages (transaction)
        /// </summary>
        public void CheckPoint()
        {
            if (_cache.CleanUsed > _cacheSize)
            {
                _cache.ClearPages();
            }
        }

        /// <summary>
        /// Save all dirty pages to disk
        /// </summary>
        public void Commit()
        {
            // get header page
            var header = _pager.GetPage<HeaderPage>(0);

            // increase file changeID (back to 0 when overflow)
            header.ChangeID = header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(header.ChangeID + (ushort)1);

            // mark header as dirty
            _pager.SetDirty(header);

            // write journal file
            _disk.WriteJournal(_cache.GetDirtyPages()
                .Select(x => x.DiskData)
                .Where(x => x.Length > 0)
                .ToList());

            // enter in exclusive lock mode to write on disk
            using (_locker.Exclusive())
            {
                // set final datafile length (optimize page writes)
                _disk.SetLength(BasePage.GetSizeOfPages(header.LastPageID + 1));

                foreach (var page in _cache.GetDirtyPages())
                {
                    // page.WritePage() updated DiskData with new rendered buffer
                    var buffer = _crypto == null || page.PageID == 0 ? 
                        page.WritePage() : 
                        _crypto.Encrypt(page.WritePage());

                    _disk.WritePage(page.PageID, buffer);
                }

                // mark all dirty pages in clean pages (all are persisted in disk and are valid pages)
                _cache.MarkDirtyAsClean();

                // discard journal file
                _disk.ClearJournal();
            }
        }

        /// <summary>
        /// Clear cache, discard journal file
        /// </summary>
        public void Rollback()
        {
            // clear all dirty pages from memory
            _cache.DiscardDirtyPages();

            _disk.ClearJournal();
        }

        /// <summary>
        /// Try recovery journal file (if exists). Restore original datafile
        /// Journal file are NOT encrypted (even when datafile are encrypted)
        /// </summary>
        public void Recovery()
        {
            var fileSize = _disk.FileLength;
            var pages = 0;

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

                pages++;
            }

            // no pages, no recovery
            if (pages ==  0) return;

            _log.Write(Logger.RECOVERY, "resize datafile to {0} bytes", fileSize);

            // redim filesize if grow more than original before rollback
            _disk.SetLength(fileSize);

            // empty journal file
            _disk.ClearJournal();
        }
    }
}