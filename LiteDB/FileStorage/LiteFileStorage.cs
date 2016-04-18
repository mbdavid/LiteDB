using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams.
    /// </summary>
    public partial class LiteFileStorage
    {
        internal const string FILES = "_files";
        internal const string CHUNKS = "_chunks";

        private DbEngine _engine;

        internal LiteFileStorage(DbEngine engine)
        {
            _engine = engine;
        }

        #region Upload

        /// <summary>
        /// Insert a new file content inside datafile in _files collection
        /// </summary>
        public LiteFileInfo Upload(LiteFileInfo file, Stream stream)
        {
            if (file == null) throw new ArgumentNullException("id");
            if (stream == null) throw new ArgumentNullException("stream");

            file.UploadDate = DateTime.Now;

            // insert file in _files collections with 0 file length
            _engine.Insert(FILES, new BsonDocument[] { file.AsDocument });

            // for each chunk, insert as a chunk document
            foreach (var chunk in file.CreateChunks(stream))
            {
                _engine.Insert(CHUNKS, new BsonDocument[] { chunk });
            }

            // update fileLength/chunks to confirm full file length stored in disk
            _engine.Update(FILES, new BsonDocument[] { file.AsDocument });

            return file;
        }

        public LiteFileInfo Upload(string id, Stream stream)
        {
            return this.Upload(new LiteFileInfo(id), stream);
        }

        public LiteFileInfo Upload(string id, string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return this.Upload(new LiteFileInfo(id, filename), stream);
            }
        }

        /// <summary>
        /// Upload a file to FileStorage using Path.GetFilename as file Id
        /// </summary>
        public LiteFileInfo Upload(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return this.Upload(new LiteFileInfo(Path.GetFileName(filename), filename), stream);
            }
        }

        /// <summary>
        /// Update metada on a file. File must exisits
        /// </summary>
        public bool SetMetadata(string id, BsonDocument metadata)
        {
            var file = this.FindById(id);
            if (file == null) return false;
            file.Metadata = metadata;
            _engine.Update(FILES, new BsonDocument[] { file.AsDocument });
            return true;
        }

        #endregion Upload

        #region Download

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

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        public LiteFileStream OpenRead(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _engine.Find(FILES, Query.EQ("_id", new BsonValue(id))).FirstOrDefault();

            if (doc == null) return null;

            return this.OpenRead(new LiteFileInfo(doc));
        }

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        internal LiteFileStream OpenRead(LiteFileInfo entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            return new LiteFileStream(_engine, entry);
        }

        #endregion Download

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

        #endregion Find Files

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

            var index = 0;

            while (true)
            {
                var del = _engine.Delete(CHUNKS, Query.EQ("_id", LiteFileInfo.GetChunckId(id, index++)));

                if (del == 0) break;
            }

            return true;
        }

        #endregion Delete
    }
}