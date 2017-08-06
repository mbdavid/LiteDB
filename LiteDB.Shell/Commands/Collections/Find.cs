using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionFind : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var query = this.ReadQuery(s);
            var skipLimit = this.ReadSkipLimit(s);
            var docs = env.Engine.Find(col, query, skipLimit.Key, skipLimit.Value);

            env.Display.WriteResult(new BsonArray(docs));
        }
    }
}