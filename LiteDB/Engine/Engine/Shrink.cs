using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Reduce disk size re-arranging unused spaces.
        /// </summary>
        public long Shrink()
        {
            return this.Shrink(new FileReaderV8(this, _header));
        }

        /// <summary>
        /// Run shrink operation using an file reader interface (can be used as Upgrade datafile)
        /// </summary>
        private long Shrink(IFileReader reader)
        {
            throw new NotImplementedException(); /*
            var originalSize = _dataFile.Length;

            // shrink can only run with no transaction
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("Shrink", TransactionState.Active);

            // shrink works with a temp engine that will use same log file
            // after copy all data from current datafile to temp datafile (all data will be in log)
            // run checkpoint in current database

            _locker.EnterReserved(true);
            
            try
            {
                // first do checkpoint with WAL delete
                _wal.Checkpoint(_header, false);

                var s = new EngineSettings
                {
                    DataStream = new MemoryStream(),
                    LogStream = _wal.LogFile.Stream,
                    CheckpointOnShutdown = false
                };

                DEBUG(s.LogStream.Length > 0, "LOG file must be an empty stream here");

                // temp datafile
                using (var temp = new LiteEngine(s))
                {
                    // get all indexes
                    var indexes = reader.GetIndexes().ToArray();

                    // begin transaction and get TransactionID
                    var transaction = temp.GetTransaction(true, out var isNew);

                    try
                    {
                        foreach (var collection in reader.GetCollections())
                        {
                            // first create all user indexes (exclude _id index)
                            foreach (var index in indexes.Where(x => x.Collection == collection && x.Name != "_id"))
                            {
                                temp.EnsureIndex(collection,
                                    index.Name,
                                    BsonExpression.Create(index.Expression),
                                    index.Unique);
                            }

                            // get all documents from current collection
                            var docs = reader.GetDocuments(indexes.Single(x => x.Collection == collection && x.Name == "_id"));

                            // and insert into 
                            temp.Insert(collection, docs, BsonAutoId.ObjectId);
                        }

                        // update header page and create another fake-transaction
                        temp._header.CreationTime = reader.CreationTime;
                        temp._header.UserVersion = reader.UserVersion;

                        if (indexes.Length == 0)
                        {
                            // if there is no collection, force commit only header page 
                            // by default, commit() will only store confirm page if there is any changed page
                            temp._header.TransactionID = transaction.TransactionID;
                            temp._header.IsConfirmed = true;
                            temp._header.IsDirty = true;
                            temp._wal.LogFile.WritePages(new[] { temp._header }, null);

                            temp._wal.ConfirmTransaction(transaction.TransactionID, new PagePosition[0]);
                        }
                        else
                        {
                            temp.Commit();
                        }

                        // add this commited transaction as confirmed transaction in current datafile (to do checkpoint after)
                        _wal.ConfirmedTransactions.Add(transaction.TransactionID);

                    }
                    finally
                    {
                        transaction.Release();
                    }
                }

                // this checkpoint will use log file from temp database and will override all datafile pages
                _wal.Checkpoint(_header, false);

                // must reload header page because current _header has complete different pageIDs for collections
                _header = _dataFile.ReadPage(0) as HeaderPage;

                // if datafile grow (it's possible because index flipcoin can change) return 0
                return Math.Max(0, originalSize - _dataFile.Length);
            }
            finally
            {
                _locker.ExitReserved(true);
            }*/
        }
    }
}
