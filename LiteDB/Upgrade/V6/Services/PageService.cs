using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    internal class PageService
    {
        private FileDiskService _disk;
        private Dictionary<uint, BasePage> _cache = new Dictionary<uint, BasePage>();

        public PageService(FileDiskService disk)
        {
            _disk = disk;
        }

        /// <summary>
        /// Get a page from cache or from disk (and put on cache)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            BasePage page;
                
            if(_cache.TryGetValue(pageID, out page))
            {
                return (T)page;
            }

            // clear cache if too big
            if (_cache.Count > 5000) _cache.Clear();

            var buffer = _disk.ReadPage(pageID);
            page = BasePage.ReadPage(buffer);
            _cache[pageID] = page;

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