using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public class LiteFileStream : Stream
    {
        private LiteEngine _engine;
        private FileEntry _entry;
        private readonly long _streamLength = 0;

        private long _streamPosition = 0;

        private int _currentChunkIndex = 0;
        private byte[] _currentChunkData = null;
        private int _positionInChunk = 0;

        internal LiteFileStream(LiteEngine engine, FileEntry entry)
        {
            _engine = engine;
            _entry = entry;

            if (entry.Length == 0)
            {
                throw new LiteException("This file has no content or is corrupted");
            }

            _positionInChunk = 0;
            _currentChunkIndex = 0;
            _currentChunkData = this.GetChunkData(_currentChunkIndex);
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public FileEntry FileEntry { get { return _entry; } }

        public override long Length { get { return _streamLength; } }

        public override bool CanRead { get { return true; } }

        public override long Position
        {
            get
            {
                return _streamPosition;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesLeft = count;

            while (_currentChunkData != null && bytesLeft > 0)
            {
                var bytesToCopy = Math.Min(bytesLeft, _currentChunkData.Length - _positionInChunk);

                Buffer.BlockCopy(_currentChunkData, _positionInChunk, buffer, offset, bytesToCopy);

                _positionInChunk += bytesToCopy;
                bytesLeft -= bytesToCopy;
                offset += bytesToCopy;
                _streamPosition += bytesToCopy;

                if (_positionInChunk >= _currentChunkData.Length)
                {
                    _positionInChunk = 0;

                    _currentChunkData = this.GetChunkData(++_currentChunkIndex);
                }
            }

            return count - bytesLeft;
        }

        private byte[] GetChunkData(int index)
        {
            // avoid too many extend pages on memory
            _engine.Cache.RemoveExtendPages();

            // check if there is no more chunks in this file
            var chunks = _engine.GetCollection("_chunks");

            var chunk = chunks.FindById(_entry.Id + "\\" + index);

            // if chunk is null there is no more chunks
            return chunk == null ? null : chunk["data"].AsByteArray;
        }

        #region Not supported operations

        public override bool CanWrite { get { return false; } }

        public override bool CanSeek { get { return false; } }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
