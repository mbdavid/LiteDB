using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal class CollectionIndexes : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);

            s.ThrowIfNotFinish();

            var indexes = engine.GetIndexes(col);

            foreach(var index in indexes)
            {
                yield return new BsonDocument
                {
                    { "slot", index.Slot },
                    { "field", index.Field },
                    { "expression", index.Expression },
                    { "unique", index.Unique }
                };
            }
        }
    }
}