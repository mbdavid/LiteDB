using System.Collections.Generic;
using System.Linq;

namespace LiteDB_V6
{
    internal class IndexPage : BasePage
    {
        /// <summary>
        /// Page type = Index
        /// </summary>
        public override PageType PageType { get { return PageType.Index; } }

        public Dictionary<ushort, IndexNode> Nodes { get; set; }

        public IndexPage(uint pageID)
            : base(pageID)
        {
            this.Nodes = new Dictionary<ushort, IndexNode>();
        }
		
        protected override void ReadContent(LiteDB.ByteReader reader)
        {
            this.Nodes = new Dictionary<ushort, IndexNode>(this.ItemCount);

            for (var i = 0; i < this.ItemCount; i++)
            {
                var index = reader.ReadUInt16();
                var levels = reader.ReadByte();

                var node = new IndexNode(levels);

                node.Page = this;
                node.Position = new LiteDB.PageAddress(this.PageID, index);
                node.KeyLength = reader.ReadUInt16();
                node.Key = reader.ReadBsonValue(node.KeyLength);
                node.DataBlock = reader.ReadPageAddress();

                for (var j = 0; j < node.Prev.Length; j++)
                {
                    node.Prev[j] = reader.ReadPageAddress();
                    node.Next[j] = reader.ReadPageAddress();
                }

                this.Nodes.Add(node.Position.Index, node);
            }
        }
    }
}