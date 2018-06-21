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
            // do not accept any command after shutdown database
            if (_shutdown) throw LiteException.DatabaseShutdown();

            var cols = collections == null || collections.Length == 0 ? _header.Collections.Keys.ToArray() : collections;
            var count = 0;

            _log.Info("analyze collections " + string.Join("', '", collections));

            foreach (var collection in cols)
            {
                var dict = new Dictionary<string, uint>();

                // create one transaction per colection to avoid lock all database
                this.AutoTransaction(transaction =>
                {
                    // first, get read-only snapshot
                    var snapshot = transaction.CreateSnapshot(SnapshotMode.Read, collection, false);
                    var col = snapshot.CollectionPage;

                    if (col == null) return 0;

                    var indexer = new IndexService(snapshot);
                    var indexes = col.GetIndexes(true).ToArray();

                    foreach (var index in indexes)
                    {
                        // if unique index, 1 document = 1 unique key
                        if (index.Unique)
                        {
                            dict[index.Name] = index.KeyCount;
                            continue;
                        }

                        var last = BsonValue.MinValue;
                        var counter = 0u;

                        foreach (var node in indexer.FindAll(index, LiteDB.Query.Ascending))
                        {
                            counter += node.Key.Equals(last) ? 0u : 1u;
                            last = node.Key;
                        }

                        dict[index.Name] = counter;
                    }

                    // after do all analyze, update snapshot to write mode
                    snapshot.WriteMode(false);

                    foreach (var index in indexes)
                    {
                        if (dict.TryGetValue(index.Name, out var counter))
                        {
                            index.UniqueKeyCount = counter;
                        }
                    }

                    snapshot.SetDirty(col);

                    return ++count;
                });
            }

            return count;
        }
    }
}