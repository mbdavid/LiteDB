using System;
using System.IO;

namespace LiteDB_V6
{
    internal class DataService
    {
        private PageService _pager;

        public DataService(PageService pager)
        {
            _pager = pager;
        }
		
        /// <summary>
        /// Read all data from datafile using a pageID as reference. If data is not in DataPage, read from ExtendPage.
        /// </summary>
        public byte[] Read(LiteDB.PageAddress blockAddress)
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
        public DataBlock GetBlock(LiteDB.PageAddress blockAddress)
        {
            var page = _pager.GetPage<DataPage>(blockAddress.PageID);
            return page.DataBlocks[blockAddress.Index];
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
                    buffer.Write(extendPage.Data, 0, extendPage.Data.Length);
                }

                return buffer.ToArray();
            }
        }
    }
}