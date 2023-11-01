namespace LiteDB.Tests.Internals.Engine;

internal class MockEnumerator : IPipeEnumerator
{
    private readonly Queue<PipeValue> _items;

    public MockEnumerator(IEnumerable<PipeValue> values)
    {
        _items = new Queue<PipeValue>(values);
    }

    public PipeEmit Emit => new(true, true, true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_items.Count == 0) return PipeValue.Empty;

        var item = _items.Dequeue();

        return item;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}