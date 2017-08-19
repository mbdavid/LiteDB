using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal class CollectionInsert : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var value = JsonSerializer.Deserialize(s.ToString());

            if (value.IsArray)
            {
                var count = engine.Insert(col, value.AsArray.RawValue.Select(x => x.AsDocument));

                yield return count;
            }
            else
            {
                engine.Insert(col, new BsonDocument[] { value.AsDocument });

                yield return value.AsDocument["_id"];
            }
        }
    }
}