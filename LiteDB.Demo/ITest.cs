using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    interface ITest
    {
        void Init();
        void Populate(IEnumerable<BsonDocument> docs);
        long Count();
        List<BsonDocument> Fetch(int skip, int limit);
    }
}