using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionFind : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);
            var top = this.ReadTop(s);
            var query = this.ReadQuery(s);

            display.WriteBson<BsonDocument>(col.Find(query).Take(top));
        }
    }
}
