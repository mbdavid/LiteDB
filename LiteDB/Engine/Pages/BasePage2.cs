using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public enum PageType { Empty = 0, Header = 1, Collection = 2, Index = 3, Data = 4, Extend = 5 }

    internal class BasePage2
    {
        private readonly PageBuffer _buffer;

        /// <summary>
        /// Represent page number - start in 0 with HeaderPage [4 bytes]
        /// </summary>
        public uint PageID { get; set; }

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
        public byte ItemsCount { get; set; }

        /// <summary>
        /// Get how many blocks are used on content area (no header and no footer) [1 byte]
        /// </summary>
        public byte UsedBlocks { get; set; }

        /// <summary>
        /// Get how many blocks are fragmented (free blocks inside used blocks) [1 byte]
        /// </summary>
        public byte FragmentedBlocks { get; set; }

        /// <summary>
        /// Get next free block. Starts with block 3 (first after header) - It always at end of last block - there is no fragmentation after this [1 byte]
        /// </summary>
        public byte NextFreeBlock { get; set; }

        /// <summary>
        /// Get last (highest) used index slot [1 byte]
        /// </summary>
        public byte HighestIndex { get; set; }

        /// <summary>
        /// Get how many bytes are available in this page (content area)
        /// </summary>
        public int FreeBytes => PAGE_AVAILABLE_BYTES - ((this.UsedBlocks + this.FooterBlocks) * PAGE_BLOCK_SIZE);

        /// <summary>
        /// Get calculated how many blocks footer (index space) are used
        /// </summary>
        public byte FooterBlocks => (byte)((this.HighestIndex / PAGE_BLOCK_SIZE) + 1);

        /// <summary>
        /// Set in all datafile pages the page id about data/index collection. Useful if want re-build database without any index [4 bytes]
        /// </summary>
        public uint ColID { get; set; }

        /// <summary>
        /// Represent transaction page ID that was stored [12 bytes]
        /// </summary>
        public ObjectId TransactionID { get; set; }

        /// <summary>
        /// Used in WAL, define this page is last transaction page and are confirmed on disk [1 byte]
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Set this pages that was changed and must be persist in disk [not peristable]
        /// </summary>
        public bool IsDirty { get; set; }

        public BasePage2(PageBuffer buffer)
        {
            _buffer = buffer;
        }

        #region New/Read/Write

        /// <summary>
        /// Set this page instance with new (default) values
        /// </summary>
        public virtual void NewPage(uint pageID, PageType pageType)
        {
            // page information
            this.PageID = pageID;
            this.PageType = pageType;
            this.PrevPageID = uint.MaxValue;
            this.NextPageID = uint.MaxValue;

            // block information
            this.ItemsCount = 0;
            this.UsedBlocks = 0;
            this.FragmentedBlocks = 0;
            this.NextFreeBlock = 3; // first block index should be 3 (0, 1, 2 are header position)
            this.HighestIndex = 0;

            // default data
            this.ColID = uint.MaxValue;
            this.TransactionID = ObjectId.Empty;
            this.IsConfirmed = false;
            this.IsDirty = false;
        }

        public virtual void ReadHeaderBuffer()
        {
            // page information
            this.PageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 0); // 00-03
            this.PageType = (PageType)_buffer[4]; // 04-04
            this.PrevPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 5); // 05-08
            this.NextPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 9); // 09-12

            // blocks information
            this.ItemsCount = _buffer[13]; // 13-13
            this.UsedBlocks = _buffer[14]; // 14-14
            this.FragmentedBlocks = _buffer[15]; // 15-15
            this.NextFreeBlock = _buffer[16]; // 16-16
            this.HighestIndex = _buffer[17]; // 17-17

            // transaction information
            this.ColID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 18); // 18-21
            this.TransactionID = new ObjectId(_buffer.Array, _buffer.Offset + 22); // 22-34 
            this.IsConfirmed = BitConverter.ToBoolean(_buffer.Array, _buffer.Offset + 35); // 35
        }

        public virtual void UpdateHeaderBuffer()
        {
            // page information
            this.PageID.ToBytes(_buffer.Array, _buffer.Offset + 0); // 00-03
            _buffer[4] = (byte)this.PageType; // 04-04
            this.PrevPageID.ToBytes(_buffer.Array, _buffer.Offset + 5); // 05-08
            this.NextPageID.ToBytes(_buffer.Array, _buffer.Offset + 9); // 09-12

            // block information
            _buffer[13] = this.ItemsCount; // 13-13
            _buffer[14] = this.UsedBlocks; // 14-14
            _buffer[15] = this.FragmentedBlocks; // 15-15
            _buffer[16] = this.NextFreeBlock; // 16-16
            _buffer[17] = this.HighestIndex; // 17-17

            // transaction information
            this.ColID.ToBytes(_buffer.Array, _buffer.Offset + 18); // 18-22
            this.TransactionID.ToByteArray(_buffer.Array, _buffer.Offset + 22);
            _buffer[35] = this.IsConfirmed ? (byte)1 : (byte)0;
        }

        #endregion

        /// <summary>
        /// Get a page item based on index slot
        /// </summary>
        public PageSegment Get(byte index)
        {
            var block = _buffer[PAGE_SIZE - index - 1];

            return new PageSegment(_buffer, index, block);
        }

        /// <summary>
        /// Create a new page item and return PageItem as reference to be buffer fill outside
        /// </summary>
        public PageSegment Insert(int bytesLength)
        {
            DEBUG(this.FreeBytes < bytesLength, "length must be always lower than current free space");

            // calculate how many continuous bytes are avaiable in this page
            var continuosBytes = this.FreeBytes - (this.FragmentedBlocks * PAGE_BLOCK_SIZE);

            // if continuous bytes are not big enouth for this data, must run page defrag
            if (bytesLength > continuosBytes)
            {
                this.Defrag();
            }

            // get a free index slot
            var index = this.GetFreeSlot();
            var length = (byte)((bytesLength / PAGE_BLOCK_SIZE) + 1); // length in blocks
            var block = this.NextFreeBlock;

            // update index slot with this block position
            _buffer[PAGE_SIZE - index - 1] = block;

            // and update for next insert
            //TODO: should test page full
            this.NextFreeBlock += length;

            // update counters
            this.ItemsCount++;
            this.UsedBlocks += length;

            // update highest slot if this slot are new highest
            if (index > this.HighestIndex) this.HighestIndex = index;

            // create page item (will set length in first byte block)
            return new PageSegment(_buffer, index, block, length);
        }

        /// <summary>
        /// Remove index slot about this page item (will not clean page item data space)
        /// </summary>
        public void Delete(byte index)
        {
            // read position on page that this index are linking
            var slot = _buffer[PAGE_SIZE - index - 1];

            DEBUG(slot < 3, "existing page item must contains a valid block position (after header)");

            var block = slot * PAGE_BLOCK_SIZE;

            // read how many blocks this block use
            var length = _buffer[block];

            // clean slot index
            _buffer[PAGE_SIZE - index - 1] = (byte)0x00;

            // add as free blocks
            this.ItemsCount--;
            this.UsedBlocks -= length;

            if (this.HighestIndex != index)
            {
                // if slot is in middle, add this blocks as fragment block
                this.FragmentedBlocks += length;
            }
            else
            {
                this.UpdateHighestSlot();
            }

            // set page as dirty
            this.IsDirty = true;
        }

        /// <summary>
        /// Defrag method re-organize all byte data removing all fragmented data. This will move all page blocks
        /// to create a single continuous area
        /// </summary>
        public void Defrag()
        {
            // first get all segments inside this page
            var segments = new List<PageSegment>();

            for (byte i = 0; i <= this.HighestIndex; i++)
            {
                var slot = _buffer[PAGE_SIZE - i - 1];

                if (slot != 0)
                {
                    segments.Add(new PageSegment(_buffer, i, slot));
                }

            }

            // here first block position
            var next = (byte)3;

            // now, list all segment in block position
            foreach(var segment in segments.OrderBy(x => x.Block))
            {
                // if current segment are not as excpect, copy buffer to right position (excluding empty space)
                if (segment.Block != next)
                {
                    // copy from original position into new (correct) position
                    Buffer.BlockCopy(_buffer.Array,
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
            // clear free segment (for debug propose only)
            Array.Clear(_buffer.Array, 
                next * PAGE_BLOCK_SIZE, 
                PAGE_SIZE - this.HighestIndex - (next * PAGE_BLOCK_SIZE) - 1);
#endif

            // clear fragment blocks (page are in a continuous segment)
            this.FragmentedBlocks = 0;
            this.NextFreeBlock = next;

            // there is no change in any index slot related
        }

        /// <summary>
        /// Get first free slot
        /// </summary>
        private byte GetFreeSlot()
        {
            for(byte index = 0; index < byte.MaxValue; index++)
            {
                var slot = _buffer[PAGE_SIZE - index - 1];

                if (slot == 0) return index;
            }

            throw new InvalidOperationException("This page has no more free space to insert new data");
        }

        /// <summary>
        /// Update highest used slot
        /// </summary>
        private void UpdateHighestSlot()
        {
            for(byte i = (byte)(this.HighestIndex - 1); i <= 0; i--)
            {
                var slot = _buffer[PAGE_SIZE - i - 1];

                if (slot != 0)
                {
                    this.HighestIndex = i;
                    break;
                }
            }

            this.HighestIndex  = 0;
        }

        public override string ToString()
        {
            return this.PageID.ToString().PadLeft(4, '0') + " : " + this.PageType + " (" + this.ItemsCount + ")";
        }
    }
}