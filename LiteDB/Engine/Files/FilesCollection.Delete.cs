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
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        public bool Delete(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");

            var doc = _col.FindById(key);

            if (doc == null) return false;

            if (_engine.Transaction.IsInTransaction)
                throw new LiteDBException("Files can´t be used inside a transaction.");

            var entry = new FileEntry(doc);

            _engine.Transaction.Begin();

            // at this point, pages in cache are equals in disk - I can use anyone

            try
            {
                // set all pages to empty directly to disk - except Header ponter and last deleted page (both are will be saved during commit)
                _engine.Data.DeleteStreamData(entry.PageID);

                // delete FileEntry document
                _col.Delete(key);

                _engine.Transaction.Commit();
            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }

            return true;
        }
    }
}
