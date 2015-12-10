namespace LiteDB.Shell.Commands
{
    internal class CollectionFind : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s);
            var skipLimit = this.ReadSkipLimit(s);
            var docs = engine.Find(col, query, skipLimit.Key, skipLimit.Value);

            return new BsonArray(docs);
        }
    }
}