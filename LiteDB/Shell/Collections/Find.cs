using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "find",
        Syntax = "db.<collection>.find [<filter>] [skip <N>] [limit <M>]",
        Description = "Search documents inside collection using filter clause. If filter omited, return all documents. Can be used with skip/limit to restrict results. Can be use an index or full scan query.",
        Examples = new string[] {
            "db.customers.find _id > 10",
            "db.customers.find _id != 1 and YEAR($.birthday) <= 1977",
            "db.customers.find name startsWith \"John\" skip 50 limit 25",
            "db.customers.find UPPER($.tags[*]) startsWith \"NEW\""
        }
    )]
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