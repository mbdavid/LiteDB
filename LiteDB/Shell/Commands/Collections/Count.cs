using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionCount : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "count");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var query = this.ReadQuery(s);

            return col.Count(query);
        }
    }
}
