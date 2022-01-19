using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement a Index service - Add/Remove index nodes on SkipList
    /// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
    /// </summary>
    internal class IndexService
    {
        private static Random _rnd = new Random();

        private readonly Snapshot _snapshot;
        private readonly Collation _collation;

        public IndexService(Snapshot snapshot, Collation collation)
        {
            _snapshot = snapshot;
            _collation = collation;
        }

        public Collation Collation => _collation;

        /// <summary>
        /// Create a new index and returns head page address (skip list)
        /// </summary>
        public CollectionIndex CreateIndex(string name, string expr, bool unique)
        {
            // get how many bytes needed for each head/tail (both has same size)
            var bytesLength = IndexNode.GetNodeLength(MAX_LEVEL_LENGTH, BsonValue.MinValue, out var keyLength);

            // get a new empty page (each index contains its own linked nodes)
            var indexPage = _snapshot.NewPage<IndexPage>();

            // create index ref
            var index = _snapshot.CollectionPage.InsertCollectionIndex(name, expr, unique);

            // insert head/tail nodes
            var head = indexPage.InsertIndexNode(index.Slot, MAX_LEVEL_LENGTH, BsonValue.MinValue, PageAddress.Empty, bytesLength);
            var tail = indexPage.InsertIndexNode(index.Slot, MAX_LEVEL_LENGTH, BsonValue.MaxValue, PageAddress.Empty, bytesLength);

            // link head-to-tail with double link list in first level
            head.SetNext(0, tail.Position);
            tail.SetPrev(0, head.Position);

            // add this new page in free list (slot 0)
            index.FreeIndexPageList = indexPage.PageID;
            indexPage.PageListSlot = 0;

            index.Head = head.Position;
            index.Tail = tail.Position;

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

            // random level (flip coin mode) - return number between 1-32
            var level = this.Flip();

            // set index collection with max-index level
            if (level > index.MaxLevel)
            {
                // update max level
                _snapshot.CollectionPage.UpdateCollectionIndex(index.Name).MaxLevel = level;
            }

            // call AddNode with key value
            return this.AddNode(index, key, dataBlock, level, last);
        }

        /// <summary>
        /// Insert a new node index inside an collection index.
        /// </summary>
        private IndexNode AddNode(CollectionIndex index, BsonValue key, PageAddress dataBlock, byte level, IndexNode last)
        {
            // get a free index page for head note
            var bytesLength = IndexNode.GetNodeLength(level, key, out var keyLength);

            // test for index key maxlength
            if (keyLength > MAX_INDEX_KEY_LENGTH) throw LiteException.InvalidIndexKey($"Index key must be less than {MAX_INDEX_KEY_LENGTH} bytes.");

            var indexPage = _snapshot.GetFreeIndexPage(bytesLength, ref index.FreeIndexPageList);

            // create node in buffer
            var node = indexPage.InsertIndexNode(index.Slot, level, key, dataBlock, bytesLength);

            // now, let's link my index node on right place
            var cur = this.GetNode(index.Head);

            // using as cache last
            IndexNode cache = null;

            // scan from top left
            for (int i = index.MaxLevel - 1; i >= 0; i--)
            {
                // get cache for last node
                cache = cache != null && cache.Position == cur.Next[i] ? cache : this.GetNode(cur.Next[i]);

                // for(; <while_not_this>; <do_this>) { ... }
                for (; cur.Next[i].IsEmpty == false; cur = cache)
                {
                    // get cache for last node
                    cache = cache != null && cache.Position == cur.Next[i] ? cache : this.GetNode(cur.Next[i]);

                    // read next node to compare
                    var diff = cache.Key.CompareTo(key, _collation);

                    // if unique and diff = 0, throw index exception (must rollback transaction - others nodes can be dirty)
                    if (diff == 0 && index.Unique) throw LiteException.IndexDuplicateKey(index.Name, key);

                    if (diff == 1) break;
                }

                if (i <= (level - 1)) // level == length
                {
                    // cur = current (immediately before - prev)
                    // node = new inserted node
                    // next = next node (where cur is pointing)

                    node.SetNext((byte)i, cur.Next[i]);
                    node.SetPrev((byte)i, cur.Position);
                    cur.SetNext((byte)i, node.Position);

                    var next = this.GetNode(node.Next[i]);

                    if (next != null)
                    {
                        next.SetPrev((byte)i, node.Position);
                    }
                }
            }

            // if last node exists, create a double link list
            if (last != null)
            {
                ENSURE(last.NextNode == PageAddress.Empty, "last index node must point to null");

                // reload 'last' index node in case the IndexPage has gone through a defrag
                last = this.GetNode(last.Position);
                last.SetNextNode(node.Position);
            }

            // fix page position in free list slot
            _snapshot.AddOrRemoveFreeIndexList(node.Page, ref index.FreeIndexPageList);

            return node;
        }


        /// <summary>
        /// Flip coin - skip list - returns level node (start in 1)
        /// </summary>
        public byte Flip()
        {
            byte level = 1;

            for (int R = _rnd.Next(); (R & 1) == 1; R >>= 1)
            {
                level++;
                if (level == MAX_LEVEL_LENGTH) break;
            }

            return level;
        }

        /// <summary>
        /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
        /// </summary>
        public IndexNode GetNode(PageAddress address)
        {
            if (address.PageID == uint.MaxValue) return null;

            var indexPage = _snapshot.GetPage<IndexPage>(address.PageID);

            return indexPage.GetIndexNode(address.Index);
        }

        /// <summary>
        /// Gets all node list from passed nodeAddress (forward only)
        /// </summary>
        public IEnumerable<IndexNode> GetNodeList(PageAddress nodeAddress)
        {
            var node = this.GetNode(nodeAddress);

            while (node != null)
            {
                yield return node;

                node = this.GetNode(node.NextNode);
            }
        }

        /// <summary>
        /// Deletes all indexes nodes from pkNode
        /// </summary>
        public void DeleteAll(PageAddress pkAddress)
        {
            var node = this.GetNode(pkAddress);
            var indexes = _snapshot.CollectionPage.GetCollectionIndexesSlots();

            while (node != null)
            {
                this.DeleteSingleNode(node, indexes[node.Slot]);

                // move to next node
                node = this.GetNode(node.NextNode);
            }
        }

        /// <summary>
        /// Deletes all list of nodes in toDelete - fix single linked-list and return last non-delete node
        /// </summary>
        public IndexNode DeleteList(PageAddress pkAddress, HashSet<PageAddress> toDelete)
        {
            var last = this.GetNode(pkAddress);
            var node = this.GetNode(last.NextNode); // starts in first node after PK
            var indexes = _snapshot.CollectionPage.GetCollectionIndexesSlots();

            while (node != null)
            {
                if (toDelete.Contains(node.Position))
                {
                    this.DeleteSingleNode(node, indexes[node.Slot]);

                    // fix single-linked list from last non-delete delete
                    last.SetNextNode(node.NextNode);
                }
                else
                {
                    // last non-delete node to set "NextNode"
                    last = node;
                }

                // move to next node
                node = this.GetNode(node.NextNode);
            }

            return last;
        }

        /// <summary>
        /// Delete a single index node - fix tree double-linked list levels
        /// </summary>
        private void DeleteSingleNode(IndexNode node, CollectionIndex index)
        {
            for (int i = node.Level - 1; i >= 0; i--)
            {
                // get previous and next nodes (between my deleted node)
                var prevNode = this.GetNode(node.Prev[i]);
                var nextNode = this.GetNode(node.Next[i]);

                if (prevNode != null)
                {
                    prevNode.SetNext((byte)i, node.Next[i]);
                }
                if (nextNode != null)
                {
                    nextNode.SetPrev((byte)i, node.Prev[i]);
                }
            }

            node.Page.DeleteIndexNode(node.Position.Index);

            _snapshot.AddOrRemoveFreeIndexList(node.Page, ref index.FreeIndexPageList);
        }

        /// <summary>
        /// Delete all index nodes from a specific collection index. Scan over all PK nodes, read all nodes list and remove
        /// </summary>
        public void DropIndex(CollectionIndex index)
        {
            var slot = index.Slot;
            var pkIndex = _snapshot.CollectionPage.PK;

            foreach(var pkNode in this.FindAll(pkIndex, Query.Ascending))
            {
                var next = pkNode.NextNode;
                var last = pkNode;

                while (next != PageAddress.Empty)
                {
                    var node = this.GetNode(next);

                    if (node.Slot == slot)
                    {
                        // delete node from page (mark as dirty)
                        node.Page.DeleteIndexNode(node.Position.Index);

                        last.SetNextNode(node.NextNode);
                    }
                    else
                    {
                        last = node;
                    }

                    next = node.NextNode;
                }
            }

            // removing head/tail index nodes
            this.GetNode(index.Head).Page.DeleteIndexNode(index.Head.Index);
            this.GetNode(index.Tail).Page.DeleteIndexNode(index.Tail.Index);
        }

        #region Find
        
        /// <summary>
        /// Return all index nodes from an index
        /// </summary>
        public IEnumerable<IndexNode> FindAll(CollectionIndex index, int order)
        {
            var cur = order == Query.Ascending ? this.GetNode(index.Head) : this.GetNode(index.Tail);

            while (!cur.GetNextPrev(0, order).IsEmpty)
            {
                cur = this.GetNode(cur.GetNextPrev(0, order));

                // stop if node is head/tail
                if (cur.Key.IsMinValue || cur.Key.IsMaxValue) yield break;

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
            var cur = order == Query.Ascending ? this.GetNode(index.Head) : this.GetNode(index.Tail);

            for (int i = index.MaxLevel - 1; i >= 0; i--)
            {
                for (; cur.GetNextPrev((byte)i, order).IsEmpty == false; cur = this.GetNode(cur.GetNextPrev((byte)i, order)))
                {
                    var next = this.GetNode(cur.GetNextPrev((byte)i, order));
                    var diff = next.Key.CompareTo(value, _collation);

                    if (diff == order && (i > 0 || !sibling)) break;
                    if (diff == order && i == 0 && sibling)
                    {
                        // is head/tail?
                        return (next.Key.IsMinValue || next.Key.IsMaxValue) ? null : next;
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

        #endregion
    }
}