using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionStats : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "stats");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);

            display.WriteBson(col.Stats());
        }
    }
}
