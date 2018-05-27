using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Interface factory to provider new Stream instances for datafile/walfile resources. It's useful to multiple threads can read same datafile
    /// </summary>
    public interface IDiskFactory
    {
        /// <summary>
        /// Get filename
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// Get new Stream instance of data stream
        /// </summary>
        Stream GetStream();

        /// <summary>
        /// Indicate that factory must be dispose on finish
        /// </summary>
        bool CloseOnDispose { get; }
    }
}