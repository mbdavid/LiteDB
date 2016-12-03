using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Upgrade
{
    /// <summary>
    /// Interface to implement old datafile format reader. Implements V6
    /// </summary>
    internal interface IDatafileReader
    {
        bool IsVersion(Stream stream);
        void Initialize(Stream stream);
        IEnumerable<string> GetCollections();
        IEnumerable<KeyValuePair<string, bool>> GetIndexes(string collection);
        IEnumerable<BsonDocument> GetDocuments(string collection);
    }
}
