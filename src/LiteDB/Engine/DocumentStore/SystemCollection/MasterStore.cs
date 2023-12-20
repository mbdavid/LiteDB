namespace LiteDB.Engine;

internal class MasterStore : IDocumentStore
{
    private readonly IDictionary<RowID, BsonDocument> _data;

    public byte ColID => SYS_MASTER_COL_ID;
    public string Name => "$master";

    public MasterStore()
    {
        _data = new Dictionary<RowID, BsonDocument>();
    }

    public void Initialize(IMasterService masterService)
    {
        var master = masterService.GetMaster(false);

        foreach(var col in master.Collections.Values)
        {
            var doc = new BsonDocument
            {
                ["colID"] = col.ColID,
                ["name"] = col.Name,
                ["type"] = "user",
            };

            this.AddSequencialItem(doc);
        }

        this.AddSequencialItem(new BsonDocument
        {
            ["colID"] = SYS_MASTER_COL_ID,
            ["name"] = "$master",
            ["type"] = "system",
        });

        this.AddSequencialItem(new BsonDocument
        {
            ["colID"] = SYS_MASTER_COL_ID + 1,
            ["name"] = "$database",
            ["type"] = "system",
        });
    }

    public void AddSequencialItem(BsonDocument data)
    {
        var pageID = (uint)_data.Count + 1;
        var rowID = new RowID(pageID, 0);

        _data.Add(rowID, data);
    }

    public CollectionDocument GetCollection() => throw new NotSupportedException();

    public IReadOnlyList<IndexDocument> GetIndexes() => throw new NotSupportedException();

    public (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
