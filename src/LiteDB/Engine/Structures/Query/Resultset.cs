namespace LiteDB.Engine;

public struct Resultset
{
    public int From;
    public int To;
    public int DocumentCount;
    public bool HasMore;
    public readonly SharedArray<BsonValue> Results;

    public Resultset(SharedArray<BsonValue> sharedArray)
    {
        this.Results = sharedArray;
    }

    public override string ToString() => Dump.Object(this);
}
