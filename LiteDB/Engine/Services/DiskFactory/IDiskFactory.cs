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
        Stream GetDataFileStream(bool readOnly);

        /// <summary>
        /// Get new WAL file stream instance
        /// </summary>
        Stream GetWalFileStream(bool readOnly);

        /// <summary>
        /// Return if wal file exist
        /// </summary>
        bool IsWalFileExists();

        /// <summary>
        /// Delete physical wal file
        /// </summary>
        void DeleteWalFile();

        /// <summary>
        /// Indicate that factory must be dispose on finish
        /// </summary>
        bool CloseOnDispose { get; }
    }
}