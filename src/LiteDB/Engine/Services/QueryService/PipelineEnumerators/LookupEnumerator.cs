namespace LiteDB.Engine;

internal class LookupEnumerator : IPipeEnumerator
{
    private readonly IDocumentLookup _lookup;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public LookupEnumerator(IDocumentLookup lookup, IPipeEnumerator enumerator)
    {
        _lookup = lookup;
        _enumerator = enumerator;

        if (_enumerator.Emit.DataBlockID == false) throw ERR($"Lookup pipe enumerator requires DataBlockID from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: _enumerator.Emit.IndexNodeID, dataBlockID: true, value: true);

    public async ValueTask<PipeValue> MoveNextAsync(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = await _enumerator.MoveNextAsync(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        var doc = await _lookup.LoadAsync(item, context);

        return new PipeValue(item.IndexNodeID, item.DataBlockID, doc);
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"LOOKUP {_lookup}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
