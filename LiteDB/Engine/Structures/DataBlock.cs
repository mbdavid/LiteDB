using System;

namespace LiteDB
{
    internal class DataBlock
    {
        public const int DATA_BLOCK_FIXED_SIZE = 2 + // Position.Index (ushort)
                                                 4 + // ExtendedPageID (uint)
                                                 2; // block.Data.Length (ushort)

        /// <summary>
        /// Position of this dataBlock inside a page (store only Position.Index)
        /// </summary>
        public PageAddress Position { get; set; }

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
        /// Get length of this dataBlock (persist as ushort 2 bytes)
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
        }
    }
}