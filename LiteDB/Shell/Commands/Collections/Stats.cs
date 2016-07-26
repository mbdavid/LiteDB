using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionStats : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "stats");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);

            var mapper = new BsonMapper().UseCamelCase();

            return mapper.ToDocument<CollectionInfo>(engine.Stats(col));
        }
    }
}