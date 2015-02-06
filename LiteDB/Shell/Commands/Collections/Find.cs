using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionFind : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var top = this.ReadTop(s);
            var query = this.ReadQuery(s);
            var docs = col.Find(query).Take(top);

            return BsonArray.FromEnumerable<BsonDocument>(docs);
        }
    }
}
