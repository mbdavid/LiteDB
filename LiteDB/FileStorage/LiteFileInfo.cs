using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Represets a file inside storage collection
    /// </summary>
    public class LiteFileInfo
    {
        /// <summary>
        /// File id have a specific format - it's like file path.
        /// </summary>
        public const string ID_PATTERN = @"^[\w-$@!+%;\.]+(\/[\w-$@!+%;\.]+)*$";

        private static Regex IdPattern = new Regex(ID_PATTERN);

        /// <summary>
        /// Number of bytes on each chunk document to store
        /// </summary>
        public const int CHUNK_SIZE = BsonDocument.MAX_DOCUMENT_SIZE - BasePage.PAGE_AVAILABLE_BYTES; // Chunk size is a page less than a max document size

        public string Id { get; private set; }
        public string Filename { get; set; }
        public string MimeType { get; set; }
        public long Length { get; private set; }
        public int Chunks { get; private set; }
        public DateTime UploadDate { get; internal set; }
        public BsonDocument Metadata { get; set; }

        private DbEngine _engine;

        public LiteFileInfo(string id)
            : this(id, id)
        {
        }

        public LiteFileInfo(string id, string filename)
        {
            if (!IdPattern.IsMatch(id)) throw LiteException.InvalidFormat("FileId", id);

            this.Id = id;
            this.Filename = Path.GetFileName(filename);
            this.MimeType = MimeTypeConverter.GetMimeType(this.Filename);
            this.Length = 0;
            this.Chunks = 0;
            this.UploadDate = DateTime.Now;
            this.Metadata = new BsonDocument();
        }

        internal LiteFileInfo(DbEngine engine, BsonDocument doc)
        {
            _engine = engine;

            this.Id = doc["_id"].AsString;
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
                var doc = new BsonDocument();

                doc["_id"] = this.Id;
                doc["filename"] = this.Filename;
                doc["mimeType"] = this.MimeType;
                doc["length"] = this.Length;
                doc["chunks"] = this.Chunks;
                doc["uploadDate"] = this.UploadDate;
                doc["metadata"] = this.Metadata ?? new BsonDocument();

                return doc;
            }
        }

        internal IEnumerable<BsonDocument> CreateChunks(Stream stream)
        {
            var buffer = new byte[CHUNK_SIZE];
            var read = 0;
            var index = 0;

            while ((read = stream.Read(buffer, 0, LiteFileInfo.CHUNK_SIZE)) > 0)
            {
                this.Length += (long)read;
                this.Chunks++;

                var chunk = new BsonDocument();

                chunk["_id"] = GetChunckId(this.Id, index++); // index zero based

                if (read != CHUNK_SIZE)
                {
                    var bytes = new byte[read];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, read);
                    chunk["data"] = bytes;
                }
                else
                {
                    chunk["data"] = buffer;
                }

                yield return chunk;
            }

            yield break;
        }

        /// <summary>
        /// Returns chunck Id for a file
        /// </summary>
        internal static string GetChunckId(string fileId, int index)
        {
            return string.Format("{0}\\{1:00000}", fileId, index);
        }

        /// <summary>
        /// Open file stream to read from database
        /// </summary>
        public LiteFileStream OpenRead()
        {
            if (_engine == null) throw LiteException.NoDatabase();

            return new LiteFileStream(_engine, this);
        }

        /// <summary>
        /// Save file content to a external file
        /// </summary>
        public void SaveAs(string filename, bool overwritten = true)
        {
            if (_engine == null) throw LiteException.NoDatabase();

            using (var file = new FileStream(filename, overwritten ? FileMode.Create : FileMode.CreateNew))
            {
                this.OpenRead().CopyTo(file);
            }
        }

        /// <summary>
        /// Copy file content to another stream
        /// </summary>
        public void CopyTo(Stream stream)
        {
            using (var reader = this.OpenRead())
            {
                reader.CopyTo(stream);
            }
        }
    }
}