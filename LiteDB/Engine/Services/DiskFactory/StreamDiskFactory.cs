using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
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
        /// Stream has no name (use strem type)
        /// </summary>
        public string Filename => _stream is MemoryStream ? ":memory:" : ":temp:";

        /// <summary>
        /// Get always same Stream instance, do dot accept concurrency
        /// </summary>
        public Stream GetStream() => _stream;

        /// <summary>
        /// No action when came from Stream
        /// </summary>
        public void Delete()
        {
        }

        /// <summary>
        /// Do no dispose on finish
        /// </summary>
        public bool Dispose => false;
    }
}