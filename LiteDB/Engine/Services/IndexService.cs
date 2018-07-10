using System;
using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
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

        private Snapshot _snapshot;
        private Random _rand = new Random();

        public IndexService(Snapshot snapshot)
        {
            _snapshot = snapshot;
        }

        /// <summary>
        /// Create a new index and returns head page address (skip list)
        /// </summary>
        public CollectionIndex CreateIndex(CollectionPage collectionPage)
        {
            // get index slot
            var index = collectionPage.GetFreeIndex();

            // get a new index page for first index page
            var page = _snapshot.NewPage<IndexPage>();

            // create a empty node with full max level
            var head = new IndexNode(MAX_LEVEL_LENGTH)
            {
                KeyLength = (ushort)BsonValue.MinValue.GetBytesCount(false),
                Key = BsonValue.MinValue,
                Slot = (byte)index.Slot
            };

            // add as first node
            page.AddNode(head);

            // set index page as dirty
            _snapshot.SetDirty(collectionPage);

            // add indexPage on freelist if has space
            _snapshot.AddOrRemoveToFreeList(true, page, collectionPage, ref index.FreeIndexPageID);

            // point the head/tail node to this new node position
            index.HeadNode = head.Position;

            // insert tail node
            var tail = this.AddNode(collectionPage, index, BsonValue.MaxValue, MAX_LEVEL_LENGTH, null);

            index.TailNode = tail.Position;
            index.KeyCount = index.UniqueKeyCount = 0; // reset counter

            return index;
        }

        /// <summary>
        /// Insert a new node index inside an collection index. Flip coin to know level
        /// </summary>
        public IndexNode AddNode(CollectionPage collectionPage, CollectionIndex index, BsonValue key, IndexNode last)
        {
            // when min/max values, use max level
            var level = 
                key.Type == BsonType.MinValue || key.Type == BsonType.MaxValue ?
                (byte)MAX_LEVEL_LENGTH : this.FlipCoin();

            // set index collection with max-index level
            if (level > index.MaxLevel)
            {
                index.MaxLevel = level;

                _snapshot.SetDirty(collectionPage);
            }

            // call AddNode with key value
            return this.AddNode(collectionPage, index, key, level, last);
        }

        /// <summary>
        /// Insert a new node index inside an collection index.
        /// </summary>
        private IndexNode AddNode(CollectionPage collectionPage, CollectionIndex index, BsonValue key, byte level, IndexNode last)
        {
            // calc key size
            var keyLength = key.GetBytesCount(false);
            var isUniqueKey = true;

            // test for index key maxlength
            if (keyLength > MAX_INDEX_LENGTH) throw LiteException.IndexKeyTooLong();

            // creating a new index node
            var node = new IndexNode(level)
            {
                KeyLength = (ushort)keyLength,
                Key = key,
                Slot = (byte)index.Slot
            };

            // get a free page to insert my index node
            var page = _snapshot.GetFreePage<IndexPage>(index.FreeIndexPageID, node.Length);

            // add index node to page
            page.AddNode(node);

            // now, let's link my index node on right place
            var cur = this.GetNode(index.HeadNode, out var curPage);

            // using as cache last
            IndexNode cache = null;

            // check key is adding max-value key node (need added before tail)
            var isMax = !index.TailNode.IsEmpty && key.Type == BsonType.MaxValue;

            // scan from top left
            for (var i = index.MaxLevel - 1; i >= 0; i--)
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
                    if (diff == 0 && index.Unique) throw LiteException.IndexDuplicateKey(index.Name, key);

                    if (diff == 0) isUniqueKey = false;

                    if (diff == 1) break;
                }

                if (i <= (level - 1)) // level == length
                {
                    // cur = current (immediately before - prev)
                    // node = new inserted node
                    // next = next node (where cur is pointing)
                    // DEBUG(cur.Position.PageID != curPage.PageID, "Current page must be same as currentNode");

                    if (cur.Position.PageID != curPage.PageID)
                    {
                        //TODO must avoid this re-load
                        curPage = _snapshot.GetPage<IndexPage>(cur.Position.PageID);
                    }

                    _snapshot.SetDirty(curPage);

                    // if inserting MaxValue, left add just before tail Node (and not after tail)
                    if (isMax && cur.Position.Equals(index.TailNode))
                    {
                        cur = this.GetNode(cur.Prev[0], out curPage);
                    }

                    node.Next[i] = cur.Next[i];
                    node.Prev[i] = cur.Position;
                    cur.Next[i] = node.Position;

                    var next = this.GetNode(node.Next[i], out var nextPage);

                    if (next != null)
                    {
                        next.Prev[i] = node.Position;
                        _snapshot.SetDirty(nextPage);
                    }
                }
            }

            // add/remove indexPage on freelist if has space
            _snapshot.AddOrRemoveToFreeList(page.FreeBytes > INDEX_RESERVED_BYTES, page, collectionPage, ref index.FreeIndexPageID);

            // if last node exists, create a double link list
            if (last != null)
            {
                // link new node with last node
                if (last.NextNode.IsEmpty == false)
                {
                    // fix link pointer with has more nodes in list
                    var next = this.GetNode(last.NextNode, out var nextPage);
                    next.PrevNode = node.Position;
                    last.NextNode = node.Position;
                    node.PrevNode = last.Position;
                    node.NextNode = next.Position;

                    _snapshot.SetDirty(nextPage);
                }
                else
                {
                    last.NextNode = node.Position;
                    node.PrevNode = last.Position;
                }

                // set last node page as dirty
                // _snapshot.SetDirty(last.Page);
                // must set=drity outside (from Update cmd)
                DEBUG(_snapshot.GetPage<IndexPage>(last.Position.PageID).IsDirty == false, "At this point, last index node page always must be dirty");
            }

            index.KeyCount++;
            if (isUniqueKey) index.UniqueKeyCount++;

            _snapshot.SetDirty(collectionPage);

            return node;
        }

        /// <summary>
        /// Gets all node list from any index node (go forward and backward)
        /// </summary>
        public IEnumerable<IndexNode> GetNodeList(IndexNode node, bool includeInitial)
        {
            var next = node.NextNode;
            var prev = node.PrevNode;

            // returns some initial node
            if (includeInitial) yield return node;

            // go forward
            while (next.IsEmpty == false)
            {
                var n = this.GetNode(next);
                next = n.NextNode;
                yield return n;
            }

            // go backward
            while (prev.IsEmpty == false)
            {
                var p = this.GetNode(prev);
                prev = p.PrevNode;
                yield return p;
            }
        }

        /// <summary>
        /// Deletes an indexNode from a Index and adjust Next/Prev nodes
        /// </summary>
        public void Delete(CollectionPage collectionPage, CollectionIndex index, PageAddress nodeAddress)
        {
            var node = this.GetNode(nodeAddress, out var nodePage);
            var isUniqueKey = false;

            // mark page as dirty here because, if deleted, page type will change
            _snapshot.SetDirty(nodePage);

            for (int i = node.Prev.Length - 1; i >= 0; i--)
            {
                // get previous and next nodes (between my deleted node)
                var prev = this.GetNode(node.Prev[i], out var prevPage);
                var next = this.GetNode(node.Next[i], out var nextPage);

                if (prev != null)
                {
                    prev.Next[i] = node.Next[i];
                    _snapshot.SetDirty(prevPage);
                }
                if (next != null)
                {
                    next.Prev[i] = node.Prev[i];
                    _snapshot.SetDirty(nextPage);
                }

                // in level 0, if any sibling are same value it's not an unique key
                if (i == 0)
                {
                    isUniqueKey = next?.Key == node.Key || prev?.Key == node.Key || false;
                }
            }

            nodePage.DeleteNode(node);

            // if there is no more nodes in this page, delete them
            if (nodePage.NodesCount == 0)
            {
                // first, remove from free list
                _snapshot.AddOrRemoveToFreeList(false, nodePage, collectionPage, ref index.FreeIndexPageID);

                _snapshot.DeletePage(nodePage.PageID);
            }
            else
            {
                // add or remove page from free list
                _snapshot.AddOrRemoveToFreeList(nodePage.FreeBytes > INDEX_RESERVED_BYTES, nodePage, collectionPage, ref index.FreeIndexPageID);
            }

            // now remove node from nodelist 
            var prevNode = this.GetNode(node.PrevNode, out var prevNodePage);
            var nextNode = this.GetNode(node.NextNode, out var nextNodePage);

            if (prevNode != null)
            {
                prevNode.NextNode = node.NextNode;
                _snapshot.SetDirty(prevNodePage);
            }
            if (nextNode != null)
            {
                nextNode.PrevNode = node.PrevNode;
                _snapshot.SetDirty(nextNodePage);
            }

            index.KeyCount--;
            if (isUniqueKey) index.UniqueKeyCount--;

            _snapshot.SetDirty(collectionPage);
        }

        /// <summary>
        /// Drop all indexes pages. Each index use a single page sequence
        /// </summary>
        public void DropIndex(CollectionIndex index)
        {
            var pages = new HashSet<uint>();
            var nodes = this.FindAll(index, Query.Ascending);

            // get reference for pageID from all index nodes
            foreach (var node in nodes)
            {
                pages.Add(node.Position.PageID);

                // for each node I need remove from node list datablock reference
                var prevNode = this.GetNode(node.PrevNode, out var prevNodePage);
                var nextNode = this.GetNode(node.NextNode, out var nextNodePage);

                if (prevNode != null)
                {
                    prevNode.NextNode = node.NextNode;
                    _snapshot.SetDirty(prevNodePage);
                }
                if (nextNode != null)
                {
                    nextNode.PrevNode = node.PrevNode;
                    _snapshot.SetDirty(nextNodePage);
                }
            }

            // now delete all pages
            foreach (var pageID in pages)
            {
                _snapshot.DeletePage(pageID);
            }
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
        /// </summary>
        public IndexNode GetNode(PageAddress address)
        {
            if (address.IsEmpty) return null;
            var page = _snapshot.GetPage<IndexPage>(address.PageID);
            return page.GetNode(address.Index);
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty (return IndexPage as output parameter)
        /// </summary>
        public IndexNode GetNode(PageAddress address, out IndexPage indexPage)
        {
            if (address.IsEmpty)
            {
                indexPage = null;
                return null;
            }

            indexPage = _snapshot.GetPage<IndexPage>(address.PageID);

            return indexPage.GetNode(address.Index);
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
                if (level == MAX_LEVEL_LENGTH) break;
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

            for (var i = index.MaxLevel - 1; i >= 0; i--)
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
                        if (index.Unique) return next;

                        return this.FindBoundary(index, next, value, order * -1, i);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Goto the first/last occurrence of this index value
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

        #endregion
    }
}