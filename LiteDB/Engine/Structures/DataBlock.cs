using System;

namespace LiteDB.Engine
{
    internal class DataBlock
    {
        public const int DATA_BLOCK_FIXED_SIZE = 2 + // Position.Index (ushort)
                                                  4 + // ExtendedPageID (uint)
                                                  4 + // DocumentLength (int)
                                                  2;  // block.Data.Length (ushort)

        /// <summary>
        /// Position of this dataBlock inside a page (store only Position.Index)
        /// </summary>
        public PageAddress Position { get; set; }

        /// <summary>
        /// If object is bigger than this page - use a ExtendPage (and do not use Data array)
        /// </summary>
        public uint ExtendPageID { get; set; }

        /// <summary>
        /// Get document length (from Data array or from ExtendPages)
        /// </summary>
        public int DocumentLength { get; set; }

        /// <summary>
        /// Data of a record - could be empty if is used in ExtedPage
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Get length of this dataBlock (persist as ushort 2 bytes)
        /// </summary>
        public int BlockLength
        {
            get { return DATA_BLOCK_FIXED_SIZE + this.Data.Length; }
        }

        public DataBlock()
        {
            this.Position = PageAddress.Empty;
            this.ExtendPageID = uint.MaxValue;
            this.Data = new byte[0];
            this.DocumentLength = 0;
        }

        public DataBlock Clone()
        {
            var data = new byte[this.Data.Length];

            Buffer.BlockCopy(this.Data, 0, data, 0, data.Length);

            return new DataBlock
            {
                Position = this.Position,
                ExtendPageID = this.ExtendPageID,
                Data = data,
                DocumentLength = this.DocumentLength
            };
        }
    }
}