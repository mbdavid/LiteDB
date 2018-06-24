using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class TempFile : IDisposable
    {
        public string Filename { get; private set; }

        public TempFile()
        {
            this.Filename = Path.Combine(Path.GetTempPath(), string.Format("test-{0}.{1}", Guid.NewGuid(), ".db"));
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
    }
}