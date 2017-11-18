using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "min",
        Syntax = "db.<collection>.min [<field>]",
        Description = "Returns min/first value from collection using index field. Use default _id index if not defined",
        Examples = new string[] {
            "db.orders.min",
            "db.orders.min order_date"
        }
    )]
    internal class CollectionMin : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "min");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            if (!s.HasTerminated) throw LiteException.SyntaxError(s, "Invalid field/index name");

            yield return engine.Min(col, index.Length == 0 ? "_id" : index);
        }
    }
}