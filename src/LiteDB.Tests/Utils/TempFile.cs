internal class TempFile : IDisposable
{
    private readonly string _filename;

    public TempFile()
    {
        _filename = Path.Combine(Path.GetTempPath(), "litedb-" + (new Random().NextInt64(100000, 999999)) + ".db");
    }

    public async ValueTask<ILiteEngine> CreateOrderDBAsync(OrderSet dataset)
    {
        var settings = new EngineSettings
        {
            Filename = _filename,
            Collation = Collation.Default
        };

        var db = new LiteEngine(settings);

        await db.OpenAsync();

        await db.ExecuteAsync("CREATE COLLECTION orders");
        await db.ExecuteAsync("CREATE COLLECTION customers");

        await db.ExecuteAsync("INSERT INTO orders VALUES @0", BsonArray.FromArray(dataset.Orders));
        await db.ExecuteAsync("INSERT INTO customers VALUES @0", BsonArray.FromArray(dataset.Customers));

        return db;
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_filename);
        }
        catch
        {
        }
    }
}
