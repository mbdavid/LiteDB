using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionIndexes : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "indexes$");
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            
            return new BsonArray(engine.GetIndexes(col).Select(x => new BsonDocument
            {
                { "slot", x.Slot },
                { "field", x.Field },
                { "unique", x.Unique }
            }));
        }
    }
}