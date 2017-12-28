using System;
using System.IO;

namespace LiteDB
{
    internal class DiskFile : IDisposable
    {
        private Action<Stream, Stream> _dispose;

        public Stream Data { get; set; }
        public Stream Wal { get; set; }

        public DiskFile(Action<Stream, Stream> dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose(this.Data, this.Wal);
        }
    }
}