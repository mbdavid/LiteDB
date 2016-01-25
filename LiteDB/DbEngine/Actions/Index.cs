using System;
using System.Collections.Generic;
using System.Linq;

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

                _log.Write(Logger.COMMAND, "create index on '{0}' :: '{1}' unique: {2}", colName, field, options.Unique);

                // create index head
                var index = _indexer.CreateIndex(col);

                index.Field = field;
                index.Options = options;

                // read all objects (read from PK index)
                foreach (var node in new QueryAll("_id", Query.Ascending).Run(col, _indexer))
                {
                    var buffer = _data.Read(node.DataBlock);
                    var dataBlock = _data.GetBlock(node.DataBlock);

                    // mark datablock page as dirty
                    _pager.SetDirty(dataBlock.Page);

                    // read object
                    var doc = BsonSerializer.Deserialize(buffer).AsDocument;

                    // adding index
                    var key = doc.Get(field);

                    var newNode = _indexer.AddNode(index, key);

                    // adding this new index Node to indexRef
                    dataBlock.IndexRef[index.Slot] = newNode.Position;

                    // link index node to datablock
                    newNode.DataBlock = dataBlock.Position;

                    _cache.CheckPoint();
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

                // mark collection page as dirty before changes
                _pager.SetDirty(col);

                // search for index reference
                var index = col.GetIndex(field);

                // no index, no drop
                if (index == null) return false;

                _log.Write(Logger.COMMAND, "drop index on '{0}' :: '{1}'", colName, field);

                // delete all data pages + indexes pages
                _indexer.DropIndex(index);

                // clear index reference
                index.Clear();

                return true;
            });
        }

        /// <summary>
        /// List all indexes inside a collection
        /// </summary>
        public IEnumerable<BsonDocument> GetIndexes(string colName, bool stats = false)
        {
            var col = this.GetCollectionPage(colName, false);

            if (col == null) yield break;

            foreach (var index in col.GetIndexes(true))
            {
                var doc = new BsonDocument()
                    .Add("slot", index.Slot)
                    .Add("field", index.Field)
                    .Add("options", new BsonDocument()
                        .Add("unique", index.Options.Unique)
                        .Add("ignoreCase", index.Options.IgnoreCase)
                        .Add("removeAccents", index.Options.RemoveAccents)
                        .Add("trimWhitespace", index.Options.TrimWhitespace)
                        .Add("emptyStringToNull", index.Options.EmptyStringToNull)
                    );

                if (stats)
                {
                    _cache.CheckPoint();

                    var pages = _indexer.FindAll(index, Query.Ascending).GroupBy(x => x.Page.PageID).Count();

                    // this command can be consume too many memory!! has no CheckPoint on loop
                    var keySize = pages == 0 ? 0 : _indexer.FindAll(index, Query.Ascending).Average(x => x.KeyLength);

                    doc.Add("stats", new BsonDocument()
                        .Add("pages", pages)
                        .Add("allocated", BasePage.GetSizeOfPages(pages))
                        .Add("keyAverageSize", (int)keySize)
                    );
                }

                yield return doc;
            }
        }
    }
}