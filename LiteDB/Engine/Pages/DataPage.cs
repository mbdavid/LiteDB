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
        //public const int DATA_RESERVED_BYTES = PAGE_AVAILABLE_BYTES / 2;
        public const int DATA_RESERVED_BYTES = 400;

        /// <summary>
        /// Returns all data blocks - Each block has one object
        /// </summary>
        public Dictionary<ushort, DataBlock> DataBlocks { get; set; }

        public DataPage(uint pageID)
            : base(pageID)
        {
            this.DataBlocks = new Dictionary<ushort, DataBlock>();
        }

        /// <summary>
        /// Update freebytes + items count
        /// </summary>
        public override void UpdateItemCount()
        {
            this.ItemCount = (ushort)this.DataBlocks.Count;
            this.FreeBytes = PAGE_AVAILABLE_BYTES - this.DataBlocks.Sum(x => x.Value.Length);
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            this.DataBlocks = new Dictionary<ushort, DataBlock>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                var block = new DataBlock();

                block.Page = this;
                block.Position = new PageAddress(this.PageID, reader.ReadUInt16());
                block.ExtendPageID = reader.ReadUInt32();
                var size = reader.ReadUInt16();
                block.Data = reader.ReadBytes(size);

                this.DataBlocks.Add(block.Position.Index, block);
            }
        }

        protected override void WriteContent(ByteWriter writer)
        {
            foreach (var block in this.DataBlocks.Values)
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