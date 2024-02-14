using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// If Upgrade=true, run this before open Disk service
        /// </summary>
        private void TryUpgrade()
        {
            var filename = _settings.Filename;

            // if file not exists, just exit
            if (!File.Exists(filename)) return;

            using (var stream = new FileStream(
                _settings.Filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read, 1024))
            {
                var buffer = new byte[1024];

                stream.Position = 0;
                stream.Read(buffer, 0, buffer.Length);

                if (FileReaderV7.IsVersion(buffer) == false) return;
            }

            // run rebuild process
            this.Recovery();

        }

        /// <summary>
        /// Recovery datafile using a rebuild process. Run only on "Open" database
        /// </summary>
        private void Recovery()
        {
            // run build service
            var rebuilder = new RebuildService(_settings);
            var options = new RebuildOptions
            {
                Collation = new Collation(this.Pragma(Pragmas.COLLATION)),
                Password = _settings.Password,
                IncludeErrorReport = true
            };

            // run rebuild process
            rebuilder.Rebuild(options);
        }

        /// <summary>
        /// Implement a full rebuild database. Engine will be closed and re-created in another instance.
        /// A backup copy will be created with -backup extention. All data will be readed and re created in another database
        /// After run, will re-open database
        /// </summary>
        public long Rebuild(RebuildOptions options)
        {
            this.Close();

            // run build service
            var rebuilder = new RebuildService(_settings);

            // return how many bytes of diference from original/rebuild version
            var diff = rebuilder.Rebuild(options);

            // re-open engine
            this.Open();

            _state.Disposed = false;

            return diff;
        }

        /// <summary>
        /// Implement a full rebuild database. A backup copy will be created with -backup extention. All data will be readed and re created in another database
        /// </summary>
        public long Rebuild()
        {
            var collation = new Collation(this.Pragma(Pragmas.COLLATION));
            var password = _settings.Password;

            return this.Rebuild(new RebuildOptions { Password = password, Collation = collation });
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
                    // get snapshot, indexer and data services
                    var snapshot = transaction.CreateSnapshot(LockMode.Write, collection, true);
                    var indexer = new IndexService(snapshot, _header.Pragmas.Collation);
                    var data = new DataService(snapshot);

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

                    // insert one-by-one
                    foreach (var doc in docs)
                    {
                        transaction.Safepoint();

                        this.InsertDocument(snapshot, doc, BsonAutoId.ObjectId, indexer, data);
                    }
                }

                transaction.Commit();

                _monitor.ReleaseTransaction(transaction);
            }
            catch (Exception ex)
            {
                this.Close(ex);

                throw;
            }
        }
    }
}