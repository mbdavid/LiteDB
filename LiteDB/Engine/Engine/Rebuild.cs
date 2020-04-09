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

            try
            {
                // do a checkpoint before starts
                _walIndex.Checkpoint();

                var originalLength = (_header.LastPageID + 1) * PAGE_SIZE;

                // initialize V8 file reader
                var reader = new FileReaderV8(_header, _disk);

                // create a new engine after current database with no encryption
                var offset = _disk.Factory.GetLength() + (_settings.Password != null ? PAGE_SIZE : 0);
                var stream = (_disk.Writer as AesStream)?.BaseStream ?? _disk.Writer;

                var engine = new LiteEngine(new EngineSettings
                {
                    DataStream = new OffsetStream(stream, offset),
                    Collation = options?.Collation
                });

                // rebuild entrie database using FileReader
                this.RebuildContent(reader, engine);

                // if options are defined, change password (if change also)
                if (options != null)
                {
                    _disk.ChangePassword(options.Password, _settings);

                    // close 
                    engine.Dispose();

                    // re-link
                    stream = (_disk.Writer as AesStream)?.BaseStream ?? _disk.Writer;

                    engine = new LiteEngine(new EngineSettings
                    {
                        DataStream = new OffsetStream(stream, offset),
                        Collation = options?.Collation
                    });
                }

                // confirm transactions from another engine
                _walIndex.ConfirmTransaction(1, new List<PagePosition>()); // for Pragma/Checkpoint
                _walIndex.ConfirmTransaction(2, new List<PagePosition>()); // for all data

                // update LastPageID/FreeEmptyPageList in current database
                _header.LastPageID = engine._header.LastPageID;
                _header.FreeEmptyPageList = engine._header.FreeEmptyPageList;

                // do checkpoint in this engine but using another engine as log page sources
                _walIndex.Checkpoint(false, engine._disk);

                // dispose engine
                engine.Dispose();

                // get new filelength to compare
                var newLength = (_header.LastPageID + 1) * PAGE_SIZE;

                return originalLength - newLength;
            }
            catch
            {

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
        internal void RebuildContent(IFileReader reader, LiteEngine engine)
        {
            // no checkpoint 
            engine.Pragma(Pragmas.CHECKPOINT, 0);

            // begin transaction and get TransactionID
            engine.BeginTrans();

            try
            {
                foreach (var collection in reader.GetCollections())
                {
                    // first create all user indexes (exclude _id index)
                    foreach (var index in reader.GetIndexes(collection))
                    {
                        engine.EnsureIndex(collection,
                            index.Name,
                            BsonExpression.Create(index.Expression),
                            index.Unique);
                    }

                    // get all documents from current collection
                    var docs = reader.GetDocuments(collection);

                    // insert all in documents
                    engine.Insert(collection, docs, BsonAutoId.ObjectId);
                }

                engine.Commit();

                // wait async queue writer
                engine._disk.Queue.Wait();
            }
            catch (Exception)
            {
                engine.Rollback();

                throw;
            }
        }
    }
}