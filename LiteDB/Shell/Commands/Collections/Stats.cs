using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionStats : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "stats");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);

            display.WriteBson(col.Stats());
        }
    }
}
