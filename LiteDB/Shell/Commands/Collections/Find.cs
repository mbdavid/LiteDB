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
            var skipLimit = this.ReadSkipLimit(s);
            var docs = col.Find(query, skipLimit.Key, skipLimit.Value);

            return new BsonArray(docs);
        }
    }

#if DEBUG
    // to test only - only read all data
    internal class CollectionFind2 : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "check");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var query = this.ReadQuery(s);
            var skipLimit = this.ReadSkipLimit(s);
            var docs = col.Find(query, skipLimit.Key, skipLimit.Value);
            var cnt = 0;

            foreach(var d in docs)
            {
                cnt++;
            }

            return new BsonValue(cnt);
        }
    }
#endif
}
