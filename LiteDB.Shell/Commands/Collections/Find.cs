using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionFind : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s);
            var skipLimit = this.ReadSkipLimit(s);
            var docs = engine.Find(col, query, skipLimit.Key, skipLimit.Value);

            display.WriteResult(new BsonArray(docs));
        }
    }
}