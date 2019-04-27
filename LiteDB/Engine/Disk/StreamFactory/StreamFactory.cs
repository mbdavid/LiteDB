using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Simple Stream disk implementation of disk factory - used for Memory/Temp database
    /// [ThreadSafe]
    /// </summary>
    internal class StreamFactory : IStreamFactory
    {
        private readonly Stream _stream;

        public StreamFactory(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Stream has no name (use stream type)
        /// </summary>
        public string Name => _stream is MemoryStream ? ":memory:" : _stream is TempStream ? ":temp:" : ":stream:";

        /// <summary>
        /// Use ConcurrentStream wrapper to support multi thread in same Stream (using lock control)
        /// </summary>
        public Stream GetStream(bool canWrite, bool sequencial) => new ConcurrentStream(_stream, canWrite);

        /// <summary>
        /// Check if file exists based on stream length
        /// </summary>
        public bool Exists() => _stream.Length > 0;

        /// <summary>
        /// There is no delete method in Stream factory
        /// </summary>
        public void Delete()
        {
        }

        /// <summary>
        /// Do no dispose on finish
        /// </summary>
        public bool CloseOnDispose => false;
    }
}