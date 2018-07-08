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
        /// Reduce disk size re-arranging unused spaces. Can change password.
        /// </summary>
        public long Shrink(string password = null)
        {
            _log.Info("shrink datafile" + (password != null ? " with password" : ""));

            var originalSize = _dataFile.Length;

            // shrink works with a temp engine that will use same wal file name as current datafile
            // after copy all data from current datafile to temp datafile (all data will be in WAL)
            // run checkpoint in current database

            _locker.EnterReserved(true);
            
            try
            {
                this.WaitAsyncWrite();

                // first do checkpoint with WAL delete
                _wal.Checkpoint(true, _header, false);

                using (var walStream = _settings.GetDiskFactory().GetWalFileStream(true))
                {
                    var s = new EngineSettings
                    {
                        DataStream = new MemoryStream(),
                        WalStream = walStream,
                        CheckpointOnShutdown = false
                    };

                    DEBUG(s.WalStream.Length > 0, "WAL must be an empty stream here");

                    // temp datafile
                    using (var temp = new LiteEngine(s))
                    {
                        // get all indexes
                        var indexes = this.SysIndexes().ToArray();

                        // init transaction in temp engine
                        var transactionID = temp.BeginTrans();

                        foreach (var collection in this.GetCollectionNames())
                        {
                            // first create all user indexes (exclude _id index)
                            foreach (var index in indexes.Where(x => x["collection"] == collection && x["slot"].AsInt32 > 0))
                            {
                                temp.EnsureIndex(collection,
                                    index["name"].AsString,
                                    BsonExpression.Create(index["expression"].AsString),
                                    index["unique"].AsBoolean);
                            }

                            // get all documents from current collection
                            var docs = this.Query(collection).ToEnumerable();

                            // and insert into 
                            temp.Insert(collection, docs, BsonAutoId.ObjectId);
                        }

                        // update header page and create another fake-transaction
                        temp._header.CreationTime = _header.CreationTime;
                        temp._header.CommitCounter = _header.CommitCounter;
                        temp._header.LastCommit = _header.LastCommit;
                        temp._header.UserVersion = _header.UserVersion;

                        if (indexes.Length == 0)
                        {
                            // if there is no collection, force commit only header page 
                            // by default, commit() will only store confirm page if there is any changed page
                            temp._header.TransactionID = transactionID;

                            temp._wal.ConfirmTransaction(temp._header, new List<PagePosition>());
                        }
                        else
                        {
                            temp.Commit();
                        }

                        // add this commited transaction as confirmed transaction in current datafile (to do checkpoint after)
                        _wal.ConfirmedTransactions.Add(transactionID);
                    }
                }

                // must empty main datafile cache
                _dataFile.Cache.Clear();

                // this checkpoint will use WAL file from temp database and will override all datafile pages
                _wal.Checkpoint(true, _header, false);

                // must reload header page because current _header has complete different pageIDs for collections
                _header = _dataFile.ReadPage(0, true) as HeaderPage;

                // if datafile grow (it's possible because index flipcoin can change) return 0
                return Math.Max(0, originalSize - _dataFile.Length);
            }
            finally
            {
                _locker.ExitReserved(true);
            }
        }
    }
}
