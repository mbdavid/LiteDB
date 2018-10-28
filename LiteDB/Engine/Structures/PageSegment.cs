using System;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a linear, continuous segment of data inside a page. Can contains 1 or more page blocks. Units are in block
    /// </summary>
    internal struct PageSegment
    {
        private readonly byte _index;
        private readonly byte _length;
        private readonly byte _block;
        private readonly ArraySlice<byte> _buffer;

        /// <summary>
        /// Get segment index on footer of page
        /// </summary>
        public byte Index => _index;

        /// <summary>
        /// Get block position on page (in blocks)
        /// </summary>
        public byte Block => _block;

        /// <summary>
        /// Get page item size (in blocks)
        /// </summary>
        public byte Length => _length;

        /// <summary>
        /// Get buffer data of this page item (contains 1 byte less because was used to store length)
        /// </summary>
        public ArraySlice<byte> Buffer => _buffer;

        /// <summary>
        /// Create new page segment inside page
        /// </summary>
        public PageSegment(PageBuffer page, byte index, byte block, byte length)
        {
            _index = index;
            _block = block;
            _length = length;

            // store block length at initial first byte
            page[block * PAGE_BLOCK_SIZE] = length;

            // slice original buffer removing this first byte
            _buffer = new ArraySlice<byte>(page.Array,
                page.Offset + (block * PAGE_BLOCK_SIZE) + 1,
                (_length * PAGE_BLOCK_SIZE) - 1);
        }

        /// <summary>
        /// Load a segment that was already inside page
        /// </summary>
        public PageSegment(PageBuffer page, byte index, byte block)
        {
            _index = index;
            _block = block;

            // read block length
            _length = page[block * PAGE_BLOCK_SIZE];

            // slice original buffer removing this first byte
            _buffer = new ArraySlice<byte>(page.Array,
                page.Offset + (block * PAGE_BLOCK_SIZE) + 1,
                (_length * PAGE_BLOCK_SIZE) - 1);
        }
    }
}