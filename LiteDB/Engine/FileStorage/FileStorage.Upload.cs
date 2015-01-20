using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class FileStorage
    {
        /// <summary>
        /// Insert a new file content inside datafile in _files collection
        /// </summary>
        public FileEntry Upload(FileEntry file, Stream stream)
        {
            if (file == null) throw new ArgumentNullException("id");
            if (stream == null) throw new ArgumentNullException("stream");

            // no transaction allowed
            if (_engine.Transaction.IsInTransaction)
                throw new LiteException("Files can´t be used inside a transaction.");

            file.UploadDate = DateTime.Now;

            // insert file in _files collections with 0 file length
            _files.Insert(file.AsDocument);

            // for each chunk, insert as a chunk document
            foreach (var chunk in file.CreateChunks(stream))
            {
                _chunks.Insert(chunk);

                // clear extend pages in cache to avoid too many use of memory in big files
                _engine.Cache.RemoveExtendPages();
            }

            // update fileLength to confirm full file length stored in disk
            _files.Update(file.AsDocument);

            return file;
        }

        public FileEntry Upload(string id, Stream stream)
        {
            return this.Upload(new FileEntry(id), stream);
        }

        public FileEntry Upload(string id, string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                return this.Upload(new FileEntry(id, filename), stream);
            }
        }

        /// <summary>
        /// Update a file entry on storage - do not change file content, only filename/metadata will be update
        /// </summary>
        public bool Update(FileEntry file)
        {
            if (file == null) throw new ArgumentNullException("file");

            return _files.Update(file.AsDocument);
        }
    }
}
