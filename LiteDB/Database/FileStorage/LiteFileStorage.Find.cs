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
        /// Find a file inside datafile and returns FileEntry instance. Returns null if not found
        /// </summary>
        public LiteFileInfo FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            var doc = this.Files.FindById(id);

            if (doc == null) return null;

            return new LiteFileInfo(Database, doc);
        }

        /// <summary>
        /// Returns all FileEntry founded starting with id passed.
        /// </summary>
        public IEnumerable<LiteFileInfo> Find(string startsWith)
        {
            var result = string.IsNullOrEmpty(startsWith) ?
                this.Files.Find(Query.All()) :
                this.Files.Find(Query.StartsWith("_id", startsWith));

            foreach (var doc in result)
            {
                yield return new LiteFileInfo(this.Database, doc);
            }
        }

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        public bool Exists(string id)
        {
            return this.Files.Exists(Query.EQ("_id", id));
        }

        /// <summary>
        /// Returns all FileEntry inside database
        /// </summary>
        public IEnumerable<LiteFileInfo> FindAll()
        {
            return this.Find(null);
        }
    }
}
