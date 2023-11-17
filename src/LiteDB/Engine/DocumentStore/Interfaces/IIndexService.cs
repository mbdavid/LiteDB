namespace LiteDB.Engine;

internal interface IIndexService
{
    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    ValueTask<(RowID head, RowID tail)> CreateHeadTailNodesAsync(byte colID);

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    ValueTask<(IndexNodeResult result, bool defrag)> AddNodeAsync(byte colID, IndexDocument index, BsonValue key, RowID dataBlockID, IndexNodeResult last);

    /// <summary>
    /// Flip coin (skipped list): returns how many levels the node will have (starts in 1, max of INDEX_MAX_LEVELS)
    /// </summary>
    int Flip();

    /// <summary>
    /// Get a node/pageBuffer inside a page using RowID. IndexNodeID must be a valid position
    /// </summary>
    ValueTask<IndexNodeResult> GetNodeAsync(RowID indexNodeID);

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    ValueTask<IndexNodeResult> FindAsync(IndexDocument index, BsonValue key, bool sibling, int order);

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    ValueTask DeleteAllAsync(RowID pkIndexNodeID);

    ValueTask DropIndexAsync(int slot, RowID pkHeadIndexNodeID, RowID pkTailIndexNodeID);
}
