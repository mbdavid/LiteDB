using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "count",
        Syntax = "db.<collection>.count [filter]",
        Description = "Show count rows according query filter",
        Examples = new string[] {
            "db.orders.count",
            "db.orders.count customer = \"John Doe\"",
            "db.orders.count customer startsWith \"John\" and YEAR($.orderDate) >= 2015"
        }
    )]
    internal class CollectionCount : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "count");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s, false);

            s.ThrowIfNotFinish();

            yield return engine.Count(col, query);
        }
    }
}