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
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public FileEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = _files.FindById(id);

            if (doc == null) return null;

            return new FileEntry(_engine, doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<FileEntry> Find(string startsWith)
        {
            var result = string.IsNullOrEmpty(startsWith) ?
                _files.Find(Query.All()) :
                _files.Find(Query.StartsWith("_id", startsWith));

            foreach (var doc in result)
            {
                yield return new FileEntry(_engine, doc);
            }
        }

        /// <summary>
        /// Returns all FileEntry inside database
        /// </summary>
        public IEnumerable<FileEntry> All()
        {
            return this.Find(null);
        }
    }
}
