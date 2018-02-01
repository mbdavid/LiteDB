using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Represent a index node inside a Index Page
    /// </summary>
    internal class IndexNode
    {
        public const int INDEX_NODE_FIXED_SIZE = 2 + // Position.Index (ushort)
                                                 1 + // Levels (byte)
                                                 2 + // ValueLength (ushort)
                                                 1 + // BsonType (byte)
                                                 1 + // Slot (1 byte)
                                                 (PageAddress.SIZE * 2) + // Prev/Next Node (6 bytes)
                                                 PageAddress.SIZE; // DataBlock

        /// <summary>
        /// Max level used on skip list
        /// </summary>
        public const int MAX_LEVEL_LENGTH = 32;

        /// <summary>
        /// Position of this node inside a IndexPage - Store only Position.Index
        /// </summary>
        public PageAddress Position { get; set; }

        /// <summary>
        /// Slot position of index in data block
        /// </summary>
        public byte Slot { get; set; }

        /// <summary>
        /// Prev node in same document list index nodes
        /// </summary>
        public PageAddress PrevNode { get; set; }

        /// <summary>
        /// Next node in same document list index nodes
        /// </summary>
        public PageAddress NextNode { get; set; }

        /// <summary>
        /// Link to prev value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public PageAddress[] Prev { get; set; }

        /// <summary>
        /// Link to next value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public PageAddress[] Next { get; set; }

        /// <summary>
        /// Length of key - used for calculate Node size
        /// </summary>
        public ushort KeyLength { get; set; }

        /// <summary>
        /// The object value that was indexed
        /// </summary>
        public BsonValue Key { get; set; }

        /// <summary>
        /// Reference for a datablock - the value
        /// </summary>
        public PageAddress DataBlock { get; set; }

        /// <summary>
        /// Get page reference
        /// </summary>
        public IndexPage Page { get; set; }

        /// <summary>
        /// Returns Next (order == 1) OR Prev (order == -1)
        /// </summary>
        public PageAddress NextPrev(int index, int order)
        {
            return order == Query.Ascending ? this.Next[index] : this.Prev[index];
        }

        /// <summary>
        /// Returns if this node is header or tail from collection Index
        /// </summary>
        public bool IsHeadTail(CollectionIndex index)
        {
            return this.Position.Equals(index.HeadNode) || this.Position.Equals(index.TailNode);
        }

        /// <summary>
        /// Get the length size of this node in disk - not persistable
        /// </summary>
        public int Length
        {
            get
            {
                return IndexNode.INDEX_NODE_FIXED_SIZE +
                    (this.Prev.Length * PageAddress.SIZE * 2) + // Prev + Next
                    this.KeyLength; // bytes count in BsonValue
            }
        }

        /// <summary>
        /// Cached document - if null, use DataBlock
        /// </summary>
        public BsonDocument CacheDocument { get; set; }

        public IndexNode(byte level)
        {
            this.Position = PageAddress.Empty;
            this.PrevNode = PageAddress.Empty;
            this.NextNode = PageAddress.Empty;
            this.DataBlock = PageAddress.Empty;
            this.Prev = new PageAddress[level];
            this.Next = new PageAddress[level];

            for (var i = 0; i < level; i++)
            {
                this.Prev[i] = PageAddress.Empty;
                this.Next[i] = PageAddress.Empty;
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