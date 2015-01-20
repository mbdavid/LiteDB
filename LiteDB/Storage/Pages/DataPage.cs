using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The DataPage thats stores object data.
    /// </summary>
    internal class DataPage : BasePage
    {
        /// <summary>
        /// Returns all data blocks - Each block has one object
        /// </summary>
        public Dictionary<ushort, DataBlock> DataBlocks { get; set; }

        /// <summary>
        /// Bytes available in this page
        /// </summary>
        public override int FreeBytes
        {
            get { return PAGE_AVAILABLE_BYTES - this.DataBlocks.Sum(x => x.Value.Length); }
        }

        public DataPage()
            : base()
        {
            this.PageType = PageType.Data;
            this.DataBlocks = new Dictionary<ushort, DataBlock>();
        }

        public override void Clear()
        {
            base.Clear();
            this.DataBlocks = new Dictionary<ushort, DataBlock>();
        }

        protected override void UpdateItemCount()
        {
            this.ItemCount = (ushort)this.DataBlocks.Count;
        }

        public override void WriteContent(BinaryWriter writer)
        {
            foreach (var block in this.DataBlocks.Values)
            {
                writer.Write(block.Position.Index);
                writer.Write(block.Key);
                writer.Write(block.ExtendPageID);
                foreach (var idx in block.IndexRef)
                {
                    writer.Write(idx);
                }
                writer.Write((ushort)block.Data.Length);
                writer.Write(block.Data);
            }
        }

        public override void ReadContent(BinaryReader reader)
        {
            this.DataBlocks = new Dictionary<ushort, DataBlock>(ItemCount);

            for (var i = 0; i < ItemCount; i++)
            {
                var block = new DataBlock();

                block.Page = this;
                block.Position = new PageAddress(this.PageID, reader.ReadUInt16());
                block.Key = reader.ReadIndexKey();
                block.ExtendPageID = reader.ReadUInt32();

                for(var j = 0; j < CollectionIndex.INDEX_PER_COLLECTION; j++)
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
