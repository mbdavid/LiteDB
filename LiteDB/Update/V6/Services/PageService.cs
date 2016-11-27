using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    internal class PageService
    {
        private IDiskService _disk;
        private CacheService _cache;

        public PageService(IDiskService disk, CacheService cache)
        {
            _disk = disk;
            _cache = cache;
        }

        /// <summary>
        /// Get a page from cache or from disk (and put on cache)
        /// </summary>
        public T GetPage<T>(uint pageID, bool setDirty = false)
            where T : BasePage
        {
            var page = _cache.GetPage(pageID);

            // is not on cache? load from disk
            if (page == null)
            {
                var buffer = _disk.ReadPage(pageID);
                page = BasePage.ReadPage(buffer);
                _cache.AddPage(page);
            }
			
            // set page as dirty if passing by param
            if (setDirty)
            {
                this.SetDirty((T)page);
            }

            return (T)page;
        }


        /// <summary>
        /// Read all sequences pages from a start pageID (using NextPageID)
        /// </summary>
        public IEnumerable<T> GetSeqPages<T>(uint firstPageID)
            where T : BasePage
        {
            var pageID = firstPageID;

            while (pageID != uint.MaxValue)
            {
                var page = this.GetPage<T>(pageID);

                pageID = page.NextPageID;

                yield return page;
            }
        }
    }
}