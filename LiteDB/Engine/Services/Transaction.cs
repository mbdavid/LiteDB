using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// </summary>
    internal class Transaction : IDisposable
    {
        // instances from Engine
        private FileService _datafile;
        private FileService _walfile;
        private WalService _wal;
        private LockService _locker;
        private Logger _log;

        // new instances
        private PageService _pager;
        private IndexService _index;
        private CollectionService _collection;
        private DataService _data;

        private Guid _transID;
        private uint _readVersion;
        private Dictionary<uint, BasePage> _dirtyPages = new Dictionary<uint, BasePage>();
        private Dictionary<uint, long> _dirtyPagesWal = new Dictionary<uint, long>();


        void SaveDirtyPagesToWal()
        {

        }

        void Commit()
        {

        }

        /// <summary>
        /// Must be call before any page change
        /// </summary>
        public T SetDirty<T>(BasePage page)
            where T : BasePage
        {
            // clone before edit
            var clone = BasePage.ReadPage(page.WritePage());

            _dirtyPages[clone.PageID] = clone;

            return (T)clone;
        }

        /// <summary>
        /// Get a page from cache or from disk (get from cache or from disk)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {

            //lock(_disk)
            //{
            //    var page = _cache.GetPage(pageID);
            //
            //    // is not on cache? load from disk
            //    if (page == null)
            //    {
            //        var buffer = _disk.ReadPage(pageID);
            //
            //        // if datafile are encrypted, decrypt buffer (header are not encrypted)
            //        if (_crypto != null && pageID > 0)
            //        {
            //            buffer = _crypto.Decrypt(buffer);
            //        }
            //
            //        page = BasePage.ReadPage(buffer);
            //
            //        _cache.AddPage(page);
            //    }
            //
            //    return (T)page;
            //}
        }


        public void Dispose()
        {
        }
    }
}