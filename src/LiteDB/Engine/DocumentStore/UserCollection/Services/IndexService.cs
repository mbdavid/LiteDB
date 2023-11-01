namespace LiteDB.Engine;

/// <summary>
/// Implement a Index service - Add/Remove index nodes on SkipList
/// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
/// </summary>
unsafe internal class IndexService : IIndexService
{
    // dependency injection
    private readonly ITransaction _transaction;
    private readonly Collation _collation;

    public IndexService(
        Collation collation,
        ITransaction transaction)
    {
        _collation = collation;
        _transaction = transaction;
    }

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public (RowID head, RowID tail) CreateHeadTailNodes(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetSize(INDEX_MAX_LEVELS, BsonValue.MinValue);

        // get a index page for this collection
        var page = _transaction.GetFreeIndexPage(colID, bytesLength * 2);

        // get initial pageExtend value
        var before = page->ExtendPageValue;

        // add head/tail nodes into page
        var head = PageMemory.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MinValue, RowID.Empty, out _, out var newPageValue);
        var tail = PageMemory.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MaxValue, RowID.Empty, out _, out _);

        // link head-to-tail with double link list in first level
        head[0]->NextID = tail.IndexNodeID;
        tail[0]->PrevID = head.IndexNodeID;

        // update allocation map if needed
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(page->PageID, newPageValue);
        }

        return (head.IndexNodeID, tail.IndexNodeID);
    }

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    public IndexNodeResult AddNode(byte colID, IndexDocument index, BsonValue key, RowID dataBlockID, IndexNodeResult last, out bool defrag)
    {
        using var _pc = PERF_COUNTER(60, nameof(AddNode), nameof(IndexService));

        // random level (flip coin mode) - return number between 0-31
        var levels = this.Flip();

        // call AddNode with key value
        return this.AddNodeInternal(colID, index, key, dataBlockID, levels, last, out defrag);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private IndexNodeResult AddNodeInternal(
        byte colID, 
        IndexDocument index, 
        BsonValue key, 
        RowID dataBlockID, 
        int insertLevels, 
        IndexNodeResult last, 
        out bool defrag)
    {
        // get a free index page for head note
        var bytesLength = IndexNode.GetSize(insertLevels, key);

        // get an index page with avaliable space to add this node
        var page = _transaction.GetFreeIndexPage(colID, bytesLength);

        // get initial pageValue
        var before = page->ExtendPageValue;

        // create node in page
        var node = PageMemory.InsertIndexNode(page, index.Slot, (byte)insertLevels, key, dataBlockID, out defrag, out var newPageValue);

        // update allocation map if needed (this page has no more "size" changes)
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(page->PageID, newPageValue);
        }

        // now, let's link my index node on right place
        var leftNode = this.GetNode(index.HeadIndexNodeID);

        // for: scan from top to bottom
        for (int currentLevel = INDEX_MAX_LEVELS - 1; currentLevel >= 0; currentLevel--)
        {
            var right = leftNode[currentLevel]->NextID;

            // while: scan from left to right
            while (right.IsEmpty == false && right != index.TailIndexNodeID)
            {
                var rightNode = this.GetNode(right);

                // read next node to compare
                //***var diff = rightNode.Node.Key.CompareTo(key, _collation);
                var diff = IndexKey.Compare(rightNode.Key, key, _collation);

                //***if unique and diff == 0, throw index exception (must rollback transaction - others nodes can be dirty)
                if (diff == 0 && index.Unique) throw ERR("IndexDuplicateKey(index.Name, key)");

                if (diff == 1) break; // stop going right

                leftNode = rightNode;
                //***right = rightNode.Node.Next[currentLevel];
                right = rightNode[currentLevel]->NextID;
            }

            if (currentLevel <= insertLevels - 1) // level == length
            {
                // prev: immediately before new node
                // node: new inserted node
                // next: right node from prev (where left is pointing)

                //***var prev = leftNode.Node.IndexNodeID;
                //***var next = leftNode.Node.Next[currentLevel];
                var prev = leftNode.IndexNodeID;
                var next = leftNode[currentLevel]->NextID;

                // if next is empty, use tail (last key)
                if (next.IsEmpty) next = index.TailIndexNodeID;

                // set new node pointer links with current level sibling
                //***node.SetNext(page, currentLevel, next);
                //***node.SetPrev(page, currentLevel, prev);
                node[currentLevel]->NextID = next;
                node[currentLevel]->PrevID = prev;

                // fix sibling pointer to new node
                //***leftNode.Node.SetNext(leftNode.Page, currentLevel, node.IndexNodeID);
                leftNode[currentLevel]->NextID = node.IndexNodeID;
                leftNode.Page->IsDirty = true;

                //***right = node.Next[currentLevel];
                right = node[currentLevel]->NextID;

                //***var rightNode = await this.GetNodeAsync(right);
                //***rightNode.Node.SetPrev(rightNode.Page, currentLevel, node.IndexNodeID);
                var rightNode = this.GetNode(right);

                // mark right page as dirty (after change PrevID)
                rightNode[currentLevel]->PrevID = node.IndexNodeID;
                rightNode.Page->IsDirty = true;
            }

        }

        // if last node exists, create a single link list between node list
        if (!last.IsEmpty)
        {
            // set last node to link with current node
            //***last.Node.SetNextNodeID(last.Page, node.IndexNodeID);
            last.Node->NextNodeID = node.IndexNodeID;
            last.Page->IsDirty = true;
        }

        return node;
    }

    /// <summary>
    /// Flip coin (skipped list): returns how many levels the node will have (starts in 1, max of INDEX_MAX_LEVELS)
    /// </summary>
    public int Flip()
    {
        byte levels = 1;

        for (int R = Randomizer.Next(); (R & 1) == 1; R >>= 1)
        {
            levels++;
            if (levels == INDEX_MAX_LEVELS) break;
        }

        return levels;
    }

    /// <summary>
    /// Get a node/pageBuffer inside a page using RowID. IndexNodeID must be a valid position
    /// </summary>
    public IndexNodeResult GetNode(RowID indexNodeID)
    {
        using var _pc = PERF_COUNTER(70, nameof(GetNode), nameof(IndexService));

        ENSURE(!indexNodeID.IsEmpty);
        ENSURE(!(indexNodeID.PageID ==0 && indexNodeID.Index == 0));

        var page = _transaction.GetPage(indexNodeID.PageID);

        ENSURE(page->PageType == PageType.Index, new { indexNodeID });

        var result = new IndexNodeResult(page, indexNodeID);

        return result;
    }

    #region Find

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    public IndexNodeResult Find(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        var left = order == Query.Ascending ? index.HeadIndexNodeID : index.TailIndexNodeID;
        var leftNode = this.GetNode(left);

        for (var level = INDEX_MAX_LEVELS - 1; level >= 0; level--)
        {
            //***var right = leftNode.Node.GetNextPrev(level, order);
            var right = leftNode[level]->GetNext(order);

            while (right.IsEmpty == false)
            {
                var rightNode = this.GetNode(right);

                //var diff = rightNode.Node.Key.CompareTo(key, _collation);
                var diff = IndexKey.Compare(rightNode.Key, key, _collation);

                if (diff == order && (level > 0 || !sibling)) break; // go down one level

                if (diff == order && level == 0 && sibling)
                {
                    // is head/tail?
                    //***return (rightNode.Node.Key.IsMinValue || rightNode.Node.Key.IsMaxValue) ? __IndexNodeResult.Empty : rightNode;
                    return (rightNode.Key->IsMinValue || rightNode.Key->IsMaxValue) ? IndexNodeResult.Empty : rightNode;
                }

                // if equals, return index node
                if (diff == 0)
                {
                    return rightNode;
                }

                leftNode = rightNode;
                //***right = rightNode.Node.GetNextPrev(level, order);
                right = rightNode[level]->GetNext(order);
            }
        }

        return IndexNodeResult.Empty;
    }

    #endregion

    /// <summary>
    /// Deletes all indexes nodes from pk RowID
    /// </summary>
    public void DeleteAll(RowID indexNodeID)
    {
         this.GetNode(indexNodeID);
    }

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    public void DeleteAll(IndexNodeResult nodeResult)
    {
        // get a copy before change
        var node = nodeResult;

        // all indexes nodes from a document are connected by nextNode
        while (!node.IsEmpty)
        {
            // keep result before delete
            var nextNodeID = node.Node->NextNodeID;

            this.DeleteSingleNode(node);

            if (nextNodeID.IsEmpty) break;

            // move to next node
            node = this.GetNode(nextNodeID);
        }
    }

    /// <summary>
    /// Delete a single node fixing all next/prev levels pointers
    /// </summary>
    private void DeleteSingleNode(IndexNodeResult node)
    {
        // run over all levels linking prev with next
        for (int i = node.Node->Levels - 1; i >= 0; i--)
        {
            // get previous and next nodes (between my deleted node)
            //***var prevNode, prevPage) = this.GetNode(nodePtr.Prev[i]);
            //***var nextNode, nextPage) = this.GetNode(nodePtr.Next[i]);
            var prev = this.GetNode(node[i]->PrevID);
            var next = this.GetNode(node[i]->NextID);

            //***if (!prevNode.IsEmpty)
            //***{
            //***    prevNode.SetNext(prevPage, (byte)i, nodePtr.Next[i]);
            //***}
            //***
            //***if (!nextNode.IsEmpty)
            //***{
            //***    nextNode.SetPrev(nextPage, (byte)i, nodePtr.Prev[i]);
            //***}
            if (!prev.IsEmpty)
            {
                prev[i]->NextID = node[i]->NextID;
                prev.Page->IsDirty = true;
            }

            if (!next.IsEmpty)
            {
                next[i]->PrevID = node[i]->PrevID;
                next.Page->IsDirty = true;
            }
        }

        // delete node segment in page (set IsDirtry = true)
        PageMemory.DeleteSegment(node.Page, node.IndexNodeID.Index, out var newPageValue);

        // update map page only if change page value
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(node.Page->PageID, newPageValue);
        }
    }
}