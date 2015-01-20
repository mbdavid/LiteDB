using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionIndexes : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes$");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            display.WriteBson<BsonObject>(this.ReadCollection(db, s).GetIndexes());
        }
    }
}
