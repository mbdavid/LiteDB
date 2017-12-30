using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// Interface factory to provider new Stream instances for datafile/walfile resources. It's useful to multiple threads can read same datafile
    /// </summary>
    public interface IDiskFactory
    {
        /// <summary>
        /// Get new Stream instance of datafile/wal
        /// </summary>
        Stream GetStream();

        /// <summary>
        /// Delete file/stream
        /// </summary>
        void Delete();

        /// <summary>
        /// Indicate that factory must be dispose on finish
        /// </summary>
        bool Dispose { get; }
    }
}