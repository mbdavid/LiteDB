using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Engine
{
    /// <summary>
    /// Simple stream disk implementation of disk factory - used for Memory/Temp database
    /// </summary>
    public class StreamDiskFactory : IDiskFactory
    {
        private Stream _data;
        private Stream _wal;

        public StreamDiskFactory(Stream data, Stream wal)
        {
            _data = data;
            _wal = wal;
        }

        /// <summary>
        /// Stream has no name (use stream type)
        /// </summary>
        public string FileName => _data is MemoryStream ? ":memory:" : _data is TempStream ? ":temp:" : ":stream:";

        /// <summary>
        /// Use ConcurrentStream wrapper to support multi thread in same Stream (using lock control)
        /// </summary>
        public Stream GetDataFileStream(bool write) => new ConcurrentStream(_data);

        public Stream GetWalFileStream(bool write) => new ConcurrentStream(_wal);

        public bool IsWalFileExists() => _wal.Length > 0;

        public void DeleteWalFile()
        {
            // stream factory do not delete wal file
        }

        /// <summary>
        /// Do no dispose on finish
        /// </summary>
        public bool CloseOnDispose => false;
    }
}