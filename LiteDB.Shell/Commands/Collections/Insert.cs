using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionInsert : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var value = JsonSerializer.Deserialize(s.ToString());

            if (value.IsArray)
            {
                display.WriteResult(engine.Insert(col, value.AsArray.RawValue.Select(x => x.AsDocument)));
            }
            else
            {
                engine.Insert(col, new BsonDocument[] { value.AsDocument });

                display.WriteResult(value.AsDocument["_id"]);
            }
        }
    }
}