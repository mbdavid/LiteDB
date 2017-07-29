using System;
using System.IO;
using System.Linq;

namespace LiteDB
{
    public partial class LiteFileStream : Stream
    {
        /// <summary>
        /// Number of bytes on each chunk document to store
        /// </summary>
        public const int MAX_CHUNK_SIZE = 255 * 1024; // 255kb like GridFS

        private LiteEngine _engine;
        private LiteFileInfo _file;
        private FileAccess _mode;

        private long _streamPosition = 0;
        private int _currentChunkIndex = 0;
        private byte[] _currentChunkData = null;
        private int _positionInChunk = 0;
        private MemoryStream _buffer;

        internal LiteFileStream(LiteEngine engine, LiteFileInfo file, FileAccess mode)
        {
            _engine = engine;
            _file = file;
            _mode = mode;

            if (mode == FileAccess.Read)
            {
                // initialize first data block
                _currentChunkData = this.GetChunkData(_currentChunkIndex);
            }
            else if(mode == FileAccess.Write)
            {
                _buffer = new MemoryStream(MAX_CHUNK_SIZE);

                // delete chunks content if needed
                if (file.Length > 0)
                {
                    var index = 0;
                    var deleted = true;

                    // delete one-by-one to avoid all pages files dirty in memory
                    while (deleted)
                    {
                        deleted = _engine.Delete(LiteStorage.CHUNKS, LiteFileStream.GetChunckId(_file.Id, index++)); // index zero based
                    }
                }

                // clear size counters
                file.Length = 0;
                file.Chunks = 0;
            }
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public LiteFileInfo FileInfo { get { return _file; } }

        public override long Length { get { return _file.Length; } }

        public override bool CanRead { get { return _mode == FileAccess.Read; } }

        public override bool CanWrite { get { return _mode == FileAccess.Write; } }

        public override bool CanSeek { get { return false; } }

        public override long Position
        {
            get { return _streamPosition; }
            set { throw new NotSupportedException(); }
        }

        internal static string GetChunckId(string id, int index)
        {
            return string.Format("{0}\\{1:00000}", id, index);
        }

        #region Dispose

        private bool _disposed = false;

        ~LiteFileStream()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (this.CanWrite)
                {
                    this.Flush();
                }
                _disposed = true;
            }
        }

        #endregion

        #region Not supported operations

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}