using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4 }

    internal class BasePage
    {
        protected readonly PageBuffer _buffer;

        #region Buffer Field Positions

        private const int P_PAGE_ID = 0;  // 00-03
        private const int P_PAGE_TYPE = 4; // 04
        private const int P_PREV_PAGE_ID = 5; // 05-08
        private const int P_NEXT_PAGE_ID = 9; // 09-12
        private const int P_CRC = 13;

        private const int P_TRANSACTION_ID = 14; // 14-21
        private const int P_IS_CONFIRMED = 22;
        private const int P_COL_ID = 23; // 23-26

        private const int P_ITEMS_COUNT = 27;
        private const int P_USED_CONTENT_BLOCKS = 28;
        private const int P_FRAGMENTED_BLOCKS = 29;
        private const int P_NEXT_FREE_BLOCK = 30;
        private const int P_HIGHEST_INDEX = 31;

        #endregion

        /// <summary>
        /// Represent page number - start in 0 with HeaderPage [4 bytes]
        /// </summary>
        public uint PageID { get; private set; }

        /// <summary>
        /// Indicate the page type [1 byte]
        /// </summary>
        public PageType PageType { get; private set; }

        /// <summary>
        /// Represent the previous page. Used for page-sequences - MaxValue represent that has NO previous page [4 bytes]
        /// </summary>
        public uint PrevPageID { get; set; }

        /// <summary>
        /// Represent the next page. Used for page-sequences - MaxValue represent that has NO next page [4 bytes]
        /// </summary>
        public uint NextPageID { get; set; }

        /// <summary>
        /// Page CRC8 - page CRC are calculated from byte 14 to 8191
        /// </summary>
        public byte CRC { get; private set; }

        /// <summary>
        /// Indicate how many items are used inside this page [1 byte]
        /// </summary>
        public byte ItemsCount { get; private set; }

        /// <summary>
        /// Get how many blocks are used on content area (exclude header and footer blocks) [1 byte]
        /// </summary>
        public byte UsedContentBlocks { get; private set; }

        /// <summary>
        /// Get how many blocks are fragmented (free blocks inside used blocks) [1 byte]
        /// </summary>
        public byte FragmentedBlocks { get; private set; }

        /// <summary>
        /// Get next free block. Starts with block 3 (first after header) - It always at end of last block - there is no fragmentation after this [1 byte]
        /// </summary>
        public byte NextFreeBlock { get; private set; }

        /// <summary>
        /// Get last (highest) used index slot [1 byte]
        /// </summary>
        public byte HighestIndex { get; private set; }

        /// <summary>
        /// Get how many blocks are available in this page (content area) - consider footer blocks too
        /// </summary>
        public byte FreeBlocks => (byte)(PAGE_AVAILABLE_BLOCKS - this.UsedContentBlocks - this.FooterBlocks);

        /// <summary>
        /// Get calculated how many blocks footer (index space) are used
        /// </summary>
        public byte FooterBlocks => (byte)((this.HighestIndex / PAGE_BLOCK_SIZE) + 1);

        /// <summary>
        /// Set in all datafile pages the page id about data/index collection. Useful if want re-build database without any index [4 bytes]
        /// </summary>
        public uint ColID { get; set; }

        /// <summary>
        /// Represent transaction ID that was stored [8 bytes]
        /// </summary>
        public long TransactionID { get; set; }

        /// <summary>
        /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte]
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Set this pages that was changed and must be persist in disk [not peristable]
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Get internal buffer for this page
        /// </summary>
        public PageBuffer Buffer => _buffer;

        #region Initialize/Update buffer

        /// <summary>
        /// Create new Page based on pre-defined PageID and PageType
        /// </summary>
        public BasePage(PageBuffer buffer, uint pageID, PageType pageType)
        {
            _buffer = buffer;

            // page information
            this.PageID = pageID;
            this.PageType = pageType;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;
            this.CRC = 0;

            // block information
            this.ItemsCount = 0;
            this.UsedContentBlocks = 0;
            this.FragmentedBlocks = 0;
            this.NextFreeBlock = PAGE_HEADER_SIZE / PAGE_BLOCK_SIZE; // first block index should be 3 (blocks 0, 1, 2 are reserved to header)
            this.HighestIndex = 0;

            // default data
            this.ColID = uint.MaxValue;
            this.TransactionID = long.MaxValue;
            this.IsConfirmed = false;

            this.IsDirty = false;

            // writing direct into buffer in Ctor() because there is no change later (write once)
            this.PageID.ToBytes(_buffer.Array, _buffer.Offset + 0); // 00-03
            _buffer[4] = (byte)this.PageType; // 04-04

        }

        /// <summary>
        /// Read header data from byte[] buffer into local variables
        /// using fixed position be be faster than use BufferReader
        /// </summary>
        public BasePage(PageBuffer buffer)
        {
            _buffer = buffer;

            // page information
            this.PageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_PAGE_ID);
            this.PageType = (PageType)_buffer[P_PAGE_TYPE];
            this.PrevPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_PREV_PAGE_ID);
            this.NextPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_NEXT_PAGE_ID);
            this.CRC = _buffer[P_CRC];

            // transaction information
            this.ColID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_COL_ID);
            this.TransactionID = BitConverter.ToInt64(_buffer.Array, _buffer.Offset + P_TRANSACTION_ID);
            this.IsConfirmed = BitConverter.ToBoolean(_buffer.Array, _buffer.Offset + P_IS_CONFIRMED);

            // blocks information
            this.ItemsCount = _buffer[P_ITEMS_COUNT];
            this.UsedContentBlocks = _buffer[P_USED_CONTENT_BLOCKS];
            this.FragmentedBlocks = _buffer[P_FRAGMENTED_BLOCKS];
            this.NextFreeBlock = _buffer[P_NEXT_FREE_BLOCK];
            this.HighestIndex = _buffer[P_HIGHEST_INDEX];

        }

        /// <summary>
        /// Write header data from variable into byte[] buffer. When override, call base.UpdateBuffer() after write your code
        /// </summary>
        public virtual PageBuffer UpdateBuffer()
        {
            // using fixed position to be faster than BufferWriter

            // page information
            // PageID   - never change!
            // PageType - never change!
            this.PrevPageID.ToBytes(_buffer.Array, _buffer.Offset + P_PREV_PAGE_ID);
            this.NextPageID.ToBytes(_buffer.Array, _buffer.Offset + P_NEXT_PAGE_ID);

            // block information
            _buffer[P_ITEMS_COUNT] = this.ItemsCount;
            _buffer[P_USED_CONTENT_BLOCKS] = this.UsedContentBlocks;
            _buffer[P_FRAGMENTED_BLOCKS] = this.FragmentedBlocks;
            _buffer[P_NEXT_FREE_BLOCK] = this.NextFreeBlock;
            _buffer[P_HIGHEST_INDEX] = this.HighestIndex;

            // transaction information
            this.ColID.ToBytes(_buffer.Array, _buffer.Offset + P_COL_ID);
            this.TransactionID.ToBytes(_buffer.Array, _buffer.Offset + P_TRANSACTION_ID);
            _buffer[P_IS_CONFIRMED] = this.IsConfirmed ? (byte)1 : (byte)0;

            // last serialize field must be CRC checksum
            _buffer[P_CRC] = this.ComputeChecksum();

            return _buffer;
        }

        #endregion

        #region Access/Manipulate PageSegments

        /// <summary>
        /// Get a page item based on index slot
        /// </summary>
        public PageSegment Get(byte index)
        {
            var block = _buffer[PAGE_SIZE - index - 1];

            return new PageSegment(_buffer, index, block);
        }

        /// <summary>
        /// Create a new page item and return PageItem as reference to be buffer fill outside.
        /// Do not add 1 extra byte in "bytesLength" for store segment length (will be added inside this method)
        /// </summary>
        protected PageSegment Insert(int bytesLength)
        {
            // length in blocks (+1 to consider store length inside page segment)
            var length = (byte)((bytesLength / PAGE_BLOCK_SIZE) + 1);

            return this.Insert(this.GetFreeIndex(), length);
        }

        /// <summary>
        /// Internal implementation with index as parameter (used also in Update)
        /// </summary>
        private PageSegment Insert(byte index, byte length)
        {
            DEBUG(this.FreeBlocks < length, "length must be always lower than current free space");
            DEBUG(_buffer.ShareCounter != -1, "page must be writable to support changes");

            // update highest slot if this slot are new highest
            if (index > this.HighestIndex) this.HighestIndex = index;

            // calculate how many continuous blocks are avaiable in this page
            var continuosBlocks = this.FreeBlocks - this.FragmentedBlocks;

            // if continuous blocks are not big enouth for this data, must run page defrag
            if (length > continuosBlocks)
            {
                this.Defrag();

                DEBUG(length > (this.FreeBlocks - this.FragmentedBlocks), "after defrag length must fit in page");
            }

            // get a free index slot
            var block = this.NextFreeBlock;

            DEBUG(_buffer[PAGE_SIZE - index - 1] != 0, "slot must be empty before use");

            // update index slot with this block position
            _buffer[PAGE_SIZE - index - 1] = block;

            // and update for next insert
            this.NextFreeBlock += length;

            // update counters
            this.ItemsCount++;
            this.UsedContentBlocks += length;

            // create page item (will set length in first byte block)
            return new PageSegment(_buffer, index, block, length);
        }

        /// <summary>
        /// Remove index slot about this page item (will not clean page item data space)
        /// </summary>
        protected PageSegment Delete(byte index)
        {
            // read block position on index slot
            var slot = PAGE_SIZE - index - 1;
            var block = _buffer[slot];

            DEBUG(block < 3, "existing page segment must contains a valid block position (after header)");
            DEBUG(_buffer.ShareCounter != -1, "page must be writable to support changes");

            var position = block * PAGE_BLOCK_SIZE;

            // read how many blocks this segment use
            var blocks = _buffer[position];

            // clear slot index
            _buffer[slot] = (byte)0x00;

            // add as free blocks
            this.ItemsCount--;
            this.UsedContentBlocks -= blocks;

            if (this.HighestIndex != index)
            {
                // if segment is in middle, add this blocks as fragment block
                this.FragmentedBlocks += blocks;
            }
            else
            {
                this.UpdateHighestIndex();
            }

            // set page as dirty
            this.IsDirty = true;

            // get deleted page segment
            return new PageSegment(_buffer, index, block, blocks);
        }

        /// <summary>
        /// Update segment bytes with new data. Current page must have bytes enougth for this new size. Index will not be changed
        /// Update will try use same segment to store. If not possible, write on end of page (with possible Defrag operation)
        /// Do not add 1 extra byte in "bytesLength" for store segment length (will be added inside this method)
        /// </summary>
        protected PageSegment Update(byte index, int bytesLength)
        {
            // read position on page where index are linking to
            var slot = PAGE_SIZE - index - 1;
            var block = _buffer[slot];

            DEBUG(block < 3, "existing page segment must contains a valid block position (after header)");
            DEBUG(_buffer.ShareCounter != -1, "page must be writable to support changes");

            var originalLength = _buffer[block * PAGE_BLOCK_SIZE]; // length in blocks
            var newLength = (byte)((bytesLength / PAGE_BLOCK_SIZE) + 1); // length in blocks (+1 for store segment length)
            var isLast = index == this.HighestIndex;

            // best situation: same block count
            if (newLength == originalLength)
            {
                return new PageSegment(_buffer, index, block);
            }
            // when new length are less than original length (will fit in current segment)
            else if (newLength < originalLength)
            {
                var diff = (byte)(originalLength - newLength); // blocks removed

                // is this segment are not at end, must add this as fragment
                if (isLast == false)
                {
                    this.FragmentedBlocks += diff;
                }

                // less blocks will be used
                this.UsedContentBlocks -= diff;

                return new PageSegment(_buffer, index, newLength);
            }
            // when new length are large than current segment
            else
            {
                var diff = (byte)(newLength - originalLength); // blocks added

                // if segment finish is last used block, just use this free blocks
                if (isLast)
                {
                    this.UsedContentBlocks += diff; // diff is negative, will add value
                    this.NextFreeBlock += newLength; // need fix next free block 

                    return new PageSegment(_buffer, index, newLength);
                }
                // ok, worst case: do not fit in current segment, move new segment area
                else
                {
#if DEBUG
                    // fill segment with 99 value (for debug propose only) - there is no need on release
                    _buffer.Array.Fill((byte)99, block * PAGE_BLOCK_SIZE, originalLength * PAGE_BLOCK_SIZE);
#endif

                    // more fragmented blocks and less used blocks (because I will run insert command soon)
                    this.FragmentedBlocks += originalLength;
                    this.UsedContentBlocks -= originalLength;

                    // clean slot position (to better debug)
                    _buffer[slot] = 0;

                    // run insert command but use same index
                    return this.Insert(index, newLength);
                }
            }
        }

        /// <summary>
        /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
        /// to create a single continuous area at first block (3) - after this method there is no more fragments and 
        /// </summary>
        protected void Defrag()
        {
            DEBUG(this.FragmentedBlocks == 0, "do not call this when page has no fragmentation");
            DEBUG(_buffer.ShareCounter != -1, "page must be writable to support changes");

            // first get all segments inside this page
            var segments = new List<PageSegment>();

            for (byte i = 0; i <= this.HighestIndex; i++)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    segments.Add(new PageSegment(_buffer, i, block));
                }
            }

            // here first block position
            var next = (byte)3;

            // now, list all segment in block position order
            foreach (var segment in segments.OrderBy(x => x.Block))
            {
                // if current segment are not as excpect, copy buffer to right position (excluding empty space)
                if (segment.Block != next)
                {
                    // copy from original position into new (correct) position
                    System.Buffer.BlockCopy(_buffer.Array,
                        _buffer.Offset + (segment.Block * PAGE_BLOCK_SIZE),
                        _buffer.Array,
                        _buffer.Offset + (next * PAGE_BLOCK_SIZE),
                        segment.Length * PAGE_BLOCK_SIZE);

                    // update index slot with this new block position
                    _buffer[PAGE_SIZE - segment.Index - 1] = next;
                }

                next += segment.Length;
            }

#if DEBUG
            // fill all non-used content area with 77 (for debug propose only) - there is no need on release
            var len = PAGE_SIZE - (next * PAGE_BLOCK_SIZE) - this.HighestIndex - 1;
            _buffer.Array.Fill((byte)77, next * PAGE_BLOCK_SIZE, len);
#endif

            // clear fragment blocks (page are in a continuous segment)
            this.FragmentedBlocks = 0;
            this.NextFreeBlock = next;

            // there is no change in any index slot related
        }

        /// <summary>
        /// Get first free slot
        /// </summary>
        private byte GetFreeIndex()
        {
            for (byte index = 0; index < byte.MaxValue; index++)
            {
                var block = _buffer[PAGE_SIZE - index - 1];

                if (block == 0) return index;
            }

            throw new InvalidOperationException("This page has no more free space to insert new data");
        }

        /// <summary>
        /// Get all used indexes in this page
        /// </summary>
        protected IEnumerable<byte> GetIndexes()
        {
            for (byte i = 0; i <= this.HighestIndex; i++)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    yield return i;
                }
            }
        }

        /// <summary>
        /// Update highest used index slot
        /// </summary>
        private void UpdateHighestIndex()
        {
            for (byte i = (byte)(this.HighestIndex - 1); i <= 0; i--)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    this.HighestIndex = i;
                    break;
                }
            }

            this.HighestIndex = 0;
        }

        /// <summary>
        /// Computed checksum using CRC-8 from current page (only after CRC field)
        /// </summary>
        public byte ComputeChecksum()
        {
            return Crc8.ComputeChecksum(_buffer.Array, _buffer.Offset + P_CRC + 1, PAGE_SIZE - P_CRC - 1);
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Returns a size of specified number of pages
        /// </summary>
        public static long GetPagePosition(uint pageID)
        {
            return checked((long)pageID * PAGE_SIZE);
        }

        /// <summary>
        /// Returns a size of specified number of pages
        /// </summary>
        public static long GetPagePosition(int pageID)
        {
            DEBUG(pageID < 0, "page could not be less than 0.");

            return BasePage.GetPagePosition((uint)pageID);
        }

        #endregion

        public override string ToString()
        {
            return "PageID: " + this.PageID.ToString().PadLeft(4, '0') + " : " + this.PageType + " (" + this.ItemsCount + " Items)";
        }
    }
}