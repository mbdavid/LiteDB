using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionDrop : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            display.WriteBson(this.ReadCollection(db, s).Drop());
        }
    }
}
