using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB
{
    /// <summary>
    /// Implement thread safe stream data access
    /// - Pages can be encrypted in write and decrypted on read (except header page)
    /// - Pages are stored in memory cache
    /// - Read operations use pool of streams - multiple reads
    /// - Single writer operation that use queue to run in async task
    /// </summary>
    internal class FileService : IDisposable
    {
        private ConcurrentBag<BinaryReader> _pool = new ConcurrentBag<BinaryReader>();
        private IDiskFactory _factory;

        private TimeSpan _timeout;
        private long _sizeLimit;
        private Logger _log;
        private CacheService _cache;
        private bool _utcDate;

        private BinaryWriter _writer;
        private long _virtualPosition = 0;
        private long _virtualLength = 0;

        // async writer control
        private ConcurrentQueue<Tuple<long, BasePage>> _queue = new ConcurrentQueue<Tuple<long, BasePage>>();
        private Task _async;

        public FileService(IDiskFactory factory, TimeSpan timeout, long initialSize, long sizeLimit, bool utcDate, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _sizeLimit = sizeLimit;
            _utcDate = utcDate;
            _log = log;

            // initialize cache service
            _cache = new CacheService(_log);

            // get first stream (will be used as single writer)
            var stream = factory.GetStream();

            try
            {
                _writer = new BinaryWriter(stream);

                // if empty datafile, create database here
                if (stream.Length == 0)
                {
                    this.CreateDatafile(stream, initialSize);
                }

                // update virtual file length with real file length
                _virtualPosition = stream.Length;
                _virtualLength = stream.Length;
            }
            catch
            {
                // close stream if any error occurs
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Get/Set stream length - set operation must be sync before
        /// </summary>
        public long Length { get => _virtualLength; }

        /// <summary>
        /// Set new length for datafile in async mode - will be executed in queue order
        /// </summary>
        public void SetLength(long length)
        {
            lock (_writer)
            {
                // this queue item will be executed in queue async writer
                // will be run as a SetLength method on stream
                _queue.Enqueue(new Tuple<long, BasePage>(length, null));

                // update virtual file length
                _virtualLength = length;
            }
        }

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position, bool clone)
        {
            // try get page from cache
            var page = _cache.GetPage(position, clone);

            if (page != null) return page;

            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetStream());

            try
            {
                reader.BaseStream.Position = position;

                // read binary data and create page instance page
                page = BasePage.ReadPage(reader, _utcDate);

                // add fresh disk page into cache
                _cache.AddPage(position, page);

                return page;
            }
            finally
            {
                // add stream back to pool
                _pool.Add(reader);
            }
        }

        /// <summary>
        /// Get/Set position of virtual writer stream (lock with _writer)
        /// </summary>
        public long VirtualPosition
        {
            get
            {
                lock (_writer)
                {
                    return _virtualPosition;
                }
            }
            set
            {
                lock(_writer)
                {
                    _virtualPosition = value;
                }
            }
        }

        /// <summary>
        /// Add all pages to queue using virtual position. Pages in this queue will be write on disk in async task
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages, bool absolute, IDictionary<uint, PagePosition> pagePositions)
        {
            // lock writer but don't use writer here (will be used only in async writer task)
            lock (_writer)
            {
                foreach (var page in pages)
                {
                    // mark sure that page are marked as dirty (will be clean on async write)
                    page.IsDirty = true;

                    // if absolute position, set cursor position to pageID (otherwise use current position increment)
                    if (absolute)
                    {
                        _virtualPosition = BasePage.GetPagePosition(page.PageID);
                    }

                    // test max file size (includes wal operations)
                    if (_virtualPosition > _sizeLimit) throw LiteException.FileSizeExceeded(_sizeLimit);

                    // add dirty page to cache
                    _cache.AddPage(_virtualPosition, page);

                    // add to writer queue
                    _queue.Enqueue(new Tuple<long, BasePage>(_virtualPosition, page));

                    // return page position on disk (where will be write on disk)
                    if (pagePositions != null)
                    {
                        pagePositions[page.PageID] = new PagePosition(page.PageID, _virtualPosition);
                    }

                    _virtualPosition += BasePage.PAGE_SIZE;

                    // update "virtual" file size
                    if (_virtualPosition > _virtualLength) _virtualLength = _virtualPosition;
                }

                // if async writer are not running, start/re-start now
                if (_async == null || _async.Status == TaskStatus.RanToCompletion)
                {
                    _async = this.CreateAsyncWriter();
                    _async.Start();
                }
            }
        }

        /// <summary>
        /// Implement async writer disk in a background task - will consume all items on queue
        /// </summary>
        private Task CreateAsyncWriter()
        {
            return new Task(() =>
            {
                // write all pages that are in queue
                while (!_queue.IsEmpty)
                {
                    // get page from queue
                    if (!_queue.TryDequeue(out var item)) break;

                    var position = item.Item1;
                    var page = item.Item2;

                    // if page is empty, this is special queue item: SetLength
                    if (page == null)
                    {
                        // use position as file length
                        _writer.BaseStream.SetLength(position);
                        continue;
                    }

                    _writer.BaseStream.Position = position;

                    //TODO for debug propose
                    if (page.TransactionID == Guid.Empty && BasePage.GetPagePosition(page.PageID) != position) throw new Exception("Não pode ter pagina na WAL sem transação");

                    page.WritePage(_writer);

                    // set page position, in cache, not as dirty
                    _cache.ClearDirty(position);
                }

                // lock writer to clear dirty cache
                lock(_writer)
                {
                    // before clear cache, test if queue are empty, otherwise do not clear cache.
                    if (_queue.IsEmpty)
                    {
                        _cache.ClearDirty();
                    }
                }
            });
        }

        /// <summary>
        /// Lock writer and wait all queue be write on disk
        /// </summary>
        public void WaitAsyncWrite()
        {
            // if has pages on queue but async writer are not running, run sync
            if (_queue.IsEmpty == false && _async.Status == TaskStatus.RanToCompletion)
            {
                this.CreateAsyncWriter().RunSynchronously();
            }

            // if async writer are running, wait to finish
            if (_async != null && _async.Status != TaskStatus.RanToCompletion)
            {
                _async.Wait();
            }

            // do a disk flush
            _writer.BaseStream.Flush();
        }

        /// <summary>
        /// Create new datafile based in empty Stream
        /// </summary>
        private void CreateDatafile(Stream stream, long initialSize)
        {
            _writer = new BinaryWriter(stream);

            var header = new HeaderPage(0);

            header.WritePage(_writer);

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                //TODO must implement linked list - this initial will shrink in first checkpoint
                _writer.BaseStream.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Dispose all stream in pool and async writer
        /// </summary>
        public void Dispose()
        {
            // wait async
            this.WaitAsyncWrite();

            if (_factory.CloseOnDispose)
            {
                // first dispose writer
                _writer.BaseStream.Dispose();

                // after, dispose all readers
                while (_pool.TryTake(out var reader))
                {
                    reader.BaseStream.Dispose();
                }
            }
        }
    }
}