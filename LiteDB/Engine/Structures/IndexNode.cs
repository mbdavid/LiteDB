using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a index node inside a Index Page
    /// </summary>
    internal class IndexNode
    {
        private const int INDEX_NODE_FIXED_SIZE = 1 + // Levels (byte)
                                                  (PageAddress.SIZE * 2) + // Prev/Next Node (5 bytes)
                                                  1 + // KeyLength (byte)
                                                  PageAddress.SIZE + // DataBlock
                                                  1; // BsonType (byte)

        private readonly PageAddress _position;
        private readonly byte _level;
        private readonly BsonValue _key;
        private readonly PageAddress _dataBlock;

        private PageAddress _prevNode;
        private PageAddress _nextNode;
        private PageAddress[] _prev;
        private PageAddress[] _next;

        private readonly IndexPage _page;
        private readonly PageSegment _pageSegment;

        /// <summary>
        /// Position of this node inside a IndexPage (not persist)
        /// </summary>
        public PageAddress Position => _position;

        /// <summary>
        /// Skip-list level (0-31) - [1 byte]
        /// </summary>
        public byte Level => _level;

        /// <summary>
        /// The object value that was indexed (max 255 bytes value)
        /// </summary>
        public BsonValue Key => _key;

        /// <summary>
        /// Reference for a datablock address
        /// </summary>
        public PageAddress DataBlock => _dataBlock;

        /// <summary>
        /// Prev node in same document list index nodes [5 bytes]
        /// </summary>
        public PageAddress PrevNode => _prevNode;

        /// <summary>
        /// Next node in same document list index nodes  [5 bytes]
        /// </summary>
        public PageAddress NextNode => _nextNode;

        /// <summary>
        /// Link to prev value (used in skip lists - Prev.Length = Next.Length) [5 bytes]
        /// </summary>
        public PageAddress[] Prev => _prev;

        /// <summary>
        /// Link to next value (used in skip lists - Prev.Length = Next.Length)
        /// </summary>
        public PageAddress[] Next => _next;

        /// <summary>
        /// Returns Next (order == 1) OR Prev (order == -1)
        /// </summary>
        public PageAddress NextPrev(int index, int order)
        {
            return order == Query.Ascending ? this.Next[index] : this.Prev[index];
        }

        /// <summary>
        /// Calculate how many bytes this node will need on page segment
        /// </summary>
        public static int GetNodeLength(byte level, BsonValue key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get how many bytes will be used to store this value. Must consider:
        /// [1 byte] - BsonType
        /// [1 byte] - KeyLength (used only in String|Byte[])
        /// [N bytes] - BsonValue in bytes (0-255)
        /// </summary>
        public static int GetKeyLength(BsonValue key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read index node from page segment
        /// </summary>
        public IndexNode(IndexPage page, PageSegment pageSegment)
        {
        }

        /// <summary>
        /// Create new index node and persist into pageSegment
        /// </summary>
        public IndexNode(IndexPage page, PageSegment pageSegment, byte level, BsonValue key, PageAddress dataBlock)
        {
            _position = new PageAddress(page.PageID, pageSegment.Index);
            _level = level;
            _key = key;
            _dataBlock = dataBlock;

            _prevNode = PageAddress.Empty;
            _nextNode = PageAddress.Empty;

            _next = new PageAddress[level];
            _prev = new PageAddress[level];

            for (var i = 0; i < level; i++)
            {
                _prev[i] = PageAddress.Empty;
                _next[i] = PageAddress.Empty;
            }

            // persist all data into pageSegment
            // _pageSegment.Buffer[P_LEVEL] = _level;
        }

        // need update methods (persist also in pageSegment) - set page.isdity = true
        public void SetPrevNode(PageAddress value)
        {
        }

        public void SetNextNode(PageAddress value)
        {
        }

        public void SetPrev(byte level, PageAddress value)
        {
        }

        public void SetNext(byte level, PageAddress value)
        {
        }



    }

    internal class IndexNodeComparer : IEqualityComparer<IndexNode>
    {
        public bool Equals(IndexNode x, IndexNode y)
        {
            if (object.ReferenceEquals(x, y)) return true;

            if (x == null || y == null) return false;

            return x.DataBlock == y.DataBlock;
        }

        public int GetHashCode(IndexNode obj)
        {
            return obj.DataBlock.GetHashCode();
        }
    }
}