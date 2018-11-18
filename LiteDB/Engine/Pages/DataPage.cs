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
        public DataPage(PageBuffer buffer)
            : base(buffer)
        {
            ENSURE(this.PageType == PageType.Data);
        }

        public DataPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Data)
        {
        }

        /// <summary>
        /// Read single DataBlock
        /// </summary>
        public DataBlock ReadBlock(byte index)
        {
            var segment = base.Get(index);

            return new DataBlock(this, segment);
        }

        /// <summary>
        /// Insert new DataBlock. Use dataIndex as sequencial for large documents
        /// </summary>
        public DataBlock InsertBlock(int bytesLength, byte dataIndex)
        {
            var segment = base.Insert(bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE);

            return new DataBlock(this, segment, dataIndex, PageAddress.Empty);
        }

        /// <summary>
        /// Update current block returning data block to be fill
        /// </summary>
        public DataBlock UpdateBlock(DataBlock currentBlock, int bytesLength)
        {
            var segment = base.Update(currentBlock.Position.Index, bytesLength + DataBlock.DATA_BLOCK_FIXED_SIZE);

            return new DataBlock(this, segment, currentBlock.DataIndex, currentBlock.NextBlock);
        }

        /// <summary>
        /// Delete single data block inside this page
        /// </summary>
        public DataBlock DeleteBlock(byte index)
        {
            var segment = base.Delete(index);

            return new DataBlock(this, segment);
        }
    }
}