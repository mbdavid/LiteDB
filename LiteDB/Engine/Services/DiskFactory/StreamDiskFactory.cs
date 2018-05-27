using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Simple stream disk implementation of disk factory - no concurrency - used for Memory/Temp database
    /// </summary>
    public class StreamDiskFactory : IDiskFactory
    {
        private Stream _stream;

        public StreamDiskFactory(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Stream has no name (use stream type)
        /// </summary>
        public string Filename => _stream is MemoryStream ? ":memory:" : _stream is TempStream ? ":temp:" : ":stream:";

        /// <summary>
        /// Use ConcurrentStream wrapper to support multi thread in same Stream (using lock control)
        /// </summary>
        public Stream GetStream() => new ConcurrentStream(_stream);

        /// <summary>
        /// Do no dispose on finish
        /// </summary>
        public bool CloseOnDispose => false;
    }
}