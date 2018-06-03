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

        // async writer control - dirty pages queue
        private ConcurrentQueue<long> _dirtyQueue = new ConcurrentQueue<long>();
        private Task _asyncWriter;

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
        /// Flush data on disk - avoid OS cache when using FileStream
        /// </summary>
        private void Flush()
        {
            if (_writer.BaseStream is FileStream stream)
            {
                stream.Flush(true);
            }
            else
            {
                _writer.BaseStream.Flush();
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

                    // update "virtual" file size
                    if (_virtualPosition > _virtualLength) _virtualLength = _virtualPosition;
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
                _writer.BaseStream.Position = position;

                page.WritePage(_writer);

                // mark page as clean on cache
                page.IsDirty = false;
            }
        }

        /// <summary>
        /// Lock writer and wait all queue be write on disk
        /// </summary>
        public void WaitAsyncWrite()
        {
            // if has pages on queue but async writer are not running, run sync
            if (_dirtyQueue.IsEmpty == false && _asyncWriter.Status == TaskStatus.RanToCompletion)
            {
                this.RunWriterQueue();
            }

            // if async writer are running, wait to finish
            if (_asyncWriter != null && _asyncWriter.Status != TaskStatus.RanToCompletion)
            {
                _asyncWriter.Wait();
            }

            // do a disk flush
            this.Flush();
        }

        /// <summary>
        /// Write all WAL page in data file disk - this is sync write operation with absolute pages position
        /// </summary>
        public int WritePages(HeaderPage header, IEnumerable<BasePage> pages)
        {
            var count = 0;
            var lastPageID = header.LastPageID;

            foreach (var page in pages)
            {
                // WAL pages are write on absolute position
                var position = BasePage.GetPagePosition(page.PageID);

                _writer.BaseStream.Position = position;

                if (page.PageType == PageType.Header)
                {
                    lastPageID = (page as HeaderPage).LastPageID;
                }

                page.WritePage(_writer);

                _cache.AddPage(position, page);

                count++;
            }

            // get last page position
            var pos = BasePage.GetPagePosition(lastPageID + 1);

            // if virtual position changed than has pages on wal and need shrink datafile
            if (_virtualPosition != pos)
            {
                // flush all data into disk
                this.Flush();

                // position writer on end of file
                _virtualPosition = pos;

                // update virtual length (now virtual length = real length)
                _virtualLength = _virtualPosition;

                // and shrink rest of file (wal area)
                _writer.BaseStream.SetLength(_virtualPosition);
            }

            return count;
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
            if (initialSize > (PAGE_SIZE * 10))
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