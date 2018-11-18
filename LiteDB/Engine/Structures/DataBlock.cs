using System;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class DataBlock
    {
        /// <summary>
        /// Get fixed part of DataBlock
        /// </summary>
        public const int DATA_BLOCK_FIXED_SIZE = 6; // DataIndex[1] + NextBlock[5]

        private const int P_DATA_INDEX = 0; // 00
        private const int P_NEXT_BLOCK = 1; // 01-05
        private const int P_BUFFER = 6; // 06-EOF

        private readonly PageSegment _pageSegment;
        private readonly PageAddress _position;
        private readonly byte _dataIndex;
        private PageAddress _nextBlock;
        private readonly BufferSlice _buffer;

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
        public DataBlock(DataPage page, PageSegment pageSegment)
        {
            _pageSegment = pageSegment;
            _position = new PageAddress(page.PageID, pageSegment.Index);

            // byte 00: DataIndex
            _dataIndex = _pageSegment.Buffer[P_DATA_INDEX];

            // byte 01-05: NextBlock (PageID, Index)
            _nextBlock = _pageSegment.Buffer.ReadPageAddress(P_NEXT_BLOCK);

            // byte 06-EOL: Buffer
            _buffer = _pageSegment.Buffer.Slice(P_BUFFER, (_pageSegment.Length * PAGE_BLOCK_SIZE) - P_BUFFER- 1);
        }

        /// <summary>
        /// Read new DataBlock from filled pageSegment
        /// </summary>
        public DataBlock(DataPage page, PageSegment pageSegment, byte dataIndex, PageAddress nextBlock)
        {
            _pageSegment = pageSegment;
            _position = new PageAddress(page.PageID, pageSegment.Index);

            _nextBlock = nextBlock;
            _dataIndex = dataIndex;

            // byte 00: Data Index
            _pageSegment.Buffer[P_DATA_INDEX] = dataIndex;

            // byte 01-04 (can be updated in "UpdateNextBlock")
            _pageSegment.Buffer.Write(nextBlock, P_NEXT_BLOCK);

            // byte 06-EOL: Buffer
            _buffer = _pageSegment.Buffer.Slice(P_BUFFER, (_pageSegment.Length * PAGE_BLOCK_SIZE) - P_BUFFER - 1);
        }

        public void UpdateNextBlock(PageAddress nextBlock)
        {
            _nextBlock = nextBlock;

            // update segment buffer with NextBlock (uint + byte)
            _pageSegment.Buffer.Write(nextBlock, P_NEXT_BLOCK);
        }

        public override string ToString()
        {
            return $"Pos: [{_position}] - Seq: [{_dataIndex}] - Next: [{_nextBlock}] - Buffer: [{_buffer}]";
        }
    }
}