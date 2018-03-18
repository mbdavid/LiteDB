using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    internal class DataService
    {
        private Snapshot _snapshot;

        public DataService(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Insert data inside a datapage. Returns dataPageID that indicates the first page
        /// </summary>
        public DataBlock Insert(CollectionPage col, ChunkStream data)
        {
            // need to extend (data is bigger than 1 page)
            var extend = (data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE) > BasePage.PAGE_AVAILABLE_BYTES;

            // if extend, just search for a page with BLOCK_SIZE available
            var dataPage = _snapshot.GetFreePage<DataPage>(col.FreeDataPageID, extend ? DataBlock.DATA_BLOCK_FIXED_SIZE : (int)data.Length + DataBlock.DATA_BLOCK_FIXED_SIZE);

            // create a new block with first empty index on DataPage
            var block = new DataBlock { Page = dataPage, DocumentLength = (int)data.Length };

            // if extend, store all bytes on extended page.
            if (extend)
            {
                var extendPage = _snapshot.NewPage<ExtendPage>();
                block.ExtendPageID = extendPage.PageID;
                this.StoreExtendData(extendPage, data);
            }
            else
            {
                block.Data = data.ToArray();
            }

            // add dataBlock to this page
            dataPage.AddBlock(block);

            // set page as dirty
            _snapshot.SetDirty(dataPage);

            // add/remove dataPage on freelist if has space
            _snapshot.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.DATA_RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            // increase document count in collection
            col.DocumentCount++;

            // set collection page as dirty
            _snapshot.SetDirty(col);

            return block;
        }

        /// <summary>
        /// Update data inside a datapage. If new data can be used in same datapage, just update. Otherwise, copy content to a new ExtendedPage
        /// </summary>
        public DataBlock Update(CollectionPage col, PageAddress blockAddress, ChunkStream data)
        {
            // get datapage and mark as dirty
            var dataPage = _snapshot.GetPage<DataPage>(blockAddress.PageID);
            var block = dataPage.GetBlock(blockAddress.Index);
            var extend = dataPage.FreeBytes + block.Data.Length - data.Length <= 0;

            // update document length on data block
            block.DocumentLength = (int)data.Length;

            // check if need to extend
            if (extend)
            {
                // clear my block data
                dataPage.UpdateBlockData(block, new byte[0]);

                // create (or get a existed) extendpage and store data there
                ExtendPage extendPage;

                if (block.ExtendPageID == uint.MaxValue)
                {
                    extendPage = _snapshot.NewPage<ExtendPage>();
                    block.ExtendPageID = extendPage.PageID;
                }
                else
                {
                    extendPage = _snapshot.GetPage<ExtendPage>(block.ExtendPageID);
                }

                this.StoreExtendData(extendPage, data);
            }
            else
            {
                // if no extends, just update data block
                dataPage.UpdateBlockData(block, data.ToArray());

                // if there was a extended bytes, delete
                if (block.ExtendPageID != uint.MaxValue)
                {
                    _snapshot.DeletePage(block.ExtendPageID, true);
                    block.ExtendPageID = uint.MaxValue;
                }
            }

            // set DataPage as dirty
            _snapshot.SetDirty(dataPage);

            // add/remove dataPage on freelist if has space AND its on/off free list
            _snapshot.AddOrRemoveToFreeList(dataPage.FreeBytes > DataPage.DATA_RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            return block;
        }

        /// <summary>
        /// Create Stream with chunck data (from ExtendPages) or from datablock Data
        /// </summary>
        public ChunkStream Read(DataBlock block)
        {
            // return new chuckstream based on data source (Data[] or ExtendPage)
            return new ChunkStream(source(), block.BlockLength);

            // create data source based on byte[] - single Data on DataBlock or multiple pages
            IEnumerable<byte[]> source()
            {
                if (block.ExtendPageID == uint.MaxValue)
                {
                    yield return block.Data;
                }
                else
                {
                    foreach (var extendPage in _snapshot.GetSeqPages<ExtendPage>(block.ExtendPageID))
                    {
                        yield return extendPage.GetData();
                    }
                }
            };
        }

        /// <summary>
        /// Get a data block from a DataPage using address
        /// </summary>
        public DataBlock GetBlock(PageAddress blockAddress)
        {
            var page = _snapshot.GetPage<DataPage>(blockAddress.PageID);
            return page.GetBlock(blockAddress.Index);
        }

        /// <summary>
        /// Delete one dataBlock
        /// </summary>
        public DataBlock Delete(CollectionPage col, PageAddress blockAddress)
        {
            // get page and mark as dirty
            var page = _snapshot.GetPage<DataPage>(blockAddress.PageID);
            var block = page.GetBlock(blockAddress.Index);

            // if there a extended page, delete all
            if (block.ExtendPageID != uint.MaxValue)
            {
                _snapshot.DeletePage(block.ExtendPageID, true);
            }

            // delete block inside page
            page.DeleteBlock(block);

            // set page as dirty here
            _snapshot.SetDirty(page);

            // if there is no more datablocks, lets delete all page
            if (page.BlocksCount == 0)
            {
                // first, remove from free list
                _snapshot.AddOrRemoveToFreeList(false, page, col, ref col.FreeDataPageID);

                _snapshot.DeletePage(page.PageID);
            }
            else
            {
                // add or remove to free list
                _snapshot.AddOrRemoveToFreeList(page.FreeBytes > DataPage.DATA_RESERVED_BYTES, page, col, ref col.FreeDataPageID);
            }

            col.DocumentCount--;

            // mark collection page as dirty
            _snapshot.SetDirty(col);

            return block;
        }

        /// <summary>
        /// Store all bytes in one extended page. If data ir bigger than a page, store in more pages and make all in sequence
        /// </summary>
        public void StoreExtendData(ExtendPage page, ChunkStream data)
        {
            var bytesLeft = (int)data.Length;
            var buffer = new byte[BasePage.PAGE_AVAILABLE_BYTES];

            while (bytesLeft > 0)
            {
                var bytesToCopy = Math.Min(bytesLeft, BasePage.PAGE_AVAILABLE_BYTES);

                data.Read(buffer, 0, bytesToCopy);

                page.SetData(buffer, 0, bytesToCopy);

                bytesLeft -= bytesToCopy;

                // set extend page as dirty
                _snapshot.SetDirty(page);

                // if has bytes left, let's get a new page
                if (bytesLeft > 0)
                {
                    // if i have a continuous page, get it... or create a new one
                    page = page.NextPageID != uint.MaxValue ?
                        _snapshot.GetPage<ExtendPage>(page.NextPageID) :
                        _snapshot.NewPage<ExtendPage>(page);
                }
            }

            // when finish, check if last page has a nextPageId - if have, delete them
            if (page.NextPageID != uint.MaxValue)
            {
                // Delete nextpage and all nexts
                _snapshot.DeletePage(page.NextPageID, true);

                // set my page with no NextPageID
                page.NextPageID = uint.MaxValue;

                // set page as dirty
                _snapshot.SetDirty(page);
            }
        }
    }
}