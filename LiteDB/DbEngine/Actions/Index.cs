using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Create a new index (or do nothing if already exisits) to a collection/field
        /// </summary>
        public bool EnsureIndex(string colName, string field, IndexOptions options)
        {
            return this.Transaction<bool>(colName, true, (col) =>
            {
                // check if index already exists
                if (col.GetIndex(field) != null) return false;

                // create index head
                var index = _indexer.CreateIndex(col);

                index.Field = field;
                index.Options = options;

                // read all objects (read from PK index)
                foreach (var node in new QueryAll("_id", Query.Ascending).Run(col, _indexer))
                {
                    var dataBlock = _data.Read(node.DataBlock, true);

                    // read object
                    var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

                    // adding index
                    var key = doc.Get(field);

                    var newNode = _indexer.AddNode(index, key);

                    // adding this new index Node to indexRef
                    dataBlock.IndexRef[index.Slot] = newNode.Position;

                    // link index node to datablock
                    newNode.DataBlock = dataBlock.Position;

                    // mark datablock page as dirty
                    _pager.SetDirty(dataBlock.Page);
                }

                return true;
            });
        }

        /// <summary>
        /// Drop an index from a collection
        /// </summary>
        public bool DropIndex(string colName, string field)
        {
            if (field == "_id") throw LiteException.IndexDropId();

            return this.Transaction<bool>(colName, false, (col) =>
            {
                // no collection, no index
                if (col == null) return false;

                // search for index reference
                var index = col.GetIndex(field);

                // no index, no drop
                if (index == null) return false;

                // delete all data pages + indexes pages
                _indexer.DropIndex(index);

                // clear index reference
                index.Clear();

                // save collection page
                _pager.SetDirty(col);

                return true;
            });
        }

        /// <summary>
        /// List all indexes inside a collection
        /// </summary>
        public IEnumerable<BsonDocument> GetIndexes(string colName)
        {
            var col = this.GetCollectionPage(colName, false);

            if (col == null) yield break;

            foreach (var index in col.GetIndexes(true))
            {
                yield return new BsonDocument()
                    .Add("slot", index.Slot)
                    .Add("field", index.Field)
                    .Add("unique", index.Options.Unique)
                    .Add("ignoreCase", index.Options.IgnoreCase)
                    .Add("removeAccents", index.Options.RemoveAccents)
                    .Add("trimWhitespace", index.Options.TrimWhitespace)
                    .Add("emptyStringToNull", index.Options.EmptyStringToNull);
            }
        }
    }
}
