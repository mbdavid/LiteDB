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
            var lockWasTaken = _locker.EnterExclusive();

            // get a header backup/savepoint before change
            PageBuffer savepoint = null;

            try
            {
                // do a checkpoint before starts
                _walIndex.Checkpoint();

                var originalLength = (_header.LastPageID + 1) * PAGE_SIZE;

                // must clear all cache pages because all of them will change
                _disk.Cache.Clear();

                // create a savepoint in header page - restore if any error occurs
                savepoint = _header.Savepoint();

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

                // do checkpoint
                _walIndex.Checkpoint(false);

                // override header page
                _disk.Write(new[] { _header.UpdateBuffer() });

                // if options are defined, change password (if change also)
                if (options != null)
                {
                    _disk.ChangePassword(options.Password, _settings);
                }

                // get new filelength to compare
                var newLength = (_header.LastPageID + 1) * PAGE_SIZE;

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
                if (lockWasTaken)
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
            this.BeginTrans();

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

                    // insert all in documents
                    this.Insert(collection, docs, BsonAutoId.ObjectId);
                }

                this.Commit();

                // wait async queue writer
                _disk.Queue.Wait();
            }
            catch (Exception)
            {
                this.Rollback();

                throw;
            }
        }
    }
}