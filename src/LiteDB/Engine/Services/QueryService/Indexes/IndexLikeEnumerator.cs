using System.Xml.Linq;

namespace LiteDB.Engine;

unsafe internal class IndexLikeEnumerator : IPipeEnumerator
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

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        return _startsWith.Length > 0 ? this.ExecuteLike(context) : this.ExecuteFullScan(context);
    }

    private unsafe PipeValue ExecuteLike(PipeContext context)
    {
        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, look for index node
        if (_init == false)
        {
            _init = true;

            var node = indexService.Find(_indexDocument, _startsWith, true, Query.Ascending);

            // if node was not found, end enumerator
            if (node.IsEmpty || node.Key->Type != BsonType.String) return this.Finish();

            // get start prev (left side)
            var prevID = node.IndexNodeID;

            // get next index node
            _next = node[0]->GetNext(_order);

            // add all prev items into _prevs
            while (true)
            {
                var nodePrev = indexService.GetNode(prevID);

                if (nodePrev.Key->Type != BsonType.String) break;

                var keyPrev = IndexKey.ToBsonValue(nodePrev.Key).AsString;

                // test if match initial startsWith
                if (!_collation.StartsWith(keyPrev, _startsWith)) break;

                if (_hasMore == false || keyPrev.SqlLike(_value, _collation))
                {
                    var value = _returnKey ? IndexKey.ToBsonValue(nodePrev.Key) : BsonValue.Null;

                    // push current value
                    _prev.Push(new(nodePrev.IndexNodeID, nodePrev.DataBlockID, value));
                }

                prevID = nodePrev[0]->GetPrev(_order);
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
            var nodeNext = indexService.GetNode(_next);

            // set for next
            _next = nodeNext[0]->GetNext(_order);

            if (_next == tail) _eof = true;

            // if not string, finish
            if (nodeNext.Key->Type != BsonType.String) return this.Finish();

            //get nextKey as string
            var keyNext = IndexKey.ToBsonValue(nodeNext.Key).AsString;

            // test if match initial startsWith
            if (!_collation.StartsWith(keyNext, _startsWith)) break;

            // test if not match
            if (_hasMore == false || keyNext.SqlLike(_value, _collation))
            {
                var value = _returnKey ? IndexKey.ToBsonValue(nodeNext.Key) : BsonValue.Null;

                // return current node
                return new PipeValue(nodeNext.IndexNodeID, nodeNext.DataBlockID, value);
            }
        }

        return this.Finish();
    }

    /// <summary>
    /// Do a full scan over index (head to tail) and return match strings
    /// </summary>
    private unsafe PipeValue ExecuteFullScan(PipeContext context)
    {
        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, gets head node
        if (_init == false)
        {
            _init = true;

            var node = indexService.GetNode(head);

            _next = node[0]->GetNext(_order);

            if (_next == tail)
            {
                _eof = true;

                return PipeValue.Empty;
            }
        }

        // go forward
        while (_eof == false)
        {
            var node = indexService.GetNode(_next);

            // update next node
            _next = node[0]->GetNext(_order);

            // if next node if tail, finish after return
            if (_next == tail) _eof = true;

            // tests only if is string key
            if (node.Key->Type == BsonType.String)
            {
                var key = IndexKey.ToBsonValue(node.Key).AsString;

                if (key.SqlLike(_value, _collation))
                {
                    var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

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
