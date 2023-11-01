namespace LiteDB;

internal class BsonExpressionContext
{
    public BsonValue Root { get; }
    public BsonValue Current { get; set; }
    public BsonDocument Parameters { get; }
    public Collation Collation { get; }

    public BsonExpressionContext(BsonValue root, BsonDocument parameters, Collation collation)
    {
        this.Root = root;
        this.Current = this.Root;
        this.Parameters = parameters;
        this.Collation = collation;
    }
}
