using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionFind : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var query = this.ReadQuery(s);
            var docs = col.Find(query);

            var result = this.ReadSkipLimit(s, docs);

            return BsonArray.FromEnumerable<BsonDocument>(result);
        }
    }
}
