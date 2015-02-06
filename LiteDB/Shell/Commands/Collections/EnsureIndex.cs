using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionEnsureIndex : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var field = s.Scan(@"\w+(.\w+)*");
            var unique = s.Scan(@"\s*unique$");

            return col.EnsureIndex(field, unique.Length > 0);
        }
    }
}
