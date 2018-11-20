using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent a file in memory. Can be used for datafile, logfile or tempfile. Will have a single instance per engine instance/file
    /// Has an own instance of memory store (large byte[] in memory)
    /// [ThreadSafe]
    /// </summary>
    internal class MemoryFile : IDisposable
    {
        private readonly MemoryStore _store;
        private readonly StreamPool _pool;
        private readonly AesEncryption _aes;

        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private readonly Lazy<MemoryFileWriter> _writer;

        public MemoryFile(StreamPool pool, AesEncryption aes)
        {
            _pool = pool;
            _aes = aes;

            _store = new MemoryStore(_locker);

            // create lazy writer to avoid create file if not needed (readonly file access)
            _writer = new Lazy<MemoryFileWriter>(() => new MemoryFileWriter(_store, _locker, _pool, _aes));
        }

        /// <summary>
        /// Get file length (return 0 if not exists or if empty)
        /// </summary>
        public long Length => _pool.Factory.Exists() ? _writer.Value.Length : 0;

        /// <summary>
        /// Get how many bytes this file allocate inside heap memory
        /// </summary>
        public int MemoryBufferSize => _store.ExtendSegments * MEMORY_SEGMENT_SIZE * PAGE_SIZE;

        /// <summary>
        /// Get instance of MemoryFile for a single thread. This MemoryFileThread can't be shared across threads.
        /// </summary>
        public MemoryFileThread Open()
        {
            return new MemoryFileThread(_store, _pool, _aes, _locker, _writer);
        }

        public void Dispose()
        {
            // wait async writer task finish
            if (_writer.IsValueCreated)
            {
                _writer.Value.Dispose();
            }

            _store.Dispose();
            _locker.Dispose();
        }
    }
}