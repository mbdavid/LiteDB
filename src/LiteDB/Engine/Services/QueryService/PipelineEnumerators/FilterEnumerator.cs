namespace LiteDB.Engine;

internal class FilterEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly Collation _collation;

    private readonly IPipeEnumerator _enumerator;
    private readonly BsonExpression _filter;

    private bool _eof = false;

    public FilterEnumerator(BsonExpression filter, IPipeEnumerator enumerator, Collation collation)
    {
        _filter = filter;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Value == false) throw ERR($"Filter pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => _enumerator.Emit;

    public PipeValue MoveNext(PipeContext context)
    {
        while (!_eof)
        {
            var item = _enumerator.MoveNext(context);

            if (item.IsEmpty)
            {
                _eof = true;
            }
            else
            {
                var result = _filter.Execute(item.Value, context.QueryParameters, _collation);

                if (result.IsBoolean && result.AsBoolean)
                {
                    return item;
                }
            }
        }

        return PipeValue.Empty;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"FILTER {_filter}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
