using System;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// Implement a tempoary stream that uses MemoryStream until get LIMIT bytes, then copy all to tempoary disk file and delete on dispose
    /// </summary>
    public class TempStream : Stream
    {
        private Stream _stream = new MemoryStream();
        private string _filename = null;
        private long _maxMemoryUsage;

        public TempStream(long maxMemoryUsage = 104857600 /* 100MB */)
        {
            _maxMemoryUsage = maxMemoryUsage;
        }

        /// <summary>
        /// Indicate that stream are all in memory
        /// </summary>
        public bool InMemory => _stream is MemoryStream;

        /// <summary>
        /// Indicate that stream is now on this
        /// </summary>
        public bool InDisk => _stream is FileStream;

        /// <summary>
        /// Get temp disk filename (used only when IsFile is true)
        /// </summary>
        public string Filename => _filename;

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanWrite;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => this.Seek(value, SeekOrigin.Begin); }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
        {
            var position =
                origin == SeekOrigin.Begin ? offset :
                origin == SeekOrigin.Current ? _stream.Position + offset :
                _stream.Position - offset;

            // if offset pass limit, change current _strem from MemoryStrem to FileStream with TempFileName()
            if (position > _maxMemoryUsage)
            {
                _filename = Path.GetTempFileName();
                var file = new FileStream(_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, BasePage.PAGE_SIZE, FileOptions.RandomAccess);

                _stream.Position = 0;
                _stream.CopyTo(file);

                // dispose MemoryStream
                _stream.Dispose();

                // and replace with FileStream
                _stream = file;
            }

            return _stream.Seek(offset, origin);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _stream.Dispose();

            // if any file was created, let's delete now
            if (_filename != null)
            {
                File.Delete(_filename);
            }
        }
    }
}