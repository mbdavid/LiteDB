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

        private const int P_DATA_INDEX = 0; // 00
        private const int P_NEXT_BLOCK = 1; // 01-05
        private const int P_BUFFER = 6; // 06-EOF

        private readonly PageAddress _position;
        private readonly byte _dataIndex;
        private PageAddress _nextBlock;
        private readonly BufferSlice _buffer;

        private readonly PageSegment _segment;

        /// <summary>
        /// Position block inside page
        /// </summary>
        public PageAddress Position => _position;

        /// <summary>
        /// Data index block (single document can use 0-255 index blocks)
        /// </summary>
        public byte DataIndex => _dataIndex;

        /// <summary>
        /// If document need more than 1 block, use this link to next block
        /// </summary>
        public PageAddress NextBlock => _nextBlock;

        /// <summary>
        /// Document buffer slice
        /// </summary>
        public BufferSlice Buffer => _buffer;

        /// <summary>
        /// Read new DataBlock from filled pageSegment
        /// </summary>
        public DataBlock(DataPage page, PageSegment segment)
        {
            _segment = segment;
            _position = new PageAddress(page.PageID, segment.Index);

            // byte 00: DataIndex
            _dataIndex = _segment.Buffer[P_DATA_INDEX];

            // byte 01-05: NextBlock (PageID, Index)
            _nextBlock = _segment.Buffer.ReadPageAddress(P_NEXT_BLOCK);

            // byte 06-EOL: Buffer
            _buffer = _segment.Buffer.Slice(P_BUFFER, (_segment.Length * PAGE_BLOCK_SIZE) - P_BUFFER- 1);
        }

        /// <summary>
        /// Create new DataBlock and fill into buffer
        /// </summary>
        public DataBlock(DataPage page, PageSegment segment, byte dataIndex, PageAddress nextBlock)
        {
            _segment = segment;
            _position = new PageAddress(page.PageID, segment.Index);

            _nextBlock = nextBlock;
            _dataIndex = dataIndex;

            // byte 00: Data Index
            _segment.Buffer[P_DATA_INDEX] = dataIndex;

            // byte 01-04 (can be updated in "UpdateNextBlock")
            _segment.Buffer.Write(nextBlock, P_NEXT_BLOCK);

            // byte 06-EOL: Buffer
            _buffer = _segment.Buffer.Slice(P_BUFFER, (_segment.Length * PAGE_BLOCK_SIZE) - P_BUFFER - 1);
        }

        public void SetNextBlock(PageAddress nextBlock)
        {
            _nextBlock = nextBlock;

            // update segment buffer with NextBlock (uint + byte)
            _segment.Buffer.Write(nextBlock, P_NEXT_BLOCK);
        }

        public override string ToString()
        {
            return $"Pos: [{_position}] - Seq: [{_dataIndex}] - Next: [{_nextBlock}] - Buffer: [{_buffer}]";
        }
    }
}