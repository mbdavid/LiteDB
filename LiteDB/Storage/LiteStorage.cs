using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams.
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
            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo(_engine, id, filename ?? id);

                // insert if new
                _engine.Insert(FILES, file.AsDocument);
            }

            // copy stream content to litedb file stream
            stream.CopyTo(file.OpenWrite());

            return file;
        }

        /// <summary>
        /// Upload a file based on filesystem data
        /// </summary>
        public LiteFileInfo Upload(string id, string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return this.Upload(id, Path.GetFileName(filename), stream);
            }
        }

        /// <summary>
        /// Update metada on a file. File must exisits
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
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var file = this.FindById(id);

            if (file == null) throw LiteException.FileNotFound(id);

            return file.OpenRead();
        }

        /// <summary>
        /// Copy all file content to a steam
        /// </summary>
        public void Download(string id, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            var file = this.FindById(id);

            if (file == null) throw LiteException.FileNotFound(id);

            file.CopyTo(stream);
        }

        #endregion

        #region Find Files

        /// <summary>
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public LiteFileInfo FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _engine.Find(FILES, Query.EQ("_id", id)).FirstOrDefault();

            if (doc == null) return null;

            return new LiteFileInfo(_engine, doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<LiteFileInfo> Find(string startsWith)
        {
            var query = string.IsNullOrEmpty(startsWith) ?
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
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            // remove file reference in _files
            var d = _engine.Delete(FILES, Query.EQ("_id", id));

            // if not found, just return false
            if (d == 0) return false;

            // delete all files content based on _id string
            _engine.Delete(LiteStorage.CHUNKS, Query.StartsWith("_id", id + "\\"));

            return true;
        }

        #endregion
    }
}