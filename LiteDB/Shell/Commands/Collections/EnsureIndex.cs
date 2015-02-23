using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionEnsureIndex : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var field = s.Scan(this.FieldPattern).Trim();
            var unique = s.Scan(@"unique$");

            return col.EnsureIndex(field, unique.Length > 0);
        }
    }
}
