namespace LiteDB
{
    internal class DataBlock
    {
        public const int DATA_BLOCK_FIXED_SIZE = 2 + // Position.Index
                                                 4 + // ExtendedPageID (uint)
                                                 (PageAddress.SIZE * CollectionIndex.INDEX_PER_COLLECTION) + // IndexRef pointer
                                                 2; // block.Data.Length (ushort)

        /// <summary>
        /// Position of this dataBlock inside a page (store only Position.Index)
        /// </summary>
        public PageAddress Position { get; set; }

        /// <summary>
        /// Indexes nodes for all indexes for this data block
        /// </summary>
        public PageAddress[] IndexRef { get; set; }

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

        /// <summary>
        /// Get length of this dataBlock - not persistable
        /// </summary>
        public int Length
        {
            get { return DataBlock.DATA_BLOCK_FIXED_SIZE + this.Data.Length; }
        }

        public DataBlock()
        {
            this.Position = PageAddress.Empty;
            this.ExtendPageID = uint.MaxValue;
            this.Data = new byte[0];

            this.IndexRef = new PageAddress[CollectionIndex.INDEX_PER_COLLECTION];

            for (var i = 0; i < CollectionIndex.INDEX_PER_COLLECTION; i++)
            {
                this.IndexRef[i] = PageAddress.Empty;
            }
        }
    }
}