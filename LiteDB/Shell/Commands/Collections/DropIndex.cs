using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionDropIndex : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop[iI]ndex");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            display.WriteBson(this.ReadCollection(db, s).DropIndex(s.Scan(@"\w+").Trim()));
        }
    }
}
