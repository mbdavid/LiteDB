using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Recreate database using empty LOG file to re-write all documents with all indexes
        /// </summary>
        public long Rebuild(RebuildOptions options)
        {
            _walIndex.Checkpoint(false);

            if (_disk.GetLength(FileOrigin.Log) > 0) throw new LiteException(0, "Rebuild operation requires no log file - run Checkpoint before continue");

            _locker.EnterReserved(true);

            var originalLength = _disk.GetLength(FileOrigin.Data);

            // create a savepoint in header page - restore if any error occurs
            var savepoint = _header.Savepoint();

            // must clear all cache pages because all of them will change
            _disk.Cache.Clear();

            try
            {
                // initialize V8 file reader
                var reader = new FileReaderV8(_header, _disk);

                // clear current header
                _header.FreeEmptyPageList = uint.MaxValue;
                _header.LastPageID = 0;
                _header.GetCollections().ToList().ForEach(c => _header.DeleteCollection(c.Key));

                // override collation pragma
                if (options?.Collation != null)
                {
                    _header.Pragmas.Set(Pragmas.COLLATION, options.Collation.ToString(), false);
                }

                // rebuild entrie database using FileReader
                this.RebuildContent(reader);

                // change password (can be a problem if any error occurs after here)
                if (options != null)
                {
                    _disk.ChangePassword(options.Password, _settings);
                }

                // exit reserved before checkpoint
                _locker.ExitReserved(true);

                // do checkpoint
                _walIndex.Checkpoint(false);

                // set new fileLength
                _disk.SetLength((_header.LastPageID + 1) * PAGE_SIZE, FileOrigin.Data);

                // get new filelength to compare
                var newLength = _disk.GetLength(FileOrigin.Data);

                return originalLength - newLength;
            }
            catch
            {
                _header.Restore(savepoint);

                _locker.ExitReserved(true);

                throw;
            }
        }

        /// <summary>
        /// Fill current database with data inside file reader - run inside a transacion
        /// </summary>
        internal void RebuildContent(IFileReader reader)
        {
            // begin transaction and get TransactionID
            var transaction = _monitor.GetTransaction(true, out var isNew);

            try
            {
                var transactionID = transaction.TransactionID;

                foreach (var collection in reader.GetCollections())
                {
                    // first create all user indexes (exclude _id index)
                    foreach (var index in reader.GetIndexes(collection))
                    {
                        this.EnsureIndex(collection,
                            index.Name,
                            BsonExpression.Create(index.Expression),
                            index.Unique);
                    }

                    // get all documents from current collection
                    var docs = reader.GetDocuments(collection);

                    // and insert into 
                    this.Insert(collection, docs, BsonAutoId.ObjectId);
                }

                this.Commit();
            }
            catch (Exception)
            {
                this.Rollback();

                _walIndex.Clear();

                throw;
            }
        }
    }
}