using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionCount : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "count");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);
            var query = this.ReadQuery(s);

            display.WriteBson(col.Count(query));
        }
    }
}
