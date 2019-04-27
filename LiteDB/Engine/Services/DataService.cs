using System;
using System.Collections.Generic;
using System.IO;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class DataService
    {
        /// <summary>
        /// Get maximum data bytes[] that fit in 1 page = 8149
        /// </summary>
        public const int MAX_DATA_BYTES_PER_PAGE =
            PAGE_SIZE - // 8192
            PAGE_HEADER_SIZE - // [32 bytes]
            BasePage.SLOT_SIZE - // [4 bytes]
            DataBlock.DATA_BLOCK_FIXED_SIZE; // [6 bytes];

        private Snapshot _snapshot;

        public DataService(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Insert BsonDocument into new data pages
        /// </summary>
        public PageAddress Insert(BsonDocument doc)
        {
            var bytesLeft = doc.GetBytesCount();

            if (bytesLeft > MAX_DOCUMENT_SIZE) throw new LiteException(0, "Document size exceed {0} limit", MAX_DOCUMENT_SIZE);

            var firstBlock = PageAddress.Empty;

            IEnumerable<BufferSlice> source()
            {
                byte dataIndex = 0;
                DataBlock lastBlock = null;

                while (bytesLeft > 0)
                {
                    var bytesToCopy = Math.Min(bytesLeft, MAX_DATA_BYTES_PER_PAGE);
                    var dataPage = _snapshot.GetFreePage<DataPage>(bytesToCopy + DataBlock.DATA_BLOCK_FIXED_SIZE);
                    var dataBlock = dataPage.InsertBlock(bytesToCopy, dataIndex);

                    dataIndex++;

                    if (lastBlock != null)
                    {
                        lastBlock.SetNextBlock(dataBlock.Position);
                    }

                    if (firstBlock.IsEmpty) firstBlock = dataBlock.Position;

                    yield return dataBlock.Buffer;

                    lastBlock = dataBlock;

                    bytesLeft -= bytesToCopy;
                }
            }

            // consume all source bytes to write BsonDocument direct into PageBuffer
            // must be fastest as possible
            using (var w = new BufferWriter(source()))
            {
                w.WriteDocument(doc);
                w.Consume();
            }

            return firstBlock;
        }

        /// <summary>
        /// Update data inside a datapage. If new data can be used in same datapage, just update. Otherwise, copy content to a new ExtendedPage
        /// </summary>
        public DataBlock Update(CollectionPage col, PageAddress blockAddress, BsonDocument doc)
        {
            throw new NotImplementedException(); /*
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
                    _snapshot.DeletePages(block.ExtendPageID);
                    block.ExtendPageID = uint.MaxValue;
                }
            }

            // set DataPage as dirty
            _snapshot.SetDirty(dataPage);

            // add/remove dataPage on freelist if has space AND its on/off free list
            _snapshot.AddOrRemoveToFreeList(dataPage.FreeBytes > DATA_RESERVED_BYTES, dataPage, col, ref col.FreeDataPageID);

            return block;*/
        }

        /// <summary>
        /// Get all buffer slices that address block contains. Need use BufferReader to read document
        /// </summary>
        public IEnumerable<BufferSlice> Read(PageAddress address)
        {
            while (address != PageAddress.Empty)
            {
                var dataPage = _snapshot.GetPage<DataPage>(address.PageID);

                var block = dataPage.GetBlock(address.Index);

                yield return block.Buffer;

                address = block.NextBlock;
            }
        }

        /// <summary>
        /// Delete all datablock that contains a document (can use multiples data blocks)
        /// </summary>
        public void Delete(PageAddress blockAddress)
        {
            var index = 0;

            // delete all document blocks
            while(blockAddress != PageAddress.Empty)
            {
                var page = _snapshot.GetPage<DataPage>(blockAddress.PageID);
                var block = page.GetBlock(blockAddress.Index);
                var slot = BasePage.FreeIndexSlot(page.FreeBytes);

                ENSURE(block.DataIndex == index++, "blocks must be in order");

                // delete block inside page
                page.DeleteBlock(blockAddress.Index);

                // fix page empty list (or delete page)
                _snapshot.AddOrRemoveFreeList(page, slot);

                blockAddress = block.NextBlock;
            }
        }

        /// <summary>
        /// Read all documents from current collection with NO index use - read direct from free lists
        /// There is no document order
        /// </summary>
        public IEnumerable<BsonDocument> ReadAll(HashSet<string> fields = null)
        {
            for (var slot = 0; slot < CollectionPage.PAGE_FREE_LIST_SLOTS; slot++)
            {
                var next = _snapshot.CollectionPage.FreeDataPageID[slot];

                while (next != uint.MaxValue)
                {
                    var page = _snapshot.GetPage<DataPage>(next);

                    foreach (var block in page.GetBlocks(true))
                    {
                        using (var r = new BufferReader(this.Read(block)))
                        {
                            var doc = r.ReadDocument(fields);

                            yield return doc;
                        }
                    }

                    next = page.NextPageID;
                }
            }
        }
    }
}