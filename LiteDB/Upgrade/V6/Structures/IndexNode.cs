using System;
using System.Collections.Generic;

namespace LiteDB_V6
{
    /// <summary>
    /// Represent a index node inside a Index Page
    /// </summary>
    internal class IndexNode
    {
        /// <summary>
        /// Position of this node inside a IndexPage - Store only Position.Index
        /// </summary>
        public LiteDB.PageAddress Position { get; set; }

        /// <summary>
        /// Pointer to prev value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public LiteDB.PageAddress[] Prev { get; set; }

        /// <summary>
        /// Pointer to next value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public LiteDB.PageAddress[] Next { get; set; }

        /// <summary>
        /// Length of key - used for calculate Node size
        /// </summary>
        public ushort KeyLength { get; set; }

        /// <summary>
        /// The object value that was indexed
        /// </summary>
        public LiteDB.BsonValue Key { get; set; }

        /// <summary>
        /// Reference for a datablock - the value
        /// </summary>
        public LiteDB.PageAddress DataBlock { get; set; }

        /// <summary>
        /// Get page reference
        /// </summary>
        public IndexPage Page { get; set; }

        /// <summary>
        /// Returns if this node is header or tail from collection Index
        /// </summary>
        public bool IsHeadTail(CollectionIndex index)
        {
            return this.Position.Equals(index.HeadNode) || this.Position.Equals(index.TailNode);
        }

        public IndexNode(byte level)
        {
            this.Position = LiteDB.PageAddress.Empty;
            this.DataBlock = LiteDB.PageAddress.Empty;
            this.Prev = new LiteDB.PageAddress[level];
            this.Next = new LiteDB.PageAddress[level];

            for (var i = 0; i < level; i++)
            {
                this.Prev[i] = LiteDB.PageAddress.Empty;
                this.Next[i] = LiteDB.PageAddress.Empty;
            }
        }
    }

    internal class IndexNodeComparer : IEqualityComparer<IndexNode>
    {
        public bool Equals(IndexNode x, IndexNode y)
        {
            if (object.ReferenceEquals(x, y)) return true;

            if (x == null || y == null) return false;

            return x.DataBlock.Equals(y.DataBlock);
        }

        public int GetHashCode(IndexNode obj)
        {
            return obj.DataBlock.GetHashCode();
        }
    }
}