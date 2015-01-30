using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionDelete : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "delete");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var query = this.ReadQuery(s);

            display.WriteBson(col.Delete(query));
        }
    }
}
