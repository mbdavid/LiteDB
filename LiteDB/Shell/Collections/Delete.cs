using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
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