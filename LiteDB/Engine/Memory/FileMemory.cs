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
    /// Represent a file in memory. Can be used for datafile, logfile or tempfile. Will have a single instance per engine instance
    /// ThreadSafe
    /// </summary>
    internal class FileMemory : IDisposable
    {
        private readonly MemoryStore _memory = new MemoryStore();

        private readonly ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();

        private readonly IDiskFactory _factory;

        private readonly Lazy<FileMemoryWriter> _writer;

        public FileMemory(IDiskFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Get reader from pool (or from new instance). Must be Dispose after use
        /// </summary>
        public FileMemoryReader GetReader()
        {
            // checks if pool contains already opened stream
            if (!_pool.TryTake(out var stream))
            {
                stream = _factory.GetDataFileStream(true);
            }

            // acho que a factory deveria 1 para cada arquivo (1 data e 1 log)
            // settings.GetDatafileFactory() e GetLogfileFactory())

            // when reader dispose, return back stream to pool
            void disposing(Stream s) { _pool.Add(s); }

            // create new instance
            return new FileMemoryReader(disposing);
        }

        /// <summary>
        /// Write page at end of file and update Position. Pending pages will be avaiable in reader
        /// </summary>
        public void AppendWriteAsync(IEnumerable<PageBuffer> pages)
        {
            foreach(var page in pages)
            {
                _writer.Value.QueuePage(page, true);
            }

            _writer.Value.RunQueue();
        }

        /// <summary>
        /// Write page in original Position that was get from
        /// </summary>
        public void WriteAsync(PageBuffer page)
        {
            _writer.Value.QueuePage(page, false);
        }


        public void Dispose()
        {
            // dispose all stream
            foreach(var stream in _pool)
            {
                stream.Dispose();
            }

            if (_writer.IsValueCreated)
            {
                // do writer dispose (wait before all async writes)
                _writer.Value.Dispose();
            }

            _cache.Dispose();
        }
    }
}