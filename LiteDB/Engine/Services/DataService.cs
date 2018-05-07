using System;
using System.IO;

namespace LiteDB
{
    internal class DataService
    {
        private PageService _pager;
        private Logger _log;

        public DataService(PageService pager, Logger log)
        {
            _pager = pager;
            _log = log;
        }

        /// <summary>
        /// Insert data inside a datapage. Returns dataPageID that indicates the first page
        /// </summary>
        public DataBlock Insert(CollectionPage col, byte[] data)
        {
            // need to extend (data is bigger than 1 page)
            var extend = (data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE) > BasePage.PAGE_AVAILABLE_BYTES;

            // if extend, just search for a page with BLOCK_SIZE available
            var dataPage = _pager.GetFreePage<DataPage>(col.FreeDataPageID, extend ? DataBlock.DATA_BLOCK_FIXED_SIZE : data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            // create a new block with first empty index on DataPage
            var block = new DataBlock { Page = dataPage };

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
            dataPage.AddBlock(block);

            // set page as dirty
            _pager.SetDirty(dataPage);

            // add/remove dataPage on freelist if has space
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.DATA_RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            // increase document count in collection
            col.DocumentCount++;

            // set collection page as dirty
            _pager.SetDirty(col);

            return block;
        }

        /// <summary>
        /// Update data inside a datapage. If new data can be used in same datapage, just update. Otherwise, copy content to a new ExtendedPage
        /// </summary>
        public DataBlock Update(CollectionPage col, PageAddress blockAddress, byte[] data)
        {
            // get datapage and mark as dirty
            var dataPage = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = dataPage.GetBlock(blockAddress.Index);
            var extend = dataPage.FreeBytes + block.Data.Length - data.Length <= 0;

            // check if need to extend
            if (extend)
            {
                // clear my block data
                dataPage.UpdateBlockData(block, new byte[0]);

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
                // if no extends, just update data block
                dataPage.UpdateBlockData(block, data);

                // if there was a extended bytes, delete
                if (block.ExtendPageID != uint.MaxValue)
                {
                    _pager.DeletePage(block.ExtendPageID, true);
                    block.ExtendPageID = uint.MaxValue;
                }
            }

            // set DataPage as dirty
            _pager.SetDirty(dataPage);

            // add/remove dataPage on freelist if has space AND its on/off free list
            _pager.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.DATA_RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            return block;
        }

        /// <summary>
        /// Read all data from datafile using a pageID as reference. If data is not in DataPage, read from ExtendPage.
        /// </summary>
        public byte[] Read(PageAddress blockAddress)
        {
            var block = this.GetBlock(blockAddress);

            // if there is a extend page, read bytes all bytes from extended pages
            if (block.ExtendPageID != uint.MaxValue)
            {
                return this.ReadExtendData(block.ExtendPageID);
            }

            return block.Data;
        }

        /// <summary>
        /// Get a data block from a DataPage using address
        /// </summary>
        public DataBlock GetBlock(PageAddress blockAddress)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            return page.GetBlock(blockAddress.Index);
        }

        /// <summary>
        /// Read all data from a extended page with all subsequences pages if exits
        /// </summary>
        public byte[] ReadExtendData(uint extendPageID)
        {
            // read all extended pages and build byte array
            using (var buffer = new MemoryStream())
            {
                foreach (var extendPage in _pager.GetSeqPages<ExtendPage>(extendPageID))
                {
                    buffer.Write(extendPage.GetData(), 0, extendPage.ItemCount);
                }

                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Delete one dataBlock
        /// </summary>
        public DataBlock Delete(CollectionPage col, PageAddress blockAddress)
        {
            // get page and mark as dirty
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            var block = page.GetBlock(blockAddress.Index);

            // if there a extended page, delete all
            if (block.ExtendPageID != uint.MaxValue)
            {
                _pager.DeletePage(block.ExtendPageID, true);
            }

            // delete block inside page
            page.DeleteBlock(block);

            // set page as dirty here
            _pager.SetDirty(page);

            // if there is no more datablocks, lets delete all page
            if (page.BlocksCount == 0)
            {
                // first, remove from free list
                _pager.AddOrRemoveToFreeList(false, page, col, ref col.FreeDataPageID);

                _pager.DeletePage(page.PageID);
            }
            else
            {
                // add or remove to free list
                _pager.AddOrRemoveToFreeList(page.FreeBytes > DataPage.DATA_RESERVED_BYTES, page, col, ref col.FreeDataPageID);
            }

            col.DocumentCount--;

            // mark collection page as dirty
            _pager.SetDirty(col);

            return block;
        }

        /// <summary>
        /// Store all bytes in one extended page. If data ir bigger than a page, store in more pages and make all in sequence
        /// </summary>
        public void StoreExtendData(ExtendPage page, byte[] data)
        {
            var offset = 0;
            var bytesLeft = data.Length;

            while (bytesLeft > 0)
            {
                var bytesToCopy = Math.Min(bytesLeft, BasePage.PAGE_AVAILABLE_BYTES);

                page.SetData(data, offset, bytesToCopy);

                bytesLeft -= bytesToCopy;
                offset += bytesToCopy;

                // set extend page as dirty
                _pager.SetDirty(page);

                // if has bytes left, let's get a new page
                if (bytesLeft > 0)
                {
                    // if i have a continuous page, get it... or create a new one
                    page = page.NextPageID != uint.MaxValue ?
                        _pager.GetPage<ExtendPage>(page.NextPageID) :
                        _pager.NewPage<ExtendPage>(page);
                }
            }

            // when finish, check if last page has a nextPageId - if have, delete them
            if (page.NextPageID != uint.MaxValue)
            {
                // Delete nextpage and all nexts
                _pager.DeletePage(page.NextPageID, true);

                // set my page with no NextPageID
                page.NextPageID = uint.MaxValue;

                // set page as dirty
                _pager.SetDirty(page);
            }
        }
    }
}