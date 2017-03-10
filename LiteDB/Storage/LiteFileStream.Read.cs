using System;
using System.IO;
using System.Linq;

namespace LiteDB
{
    public partial class LiteFileStream : Stream
    {
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != FileAccess.Read) throw new NotSupportedException();

            var bytesLeft = count;

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
            // check if there is no more chunks in this file
            var chunk = _engine
                .Find(LiteStorage.CHUNKS, Query.EQ("_id", GetChunckId(_file.Id, index)))
                .FirstOrDefault();

            // if chunk is null there is no more chunks
            return chunk == null ? null : chunk["data"].AsBinary;
        }
    }
}