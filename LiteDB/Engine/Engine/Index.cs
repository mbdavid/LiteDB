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
        public bool EnsureIndex(string collection, string field, bool unique = false)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException("field");

            return this.Transaction<bool>(collection, true, (col) =>
            {
                // check if index already exists
                var current = col.GetIndex(field);

                // if already exists, checks if changed unique
                if (current != null)
                {
                    // change from unique to non-unique (just update)
                    if (current.Unique == true && unique == false)
                    {
                        current.Unique = false;
                        _pager.SetDirty(col);
                        return true;
                    }
                    // change from non-unique to unique (need be re-created)
                    else if (current.Unique == false && unique == true)
                    {
                        _indexer.DropIndex(current);
                        current.Clear();
                    }
                    else
                    {
                        return false; // no changes
                    }
                }

                _log.Write(Logger.COMMAND, "create index on '{0}' :: '{1}' unique: {2}", collection, field, unique);

                // create index head
                var index = _indexer.CreateIndex(col);

                index.Field = field;
                index.Unique = unique;

                // read all objects (read from PK index)
                foreach (var pkNode in new QueryAll("_id", Query.Ascending).Run(col, _indexer))
                {
                    // read binary and deserialize document
                    var buffer = _data.Read(pkNode.DataBlock);
                    var doc = BsonSerializer.Deserialize(buffer).AsDocument;

                    // get distinct values from field in document
                    var keys = doc.GetValues(field, true, unique);

                    // adding index node for each value
                    foreach (var key in keys)
                    {
                        // insert new index node
                        var node = _indexer.AddNode(index, key, pkNode);

                        // link index node to datablock
                        node.DataBlock = pkNode.DataBlock;
                    }

                    // check memory usage
                    _trans.CheckPoint();
                }

                return true;
            });
        }

        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string collection, string field)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException("field");

            if (field == "_id") throw LiteException.IndexDropId();

            return this.Transaction<bool>(collection, false, (col) =>
            {
                // no collection, no index
                if (col == null) return false;

                // search for index reference
                var index = col.GetIndex(field);

                // no index, no drop
                if (index == null) return false;

                _log.Write(Logger.COMMAND, "drop index on '{0}' :: '{1}'", collection, field);

                // delete all data pages + indexes pages
                _indexer.DropIndex(index);

                // clear index reference
                index.Clear();

                // mark collection page as dirty
                _pager.SetDirty(col);

                return true;
            });
        }

        /// <summary>
        /// List all indexes inside a collection
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes(string collection)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");

            using (_locker.Shared())
            {
                var col = this.GetCollectionPage(collection, false);

                if (col == null) yield break;

                foreach (var index in col.GetIndexes(true))
                {
                    yield return new IndexInfo(index);
                }
            }
        }
    }
}