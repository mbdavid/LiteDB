using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Represent a index node inside a Index Page
    /// </summary>
    internal class IndexNode : IEqualityComparer<IndexNode>
    {
        public const int INDEX_NODE_FIXED_SIZE = 2 + // Position.Index (ushort)
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
        /// Pointer to prev value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public PageAddress[] Prev { get; set; }

        /// <summary>
        /// Pointer to next value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public PageAddress[] Next { get; set; }

        /// <summary>
        /// The object key that was indexed
        /// </summary>
        public IndexKey Key { get; set; }

        /// <summary>
        /// Reference for a datablock - the value
        /// </summary>
        public PageAddress DataBlock { get; set; }

        /// <summary>
        /// Get page reference
        /// </summary>
        public IndexPage Page { get; set; }

        /// <summary>
        /// Get the length size of this node in disk - not persistable
        /// </summary>
        public int Length
        {
            get 
            { 
                return IndexNode.INDEX_NODE_FIXED_SIZE + 
                    (this.Prev.Length * PageAddress.SIZE * 2) + // Prev + Next
                    Key.Length; // Key
            }
        }

        public IndexNode(byte length)
        {
            this.Position = PageAddress.Empty;
            this.DataBlock = PageAddress.Empty;
            this.Prev = new PageAddress[length];
            this.Next = new PageAddress[length];

            for (var i = 0; i < length; i++)
            {
                this.Prev[i] = PageAddress.Empty;
                this.Next[i] = PageAddress.Empty;
            }
        }

        public bool Equals(IndexNode x, IndexNode y)
        {
            return x.Position.Equals(y.Position);
        }

        public int GetHashCode(IndexNode obj)
        {
            return obj.GetHashCode();
        }
    }
}
