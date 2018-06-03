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
        /// <summary>
        /// Page type = Extend
        /// </summary>
        public override PageType PageType { get { return PageType.Data; } }

        /// <summary>
        /// Returns all data blocks - Each block has one object
        /// </summary>
        private Dictionary<ushort, DataBlock> _dataBlocks = new Dictionary<ushort, DataBlock>();

        /// <summary>
        /// Expose DataBlocks
        /// </summary>
        public Dictionary<ushort, DataBlock> DataBlocks => _dataBlocks;

        private DataPage()
        {
        }

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
            this.FreeBytes -= block.BlockLength;

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
            this.FreeBytes += block.BlockLength;

            _dataBlocks.Remove(block.Position.Index);
        }

        /// <summary>
        /// Get block counter from this page
        /// </summary>
        public int BlocksCount => _dataBlocks.Count;

        #region Read/Write pages

        protected override void ReadContent(BinaryReader reader, bool utcDate)
        {
            _dataBlocks = new Dictionary<ushort, DataBlock>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                var block = new DataBlock();

                block.Page = this;
                block.Position = new PageAddress(this.PageID, reader.ReadUInt16());
                block.ExtendPageID = reader.ReadUInt32();
                block.DocumentLength = reader.ReadInt32();
                var size = reader.ReadUInt16();
                block.Data = reader.ReadBytes(size);

                _dataBlocks.Add(block.Position.Index, block);
            }
        }

        protected override void WriteContent(BinaryWriter writer)
        {
            foreach (var block in _dataBlocks.Values)
            {
                writer.Write(block.Position.Index);
                writer.Write(block.ExtendPageID);
                writer.Write(block.DocumentLength);
                writer.Write((ushort)block.Data.Length);
                writer.Write(block.Data);
            }
        }

        public override BasePage Clone()
        {
            var page = new DataPage
            {
                // base page
                PageID = this.PageID,
                PrevPageID = this.PrevPageID,
                NextPageID = this.NextPageID,
                ItemCount = this.ItemCount,
                FreeBytes = this.FreeBytes,
                TransactionID = this.TransactionID,
                // data page
                _dataBlocks = new Dictionary<ushort, DataBlock>()
            };

            foreach (var item in _dataBlocks) page._dataBlocks.Add(item.Key, item.Value.Clone(page));

            return page;
        }

        #endregion

    }
}