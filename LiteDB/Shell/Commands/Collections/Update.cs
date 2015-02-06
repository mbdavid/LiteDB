using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionUpdate : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var doc = new BsonDocument(new JsonReader().ReadValue(s));

            return col.Update(doc);
        }
    }
}
