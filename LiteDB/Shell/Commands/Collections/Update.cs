namespace LiteDB.Shell.Commands
{
    public class CollectionUpdate : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var doc = JsonSerializer.Deserialize(s).AsDocument;

            return engine.Update(col, new BsonDocument[] { doc });
        }
    }
}