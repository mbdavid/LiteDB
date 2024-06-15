namespace LiteDB.Engine;

using System.Collections.Generic;

public partial class LiteEngine
{
    private IEnumerable<BsonDocument> SysIndexes()
    {
        // get any transaction from current thread ID
        var transaction = _monitor.GetThreadTransaction();

        foreach (var collection in _header.GetCollections())
        {
            var snapshot = transaction.CreateSnapshot(LockMode.Read, collection.Key, false);

            foreach (var index in snapshot.CollectionPage.GetCollectionIndexes())
            {
                yield return new BsonDocument
                {
                    ["collection"] = collection.Key,
                    ["name"] = index.Name,
                    ["expression"] = index.Expression,
                    ["unique"] = index.Unique,
                };
            }
        }
    }
}