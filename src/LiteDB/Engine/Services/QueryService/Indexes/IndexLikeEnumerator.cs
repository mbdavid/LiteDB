namespace LiteDB.Engine;

internal class IndexLikeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _value;
    private readonly string _startsWith;
    private readonly bool _hasMore;
    private readonly int _order;
    private readonly bool _returnKey;

    private bool _init = false;
    private bool _eof = false;

    private Stack<(RowID indexNodeID, RowID dataBlockID, BsonValue key)> _prev = new(); // use a stack to keep order output
    private RowID _next = RowID.Empty;

    public IndexLikeEnumerator(
        BsonValue value,
        IndexDocument indexDocument,
        Collation collation,
        int order,
        bool returnKey)
    {
        _startsWith = value.AsString.SqlLikeStartsWith(out _hasMore);
        _value = value;
        _indexDocument = indexDocument;
        _collation = collation;
        _order = order;
        _returnKey = returnKey;
    }

    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, value: _returnKey);

    public ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return new ValueTask<PipeValue>(PipeValue.Empty);

        return _startsWith.Length > 0 ? this.ExecuteLikeAsync(context) : this.ExecuteFullScanAsync(context);
    }

    private async ValueTask<PipeValue> ExecuteLikeAsync(PipeContext context)
    {
        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, look for index node
        if (_init == false)
        {
            _init = true;

            var node = await indexService.FindAsync(_indexDocument, _startsWith, true, Query.Ascending);

            // if node was not found, end enumerator
            if (node.IsEmpty || !node.IsStringValue) return this.Finish();

            // get start prev (left side)
            var prevID = node.IndexNodeID;

            // get next index node
            _next = node.GetNextID(0, _order);

            // add all prev items into _prevs
            while (true)
            {
                var nodePrev = await indexService.GetNodeAsync(prevID);

                if (!nodePrev.IsStringValue) break;

                var keyPrev = nodePrev.ToBsonValue().AsString;

                // test if match initial startsWith
                if (!_collation.StartsWith(keyPrev, _startsWith)) break;

                if (_hasMore == false || keyPrev.SqlLike(_value, _collation))
                {
                    var value = _returnKey ? nodePrev.ToBsonValue() : BsonValue.Null;

                    // push current value
                    _prev.Push(new(nodePrev.IndexNodeID, nodePrev.DataBlockID, value));
                }

                prevID = nodePrev.GetPrevID(0, _order);
            }
        }

        // pop all prev values in order
        if (_prev.TryPop(out var nodePop))
        {
            return new PipeValue(nodePop.indexNodeID, nodePop.dataBlockID, nodePop.key);
        }

        while (!_eof)
        {
            // if _next if head, no more to go
            if (_next == head) return this.Finish();

            // get nextNode and test if match
            var nodeNext = await indexService.GetNodeAsync(_next);

            // set for next
            _next = nodeNext.GetNextID(0, _order);

            if (_next == tail) _eof = true;

            // if not string, finish
            if (!nodeNext.IsStringValue) return this.Finish();

            //get nextKey as string
            var keyNext = nodeNext.ToBsonValue().AsString;

            // test if match initial startsWith
            if (!_collation.StartsWith(keyNext, _startsWith)) break;

            // test if not match
            if (_hasMore == false || keyNext.SqlLike(_value, _collation))
            {
                var value = _returnKey ? nodeNext.ToBsonValue() : BsonValue.Null;

                // return current node
                return new PipeValue(nodeNext.IndexNodeID, nodeNext.DataBlockID, value);
            }
        }

        return this.Finish();
    }

    /// <summary>
    /// Do a full scan over index (head to tail) and return match strings
    /// </summary>
    private async ValueTask<PipeValue> ExecuteFullScanAsync(PipeContext context)
    {
        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var node = await indexService.GetNodeAsync(head);

            _next = node.GetNextID(0, _order);

            if (_next == tail)
            {
                _eof = true;

                return PipeValue.Empty;
            }
        }

        // go forward
        while (_eof == false)
        {
            var node = await indexService.GetNodeAsync(_next);

            // update next node
            _next = node.GetNextID(0, _order);

            // if next node if tail, finish after return
            if (_next == tail) _eof = true;

            // tests only if is string key
            if (node.IsStringValue)
            {
                var key = node.ToBsonValue().AsString;

                if (key.SqlLike(_value, _collation))
                {
                    var value = _returnKey ? node.ToBsonValue() : BsonValue.Null;

                    return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
                }
            }
        }

        _eof = true;

        return PipeValue.Empty;
    }

    private PipeValue Finish()
    {
        _eof = true;
        return PipeValue.Empty;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        var str = string.Format("{0} \"{1}\" ({1} {2} \"{3}\")",
            _startsWith.Length > 0 ? "INDEX SEEK (+RANGE SCAN)" : "FULL INDEX SCAN",
            _indexDocument.Name,
            _indexDocument.Expression,
            _order > 0 ? ">=" : "<=",
            _startsWith);

        builder.Add(str + (_order > 0 ? " ASC" : " DESC"), deep);
    }

    public void Dispose()
    {
    }
}
