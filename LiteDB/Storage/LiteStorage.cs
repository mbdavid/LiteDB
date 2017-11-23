using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams. Transactions are not supported in Upload/Download operations.
    /// </summary>
    public class LiteStorage
    {
        internal const string FILES = "_files";
        internal const string CHUNKS = "_chunks";

        private LiteEngine _engine;

        public LiteStorage(LiteEngine engine)
        {
            _engine = engine;
        }

        #region Upload

        /// <summary>
        /// Open/Create new file storage and returns linked Stream to write operations
        /// </summary>
        public LiteFileStream OpenWrite(string id, string filename, BsonDocument metadata = null)
        {
            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo(_engine, id, filename ?? id);

                // insert if new
                _engine.Insert(FILES, file.AsDocument);
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
        public LiteFileInfo Upload(string id, string filename, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo(_engine, id, filename ?? id);

                // insert if new
                _engine.Insert(FILES, file.AsDocument);
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
        public LiteFileInfo Upload(string id, string filename)
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
        public bool SetMetadata(string id, BsonDocument metadata)
        {
            var file = this.FindById(id);
            if (file == null) return false;
            file.Metadata = metadata ?? new BsonDocument();
            _engine.Update(FILES, file.AsDocument);
            return true;
        }

        #endregion

        #region Download

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        public LiteFileStream OpenRead(string id)
        {
            var file = this.FindById(id);

            if (file == null) throw LiteException.FileNotFound(id);

            return file.OpenRead();
        }

        /// <summary>
        /// Copy all file content to a steam
        /// </summary>
        public LiteFileInfo Download(string id, Stream stream)
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
        public LiteFileInfo FindById(string id)
        {
            if (id.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(id));

            var doc = _engine.Find(FILES, Query.EQ("_id", id)).FirstOrDefault();

            if (doc == null) return null;

            return new LiteFileInfo(_engine, doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<LiteFileInfo> Find(string startsWith)
        {
            var query = startsWith.IsNullOrWhiteSpace() ?
                Query.All() :
                Query.StartsWith("_id", startsWith);

            var docs = _engine.Find(FILES, query);

            foreach (var doc in docs)
            {
                yield return new LiteFileInfo(_engine, doc);
            }
        }

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        public bool Exists(string id)
        {
            if (id.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(id));

            return _engine.Exists(FILES, Query.EQ("_id", id));
        }

        /// <summary>
        /// Returns all FileEntry inside database
        /// </summary>
        public IEnumerable<LiteFileInfo> FindAll()
        {
            return this.Find(null);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(string id)
        {
            if (id.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(id));

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

            return true;
        }

        #endregion
    }
}