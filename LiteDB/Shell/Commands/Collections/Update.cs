using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class CollectionUpdate : Collection, ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            var col = this.ReadCollection(db, s);
            var value = new JsonReader().ReadValue(s);

            display.WriteBson(col.Update(new BsonDocument(value)));
        }
    }
}
