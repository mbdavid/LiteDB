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
        /// Reduce disk size re-arranging unused spaces - can change password or remove (using null password)
        /// </summary>
        public long Shrink(string password)
        {
            return this.Shrink(new FileReaderV8(this), password);
        }

        /// <summary>
        /// Run shrink operation using an file reader interface (can be used as Upgrade datafile)
        /// Can change/remove password (use null in password if want remove password)
        /// </summary>
        private long Shrink(IFileReader reader, string password)
        {
            // shrink can only run with no transaction
            if (_locker.IsInTransaction) throw LiteException.AlreadyExistsTransaction();

            // shrink works with a temp engine that will use same log file
            // after copy all data from current datafile to temp datafile (all data will be in log)
            // run checkpoint in current database
            
            // first do a complete checkpoint 
            _walIndex.Checkpoint();

            var originalSize = _disk.GetLength(FileOrigin.Data);
            var logLength = 0L;

            // just after enter
            _locker.EnterReserved(true);

            var s = new EngineSettings
            {
                DataStream = new MemoryStream(),
                LogStream = _disk.LogStream,
                Password = password
            };

            if(_disk.GetLength(FileOrigin.Log) != 0) throw new LiteException(0, "Invalid shrink command - log file are not empty");

            // create temp engine using same LOG file
            using (var temp = new LiteEngine(s))
            {
                // get all indexes
                var indexes = reader.GetIndexes().ToArray();

                // begin transaction and get TransactionID
                temp.AutoTransaction(transaction =>
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

                    temp.Commit();

                    return true;
                });

                // update DB userversion
                temp.DbParam(DB_PARAM_USERVERSION, reader.UserVersion);

                logLength = temp._disk.GetLength(FileOrigin.Log);
            }

            // force disk update log file
            _disk.SetLength(logLength, FileOrigin.Log);

            // now, restore index confirm transactions
            _walIndex.RestoreIndex(ref _header);

            // crop data file
            _disk.SetLength(BasePage.GetPagePosition(_header.LastPageID), FileOrigin.Data);

            // exit locker
            _locker.ExitReserved(true);

            // this checkpoint will use log file from temp database and will override all datafile pages
            _walIndex.Checkpoint();

            // if datafile grow (it's possible because index flipcoin can change) return 0
            return Math.Max(0, originalSize - _disk.GetLength(FileOrigin.Data));
        }
    }
}
