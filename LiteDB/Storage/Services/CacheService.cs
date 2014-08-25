using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Represent all cache system and track dirty pages. All pages that load and need to be track for
    /// dirty (to be persist after) must be added in this class.
    /// </summary>
    internal class CacheService
    {
        // a very simple dictionary for pages cache and track
        private Dictionary<uint, BasePage> _cache;

        private DiskService _disk;

        private HeaderPage _header;

        public CacheService(DiskService disk)
        {
            _disk = disk;

            _cache = new Dictionary<uint, BasePage>();
        }

        /// <summary>
        /// Get header page in cache or request for a new instance if not existis yet
        /// </summary>
        public HeaderPage Header
        {
            get
            {
                if (_header == null)
                    _header = _disk.ReadPage<HeaderPage>(0);
                return _header;
            }
        }

        /// <summary>
        /// Get a page inside cache system. Returns null if page not existis. 
        /// If T is more specific than page that I have in cache, returns null (eg. Page 2 is BasePage in cache and this method call for IndexPage PageId 2)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            var page = _cache.ContainsKey(pageID) ? _cache[pageID] : null;

            // if a need a specific page but has a BasePage, returns null
            if (page != null && page.GetType() == typeof(BasePage) && typeof(T) != typeof(BasePage))
            {
                return null;
            }

            return (T)page;
        }

        /// <summary>
        /// Add a page to cache. if this page is in cache, override (except if is basePage - in this case, copy header)
        /// </summary>
        public void AddPage(BasePage page)
        {
            var p = _cache.ContainsKey(page.PageID) ? _cache[page.PageID] : null;

            // if page is in cache but is basePage, the copy header attributes to new page copy
            if (p != null && p.GetType() == typeof(BasePage) && page.GetType() != typeof(BasePage))
            {
                page.IsDirty = p.IsDirty;
                page.NextPageID = p.NextPageID;
                page.PrevPageID = p.PrevPageID;
            }

            _cache[page.PageID] = page;
        }

        /// <summary>
        /// Removing a page from cache
        /// </summary>
        public void RemovePage(uint pageID)
        {
            _cache.Remove(pageID);
        }

        /// <summary>
        /// Empty cache and header page
        /// </summary>
        public void Clear()
        {
            _header = null;
            _cache.Clear();
        }

        /// <summary>
        /// Persist all dirty pages
        /// </summary>
        public void PersistDirtyPages()
        {
            var pages = _cache.Values.Where(x => x.IsDirty);

            if (Header.IsDirty)
                _disk.WritePage(Header);

            foreach (var page in pages)
            {
                _disk.WritePage(page);
            }
        }

        public IEnumerable<BasePage> GetDirtyPages()
        {
            return _cache.Values.Where(x => x.IsDirty);
        }
    }
}
