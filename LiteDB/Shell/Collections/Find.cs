using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class CollectionFind : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "find");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var query = this.ReadQuery(s, false);
            var skipLimit = this.ReadSkipLimit(s);
            var includes = this.ReadIncludes(s);

            s.ThrowIfNotFinish();

            var docs = engine.Find(col, query, includes, skipLimit.Key, skipLimit.Value);

            foreach(var doc in docs)
            {
                yield return doc;
            }
        }
    }
}