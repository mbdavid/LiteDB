using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams. Transactions are not supported in Upload/Download operations.
    /// </summary>
    public class LiteStorage<T>
    {
        private readonly LiteDatabase _db;
        private readonly LiteCollection<LiteFileInfo<T>> _files;
        private readonly LiteCollection<BsonDocument> _chunks;
        private readonly int _chunkSize;

        public LiteStorage(LiteDatabase db, string filesCollection, string chunkCollection, int chunkSize)
        {
            _db = db;
            _files = db.GetCollection<LiteFileInfo<T>>(filesCollection);
            _chunks = db.GetCollection(chunkCollection);
            _chunkSize = chunkSize;
        }

        #region Upload

        /// <summary>
        /// Open/Create new file storage and returns linked Stream to write operations
        /// </summary>
        public LiteFileStream OpenWrite(T id, string filename, BsonDocument metadata = null)
        {
            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo<T>(_db, id, filename);

                // insert if new
                _files.Insert(file);
            }

            // update metadata if passed
            if (metadata != null)
            {
                file.Metadata = metadata;
            }

            return file.OpenWrite();
        }

        /// <summary>
        /// Upload a file based on stream data
        /// </summary>
        public LiteFileInfo<T> Upload(T id, string filename, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo(_db, id, filename);

                // insert if new
                _files.Insert(file.AsDocument);
            }

            // copy stream content to litedb file stream
            using (var writer = file.OpenWrite())
            {
                stream.CopyTo(writer);
            }

            return file;
        }

        /// <summary>
        /// Upload a file based on file system data
        /// </summary>
        public LiteFileInfo<T> Upload(T id, string filename)
        {
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));

            using (var stream = File.OpenRead(filename))
            {
                return this.Upload(id, Path.GetFileName(filename), stream);
            }
        }

        /// <summary>
        /// Update metadata on a file. File must exist.
        /// </summary>
        public bool SetMetadata(T id, BsonDocument metadata)
        {
            var file = this.FindById(id);

            if (file == null) return false;

            file.Metadata = metadata ?? new BsonDocument();

            _files.Update(file.AsDocument);

            return true;
        }

        #endregion

        #region Download

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        public LiteFileStream OpenRead(T id)
        {
            var file = this.FindById(id);

            if (file == null) throw LiteException.FileNotFound(id.ToString());

            return file.OpenRead();
        }

        /// <summary>
        /// Copy all file content to a steam
        /// </summary>
        public LiteFileInfo<T> Download(T id, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var file = this.FindById(id);

            if (file == null) throw LiteException.FileNotFound(id);

            file.CopyTo(stream);

            return file;
        }

        #endregion

        #region Find Files

        /// <summary>
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public LiteFileInfo<T> FindById(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var doc = _files.FindOne(x => x.Id == id);

            if (doc == null) return null;

            return new LiteFileInfo<T>(_db, doc);
        }

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        public bool Exists(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            return _files.Exists("_id = @0", id);
        }

        /// <summary>
        /// Returns all FileEntry inside database
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> FindAll()
        {
            return this.Find;
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var newTransaction = _db.BeginTrans();

            // remove file reference in _files
            var deleted = _engine.Delete(FILES, id);

            // if not found, just return false
            if (!deleted) return false;

            // delete all files chunks based on _id string
            var index = 0;

            // delete one-by-one to avoid all pages files dirty in memory
            while(deleted)
            {
                deleted = _engine.Delete(CHUNKS, LiteFileStream.GetChunckId(id, index++)); // index zero based
            }

            // if new transaction was created, commit now
            if (newTransaction) _db.Commit();


            return true;
        }

        #endregion
    }
}