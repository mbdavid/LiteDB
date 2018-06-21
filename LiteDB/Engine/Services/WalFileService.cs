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
        private IDiskFactory _factory;

        private TimeSpan _timeout;
        private long _sizeLimit;
        private Logger _log;
        private CacheService _cache;
        private bool _utcDate;

        private Lazy<BinaryWriter> _writer;
        private long _virtualPosition = 0;

        // async writer control - dirty pages queue
        private ConcurrentQueue<long> _dirtyQueue = new ConcurrentQueue<long>();
        private Task _asyncWriter;

        public CacheService Cache => _cache;

        public WalFileService(IDiskFactory factory, TimeSpan timeout, long sizeLimit, bool utcDate, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _utcDate = utcDate;
            _log = log;

            // initialize cache service
            _cache = new CacheService(_log);

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
        public long Length => _virtualPosition;

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position, bool clone)
        {
            // try get page from cache
            var page = _cache.GetPage(position, clone);

            if (page != null) return page;

            // if not found, get from disk
            page = this.ReadPageDisk(position);

            // and them add to cache (if page will be used to write, insert in cache clone copy)
            _cache.AddPage(position, clone ? page.Clone() : page);

            return page;
        }

        /// <summary>
        /// Read page direct from disk, ignoring cache
        /// </summary>
        private BasePage ReadPageDisk(long position)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetWalFileStream(false));

            try
            {
                reader.BaseStream.Position = position;

                // read binary data and create page instance page
                var page = BasePage.ReadPage(reader, _utcDate);

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

            if (_virtualPosition > 0)
            {
                return true;
            }
            else
            {
                return _writer.Value.BaseStream.Length > 0;
            }
        }

        /// <summary>
        /// Read all pages inside wal file in order, using _writer stream (is sequencial stream). Do not use cache (read all direct from disk)
        /// </summary>
        public IEnumerable<BasePage> ReadPages()
        {
            lock (_writer)
            {
                // before read pages from disk, wait any async write
                this.WaitAsyncWrite(false);

                var stream = _writer.Value.BaseStream;

                stream.Position = 0;

                var reader = new BinaryReader(_writer.Value.BaseStream);

                while (stream.Position < stream.Length)
                {
                    var page = BasePage.ReadPage(reader, _utcDate);

                    yield return page;
                }

                DEBUG(_virtualPosition > 0 && stream.Position != _virtualPosition, "After read all pages, virtual position must be same as current stream position");
            }
        }

        /// <summary>
        /// Add all pages to queue using virtual position. Pages in this queue will be write on disk in async task
        /// </summary>
        public void WriteAsyncPages(IEnumerable<BasePage> pages, IDictionary<uint, PagePosition> pagePositions)
        {
            // lock writer but don't use writer here (will be used only in async writer task)
            lock (_writer)
            {
                foreach (var page in pages)
                {
                    DEBUG(page.IsDirty == false, "page always must be dirty when be write on disk (async mode)");
                    DEBUG(page.TransactionID == Guid.Empty, "to write on wal, page must have a transactionID");

                    // test max file size (includes wal operations)
                    if (_virtualPosition > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

                    // add dirty page to cache
                    _cache.AddPage(_virtualPosition, page);

                    // add to writer queue
                    _dirtyQueue.Enqueue(_virtualPosition);

                    // return page position on disk (where will be write on disk)
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, _virtualPosition);
                    }

                    _virtualPosition += PAGE_SIZE;
                }

                // if async writer are not running, start/re-start now
                if (_asyncWriter == null || _asyncWriter.Status == TaskStatus.RanToCompletion)
                {
                    _asyncWriter = new Task(this.RunWriterQueue);
                    _asyncWriter.Start();
                }
            }
        }

        /// <summary>
        /// Consule all writer queue saving on disk all queued pages
        /// </summary>
        private void RunWriterQueue()
        {
            // write all pages that are in queue
            while (!_dirtyQueue.IsEmpty)
            {
                // get page from queue
                if (!_dirtyQueue.TryDequeue(out var position)) break;

                // get dirty page from cache
                var page = _cache.GetPage(position, false);

                DEBUG(page == null, "page must always exists on cache");
                DEBUG(page.TransactionID == Guid.Empty && BasePage.GetPagePosition(page.PageID) != position, "não pode ter pagina na WAL sem transação");

                // position cursor and write page on disk
                _writer.Value.BaseStream.Position = position;

                page.WritePage(_writer.Value);

                page.IsDirty = false;
            }
        }

        /// <summary>
        /// Lock writer and wait all queue be write on disk
        /// </summary>
        public void WaitAsyncWrite(bool flush)
        {
            // if has pages on queue but async writer are not running, run sync
            if (_dirtyQueue.IsEmpty == false && _asyncWriter?.Status == TaskStatus.RanToCompletion)
            {
                this.RunWriterQueue();
            }

            // if async writer are running, wait to finish
            if (_asyncWriter != null && _asyncWriter.Status != TaskStatus.RanToCompletion)
            {
                _asyncWriter.Wait();
            }

            // do a disk flush
            if (_writer.IsValueCreated && flush)
            {
                _writer.Value.BaseStream.FlushToDisk();
            }
        }

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

                _virtualPosition = 0;

                _cache.Clear();
            }
        }

        /// <summary>
        /// Delete WAL file (check before if is empty) and re-initialize writer for new file
        /// </summary>
        public bool Delete()
        {
            if (_factory.IsWalFileExists() == false) return true;

            if (_writer.IsValueCreated && _writer.Value.BaseStream.Length == 0)
            {
                this.Dispose();

                _factory.DeleteWalFile();

                this.InitializeWriter();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            _log.Info($"dispose wal file ({(_writer.IsValueCreated ? 0 : 1)} writer + {_pool.Count} readers)");

            // first dispose writer
            if (_writer.IsValueCreated)
            {
                _writer.Value.BaseStream.Dispose();
            }

            // after, dispose all readers
            while (_pool.TryTake(out var reader))
            {
                reader.BaseStream.Dispose();
            }
        }
    }
}