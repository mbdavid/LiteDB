using System;
using System.Collections.Generic;

namespace LiteDB
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
        /// Create a new index and returns head page address (skip list)
        /// </summary>
        public CollectionIndex CreateIndex(CollectionPage col)
        {
            // get index slot
            var index = col.GetFreeIndex();

            // get a new index page for first index page
            var page = _pager.NewPage<IndexPage>();

            // create a empty node with full max level
            var head = new IndexNode(IndexNode.MAX_LEVEL_LENGTH)
            {
                Key = BsonValue.MinValue,
                KeyLength = (ushort)BsonValue.MinValue.GetBytesCount(false),
                Page = page,
                Position = new PageAddress(page.PageID, 0)
            };

            // add as first node
            page.Nodes.Add(head.Position.Index, head);

            // update freebytes + item count (for head)
            page.UpdateItemCount();

            _pager.SetDirty(index.Page);

            // add indexPage on freelist if has space
            _pager.AddOrRemoveToFreeList(true, page, index.Page, ref index.FreeIndexPageID);

            // point the head/tail node to this new node position
            index.HeadNode = head.Position;

            // insert tail node
            var tail = this.AddNode(index, BsonValue.MaxValue, IndexNode.MAX_LEVEL_LENGTH);

            index.TailNode = tail.Position;

            return index;
        }

        /// <summary>
        /// Insert a new node index inside an collection index. Flip coin to know level
        /// </summary>
        public IndexNode AddNode(CollectionIndex index, BsonValue key)
        {
            // call AddNode normalizing value
            return this.AddNode(index, key.Normalize(index.Options), this.FlipCoin());
        }

        /// <summary>
        /// Insert a new node index inside an collection index.
        /// </summary>
        private IndexNode AddNode(CollectionIndex index, BsonValue key, byte level)
        {
            // calc key size
            var keyLength = key.GetBytesCount(false);

            if (keyLength > MAX_INDEX_LENGTH)
            {
                throw LiteException.IndexKeyTooLong();
            }

            // creating a new index node
            var node = new IndexNode(level)
            {
                Key = key,
                KeyLength = (ushort)keyLength
            };

            // get a free page to insert my index node
            var page = _pager.GetFreePage<IndexPage>(index.FreeIndexPageID, node.Length);

            node.Position = new PageAddress(page.PageID, page.Nodes.NextIndex());
            node.Page = page;

            // add index node to page
            page.Nodes.Add(node.Position.Index, node);

            // update freebytes + items count
            page.UpdateItemCount();

            // now, let's link my index node on right place
            var cur = this.GetNode(index.HeadNode);

            // using as cache last
            IndexNode cache = null;

            // scan from top left
            for (var i = IndexNode.MAX_LEVEL_LENGTH - 1; i >= 0; i--)
            {
                // get cache for last node
                cache = cache != null && cache.Position.Equals(cur.Next[i]) ? cache : this.GetNode(cur.Next[i]);

                // for(; <while_not_this>; <do_this>) { ... }
                for (; cur.Next[i].IsEmpty == false; cur = cache)
                {
                    // get cache for last node
                    cache = cache != null && cache.Position.Equals(cur.Next[i]) ? cache : this.GetNode(cur.Next[i]);

                    // read next node to compare
                    var diff = cache.Key.CompareTo(key);

                    // if unique and diff = 0, throw index exception (must rollback transaction - others nodes can be dirty)
                    if (diff == 0 && index.Options.Unique) throw LiteException.IndexDuplicateKey(index.Field, key);

                    if (diff == 1) break;
                }

                if (i <= (level - 1)) // level == length
                {
                    // cur = current (imediatte before - prev)
                    // node = new inserted node
                    // next = next node (where cur is poiting)
                    _pager.SetDirty(cur.Page);

                    node.Next[i] = cur.Next[i];
                    node.Prev[i] = cur.Position;
                    cur.Next[i] = node.Position;

                    var next = this.GetNode(node.Next[i]);

                    if (next != null)
                    {
                        _pager.SetDirty(next.Page);
                        next.Prev[i] = node.Position;
                    }
                }
            }

            // add/remove indexPage on freelist if has space
            _pager.AddOrRemoveToFreeList(page.FreeBytes > IndexPage.INDEX_RESERVED_BYTES, page, index.Page, ref index.FreeIndexPageID);

            return node;
        }

        /// <summary>
        /// Delete indexNode from a Index  ajust Next/Prev nodes
        /// </summary>
        public void Delete(CollectionIndex index, PageAddress nodeAddress)
        {
            var node = this.GetNode(nodeAddress);
            var page = node.Page;

            // mark page as dirty
            _pager.SetDirty(page);

            for (int i = node.Prev.Length - 1; i >= 0; i--)
            {
                // get previus and next nodes (between my deleted node)
                var prev = this.GetNode(node.Prev[i]);
                var next = this.GetNode(node.Next[i]);

                if (prev != null)
                {
                    _pager.SetDirty(prev.Page);
                    prev.Next[i] = node.Next[i];
                }
                if (next != null)
                {
                    _pager.SetDirty(next.Page);
                    next.Prev[i] = node.Prev[i];
                }
            }

            page.Nodes.Remove(node.Position.Index);

            // update freebytes + items count
            page.UpdateItemCount();

            // if there is no more nodes in this page, delete them
            if (page.Nodes.Count == 0)
            {
                // first, remove from free list
                _pager.AddOrRemoveToFreeList(false, page, index.Page, ref index.FreeIndexPageID);

                _pager.DeletePage(page.PageID);
            }
            else
            {
                // add or remove page from free list
                _pager.AddOrRemoveToFreeList(page.FreeBytes > IndexPage.INDEX_RESERVED_BYTES, node.Page, index.Page, ref index.FreeIndexPageID);
            }
        }

        /// <summary>
        /// Drop all indexes pages
        /// </summary>
        public void DropIndex(CollectionIndex index)
        {
            var pages = new HashSet<uint>();
            var nodes = this.FindAll(index, Query.Ascending);

            // get reference for pageID from all index nodes
            foreach (var node in nodes)
            {
                pages.Add(node.Position.PageID);
            }

            // now delete all pages
            foreach (var pageID in pages)
            {
                _pager.DeletePage(pageID);
            }
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
        /// </summary>
        public IndexNode GetNode(PageAddress address)
        {
            if (address.IsEmpty) return null;
            var page = _pager.GetPage<IndexPage>(address.PageID);
            return page.Nodes[address.Index];
        }

        /// <summary>
        /// Flip coin - skip list - returns level node (start in 1)
        /// </summary>
        public byte FlipCoin()
        {
            byte level = 1;
            for (int R = _rand.Next(); (R & 1) == 1; R >>= 1)
            {
                level++;
                if (level == IndexNode.MAX_LEVEL_LENGTH) break;
            }
            return level;
        }

        #region Find

        public IEnumerable<IndexNode> FindAll(CollectionIndex index, int order)
        {
            var cur = this.GetNode(order == Query.Ascending ? index.HeadNode : index.TailNode);

            while (!cur.NextPrev(0, order).IsEmpty)
            {
                cur = this.GetNode(cur.NextPrev(0, order));

                // stop if node is head/tail
                if (cur.IsHeadTail(index)) yield break;

                yield return cur;
            }
        }

        /// <summary>
        /// Find first node that index match with value. If not found but sibling = true, returns near node (only non-unique index)
        /// Before find, value must be normalized
        /// </summary>
        public IndexNode Find(CollectionIndex index, BsonValue value, bool sibling, int order)
        {
            var cur = this.GetNode(order == Query.Ascending ? index.HeadNode : index.TailNode);

            for (var i = IndexNode.MAX_LEVEL_LENGTH - 1; i >= 0; i--)
            {
                for (; cur.NextPrev(i, order).IsEmpty == false; cur = this.GetNode(cur.NextPrev(i, order)))
                {
                    var next = this.GetNode(cur.NextPrev(i, order));
                    var diff = next.Key.CompareTo(value);

                    if (diff == order && (i > 0 || !sibling)) break;
                    if (diff == order && i == 0 && sibling)
                    {
                        return next.IsHeadTail(index) ? null : next;
                    }

                    // if equals, test for duplicates - go back to first occurs on duplicate values
                    if (diff == 0)
                    {
                        // if unique index has no duplicates - just return node
                        if (index.Options.Unique) return next;

                        return this.FindBoundary(index, next, value, order * -1, i);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Go first/last occurence of this index value
        /// </summary>
        private IndexNode FindBoundary(CollectionIndex index, IndexNode cur, BsonValue value, int order, int level)
        {
            var last = cur;

            while (cur.Key.CompareTo(value) == 0)
            {
                last = cur;
                cur = this.GetNode(cur.NextPrev(0, order));
                if (cur.IsHeadTail(index)) break;
            }

            return last;
        }

        #endregion Find
    }
}