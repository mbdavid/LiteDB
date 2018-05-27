using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
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
        private HashSet<long> _dirty = new HashSet<long>();

        private Logger _log;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Get how many clean pages are in cache
        /// </summary>
        public int Length => _cache.Count - _dirty.Count;

        /// <summary>
        /// Add page in cache. If page are marked as dirty, add position in a list of dirty pages
        /// </summary>
        public void AddPage(long position, BasePage page)
        {
            if (page.IsDirty)
            {
                lock (_dirty)
                {
                    _dirty.Add(position);
                }
            }

            if (this.Length > MAX_CACHE_SIZE)
            {
                this.Clear();
            }

            _cache[position] = page;
        }

        /// <summary>
        /// Get page from cache. Use clone = true if you need change this page. Return null if not found
        /// </summary>
        public BasePage GetPage(long position, bool clone)
        {
            if (_cache.TryGetValue(position, out var page))
            {
                // if page are dirty, return a clone version marked as clean
                return clone || page.IsDirty ? page.Clone() : page;
            }

            return null;
        }

        /// <summary>
        /// Mark single/all cache as clean pages - (non-dirty pages can be removed from cache any time)
        /// </summary>
        public void ClearDirty(long? position = null)
        {
            lock(_dirty)
            {
                if (position.HasValue)
                {
                    _dirty.Remove(position.Value);
                }
                else
                {
                    _dirty.Clear();
                }
            }
        }

        /// <summary>
        /// Clear all clean pages in cache. Do not remove dirty pages. Return how many pages was deleted from cache;
        /// </summary>
        public int Clear()
        {
            lock(_dirty)
            {
                if (_dirty.Count == 0)
                {
                    _cache.Clear();
                    return 0;
                }

                var keys = _cache.Keys.Except(_dirty).ToArray();

                foreach(var key in keys)
                {
                    _cache.TryRemove(key, out var page);
                }

                return keys.Length;
            }
        }
    }
}