using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionIndexes : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes$");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var docs = col.GetIndexes();

            return BsonArray.FromEnumerable<BsonObject>(docs);
        }
    }
}
