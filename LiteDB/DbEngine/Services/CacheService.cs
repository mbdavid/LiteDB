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
        // double cache structure - use Sort dictionary for get pages in order (fast to store in sequence on disk)
        private SortedDictionary<uint, BasePage> _cache;
        private SortedDictionary<uint, BasePage> _dirty;

        public Action<BasePage> MarkAsDirtyAction = (page) => { };

        public CacheService()
        {
            _cache = new SortedDictionary<uint, BasePage>();
            _dirty = new SortedDictionary<uint, BasePage>();
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
            lock(_cache)
            {
                // do not cache extend page - never will be reused
                if (page.PageType != PageType.Extend)
                {
                    _cache[page.PageID] = page;
                }
            }
        }

        /// <summary>
        /// Add a page as dirty page
        /// </summary>
        public void SetPageDirty(BasePage page)
        {
            lock(_dirty)
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
            }
        }

        /// <summary>
        /// Empty cache and dirty pages - returns true if had dirty pages
        /// </summary>
        public bool Clear()
        {
            var hasDirty = _dirty.Count > 0;

            lock(_cache)
            {
                _dirty.Clear();
                _cache.Clear();
            }

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
