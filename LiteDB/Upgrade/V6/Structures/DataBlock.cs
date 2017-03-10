using System;

namespace LiteDB_V6
{
    internal class DataBlock
    {
        /// <summary>
        /// Position of this dataBlock inside a page (store only Position.Index)
        /// </summary>
        public LiteDB.PageAddress Position { get; set; }

        /// <summary>
        /// Indexes nodes for all indexes for this data block
        /// </summary>
        public LiteDB.PageAddress[] IndexRef { get; set; }

        /// <summary>
        /// If object is bigger than this page - use a ExtendPage (and do not use Data array)
        /// </summary>
        public uint ExtendPageID { get; set; }

        /// <summary>
        /// Data of a record - could be empty if is used in ExtedPage
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Get a reference for page
        /// </summary>
        public DataPage Page { get; set; }

        public DataBlock()
        {
            this.Position = LiteDB.PageAddress.Empty;
            this.ExtendPageID = uint.MaxValue;
            this.Data = new byte[0];

            this.IndexRef = new LiteDB.PageAddress[CollectionIndex.INDEX_PER_COLLECTION];

            for (var i = 0; i < CollectionIndex.INDEX_PER_COLLECTION; i++)
            {
                this.IndexRef[i] = LiteDB.PageAddress.Empty;
            }
        }
    }
}