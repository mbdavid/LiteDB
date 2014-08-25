using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class IndexPage : BasePage
    {
        public Dictionary<ushort, IndexNode> Nodes { get; set; }

        /// <summary>
        /// Bytes available in this page
        /// </summary>
        public override int FreeBytes
        {
            get { return PAGE_AVAILABLE_BYTES - Nodes.Sum(x => x.Value.Length); }
        }

        public IndexPage()
            : base()
        {
            this.PageType = LiteDB.PageType.Index;
            this.Nodes = new Dictionary<ushort, IndexNode>();
        }

        protected override void UpdateItemCount()
        {
            this.ItemCount = this.Nodes.Count;
        }

        public override void Clear()
        {
            base.Clear();
            this.Nodes = new Dictionary<ushort, IndexNode>();
        }

        public override void ReadContent(BinaryReader reader)
        {
            this.Nodes = new Dictionary<ushort, IndexNode>(this.ItemCount);

            for (var i = 0; i < this.ItemCount; i++)
            {
                var index = reader.ReadUInt16();
                var levels = reader.ReadByte();

                var node = new IndexNode(levels);

                node.Page = this;
                node.Position = new PageAddress(this.PageID, index);
                node.Key = reader.ReadIndexKey();
                node.DataBlock = reader.ReadPageAddress();

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    node.Prev[j] = reader.ReadPageAddress();
                    node.Next[j] = reader.ReadPageAddress();
                }

                this.Nodes.Add(node.Position.Index, node);
            }            
        }

        public override void WriteContent(BinaryWriter writer)
        {
            foreach (var node in this.Nodes.Values)
            {
                writer.Write(node.Position.Index); // Node Index on this page
                writer.Write((byte)node.Prev.Length); // Level length
                writer.Write(node.Key); // Key
                writer.Write(node.DataBlock); // Data block reference

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    writer.Write(node.Prev[j]);
                    writer.Write(node.Next[j]);
                }
            }
        }
    }
}
