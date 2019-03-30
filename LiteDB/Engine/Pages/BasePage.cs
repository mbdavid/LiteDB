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

        /// <summary>
        /// Bytes used in each offset slot (to store segment position (2) + length (2))
        /// </summary>
        public const int SLOT_SIZE = 4;

        #region Buffer Field Positions

        public const int P_PAGE_ID = 0;  // 00-03 [uint]
        public const int P_PAGE_TYPE = 4; // 04-04 [byte]
        public const int P_PREV_PAGE_ID = 5; // 05-08 [uint]
        public const int P_NEXT_PAGE_ID = 9; // 09-12 [uint]

        public const int P_TRANSACTION_ID = 13; // 13-16 [uint]
        public const int P_IS_CONFIRMED = 17; // 17-17 [byte]
        public const int P_COL_ID = 18; // 18-21 [uint]

        public const int P_ITEMS_COUNT = 22; // 22-22 [byte]
        public const int P_USED_BYTES = 23; // 23-24 [ushort]
        public const int P_FRAGMENTED_BYTES = 25; // 25-26 [ushort]
        public const int P_NEXT_FREE_POSITION = 27; // 27-28 [ushort]
        public const int P_HIGHEST_INDEX = 29; // 29-29 [byte]

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
        /// Indicate how many items are used inside this page [1 byte]
        /// </summary>
        public byte ItemsCount { get; private set; }

        /// <summary>
        /// Get how many bytes are used on content area (exclude header and footer blocks) [2 bytes]
        /// </summary>
        public ushort UsedBytes { get; private set; }

        /// <summary>
        /// Get how many bytes are fragmented inside this page (free blocks inside used blocks) [2 bytes]
        /// </summary>
        public ushort FragmentedBytes { get; private set; }

        /// <summary>
        /// Get next free position. Starts with 32 (first byte after header) - There is no fragmentation after this [2 bytes]
        /// </summary>
        public ushort NextFreePosition { get; private set; }

        /// <summary>
        /// Get last (highest) used index slot - use byte.MaxValue for empty [1 byte]
        /// </summary>
        public byte HighestIndex { get; private set; }

        /// <summary>
        /// Get how many free bytes (including fragmented bytes) are in this page (content space)
        /// </summary>
        public int FreeBytes => PAGE_SIZE - PAGE_HEADER_SIZE - this.UsedBytes - this.FooterSize;

        /// <summary>
        /// Get how many bytes are used in footer page at this moment
        /// CRC [1 byte] + ((HighestIndex + 1) * 4 bytes per slot: [2 for position, 2 for length])
        /// </summary>
        public int FooterSize => 1 + // CRC
            (this.HighestIndex == byte.MaxValue ? 
            0 :  // no items in page
            ((this.HighestIndex + 1) * SLOT_SIZE)); // 4 bytes PER item (2 to position + 2 to length) - need consider HighestIndex used

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

            ENSURE(buffer.CheckEmpty() == false, "new page buffer must be empty before use in a new page");

            // page information
            this.PageID = pageID;
            this.PageType = pageType;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;

            // transaction information
            this.ColID = uint.MaxValue;
            this.TransactionID = uint.MaxValue;
            this.IsConfirmed = false;

            // block information
            this.ItemsCount = 0;
            this.UsedBytes = 0;
            this.FragmentedBytes = 0;
            this.NextFreePosition = PAGE_HEADER_SIZE; // 32
            this.HighestIndex = byte.MaxValue; // empty - not used yet

            // default values
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
            this.PageType = (PageType)_buffer.ReadByte(P_PAGE_TYPE);
            this.PrevPageID = _buffer.ReadUInt32(P_PREV_PAGE_ID);
            this.NextPageID = _buffer.ReadUInt32(P_NEXT_PAGE_ID);

            // transaction information
            this.TransactionID = _buffer.ReadUInt32(P_TRANSACTION_ID);
            this.IsConfirmed = _buffer.ReadBool(P_IS_CONFIRMED);
            this.ColID = _buffer.ReadUInt32(P_COL_ID);

            // blocks information
            this.ItemsCount = _buffer.ReadByte(P_ITEMS_COUNT);
            this.UsedBytes = _buffer.ReadUInt16(P_USED_BYTES);
            this.FragmentedBytes = _buffer.ReadUInt16(P_FRAGMENTED_BYTES);
            this.NextFreePosition = _buffer.ReadUInt16(P_NEXT_FREE_POSITION);
            this.HighestIndex = _buffer.ReadByte(P_HIGHEST_INDEX);

            // last CRC byte
            this.CRC = _buffer.ReadByte(P_CRC);
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
            _buffer.Write((byte)this.PageType, P_PAGE_TYPE);
            _buffer.Write(this.PrevPageID, P_PREV_PAGE_ID);
            _buffer.Write(this.NextPageID, P_NEXT_PAGE_ID);

            // transaction information
            _buffer.Write(this.TransactionID, P_TRANSACTION_ID);
            _buffer.Write(this.IsConfirmed, P_IS_CONFIRMED);
            _buffer.Write(this.ColID, P_COL_ID);

            // segments information
            _buffer.Write(this.ItemsCount, P_ITEMS_COUNT);
            _buffer.Write(this.UsedBytes, P_USED_BYTES);
            _buffer.Write(this.FragmentedBytes, P_FRAGMENTED_BYTES);
            _buffer.Write(this.NextFreePosition, P_NEXT_FREE_POSITION);
            _buffer.Write(this.HighestIndex, P_HIGHEST_INDEX);

            // compute CRC byte
            this.CRC = _buffer.ComputeChecksum();

            _buffer.Write(this.CRC, P_CRC);

            return _buffer;
        }

        #endregion

        #region Access/Manipulate PageSegments

        /// <summary>
        /// Get a page segment item based on index slot
        /// </summary>
        public BufferSlice Get(byte index)
        {
            ENSURE(index < byte.MaxValue, "slot index must be between 0-254");

            // read slot address
            var positionAddr = CalcPositionAddr(index);
            var lengthAddr = CalcLengthAddr(index);

            // read segment position/length
            var position = _buffer.ReadUInt16(positionAddr);
            var length = _buffer.ReadUInt16(lengthAddr);

            ENSURE(position > 0 && length > 0, "this page index are not in use (empty position/length)");

            // retrn buffer slice with content only data
            return _buffer.Slice(position, length);
        }

        /// <summary>
        /// Get a new page segment for this length content
        /// </summary>
        public BufferSlice Insert(ushort bytesLength, out byte index)
        {
            index = this.GetFreeIndex();

            return this.Insert(bytesLength, index);
        }

        /// <summary>
        /// Get a new page segment for this length content using fixed index
        /// </summary>
        private BufferSlice Insert(ushort bytesLength, byte index)
        {
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");
            ENSURE(this.FreeBytes >= bytesLength + SLOT_SIZE, "length must be always lower than current free space");
            ENSURE(index != byte.MaxValue, "index shloud be a valid number (0-254)");

            // if index are bigger than HighestIndex, let's update this HighestIndex with my new index
            if (index > this.HighestIndex) this.HighestIndex = index;

            // calculate how many continuous bytes are avaiable in this page (must consider new footer slot [4 bytes])
            var continuosBlocks = this.FreeBytes - this.FragmentedBytes - SLOT_SIZE;

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
            _buffer.Write(position, positionAddr);

            // write page segment length in my length address
            _buffer.Write(bytesLength, lengthAddr);

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
        /// to create a single continuous content area (just after header area). No index segment will be changed (only positions)
        /// </summary>
        public void Defrag()
        {
            ENSURE(this.FragmentedBytes > 0, "do not call this when page has no fragmentation");
            ENSURE(_buffer.ShareCounter == BUFFER_WRITABLE, "page must be writable to support changes");

            // first get all segments inside this page sorted by position (position, index)
            var segments = new SortedList<ushort, byte>();

            for (byte index = 0; index <= this.HighestIndex; index++)
            {
                var positionAddr = CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(positionAddr);

                // get only used index
                if (position != 0)
                {
                    // sort by position
                    segments.Add(position, index);
                }
            }

            // here first block position
            var next = (ushort)PAGE_HEADER_SIZE;

            // now, list all segments order by Position
            foreach (var slot in segments)
            {
                var index = slot.Value;
                var position = slot.Key;

                // get segment length
                var lengthAddr = CalcLengthAddr(index);
                var length = _buffer.ReadUInt16(lengthAddr);

                // if current segment are not as excpect, copy buffer to right position (excluding empty space)
                if (position != next)
                {
                    ENSURE(position > next, "current segment position must be greater than current empty space");

                    // copy from original position into new (correct) position
                    System.Buffer.BlockCopy(_buffer.Array,
                        _buffer.Offset + position,
                        _buffer.Array,
                        _buffer.Offset + next,
                        length);

                    // update index slot with this new block position
                    var positionAddr = CalcPositionAddr(index);

                    _buffer.Write((ushort)next, positionAddr);
                }

                next += length;
            }

            // fill all non-used content area with 0
            var emptyLength = PAGE_SIZE - next - this.FooterSize;
            _buffer.Array.Fill((byte)0, next, emptyLength);

            // clear fragment blocks (page are in a continuous segment)
            this.FragmentedBytes = 0;
            this.NextFreePosition = (ushort)next;
        }

        /// <summary>
        /// Get a free index slot in this page
        /// </summary>
        private byte GetFreeIndex()
        {
            //TODO** Aqui dá pra otimizar: manter uma MEMORIA de qual foi o ultimo espaço em branco achado
            // com isso, a proxima busca é feita a partir deste valor....

            // check for all slot area to get first empty slot
            for (byte index = 0; index < byte.MaxValue; index++)
            {
                var positionAddr = CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(positionAddr);

                // if position contains 0x00 means this slot are not used
                if (position == 0) return index;
            }

            throw new InvalidOperationException("This page has no more free space to insert new data");
        }

        /// <summary>
        /// Get all used slots indexes in this page
        /// </summary>
        public IEnumerable<byte> GetUsedIndexs()
        {
            // check for empty before loop
            if (this.ItemsCount == 0) yield break;

            ENSURE(this.HighestIndex != byte.MaxValue, "if has items count Heighest index should be not emtpy");

            for (byte index = 0; index <= this.HighestIndex; index++)
            {
                var positionAddr = CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(positionAddr);

                if (position != 0)
                {
                    yield return index;
                }
            }
        }

        /// <summary>
        /// Update highest used index slot
        /// </summary>
        private void UpdateHighestIndex()
        {
            for (byte index = (byte)(this.HighestIndex - 1); index > 0; index--)
            {
                var positionAddr = CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(positionAddr);

                if (position != 0)
                {
                    this.HighestIndex = index;
                    return;
                }
            }

            // empty value
            this.HighestIndex = byte.MaxValue;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Get buffer offset position where one page segment length are located (based on index slot)
        /// </summary>
        public static int CalcPositionAddr(byte index) => P_CRC - ((index + 1) * SLOT_SIZE) + 2;

        /// <summary>
        /// Get buffer offset position where one page segment length are located (based on index slot)
        /// </summary>
        public static int CalcLengthAddr(byte index) => P_CRC - ((index + 1) * 4);

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
        /// FreeBytes ranges on page slot for free list page
        /// 90% - 100% = 0
        /// 75% -  90% = 1
        /// 60% -  75% = 2
        /// 30% -  60% = 3
        ///  0% -  30% = 4
        /// </summary>
        private static int[] _freePageSlots = new[] 
        {
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .90), // 0
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .75), // 1
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .60), // 2
            (int)((PAGE_SIZE - PAGE_HEADER_SIZE) * .30)  // 3
        };

        /// <summary>
        /// Get page index slot on FreeDataPageID/FreeIndexPageID 
        /// </summary>
        public static int FreeIndexSlot(int freeBytes)
        {
            for(var i = 0; i < _freePageSlots.Length; i++)
            {
                if (freeBytes >= _freePageSlots[i]) return i;
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