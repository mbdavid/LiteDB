using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        private ConcurrentDictionary<uint, List<BasePage>> _cache = new ConcurrentDictionary<uint, List<BasePage>>();

        private Logger _log;

        public CacheService(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Get page from cache with version that must be equals or less than parameter version. Returns null if not found
        /// </summary>
        public BasePage GetPage(uint pageID, uint version)
        {
            // get page list versions
            if(_cache.TryGetValue(pageID, out var pages))
            {
                lock(pages)
                {
                    // get page version 
                    return pages.FirstOrDefault(x => x.Version <= version);
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Add new page into cache with page version
        /// </summary>
        public void AddPage(BasePage page, uint version)
        {
            // get page from versionID
            var pages = _cache.GetOrAdd(page.PageID, (v) => new List<BasePage>());

            // add page version (only if not exists)
            lock(pages)
            {
                page.Version = version;

                pages.Add(page);
            }
        }

        /// <summary>
        /// Clear all pages from cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }
    }
}