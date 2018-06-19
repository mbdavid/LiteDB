using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement thread-safe cache for pages that read/write from disk
    /// </summary>
    internal class CacheService
    {
        private int _cacheCounter = 0;

        private ConcurrentDictionary<long, BasePage> _cache = new ConcurrentDictionary<long, BasePage>();

        private Logger _log;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Get how many pages are in cache
        /// </summary>
        public int Length => _cache.Count;

        /// <summary>
        /// Get cache data
        /// </summary>
        public ConcurrentDictionary<long, BasePage> Data => _cache;

        /// <summary>
        /// Clear cache
        /// </summary>
        public void Clear() => _cache.Clear();

        /// <summary>
        /// Add page in cache and if exceed add counter, try clear cache
        /// </summary>
        public void AddPage(long position, BasePage page)
        {
            if (_cache.TryAdd(position, page))
            {
                Interlocked.Increment(ref _cacheCounter);

                if (_cacheCounter > MAX_CACHE_ADD)
                {
                    lock(_cache)
                    {
                        if (_cacheCounter > MAX_CACHE_ADD)
                        {
                            _cacheCounter = 0;

                            // clear all clean pages
                            var keys = _cache
                                .Where(x => x.Value.IsDirty == false)
                                .Select(x => x.Key)
                                .ToArray();

                            foreach(var p in keys)
                            {
                                _cache.TryRemove(p, out var dummy);
                            }
                        }
                    }
                }
            }
            else
            {
                DEBUG(!Object.ReferenceEquals(_cache[position], page), "check why page are already in cache and are not same instance");
            }
        }


        /// <summary>
        /// Get page from cache. Use clone = true if you need change this page. Return null if not found
        /// </summary>
        public BasePage GetPage(long position, bool clone)
        {
            if (_cache.TryGetValue(position, out var page))
            {
                return clone ? page.Clone() : page;
            }

            return null;
        }
    }
}