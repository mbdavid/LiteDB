//namespace LiteDB.Engine;

//internal class SystemCollection /*: IDocumentStore*/
//{
//    private readonly byte _colID;
//    private readonly string _collectionName;

//    public byte ColID => _colID;
//    public string Name => _collectionName;

//    public SystemCollection(byte colID, string collectionName)
//    {
//        _colID = colID;
//        _collectionName = collectionName;
//    }

//    public void Initialize(IMasterService masterService)
//    {
//        var master = masterService.GetMaster(false);

//        foreach(var col in master.Collections.Values)
//        {
//            var doc = new BsonDocument
//            {
//                ["colID"] = col.ColID,
//                ["name"] = col.Name,
//                ["type"] = "user",
//            };

//            this.AddSequencialItem(doc);
//        }

//        this.AddSequencialItem(new BsonDocument
//        {
//            ["colID"] = SYS_MASTER_COL_ID,
//            ["name"] = "$master",
//            ["type"] = "system",
//        });

//        this.AddSequencialItem(new BsonDocument
//        {
//            ["colID"] = SYS_MASTER_COL_ID + 1,
//            ["name"] = "$database",
//            ["type"] = "system",
//        });
//    }

//    public void AddSequencialItem(BsonDocument data)
//    {
//        var pageID = (uint)_data.Count + 1;
//        var rowID = new RowID(pageID, 0);

//        _data.Add(rowID, data);
//    }

//    public CollectionDocument GetCollection() => throw new NotSupportedException();

//    public IReadOnlyList<IndexDocument> GetIndexes() => throw new NotSupportedException();

//    public (IDataService dataService, IIndexService indexService) GetServices(IServicesFactory factory, ITransaction transaction)
//    {
//        throw new NotImplementedException();
//    }

//    public void Dispose()
//    {
//    }
//}
