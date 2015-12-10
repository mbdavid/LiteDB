namespace LiteDB.Shell.Commands
{
    internal class CollectionCount : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "count");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s);

            return engine.Count(col, query);
        }
    }
}