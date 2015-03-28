using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionEnsureIndex : BaseCollection, ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var col = this.ReadCollection(db, s);
            var field = s.Scan(this.FieldPattern).Trim();
            var doc = JsonSerializer.Deserialize(s);

            if (doc.IsNull)
            {
                return col.EnsureIndex(field, false);
            }
            else if (doc.IsBoolean)
            {
                return col.EnsureIndex(field, doc.AsBoolean);
            }
            else
            {
                var options = db.Mapper.ToObject<IndexOptions>(doc.AsDocument);

                return col.EnsureIndex(field, options);
            }
        }
    }
}
