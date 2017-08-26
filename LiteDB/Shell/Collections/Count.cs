using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
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