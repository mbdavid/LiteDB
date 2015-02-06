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

        public void Execute(LiteDatabase db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            display.WriteBson<BsonObject>(this.ReadCollection(db, s).GetIndexes());
        }
    }
}
