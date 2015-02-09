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
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            if (this.Database.Transaction.IsInTransaction)
                throw new LiteException("Files can't be used inside a transaction.");

            // remove file reference in _files
            var d = this.Files.Delete(id);

            // if not found, just return false
            if(d == false) return false;

            var index = 0;

            while (true)
            {
                var del = Chunks.Delete(LiteFileInfo.GetChunckId(id, index++));

                this.Database.Cache.RemoveExtendPages();

                if (del == false) break;
            }

            return true;
        }
    }
}
