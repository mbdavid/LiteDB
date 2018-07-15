using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysCols()
        {
            var transaction = this.GetTransaction(true, out var isNew);

            try
            {
                // encapsulate all execution to catch any error
                return GetCollections();
            }
            catch
            {
                // if any error, rollback transaction
                transaction.Dispose();
                throw;
            }

            IEnumerable<BsonDocument> GetCollections()
            {
                foreach (var name in this.GetCollectionNames().OrderBy(x => x))
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Write, name, false);

                    yield return new BsonDocument
                    {
                        ["name"] = name,
                        ["creationTime"] = snapshot.CollectionPage.CreationTime
                    };
                }

                if(isNew)
                {
                    transaction.Dispose();
                }
            }
        }
    }
}