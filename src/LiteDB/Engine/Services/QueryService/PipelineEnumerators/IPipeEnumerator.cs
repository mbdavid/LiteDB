namespace LiteDB.Engine;

/// <summary>
/// Interface for a custom query pipe
/// </summary>
internal interface IPipeEnumerator : IDisposable
{
    PipeEmit Emit { get; }

    PipeValue MoveNext(PipeContext context);

    void GetPlan(ExplainPlainBuilder builder, int deep);
}
