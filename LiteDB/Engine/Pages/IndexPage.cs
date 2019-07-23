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
        /// <summary>
        /// Read existing IndexPage in buffer
        /// </summary>
        public IndexPage(PageBuffer buffer)
            : base(buffer)
        {
            if (this.PageType != PageType.Index) throw new LiteException(0, $"Invalid IndexPage buffer on {PageID}");
        }

        /// <summary>
        /// Create new IndexPage
        /// </summary>
        public IndexPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Index)
        {
        }

        /// <summary>
        /// Read single IndexNode
        /// </summary>
        public IndexNode GetIndexNode(byte index)
        {
            var segment = base.Get(index);

            var node = new IndexNode(this, index, segment);

            return node;
        }

        /// <summary>
        /// Insert new IndexNode. After call this, "node" instance can't be changed
        /// </summary>
        public IndexNode InsertIndexNode(byte slot, byte level, BsonValue key, PageAddress dataBlock, int bytesLength)
        {
            var segment = base.Insert((ushort)bytesLength, out var index);

            var node = new IndexNode(this, index, segment, slot, level, key, dataBlock);

            return node;
        }

        /// <summary>
        /// Delete index node based on page index
        /// </summary>
        public void DeleteIndexNode(byte index)
        {
            base.Delete(index);
        }

        /// <summary>
        /// Get all index nodes inside this page
        /// </summary>
        public IEnumerable<IndexNode> GetIndexNodes()
        {
            foreach (var index in base.GetUsedIndexs())
            {
                yield return this.GetIndexNode(index);
            }
        }
    }
}