using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class LiteFileStorage
    {
        /// <summary>
        /// Insert a new file content inside datafile in _files collection
        /// </summary>
        public LiteFileInfo Upload(LiteFileInfo file, Stream stream)
        {
            if (file == null) throw new ArgumentNullException("id");
            if (stream == null) throw new ArgumentNullException("stream");

            // no transaction allowed
            if (this.Database.Transaction.IsInTransaction)
                throw new LiteException("Files can´t be used inside a transaction.");

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
    }
}
