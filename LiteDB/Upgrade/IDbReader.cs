using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Interface to implement old datafile format reader. Implements V6
    /// </summary>
    internal interface IDbReader : IDisposable
    {
        bool Initialize(Stream stream, string password);
        IEnumerable<string> GetCollections();
        IEnumerable<string> GetUniqueIndexes(string collection);
        IEnumerable<BsonDocument> GetDocuments(string collection);
    }
}
