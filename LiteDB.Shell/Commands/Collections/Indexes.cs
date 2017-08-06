using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionIndexes : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes$");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);

            env.Display.WriteResult(new BsonArray(env.Engine.GetIndexes(col).Select(x => new BsonDocument
            {
                { "slot", x.Slot },
                { "field", x.Field },
                { "unique", x.Unique }
            })));
        }
    }
}