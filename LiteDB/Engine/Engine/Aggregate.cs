using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class LiteEngine : IDisposable
    {
        public BsonValue Min(string colName, string field)
        {
            return true;
        }

        public BsonValue Max(string colName, string field)
        {
            return true;
        }

        public int Count(string colName, Query query)
        {
            return 0;
        }

        public bool Exists(string colName, Query query)
        {
            return true;
        }
    }
}
