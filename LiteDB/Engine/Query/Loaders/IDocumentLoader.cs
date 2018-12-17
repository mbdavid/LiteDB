using System.Collections.Generic;

namespace LiteDB.Engine
{
    /// <summary>
    /// Interface for abstract document loader that can be direct from datafile or by virtual collections
    /// </summary>
    internal interface IDocumentLoader
    {
        BsonDocument Load(IndexNode node);
    }
}