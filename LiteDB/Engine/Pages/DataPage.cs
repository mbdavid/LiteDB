using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// The DataPage thats stores object data.
    /// </summary>
    internal class DataPage : BasePage
    {
        /// <summary>
        /// Read existing DataPage in buffer
        /// </summary>
        public DataPage(PageBuffer buffer)
            : base(buffer)
        {
            if (this.PageType != PageType.Data) throw new LiteException(0, $"Invalid DataPage buffer on {PageID}");
        }

        /// <summary>
        /// Create new DataPage
        /// </summary>
        public DataPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Data)
        {
        }

        /// <summary>
        /// Get single DataBlock
        /// </summary>
        public DataBlock GetBlock(byte index)
        {
            var segment = base.Get(index);

            return new DataBlock(this, index, segment);
        }

        /// <summary>
        /// Insert new DataBlock. Use extend to indicate document sequence (document are large than PAGE_SIZE)
        /// </summary>
        public DataBlock InsertBlock(int bytesLength, bool extend)
        {
            var segment = base.Insert((ushort)(bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE), out var index);

            return new DataBlock(this, index, segment, extend, PageAddress.Empty);
        }

        /// <summary>
        /// Update current block returning data block to be fill
        /// </summary>
        public DataBlock UpdateBlock(DataBlock currentBlock, int bytesLength)
        {
            var segment = base.Update(currentBlock.Position.Index, (ushort)(bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE));

            return new DataBlock(this, currentBlock.Position.Index, segment, currentBlock.Extend, currentBlock.NextBlock);
        }

        /// <summary>
        /// Delete single data block inside this page
        /// </summary>
        public void DeleteBlock(byte index)
        {
            base.Delete(index);
        }

        /// <summary>
        /// Get all block positions inside this page that are not extend blocks (initial data block)
        /// </summary>
        public IEnumerable<PageAddress> GetBlocks(bool onlyDataBlock)
        {
            foreach(var index in base.GetUsedIndexs())
            {
                var slotPosition = BasePage.CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(slotPosition);

                var extend = _buffer.ReadBool(position + DataBlock.P_EXTEND);

                if (onlyDataBlock == false || extend == false)
                {
                    yield return new PageAddress(this.PageID, index);
                }
            }
        }
    }
}