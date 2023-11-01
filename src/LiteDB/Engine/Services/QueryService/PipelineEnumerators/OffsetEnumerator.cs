namespace LiteDB.Engine;

internal class OffsetEnumerator : IPipeEnumerator
{
    private readonly IPipeEnumerator _enumerator;

    private readonly int _offset;

    private int _count = 0;
    private bool _eof = false;

    public OffsetEnumerator(int offset, IPipeEnumerator enumerator)
    {
        _offset = offset;
        _enumerator = enumerator;
    }

    public PipeEmit Emit => _enumerator.Emit;

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        while (_count <= _offset)
        {
            var skiped = _enumerator.MoveNext(context);

            if (skiped.IsEmpty)
            {
                _eof = true;

                return PipeValue.Empty;
            }

            _count++;
        }

        var item = _enumerator.MoveNext(context);

        if (item.IsEmpty)
        {
            _eof = true;

            return PipeValue.Empty;
        }

        return item;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"OFFSET {_offset}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
