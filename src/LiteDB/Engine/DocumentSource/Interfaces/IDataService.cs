namespace LiteDB.Engine;

internal interface IDataService
{
    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    RowID InsertDocument(byte colID, BsonDocument doc);

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    void UpdateDocument(RowID dataBlockID, BsonDocument doc);

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    BsonReadResult ReadDocument(RowID dataBlockID, string[] fields);

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    void DeleteDocument(RowID dataBlockID);
}