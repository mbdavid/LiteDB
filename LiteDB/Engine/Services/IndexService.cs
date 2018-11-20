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
        private readonly Random _rand = new Random();
        private readonly Snapshot _snapshot;
        private readonly CollectionPage _collectionPage;

        public IndexService(Snapshot snapshot, CollectionPage collectionPage)
        {
            _snapshot = snapshot;
            _collectionPage = collectionPage;
        }

        /// <summary>
        /// Create a new index and returns head page address (skip list)
        /// </summary>
        public CollectionIndex CreateIndex(string name, string expr, bool unique)
        {
            var index = _collectionPage.InsertIndex(name, expr, unique);

            // get a free index page for head note
            var indexPage = _snapshot.GetFreePage<IndexPage>(IndexNode.GetNodeLength(MAX_LEVEL_LENGTH, BsonValue.MinValue));

            // for head node, just insert in page
            var head = indexPage.InsertNode(MAX_LEVEL_LENGTH, BsonValue.MinValue, PageAddress.Empty);

            // set head position
            index.HeadNode = head.Position;

            // insert tail node (now, use offical AddNode method)
            var tail = this.AddNode(index, BsonValue.MaxValue, PageAddress.Empty, MAX_LEVEL_LENGTH, null);

            // set tail position
            index.TailNode = tail.Position;

            return index;
        }

        /// <summary>
        /// Insert a new node index inside an collection index. Flip coin to know level
        /// </summary>
        public IndexNode AddNode(CollectionIndex index, BsonValue key, PageAddress dataBlock, IndexNode last)
        {
            // do not accept Min/Max value as index key (only head/tail can have this value)
            if (key.IsMaxValue || key.IsMinValue)
            {
                throw LiteException.InvalidIndexKey($"BsonValue MaxValue/MinValue are not supported as index key");
            }

            // random level (flip coin mode)
            var level = this.FlipCoin();

            // set index collection with max-index level
            if (level > index.MaxLevel)
            {
                // update max level
                _collectionPage.UpdateIndex(index.Name).MaxLevel = level;
            }

            // call AddNode with key value
            return this.AddNode(index, key, dataBlock, level, last);
        }

        /// <summary>
        /// Insert a new node index inside an collection index.
        /// </summary>
        private IndexNode AddNode(CollectionIndex index, BsonValue key, PageAddress dataBlock, byte level, IndexNode last)
        {
            var keyLength = IndexNode.GetKeyLength(key);

            // test for index key maxlength (length must fit in 1 byte)
            if (keyLength > MAX_INDEX_KEY_LENGTH) throw LiteException.InvalidIndexKey($"Index key must be less than {MAX_INDEX_KEY_LENGTH} bytes.");

            // get a free index page for head note
            var indexPage = _snapshot.GetFreePage<IndexPage>(IndexNode.GetNodeLength(level, key));

            // create node in buffer
            var node = indexPage.InsertNode(level, key, dataBlock);

            // now, let's link my index node on right place
            var cur = this.GetNode(index.HeadNode);

            // using as cache last
            IndexNode cache = null;

            // scan from top left
            for (byte i = (byte)(index.MaxLevel - 1); i >= 0; i--)
            {
                // get cache for last node
                cache = cache != null && cache.Position == cur.Next[i] ? cache : this.GetNode(cur.Next[i]);

                // for(; <while_not_this>; <do_this>) { ... }
                for (; cur.Next[i].IsEmpty == false; cur = cache)
                {
                    // get cache for last node
                    cache = cache != null && cache.Position == cur.Next[i] ? cache : this.GetNode(cur.Next[i]);

                    // read next node to compare
                    var diff = cache.Key.CompareTo(key);

                    // if unique and diff = 0, throw index exception (must rollback transaction - others nodes can be dirty)
                    if (diff == 0 && index.Unique) throw LiteException.IndexDuplicateKey(index.Name, key);

                    if (diff == 1) break;
                }

                if (i <= (level - 1)) // level == length
                {
                    // cur = current (immediately before - prev)
                    // node = new inserted node
                    // next = next node (where cur is pointing)

                    //**curPage.IsDirty = true;

                    node.SetNext(i, cur.Next[i]);
                    node.SetPrev(i, cur.Next[i]);
                    cur.SetNext(i, node.Position);

                    //**node.Next[i] = cur.Next[i];
                    //**node.Prev[i] = cur.Position;
                    //**cur.Next[i] = node.Position;

                    var next = this.GetNode(node.Next[i]);

                    if (next != null)
                    {
                        node.SetPrev(i, node.Position);
                        //**next.Prev[i] = node.Position;
                        //**_snapshot.SetDirty(next.Page);
                    }
                }
            }

//**            // add/remove indexPage on freelist if has space
//**            _snapshot.AddOrRemoveToFreeList(page.FreeBytes > INDEX_RESERVED_BYTES, page, index.Page, ref index.FreeIndexPageID);

            // if last node exists, create a double link list
            if (last != null)
            {
                // link new node with last node
                if (last.NextNode.IsEmpty == false)
                {
                    // fix link pointer with has more nodes in list
                    var next = this.GetNode(last.NextNode);

                    next.SetPrevNode(node.Position);
                    last.SetNextNode(node.Position);
                    node.SetPrevNode(last.Position);
                    node.SetNextNode(next.Position);

                    //**next.PrevNode = node.Position;
                    //**last.NextNode = node.Position;
                    //**node.PrevNode = last.Position;
                    //**node.NextNode = next.Position;

                    //**_snapshot.SetDirty(next.Page);
                }
                else
                {
                    last.SetNextNode(node.Position);
                    node.SetPrevNode(last.Position);

                    //**last.NextNode = node.Position;
                    //**node.PrevNode = last.Position;
                }

                // set last node page as dirty
                //_snapshot.SetDirty(last.Page);
            }

            return node;
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
        /// </summary>
        public IndexNode GetNode(PageAddress address)
        {
            if (address.IsEmpty) return null;

            var page = _snapshot.GetPage<IndexPage>(address.PageID);

            return page.ReadNode(address.Index);
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
        /*
        /// <summary>
        /// Deletes an indexNode from a Index and adjust Next/Prev nodes
        /// </summary>
        public void Delete(CollectionIndex index, PageAddress nodeAddress)
        {
            var node = this.GetNode(nodeAddress);
            var page = node.Page;
            var isUniqueKey = false;

            // mark page as dirty here because, if deleted, page type will change
            _snapshot.SetDirty(page);

            for (int i = node.Prev.Length - 1; i >= 0; i--)
            {
                // get previous and next nodes (between my deleted node)
                var prev = this.GetNode(node.Prev[i]);
                var next = this.GetNode(node.Next[i]);

                if (prev != null)
                {
                    prev.Next[i] = node.Next[i];
                    _snapshot.SetDirty(prev.Page);
                }
                if (next != null)
                {
                    next.Prev[i] = node.Prev[i];
                    _snapshot.SetDirty(next.Page);
                }

                // in level 0, if any sibling are same value it's not an unique key
                if (i == 0)
                {
                    isUniqueKey = next?.Key == node.Key || prev?.Key == node.Key || false;
                }
            }

            page.DeleteNode(node);

            // if there is no more nodes in this page, delete them
            if (page.NodesCount == 0)
            {
                // first, remove from free list
                _snapshot.AddOrRemoveToFreeList(false, page, index.Page, ref index.FreeIndexPageID);

                _snapshot.DeletePage(page.PageID);
            }
            else
            {
                // add or remove page from free list
                _snapshot.AddOrRemoveToFreeList(page.FreeBytes > INDEX_RESERVED_BYTES, node.Page, index.Page, ref index.FreeIndexPageID);
            }

            // now remove node from nodelist 
            var prevNode = this.GetNode(node.PrevNode);
            var nextNode = this.GetNode(node.NextNode);

            if (prevNode != null)
            {
                prevNode.NextNode = node.NextNode;
                _snapshot.SetDirty(prevNode.Page);
            }
            if (nextNode != null)
            {
                nextNode.PrevNode = node.PrevNode;
                _snapshot.SetDirty(nextNode.Page);
            }
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
                var prevNode = this.GetNode(node.PrevNode);
                var nextNode = this.GetNode(node.NextNode);

                if (prevNode != null)
                {
                    prevNode.NextNode = node.NextNode;
                    _snapshot.SetDirty(prevNode.Page);
                }
                if (nextNode != null)
                {
                    nextNode.PrevNode = node.PrevNode;
                    _snapshot.SetDirty(nextNode.Page);
                }
            }

            // now delete all pages
            foreach (var pageID in pages)
            {
                _snapshot.DeletePage(pageID);
            }
        }*/

        #region Find
        /*
        /// <summary>
        /// Return all index nodes from an index
        /// </summary>
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
        /// Find first node that index match with value . 
        /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
        /// If not found but sibling = true, returns near node (only non-unique index)
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

                    // if equals, return index node
                    if (diff == 0)
                    {
                        return next;
                    }
                }
            }

            return null;
        }
        */
        #endregion
    }
}