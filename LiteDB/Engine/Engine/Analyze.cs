using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Analyze collection indexes to update UniqueKey counter. If collections parameter are null, analyze all collections
        /// </summary>
        public int Analyze(string[] collections)
        {
            throw new NotImplementedException();
            /*
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("Analyze", TransactionState.Active);

            var cols = collections == null || collections.Length == 0 ? _header.Collections.Keys.ToArray() : collections;
            var count = 0;

            _log.Info("analyze collections " + string.Join("', '", collections));

            foreach (var collection in cols)
            {
                // counters for indexes
                var keyCount = new Dictionary<string, uint>();
                var keyUniqueCount = new Dictionary<string, uint>();

                // create one transaction per colection to avoid lock all database
                this.AutoTransaction(transaction =>
                {
                    // first, get read-only snapshot
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, collection, false);

                    // do not use "col" local variable because `WriteMode()` clear _collectionPage instance
                    if (snapshot.CollectionPage == null) return 0;

                    var indexer = new IndexService(snapshot);
                    var indexes = snapshot.CollectionPage.GetIndexes(true).ToArray();

                    foreach (var index in indexes)
                    {
                        var last = BsonValue.MinValue;
                        var counter = 0u;
                        var uniqueCounter = 0u;

                        foreach (var node in indexer.FindAll(index, LiteDB.Query.Ascending))
                        {
                            counter++;
                            uniqueCounter += node.Key == last ? 0u : 1u;
                            last = node.Key;
                        }

                        keyCount[index.Name] = counter;
                        keyUniqueCount[index.Name] = uniqueCounter;
                    }

                    // after do all analyze, update snapshot to write mode
                    snapshot.WriteMode(false);

                    foreach (var index in snapshot.CollectionPage.GetIndexes(true))
                    {
                        index.KeyCount = keyCount[index.Name];
                        index.UniqueKeyCount = keyUniqueCount[index.Name];
                    }

                    snapshot.CollectionPage.LastAnalyzed = DateTime.Now;

                    snapshot.SetDirty(snapshot.CollectionPage);

                    return ++count;
                });
            }

            return count;*/
        }
    }
}