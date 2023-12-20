//namespace LiteDB.Engine;

//internal class IndexVirtualEnumerator : IPipeEnumerator
//{
//    private readonly  _store;
//    private readonly int _order;

//    private bool _init = false;
//    private bool _eof = false;

//    private RowID _next = RowID.Empty; // all nodes from right of first node found

//    public IndexVirtualEnumerator(
//        IDocumentStore store,
//        int order)
//    {
//        _store = store;
//        _order = order;
//    }

//    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, value: true);

//    public unsafe PipeValue MoveNext(PipeContext context)
//    {
//        if (_eof) return PipeValue.Empty;

//        var indexService = context.IndexService;



//        // in first run, gets head node
//        if (_init == false)
//        {
//            _init = true;

//            var first = indexService.GetNode(head);

//            // get pointer to first element 
//            _next = first[0]->GetNext(_order);

//            // check if not empty
//            if (_next == tail)
//            {
//                _eof = true;
//                return PipeValue.Empty;
//            }
//        }

//        // go forward
//        var node = indexService.GetNode(_next);

//        _next = node[0]->GetNext(_order);

//        if (_next == tail) _eof = true;

//        var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

//        return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
//    }

//    public void GetPlan(ExplainPlainBuilder builder, int deep)
//    {
//        builder.Add($"INDEX FULL SCAN \"{_indexDocument.Name}\" {(_order > 0 ? "ASC" : "DESC")}", deep);
//    }

//    public void Dispose()
//    {
//    }
//}
