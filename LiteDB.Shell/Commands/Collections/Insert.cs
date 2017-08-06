using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionInsert : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var value = JsonSerializer.Deserialize(s.ToString());

            if (value.IsArray)
            {
                env.Display.WriteResult(env.Engine.Insert(col, value.AsArray.RawValue.Select(x => x.AsDocument)));
            }
            else
            {
                env.Engine.Insert(col, new BsonDocument[] { value.AsDocument });

                env.Display.WriteResult(value.AsDocument["_id"]);
            }
        }
    }
}