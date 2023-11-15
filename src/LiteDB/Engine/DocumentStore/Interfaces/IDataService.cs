namespace LiteDB.Engine;

internal interface IDataService
{
    /// <summary>
    /// Insert BsonDocument into new data pages
    /// </summary>
    ValueTask<RowID> InsertDocumentAsync(byte colID, BsonDocument doc);

    /// <summary>
    /// Update existing document in a single or multiple pages
    /// </summary>
    ValueTask UpdateDocumentAsync(RowID dataBlockID, BsonDocument doc);

    /// <summary>
    /// Read a single document in a single/multiple pages
    /// </summary>
    ValueTask<BsonReadResult> ReadDocumentAsync(RowID dataBlockID, string[] fields);

    /// <summary>
    /// Delete a full document from a single or multiple pages
    /// </summary>
    ValueTask DeleteDocumentAsync(RowID dataBlockID);
}