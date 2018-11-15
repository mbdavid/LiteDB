using System;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a linear, continuous segment of data inside a page. Can contains 1 or more page blocks. Units are in block
    /// </summary>
    internal struct PageSegment
    {
        /// <summary>
        /// Get segment index on footer of page
        /// </summary>
        public readonly byte Index;

        /// <summary>
        /// Get block position on page (in blocks)
        /// </summary>
        public readonly byte Length;

        /// <summary>
        /// Get page item size (in blocks)
        /// </summary>
        public readonly byte Block;

        /// <summary>
        /// Get buffer data of this page item (contains 1 byte less because was used to store length)
        /// </summary>
        public readonly BufferSlice Buffer;

        /// <summary>
        /// Create new page segment inside page
        /// </summary>
        public PageSegment(PageBuffer buffer, byte index, byte block, byte length)
        {
            this.Index = index;
            this.Block = block;
            this.Length = length;

            // store block length at initial first byte
            buffer[block * PAGE_BLOCK_SIZE] = length;

            // slice original buffer removing this first byte
            this.Buffer = buffer.Slice((block * PAGE_BLOCK_SIZE) + 1, (this.Length * PAGE_BLOCK_SIZE) - 1);
        }

        /// <summary>
        /// Load a segment that was already inside page
        /// </summary>
        public PageSegment(PageBuffer buffer, byte index, byte block)
        {
            this.Index = index;
            this.Block = block;

            // read block length
            this.Length = buffer[block * PAGE_BLOCK_SIZE];

            // slice original buffer removing this first byte
            this.Buffer = buffer.Slice((block * PAGE_BLOCK_SIZE) + 1, (this.Length * PAGE_BLOCK_SIZE) - 1);
        }

        public override string ToString()
        {
            return $"Slot: {this.Index} - Block: {this.Block} - Len: {this.Length}";
        }
    }
}