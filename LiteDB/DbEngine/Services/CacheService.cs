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
        // single cache structure - use Sort dictionary for get pages in order (fast to store in sequence on disk)
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
            where T : BasePage, new()
        {
            var page = _cache.GetOrDefault(pageID, null);

            // if a need a specific page but has a BasePage, returns null
            if (page != null && page.GetType() == typeof(BasePage) && typeof(T) != typeof(BasePage))
            {
                //if(page.IsDirty) throw new SystemException("Can convert page - page is dirty?");

                //var specificPage = new T();
                //
                //specificPage.ReadPage(page.DiskData);
                //
                //lock(_cache)
                //{
                //    _cache[pageID] = specificPage;
                //}
                //
                //return specificPage;
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
            lock(_cache)
            {
                // do not cache extend page - never will be reused
                //if (page.PageType != PageType.Extend)
                {
                    _cache[page.PageID] = page;
                }

                // page is dirty? add in a special list too and mark as dirty (all type of pages)
                if (dirty && !page.IsDirty)
                {
                    page.IsDirty = true;
                    _dirty[page.PageID] = page;
                    //_cache[page.PageID] = page;

                    // if page is new (not exits on datafile), there is no journal for them
                    if(page.DiskData.Length > 0)
                    {
                        // call action passing dirty page - used for journal file writes 
                        MarkAsDirtyAction(page);
                    }
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

        /// <summary>
        /// Set all pages as clear and remove them from dirty list
        /// </summary>
        public void ClearDirty()
        {
            lock(_cache)
            {
                foreach(var page in _dirty.Values)
                {
                    page.IsDirty = false;

                    // remove all non-header-collection pages (this can be optional in future)
                    //if (page.PageType != PageType.Header && page.PageType != PageType.Collection)
                    if (page.PageType == PageType.Extend)
                    {
                        _cache.Remove(page.PageID);
                    }
                }

                //_cache.Clear();
                _dirty.Clear();
            }
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
