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
        /// Get page start block position
        /// </summary>
        public readonly byte Block;

        /// <summary>
        /// Get block position on page (in blocks)
        /// </summary>
        public readonly byte Length;

        /// <summary>
        /// Get buffer data of this page item
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

            ENSURE(length > 0, "length should always be > 0");
            ENSURE(block > 0, "page segment block position should always between 1 and 255");

            // store block length at initial first byte
            buffer[block * PAGE_BLOCK_SIZE] = length;

            // slice original page into a single buffer segment
            this.Buffer = buffer.Slice(block * PAGE_BLOCK_SIZE, this.Length * PAGE_BLOCK_SIZE);
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

            ENSURE(this.Length > 0, "page segment length should always be > 0");
            ENSURE(block > 0, "page segment block position should always between 1 and 255");

            // slice original page into a single buffer segment
            this.Buffer = buffer.Slice(block * PAGE_BLOCK_SIZE, this.Length * PAGE_BLOCK_SIZE);
        }

        /// <summary>
        /// Get content length in blocks (in byte). Add +1 byte in bytesLength for segment length (first byte)
        /// </summary>
        public static byte GetLength(int bytesLength)
        {
            return (byte)Math.Ceiling((double)(bytesLength + 1) / PAGE_BLOCK_SIZE);
        }

        public override string ToString()
        {
            return $"Slot: {this.Index} - Block: {this.Block} - Len: {this.Length}";
        }
    }
}