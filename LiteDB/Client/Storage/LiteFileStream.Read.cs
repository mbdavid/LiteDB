using System;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB
{
    public partial class LiteFileStream<TFileId> : Stream
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
            var chunk = _chunks
                .FindOne("_id = { f: @0, n: @1 }", _fileId, index);

            // if chunk is null there is no more chunks
            byte[] result = chunk?["data"].AsBinary;
            if (result != null) {
                _chunkLengths[index] = result.Length;
            }
            return result;
        }

        private void SetReadStreamPosition(long newPosition)
        {
            if (newPosition < 0 || newPosition > Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            _streamPosition = newPosition;

            // calculate new chunk position
            long seekStreamPosition = 0;
            int loadedChunk = _currentChunkIndex;
            int newChunkIndex = 0;
            while (seekStreamPosition <= _streamPosition) {
                if (!_chunkLengths.ContainsKey(newChunkIndex)) {
                    loadedChunk = newChunkIndex;
                    _currentChunkData = GetChunkData(newChunkIndex);
                }
                seekStreamPosition += _chunkLengths[newChunkIndex];
                newChunkIndex++;
            }
            newChunkIndex--;
            seekStreamPosition -= _chunkLengths[newChunkIndex];
            _positionInChunk = (int)(_streamPosition - seekStreamPosition);
            _currentChunkIndex = newChunkIndex;
            if (loadedChunk != _currentChunkIndex) {
                _currentChunkData = GetChunkData(_currentChunkIndex);
            }
        }
    }
}