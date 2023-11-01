namespace LiteDB.Engine;

internal class Cursor : IDisposable, IIsEmpty
{
    public int CursorID { get; }

    public Query Query { get; }
    public BsonDocument Parameters { get; }
    public int ReadVersion { get; }
    public IPipeEnumerator Enumerator { get; }

    public int FetchCount { get; set; } = 0;
    public int Offset { get; set; } = 0;
    public bool IsRunning { get; set; } = false;

    public DateTime Start { get; } = DateTime.UtcNow;
    public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

    public BsonDocument? NextDocument { get; set; }

    public bool IsEmpty => this.CursorID == 0;

    public static Cursor Empty = new Cursor();

    public Cursor()
    {
        this.CursorID = 0;
        this.Parameters = BsonDocument.Empty;
        this.ReadVersion = 0;
        this.Enumerator = null;
    }

    public Cursor(Query query, BsonDocument parameters, int readVersion, IPipeEnumerator enumerator)
    {
        this.CursorID = Randomizer.Next(1000, int.MaxValue); // create a random cursorID
        this.Query = query;
        this.Parameters = parameters;
        this.ReadVersion = readVersion;
        this.Enumerator = enumerator;
    }

    public BsonArray GetExplainPlan()
    {
        if (this.Enumerator is null) return BsonArray.Empty;

        var explain = new ExplainPlainBuilder();

        this.Enumerator.GetPlan(explain, 0);

        return explain.ToBsonArray();
    }

    public void Dispose()
    {
        this.Enumerator.Dispose();
    }
}
