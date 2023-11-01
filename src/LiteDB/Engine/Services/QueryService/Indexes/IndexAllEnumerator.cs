namespace LiteDB.Engine;

internal class IndexAllEnumerator : IPipeEnumerator
{
    private readonly IndexDocument _indexDocument;
    private readonly int _order;
    private readonly bool _returnKey;

    private bool _init = false;
    private bool _eof = false;

    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexAllEnumerator(
        IndexDocument indexDocument, 
        int order,
        bool returnKey)
    {
        _indexDocument = indexDocument;
        _order = order;
        _returnKey = returnKey;
    }

    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, value: _returnKey);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var first = indexService.GetNode(head);

            // get pointer to first element 
            _next = first[0]->GetNext(_order);

            // check if not empty
            if (_next == tail)
            {
                _eof = true;
                return PipeValue.Empty;
            }
        }

        // go forward
        var node = indexService.GetNode(_next);

        _next = node[0]->GetNext(_order);

        if (_next == tail) _eof = true;

        var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

        return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"INDEX FULL SCAN \"{_indexDocument.Name}\" {(_order > 0 ? "ASC" : "DESC")}", deep);
    }

    public void Dispose()
    {
    }
}
