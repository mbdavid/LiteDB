using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            // get BsonDocument from object
            var doc = this.Database.Mapper.ToDocument(document);

            var id = doc["_id"];

            if (id.IsNull || id.IsMinValue || id.IsMaxValue) throw LiteException.InvalidDataType("_id", id);

            lock(_locker)
            {
                this.Database.Transaction.Begin();

                try
                {
                    var result = this.UpdateDocument(id, doc);

                    this.Database.Transaction.Commit();

                    return result;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            // get BsonDocument from object
            var doc = this.Database.Mapper.ToDocument(document);

            lock (_locker)
            {
                this.Database.Transaction.Begin();

                try
                {
                    var result = this.UpdateDocument(id, doc);

                    this.Database.Transaction.Commit();

                    return result;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Query documents and execute, for each document, action method. After action, update each document
        /// </summary>
        public int Update(Query query, Action<T> action)
        {
            if (query == null) throw new ArgumentNullException("query");
            if (action == null) throw new ArgumentNullException("action");

            lock(_locker)
            {
                this.Database.Transaction.Begin();

                try
                {
                    var docs = this.Find(query).ToArray(); // used to avoid changes during Action<T>
                    var count = 0;

                    foreach (var doc in docs)
                    {
                        action(doc);

                        // get BsonDocument from object
                        var bson = this.Database.Mapper.ToDocument(doc);

                        var id = bson["_id"];

                        if (id.IsNull || id.IsMinValue || id.IsMaxValue) throw LiteException.InvalidDataType("_id", id);

                        count += this.UpdateDocument(id, bson) ? 1 : 0;
                    }

                    this.Database.Transaction.Commit();

                    return count;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Query documents and execute, for each document, action method. All data is locked during execution
        /// </summary>
        public void Update(Expression<Func<T, bool>> predicate, Action<T> action)
        {
            this.Update(_visitor.Visit(predicate), action);
        }

        /// <summary>
        /// Internal implementation of Update a document (no lock, no transaction)
        /// </summary>
        private bool UpdateDocument(BsonValue id, BsonDocument doc)
        {
            // serialize object
            var bytes = BsonSerializer.Serialize(doc);

            var col = this.GetCollectionPage(false);

            // if no collection, no updates
            if (col == null) return false;

            // normalize id before find
            var value = id.Normalize(col.PK.Options);

            // find indexNode from pk index
            var indexNode = this.Database.Indexer.Find(col.PK, value, false, Query.Ascending);

            // if not found document, no updates
            if (indexNode == null) return false;

            // update data storage
            var dataBlock = this.Database.Data.Update(col, indexNode.DataBlock, bytes);

            // delete/insert indexes - do not touch on PK
            foreach (var index in col.GetIndexes(false))
            {
                var key = doc.Get(index.Field);

                var node = this.Database.Indexer.GetNode(dataBlock.IndexRef[index.Slot]);

                // check if my index node was changed
                if (node.Key.CompareTo(key) != 0)
                {
                    // remove old index node
                    this.Database.Indexer.Delete(index, node.Position);

                    // and add a new one
                    var newNode = this.Database.Indexer.AddNode(index, key);

                    // point my index to data object
                    newNode.DataBlock = dataBlock.Position;

                    // point my dataBlock
                    dataBlock.IndexRef[index.Slot] = newNode.Position;

                    this.Database.Pager.SetDirty(dataBlock.Page);
                }
            }

            return true;
        }
    }
}
