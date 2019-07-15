using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Fill current with data inside file reader - run inside a transacion
        /// </summary>
        internal void Rebuild(IFileReader reader)
        {
            // get all indexes
            var indexes = reader.GetIndexes().ToArray();

            // begin transaction and get TransactionID
            var transaction = this.GetTransaction(true, out var isNew);

            try
            {
                var transactionID = transaction.TransactionID;

                foreach (var collection in reader.GetCollections())
                {
                    // first create all user indexes (exclude _id index)
                    foreach (var index in indexes.Where(x => x.Collection == collection && x.Name != "_id"))
                    {
                        this.EnsureIndex(collection,
                            index.Name,
                            BsonExpression.Create(index.Expression),
                            index.Unique);
                    }

                    // get all documents from current collection
                    var docs = reader.GetDocuments(indexes.Single(x => x.Collection == collection && x.Name == "_id"));

                    // and insert into 
                    this.Insert(collection, docs, BsonAutoId.ObjectId);
                }

                // update user version on commit
                transaction.Pages.Commit += h => h.UserVersion = reader.UserVersion;

                this.Commit();
            }
            catch
            {
                this.Rollback();

                throw;
            }

        }
    }
}