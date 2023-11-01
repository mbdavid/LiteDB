namespace LiteDB.Engine;

internal class OrderByEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly ISortOperation _sorter;

    private readonly OrderBy _orderBy;
    private readonly IPipeEnumerator _enumerator;
    private bool _init;

    public OrderByEnumerator(
        OrderBy orderBy,
        IPipeEnumerator enumerator,
        ISortService sortService)
    {
        _enumerator = enumerator;

        if (_enumerator.Emit.DataBlockID == false) throw ERR($"OrderBy pipe enumerator requires DataBlockID from last pipe");
        if (_enumerator.Emit.Value == false) throw ERR($"OrderBy pipe enumerator requires Document from last pipe");

        _orderBy = orderBy;
        _sorter = sortService.CreateSort(orderBy);
    }

    public PipeEmit Emit => new(indexNodeID: false, dataBlockID: true, value: false);

    public PipeValue MoveNext(PipeContext context)
    {
        if(_init == false)
        {
            // consume all _enumerator and get ready for new enumerator: _sorter
            _sorter.InsertData(_enumerator, context);
            _init = true;
        }

        // get next sorted item (returns Empty when EOF)
        var item = _sorter.MoveNext();

        return new PipeValue(RowID.Empty, item.DataBlockID);
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"ORDER BY {_orderBy}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
        // dispose/release all used containers
        _sorter.Dispose();
    }
}