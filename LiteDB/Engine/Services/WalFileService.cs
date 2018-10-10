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

        private Lazy<BinaryWriter> _writer;

        /// <summary>
        /// Get limit of datafile in bytes (not WAL file size)
        /// </summary>
        public long LimitSize => _limitSize;

        public WalFileService(IDiskFactory factory, long sizeLimit, bool utcDate, Logger log)
        {
            _factory = factory;
            _limitSize = sizeLimit;
            _utcDate = utcDate;
            _log = log;

            this.InitializeWriter();
        }

        private void InitializeWriter()
        {
            // initialize lazy writer
            _writer = new Lazy<BinaryWriter>(() =>
            {
                var stream = _factory.GetWalFileStream(true);

                return new BinaryWriter(stream);
            });
        }
        /// <summary>
        /// Get virtual file length
        /// </summary>
        public long Length => _factory.IsWalFileExists() ? _writer.Value.BaseStream.Length : 0;

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
        /// Return if WAL file contains pages
        /// </summary>
        public bool HasPages()
        {
            if (_factory.IsWalFileExists() == false) return false;

            return _writer.Value.BaseStream.Length > 0;
        }

        /// <summary>
        /// Read all pages inside wal file in order. Locking writer to avoid writing durting my disk read. Read direct from disk with no cache
        /// </summary>
        public IEnumerable<BasePage> ReadPages(bool readContent)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetWalFileStream(false));

            lock(_writer)
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
            lock (_writer)
            {
                var stream = _writer.Value.BaseStream;

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
        public void Flush() => _writer.Value.BaseStream.FlushToDisk();

        /// <summary>
        /// Clear WAL file content and reset writer position
        /// </summary>
        public void Clear()
        {
            lock(_writer)
            {
                // just shrink wal to 0 bytes (is faster than delete and can be re-used)
                var stream = _writer.Value.BaseStream;

                stream.SetLength(0);

                stream.Position = 0;
            }
        }

        /// <summary>
        /// Define writer position
        /// </summary>
        public void SetPosition(long position)
        {
            _writer.Value.BaseStream.Position = position;
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            _log.Info($"dispose wal file writer + {_pool.Count} readers)");

            var length = -1L;

            // first dispose writer
            if (_writer?.IsValueCreated ?? false)
            {
                length = _writer.Value.BaseStream.Length;

                _writer.Value.BaseStream.FlushToDisk();
                _writer.Value.BaseStream.Dispose();
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