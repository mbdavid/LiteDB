using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Cache service to manager pages in memory with pager versions. In cache, there is no modified pages, only clean pages. This pages came from datafile or wal-file
    /// </summary>
    internal class CacheService
    {
        /// <summary>
        /// Dictionary to store pages in memory with page version support
        /// </summary>
        private ConcurrentDictionary<uint, ConcurrentDictionary<uint, BasePage>> _cache = new ConcurrentDictionary<uint, ConcurrentDictionary<uint, BasePage>>();

        /// <summary>
        /// Get how many pages (with all versions) are inside cache
        /// </summary>
        private int _count;

        private Logger _log;

        /// <summary>
        /// Get how many pages (with all versions) are inside cache
        /// </summary>
        public int Count => _count;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Get page from cache with version that must be equals or less than parameter version. Returns null if not found
        /// </summary>
        public BasePage GetPage(uint pageID, uint version)
        {
            // get page slot in cache
            if (_cache.TryGetValue(pageID, out var slot))
            {
                // get all page versions avaiable in cache slot
                var versions = slot.Keys.OrderBy(x => x);

                // get best version for request verion
                var v = versions.FirstOrDefault(x => x <= version);

                // if versions on cache are higher then request, exit
                if (v == 0) return null;

                // try get for concurrent dict this page (it's possible this page are no anymore in cache - other concurrency thread clear cache)
                if (slot.TryGetValue(v, out var page))
                {
                    return page;
                }
            }

            return null;
        }

        /// <summary>
        /// Add new page into cache with page version
        /// </summary>
        public void AddPage(BasePage page, uint version)
        {
            // get page slot in cache (or create if not exists)
            var slot = _cache.GetOrAdd(page.PageID, new ConcurrentDictionary<uint, BasePage>());

            // add page to cache only if not exists (with this version)
            if(slot.TryAdd(version, page))
            {
                // concurrency count increment
                Interlocked.Increment(ref _count);
            }
        }

        /// <summary>
        /// Clear all pages from cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();

            _count = 0;
        }
    }
}