using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// Storage is a special collection to store files and streams.
    /// </summary>
    public class LiteStorage<T> : ILiteStorage<T>
    {
        private readonly ILiteDatabase _db;
        private readonly ILiteCollection<LiteFileInfo<T>> _files;
        private readonly ILiteCollection<BsonDocument> _chunks;

        public LiteStorage(ILiteDatabase db, string filesCollection, string chunksCollection)
        {
            _db = db;
            _files = db.GetCollection<LiteFileInfo<T>>(filesCollection);
            _chunks = db.GetCollection(chunksCollection);
        }

        #region Find Files

        /// <summary>
        /// Find a file inside datafile and returns LiteFileInfo instance. Returns null if not found
        /// </summary>
        public LiteFileInfo<T> FindById(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var fileId = _db.Mapper.Serialize(typeof(T), id);

            var file = _files.FindById(fileId);

            if (file == null) return null;

            file.SetReference(fileId, _files, _chunks);

            return file;
        }

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> Find(BsonExpression predicate)
        {
            var files = _files.Query()
                .Where(predicate)
                .ToEnumerable();

            foreach (var file in files)
            {
                var fileId = _db.Mapper.Serialize(typeof(T), file.Id);

                file.SetReference(fileId, _files, _chunks);

                yield return file;
            }
        }

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> Find(string predicate, BsonDocument parameters) => this.Find(BsonExpression.Create(predicate, parameters));

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> Find(string predicate, params BsonValue[] args) => this.Find(BsonExpression.Create(predicate, args));

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> Find(Expression<Func<LiteFileInfo<T>, bool>> predicate) => this.Find(_db.Mapper.GetExpression(predicate));

        /// <summary>
        /// Find all files inside file collections
        /// </summary>
        public IEnumerable<LiteFileInfo<T>> FindAll() => this.Find((BsonExpression)null);

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        public bool Exists(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var fileId = _db.Mapper.Serialize(typeof(T), id);

            return _files.Exists("_id = @0", fileId);
        }

        #endregion

        #region Upload

        /// <summary>
        /// Open/Create new file storage and returns linked Stream to write operations.
        /// </summary>
        public LiteFileStream<T> OpenWrite(T id, string filename, BsonDocument metadata = null)
        {
            // get _id as BsonValue
            var fileId = _db.Mapper.Serialize(typeof(T), id);

            // checks if file exists
            var file = this.FindById(id);

            if (file == null)
            {
                file = new LiteFileInfo<T>
                {
                    Id = id,
                    Filename = Path.GetFileName(filename),
                    MimeType = MimeTypeConverter.GetMimeType(filename),
                    Metadata = metadata ?? new BsonDocument()
                };

                // set files/chunks instances
                file.SetReference(fileId, _files, _chunks);
            }
            else
            {
                // if filename/metada was changed
                file.Filename = Path.GetFileName(filename);
                file.MimeType = MimeTypeConverter.GetMimeType(filename);
                file.Metadata = metadata ?? file.Metadata;
            }

            return file.OpenWrite();
        }

        /// <summary>
        /// Upload a file based on stream data
        /// </summary>
        public LiteFileInfo<T> Upload(T id, string filename, Stream stream, BsonDocument metadata = null)
        {
            using (var writer = this.OpenWrite(id, filename, metadata))
            {
                stream.CopyTo(writer);

                return writer.FileInfo;
            }
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

            _files.Update(file);

            return true;
        }

        #endregion

        #region Download

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        public LiteFileStream<T> OpenRead(T id)
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
            var file = this.FindById(id) ?? throw LiteException.FileNotFound(id.ToString());

            file.CopyTo(stream);

            return file;
        }

        /// <summary>
        /// Copy all file content to a file
        /// </summary>
        public LiteFileInfo<T> Download(T id, string filename, bool overwritten)
        {
            var file = this.FindById(id) ?? throw LiteException.FileNotFound(id.ToString());

            file.SaveAs(filename, overwritten);

            return file;
        }

        #endregion

        #region Delete

        /// <summary>
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(T id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            // get Id as BsonValue
            var fileId = _db.Mapper.Serialize(typeof(T), id);

            // remove file reference
            var deleted = _files.Delete(fileId);

            if (deleted)
            {
                // delete all chunks
                _chunks.DeleteMany("_id BETWEEN { f: @0, n: 0} AND {f: @0, n: @1 }", fileId, int.MaxValue);
            }

            return deleted;
        }

        #endregion
    }
}