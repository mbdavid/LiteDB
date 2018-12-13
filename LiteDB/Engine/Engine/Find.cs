using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns all documents inside a collection. Do not use Index
        /// </summary>
        public IEnumerable<BsonDocument> FindAll(string collection, params string[] fields)
        {
            var transaction = this.GetTransaction(true, out var isNew);

            try
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);
                var data = new DataService(snapshot);
                var f = fields.Length > 0 ? new HashSet<string>(fields) : null;

                if (snapshot.CollectionPage == null) yield break;

                for (var slot = 0; slot < 5; slot++)
                {
                    var next = snapshot.CollectionPage.FreeDataPageID[slot];

                    while (next != uint.MaxValue)
                    {
                        var page = snapshot.GetPage<DataPage>(next);

                        foreach (var block in page.GetBlocks())
                        {
                            using (var r = new BufferReader(data.Read(block)))
                            {
                                var doc = r.ReadDocument(f);

                                yield return doc;
                            }
                        }

                        next = page.NextPageID;

                        transaction.Safepoint();
                    }
                }
            }
            finally
            {
                // new transactions must commit before leave
                if (isNew)
                {
                    transaction.Commit();
                }
            }
        }
    }
}