using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class TempFile : IDisposable
    {
        private bool _checkIntegrity = false;

        public string Filename { get; private set; }

        public TempFile(bool checkIntegrity = true)
        {
            this.Filename = Path.Combine(Path.GetTempPath(), string.Format("test-{0}.{1}", Guid.NewGuid(), ".db"));
            _checkIntegrity = checkIntegrity;
        }

        #region Dispose

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TempFile()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // free other managed objects that implement
                // IDisposable only
            }

            // check file integrity
            this.CheckIntegrity();

            File.Delete(this.Filename);

            _disposed = true;
        }

        #endregion

        public long Size
        {
            get { return new FileInfo(this.Filename).Length; }
        }

        public string ReadAsText()
        {
            return File.ReadAllText(this.Filename);
        }

        /// <summary>
        /// Read all colleciton, indexes and documents inside current datafile
        /// Drop per index, per collection and shrink
        /// This steps will check/validate all file data
        /// </summary>
        private void CheckIntegrity()
        {
            /*
            using (var db = new LiteEngine(this.Filename))
            {
                var cols = db.GetCollectionNames().ToArray();

                foreach (var col in cols)
                {
                    var indexes = db.GetIndexes(col).ToArray();

                    foreach (var idx in indexes)
                    {
                        var q = db.Find(col, Query.All(idx.Name));

                        foreach (var doc in q)
                        {
                            // document are ok!
                        }

                        // lets drop this index (if not _id)
                        if (idx.Name != "_id")
                        {
                            db.DropIndex(col, idx.Name);
                        }
                    }

                    // and drop collection
                    db.DropCollection(col);
                }

                // and now shrink
                db.Shrink();
            }
            */
        }
    }
}