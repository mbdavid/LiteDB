using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public const int P_PAGE_ID = 0;  // 00-03
        public const int P_PAGE_TYPE = 4; // 04
        public const int P_PREV_PAGE_ID = 5; // 05-08
        public const int P_NEXT_PAGE_ID = 9; // 09-12
        public const int P_CRC = 13;

        public const int P_TRANSACTION_ID = 14; // 14-21
        public const int P_IS_CONFIRMED = 22;
        public const int P_COL_ID = 23; // 23-26

        public const int P_ITEMS_COUNT = 27;
        public const int P_USED_CONTENT_BLOCKS = 28;
        public const int P_FRAGMENTED_BLOCKS = 29;
        public const int P_NEXT_FREE_BLOCK = 30;
        public const int P_HIGHEST_INDEX = 31;

        #endregion

        /// <summary>
        /// Represent page number - start in 0 with HeaderPage [4 bytes]
        /// </summary>
        public uint PageID { get; }

        /// <summary>
        /// Indicate the page type [1 byte]
        /// </summary>
        public PageType PageType { get; set; }

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
        /// Get next free block. Starts with block 1 (first after header) - It always at end of last block - there is no fragmentation after this [1 byte]
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
        /// Get calculated how many blocks footer (index space) are used. Always add 1 to ensure new item will not need more footer
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
            this.NextFreeBlock = PAGE_HEADER_SIZE / PAGE_BLOCK_SIZE; // first block index should be #1 (block #0 is reserved to header)
            this.HighestIndex = 0;

            // default data
            this.ColID = uint.MaxValue;
            this.TransactionID = long.MaxValue;
            this.IsConfirmed = false;

            this.IsDirty = false;

            // writing direct into buffer in Ctor() because there is no change later (write once)
            _buffer.Write(this.PageID, P_PAGE_ID);
        }

        /// <summary>
        /// Read header data from byte[] buffer into local variables
        /// using fixed position be be faster than use BufferReader
        /// </summary>
        public BasePage(PageBuffer buffer)
        {
            _buffer = buffer;

            // page information
            this.PageID = _buffer.ReadUInt32(P_PAGE_ID);
            this.PageType = (PageType)_buffer[P_PAGE_TYPE];
            this.PrevPageID = _buffer.ReadUInt32(P_PREV_PAGE_ID);
            this.NextPageID = _buffer.ReadUInt32(P_NEXT_PAGE_ID);
            this.CRC = _buffer[P_CRC];

            // transaction information
            this.ColID = _buffer.ReadUInt32(P_COL_ID);
            this.TransactionID = _buffer.ReadInt64(P_TRANSACTION_ID);
            this.IsConfirmed = _buffer[P_IS_CONFIRMED] != 0;

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
        public virtual PageBuffer GetBuffer(bool update)
        {
            // using fixed position to be faster than BufferWriter
            ENSURE(this.PageID == _buffer.ReadUInt32(P_PAGE_ID), "pageID can't be changed");

            if (update == false) return _buffer;

            // page information
            // PageID   - never change!
            _buffer[P_PAGE_TYPE] = (byte)this.PageType;
            _buffer.Write(this.PrevPageID, P_PREV_PAGE_ID);
            _buffer.Write(this.NextPageID, P_NEXT_PAGE_ID);

            // block information
            _buffer[P_ITEMS_COUNT] = this.ItemsCount;
            _buffer[P_USED_CONTENT_BLOCKS] = this.UsedContentBlocks;
            _buffer[P_FRAGMENTED_BLOCKS] = this.FragmentedBlocks;
            _buffer[P_NEXT_FREE_BLOCK] = this.NextFreeBlock;
            _buffer[P_HIGHEST_INDEX] = this.HighestIndex;

            // transaction information
            _buffer.Write(this.ColID, P_COL_ID);
            _buffer.Write(this.TransactionID, P_TRANSACTION_ID);
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
        protected PageSegment Get(byte index)
        {
            var slot = PAGE_SIZE - index - 1;
            var block = _buffer[slot];

            return new PageSegment(_buffer, index, block);
        }

        /// <summary>
        /// Create a new page item and return PageItem as reference to be buffer fill outside.
        /// Do not add 1 extra byte in "bytesLength" for store segment length (will be added inside this method)
        /// </summary>
        protected PageSegment Insert(int bytesLength)
        {
            var length = PageSegment.GetLength(bytesLength); // length in blocks (add inside method +1 byte)
            var index = this.GetFreeIndex();

            return this.Insert(index, length);
        }

        /// <summary>
        /// Internal implementation with index as parameter (used also in Update)
        /// </summary>
        private PageSegment Insert(byte index, byte length)
        {
            ENSURE(this.FreeBlocks >= length, "length must be always lower than current free space");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");
            ENSURE((int)this.NextFreeBlock + (int)this.FooterBlocks < 256, "next free block + footer can't be larger than 1 page");

            // if index are bigger than HighestIndex, let's update this HighestIndex with my new index
            if (index > this.HighestIndex) this.HighestIndex = index;

            // calculate how many continuous blocks are avaiable in this page
            var continuosBlocks = this.FreeBlocks - this.FragmentedBlocks;

            ENSURE(continuosBlocks == 256 - this.NextFreeBlock - this.FooterBlocks, "invalid next free block");

            // if continuous blocks are not big enouth for this data, must run page defrag
            if (length > continuosBlocks)
            {
                this.Defrag();

                ENSURE(this.FreeBlocks - this.FragmentedBlocks >= length, "after defrag length must fit in page");
            }

            // get a free index slot
            var block = this.NextFreeBlock;
            var slot = PAGE_SIZE - index - 1;

            ENSURE(_buffer[slot] == 0, "slot must be empty before use");

            // update index slot with this block position
            _buffer[slot] = block;

            ENSURE(this.NextFreeBlock > 0, "next block should respect header > 0");

            // and update for next insert
            this.NextFreeBlock += length;

            // update counters
            this.ItemsCount++;
            this.UsedContentBlocks += length;

            this.IsDirty = true;

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

            ENSURE(block > 1, "existing page segment must contains a valid block position (after header)");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            var position = block * PAGE_BLOCK_SIZE;

            // read how many blocks this segment use
            var length = _buffer[position];

            // clear slot index
            _buffer[slot] = (byte)0;

            // add as free blocks
            this.ItemsCount--;
            this.UsedContentBlocks -= length;

            if (this.HighestIndex != index)
            {
                // if segment is in middle of the page, add this blocks as fragment block
                this.FragmentedBlocks += length;
            }
            else
            {
                // if is last segment, must update next free block and discover new highst new index
                this.NextFreeBlock = block;
                this.UpdateHighestIndex();
            }

            // set page as dirty
            this.IsDirty = true;

            // get deleted page segment
            return new PageSegment(_buffer, index, block, length);
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

            ENSURE(block > 1, "existing page segment must contains a valid block position (after header)");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            var originalLength = _buffer[block * PAGE_BLOCK_SIZE]; // length in blocks
            var newLength = PageSegment.GetLength(bytesLength); // length in blocks (add inside method +1 byte)
            var isLastSegment = index == this.HighestIndex;

            this.IsDirty = true;

            // best situation: same block count
            if (newLength == originalLength)
            {
                return new PageSegment(_buffer, index, block, newLength);
            }
            // when new length are less than original length (will fit in current segment)
            else if (newLength < originalLength)
            {
                var diff = (byte)(originalLength - newLength); // blocks removed

                if (isLastSegment)
                {
                    // if is at end of segment, must get back unused blocks 
                    this.NextFreeBlock -= diff;
                }
                else
                {
                    // is this segment are not at end, must add this as fragment
                    this.FragmentedBlocks += diff;
                }

                // less blocks will be used
                this.UsedContentBlocks -= diff;

                return new PageSegment(_buffer, index, block, newLength);
            }
            // when new length are large than current segment
            else
            {
#if DEBUG
                // fill segment with 99 value (for debug propose only) - there is no need on release
                var diff = (byte)(newLength - originalLength); // blocks added
                _buffer.Array.Fill(99, block * PAGE_BLOCK_SIZE, originalLength * PAGE_BLOCK_SIZE);
#endif

                // release used content blocks with original length and decrease counter
                this.UsedContentBlocks -= originalLength;
                this.ItemsCount--;

                if (isLastSegment)
                {
                    // if segment is end of page, must update next free block as current block
                    this.NextFreeBlock = block;
                }
                else
                {
                    // if segment is on middle of page, add content as fragment blocks
                    this.FragmentedBlocks += originalLength;
                }

                // clean slot position (to better debug - will be checked in Insert)
                _buffer[slot] = 0;

                // run insert command but use same index
                return this.Insert(index, newLength);
            }
        }

        /// <summary>
        /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page blocks
        /// to create a single continuous area at first block (3) - after this method there is no more fragments and 
        /// </summary>
        protected void Defrag()
        {
            ENSURE(this.FragmentedBlocks > 0, "do not call this when page has no fragmentation");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            // first get all segments inside this page
            var segments = new List<PageSegment>();

            for (var i = 0; i <= this.HighestIndex; i++)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    segments.Add(new PageSegment(_buffer, (byte)i, block));
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
            var maxIndex = PAGE_AVAILABLE_BLOCKS; // 255

            for (var index = 0; index <= maxIndex; index++) // 256 - 1 - 1
            {
                var slot = PAGE_SIZE - index - 1;
                var block = _buffer[slot];

                if (block == 0) return (byte)index;
            }

            throw new InvalidOperationException("This page has no more free space to insert new data");
        }

        /// <summary>
        /// Get all used indexes in this page
        /// </summary>
        protected IEnumerable<byte> GetIndexes()
        {
            for (var i = 0; i <= this.HighestIndex; i++)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    yield return (byte)i;
                }
            }
        }

        /// <summary>
        /// Update highest used index slot
        /// </summary>
        private void UpdateHighestIndex()
        {
            for (var i = this.HighestIndex - 1; i <= 0; i--)
            {
                var block = _buffer[PAGE_SIZE - i - 1];

                if (block != 0)
                {
                    this.HighestIndex = (byte)i;
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
            ENSURE(pageID >= 0, "page could not be less than 0.");

            return BasePage.GetPagePosition((uint)pageID);
        }

        /// <summary>
        /// Create new page instance based on buffer (READ)
        /// </summary>
        public static T ReadPage<T>(PageBuffer buffer)
            where T : BasePage
        {
            if (typeof(T) == typeof(BasePage)) return (T)(object)new BasePage(buffer);
            if (typeof(T) == typeof(HeaderPage)) return (T)(object)new HeaderPage(buffer);
            if (typeof(T) == typeof(CollectionPage)) return (T)(object)new CollectionPage(buffer);
            if (typeof(T) == typeof(IndexPage)) return (T)(object)new IndexPage(buffer);
            if (typeof(T) == typeof(DataPage)) return (T)(object)new DataPage(buffer);

            throw new InvalidCastException();
        }

        /// <summary>
        /// Create new page instance with new PageID and passed buffer (NEW)
        /// </summary>
        public static T CreatePage<T>(PageBuffer buffer, uint pageID)
            where T : BasePage
        {
            if (typeof(T) == typeof(HeaderPage)) return (T)(object)new HeaderPage(buffer, pageID);
            if (typeof(T) == typeof(CollectionPage)) return (T)(object)new CollectionPage(buffer, pageID);
            if (typeof(T) == typeof(IndexPage)) return (T)(object)new IndexPage(buffer, pageID);
            if (typeof(T) == typeof(DataPage)) return (T)(object)new DataPage(buffer, pageID);

            throw new InvalidCastException();
        }

        /// <summary>
        /// Get index slot on FreeDataPageID/FreeIndexPageID - Get based on "FreeBlocks"
        /// 228 - 254 blocks => slot 0 (great than 90% free)
        /// 190 - 227 blocks => slot 1 (between 75% and 90% free)
        /// 127 - 189 blocks => slot 2 (between 50% and 75% free)
        ///  76 - 126 blocks => slot 3 (between 30% and 50% free)
        ///   0 -  75 blocks => slot 4 (less than 30% free)
        /// </summary>
        public static byte FreeIndexSlot(byte freeBlocks)
        {
            if (freeBlocks >= 228) return 0;
            if (freeBlocks >= 190) return 1;
            if (freeBlocks >= 127) return 2;
            if (freeBlocks >= 76) return 3;
            return 4;
        }

        /// <summary>
        /// Get minimum slot with space enough for your data content
        /// if your need 228 - 254 blocks => no slot => there is no garanteed slot (-1)
        /// if your need 190 - 227 blocks => slot 0
        /// if your need 127 - 189 blocks => slots 1, 0
        /// if your need  76 - 126 blocks => slots 2, 1, 0
        /// if your need   0 -  75 blocks => slots 3, 2, 1, 0
        /// </summary>
        public static int GetMinimumIndexSlot(byte length)
        {
            return FreeIndexSlot(length) - 1;
        }

        #endregion

        public override string ToString()
        {
            return $"PageID: {this.PageID.ToString().PadLeft(4, '0')} : {this.PageType} ({this.ItemsCount} Items)";
        }
    }
}