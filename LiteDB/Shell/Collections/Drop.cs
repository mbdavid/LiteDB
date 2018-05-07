using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "drop",
        Syntax = "db.<collection>.drop",
        Description = "Delete all documents, indexes and collection name. Returns true if collection has been droped"
    )]
    internal class CollectionDrop : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "drop$");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);

            yield return engine.DropCollection(col);
        }
    }
}