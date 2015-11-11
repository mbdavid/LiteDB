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
        public IEnumerable<string> GetCollectionNames()
        {
            return null;
        }

        public bool DropCollection(string colName)
        {
            return true;
        }

        public bool RenameCollection(string colName, string newName)
        {
            return true;
        }
    }
}
