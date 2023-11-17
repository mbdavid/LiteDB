namespace LiteDB.Engine;

/// <summary>
/// Implement a Index service - Add/Remove index nodes on SkipList
/// Based on: http://igoro.com/archive/skip-lists-are-fascinating/
/// </summary>
internal class IndexService : IIndexService
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

    #region NewIndex

    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    public async ValueTask<(RowID head, RowID tail)> CreateHeadTailNodesAsync(byte colID)
    {
        // get how many bytes needed for each head/tail (both has same size)
        var bytesLength = (ushort)IndexNode.GetSize(INDEX_MAX_LEVELS, BsonValue.MinValue);

        // get a index page for this collection
        var page = await _transaction.GetFreeIndexPageAsync(colID, bytesLength * 2);

        // add head/tail nodes into page
        var head = PageMemory.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MinValue, RowID.Empty, out _, out var newPageValue);
        var tail = PageMemory.InsertIndexNode(page, 0, INDEX_MAX_LEVELS, BsonValue.MaxValue, RowID.Empty, out _, out _);

        // link head-to-tail with double link list in first level
        head.SetNextID(0, tail.IndexNodeID);
        tail.SetPrevID(0, head.IndexNodeID);

        // update allocation map if needed
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(page.PageID, newPageValue);
        }

        return (head.IndexNodeID, tail.IndexNodeID);
    }

    #endregion

    #region AddNode

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    public ValueTask<(IndexNodeResult result, bool defrag)> AddNodeAsync(
        byte colID, 
        IndexDocument index, 
        BsonValue key, 
        RowID dataBlockID, 
        IndexNodeResult last)
    {
        using var _pc = PERF_COUNTER(60, nameof(AddNodeAsync), nameof(IndexService));

        // random level (flip coin mode) - return number between 0-31
        var levels = this.Flip();

        // call AddNode with key value
        return this.AddNodeInternalAsync(colID, index, key, dataBlockID, levels, last);
    }

    /// <summary>
    /// Insert a new node index inside an collection index.
    /// </summary>
    private async ValueTask<(IndexNodeResult result, bool defrag)> AddNodeInternalAsync(
        byte colID, 
        IndexDocument index, 
        BsonValue key, 
        RowID dataBlockID, 
        int insertLevels, 
        IndexNodeResult last)
    {
        // get a free index page for head note
        var bytesLength = IndexNode.GetSize(insertLevels, key);

        // get an index page with avaliable space to add this node
        var page = await _transaction.GetFreeIndexPageAsync(colID, bytesLength);

        // create node in page
        var node = PageMemory.InsertIndexNode(page, index.Slot, (byte)insertLevels, key, dataBlockID, out var defrag, out var newPageValue);

        // update allocation map if needed (this page has no more "size" changes)
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(page.PageID, newPageValue);
        }

        // now, let's link my index node on right place
        var leftNode = await this.GetNodeAsync(index.HeadIndexNodeID);

        // for: scan from top to bottom
        for (int currentLevel = INDEX_MAX_LEVELS - 1; currentLevel >= 0; currentLevel--)
        {
            var right = leftNode.GetNextID(currentLevel);

            // while: scan from left to right
            while (right.IsEmpty == false && right != index.TailIndexNodeID)
            {
                var rightNode = await this.GetNodeAsync(right);

                // read next node to compare
                var diff = rightNode.KeyCompareTo(key, _collation);

                if (diff == 0 && index.Unique) throw ERR("IndexDuplicateKey(index.Name, key)");

                if (diff == 1) break; // stop going right

                leftNode = rightNode;
                right = rightNode.GetNextID(currentLevel);
            }

            if (currentLevel <= insertLevels - 1) // level == length
            {
                // prev: immediately before new node
                // node: new inserted node
                // next: right node from prev (where left is pointing)
                var prev = leftNode.IndexNodeID;
                var next = leftNode.GetNextID(currentLevel);

                // if next is empty, use tail (last key)
                if (next.IsEmpty) next = index.TailIndexNodeID;

                // set new node pointer links with current level sibling
                node.SetNextID(currentLevel, next);
                node.SetPrevID(currentLevel, prev);

                // fix sibling pointer to new node
                leftNode.SetNextID(currentLevel, node.IndexNodeID);
                leftNode.Page.IsDirty = true;

                right = node.GetNextID(currentLevel);

                var rightNode = await this.GetNodeAsync(right);

                // mark right page as dirty (after change PrevID)
                rightNode.SetPrevID(currentLevel, node.IndexNodeID);
                rightNode.Page.IsDirty = true;
            }
        }

        // if last node exists, create a single link list between node list
        if (!last.IsEmpty)
        {
            // set last node to link with current node
            last.NextNodeID = node.IndexNodeID;
            last.Page.IsDirty = true;
        }

        return (node, defrag);
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

    #endregion

    #region GetNode/Find

    /// <summary>
    /// Get a node/pageBuffer inside a page using RowID. IndexNodeID must be a valid position
    /// </summary>
    public async ValueTask<IndexNodeResult> GetNodeAsync(RowID indexNodeID)
    {
        using var _pc = PERF_COUNTER(70, nameof(GetNodeAsync), nameof(IndexService));

        ENSURE(!indexNodeID.IsEmpty);
        ENSURE(!(indexNodeID.PageID == 0 && indexNodeID.Index == 0));

        var page = await _transaction.GetPageAsync(indexNodeID.PageID);

        ENSURE(page.PageType == PageType.Index, new { indexNodeID });

        var result = new IndexNodeResult(page, indexNodeID);

        return result;
    }

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    public async ValueTask<IndexNodeResult> FindAsync(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        var left = order == Query.Ascending ? index.HeadIndexNodeID : index.TailIndexNodeID;
        var leftNode = await this.GetNodeAsync(left);

        for (var level = INDEX_MAX_LEVELS - 1; level >= 0; level--)
        {
            var right = leftNode.GetNextID(level, order);

            while (right.IsEmpty == false)
            {
                var rightNode = await this.GetNodeAsync(right);

                var diff = rightNode.KeyCompareTo(key, _collation);

                if (diff == order && (level > 0 || !sibling)) break; // go down one level

                if (diff == order && level == 0 && sibling)
                {
                    // is head/tail?
                    return (rightNode.IsMinOrMaxValue) ? IndexNodeResult.Empty : rightNode;
                }

                // if equals, return index node
                if (diff == 0)
                {
                    return rightNode;
                }

                leftNode = rightNode;
                right = rightNode.GetNextID(level, order);
            }
        }

        return IndexNodeResult.Empty;
    }

    #endregion

    #region Delete

    /// <summary>
    /// Deletes all indexes nodes from pk RowID
    /// </summary>
    public async ValueTask DeleteAllAsync(RowID pkIndexNodeID)
    {
         await this.DeleteAllAsync(await this.GetNodeAsync(pkIndexNodeID));
    }

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    private async ValueTask DeleteAllAsync(IndexNodeResult pkNodeResult)
    {
        // get a copy before change
        var node = pkNodeResult;

        // all indexes nodes from a document are connected by nextNode
        while (!node.IsEmpty)
        {
            // keep result before delete
            var nextNodeID = node.NextNodeID;

            await this.DeleteSingleNodeAsync(node);

            if (nextNodeID.IsEmpty) break;

            // move to next node
            node = await this.GetNodeAsync(nextNodeID);
        }
    }

    /// <summary>
    /// Delete a single node fixing all next/prev levels pointers
    /// </summary>
    private async ValueTask DeleteSingleNodeAsync(IndexNodeResult node)
    {
        // run over all levels linking prev with next
        for (int i = node.Levels; i >= 0; i--)
        {
            var prevID = node.GetPrevID(i);
            var nextID = node.GetNextID(i);

            // get previous and next nodes (between my deleted node)
            var prev = await this.GetNodeAsync(prevID);
            var next = await this.GetNodeAsync(nextID);

            if (!prev.IsEmpty)
            {
                prev.SetNextID(i, node.GetNextID(i));
                prev.Page.IsDirty = true;
            }

            if (!next.IsEmpty)
            {
                next.SetPrevID(i, node.GetPrevID(i));
                next.Page.IsDirty = true;
            }
        }

        // delete node segment in page (set IsDirtry = true)
        PageMemory.DeleteSegment(node.Page.Ptr, node.IndexNodeID.Index, out var newPageValue);

        // update map page only if change page value
        if (newPageValue != ExtendPageValue.NoChange)
        {
            _transaction.UpdatePageMap(node.PageID, newPageValue);
        }
    }

    #endregion

    #region DropIndex

    /// <summary>
    /// </summary>
    public async ValueTask DropIndexAsync(int slot, RowID pkHeadIndexNodeID, RowID pkTailIndexNodeID)
    {
        // init from first node after head
        var pkNode = await this.GetNodeAsync(pkHeadIndexNodeID);

        var pkRowID = pkNode.GetNextID(0);

        // loop over all pk index nodes
        while (pkRowID.IsEmpty == false && pkRowID != pkTailIndexNodeID)
        {
            pkNode = await this.GetNodeAsync(pkRowID);

            var last = pkNode;
            var nextNodeID = last.NextNodeID;

            while (nextNodeID.IsEmpty == false)
            {
                var node = await this.GetNodeAsync(nextNodeID);

                // skip if not same slot
                if (node.Slot != slot)
                {
                    nextNodeID = node.NextNodeID;
                    last = node;
                    continue;
                }

                // fix last index node pointer
                last.NextNodeID = node.NextNodeID;
                last.Page.IsDirty = true;

                // keep nextNodeID 
                nextNodeID = node.NextNodeID;

                // delete node
                PageMemory.DeleteSegment(node.Page.Ptr, node.IndexNodeID.Index, out var pageValue);

                if (pageValue != ExtendPageValue.NoChange)
                {
                    _transaction.UpdatePageMap(node.Page.PageID, pageValue);
                }
            }

            pkRowID = pkNode.GetNextID(0);
        }
    }

    #endregion
}