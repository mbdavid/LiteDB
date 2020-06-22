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
            // enter database in exclusive mode
            var mustExit = _locker.EnterExclusive();

            // get a header backup/savepoint before change
            PageBuffer savepoint = null;

            try
            {
                // do a checkpoint before starts
                _walIndex.Checkpoint();

                var originalLength = _disk.GetLength(FileOrigin.Data);

                // create a savepoint in header page - restore if any error occurs
                savepoint = _header.Savepoint();

                // must clear all cache pages because all of them will change
                _disk.Cache.Clear();

                // must check if there is no data log
                if (_disk.GetLength(FileOrigin.Log) > 0) throw new LiteException(0, "Rebuild operation requires no log file - run Checkpoint before continue");

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

                // do checkpoint
                _walIndex.Checkpoint();

                // override header page
                _disk.Write(new[] { _header.UpdateBuffer() }, FileOrigin.Data);

                // set new fileLength
                _disk.SetLength((_header.LastPageID + 1) * PAGE_SIZE, FileOrigin.Data);

                // get new filelength to compare
                var newLength = _disk.GetLength(FileOrigin.Data);

                return originalLength - newLength;
            }
            catch
            {
                if (savepoint != null)
                {
                    _header.Restore(savepoint);
                }

                throw;
            }
            finally
            {
                if (mustExit)
                {
                    _locker.ExitExclusive();
                }
            }
        }

        /// <summary>
        /// Fill current database with data inside file reader - run inside a transacion
        /// </summary>
        internal void RebuildContent(IFileReader reader)
        {
            // begin transaction and get TransactionID
            var transaction = _monitor.GetTransaction(true, false, out _);

            try
            {
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

                    // get snapshot, indexer and data services
                    var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                    var indexer = new IndexService(snapshot, _header.Pragmas.Collation);
                    var data = new DataService(snapshot);

                    // insert one-by-one
                    foreach (var doc in docs)
                    {
                        transaction.Safepoint();

                        this.InsertDocument(snapshot, doc, BsonAutoId.ObjectId, indexer, data);
                    }
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                _walIndex.Clear();

                throw;
            }
            finally
            {
                _monitor.ReleaseTransaction(transaction);
            }
        }
    }
}