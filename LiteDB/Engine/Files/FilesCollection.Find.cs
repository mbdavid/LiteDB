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
        public FileEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _col.FindById(id);

            if (doc == null) return null;

            return new FileEntry(doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<FileEntry> Find(string startsWithId)
        {
            var result = string.IsNullOrEmpty(startsWithId) ?
                _col.Find(Query.All()) :
                _col.Find(Query.StartsWith("_id", startsWithId));

            foreach (var doc in result)
            {
                yield return new FileEntry(doc);
            }
        }

        /// <summary>
        /// Returns all FileEntry inside database
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FileEntry> All()
        {
            return this.Find(null);
        }
    }
}
