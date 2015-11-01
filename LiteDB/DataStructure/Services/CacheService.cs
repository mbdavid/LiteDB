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
    internal class CacheService : IDisposable
    {
        // a very simple dictionary for pages cache and track
        private SortedDictionary<uint, BasePage> _cache;

        public CacheService()
        {
            _cache = new SortedDictionary<uint, BasePage>();
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
        /// </summary>
        public void AddPage(BasePage page)
        {
            _cache[page.PageID] = page;
        }

        /// <summary>
        /// Empty cache and header page
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        public bool HasDirtyPages { get { return this.GetDirtyPages().FirstOrDefault() != null; } }

        /// <summary>
        /// Returns all dirty pages including header page (for better write performance, get all pages in PageID increase order)
        /// </summary>
        public IEnumerable<BasePage> GetDirtyPages()
        {
            // now returns all pages in sequence
            foreach (var page in _cache.Values.Where(x => x.IsDirty))
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
