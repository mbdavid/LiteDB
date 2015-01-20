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
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("id");

            if (_engine.Transaction.IsInTransaction)
                throw new LiteException("Files can't be used inside a transaction.");

            // remove file reference in _files
            var d = _files.Delete(id);

            // if not found, just return false
            if(d == false) return false;

            var index = 0;

            while (true)
            {
                var del = _chunks.Delete(id + "\\" + (index ++));

                _engine.Cache.RemoveExtendPages();

                if (del == false) break;
            }

            return true;
        }
    }
}
