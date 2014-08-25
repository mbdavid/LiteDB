using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class DataService
    {
        private DiskService _disk;
        private PageService _pager;
        private CacheService _cache;

        public DataService(DiskService disk, CacheService cache, PageService pager)
        {
            _disk = disk;
            _cache = cache;
            _pager = pager;
        }

        /// <summary>
        /// Insert data inside a datapage. Returns dataPageID that idicates the first page
        /// </summary>
        public DataBlock Insert(CollectionPage col, byte[] data)
        {
            // need to extend (data is bigger than 1 page)
            var extend = (data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE) > BasePage.PAGE_AVAILABLE_BYTES;

            // if extend, just search for a page with BLOCK_SIZE avaiable
            var dataPage = _pager.GetFreePage<DataPage>(col.FreeDataPageID, extend ? DataBlock.DATA_BLOCK_FIXED_SIZE : data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            // create a new block with first empty index on DataPage
            var block = new DataBlock { Position = new PageAddress(dataPage.PageID, dataPage.DataBlocks.NextIndex()), Page = dataPage };

            // if extend, store all bytes on extended page.
            if (extend)
            {
                var extendPage = _pager.NewPage<ExtendPage>();
                block.ExtendPageID = extendPage.PageID;
                this.StoreExtendData(extendPage, data);
            }
            else
            {
                block.Data = data;
            }

            // add dataBlock to this page
            dataPage.DataBlocks.Add(block.Position.Index, block);

            dataPage.IsDirty = true;

            // add/remove dataPage on freelist if has space
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > BasePage.RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            col.DocumentCount++;

            col.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Update data inside a datapage. If new data can be used in same datapage, just update. Otherside, copy content to a new ExtendedPage
        /// </summary>
        public DataBlock Update(CollectionPage col, PageAddress blockAddress, byte[] data)
        {
            var dataPage = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = dataPage.DataBlocks[blockAddress.Index];
            var extend = dataPage.FreeBytes + block.Data.Length - data.Length <= 0;

            // check if need to extend
            if (extend)
            {
                // clear my block data
                block.Data = new byte[0];

                // create (or get a existed) extendpage and store data there
                ExtendPage extendPage;

                if (block.ExtendPageID == uint.MaxValue)
                {
                    extendPage = _pager.NewPage<ExtendPage>();
                    block.ExtendPageID = extendPage.PageID;
                }
                else
                {
                    extendPage = _pager.GetPage<ExtendPage>(block.ExtendPageID);
                }

                this.StoreExtendData(extendPage, data);
            }
            else
            {
                // If no extends, just update data block
                block.Data = data;

                // If there was a extended bytes, delete
                if (block.ExtendPageID != uint.MaxValue)
                {
                    _pager.DeletePage(block.ExtendPageID, true);
                    block.ExtendPageID = uint.MaxValue;
                }
            }

            // Add/Remove dataPage on freelist if has space AND its on/off free list
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            dataPage.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Read all data from datafile using a pageID as reference. If data is not in DataPage, read from ExtendPage. If readExtendData = false, do not read extended data 
        /// </summary>
        public DataBlock Read(PageAddress blockAddress, bool readExtendData = true)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = page.DataBlocks[blockAddress.Index];

            // if there is a extend page, read bytes to block.Data
            if (readExtendData && block.ExtendPageID != uint.MaxValue)
            {
                block.Data = this.Read(block.ExtendPageID);
            }

            return block;
        }

        /// <summary>
        /// Read all data from a extended page with all subsequences pages if exits
        /// </summary>
        public byte[] Read(uint extendPageID)
        {
            // read all extended pages and build byte array
            using (var buffer = new MemoryStream())
            {
                foreach (var extendPage in _pager.GetSeqPages<ExtendPage>(extendPageID))
                {
                    buffer.Write(extendPage.Data, 0, extendPage.Data.Length);
                }

                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Delete one dataBlock
        /// </summary>
        public DataBlock Delete(CollectionPage col, PageAddress blockAddress)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = page.DataBlocks[blockAddress.Index];

            // if there a extended page, delete all
            if (block.ExtendPageID != uint.MaxValue)
            {
                _pager.DeletePage(block.ExtendPageID, true);
            }

            // delete block inside page
            page.DataBlocks.Remove(block.Position.Index);

            // if there is no more datablocks, lets delete the page
            if (page.DataBlocks.Count == 0)
            {
                // first, remove from free list
                _pager.AddOrRemoveToFreeList(false, page, col, ref col.FreeDataPageID);

                _pager.DeletePage(page.PageID, false);
            }
            else
            {
                // add or remove to free list
                _pager.AddOrRemoveToFreeList(page.FreeBytes > DataPage.RESERVED_BYTES, page, col, ref col.FreeDataPageID);
            }

            col.DocumentCount--;

            col.IsDirty = true;
            page.IsDirty = true;

            return block;
        }

        /// <summary>
        /// Store all bytes in one extended page. If excced, call again to new page and make than continuous
        /// </summary>
        public void StoreExtendData(ExtendPage page, byte[] data)
        {
            // if data length is less the page-size
            if (data.Length <= ExtendPage.PAGE_AVAILABLE_BYTES)
            {
                page.Data = data;

                // if this page contains more continuous pages delete them (its a update case)
                if (page.NextPageID != uint.MaxValue)
                {
                    // Delete nextpage and all nexts
                    _pager.DeletePage(page.NextPageID, true);

                    // set my page with no NextPageID
                    page.NextPageID = uint.MaxValue;
                }

                page.IsDirty = true;
            }
            else
            {
                // split data - insert first bytes in this page and call again to insert next data
                page.Data = data.Take(ExtendPage.PAGE_AVAILABLE_BYTES).ToArray();

                ExtendPage newPage;

                // if i have a continuous page, get it... or create a new one
                if (page.NextPageID != uint.MaxValue)
                    newPage = _pager.GetPage<ExtendPage>(page.NextPageID);
                else
                    newPage = _pager.NewPage<ExtendPage>(page);

                page.IsDirty = true;

                this.StoreExtendData(newPage, data.Skip(ExtendPage.PAGE_AVAILABLE_BYTES).ToArray());
            }
        }

        #region Data operations without Cache - directly from disk for Files

        /// <summary>
        /// Store all bytes inside stream directly to datafile (no transaction) - returns bytes length. Do not use cache! (files can be too large).
        /// Used only to FilesCollection
        /// </summary>
        public int StoreStreamData(ExtendPage page, Stream stream)
        {
            var buffer = new byte[ExtendPage.PAGE_AVAILABLE_BYTES];

            // write first page only
            var read = stream.Read(buffer, 0, buffer.Length);
            var totalBytes = read;

            page.Data = new byte[read];

            Array.Copy(buffer, page.Data, read);

            // now lets copy all other pages
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                totalBytes += read;

                var next = this.NextPage(page);

                // save to disk last page
                _disk.WritePage(page);

                page = next;

                page.Data = new byte[read];

                Array.Copy(buffer, page.Data, read);
            }

            // add last page to cache and remove from sequence freelist
            page.IsDirty = true;

            _cache.AddPage(page);

            // if there is more pages on freemeptylist, ajust first pointer to zero
            if (_cache.Header.FreeEmptyPageID == page.NextPageID && page.NextPageID != uint.MaxValue)
            {
                var next = _pager.GetPage<BasePage>(page.NextPageID);
                next.PrevPageID = 0;
            }

            page.NextPageID = uint.MaxValue;

            return totalBytes;
        }

        /// <summary>
        /// Get the next extend page direct from disk - do not add any page to cache (excepts HeaderPage) - used for StoreStream
        /// </summary>
        public ExtendPage NextPage(ExtendPage prevPage = null)
        {
            var page = new ExtendPage();

            // try get page from Empty free list
            if (_cache.Header.FreeEmptyPageID != uint.MaxValue)
            {
                var free = _disk.ReadPage<BasePage>(_cache.Header.FreeEmptyPageID);

                _cache.Header.FreeEmptyPageID = free.NextPageID;

                page.PageID = free.PageID;
                page.PrevPageID = free.PrevPageID;
                page.NextPageID = free.NextPageID;
            }
            else
            {
                page.PageID = ++_cache.Header.LastPageID;
            }

            // if there a page before, just fix NextPageID pointer
            if (prevPage != null)
            {
                page.PrevPageID = prevPage.PageID;
                prevPage.NextPageID = page.PageID;
            }

            // mark header as dirty
            _cache.Header.IsDirty = true;

            return page;
        }

        /// <summary>
        /// Read all pages/bytes from start pageID to stream. Do not use cache! (files can be too large). Used only in FilesCollection
        /// </summary>
        public void ReadStreamData(uint pageID, Stream stream)
        {
            // read first page direct from disk (no cache)
            var page = _disk.ReadPage<ExtendPage>(pageID);

            // read all pages and write directly to stream
            while (page != null)
            {
                // write data to strem
                stream.Write(page.Data, 0, page.Data.Length);

                // read next page or set page to null (last page)
                page = page.NextPageID == uint.MaxValue ? null : _disk.ReadPage<ExtendPage>(page.NextPageID);
            }
        }

        /// <summary>
        /// Delete all pages, startins in pageID, marking all as Empty and adding list to FreeEmptyList
        /// </summary>
        /// <param name="pageID"></param>
        public void DeleteStreamData(uint pageID)
        {
            var page = _disk.ReadPage<BasePage>(pageID);

            page.PageType = PageType.Empty;
            page.FreeBytes = BasePage.PAGE_AVAILABLE_BYTES;

            while (page.NextPageID != uint.MaxValue)
            {
                // save page to disk
                _disk.WritePage(page);

                // get next page in sequence
                page = _disk.ReadPage<BasePage>(page.NextPageID);

                // clear
                page.PageType = PageType.Empty;
                page.FreeBytes = BasePage.PAGE_AVAILABLE_BYTES;
            }

            // fix header first page in list (all in cache)
            if (_cache.Header.FreeEmptyPageID != uint.MaxValue)
            {
                var first = _pager.GetPage<BasePage>(_cache.Header.FreeEmptyPageID);

                // set first page to be next page after sequence
                first.PrevPageID = page.PageID;
                page.NextPageID = first.PageID;

                first.IsDirty = true;
            }

            // ajust header to first page
            _cache.Header.FreeEmptyPageID = pageID;
            _cache.Header.IsDirty = true;

            // add my last page sequence to cache and mark as dirty to me saved on persist
            page.IsDirty = true;

            _cache.AddPage(page);
        }

        #endregion
    }
}
