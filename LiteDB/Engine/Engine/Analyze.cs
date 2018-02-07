using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Analyze collection indexes to update UniqueKey counter. If collections parameter are null, analyze all collections
        /// </summary>
        public void Analyze(string[] collections, LiteTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var cols = collections ?? _header.Collections.Keys.ToArray();

            foreach(var collection in cols)
            {
                var dict = new Dictionary<string, uint>();

                // first, only read collection all index keys from all indexes
                var found = transaction.CreateSnapshot(SnapshotMode.Read, collection, false, snapshot =>
                {
                    var col = snapshot.CollectionPage;

                    if (col == null) return false;

                    var indexer = new IndexService(snapshot);
                    var indexes = col.GetIndexes(true);

                    foreach(var index in indexes)
                    {
                        // if unique index, 1 document = 1 unique key
                        if (index.Unique)
                        {
                            dict[index.Name] = index.KeyCount;
                            continue;
                        }

                        var last = BsonValue.MinValue;
                        var counter = 0u;

                        foreach(var node in indexer.FindAll(index, Query.Ascending))
                        {
                            counter += node.Key.Equals(last) ? 0u : 1u;
                            last = node.Key;
                        }

                        dict[index.Name] = counter;
                    }

                    return true;
                });

                if (found == false) continue;

                // update each index in collection with counter
                transaction.CreateSnapshot(SnapshotMode.Write, collection, false, snapshot =>
                {
                    var col = snapshot.CollectionPage;

                    if (col == null) return false;

                    var indexes = col.GetIndexes(true);

                    foreach(var index in indexes)
                    {
                        if (dict.TryGetValue(index.Name, out var counter))
                        {
                            index.UniqueKeyCount = counter;
                        }
                    }

                    snapshot.SetDirty(col);

                    return true;
                });
            }
        }
    }
}