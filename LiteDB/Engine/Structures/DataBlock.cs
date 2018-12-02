using System;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class DataBlock
    {
        /// <summary>
        /// Get fixed part of DataBlock
        /// </summary>
        public const int DATA_BLOCK_FIXED_SIZE = 1 + // DataIndex
                                                 PageAddress.SIZE; // NextBlock

        // private const int P_SEGMENT_LENGTH = 0;
        private const int P_DATA_INDEX = 1; // 01
        private const int P_NEXT_BLOCK = 2; // 02-06
        private const int P_BUFFER = 7; // 07-EOF

        private readonly PageSegment _segment;

        /// <summary>
        /// Position block inside page
        /// </summary>
        public PageAddress Position { get; }

        /// <summary>
        /// Data index block (single document can use 0-255 index blocks)
        /// </summary>
        public byte DataIndex { get; }

        /// <summary>
        /// If document need more than 1 block, use this link to next block
        /// </summary>
        public PageAddress NextBlock { get; private set; }

        /// <summary>
        /// Document buffer slice
        /// </summary>
        public BufferSlice Buffer { get; }

        /// <summary>
        /// Read new DataBlock from filled pageSegment
        /// </summary>
        public DataBlock(DataPage page, PageSegment segment)
        {
            _segment = segment;

            this.Position = new PageAddress(page.PageID, segment.Index);

            // byte 01: DataIndex
            this.DataIndex = segment.Buffer[P_DATA_INDEX];

            // byte 02-06: NextBlock (PageID, Index)
            this.NextBlock = segment.Buffer.ReadPageAddress(P_NEXT_BLOCK);

            // byte 07-EOL: Buffer
            this.Buffer = segment.Buffer.Slice(P_BUFFER, (segment.Length * PAGE_BLOCK_SIZE) - P_BUFFER);
        }

        /// <summary>
        /// Create new DataBlock and fill into buffer
        /// </summary>
        public DataBlock(DataPage page, PageSegment segment, byte dataIndex, PageAddress nextBlock)
        {
            _segment = segment;

            this.Position = new PageAddress(page.PageID, segment.Index);

            this.NextBlock = nextBlock;
            this.DataIndex = dataIndex;

            // byte 01: Data Index
            segment.Buffer[P_DATA_INDEX] = dataIndex;

            // byte 02-06 (can be updated in "UpdateNextBlock")
            segment.Buffer.Write(nextBlock, P_NEXT_BLOCK);

            // byte 07-EOL: Buffer
            this.Buffer = segment.Buffer.Slice(P_BUFFER, (segment.Length * PAGE_BLOCK_SIZE) - P_BUFFER);
        }

        public void SetNextBlock(PageAddress nextBlock)
        {
            this.NextBlock = nextBlock;

            // update segment buffer with NextBlock (uint + byte)
            _segment.Buffer.Write(nextBlock, P_NEXT_BLOCK);
        }

        public override string ToString()
        {
            return $"Pos: [{this.Position}] - Seq: [{this.DataIndex}] - Next: [{this.NextBlock}] - Buffer: [{this.Buffer}]";
        }
    }
}