namespace LiteDB.Engine;

internal class SystemCollectionDataService : IDataService
{
    public void DeleteDocument(RowID dataBlockID)
    {
        throw new NotImplementedException();
    }

    public RowID InsertDocument(byte colID, BsonDocument doc)
    {
        throw new NotImplementedException();
    }

    public BsonReadResult ReadDocument(RowID dataBlockID, string[] fields)
    {
        throw new NotImplementedException();
    }

    public void UpdateDocument(RowID dataBlockID, BsonDocument doc)
    {
        throw new NotImplementedException();
    }
}
