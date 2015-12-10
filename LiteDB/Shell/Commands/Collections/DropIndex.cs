namespace LiteDB.Shell.Commands
{
    internal class CollectionDropIndex : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop[iI]ndex");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            return engine.DropIndex(col, index);
        }
    }
}