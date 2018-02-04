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

        private ConcurrentDictionary<long, BasePage> _clean = new ConcurrentDictionary<long, BasePage>();
        private ConcurrentDictionary<long, BasePage> _dirty = new ConcurrentDictionary<long, BasePage>();

        private Logger _log;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Add clean/dirty page on dictionary
        /// </summary>
        public void AddPage(long position, BasePage page)
        {
            if (page.IsDirty)
            {
                // first add into dirty list
                //_dirty.AddOrUpdate(position, page, (k, v) => page);
                _dirty[position] = page;

                // remove from clean list if present
                // for a short time page will be in both list, but dirty list must be checked before clean list.
                // _clean.TryRemove(position, out var cleanPage);
            }
            else
            {
                // before add into cache, check if not full and must be clear
                //this.TryReset();
            
                _clean[position] = page;
            }
        }

        /// <summary>
        /// Get a page from cache from clean/dirty list. Returns cloned if this page can be changed. Returns null if not found
        /// </summary>
        public BasePage GetPage(long position, bool clone)
        {
            // first, try get from dirty list
            if (_dirty.TryGetValue(position, out var page))
            {
                return clone ? page.Clone() : page;
            }
            
            // than, try get page from clean list
            if (_clean.TryGetValue(position, out page))
            {
                return clone ? page.Clone() : page;
            }

            // if not found in any cache list, returns null
            return null;
        }

        /// <summary>
        /// Remove page from dirty list and add back into clean list
        /// </summary>
        public void CleanDirtyPage(long position, BasePage page)
        {
            page.IsDirty = false;

            _clean[position] = page;

        }

        /// <summary>
        /// Check if time to reset cache and returns true if cache was cleaned
        /// </summary>
        /// <returns></returns>
        public bool TryReset()
        {
            if (_clean.Count > MAX_CACHE_SIZE)
            {
                _clean.Clear();

                return true;
            }

            return false;
        }
    }
}