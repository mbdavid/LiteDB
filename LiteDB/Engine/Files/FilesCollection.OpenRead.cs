using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class FilesCollection
    {
        /// <summary>
        /// Load data inside storage and copy to stream
        /// </summary>
        public LiteFileStream OpenRead(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var doc = _col.FindById(key);

            if (doc == null) return null;

            return this.OpenRead(new FileEntry(doc));
        }

        /// <summary>
        /// Load data inside storage and copy to stream
        /// </summary>
        internal LiteFileStream OpenRead(FileEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");

            return new LiteFileStream(_engine, entry);
        }
    }
}
