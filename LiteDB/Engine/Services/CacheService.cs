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
        /// When add item cache counter get this size, try clean 
        /// </summary>
        private const int MAX_CACHE_ADD = 1000;

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
        /// Add page in cache and if exceed add counter, try clear cache
        /// </summary>
        public void AddPage(long position, BasePage page)
        {
            if (_cache.TryAdd(position, page))
            {
                Interlocked.Increment(ref _cacheCounter);

                if (_cacheCounter > MAX_CACHE_ADD)
                {
                    _cacheCounter = 0;
                }
            }
            else
            {
                //if (page.IsDirty )
#if DEBUG
#endif
                // if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
        }

        /// <summary>
        /// Get page from cache. Use clone = true if you need change this page. Return null if not found
        /// </summary>
        public BasePage GetPage(long position, bool clone)
        {
            if (_cache.TryGetValue(position, out var page))
            {
#if DEBUG
                if (clone == false && page.IsDirty) throw new SystemException("Page dirty in cache and are requesting original version (no clone)");
#endif

                return clone ? page.Clone() : page;
            }

            return null;
        }

        /// <summary>
        /// Get dirty page from cache to be saved in disk
        /// </summary>
        public BasePage GetDirtyPage(long position)
        {
            if (_cache.TryGetValue(position, out var page))
            {
#if DEBUG
                if (!page.IsDirty) throw new SystemException("Page request must be dirty but are clean");
#endif

                return page;
            }
            else
            {
                throw new SystemException($"Page ${position} not found in cache");
            }
        }
    }
}