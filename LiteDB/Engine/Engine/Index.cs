using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Create a new index (or do nothing if already exists) to a collection/field
        /// </summary>
        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (name.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(name));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (expression.IsIndexable == false) throw new ArgumentException("Index expressions must contains at least one document field. Used methods must be immutable. Parameters are not supported.", nameof(expression));

            if (name.Length > INDEX_NAME_MAX_LENGTH) throw LiteException.InvalidIndexName(name, collection, "MaxLength = " + INDEX_NAME_MAX_LENGTH);
            if (!name.IsWord()) throw LiteException.InvalidIndexName(name, collection, "Use only [a-Z$_]");
            if (name.StartsWith("$")) throw LiteException.InvalidIndexName(name, collection, "Index name can't starts with `$`");

            if (name == "_id") return false; // always exists

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
                var data = new DataService(snapshot);

                // check if index already exists
                var current = col.GetCollectionIndex(name);

                // if already exists, just exit
                if (current != null)
                {
                    // but if expression are different, throw error
                    if (current.Expression != expression.Source) throw LiteException.IndexAlreadyExist(name);

                    return false;
                }

                LOG($"create index `{collection}.{name}`", "COMMAND");

                // create index head
                var index = indexer.CreateIndex(name, expression.Source, unique);
                var count = 0u;

                // read all objects (read from PK index)
                foreach (var pkNode in new IndexAll("_id", LiteDB.Query.Ascending).Run(col, indexer))
                {
                    using (var reader = new BufferReader(data.Read(pkNode.DataBlock)))
                    {
                        var doc = reader.ReadDocument(expression.Fields);

                        // first/last node in this document that will be added
                        IndexNode last = null;
                        IndexNode first = null;

                        // get values from expression in document
                        var keys = expression.Execute(doc);

                        // adding index node for each value
                        foreach (var key in keys)
                        {
                            // when index key is an array, get items inside array.
                            // valid only for first level (if this items are another array, this arrays will be indexed as array)
                            if (key.IsArray)
                            {
                                var arr = key.AsArray;

                                foreach(var itemKey in arr)
                                {
                                    // insert new index node
                                    var node = indexer.AddNode(index, itemKey, pkNode.DataBlock, last);

                                    if (first == null) first = node;

                                    last = node;

                                    count++;
                                }
                            }
                            else
                            {
                                // insert new index node
                                var node = indexer.AddNode(index, key, pkNode.DataBlock, last);

                                if (first == null) first = node;

                                last = node;

                                count++;
                            }
                        }

                        // fix single linked-list in pkNode
                        if (first != null)
                        {
                            last.SetNextNode(pkNode.NextNode);
                            pkNode.SetNextNode(first.Position);
                        }
                    }

                    transaction.Safepoint();
                }

                index.KeyCount = count;

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

            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, false);
                var col = snapshot.CollectionPage;
                var indexer = new IndexService(snapshot);
            
                // no collection, no index
                if (col == null) return false;
            
                // search for index reference
                var index = col.GetCollectionIndex(name);
            
                // no index, no drop
                if (index == null) return false;

                // delete all data pages + indexes pages
                indexer.DropIndex(index);

                // remove index entry in collection page
                snapshot.CollectionPage.DeleteCollectionIndex(name);
            
                return true;
            });
        }
    }
}