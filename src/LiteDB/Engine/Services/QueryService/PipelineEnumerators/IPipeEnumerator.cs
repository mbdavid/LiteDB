namespace LiteDB.Engine;

/// <summary>
/// Interface for a custom query pipe
/// </summary>
internal interface IPipeEnumerator : IDisposable
{
    PipeEmit Emit { get; }

    ValueTask<PipeValue> MoveNextAsync(PipeContext context);

    void GetPlan(ExplainPlainBuilder builder, int deep);
}
