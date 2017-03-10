using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    /// <summary>
    /// The DataPage thats stores object data.
    /// </summary>
    internal class DataPage : BasePage
    {
        public override PageType PageType { get { return PageType.Data; } }
        public Dictionary<ushort, DataBlock> DataBlocks { get; set; }

        public DataPage(uint pageID)
            : base(pageID)
        {
            this.DataBlocks = new Dictionary<ushort, DataBlock>();
        }

        protected override void ReadContent(LiteDB.ByteReader reader)
        {
            this.DataBlocks = new Dictionary<ushort, DataBlock>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                var block = new DataBlock();

                block.Page = this;
                block.Position = new LiteDB.PageAddress(this.PageID, reader.ReadUInt16());
                block.ExtendPageID = reader.ReadUInt32();

                for (var j = 0; j < CollectionIndex.INDEX_PER_COLLECTION; j++)
                {
                    block.IndexRef[j] = reader.ReadPageAddress();
                }

                var size = reader.ReadUInt16();
                block.Data = reader.ReadBytes(size);

                this.DataBlocks.Add(block.Position.Index, block);
            }
        }
    }
}