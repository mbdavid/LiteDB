using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// The IndexPage thats stores object data.
    /// </summary>
    internal class IndexPage : BasePage
    {
        private const int P_KEY_LENGTH = 1;

        public IndexPage(PageBuffer buffer)
            : base(buffer)
        {
            ENSURE(this.PageType == PageType.Index);
        }

        public IndexPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Index)
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
                // read levels
                var levels = r.ReadByte();

                var node = new IndexNode(levels);

                // read keyLength + key
                var keyLength = r.ReadUInt16();
                r.ReadBsonValue(keyLength);

                // read data block
                node.DataBlock = r.ReadPageAddress();

                // read prev+next
                node.PrevNode = r.ReadPageAddress();
                node.NextNode = r.ReadPageAddress();

                // read double-linked list
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

                // store index key
                w.Write((byte)keyLength);
                w.WriteBsonValue(node.Key);

                // data block position
                w.Write(node.DataBlock);

                // store inter-nodes liked-list
                w.Write(node.PrevNode);
                w.Write(node.NextNode);

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
        /// Update index node inside page
        /// </summary>
        public void UpdateNode(IndexNode node)
        {
            var segment = base.Update(node.Position.Index, node.Length);

            using (var w = new BufferWriter(segment.Buffer))
            {
                var keyLength = segment.Buffer[P_KEY_LENGTH];

                // skip fixed data: levels [1], keyLength [1], key, dataBlock [5]
                w.Skip(1 + 1 + keyLength + 5);

                // update prev/next
                w.Write(node.PrevNode);
                w.Write(node.NextNode);

                // update levels linked-list
                for (var i = 0; i < node.Prev.Length; i++)
                {
                    w.Write(node.Prev[i]);
                    w.Write(node.Next[i]);
                }
            }
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