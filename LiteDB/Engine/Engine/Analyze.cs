using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Analyze collection indexes to update UniqueKey counter. If collections parameter are null, analyze all collections
        /// </summary>
        public int Analyze(string[] collections)
        {
            // collection analyze is possible only in exclusive transaction for this
            if (_locker.IsInTransaction) throw LiteException.AlreadyExistsTransaction();

            var cols = collections == null || collections.Length == 0 ? _header.GetCollections().Select(x => x.Key).ToArray() : collections;
            var count = 0;

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

                    LOG($"analyze `{collection}`", "COMMAND");

                    var indexer = new IndexService(snapshot);
                    var indexes = snapshot.CollectionPage.GetCollectionIndexes().ToArray();

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
                    snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);

                    foreach (var name in indexes.Select(x => x.Name))
                    {
                        // will get index and set as dirty
                        var index = snapshot.CollectionPage.UpdateCollectionIndex(name);

                        index.KeyCount = keyCount[index.Name];
                        index.UniqueKeyCount = keyUniqueCount[index.Name];
                    }

                    snapshot.CollectionPage.LastAnalyzed = DateTime.Now;

                    snapshot.CollectionPage.IsDirty = true;

                    return ++count;
                });
            }

            return count;
        }
    }
}