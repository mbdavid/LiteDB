using System;
using System.IO;
using System.Linq;

namespace LiteDB
{
    public partial class LiteFileStream : Stream
    {
        public override void Flush()
        {
            // write last unsaved chunks
            this.WriteChunks();

            _file.UploadDate = DateTime.Now;
            _file.Length = _streamPosition;

            _engine.Update(LiteStorage.FILES, _file.AsDocument);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _streamPosition += count;

            _buffer.Write(buffer, offset, count);

            if (_buffer.Length >= MAX_CHUNK_SIZE)
            {
                this.WriteChunks();
            }
        }

        /// <summary>
        /// Consume all _buffer bytes and write to database
        /// </summary>
        private void WriteChunks()
        {
            var buffer = new byte[MAX_CHUNK_SIZE];
            var read = 0;
            _buffer.Seek(0, SeekOrigin.Begin);

            while ((read = _buffer.Read(buffer, 0, MAX_CHUNK_SIZE)) > 0)
            {
                var chunk = new BsonDocument
                {
                    { "_id", GetChunckId(_file.Id, _file.Chunks++) } // index zero based
                };

                // get chunk byte array part
                if (read != MAX_CHUNK_SIZE)
                {
                    var bytes = new byte[read];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, read);
                    chunk["data"] = bytes;
                }
                else
                {
                    chunk["data"] = buffer;
                }

                // insert chunk part
                _engine.Insert(LiteStorage.CHUNKS, chunk);
            }

            _buffer = new MemoryStream();
        }
    }
}