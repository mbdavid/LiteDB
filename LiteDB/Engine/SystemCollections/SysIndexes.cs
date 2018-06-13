using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysIndexes()
        {
            var transaction = this.GetTransaction(true, out var isNew);

            try
            {
                // encapsulate all execution to catch any error
                return GetIndexes();
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Dispose();
                throw;
            }

            IEnumerable<BsonDocument> GetIndexes()
            {
                foreach (var collection in this.GetCollectionNames().OrderBy(x => x))
                {
                    var snapshot = transaction.CreateSnapshot(SnapshotMode.Write, collection, false);

                    foreach(var index in snapshot.CollectionPage.GetIndexes(true))
                    {
                        yield return new BsonDocument
                        {
                            ["collection"] = collection,
                            ["name"] = index.Name,
                            ["slot"] = index.Slot,
                            ["expression"] = index.Expression,
                            ["unique"] = index.Unique,
                            ["keyCount"] = (int)index.KeyCount,
                            ["uniqueKeyCount"] = (int)index.UniqueKeyCount,
                            ["maxLevel"] = (int)index.MaxLevel
                        };
                    }
                }

                if(isNew)
                {
                    transaction.Dispose();
                }
            }
        }
    }
}