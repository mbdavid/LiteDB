using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class FileStorage
    {
        /// <summary>
        /// Copy all file content to a steam
        /// </summary>
        public void Download(string id, Stream stream)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");
            if (stream == null) throw new ArgumentNullException("stream");

            using (var s = this.OpenRead(id))
            {
                if (s == null) throw new LiteException("File not found");

                s.CopyTo(stream);
            }
        }

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        public LiteFileStream OpenRead(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _files.FindById(id);

            if (doc == null) return null;

            return this.OpenRead(new FileEntry(doc));
        }

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        internal LiteFileStream OpenRead(FileEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            return new LiteFileStream(_engine, entry);
        }
    }
}
