using System;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class DataBlock
    {
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
            _dataIndex = _pageSegment.Buffer[0];

            // byte 01-05: NextBlock (PageID, Index)
            _nextBlock = new PageAddress(_pageSegment.Buffer.ReadUInt32(1), _pageSegment.Buffer[5]);

            // byte 06-EOL: Buffer
            _buffer = _pageSegment.Buffer.Slice(6, (_pageSegment.Length * PAGE_BLOCK_SIZE) - 6 - 1);
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
            _pageSegment.Buffer[0] = dataIndex;

            // byte 01-04 (can be updated in "UpdateNextBlock")
            _pageSegment.Buffer.Write(nextBlock.PageID, 1); // 01-04
            _pageSegment.Buffer[5] = nextBlock.Index; // 05

            // byte 06-EOL: Buffer
            _buffer = _pageSegment.Buffer.Slice(6, (_pageSegment.Length * PAGE_BLOCK_SIZE) - 6 - 1);
        }

        public void UpdateNextBlock(PageAddress nextBlock)
        {
            _nextBlock = nextBlock;

            // update segment buffer with NextBlock (uint + byte)
            _pageSegment.Buffer.Write(nextBlock.PageID, 1); // 01-04
            _pageSegment.Buffer[5] = nextBlock.Index; // 05
        }

        public override string ToString()
        {
            return $"Pos: [{_position}] - Seq: [{_dataIndex}] - Next: [{_nextBlock}] - Buffer: [{_buffer}]";
        }
    }
}