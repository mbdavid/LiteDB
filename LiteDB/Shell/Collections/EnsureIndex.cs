using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
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

                if (s.Scan(@"\s*using\s+(.+)").Length > 0)
                {
                    expression = BsonExpression.ReadExpression(s, true, false)?.Source;
                }
            }

            s.ThrowIfNotFinish();

            yield return engine.EnsureIndex(col, field, unique, expression);
        }
    }
}