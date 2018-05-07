using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// The DataPage thats stores object data.
    /// </summary>
    internal class DataPage : BasePage
    {
        /// <summary>
        /// Page type = Extend
        /// </summary>
        public override PageType PageType { get { return PageType.Data; } }

        /// <summary>
        /// If a Data Page has less that free space, it's considered full page for new items. Can be used only for update (DataPage) ~ 50% PAGE_AVAILABLE_BYTES
        /// This value is used for minimize
        /// </summary>
        public const int DATA_RESERVED_BYTES = PAGE_AVAILABLE_BYTES / 2;

        /// <summary>
        /// Returns all data blocks - Each block has one object
        /// </summary>
        private Dictionary<ushort, DataBlock> _dataBlocks = new Dictionary<ushort, DataBlock>();

        public DataPage(uint pageID)
            : base(pageID)
        {
        }

        /// <summary>
        /// Get datablock from internal blocks collection
        /// </summary>
        public DataBlock GetBlock(ushort index)
        {
            return _dataBlocks[index];
        }

        /// <summary>
        /// Add new data block into this page, update counter + free space
        /// </summary>
        public void AddBlock(DataBlock block)
        {
            var index = _dataBlocks.NextIndex();

            block.Position = new PageAddress(this.PageID, index);

            this.ItemCount++;
            this.FreeBytes -= block.Length;

            _dataBlocks.Add(index, block);
        }

        /// <summary>
        /// Update byte array from existing data block. Update free space too
        /// </summary>
        public void UpdateBlockData(DataBlock block, byte[] data)
        {
            this.FreeBytes = this.FreeBytes + block.Data.Length - data.Length;

            block.Data = data;
        }

        /// <summary>
        /// Remove data block from this page. Update counters and free space
        /// </summary>
        public void DeleteBlock(DataBlock block)
        {
            this.ItemCount--;
            this.FreeBytes += block.Length;

            _dataBlocks.Remove(block.Position.Index);
        }

        /// <summary>
        /// Get block counter from this page
        /// </summary>
        public int BlocksCount => _dataBlocks.Count;

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            _dataBlocks = new Dictionary<ushort, DataBlock>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                var block = new DataBlock();

                block.Page = this;
                block.Position = new PageAddress(this.PageID, reader.ReadUInt16());
                block.ExtendPageID = reader.ReadUInt32();
                var size = reader.ReadUInt16();
                block.Data = reader.ReadBytes(size);

                _dataBlocks.Add(block.Position.Index, block);
            }
        }

        protected override void WriteContent(ByteWriter writer)
        {
            foreach (var block in _dataBlocks.Values)
            {
                writer.Write(block.Position.Index);
                writer.Write(block.ExtendPageID);
                writer.Write((ushort)block.Data.Length);
                writer.Write(block.Data);
            }
        }

        #endregion
    }
}