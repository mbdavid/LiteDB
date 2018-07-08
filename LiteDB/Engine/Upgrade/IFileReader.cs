using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    interface IFileReader
    {
        DateTime CreationTime { get; }
        int CommitCounter { get; }
        DateTime LastCommit { get; }
        int UserVersion { get; }

        IEnumerable<string> GetCollections();
        IEnumerable<IndexInfo> GetIndexes();
        IEnumerable<BsonDocument> GetDocuments(string collection);
    }

    internal class IndexInfo
    {
        public string Collection { get; set; }
        public string Name { get; set; }
        public string Expression { get; set; }
        public bool Unique { get; set; }
    }
}