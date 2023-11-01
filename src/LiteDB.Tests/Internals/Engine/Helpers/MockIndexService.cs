namespace LiteDB.Tests.Internals.Engine;

internal class MockIndexService : IIndexService
{
    private List<(RowID indexNodeID, BsonValue key, RowID dataBlockID, RowID prev, RowID next)> _values = new()
    {
        new (new RowID(0, 0), BsonValue.MinValue, RowID.Empty, RowID.Empty, new RowID(2, 0)),

        new (new RowID(1, 0), 245, new RowID(1001, 0), new RowID(5, 0), new RowID(4, 0)),
        new (new RowID(2, 0), 12, new RowID(1002, 0), new RowID(0, 0), new RowID(5, 0)),
        new (new RowID(3, 0), 1024, new RowID(1003, 0), new RowID(4, 0), new RowID(999, 0)),
        new (new RowID(4, 0), 256, new RowID(1004, 0), new RowID(1, 0), new RowID(3, 0)),
        new (new RowID(5, 0), 36, new RowID(1005, 0), new RowID(2, 0), new RowID(1, 0)),


        new (new RowID(999, 0), BsonValue.MaxValue, RowID.Empty, new RowID(), RowID.Empty)
    };

    //private readonly PageBuffer _page = new PageBuffer(0);

    //public ValueTask<IndexNodeResult> FindAsync(IndexDocument index, BsonValue key, bool sibling, int order)
    //{
    //    var data = _values.FirstOrDefault(x => x.key == key);

    //    var node = new IndexNode(_page, data.indexNodeID, 0, 1, data.key, data.dataBlockID);

    //    node.SetNext(_page, 0, data.next);
    //    node.SetPrev(_page, 0, data.prev);

    //    var result = new IndexNodeResult(node, _page);

    //    return new ValueTask<IndexNodeResult>(result);
    //}

    //public ValueTask<IndexNodeResult> GetNodeAsync(RowID indexNodeID)
    //{
    //    var data = _values.First(x => x.indexNodeID == indexNodeID);

    //    var node = new IndexNode(_page, data.indexNodeID, 0, 1, data.key, data.dataBlockID);

    //    node.SetNext(_page, 0, data.next);
    //    node.SetPrev(_page, 0, data.prev);

    //    var result = new IndexNodeResult(node, _page);

    //    return new ValueTask<IndexNodeResult>(result);
    //}

    public (RowID head, RowID tail) CreateHeadTailNodes(byte colID)
    {
        throw new NotImplementedException();
    }

    public IndexNodeResult AddNode(byte colID, IndexDocument index, BsonValue key, RowID dataBlockID, IndexNodeResult last, out bool defrag)
    {
        throw new NotImplementedException();
    }

    public int Flip()
    {
        throw new NotImplementedException();
    }

    public IndexNodeResult GetNode(RowID indexNodeID)
    {
        throw new NotImplementedException();
    }

    public IndexNodeResult Find(IndexDocument index, BsonValue key, bool sibling, int order)
    {
        throw new NotImplementedException();
    }

    public void DeleteAll(IndexNodeResult nodeResult)
    {
        throw new NotImplementedException();
    }
}