using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// </summary>
    internal class WalFileService : IDisposable
    {
        private ConcurrentBag<BinaryReader> _pool = new ConcurrentBag<BinaryReader>();
        private readonly IDiskFactory _factory;

        private readonly long _limitSize;
        private readonly Logger _log;
        private readonly bool _utcDate;

        private readonly Lazy<Stream> _stream;
        private readonly Lazy<BinaryWriter> _writer;

        /// <summary>
        /// Get limit of datafile in bytes (not WAL file size)
        /// </summary>
        public long LimitSize => _limitSize;

        /// <summary>
        /// Expose writer stream
        /// </summary>
        public Stream Stream => _stream.Value;

        public WalFileService(IDiskFactory factory, long sizeLimit, bool utcDate, Logger log)
        {
            _factory = factory;
            _limitSize = sizeLimit;
            _utcDate = utcDate;
            _log = log;

            // initialize lazy stream (and set position at end of file)
            _stream = new Lazy<Stream>(() =>
            {
                var s = _factory.GetWalFileStream(true);

                s.Seek(0, SeekOrigin.End);

                return s;
            });

            // inicialize lazy writer
            _writer = new Lazy<BinaryWriter>(() => new BinaryWriter(_stream.Value));
        }

        /// <summary>
        /// Get virtual file length (based on writer position)
        /// </summary>
        public long Length => _factory.IsWalFileExists() ? _stream.Value.Position : 0;

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetWalFileStream(false));

            try
            {
                reader.BaseStream.Position = position;

                // read binary data and create page instance page
                var page = BasePage.ReadPage(reader, true, _utcDate);

                return page;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(reader);
            }
        }

        /// <summary>
        /// Read all pages inside wal file in order. Locking writer to avoid writing durting my disk read. Read direct from disk with no cache
        /// </summary>
        public IEnumerable<BasePage> ReadPages(bool readContent)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetWalFileStream(false));

            lock(_stream)
            {
                try
                {
                    var stream = reader.BaseStream;

                    stream.Position = 0;

                    while (stream.Position < stream.Length)
                    {
                        var page = BasePage.ReadPage(reader, readContent, _utcDate);

                        yield return page;
                    }
                }
                finally
                {
                    // add stream back to pool
                    _pool.Add(reader);
                }
            }
        }

        /// <summary>
        /// Add all pages to queue using virtual position. Pages in this queue will be write on disk in async task
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages, IDictionary<uint, PagePosition> pagePositions)
        {
            // lock writer but don't use writer here (will be used only in async writer task)
            lock (_stream)
            {
                var stream = _stream.Value;

                foreach (var page in pages)
                {
                    DEBUG(page.IsDirty == false, "page always must be dirty when be write on disk");
                    DEBUG(page.TransactionID == ObjectId.Empty, "to write on wal, page must have a transactionID");

                    var pos = stream.Position;

                    page.WritePage(_writer.Value);

                    // return page position on disk (where will be write on disk)
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, pos);
                    }
                }
            }
        }

        /// <summary>
        /// Do a full flush do disk
        /// </summary>
        public void Flush() => _stream.Value.FlushToDisk();

        /// <summary>
        /// Clear WAL file content and reset writer position
        /// </summary>
        public void Clear()
        {
            lock(_stream)
            {
                // just shrink wal to 0 bytes (is faster than delete and can be re-used)
                _stream.Value.SetLength(0);

                _stream.Value.Position = 0;

                _stream.Value.FlushToDisk();
            }
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            // dispose only if disk factory require
            if (_factory.CloseOnDispose == false) return;

            _log.Info($"dispose wal file writer + {_pool.Count} readers)");

            var length = -1L;

            // first dispose writer
            if (_stream?.IsValueCreated ?? false)
            {
                length = _stream.Value.Length;

                _stream.Value.FlushToDisk();
                _stream.Value.Dispose();
            }

            // after, dispose all readers
            while (_pool.TryTake(out var reader))
            {
                reader.BaseStream.Dispose();
            }

            // delete wal file only if is complete empty
            if (length == 0)
            {
                _log.Info("deleting wal file because it's empty");

                _factory.DeleteWalFile();
            }
        }
    }
}