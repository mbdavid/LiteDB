using System.Collections.Generic;
using System.Xml.Linq;

namespace LiteDB.Engine;

unsafe internal class IndexRangeEnumerator : IPipeEnumerator
{
    private readonly Collation _collation;

    private readonly IndexDocument _indexDocument;
    private readonly BsonValue _start;
    private readonly BsonValue _end;

    private readonly bool _startEquals;
    private readonly bool _endEquals;
    private readonly int _order;
    private readonly bool _returnKey;

    private bool _init = false;
    private bool _eof = false;

    private RowID _prev = RowID.Empty; // use a stack to keep order output
    private RowID _next = RowID.Empty; // all nodes from right of first node found

    public IndexRangeEnumerator(
        BsonValue start,
        BsonValue end,
        bool startEquals,
        bool endEquals,
        int order,
        IndexDocument indexDocument,
        Collation collation,
        bool returnKey)
    {
        // if order are desc, swap start/end values
        _start = order == Query.Ascending ? start : end;
        _end = order == Query.Ascending ? end : start;

        _startEquals = order == Query.Ascending ? startEquals : endEquals;
        _endEquals = order == Query.Ascending ? endEquals : startEquals;
        _order = order;
        _indexDocument = indexDocument;
        _collation = collation;
        _returnKey = returnKey;
    }

    public PipeEmit Emit => new(indexNodeID: true, dataBlockID: true, value: _returnKey);

    public unsafe PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var indexService = context.IndexService;

        var head = _order == Query.Ascending ? _indexDocument.HeadIndexNodeID : _indexDocument.TailIndexNodeID;
        var tail = _order == Query.Ascending ? _indexDocument.TailIndexNodeID : _indexDocument.HeadIndexNodeID;

        // in first run, look for index node
        if (_init == false)
        {
            _init = true;

            // find first indexNode (or get from head/tail if Min/Max value)
            var first =
                _start.IsMinValue ? indexService.GetNode(_indexDocument.HeadIndexNodeID) :
                _start.IsMaxValue ? indexService.GetNode(_indexDocument.TailIndexNodeID) :
                indexService.Find(_indexDocument, _start, true, _order);

            // get pointer to next/prev at level 0
            _prev = first[0]->GetPrev(_order);
            _next = first[0]->GetNext(_order);

            if (_startEquals && !first.IsEmpty)
            {
                if (!first.Key->IsMinValue && !first.Key->IsMaxValue)
                {
                    var value = _returnKey ? IndexKey.ToBsonValue(first.Key) : BsonValue.Null;

                    return new PipeValue(first.IndexNodeID, first.DataBlockID, value);
                }
            }
        }

        // first go forward
        if (_prev.IsEmpty == false)
        {
            var node = indexService.GetNode(_prev);

            // check for Min/Max bson values index node key
            if (node.Key->IsMaxValue || node.Key->IsMinValue)
            {
                _prev = RowID.Empty;
            }
            else
            {
                var diff = IndexKey.Compare(_start, node.Key, _collation);

                if (diff == (_order * -1 /* -1 */) || (diff == 0 && _startEquals))
                {
                    _prev = node[0]->GetPrev(_order);

                    var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

                    return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
                }
                else
                {
                    _prev = RowID.Empty;
                }
            }
        }

        // and than, go backward
        if (_next.IsEmpty == false)
        {
            var node = indexService.GetNode(_next);

            // check for Min/Max bson values index node key
            if (node.Key->IsMaxValue || node.Key->IsMinValue) return this.Finish();

            var diff = IndexKey.Compare(_end, node.Key, _collation);

            if (diff == (_order /* 1 */) || (diff == 0 && _endEquals))
            {
                _next = node[0]->GetNext(_order);

                var value = _returnKey ? IndexKey.ToBsonValue(node.Key) : BsonValue.Null;

                return new PipeValue(node.IndexNodeID, node.DataBlockID, value);
            }
            else
            {
                _eof = true;
                _next = RowID.Empty;
            }
        }

        return PipeValue.Empty;

    }

    private PipeValue Finish()
    {
        _eof = true;
        return PipeValue.Empty;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        var info =
            (_start.IsMinValue, _startEquals, _end.IsMaxValue, _endEquals) switch
            {
                (true, _, false, false) => $"INDEX SCAN ({_indexDocument.Name} < {_end})",
                (true, _, false, true) => $"INDEX SCAN ({_indexDocument.Name} <= {_end})",
                (false, false, true, _) => $"INDEX SCAN ({_indexDocument.Name} > {_start})",
                (false, true, true, _) => $"INDEX SCAN ({_indexDocument.Name} >= {_start})",
                _ => $"INDEX RANGE SCAN \"{_indexDocument.Name}\" ({_indexDocument.Expression} BETWEEN {_start} AND {_end})",
            } +
            (_order > 0 ? " ASC" : " DESC") +
            (_indexDocument.Unique ? " UNIQUE" : "");

        builder.Add(info, deep);
    }

    public void Dispose()
    {
    }
}
