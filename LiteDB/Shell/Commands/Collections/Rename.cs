namespace LiteDB.Shell.Commands
{
    internal class CollectionRename : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "rename");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var newName = s.Scan(@"[\w-]+").ThrowIfEmpty("Invalid new collection name");

            return engine.RenameCollection(col, newName);
        }
    }
}