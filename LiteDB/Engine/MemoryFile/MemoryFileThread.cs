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
    /// Represent one instace of Memory File per thread. Cann't be shared same instance across threads. Use 1 Stream per thread
    /// [no thread-safe]
    /// </summary>
    internal class MemoryFileThread : IDisposable
    {
        private readonly MemoryStore _store;
        private readonly StreamPool _pool;
        private readonly AesEncryption _aes;
        private readonly ReaderWriterLockSlim _locker;
        private readonly Lazy<MemoryFileWriter> _writer;

        private readonly Lazy<Stream> _stream;

        public MemoryFileThread(MemoryStore store,  StreamPool pool, AesEncryption aes, ReaderWriterLockSlim locker, Lazy<MemoryFileWriter> writer)
        {
            _store = store;
            _pool = pool;
            _aes = aes;
            _locker = locker;
            _writer = writer;

            _stream = new Lazy<Stream>(() => pool.Rent());
        }

        /// <summary>
        /// Get a new reader instance re-using same thread Stream. Only 1 Stream per thread
        /// </summary>
        public MemoryFileReader GetReader(bool writable)
        {
            return new MemoryFileReader(_store, _locker, _stream, _aes, writable);
        }

        /// <summary>
        /// Write page on file in async task (another thread)
        /// If file is AppendOnly (log file) page will be saved on end of file and will update Position. 
        /// Pending pages will be avaiable in reader
        /// To write any page you must get this page from Reader and dispose reader only after ready write all of them
        /// </summary>
        public void WriteAsync(IEnumerable<PageBuffer> pages)
        {
            var counter = 0;

            _locker.EnterReadLock();

            try
            {
                foreach (var page in pages)
                {
                    _writer.Value.QueuePage(page);

                    counter++;
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }

            if (counter > 0)
            {
                _writer.Value.RunQueue();
            }
        }

        /// <summary>
        /// Define new length to file stream in async queue
        /// </summary>
        public void SetLengthAsync(long length)
        {
            _writer.Value.QueueLength(length);

            _writer.Value.RunQueue();
        }

        public void Dispose()
        {
            if (_stream.IsValueCreated)
            {
                _pool.Return(_stream.Value);
            }
        }
    }
}