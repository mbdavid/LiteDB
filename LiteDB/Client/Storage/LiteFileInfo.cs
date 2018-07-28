using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Represents a file inside storage collection
    /// </summary>
    public class LiteFileInfo<T>
    {
        public T Id { get; private set; }
        public string Filename { get; private set; }
        public string MimeType { get; private set; }
        public long Length { get; internal set; }
        public int Chunks { get; internal set; }
        public DateTime UploadDate { get; internal set; }
        public BsonDocument Metadata { get; set; }

        private LiteDatabase _db;

        internal LiteFileInfo(LiteDatabase db, T id, string filename)
        {
            _db = db;

            this.Id = id;
            this.Filename = Path.GetFileName(filename);
            this.MimeType = MimeTypeConverter.GetMimeType(this.Filename);
            this.Length = 0;
            this.Chunks = 0;
            this.UploadDate = DateTime.Now;
            this.Metadata = new BsonDocument();
        }

        internal LiteFileInfo(LiteDatabase db, BsonDocument doc)
        {
            _db = db;

            this.Id = (T)db.Mapper.Deserialize(typeof(T), doc["_id"]);
            this.Filename = doc["filename"].AsString;
            this.MimeType = doc["mimeType"].AsString;
            this.Length = doc["length"].AsInt64;
            this.Chunks = doc["chunks"].AsInt32;
            this.UploadDate = doc["uploadDate"].AsDateTime;
            this.Metadata = doc["metadata"].AsDocument;
        }

        public BsonDocument AsDocument
        {
            get
            {
                return new BsonDocument
                {
                    { "_id", _db.Mapper.Serialize(typeof(T), this.Id) },
                    { "filename", this.Filename },
                    { "mimeType", this.MimeType },
                    { "length", this.Length },
                    { "chunks", this.Chunks },
                    { "uploadDate", this.UploadDate },
                    { "metadata", this.Metadata ?? new BsonDocument() }
                };
            }
        }

        /// <summary>
        /// Open file stream to read from database
        /// </summary>
        public LiteFileStream OpenRead()
        {
            return new LiteFileStream(_engine, this, FileAccess.Read);
        }

        /// <summary>
        /// Open file stream to write to database
        /// </summary>
        public LiteFileStream OpenWrite()
        {
            return new LiteFileStream(_engine, this, FileAccess.Write);
        }

        /// <summary>
        /// Copy file content to another stream
        /// </summary>
        public void CopyTo(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var reader = this.OpenRead())
            {
                reader.CopyTo(stream);
            }
        }

        /// <summary>
        /// Save file content to a external file
        /// </summary>
        public void SaveAs(string filename, bool overwritten = true)
        {
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));

            using (var file = File.Open(filename, overwritten ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew))
            {
                OpenRead().CopyTo(file);
            }
        }
    }
}