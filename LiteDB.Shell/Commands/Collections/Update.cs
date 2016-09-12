using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionUpdate : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var doc = JsonSerializer.Deserialize(s.ToString()).AsDocument;

            return engine.Update(col, new BsonDocument[] { doc });
        }
    }
}