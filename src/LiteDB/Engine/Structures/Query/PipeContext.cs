namespace LiteDB.Engine;

internal struct PipeContext
{
    public readonly IDataService DataService;
    public readonly IIndexService IndexService;
    public readonly BsonDocument QueryParameters;

    public PipeContext(IDataService dataService, IIndexService indexService, BsonDocument queryParameters)
    {
        this.DataService = dataService;
        this.IndexService = indexService;
        this.QueryParameters = queryParameters;
    }
}
