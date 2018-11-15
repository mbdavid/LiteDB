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
        private readonly ArraySlice<byte> _buffer;

        public PageAddress Position => _position;
        public byte DataIndex => _dataIndex;
        public PageAddress NextBlock => _nextBlock;
        public ArraySlice<byte> Buffer => _buffer;

        /// <summary>
        /// Read new DataBlock from filled pageSegment
        /// </summary>
        public DataBlock(DataPage page, PageSegment pageSegment)
        {
            _pageSegment = pageSegment;
            _position = new PageAddress(page.PageID, pageSegment.Index);

            // byte 00: DataIndex
            _dataIndex = _pageSegment.Buffer[0];

            // byte 01-04: NextBlock (PageID, Index)
            _nextBlock = new PageAddress(
                BitConverter.ToUInt32(_pageSegment.Buffer.Array, _pageSegment.Buffer.Offset + 1),
                _pageSegment.Buffer[5]);

            // byte 06-EOF: Buffer
            _buffer = new ArraySlice<byte>(
                _pageSegment.Buffer.Array,
                _pageSegment.Buffer.Offset + 6,
                (_pageSegment.Length * PAGE_BLOCK_SIZE) - 6);
        }

        /// <summary>
        /// Read new DataBlock from filled pageSegment
        /// </summary>
        public DataBlock(DataPage page, PageSegment pageSegment, byte dataIndex)
        {
            _pageSegment = pageSegment;
            _position = new PageAddress(page.PageID, pageSegment.Index);

            // byte 00: Data Index
            _pageSegment.Buffer[0] = dataIndex;

            // byte 01-04 (will be updated in "UpdateNextBlock")
            _nextBlock = PageAddress.Empty;

            // byte 06-EOF: Buffer
            _buffer = new ArraySlice<byte>(
                _pageSegment.Buffer.Array,
                _pageSegment.Buffer.Offset + 6,
                (_pageSegment.Length * PAGE_BLOCK_SIZE) - 6);
        }

        public void UpdateNextBlock(PageAddress nextBlock)
        {
            _nextBlock = nextBlock;

            // update segment buffer with NextBlock (uint + byte)
            nextBlock.PageID.ToBytes(_pageSegment.Buffer.Array, _pageSegment.Buffer.Offset + 1); // 01-04
            _pageSegment.Buffer[5] = nextBlock.Index; // 05
        }
    }
}