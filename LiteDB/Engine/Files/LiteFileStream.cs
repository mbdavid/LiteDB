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
        private ExtendPage _currentPage = null;
        private int _positionInPage = 0;

        internal LiteFileStream(LiteEngine engine, FileEntry entry)
        {
            _engine = engine;
            _entry = entry;

            _currentPage = engine.Disk.ReadPage<ExtendPage>(entry.PageID);
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

            while (_currentPage != null && bytesLeft > 0)
            {
                int bytesToCopy = Math.Min(bytesLeft, _currentPage.Data.Length - _positionInPage);
                Buffer.BlockCopy(_currentPage.Data, _positionInPage, buffer, offset, bytesToCopy);

                _positionInPage += bytesToCopy;
                bytesLeft -= bytesToCopy;
                offset += bytesToCopy;
                _streamPosition += bytesToCopy;

                if (_positionInPage >= _currentPage.Data.Length)
                {
                    _positionInPage = 0;

                    if (_currentPage.NextPageID == uint.MaxValue)
                        _currentPage = null;
                    else
                        _currentPage = _engine.Disk.ReadPage<ExtendPage>(_currentPage.NextPageID);
                }
            }

            return count - bytesLeft;
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
