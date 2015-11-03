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
        // cache use a circular structure to avoid consume too many memory
        private const int CACHE_LIMIT = 10000; // ~80MB
        private const int CACHE_CLEAR = 1000;

        private uint _mru = 0;
        private object _mru_lock = new object();

        private int _hit = 0;
        private int _get = 0;

        // single cache structure
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

            this.SetMRU(page);

            if(page != null) _hit++;
            _get++;

            return (T)page;
        }

        /// <summary>
        /// Add a page to cache. if this page is in cache, override (except if is basePage - in this case, copy header)
        /// </summary>
        public void AddPage(BasePage page)
        {
            this.SetMRU(page);

            _cache[page.PageID] = page;

            this.RemoveLowerMRU();
        }

        /// <summary>
        /// Empty cache and header page
        /// </summary>
        public void Clear()
        {
            _mru = 0;
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

        /// <summary>
        /// Set a new MRU to page
        /// </summary>
        private void SetMRU(BasePage page)
        {
            if(page == null) return;

            // if header or collection set most higher value to never clear
            if(page.PageType == PageType.Header || page.PageType == PageType.Collection)
            {
                page.MRU = uint.MaxValue - page.PageID;
            }
            else
            {
                // get next mru value
                lock(_mru_lock)
                {
                    page.MRU = ++_mru;
                }
            }
        }

        private Stopwatch _select = new Stopwatch();
        private Stopwatch _count = new Stopwatch();

        public void RemoveLowerMRU()
        {
            _count.Start();
            var count = _cache.Count();
            _count.Stop();

            // check if I have too many pages in cache
            if(count > CACHE_LIMIT)
            {
                // lets clear some non-dirty pages
                //TODO: test performance
                _select.Start();
                var delete = _cache
                    .Where(x => x.Value.IsDirty == false)
                    .OrderBy(x => x.Value.MRU)
                    .Take(CACHE_CLEAR)
                    .Select(x => x.Key)
                    .ToArray();
                _select.Stop();

                foreach(var pageID in delete)
                {
                    _cache.Remove(pageID);
                }
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Total em GET               : " + _get);
            Console.WriteLine("Total em HIT               : " + _hit);
            Console.WriteLine("Total em cache             : " + _cache.Count());
            Console.WriteLine("Tempo total para contar    : " + _count.ElapsedMilliseconds);
            Console.WriteLine("Tempo total para selecionar: " + _select.ElapsedMilliseconds);
            this.Clear();
        }
    }
}
