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
            var col = this.ReadCollection(db, s);
            var filename = s.Scan(@".*");
            var json = File.ReadAllText(filename, Encoding.UTF8);
            var docs = JsonEx.Deserialize(json);
            var count = 0;

            db.BeginTrans();

            if (docs.IsArray)
            {
                foreach (var doc in docs.AsArray)
                {
                    count++;
                    col.Insert(new BsonDocument(doc));
                }
            }
            else
            {
                count = 1;
                col.Insert(new BsonDocument(docs));
            }

            db.Commit();

            display.WriteBson(count);
        }
    }
}
