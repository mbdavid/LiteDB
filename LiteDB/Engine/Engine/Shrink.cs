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
                    using (var engine = new LiteEngine(s))
                    {
                        // get all indexes
                        var indexes = this.SysIndexes().ToArray();

                        // init transaction in temp engine
                        var transactionID = engine.BeginTrans();

                        foreach (var collection in this.GetCollectionNames())
                        {
                            // first create all user indexes (exclude _id index)
                            foreach (var index in indexes.Where(x => x["collection"] == collection && x["slot"].AsInt32 > 0))
                            {
                                engine.EnsureIndex(collection,
                                    index["name"].AsString,
                                    BsonExpression.Create(index["expression"].AsString),
                                    index["unique"].AsBoolean);
                            }

                            // get all documents from current collection
                            var docs = this.Query(collection).ToEnumerable();

                            // and insert into 
                            engine.Insert(collection, docs, BsonAutoId.ObjectId);
                        }

                        // update header page and create another fake-transaction
                        engine._header.CreationTime = _header.CreationTime;
                        engine._header.CommitCounter = _header.CommitCounter;
                        engine._header.LastCommit = _header.LastCommit;
                        engine._header.UserVersion = _header.UserVersion;

                        // commit all temp database
                        engine.Commit();

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
