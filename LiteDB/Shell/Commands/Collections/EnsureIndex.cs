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
            var field = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field name");
            var opts = JsonSerializer.Deserialize(s);
            var options = new IndexOptions();

            if(opts.IsBoolean)
            {
                options.Unique = opts.AsBoolean;
            }
            else if(opts.IsDocument)
            {
                var doc = opts.AsDocument;

                if (doc["unique"].IsBoolean) options.Unique = doc["unique"].AsBoolean;
                if (doc["ignoreCase"].IsBoolean) options.IgnoreCase = doc["ignoreCase"].AsBoolean;
                if (doc["removeAccents"].IsBoolean) options.RemoveAccents = doc["removeAccents"].AsBoolean;
                if (doc["trimWhitespace"].IsBoolean) options.TrimWhitespace = doc["trimWhitespace"].AsBoolean;
                if (doc["emptyStringToNull"].IsBoolean) options.EmptyStringToNull = doc["emptyStringToNull"].AsBoolean;
            }

            return engine.EnsureIndex(col, field, options);
        }
    }
}