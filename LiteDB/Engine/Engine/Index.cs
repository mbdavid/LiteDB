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
        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (!CollectionIndex.IndexNamePattern.IsMatch(name)) throw new ArgumentException("Invalid field format pattern: " + CollectionIndex.IndexNamePattern.ToString(), "field");
            if (name == "_id") return false; // always exists
            if (expression == null || expression?.Source?.Length > 200) throw new ArgumentException("expression is limited in 200 characters", "expression");

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.GetOrAdd(collection);

                // check if index already exists
                var current = col.GetIndex(name);

                // if already exists, just exit
                if (current != null)
                {
                    // do not test any difference between current index and new definition
                    return false;
                }

                // lock collection
                trans.WriteLock(collection);

                // create index head
                var index = trans.Indexer.CreateIndex(col);

                index.Name = name;
                index.Expression = expression.Source;
                index.Unique = unique;

                // read all objects (read from PK index)
                foreach (var pkNode in new QueryAll("_id", Query.Ascending).Run(col, trans.Indexer))
                {
                    // read binary and deserialize document
                    var buffer = trans.Data.Read(pkNode.DataBlock);
                    var doc = _bsonReader.Deserialize(buffer).AsDocument;
                    var expr = new BsonExpression(index.Expression);

                    // get values from expression in document
                    var keys = expr.Execute(doc, true);

                    // adding index node for each value
                    foreach (var key in keys)
                    {
                        // insert new index node
                        var node = trans.Indexer.AddNode(index, key, pkNode);

                        // link index node to datablock
                        node.DataBlock = pkNode.DataBlock;
                    }
                }

                // persist changes
                trans.Commit();

                return true;
            }
        }

        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string collection, string name)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));

            if (name == "_id") throw LiteException.IndexDropId();

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.Get(collection);

                // no collection, no index
                if (col == null) return false;

                // search for index reference
                var index = col.GetIndex(name);

                // no index, no drop
                if (index == null) return false;

                // lock collection for write operations
                trans.WriteLock(collection);

                // delete all data pages + indexes pages
                trans.Indexer.DropIndex(index);

                // clear index reference
                index.Clear();

                // mark collection page as dirty
                trans.SetDirty(col);

                // persist changes
                trans.Commit();

                return true;
            }
        }

        /// <summary>
        /// List all indexes inside a collection
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            using (var trans = this.BeginTrans())
            {
                var col = trans.Collection.Get(collection);

                if (col == null) yield break;

                foreach (var index in col.GetIndexes(true))
                {
                    yield return new IndexInfo(index);
                }
            }
        }
    }
}