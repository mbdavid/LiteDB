namespace LiteDB.Shell.Commands
{
    internal class CollectionDelete : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "delete");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s);

            return engine.Delete(col, query);
        }
    }
}