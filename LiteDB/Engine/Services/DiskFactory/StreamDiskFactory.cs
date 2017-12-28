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
        private MemoryStream _wal = new MemoryStream();
        private Stream _datafile;

        public StreamDiskFactory(Stream stream)
        {
            _datafile = stream;
        }

        /// <summary>
        /// No concurrency
        /// </summary>
        public int ConcurrencyLimit => 1;

        /// <summary>
        /// Do no dispose on finish
        /// </summary>
        public bool Dispose => false;

        /// <summary>
        /// Get always same Stream instance, do dot accept concurrency
        /// </summary>
        public Stream GetDataFile()
        {
            return _datafile;
        }

        public Stream GetWalFile()
        {
            return _wal;
        }
    }
}