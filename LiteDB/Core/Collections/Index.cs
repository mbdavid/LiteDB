using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="options">All index options</param>
        public bool EnsureIndex(string field, IndexOptions options)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");
            if (options == null) throw new ArgumentNullException("options");
            if (field == "_id") return false; // always exists

            if (!CollectionIndex.IndexPattern.IsMatch(field)) throw LiteException.InvalidFormat("IndexField", field);

            lock (_locker)
            {
                // start transaction
                this.Database.Transaction.Begin();

                try
                {
                    // do not create collection at this point
                    var col = this.GetCollectionPage(false);

                    // check if index already exists (if collection exists)
                    if (col != null && col.GetIndex(field) != null)
                    {
                        this.Database.Transaction.Commit();
                        return false;
                    }

                    // if not exists collection yet, create a new now
                    if (col == null)
                    {
                        col = this.Database.Collections.Add(this.Name);
                        _pageID = col.PageID;
                    }

                    // create index head
                    var index = this.Database.Indexer.CreateIndex(col);

                    this.EnsureIndex(col, field, options);

                    this.Database.Transaction.Commit();

                    return true;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits
        /// </summary>
        /// <param name="field">Document field name (case sensitive)</param>
        /// <param name="unique">All index options</param>
        public bool EnsureIndex(string field, bool unique = false)
        {
            return this.EnsureIndex(field, new IndexOptions { Unique = unique });
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="unique">Create a unique values index?</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            var field = _visitor.GetBsonField(property);

            return this.EnsureIndex(field, new IndexOptions { Unique = unique });
        }

        /// <summary>
        /// Create a new permanent index in all documents inside this collections if index not exists already.
        /// </summary>
        /// <param name="property">Property linq expression</param>
        /// <param name="options">Use all indexes options</param>
        public bool EnsureIndex<K>(Expression<Func<T, K>> property, IndexOptions options)
        {
            var field = _visitor.GetBsonField(property);

            return this.EnsureIndex(field, options);
        }

        /// <summary>
        /// Returns all indexes in this collections
        /// </summary>
        public IEnumerable<BsonDocument> GetIndexes()
        {
            this.Database.Transaction.AvoidDirtyRead();

            var col = this.GetCollectionPage(false);

            if (col == null) yield break;

            foreach(var index in col.GetIndexes(true))
            {
                yield return new BsonDocument()
                    .Add("slot", index.Slot)
                    .Add("field", index.Field)
                    .Add("options", this.Database.Mapper.Serialize(index.Options));
            }
        }

        /// <summary>
        /// Get an index or create a new one if not exists - if no collection, no need create an index - no lock, no trans
        /// </summary>
        internal CollectionIndex GetOrCreateIndex(string field)
        {
            // get collection page that contains all indexes
            var col = this.GetCollectionPage(false);

            // no collection, no index
            if(col == null) return null;

            // get index
            var index = col.GetIndex(field);

            // if index not found, lets check if type T has [BsonIndex] with custom options
            if (index == null && typeof(T) != typeof(BsonDocument))
            {
                var options = this.Database.Mapper.GetIndexFromMapper<T>(field);

                // create a new index using BsonIndex options
                if (options != null)
                {
                    index = this.EnsureIndex(col, field, options);
                }
            }

            // if no index, let's auto create an index with default index options
            if (index == null)
            {
                index = this.EnsureIndex(col, field, new IndexOptions());
            }

            return index;
        }

        /// <summary>
        /// Internal implementation of create an index - no locks, no trans
        /// </summary>
        private CollectionIndex EnsureIndex(CollectionPage col, string field, IndexOptions options)
        {
            // create index head
            var index = this.Database.Indexer.CreateIndex(col);

            index.Field = field;
            index.Options = options;

            // read all objects (read from PK index)
            foreach (var node in new QueryAll("_id", Query.Ascending).Run(this))
            {
                var dataBlock = this.Database.Data.Read(node.DataBlock, true);

                // read object
                var doc = BsonSerializer.Deserialize(dataBlock.Buffer).AsDocument;

                // adding index
                var key = doc.Get(field);

                var newNode = this.Database.Indexer.AddNode(index, key);

                // adding this new index Node to indexRef
                dataBlock.IndexRef[index.Slot] = newNode.Position;

                // link index node to datablock
                newNode.DataBlock = dataBlock.Position;

                // mark datablock page as dirty
                this.Database.Pager.SetDirty(dataBlock.Page);
            }

            return index;
        }

        /// <summary>
        /// Drop index and release slot for another index
        /// </summary>
        public bool DropIndex(string field)
        {
            if (string.IsNullOrEmpty(field)) throw new ArgumentNullException("field");
            if (field == "_id") throw LiteException.IndexDropId();

            var col = this.GetCollectionPage(false);

            // no collection, no index
            if(col == null) return false;

            // search for index reference
            var index = col.GetIndex(field);

            // no index, no drop
            if (index == null) return false;

            lock (_locker)
            {
                // start transaction
                this.Database.Transaction.Begin();

                try
                {
                    this.DropIndex(col, index);

                    this.Database.Transaction.Commit();

                    return true;
                }
                catch
                {
                    this.Database.Transaction.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Internal implementation of drop an index - no lock, no trans
        /// </summary>
        private void DropIndex(CollectionPage col, CollectionIndex index)
        {
            // delete all data pages + indexes pages
            this.Database.Indexer.DropIndex(index);

            // clear index reference
            index.Clear();

            // save collection page
            this.Database.Pager.SetDirty(col);
        }
    }
}
