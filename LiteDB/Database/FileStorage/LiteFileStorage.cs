using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files/streams.
    /// </summary>
    public partial class LiteFileStorage
    {
        public LiteCollection<BsonDocument> Files { get; private set; }
        public LiteCollection<BsonDocument> Chunks { get; private set; }
        public LiteDatabase Database { get; private set; }

        internal LiteFileStorage(LiteDatabase db)
        {
            this.Database = db;
            this.Files = this.Database.GetCollection("_files");
            this.Chunks = this.Database.GetCollection("_chunks");
        }

        #region Upload

        /// <summary>
        /// Insert a new file content inside datafile in _files collection
        /// </summary>
        public LiteFileInfo Upload(LiteFileInfo file, Stream stream)
        {
            if (file == null) throw new ArgumentNullException("id");
            if (stream == null) throw new ArgumentNullException("stream");

            // no transaction allowed
            if (this.Database.Transaction.IsInTransaction) throw LiteException.InvalidTransaction();

            file.UploadDate = DateTime.Now;

            // insert file in _files collections with 0 file length
            this.Files.Insert(file.AsDocument);

            // for each chunk, insert as a chunk document
            foreach (var chunk in file.CreateChunks(stream))
            {
                this.Chunks.Insert(chunk);

                // clear extend pages in cache to avoid too many use of memory in big files
                this.Database.Cache.RemoveExtendPages();
            }

            // update fileLength to confirm full file length stored in disk
            this.Files.Update(file.AsDocument);

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
            this.Files.Update(file.AsDocument);
            return true;
        }

        #endregion

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

            var doc = this.Files.FindById(id);

            if (doc == null) return null;

            return this.OpenRead(new LiteFileInfo(doc));
        }

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        internal LiteFileStream OpenRead(LiteFileInfo entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            return new LiteFileStream(this.Database, entry);
        }

        #endregion

        #region Find Files

        /// <summary>
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public LiteFileInfo FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = this.Files.FindById(id);

            if (doc == null) return null;

            return new LiteFileInfo(this.Database, doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<LiteFileInfo> Find(string startsWith)
        {
            var result = string.IsNullOrEmpty(startsWith) ?
                this.Files.Find(Query.All()) :
                this.Files.Find(Query.StartsWith("_id", startsWith));

            foreach (var doc in result)
            {
                yield return new LiteFileInfo(this.Database, doc);
            }
        }

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        public bool Exists(string id)
        {
            return this.Files.Exists(Query.EQ("_id", id));
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

            if (this.Database.Transaction.IsInTransaction) throw LiteException.InvalidTransaction();

            // remove file reference in _files
            var d = this.Files.Delete(id);

            // if not found, just return false
            if (d == false) return false;

            var index = 0;

            while (true)
            {
                var del = Chunks.Delete(LiteFileInfo.GetChunckId(id, index++));

                this.Database.Cache.RemoveExtendPages();

                if (del == false) break;
            }

            return true;
        }

        #endregion
    }
}
