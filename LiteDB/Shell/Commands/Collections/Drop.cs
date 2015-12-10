namespace LiteDB.Shell.Commands
{
    internal class CollectionDrop : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop$");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);

            return engine.DropCollection(col);
        }
    }
}