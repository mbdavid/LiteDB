namespace LiteDB;

/// <summary>
/// Class to read void, one or a collection of BsonValues. Used in SQL execution commands and query returns. Use local data source (IEnumerable[BsonDocument])
/// </summary>
public class BsonDataReader : IDataReader
{
    private readonly IServicesFactory _factory;

    private readonly Cursor _cursor;
    private readonly int _fetchSize;

    private int _current = -1;
    private Resultset _resultset;

    /// <summary>
    /// Initialize data reader with created cursor
    /// </summary>
    internal BsonDataReader(Cursor cursor, int fetchSize, IServicesFactory factory)
    {
        _cursor = cursor;
        _resultset = new Resultset(fetchSize);
        _fetchSize = fetchSize;
        _factory = factory;
    }

    /// <summary>
    /// Return current value
    /// </summary>
    public BsonValue Current => _resultset.Results[_current];

    /// <summary>
    /// Return collection name
    /// </summary>
    public string Collection => _cursor.Query.Collection;

    /// <summary>
    /// Move cursor to next result. Returns true if read was possible
    /// </summary>
    public async ValueTask<bool> ReadAsync()
    {
        if (_current == int.MaxValue) return false; // eof

        if (_current == -1) // need to be initialize
        {
            await this.FetchAsync();

            return _current != int.MaxValue;
        }
        else
        {
            // move no next in same _result
            _current++;

            // if exceed, get next resultset
            if (_current == _resultset.DocumentCount)
            {
                await this.FetchAsync();

                return _current != int.MaxValue;
            }
            else
            {
                return true;
            }
        }
    }

    private async ValueTask FetchAsync()
    {
        var monitorService = _factory.MonitorService;
        var queryService = _factory.QueryService;
        var storeFactory = _factory.StoreFactory;

        if (_factory.State != EngineState.Open) throw ERR("must be opened");

        // create a new transaction for a specific read version
        using var transaction = await monitorService.CreateTransactionAsync(_cursor.ReadVersion);

        try
        {
            var store = storeFactory.GetUserCollection(_cursor.Query.Collection);
            var (dataService, indexService) = store.GetServices(_factory, transaction);
            var context = new PipeContext(dataService, indexService, _cursor.Parameters);

            /*await*/
            queryService.FetchAsync(_cursor, _fetchSize, context, ref _resultset);

            transaction.Abort();

            monitorService.ReleaseTransaction(transaction);

            // start current index in 0
            _current = _resultset.DocumentCount == 0 ? int.MaxValue : 0;
        }
        catch (Exception ex)
        {
            transaction.Abort();
            monitorService.ReleaseTransaction(transaction);

            ex.HandleError(_factory);

            throw;
        }
    }

    public BsonValue this[string field] => _current == -1 || _current == int.MaxValue ? 
        BsonValue.Null :
        _resultset.Results[_current].AsDocument[field];

    public void Dispose()
    {
    }
}