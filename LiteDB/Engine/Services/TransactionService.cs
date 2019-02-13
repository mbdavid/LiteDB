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
        /// Return true if cache was clear
        /// </summary>
        public bool CheckPoint()
        {
            if (_cache.CleanUsed > _cacheSize)
            {
                _log.Write(Logger.CACHE, "cache size reached {0} pages, will clear now", _cache.CleanUsed);

                _cache.ClearPages();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Save all dirty pages to disk
        /// </summary>
        public void PersistDirtyPages()
        {
            // get header page
            var header = _pager.GetPage<HeaderPage>(0);

            // increase file changeID (back to 0 when overflow)
            header.ChangeID = header.ChangeID == ushort.MaxValue ? (ushort)0 : (ushort)(header.ChangeID + (ushort)1);

            // mark header as dirty
            _pager.SetDirty(header);

            _log.Write(Logger.DISK, "begin disk operations - changeID: {0}", header.ChangeID);

            var dirtyPages = _cache.GetDirtyPages().ToList();

            // update DiskData of dirty pages
            foreach (var item in dirtyPages) {
                item.WritePage();
            }
            
            if (_disk.IsJournalEnabled) {
                // sort and ensure the header page is the last
                // the result list will be like: [1, 2, 3, 0]
                dirtyPages.Sort((a, b) => {
                    if (a.PageID == 0) return int.MaxValue;
                    if (b.PageID == 0) return int.MinValue;
                    return a.PageID.CompareTo(b.PageID);
                });

                _disk.WriteJournal(dirtyPages, header.LastPageID);

                // mark header as recovery before start writing (in journal, must keep recovery = false)
                header.Recovery = true;
                header.UpdateRecoveryByte();
            }
            else
            {
                // if no journal extend, resize file here to fast writes
                _disk.SetLength(BasePage.GetSizeOfPages(header.LastPageID + 1));
            }

            // header page (id=0) always must be first page to write on disk because it's will mark disk as "in recovery".
            _disk.WritePage(header.PageID, header.DiskData);

            // write rest pages
            foreach (var page in dirtyPages)
            {
                if (page.PageID == 0) continue;

                // DiskData was updated before
                var buffer = page.DiskData;
                if (_crypto != null && page.PageID != 0)
                    buffer = _crypto.Encrypt(buffer);

                _disk.WritePage(page.PageID, buffer);
            }

            if (_disk.IsJournalEnabled)
            {
                // re-write header page but now with recovery=false
                header.Recovery = false;
                header.UpdateRecoveryByte();

                _log.Write(Logger.DISK, "re-write header page now with recovery = false");

                _disk.WritePage(header.PageID, header.DiskData);
            }

            // mark all dirty pages as clean pages (all are persisted in disk and are valid pages)
            _cache.MarkDirtyAsClean();

            // flush all data direct to disk
            _disk.Flush();

            // discard journal file
            _disk.ClearJournal(header.LastPageID);
        }

        /// <summary>
        /// Get journal pages and override all into datafile
        /// </summary>
        public void Recovery()
        {
            _log.Write(Logger.RECOVERY, "initializing recovery mode");

            using (_locker.Write())
            {
                // double check in header need recovery (could be already recover from another thread)
                var header = BasePage.ReadPage(_disk.ReadPage(0)) as HeaderPage;

                if (header.Recovery == false) return;

                byte[] headerBuffer = null;

                // read all journal pages
                foreach (var buffer in _disk.ReadJournal(header.LastPageID))
                {
                    // read pageID (first 4 bytes)
                    var pageID = BitConverter.ToUInt32(buffer, 0);

                    // don't write the header page now, write it in the end
                    if (pageID == 0) {
                        headerBuffer = buffer;
                        continue;
                    }

                    _log.Write(Logger.RECOVERY, "recover page #{0:0000}", pageID);

                    // write in stream (encrypt if datafile is encrypted)
                    _disk.WritePage(pageID, _crypto == null ? buffer : _crypto.Encrypt(buffer));
                }

                // write header page
                if (headerBuffer != null) { // (header page is always in journal?)
                    _disk.WritePage(0, headerBuffer);
                }

                // shrink datafile
                _disk.ClearJournal(header.LastPageID);
            }
        }
    }
}