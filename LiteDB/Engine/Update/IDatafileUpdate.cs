using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    interface IDatafileUpdate
    {
        bool IsFileVersion(Stream stream);
        void Initialize(Stream stream);
        IEnumerable<string> ReadCollections();
        IEnumerable<KeyValuePair<string, bool>> ReadIndexes(string colName);
        IEnumerable<BsonDocument> ReadDocuments(string colName);
    }
}
