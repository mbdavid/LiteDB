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
        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique, LiteTransaction trans)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (!CollectionIndex.IndexNamePattern.IsMatch(name)) throw new ArgumentException("Invalid field format pattern: " + CollectionIndex.IndexNamePattern.ToString(), "field");
            if (name == "_id") return false; // always exists
            if (expression == null || expression?.Source?.Length > 200) throw new ArgumentException("expression is limited in 200 characters", "expression");

            return trans.CreateSnapshot(SnapshotMode.Write, collection, true, snapshot =>
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
                foreach (var pkNode in new QueryAll("_id", Query.Ascending).Run(col, indexer))
                {
                    // read binary and deserialize document
                    var buffer = data.Read(pkNode.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;
                    var expr = new BsonExpression(index.Expression);

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

                    trans.Safepoint();
                }

                return true;
            });
        }

        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string collection, string name)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            if (name == "_id") throw LiteException.IndexDropId();

            return false;
            //return this.WriteTransaction(TransactionMode.Write, collection, true, trans =>
            //{
            //    var col = trans.CollectionPage;
            //
            //    // no collection, no index
            //    if (col == null) return false;
            //
            //    // search for index reference
            //    var index = col.GetIndex(name);
            //
            //    // no index, no drop
            //    if (index == null) return false;
            //
            //    // delete all data pages + indexes pages
            //    trans.Indexer.DropIndex(index);
            //
            //    // clear index reference
            //    index.Clear();
            //
            //    // mark collection page as dirty
            //    trans.Pager.SetDirty(col);
            //
            //    return true;
            //});
        }
    }
}