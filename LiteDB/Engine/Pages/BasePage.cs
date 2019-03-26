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

        public const int P_PAGE_ID = 0;  // 00-03 [uint]
        public const int P_PAGE_TYPE = 4; // 04-04 [byte]
        public const int P_PREV_PAGE_ID = 5; // 05-08 [uint]
        public const int P_NEXT_PAGE_ID = 9; // 09-12 [uint]

        public const int P_TRANSACTION_ID = 13; // 13-16 [uint]
        public const int P_IS_CONFIRMED = 17; // 17-17 [byte]
        public const int P_COL_ID = 18; // 18-21 [uint]

        public const int P_ITEMS_COUNT = 22; // 22-23 [ushort]
        public const int P_USED_BYTES = 24; // 24-25 [ushort]
        public const int P_FRAGMENTED_BYTES = 26; // 26-27 [ushort]
        public const int P_NEXT_FREE_POSITION = 28; // 28-29 [ushort]
        public const int P_HIGHEST_INDEX = 30; // 30-30 [byte]

        public const int P_CRC = PAGE_SIZE - 1; // 8191-8191 [byte]

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
        /// Indicate how many items are used inside this page [2 byte]
        /// </summary>
        public int ItemsCount { get; private set; }

        /// <summary>
        /// Get how many bytes are used on content area (exclude header and footer blocks) [2 bytes]
        /// </summary>
        public int UsedBytes { get; private set; }

        /// <summary>
        /// Get how many bytes are fragmented inside this page (free blocks inside used blocks) [2 bytes]
        /// </summary>
        public int FragmentedBytes { get; private set; }

        /// <summary>
        /// Get next free position. Starts with 32 (first byte after header) - There is no fragmentation after this [2 bytes]
        /// </summary>
        public int NextFreePosition { get; private set; }

        /// <summary>
        /// Get last (highest) used index slot [1 byte]
        /// </summary>
        public byte HighestIndex { get; private set; }

        /// <summary>
        /// Get how many free bytes (including fragmented bytes) in this page
        /// </summary>
        public int FreeBytes => PAGE_SIZE - PAGE_HEADER_SIZE - this.UsedBytes - this.FooterSize;

        /// <summary>
        /// Get how many bytes are used in footer page
        /// CRC [1 byte] + (HighestIndex * 4 bytes per slot: [2 for position, 2 for length])
        /// </summary>
        public int FooterSize => 1 + (this.HighestIndex * PageSlot.SIZE);

        /// <summary>
        /// Set in all datafile pages the page id about data/index collection. Useful if want re-build database without any index [4 bytes]
        /// </summary>
        public uint ColID { get; set; }

        /// <summary>
        /// Represent transaction ID that was stored [4 bytes]
        /// </summary>
        public uint TransactionID { get; set; }

        /// <summary>
        /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte]
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Page CRC8 - page CRC are calculated from byte 0 to 8190
        /// </summary>
        public byte CRC { get; private set; }

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

            // block information
            this.ItemsCount = 0;
            this.UsedBytes = 0;
            this.FragmentedBytes = 0;
            this.NextFreePosition = PAGE_HEADER_SIZE; // 32
            this.HighestIndex = 0;

            // default data
            this.ColID = uint.MaxValue;
            this.TransactionID = uint.MaxValue;
            this.IsConfirmed = false;
            this.CRC = 0;

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

            // transaction information
            this.TransactionID = _buffer.ReadUInt32(P_TRANSACTION_ID);
            this.IsConfirmed = _buffer[P_IS_CONFIRMED] != 0;
            this.ColID = _buffer.ReadUInt32(P_COL_ID);

            // blocks information
            this.ItemsCount = _buffer.ReadUInt16(P_ITEMS_COUNT);
            this.UsedBytes = _buffer.ReadUInt16(P_USED_BYTES);
            this.FragmentedBytes = _buffer.ReadUInt16(P_FRAGMENTED_BYTES);
            this.NextFreePosition = _buffer.ReadUInt16(P_NEXT_FREE_POSITION);
            this.HighestIndex = _buffer[P_HIGHEST_INDEX];

            // last CRC byte
            this.CRC = _buffer[P_CRC];
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

            // transaction information
            _buffer.Write(this.TransactionID, P_TRANSACTION_ID);
            _buffer[P_IS_CONFIRMED] = this.IsConfirmed ? (byte)1 : (byte)0;
            _buffer.Write(this.ColID, P_COL_ID);

            // segments information
            _buffer.Write((ushort)this.ItemsCount, P_ITEMS_COUNT);
            _buffer.Write((ushort)this.UsedBytes, P_USED_BYTES);
            _buffer.Write((ushort)this.FragmentedBytes, P_FRAGMENTED_BYTES);
            _buffer.Write((ushort)this.NextFreePosition, P_NEXT_FREE_POSITION);
            _buffer[P_HIGHEST_INDEX] = this.HighestIndex;

            return _buffer;
        }

        #endregion

        #region Access/Manipulate PageSegments

        /// <summary>
        /// Get a page segment item based on index slot
        /// </summary>
        public BufferSlice Get(byte index)
        {
            // read slot address
            var positionAddr = CalcPositionAddr(index);
            var lengthAddr = CalcLengthAddr(index);

            // read segment position/length
            var position = _buffer.ReadUInt16(positionAddr);
            var length = _buffer.ReadUInt16(lengthAddr);

            // retrn buffer slice with content only data
            return _buffer.Slice(position, length);
        }

        /// <summary>
        /// Get a new page segment for this length content
        /// </summary>
        public BufferSlice Insert(byte index, int bytesLength)
        {
            ENSURE(this.ItemsCount <= 254, "there is no more space (in items count) in this page");
            ENSURE(this.FreeBytes >= bytesLength + 4, "length must be always lower than current free space");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            // if index are bigger than HighestIndex, let's update this HighestIndex with my new index
            if (index > this.HighestIndex) this.HighestIndex = index;

            // calculate how many continuous bytes are avaiable in this page (must consider new footer slot [4 bytes])
            var continuosBlocks = this.FreeBytes - this.FragmentedBytes - PageSlot.SIZE;

            // if continuous blocks are not big enouth for this data, must run page defrag
            if (bytesLength > continuosBlocks)
            {
                this.Defrag();
            }

            // get segment addresses
            var positionAddr = CalcPositionAddr(index);
            var lengthAddr = CalcLengthAddr(index);

            ENSURE(_buffer.ReadUInt16(positionAddr) == 0, "slot position must be empty before use");
            ENSURE(_buffer.ReadUInt16(lengthAddr) == 0, "slot length must be empty before use");

            // get next free position in page
            var position = this.NextFreePosition;

            // write this page position in my position address
            _buffer.Write((ushort)position, positionAddr);

            // write page segment length in my length address
            _buffer.Write((ushort)bytesLength, lengthAddr);

            // update next free position and counters
            this.NextFreePosition += bytesLength;
            this.ItemsCount++;
            this.UsedBytes += bytesLength;

            this.IsDirty = true;

            // create page segment based new inserted segment
            return _buffer.Slice(position, bytesLength);
        }

        /// <summary>
        /// Remove index slot about this page segment
        /// </summary>
        public void Delete(byte index)
        {
            // read block position on index slot
            var positionAddr = CalcPositionAddr(index);
            var lengthAddr = CalcLengthAddr(index);

            var position = _buffer.ReadUInt16(positionAddr);
            var length = _buffer.ReadUInt16(lengthAddr);

            ENSURE(position >= PAGE_HEADER_SIZE, "deleted position must be after page header");
            ENSURE(length >= 0, "deleted item must has length > 0");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            // clear both position/length
            _buffer.Write((ushort)0, positionAddr);
            _buffer.Write((ushort)0, lengthAddr);

            // add as free blocks
            this.ItemsCount--;
            this.UsedBytes -= length;

            if (this.HighestIndex != index)
            {
                // if segment is in middle of the page, add this blocks as fragment block
                this.FragmentedBytes += length;
            }
            else
            {
                // if is last segment, must update next free block and discover new highst new index
                this.NextFreePosition = position;
                this.UpdateHighestIndex();
            }

            // set page as dirty
            this.IsDirty = true;
        }

        /// <summary>
        /// Update segment bytes with new data. Current page must have bytes enougth for this new size. Index will not be changed
        /// Update will try use same segment to store. If not possible, write on end of page (with possible Defrag operation)
        /// </summary>
        public BufferSlice Update(byte index, int bytesLength)
        {
            throw new NotImplementedException();
            /*
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
            */
        }

        /// <summary>
        /// Defrag method re-organize all byte data content removing all fragmented data. This will move all page segments
        /// to create a single continuous content area (just after header area)
        /// </summary>
        public void Defrag()
        {
            ENSURE(this.FragmentedBytes > 0, "do not call this when page has no fragmentation");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            // first get all segments inside this page
            var slots = new List<PageSlot>();

            for (var index = 0; index <= this.HighestIndex; index++)
            {
                var positionAddr = CalcPositionAddr(index);
                var lengthAddr = CalcLengthAddr(index);

                var position = _buffer.ReadUInt16(positionAddr);

                if (position != 0)
                {
                    var length = _buffer.ReadUInt16(lengthAddr);
                    var slot = new PageSlot(index, position, length);

                    slots.Add(slot);
                }
            }

            // here first block position
            var next = PAGE_HEADER_SIZE;

            // now, list all segment in block position order
            foreach (var slot in slots.OrderBy(x => x.Position))
            {
                // if current segment are not as excpect, copy buffer to right position (excluding empty space)
                if (slot.Position != next)
                {
                    // copy from original position into new (correct) position
                    System.Buffer.BlockCopy(_buffer.Array,
                        _buffer.Offset + slot.Position,
                        _buffer.Array,
                        _buffer.Offset + next,
                        slot.Length);

                    // update index slot with this new block position
                    var positionAddr = CalcPositionAddr(slot.Index);

                    _buffer.Write((ushort)next, positionAddr);
                }

                next += slot.Length;
            }

#if DEBUG
            // fill all non-used content area with 77 (for debug propose only) - there is no need on release
            var len = PAGE_SIZE - next - this.FooterSize;
            _buffer.Array.Fill((byte)77, next, len);
#endif

            // clear fragment blocks (page are in a continuous segment)
            this.FragmentedBytes = 0;
            this.NextFreePosition = next;

            // there is no change in any index slot related
        }

        /// <summary>
        /// Get a free slot in this page
        /// </summary>
        public byte GetFreeIndex()
        {
            // if no fragment data, there is no fragment index too (use HighestIndex)
            if (this.FragmentedBytes == 0)
            {
                return this.ItemsCount == 0 ? (byte)0 : (byte)(this.HighestIndex + 1);
            }

            for (var index = 0; index <= 255; index++)
            {
                var slot = CalcPositionAddr((byte)index);
                var position = _buffer.ReadUInt16(slot);

                if (position == 0) return (byte)index;
            }

            throw new InvalidOperationException("This page has no more free space to insert new data");
        }

        /// <summary>
        /// Get all used indexes in this page
        /// </summary>
        public IEnumerable<byte> GetIndexes()
        {
            for (var index = 0; index <= this.HighestIndex; index++)
            {
                var slot = CalcPositionAddr((byte)index);
                var position = _buffer.ReadUInt16(slot);

                if (position != 0)
                {
                    yield return (byte)index;
                }
            }
        }

        /// <summary>
        /// Update highest used index slot
        /// </summary>
        private void UpdateHighestIndex()
        {
            for (var index = this.HighestIndex - 1; index > 0; index--)
            {
                var positionAddr = CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(positionAddr);

                if (position != 0)
                {
                    this.HighestIndex = (byte)index;
                    return;
                }
            }

            this.HighestIndex = 0;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Get buffer offset position where one page segment length are located (based on index slot)
        /// </summary>
        public static int CalcPositionAddr(int index) => P_CRC - ((index + 1) * PageSlot.SIZE) + 2;

        /// <summary>
        /// Get buffer offset position where one page segment length are located (based on index slot)
        /// </summary>
        public static int CalcLengthAddr(int index) => P_CRC - ((index + 1) * 4);

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
        /// FreeBytes ranges on slot for free list page
        /// 90% - 100% = 0
        /// 75% -  90% = 1
        /// 60% -  75% = 2
        /// 30% -  60% = 3
        ///  0% -  30% = 4
        /// </summary>
        private static int[] _freeSlots = new[] 
        {
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .90), // 0
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .75), // 1
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .60), // 2
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .30)  // 3
        };

        /// <summary>
        /// Get index slot on FreeDataPageID/FreeIndexPageID 
        /// </summary>
        public static int FreeIndexSlot(int freeBytes)
        {
            for(var i = 0; i < _freeSlots.Length; i++)
            {
                if (freeBytes >= _freeSlots[i]) return i;
            }

            return CollectionPage.PAGE_FREE_LIST_SLOTS - 1; // 4
        }

        /// <summary>
        /// Get minimum slot with space enough for your data content
        /// Returns -1 if no space guaranteed (more than 90%)
        /// </summary>
        public static int GetMinimumIndexSlot(int length)
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