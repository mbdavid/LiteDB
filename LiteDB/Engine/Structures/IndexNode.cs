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
                                                  PageAddress.SIZE; // DataBlock

        private const int P_LEVEL = 0; // 00
        private const int P_DATA_BLOCK = 1; // 01-05
        private const int P_PREV_NODE = 6; // 06-09
        private const int P_NEXT_NODE = 10; // 10-13
        private const int P_PREV_NEXT = 14; // 14-(_level * 5 * 2)
        private int P_KEY => P_PREV_NEXT + (_level * PageAddress.SIZE * 2); // just after NEXT

        private readonly PageAddress _position;
        private readonly byte _level;
        private readonly PageAddress _dataBlock;
        private readonly BsonValue _key;

        private PageAddress _prevNode;
        private PageAddress _nextNode;
        private PageAddress[] _prev;
        private PageAddress[] _next;

        private readonly IndexPage _page;
        private readonly PageSegment _segment;

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
            return INDEX_NODE_FIXED_SIZE +
                (level * 2 * PageAddress.SIZE) + // prev/next
                GetKeyLength(key); // key
        }

        /// <summary>
        /// Get how many bytes will be used to store this value. Must consider:
        /// [1 byte] - BsonType
        /// [1 byte] - KeyLength (used only in String|Byte[])
        /// [N bytes] - BsonValue in bytes (0-255)
        /// </summary>
        public static int GetKeyLength(BsonValue key)
        {
            return 1 +
                ((key.IsString || key.IsBinary) ? 1 : 0) +
                key.GetBytesCount(false);
        }

        /// <summary>
        /// Read index node from page segment
        /// </summary>
        public IndexNode(IndexPage page, PageSegment segment)
        {
            _page = page;
            _segment = segment;

            _position = new PageAddress(page.PageID, segment.Index);
            _level = segment.Buffer[P_LEVEL];
            _dataBlock = segment.Buffer.ReadPageAddress(P_DATA_BLOCK);

            _prevNode = segment.Buffer.ReadPageAddress(P_PREV_NODE);
            _nextNode = segment.Buffer.ReadPageAddress(P_NEXT_NODE);

            _next = new PageAddress[_level];
            _prev = new PageAddress[_level];

            var pos = P_PREV_NEXT;

            for (var i = 0; i < _level; i++)
            {
                _prev[i] = segment.Buffer.ReadPageAddress(pos += PageAddress.SIZE);
                _next[i] = segment.Buffer.ReadPageAddress(pos += PageAddress.SIZE);
            }

            _key = segment.Buffer.ReadIndexKey(P_KEY);

        }

        /// <summary>
        /// Create new index node and persist into pageSegment
        /// </summary>
        public IndexNode(IndexPage page, PageSegment segment, byte level, BsonValue key, PageAddress dataBlock)
        {
            _page = page;
            _segment = segment;

            _position = new PageAddress(page.PageID, segment.Index);
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

            // persist in buffer read only data
            segment.Buffer[P_LEVEL] = level;
            segment.Buffer.Write(dataBlock, P_DATA_BLOCK);
            segment.Buffer.WriteIndexKey(key, P_KEY);

            // prevNode/nextNode must be defined as Empty
            segment.Buffer.Write(_prevNode, P_PREV_NODE);
            segment.Buffer.Write(_nextNode, P_NEXT_NODE);

            // prev/next will be modified by method "Set" (i'm sure about this)

            page.IsDirty = true;
        }

        /// <summary>
        /// Update PrevNode pointer (update in buffer too). Also, set page as dirty
        /// </summary>
        public void SetPrevNode(PageAddress value)
        {
            _prevNode = value;
            _segment.Buffer.Write(value, P_PREV_NODE);
            _page.IsDirty = true;
        }

        /// <summary>
        /// Update NextNode pointer (update in buffer too). Also, set page as dirty
        /// </summary>
        public void SetNextNode(PageAddress value)
        {
            _nextNode = value;
            _segment.Buffer.Write(value, P_NEXT_NODE);
            _page.IsDirty = true;
        }

        /// <summary>
        /// Update Prev[index] pointer (update in buffer too). Also, set page as dirty
        /// </summary>
        public void SetPrev(byte index, PageAddress value)
        {
            _prev[index] = value;
            _segment.Buffer.Write(value, P_PREV_NEXT + (index * PageAddress.SIZE * 2));
            _page.IsDirty = true;
        }

        /// <summary>
        /// Update Next[index] pointer (update in buffer too). Also, set page as dirty
        /// </summary>
        public void SetNext(byte index, PageAddress value)
        {
            _prev[index] = value;
            _segment.Buffer.Write(value, P_PREV_NEXT + (index * PageAddress.SIZE * 2) + PageAddress.SIZE);
            _page.IsDirty = true;
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
            return obj.Position.GetHashCode();
        }
    }
}