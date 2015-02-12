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
            int? skip = null;
            int? limit = null;

            this.ReadSkipLimit(s, ref skip, ref limit);

            // skip and limit must be in order: "skip" then "limit"
            if (skip.HasValue) docs = docs.Skip(skip.Value);
            if (limit.HasValue) docs = docs.Take(limit.Value);

            return BsonArray.FromEnumerable<BsonDocument>(docs);
        }
    }
}
