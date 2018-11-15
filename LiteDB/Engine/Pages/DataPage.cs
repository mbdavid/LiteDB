using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        }

        public DataPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Data)
        {
        }

        /// <summary>
        /// Insert new DataBlock. Use dataIndex as sequencial for large documents
        /// </summary>
        public DataBlock InsertBlock(int bytesLength, byte dataIndex)
        {
            var segment = base.Insert(bytesLength + 6);

            return new DataBlock(this, segment, dataIndex);
        }

        /// <summary>
        /// Read single DataBlock
        /// </summary>
        public DataBlock ReadBlock(byte index)
        {
            var segment = base.Get(index);

            return new DataBlock(this, segment);
        }

        public DataBlock UpdateBlock(byte index, int bytesLength, byte dataIndex)
        {
            var segment = base.Update(index, bytesLength);

            return new DataBlock(this, segment, dataIndex);
        }

        public DataBlock DeleteBlock(byte index)
        {
            var segment = base.Delete(index);

            return new DataBlock(this, segment);
        }
    }
}