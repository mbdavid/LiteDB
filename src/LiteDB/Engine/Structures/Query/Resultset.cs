namespace LiteDB.Engine;

public struct Resultset : IDisposable
{
    public int From;
    public int To;
    public int DocumentCount;
    public bool HasMore;
    public readonly SharedArray<BsonValue> Results;

    public Resultset(int fetchSize)
    {
        this.Results = SharedArray<BsonValue>.Rent(fetchSize);
    }

    public override string ToString() => Dump.Object(this);

    public void Dispose()
    {
        this.Results.Dispose();
    }
}
