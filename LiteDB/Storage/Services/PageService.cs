using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class PageService
    {
        private DiskService _disk;
        private CacheService _cache;

        public PageService(DiskService disk, CacheService cache)
        {
            _disk = disk;
            _cache = cache;
        }

        /// <summary>
        /// Get a page from cache or from disk (and put on cache)
        /// </summary>
        public T GetPage<T>(uint pageID)
            where T : BasePage, new()
        {
            var page = _cache.GetPage<T>(pageID);

            if (page == null)
            {
                page = _disk.ReadPage<T>(pageID);

                _cache.AddPage(page);
            }

            return page;
        }

        /// <summary>
        /// Read all sequences pages from a start pageID (using NextPageID) 
        /// </summary>
        public IEnumerable<T> GetSeqPages<T>(uint firstPageID)
            where T : BasePage, new()
        {
            var pageID = firstPageID;

            while (pageID != uint.MaxValue)
            {
                var page = this.GetPage<T>(pageID);

                pageID = page.NextPageID;

                yield return page;
            }
        }

        /// <summary>
        /// Get a new empty page - can be a reused page (EmptyPage) or a clean one (extend datafile) 
        /// </summary>
        public T NewPage<T>(BasePage prevPage = null)
            where T : BasePage, new()
        {
            var page = new T();

            // try get page from Empty free list
            if(_cache.Header.FreeEmptyPageID != uint.MaxValue)
            {
                var free = this.GetPage<BasePage>(_cache.Header.FreeEmptyPageID);

                // remove page from empty list
                this.AddOrRemoveToFreeList(false, free, _cache.Header, ref _cache.Header.FreeEmptyPageID);

                page.PageID = free.PageID;
            }
            else
            {
                page.PageID = ++_cache.Header.LastPageID;

                if (page.PageID > _cache.Header.MaxPageID)
                    throw new LiteException("Max file length excedded");
            }

            // if there a page before, just fix NextPageID pointer
            if (prevPage != null)
            {
                page.PrevPageID = prevPage.PageID;
                prevPage.NextPageID = page.PageID;
                prevPage.IsDirty = true;
            }

            // mark header and this new page as dirty, and then add to cache
            page.IsDirty = true;
            _cache.Header.IsDirty = true;

            _cache.AddPage(page);

            return page;
        }

        /// <summary>
        /// Delete an page using pageID - transform them in Empty Page and add to EmptyPageList
        /// </summary>
        public void DeletePage(uint pageID, bool addSequence = false)
        {
            var pages = addSequence ? this.GetSeqPages<BasePage>(pageID).ToArray() : new BasePage[] { this.GetPage<BasePage>(pageID) };

            // Adding all pages to FreeList
            foreach (var page in pages)
            {
                // update page to mark as completly empty page
                page.Clear();
                page.IsDirty = true;

                // add to empty free list
                this.AddOrRemoveToFreeList(true, page, _cache.Header, ref _cache.Header.FreeEmptyPageID);
            }
        }

        /// <summary>
        /// Add or Remove a page in a sequence
        /// </summary>
        /// <param name="add">Indicate that will add or remove from FreeList</param>
        /// <param name="page">Page to add or remove from FreeList</param>
        /// <param name="startPage">Page reference where start the header list node</param>
        /// <param name="fieldPageID">Field reference, from startPage</param>
        public void AddOrRemoveToFreeList(bool add, BasePage page, BasePage startPage, ref uint fieldPageID)
        {
            if (add)
            {
                // if this page is in sequence, its already on freelist 
                if (page.PrevPageID != uint.MaxValue || page.NextPageID != uint.MaxValue)
                    return;

                // add this page in first place on list
                page.PrevPageID = 0;
                page.NextPageID = fieldPageID;
                page.IsDirty = true;

                if (fieldPageID != uint.MaxValue)
                {
                    // get previous page and set previous ID to my new page
                    var prevPage = this.GetPage<BasePage>(fieldPageID);

                    prevPage.PrevPageID = page.PageID;
                    prevPage.IsDirty = true;
                }

                // update header first page to my pageID
                fieldPageID = page.PageID;
                startPage.IsDirty = true;
            }
            else
            {
                // if this page is not in sequence, its not on freelist 
                if (page.PrevPageID == uint.MaxValue && page.NextPageID == uint.MaxValue && fieldPageID != page.PageID)
                    return;

                // this page is the first of list
                if (page.PrevPageID == 0)
                {
                    fieldPageID = page.NextPageID;
                    startPage.IsDirty = true;
                }
                else
                {
                    // if not the first, get previous page to remove NextPageId
                    var prevPage = this.GetPage<BasePage>(page.PrevPageID);
                    prevPage.NextPageID = page.NextPageID;
                    prevPage.IsDirty = true;
                }

                // if my page is not the last on sequence, ajust the last page
                if (page.NextPageID != uint.MaxValue)
                {
                    var nextPage = this.GetPage<BasePage>(page.NextPageID);
                    nextPage.PrevPageID = page.PrevPageID;
                    nextPage.IsDirty = true;
                }

                page.PrevPageID = page.NextPageID = uint.MaxValue;
                page.IsDirty = true;
            }
        }

        /// <summary>
        /// Returns a page that contains space enouth to data to insert new object - if not exits, create a new Page
        /// </summary>
        public T GetFreePage<T>(uint startPageID, int size)
            where T : BasePage, new()
        {
            while(startPageID != uint.MaxValue)
            {
                // get the first page
                var page = this.GetPage<BasePage>(startPageID);

                // check if there space in this page
                var free = page.FreeBytes;

                // first, test if there is space on this page
                if (free >= size)
                {
                    return this.GetPage<T>(startPageID);
                }

                startPageID = page.NextPageID;
            }

            // If not found page, create a new one
            return this.NewPage<T>();
        }
    }
}
