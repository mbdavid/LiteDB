using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "dropIndex",
        Syntax = "db.<collection>.dropIndex [field|index]",
        Description = "Drop an index and make index area free to use with another. Returns true if index has been deleted.",
        Examples = new string[] {
            "db.orders.dropIndex customerName",
            "db.orders.dropIndex index_name",
        }
    )]
    internal class CollectionDropIndex : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop[iI]ndex");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Missing field index name", s);

            s.ThrowIfNotFinish();

            yield return engine.DropIndex(col, index);
        }
    }
}