namespace LiteDB.Shell.Commands
{
    internal class CollectionMax : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "max");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var index = s.Scan(this.FieldPattern).Trim();

            return engine.Max(col, index.Length == 0 ? "_id" : index);
        }
    }
}