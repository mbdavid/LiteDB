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
        /// Get number of how many Stream concurrency support
        /// </summary>
        int ConcurrencyLimit { get; }

        /// <summary>
        /// Get new Stream instance of datafile
        /// </summary>
        Stream GetDataFile();

        /// <summary>
        /// Get new Stream instance of walfile
        /// </summary>
        Stream GetWalFile();

        /// <summary>
        /// Indicate that factory must be dispose on finish
        /// </summary>
        bool Dispose { get; }
    }
}