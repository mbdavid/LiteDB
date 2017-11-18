using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "ensureIndex",
        Syntax = "db.<collection>.ensureIndex <field|name> [unique] [using <expression|path>]",
        Description = "Create a new index to collection based on field or an expressions. Index could be with unique values only. Expressions can returns list of values (values from an array).",
        Examples = new string[] {
            "db.customers.ensureIndex name",
            "db.customers.ensureIndex email unique",
            "db.customers.ensureIndex tags using $.tags[*]",
            "db.customers.ensureIndex mobile_phones using $.phones[@.type = 'Mobile'].Number"
        }
    )]
    internal class CollectionEnsureIndex : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var field = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field/index name", s);
            var unique = false;
            string expression = null;

            s.Scan(@"\s*");

            if (s.HasTerminated == false)
            {
                unique = s.Scan(@"unique\s*").Length > 0;

                if (s.Scan(@"\s*using\s+").Length > 0)
                {
                    expression = BsonExpression.ReadExpression(s, true, false)?.Source;
                }
            }

            s.ThrowIfNotFinish();

            yield return engine.EnsureIndex(col, field, expression, unique);
        }
    }
}