namespace LiteDB.Engine;

unsafe internal class IndexEqualsEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;
    private readonly bool _returnKey;

    private bool _init = false;
    private bool _eof = false;

    private RowID _prev = RowID.Empty; // all nodes from left of first node found
    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexEqualsEnumerator(
        BsonValue value, 
        IndexDocument indexDocument, 
        Collation collation, 
        bool returnKey)
    {
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
        _returnKey = returnKey;
    }

    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, value: _returnKey);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        // no _order here
        var head = _indexDocument.HeadIndexNodeID;
        var tail = _indexDocument.TailIndexNodeID;

        // in first run, look for index node
        if (_init == false)
        {
            _init = true;

            var node = indexService.Find(_indexDocument, _value, false, Query.Ascending);

            // if node was not found, end enumerator
            if (node.IsEmpty)
            {
                _eof = true;
                return PipeValue.Empty;
            }

            // if unique index, set _eof = true to do not run more than once
            if (_indexDocument.Unique)
            {
                _eof = true;
            }
            else
            {
                // get pointer to next/prev at level 0
                _prev = node[0]->PrevID;
                _next = node[0]->NextID;

                // check for head/tail nodes to set as empty
                if (_prev == head) _prev = RowID.Empty;
                if (_next == head) _next = RowID.Empty;
            }

            var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

            // current node to return
            return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
        }

        // first, go backward
        if (_prev.IsEmpty == false)
        {
            // do not test head node
            var node = indexService.GetNode(_prev);

            var isEqual = IndexKey.Compare(_value, node.Key, _collation) == 0;

            if (isEqual)
            {
                _prev = node[0]->PrevID;

                if (_prev == head) _prev = RowID.Empty;

                var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

                return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
            }
            else
            {
                _prev = RowID.Empty;
            }
        }

        // and than go forward
        if (_next.IsEmpty == false)
        {
            var node = indexService.GetNode(_next);

            //var isEqual = _collation.Equals(_value, node.Key);
            var isEqual = IndexKey.Compare(_value, node.Key, _collation) == 0;

            if (isEqual)
            {
                _next = node[0]->NextID;

                if (_next == tail) _eof = true;

                var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

                return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
            }
            else
            {
                _eof = true;
            }
        }

        return PipeValue.Empty;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"INDEX SEEK \"{_indexDocument.Name}\" ({_indexDocument.Expression} = {_value}){(_indexDocument.Unique ? " UNIQUE" : "")}", deep);
    }

    public void Dispose()
    {
    }
}
