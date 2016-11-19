using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionIndexes : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);

            display.WriteResult(new BsonArray(engine.GetIndexes(col).Select(x => new BsonDocument
            {
                { "slot", x.Slot },
                { "field", x.Field },
                { "unique", x.Unique }
            })));
        }
    }
}