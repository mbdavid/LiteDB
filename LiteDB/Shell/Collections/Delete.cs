using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "delete",
        Syntax = "db.<collection>.delete [filter]",
        Description = "Delete documents according filter clause (required). Retruns deleted document count.",
        Examples = new string[] {
            "db.orders.delete _id = 2",
            "db.orders.delete customer = \"John Doe\"",
            "db.orders.delete customer startsWith \"John\" and YEAR($.orderDate) >= 2015"
        }
    )]
    internal class CollectionDelete : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "delete");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s, true);

            s.ThrowIfNotFinish();

            yield return engine.Delete(col, query);
        }
    }
}