using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class CacheService
    {
        /// <summary>
        /// Collection to store clean only pages in cache
        /// </summary>
        private Dictionary<uint, BasePage> _clean = new Dictionary<uint, BasePage>();

        /// <summary>
        /// Collection to store dirty only pages in cache. If page was in _clean, remove from there and insert here
        /// </summary>
        private Dictionary<uint, BasePage> _dirty = new Dictionary<uint, BasePage>();

        private IDiskService _disk;
        private Logger _log;

        public CacheService(IDiskService disk, Logger log)
        {
            _disk = disk;
            _log = log;
        }

        /// <summary>
        /// Get a page from cache or from disk (and put on cache)
        /// [ThreadSafe]
        /// </summary>
        public BasePage GetPage(uint pageID)
        {
            // try get page from dirty cache or from clean list
            var page =
                _dirty.GetOrDefault(pageID) ??
                _clean.GetOrDefault(pageID);

            return page;
        }

        /// <summary>
        /// Add page to cache
        /// [ThreadSafe]
        /// </summary>
        public void AddPage(BasePage page)
        {
            if (page.IsDirty) throw new NotSupportedException("Page can't be dirty");

            _clean[page.PageID] = page;
        }

        /// <summary>
        /// Set a page as dirty and ensure page are in cache. Should be used after any change on page 
        /// Do not use on end of method because page can be deleted/change type
        /// Always remove from clean list and add in dirty list
        /// [ThreadSafe]
        /// </summary>
        public void SetDirty(BasePage page)
        {
            _clean.Remove(page.PageID);
            page.IsDirty = true;
            _dirty[page.PageID] = page;
        }

        /// <summary>
        /// Return all dirty pages
        /// [ThreadSafe]
        /// </summary>
        public ICollection<BasePage> GetDirtyPages()
        {
            return _dirty.Values;
        }

        /// <summary>
        /// Get how many pages are in clean cache
        /// </summary>
        public int CleanUsed { get { return _clean.Count; } }

        /// <summary>
        /// Get how many pages are in dirty cache
        /// </summary>
        public int DirtyUsed { get { return _dirty.Count; } }

        /// <summary>
        /// Discard only dirty pages
        /// [ThreadSafe]
        /// </summary>
        public void DiscardDirtyPages()
        {
            _log.Write(Logger.CACHE, "clearing dirty pages from cache");

            _dirty.Clear();
        }

        /// <summary>
        /// Mark all dirty pages now as clean pages and transfer to clean collection
        /// [ThreadSafe]
        /// </summary>
        public void MarkDirtyAsClean()
        {
            foreach(var p in _dirty)
            {
                p.Value.IsDirty = false;
                _clean[p.Key] = p.Value;
            }

            _dirty.Clear();
        }

        /// <summary>
        /// Remove from cache all clean pages
        /// [Non - ThreadSafe]
        /// </summary>
        public void ClearPages()
        {
            lock(_clean)
            {
                _log.Write(Logger.CACHE, "cleaning cache");
                _clean.Clear();
            }
        }
    }
}