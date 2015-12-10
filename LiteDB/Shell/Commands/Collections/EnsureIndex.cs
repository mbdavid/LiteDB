namespace LiteDB.Shell.Commands
{
    internal class CollectionEnsureIndex : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var field = s.Scan(this.FieldPattern).Trim();
            var opts = JsonSerializer.Deserialize(s);
            var options =
                opts.IsNull ? new IndexOptions() :
                opts.IsBoolean ? new IndexOptions { Unique = opts.AsBoolean } :
                (new BsonMapper()).ToObject<IndexOptions>(opts.AsDocument);

            return engine.EnsureIndex(col, field, options);
        }
    }
}