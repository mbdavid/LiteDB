using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// Implement thread-safe cache for pages that read/write from disk
    /// </summary>
    internal class CacheService
    {
        /// <summary>
        /// Max page count in clean list - dirty list is never deleted be cache reset
        /// </summary>
        private const int MAX_CACHE_SIZE = 1000;

        private ConcurrentDictionary<long, BasePage> _cache = new ConcurrentDictionary<long, BasePage>();
        private ConcurrentDictionary<long, BasePage> _dirty = new ConcurrentDictionary<long, BasePage>();

        private Logger _log;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Add page into general cache and if page are dirty, add into dirty cache too
        /// </summary>
        public void AddPage(long position, BasePage page)
        {
            // first check cache size
            if (_cache.Count > MAX_CACHE_SIZE)
            {
                _cache.Clear();
            }

            // add page into cache
            _cache[position] = page;

            // if page are dirty, add into dirty cache too
            if (page.IsDirty)
            {
                _dirty[position] = page;
            }
        }

        /// <summary>
        /// Get page first from dirty cache, otherwise from normal cache
        /// </summary>
        public BasePage GetPage(long position, bool clone)
        {
            // first, try get from dirty list
            if (_dirty.TryGetValue(position, out var page))
            {
                return clone ? page.Clone() : page;
            }
            
            // than, try get page from cache
            if (_cache.TryGetValue(position, out page))
            {
                return clone ? page.Clone() : page;
            }

            // if not found in any cache dict, returns null
            return null;
        }

        /// <summary>
        /// Clear dirty cache
        /// </summary>
        public void ClearDirtyCache()
        {
            _dirty.Clear();
        }
    }
}