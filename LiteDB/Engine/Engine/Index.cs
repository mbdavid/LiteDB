using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Create a new index (or do nothing if already exists) to a collection/field
        /// </summary>
        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            if (!CollectionIndex.IndexNamePattern.IsMatch(name)) throw LiteException.InvalidIndexName(name, collection);
            if (name == "_id") return false; // always exists

            return transaction.CreateSnapshot(SnapshotMode.Write, collection, true, snapshot =>
            {
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                // check if index already exists
                var current = col.GetIndex(name);

                // if already exists, just exit
                if (current != null)
                {
                    // do not test any difference between current index and new definition
                    return false;
                }

                // create index head
                var index = indexer.CreateIndex(col);

                index.Name = name;
                index.Expression = expression.Source;
                index.Unique = unique;

                // test if this new name/expression fit on PAGE_SIZE
                col.CalculateNameSize();

                // read all objects (read from PK index)
                foreach (var pkNode in new IndexAll("_id", LiteDB.Query.Ascending).Run(col, indexer))
                {
                    // read binary and deserialize document
                    var buffer = data.Read(pkNode.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;
                    var expr = BsonExpression.Create(index.Expression);

                    // get values from expression in document
                    var keys = expr.Execute(doc, true);

                    // adding index node for each value
                    foreach (var key in keys)
                    {
                        // insert new index node
                        var node = indexer.AddNode(index, key, pkNode);

                        // link index node to datablock
                        node.DataBlock = pkNode.DataBlock;
                    }

                    transaction.Safepoint();
                }

                return true;
            });
        }

        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string collection, string name, LiteTransaction transaction)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            if (name == "_id") throw LiteException.IndexDropId();

            return transaction.CreateSnapshot(SnapshotMode.Write, collection, true, snapshot =>
            {
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
            
                // no collection, no index
                if (col == null) return false;
            
                // search for index reference
                var index = col.GetIndex(name);
            
                // no index, no drop
                if (index == null) return false;

                // delete all data pages + indexes pages
                indexer.DropIndex(index);
            
                // clear index reference
                index.Clear();
            
                // mark collection page as dirty
                snapshot.SetDirty(col);
            
                return true;
            });
        }
    }
}