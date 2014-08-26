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
        public LiteFileStream OpenRead(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _col.FindById(id);

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
