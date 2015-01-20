using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionEnsureIndex : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);
            var field = s.Scan(@"\w+");
            var unique = s.Scan(@"\s*unique$");

            display.WriteBson(col.EnsureIndex(field, unique.Length > 0));
        }
    }
}
