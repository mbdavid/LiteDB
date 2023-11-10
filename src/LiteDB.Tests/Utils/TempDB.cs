internal class TempDB : LiteEngine, IDisposable
{
    private readonly string _filename;

    public TempDB(IEngineSettings settings)
        : base(settings)
    {
        _filename = settings.Filename;
    }

    public static async ValueTask<ILiteEngine> CreateOrderDBAsync(OrderSet dataset)
    {
        var settings = new EngineSettings
        {
            Filename = Path.Combine(Path.GetTempPath(), "litedb-" + (new Random().NextInt64(100000, 999999)) + ".db"),
            Collation = Collation.Default
        };

        var db = new TempDB(settings);

        await db.OpenAsync();

        await db.ExecuteAsync("CREATE COLLECTION orders");
        await db.ExecuteAsync("CREATE COLLECTION customers");

        await db.ExecuteAsync("INSERT INTO orders VALUES @0", new BsonArray(dataset.Orders));
        await db.ExecuteAsync("INSERT INTO customers VALUES @0", new BsonArray(dataset.Customers));

        return db;
    }

    public new void Dispose()
    {
        base.Dispose();

        try
        {
            File.Delete(_filename);
        }
        catch
        {
        }
    }
}
