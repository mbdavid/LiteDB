using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class CollectionDrop : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop$");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);

            yield return engine.DropCollection(col);
        }
    }
}