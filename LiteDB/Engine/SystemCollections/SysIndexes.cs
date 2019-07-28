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
            var transaction = _monitor.GetTransaction(true, out var isNew);

            try
            {
                // encapsulate all execution to catch any error
                return GetIndexes();
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Rollback();
                throw;
            }

            IEnumerable<BsonDocument> GetIndexes()
            {
                foreach (var collection in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, collection.Key, false);

                    foreach(var index in snapshot.CollectionPage.GetCollectionIndexes())
                    {
                        yield return new BsonDocument
                        {
                            ["collection"] = collection.Key,
                            ["name"] = index.Name,
                            ["expression"] = index.Expression,
                            ["unique"] = index.Unique,
                            ["keyCount"] = (int)index.KeyCount,
                            ["uniqueKeyCount"] = (int)index.UniqueKeyCount,
                            ["maxLevel"] = (int)index.MaxLevel,
                            ["lastAnalyzed"] = snapshot.CollectionPage.LastAnalyzed
                        };
                    }
                }

                if (isNew)
                {
                    transaction.Commit();
                }
            }
        }
    }
}