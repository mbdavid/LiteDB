namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class QueryService : IQueryService
{
    // dependency injections
    private readonly IWalIndexService _walIndexService;
    private readonly IServicesFactory _factory;

    private readonly ConcurrentDictionary<int, Cursor> _openCursors = new();

    public QueryService(
        IWalIndexService walIndexService,
        IServicesFactory factory)
    {
        _walIndexService = walIndexService;
        _factory = factory;
    }

    public Cursor CreateCursor(Query query, BsonDocument parameters, int readVersion)
    {
        var queryOptimization = _factory.CreateQueryOptimization();

        var enumerator = queryOptimization.ProcessQuery(query, parameters);

        var cursor = new Cursor(query, parameters, readVersion, enumerator);

        _openCursors.TryAdd(cursor.CursorID, cursor);

        return cursor;
    }

    public bool TryGetCursor(int cursorID, out Cursor cursor) => _openCursors.TryGetValue(cursorID, out cursor);

    public void FetchAsync(Cursor cursor, int fetchSize, PipeContext context, ref Resultset result)
    {
        var index = 0;
        var eof = false;
        var start = Stopwatch.GetTimestamp();
        var enumerator = cursor.Enumerator;

        // checks if readVersion still avaiable to execute (changes after checkpoint)
        if (cursor.ReadVersion < _walIndexService.MinReadVersion)
        {
            cursor.Dispose();

            _openCursors.TryRemove(cursor.CursorID, out _);

            throw ERR($"Cursor {cursor} expired");
        }

        cursor.IsRunning = true;

        var fetchSizeNext = fetchSize + 
            (cursor.NextDocument is null ? 1 : 0);

        if (cursor.NextDocument is not null)
        {
            result.Results[0] = cursor.NextDocument;
            cursor.NextDocument = null;
            index++;
        }

        while (index < fetchSizeNext)
        {
            var item = enumerator.MoveNext(context);

            if (item.IsEmpty)
            {
                eof = true;
                break;
            }
            else if (index < fetchSize)
            {
                result.Results[index] = item.Value!;

                index++;
            }
            else
            {
                cursor.NextDocument = item.Value.AsDocument;
                break;
            }
        }

        // add computed time to run query
        cursor.ElapsedTime += DateExtensions.GetElapsedTime(start);

        // if fetch finish, remove cursor
        if (eof)
        {
            cursor.Dispose();

            _openCursors.TryRemove(cursor.CursorID, out _);
        }

        cursor.FetchCount += index; // increment fetch count on cursor
        cursor.IsRunning = false;

        // update resultset
        result.From = cursor.Offset;
        result.To = cursor.Offset += index;
        result.DocumentCount = index;
        result.HasMore = !eof;
    }

    public override string ToString()
    {
        return Dump.Object(new { openCursors = Dump.Array(_openCursors.Select(x => x.Key)) });
    }

    public void Dispose()
    {
        _openCursors.Clear();
    }
}
