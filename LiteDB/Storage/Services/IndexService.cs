using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Implement a Index service - Add/Remove index nodes on SkipList
    /// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
    /// </summary>
    internal class IndexService
    {
        private PageService _pager;
        private CacheService _cache;

        private Random _rand = new Random();

        public IndexService(CacheService cache, PageService pager)
        {
            _cache = cache;
            _pager = pager;
        }

        /// <summary>
        /// Create a new index and returns head page address (skip list)
        /// </summary>
        public CollectionIndex CreateIndex(CollectionIndex index)
        {
            // get a new index page for first index page
            var page = _pager.NewPage<IndexPage>();

            // create a empty node with full max level
            var node = new IndexNode(IndexNode.MAX_LEVEL_LENGTH) { Key = new IndexKey(null), Page = page };

            node.Position = new PageAddress(page.PageID, 0);

            // add as first node
            page.Nodes.Add(node.Position.Index, node);

            // add/remove indexPage on freelist if has space
            _pager.AddOrRemoveToFreeList(page.FreeBytes > BasePage.RESERVED_BYTES, page, index.Page, ref index.FreeIndexPageID);

            // point the head node to this new node position
            index.HeadNode = node.Position;

            index.Page.IsDirty = true;
            page.IsDirty = true;

            return index;
        }

        /// <summary>
        /// Insert a new node index inside a index. Use skip list
        /// </summary>
        public IndexNode AddNode(CollectionIndex index, object value)
        {
            // create persist value - used on key
            var key = new IndexKey(value);

            var level = this.FlipCoin();

            // creating a new index node 
            var node = new IndexNode(level) { Key = key };

            // get a free page to insert my index node
            var page = _pager.GetFreePage<IndexPage>(index.FreeIndexPageID, node.Length);

            node.Position = new PageAddress(page.PageID, page.Nodes.NextIndex());
            node.Page = page;

            // add index node to page
            page.Nodes.Add(node.Position.Index, node);

            // now, let's link my index node on right place
            var cur = this.GetNode(index.HeadNode);

            // scan from top left
            for (int i = IndexNode.MAX_LEVEL_LENGTH - 1; i >= 0; i--)
            {
                // for(; <while_not_this>; <do_this>) { ... }
                for (; cur.Next[i].IsEmpty == false; cur = this.GetNode(cur.Next[i]))
                {
                    // read next node to compare
                    var diff = this.GetNode(cur.Next[i]).Key.CompareTo(key);

                    // if unique and diff = 0, throw index exception (must rollback transaction - others nodes can be dirty)
                    if (diff == 0 && index.Unique) throw new LiteException(string.Format("Cannot insert duplicate key in unique index '{0}'. The duplicate value is '{1}'.", index.Field, value));

                    if (diff == 1) break;
                }

                if (i <= (level - 1)) // level == length
                {
                    // cur = current (imediatte before - prev)
                    // node = new inserted node 
                    // next = next node (where cur is poiting)

                    node.Next[i] = cur.Next[i];
                    node.Prev[i] = cur.Position;
                    cur.Next[i] = node.Position;

                    var next = this.GetNode(node.Next[i]);

                    if (next != null)
                    {
                        next.Prev[i] = node.Position;

                        next.Page.IsDirty = true;
                    }

                    cur.Page.IsDirty = true;
                }
            }

            // add/remove indexPage on freelist if has space
            _pager.AddOrRemoveToFreeList(page.FreeBytes > BasePage.RESERVED_BYTES, page, index.Page, ref index.FreeIndexPageID);

            page.IsDirty = true;

            return node;
        }

        /// <summary>
        /// Delete indexNode from a Index  ajust Next/Prev nodes
        /// </summary>
        public void Delete(CollectionIndex index, PageAddress nodeAddress)
        {
            var node = this.GetNode(nodeAddress);
            var page = node.Page;

            for (int i = node.Prev.Length - 1; i >= 0; i--)
            {
                // get previus and next nodes (between my deleted node)
                var prev = this.GetNode(node.Prev[i]);
                var next = this.GetNode(node.Next[i]);

                if (prev != null)
                {
                    prev.Next[i] = node.Next[i];
                    prev.Page.IsDirty = true;
                }
                if (next != null)
                {
                    next.Prev[i] = node.Prev[i];
                    next.Page.IsDirty = true;
                }
            }

            page.Nodes.Remove(node.Position.Index);

            // if there is no more nodes in this page, delete them
            if (page.Nodes.Count == 0)
            {
                // first, remove from free list
                _pager.AddOrRemoveToFreeList(false, page, index.Page, ref index.FreeIndexPageID);

                _pager.DeletePage(page.PageID, false);
            }
            else
            {
                // add or remove page from free list
                _pager.AddOrRemoveToFreeList(page.FreeBytes > BasePage.RESERVED_BYTES, node.Page, index.Page, ref index.FreeIndexPageID);
            }

            page.IsDirty = true;
        }

        /// <summary>
        /// Get a node inside a page using PageAddress
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
                if (level == IndexNode.MAX_LEVEL_LENGTH - 1) break;
            }
            return level;
        }

        #region Find index nodes

        /// <summary>
        /// Return all nodes
        /// </summary>
        public IEnumerable<IndexNode> FindAll(CollectionIndex index)
        {
            var cur = this.GetNode(index.HeadNode);

            while (!cur.Next[0].IsEmpty)
            {
                cur = this.GetNode(cur.Next[0]);

                yield return cur;
            }
        }

        /// <summary>
        /// Find first indexNode that match with value - if not found, can return first greater (used for greaterThan/Between)
        /// </summary>
        public IndexNode FindOne(CollectionIndex index, object value, bool greater = false)
        {
            var cur = this.GetNode(index.HeadNode);
            var key = new IndexKey(value);

            for (int i = IndexNode.MAX_LEVEL_LENGTH - 1; i >= 0; i--)
            {
                for (; cur.Next[i].IsEmpty == false; cur = this.GetNode(cur.Next[i]))
                {
                    var next = this.GetNode(cur.Next[i]);
                    var diff = next.Key.CompareTo(key);

                    if (diff == 1 && (i > 0 || !greater)) break;
                    if (diff == 1 && i == 0 && greater) return next;

                    // if equals, test for duplicates
                    if (diff == 0)
                    {
                        var last = next;
                        while (next.Key.CompareTo(key) == 0)
                        {
                            last = next;
                            if (index.HeadNode.Equals(next.Prev[0])) break;
                            next = this.GetNode(next.Prev[0]);
                        }

                        return last;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Find all indexNodes that match with value
        /// </summary>
        public IEnumerable<IndexNode> FindEquals(CollectionIndex index, object value)
        {
            // find first indexNode
            var node = this.FindOne(index, value);

            if (node == null) yield break;

            yield return node;

            var key = new IndexKey(value);

            // navigate using next[0] do next node - if equals, returns
            while (!node.Next[0].IsEmpty && ((node = this.GetNode(node.Next[0])).Key.CompareTo(key) == 0))
            {
                yield return node;
            }
        }

        /// <summary>
        /// Find nodes between start and end and (inclusive)
        /// </summary>
        public IEnumerable<IndexNode> FindBetween(CollectionIndex index, object start, object end)
        {
            // find first indexNode
            var node = this.FindOne(index, start, true);
            var key = new IndexKey(end);

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var diff = node.Key.CompareTo(key);

                if (diff <= 0) yield return node;

                node = this.GetNode(node.Next[0]);
            }
        }

        /// <summary>
        /// Find nodes startswith a string
        /// </summary>
        public IEnumerable<IndexNode> FindStarstWith(CollectionIndex index, string text)
        {
            // find first indexNode
            var node = this.FindOne(index, text, true);

            // navigate using next[0] do next node - if less or equals returns
            while (node != null)
            {
                var key = node.Key.ToString();

                if (key.StartsWith(text, StringComparison.InvariantCultureIgnoreCase)) yield return node;

                node = this.GetNode(node.Next[0]);
            }
        }

        /// <summary>
        /// Find all nodes less than value (can be inclusive) 
        /// </summary>
        public IEnumerable<IndexNode> FindLessThan(CollectionIndex index, object value, bool includeValue = false)
        {
            var key = new IndexKey(value);

            foreach (var node in this.FindAll(index))
            {
                var diff = node.Key.CompareTo(key);

                if (diff == 1 || (!includeValue && diff == 0)) break;

                yield return node;
            }
        }

        /// <summary>
        /// Find all indexNodes that are greater/equals than value
        /// </summary>
        public IEnumerable<IndexNode> FindGreaterThan(CollectionIndex index, object value, bool includeValue = false)
        {
            // find first indexNode
            var node = this.FindOne(index, value, true);

            if (node == null) yield break;

            var key = new IndexKey(value);

            // move until next is last
            while (node != null)
            {
                var diff = node.Key.CompareTo(key);

                if (diff == 1 || (includeValue && diff == 0)) yield return node;

                node = this.GetNode(node.Next[0]);
            }
        }

        public IEnumerable<IndexNode> FindIn(CollectionIndex index, object[] values)
        {
            foreach (var value in values.Distinct())
            {
                foreach(var node in this.FindEquals(index, value))
                {
                    yield return node;
                }
            }
        }

        #endregion
    }
}
