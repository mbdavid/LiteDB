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
        // single cache structure
        private SortedDictionary<uint, BasePage> _cache;
        private SortedDictionary<uint, BasePage> _dirty;

        public Action<BasePage> MarkAsDirtyAction = (page) => { };

        public CacheService()
        {
            _cache = new SortedDictionary<uint, BasePage>();
            _dirty = new SortedDictionary<uint, BasePage>();
        }

        /// <summary>
        /// Get a page inside cache system. Returns null if page not existis. 
        /// If T is more specific than page that I have in cache, returns null (eg. Page 2 is BasePage in cache and this method call for IndexPage PageId 2)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage
        {
            var page = _cache.GetOrDefault(pageID, null);

            // if a need a specific page but has a BasePage, returns null
            if (page != null && page.GetType() == typeof(BasePage) && typeof(T) != typeof(BasePage))
            {
                return null;
            }

            return (T)page;
        }

        /// <summary>
        /// Add a page to cache. if this page is in cache, override (except if is basePage - in this case, copy header)
        /// If set is dirty, add in a second list
        /// </summary>
        public void AddPage(BasePage page, bool dirty = false)
        {
            // do not cache extend page - never will be reused
            if (page.PageType != PageType.Extend)
            {
                _cache[page.PageID] = page;
            }

            // page is dirty? add in a special list too and mark as dirty (all type of pages)
            if (dirty && !page.IsDirty)
            {
                page.IsDirty = true;
                _dirty[page.PageID] = page;

                // if page is new (not exits on datafile), there is no journal for them
                if(page.DiskData.Length > 0)
                {
                    // call action passing dirty page - used for journal file writes 
                    MarkAsDirtyAction(page);
                }
            }
        }

        /// <summary>
        /// Empty cache and dirty pages - returns true if has dirty pages
        /// </summary>
        public bool Clear()
        {
            var hasDirty = _dirty.Count > 0;

            _dirty.Clear();
            _cache.Clear();

            return hasDirty;
        }

        /// <summary>
        /// Set all pages as clear and remove them from dirty list
        /// </summary>
        public void ClearDirty()
        {
            // clear page and clear special list (will keep in _cache list)
            foreach(var page in _dirty.Values)
            {
                page.IsDirty = false;
            }

            _dirty.Clear();
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
