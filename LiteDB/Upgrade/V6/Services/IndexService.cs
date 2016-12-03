using System;
using System.Collections.Generic;

namespace LiteDB_V6
{
    /// <summary>
    /// Implement a Index service - Add/Remove index nodes on SkipList
    /// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
    /// </summary>
    internal class IndexService
    {
        /// <summary>
        /// Max size of a index entry - usde for string, binary, array and documents
        /// </summary>
        public const int MAX_INDEX_LENGTH = 512;

        private PageService _pager;
        private Random _rand = new Random();

        public IndexService(PageService pager)
        {
            _pager = pager;
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
        /// </summary>
        public IndexNode GetNode(LiteDB.PageAddress address)
        {
            if (address.IsEmpty) return null;
            var page = _pager.GetPage<IndexPage>(address.PageID);
            return page.Nodes[address.Index];
        }

        public IEnumerable<IndexNode> FindAll(CollectionIndex index, int order)
        {
            var cur = this.GetNode(order == LiteDB.Query.Ascending ? index.HeadNode : index.TailNode);

            while (!cur.NextPrev(0, order).IsEmpty)
            {
                cur = this.GetNode(cur.NextPrev(0, order));

                // stop if node is head/tail
                if (cur.IsHeadTail(index)) yield break;

                yield return cur;
            }
        }
    }
}