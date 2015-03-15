using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionInsert : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var value = JsonSerializer.Deserialize(s);

            if (value.IsArray)
            {
                return col.Insert(value.AsArray.RawValue.Select(x => x.AsDocument));
            }
            else
            {
                col.Insert(new BsonDocument(value.AsDocument));

                return BsonValue.Null;
            }
        }
    }
}
