using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// The IndexPage thats stores object data.
    /// </summary>
    internal class IndexPage : BasePage
    {
        public IndexPage(PageBuffer buffer)
            : base(buffer)
        {
        }

        public IndexPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Data)
        {
        }

        /// <summary>
        /// Read single IndexNode
        /// </summary>
        public IndexNode ReadNode(byte index)
        {
            var segment = base.Get(index);

            using (var r = new BufferReader(segment.Buffer))
            {
                var levels = r.ReadByte();

                var node = new IndexNode(levels);

                node.PrevNode = r.ReadPageAddress();
                node.NextNode = r.ReadPageAddress();

                node.DataBlock = r.ReadPageAddress();

                var keyLength = r.ReadUInt16();
                r.ReadBsonValue(keyLength);

                for (var i = 0; i < levels; i++)
                {
                    node.Prev[i] = r.ReadPageAddress();
                    node.Next[i] = r.ReadPageAddress();
                }

                return node;
            }
        }

        /// <summary>
        /// Insert new IndexNode. After call this, "node" instance can't be changed
        /// </summary>
        public PageAddress InsertNode(IndexNode node)
        {
            var segment = base.Insert(node.Length);

            using (var w = new BufferWriter(segment.Buffer))
            {
                var keyLength = node.Key.GetBytesCount(false);

                // store linked-list levels
                w.Write((byte)node.Prev.Length);

                // store inter-nodes liked-list
                w.Write(node.PrevNode);
                w.Write(node.NextNode);

                // data block position
                w.Write(node.DataBlock);

                // store index key
                w.Write((ushort)keyLength);
                w.WriteBsonValue(node.Key);

                // store levels linked-list
                for (var i = 0; i < node.Prev.Length; i++)
                {
                    w.Write(node.Prev[i]);
                    w.Write(node.Next[i]);
                }
            }

            node.Position = new PageAddress(this.PageID, segment.Index);

            return node.Position;
        }

        /// <summary>
        /// Delete index node based on page index
        /// </summary>
        public void DeleteNode(byte index)
        {
            base.Delete(index);
        }
    }
}