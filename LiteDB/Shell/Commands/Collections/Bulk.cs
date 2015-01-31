using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class CollectionBulk : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "bulk");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            var col = this.ReadCollection(db, s);
            var filename = s.Scan(@".*");
            var json = File.ReadAllText(filename, Encoding.UTF8);
            var docs = JsonEx.DeserializeArray<BsonDocument>(json);
            var count = 0;

            db.BeginTrans();

            foreach (var doc in docs)
            {
                count++;
                col.Insert(doc);
            }

            db.Commit();

            display.WriteBson(count);
        }
    }
}
