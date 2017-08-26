using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
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