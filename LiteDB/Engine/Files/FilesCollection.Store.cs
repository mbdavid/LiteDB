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
        /// Insert or update a file content inside datafile
        /// </summary>
        public FileEntry Store(string key, Stream stream, Dictionary<string, object> metadata = null)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            if (stream == null) throw new ArgumentNullException("stream");

            if(!Regex.IsMatch(key, @"^[^\.<>\\/|:""*][^<>\\/|:""*]*(/[^\.<>\\/|:""*][^<>\\/|:""*]*)*$"))
                throw new ArgumentException("Invalid key format. Use key as path/to/file/filename.ext");

            // Find document and convert to entry (or create a new one)
            var doc = _col.FindById(key);

            var entry = doc == null ? new FileEntry(key, metadata) : new FileEntry(doc);

            // storage do not use cache - read/write pages directly from disk
            // so, transaction is not allowed. 
            // clear cache to garantee that are do not have dirty pages

            if (_engine.Transaction.IsInTransaction)
                throw new LiteDBException("Files can´t be used inside a transaction.");

            _engine.Cache.Clear();

            _engine.Transaction.Begin();

            try
            {
                // not found? then insert
                if (doc == null)
                {
                    var page = _engine.Data.NextPage(null);

                    entry.PageID = page.PageID;

                    entry.Length = _engine.Data.StoreStreamData(page, stream);

                    _col.Insert(key, entry.ToBson());
                }
                else
                {
                    var page = _engine.Disk.ReadPage<ExtendPage>(entry.PageID);

                    entry.Length = _engine.Data.StoreStreamData(page, stream);
                    entry.UploadDate = DateTime.Now;
                    entry.Metadata = metadata ?? entry.Metadata;

                    _col.Update(key, entry.ToBson());
                }

                _engine.Transaction.Commit();

            }
            catch (Exception ex)
            {
                _engine.Transaction.Rollback();
                throw ex;
            }

            return entry;
        }

        /// <summary>
        /// Updates a file entry on storage - do not change file content, only metadata will be update
        /// </summary>
        public bool Update(FileEntry file)
        {
            if (file == null) throw new ArgumentNullException("file");

            return _col.Update(file.Key, file.ToBson());
        }
    }
}
