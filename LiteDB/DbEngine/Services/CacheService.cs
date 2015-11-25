using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal class CacheService : IDisposable
    {
        /// <summary>
        /// Max cache pages size - read or dirty. If Count pass this value, clear cache (if dirty, save in disk)
        /// </summary>
        public const int MAX_CACHE_SIZE = 5000;

        // contains only clean pages, used in read operations - can be clear any time
        private Dictionary<uint, BasePage> _cache;

        // contains only dirty pages - can be write on disk before clear
        private Dictionary<uint, BasePage> _dirty;

        // call when a page need be saved on journal
        public Action<BasePage> MarkAsDirtyAction = (page) => { };

        // call when need clear dirty cache
        public Action DirtyRecicleAction = () => { };

        public CacheService()
        {
            _cache = new Dictionary<uint, BasePage>();
            _dirty = new Dictionary<uint, BasePage>();
        }

        /// <summary>
        /// Get a page in my cache, first check if is in my dirty list. If not, check in my cache list. Returns null if not found
        /// </summary>
        public BasePage GetPage(uint pageID)
        {
            // check for in dirty list - if not found, check in cache list
            return _dirty.GetOrDefault(pageID, null) ?? _cache.GetOrDefault(pageID, null);
        }

        /// <summary>
        /// Add a page to cache. if this page is in cache, override
        /// </summary>
        public void AddPage(BasePage page)
        {
            // clear read cache if exceds cache limit size
            if(_cache.Count > MAX_CACHE_SIZE)
            {
                _cache.Clear();
            }

            // do not cache extend page - never will be reused
            if (page.PageType != PageType.Extend)
            {
                _cache[page.PageID] = page;
            }
        }

        /// <summary>
        /// Add a page as dirty page
        /// </summary>
        public void SetPageDirty(BasePage page)
        {
            _dirty[page.PageID] = page;

            if (page.IsDirty) return;

            page.IsDirty = true;

            // if page is new (not exits on datafile), there is no journal for them
            if (page.DiskData.Length > 0)
            {
                // call action passing dirty page - used for journal file writes 
                MarkAsDirtyAction(page);
            }

            // if dirty pages exceds, save in disk
            if(_dirty.Count > MAX_CACHE_SIZE && page.PageID != 0)
            {
                DirtyRecicleAction();
                this.Clear();
            }
        }

        /// <summary>
        /// Empty cache and dirty pages - returns true if had dirty pages
        /// </summary>
        public bool Clear()
        {
            var hasDirty = _dirty.Count > 0;

            _dirty.Clear();
            _cache.Clear();

            return hasDirty;
        }

        public bool HasDirtyPages { get { return _dirty.Count() > 0; } }

        /// <summary>
        /// Returns all dirty pages including header page (for better write performance, get all pages in PageID increase order)
        /// </summary>
        public IEnumerable<BasePage> GetDirtyPages()
        {
            // now returns all pages in sequence
            foreach (var page in _dirty.Values)
            {
                yield return page;
            }
        }

        public void Dispose()
        {
            this.Clear();
        }
    }
}
