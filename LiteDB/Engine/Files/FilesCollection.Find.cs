using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class FilesCollection
    {
        /// <summary>
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public FileEntry FindByKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var doc = _col.FindById(key);

            if (doc == null) return null;

            return new FileEntry(doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with key passed. Null key returns all files
        /// </summary>
        public IEnumerable<FileEntry> Find(string startsWithKey)
        {
            var result = string.IsNullOrEmpty(startsWithKey) ?
                _col.Find(Query.All()) :
                _col.Find(Query.StartsWith("_id", startsWithKey));

            foreach (var doc in result)
            {
                yield return new FileEntry(doc);
            }
        }
    }
}
