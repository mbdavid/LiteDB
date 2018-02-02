namespace LiteDB
{
    /// <summary>
    /// Interface for abstract document loader that can be direct from datafile or by virtual collections
    /// </summary>
    internal interface IDocumentLoader
    {
        BsonDocument Load(PageAddress dataBlock);
    }
}