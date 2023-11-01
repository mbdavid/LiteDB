namespace LiteDB.Engine;

internal interface IIndexService
{
    /// <summary>
    /// Create head and tail nodes for a new index
    /// </summary>
    (RowID head, RowID tail) CreateHeadTailNodes(byte colID);

    /// <summary>
    /// Insert a new node index inside an collection index. Flip coin to know level
    /// </summary>
    IndexNodeResult AddNode(byte colID, IndexDocument index, BsonValue key, RowID dataBlockID, IndexNodeResult last, out bool defrag);

    /// <summary>
    /// Flip coin (skipped list): returns how many levels the node will have (starts in 1, max of INDEX_MAX_LEVELS)
    /// </summary>
    int Flip();

    /// <summary>
    /// Get a node/pageBuffer inside a page using RowID. IndexNodeID must be a valid position
    /// </summary>
    IndexNodeResult GetNode(RowID indexNodeID);

    /// <summary>
    /// Find first node that index match with value . 
    /// If index are unique, return unique value - if index are not unique, return first found (can start, middle or end)
    /// If not found but sibling = true and key are not found, returns next value index node (if order = Asc) or prev node (if order = Desc)
    /// </summary>
    IndexNodeResult Find(IndexDocument index, BsonValue key, bool sibling, int order);

    /// <summary>
    /// Deletes all indexes nodes from pkNode
    /// </summary>
    void DeleteAll(IndexNodeResult nodeResult);
}
