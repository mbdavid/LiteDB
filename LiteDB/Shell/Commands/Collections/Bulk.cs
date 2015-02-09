using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionBulk : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "bulk");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var filename = s.Scan(@".*");
            var json = File.ReadAllText(filename, Encoding.UTF8);
            var docs = JsonSerializer.DeserializeArray<BsonDocument>(json);

            return col.InsertBulk(docs);
        }
    }
}
