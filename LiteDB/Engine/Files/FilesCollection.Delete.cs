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

            _engine.Cache.Clear();

            _engine.Transaction.Begin();

            try
            {
                _engine.Data.DeleteStreamData(entry.PageID);

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
