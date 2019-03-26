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
        /// Insert new DataBlock. Use dataIndex as sequencial for large documents
        /// </summary>
        public DataBlock InsertBlock(int bytesLength, byte dataIndex)
        {
            var index = base.GetFreeIndex();
            var segment = base.Insert(index, bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE);

            return new DataBlock(this, index, segment, dataIndex, PageAddress.Empty);
        }

        /// <summary>
        /// Update current block returning data block to be fill
        /// </summary>
        public DataBlock UpdateBlock(DataBlock currentBlock, int bytesLength)
        {
            var segment = base.Update(currentBlock.Position.Index, bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE);

            return new DataBlock(this, currentBlock.Position.Index, segment, currentBlock.DataIndex, currentBlock.NextBlock);
        }

        /// <summary>
        /// Delete single data block inside this page
        /// </summary>
        public void DeleteBlock(byte index)
        {
            base.Delete(index);
        }

        /// <summary>
        /// Get all block positions inside this page that are DataIndex = 0 (initial data block)
        /// </summary>
        public IEnumerable<PageAddress> GetBlocks(bool onlyRootBlock)
        {
            foreach(var index in base.GetIndexes())
            {
                var slotPosition = BasePage.CalcPositionAddr(index);
                var position = _buffer.ReadUInt16(slotPosition);

                var dataIndex = _buffer[position + DataBlock.P_DATA_INDEX];

                if (onlyRootBlock == false || dataIndex == 0)
                {
                    yield return new PageAddress(this.PageID, index);
                }
            }
        }
    }
}