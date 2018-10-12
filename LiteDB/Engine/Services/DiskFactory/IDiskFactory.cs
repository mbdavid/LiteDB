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
    internal interface IDiskFactory
    {
        /// <summary>
        /// Get filename
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// Get new datafile stream instance
        /// </summary>
        Stream GetDataFileStream(bool writeMode);

        /// <summary>
        /// Get new log file stream instance
        /// </summary>
        Stream GetLogFileStream(bool writeMode);

        /// <summary>
        /// Return if log file exist
        /// </summary>
        bool IsLogFileExists();

        /// <summary>
        /// Delete physical log file
        /// </summary>
        void DeleteLogFile();

        /// <summary>
        /// Indicate that factory must be dispose on finish
        /// </summary>
        bool CloseOnDispose { get; }
    }
}