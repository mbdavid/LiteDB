namespace LiteDB.Engine;

internal class SystemCollectionIndexService : IIndexService
{
    public (RowID head, RowID tail) CreateHeadTailNodes(byte colID) => throw new NotSupportedException();

    public IndexNodeResult AddNode(byte colID, IndexDocument index, BsonValue key, RowID dataBlockID, IndexNodeResult last, out bool defrag) => throw new NotSupportedException();

    public void DeleteAll(RowID pkIndexNodeID) 
    {
        throw new NotImplementedException();
    }

    public void DropIndex(int slot, RowID pkHeadIndexNodeID)
    {
        throw new NotImplementedException();
    }

    public IndexNodeResult Find(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        throw new NotImplementedException();
    }

    public IndexNodeResult GetNode(RowID indexNodeID)
    {
        throw new NotImplementedException();
    }
}
