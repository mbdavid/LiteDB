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
    /// Implement thread safe data file data access
    /// </summary>
    internal class DataFileService : IDisposable
    {
        private ConcurrentBag<BinaryReader> _pool = new ConcurrentBag<BinaryReader>();
        private IDiskFactory _factory;

        private TimeSpan _timeout;
        private Logger _log;
        private CacheService _cache;
        private bool _utcDate;

        private BinaryWriter _writer;

        public CacheService Cache => _cache;

        public DataFileService(IDiskFactory factory, TimeSpan timeout, long initialSize, bool utcDate, Logger log)
        {
            _factory = factory;
            _timeout = timeout;
            _utcDate = utcDate;
            _log = log;

            // initialize cache service
            _cache = new CacheService(_log);

            // get first stream (will be used as single writer)
            var stream = factory.GetDataFileStream(true);

            try
            {
                _writer = new BinaryWriter(stream);

                // if empty datafile, create database here
                if (stream.Length == 0)
                {
                    this.CreateDatafile(stream, initialSize);
                }
            }
            catch
            {
                // close stream if any error occurs
                stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Get data file stream length
        /// </summary>
        public long Length { get => _writer.BaseStream.Length; }

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
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetDataFileStream(false));

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
        /// Write all pages to disk on absolute position (flush after write)
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages)
        {
            lock(_writer)
            {
                // must clear cache before start writing from WAL file
                // because wal pages are different from current wal (and must be update in cache)
                _cache.Clear();

                foreach(var page in pages)
                {
                    var position = BasePage.GetPagePosition(page.PageID);

                    _writer.BaseStream.Position = position;

                    page.WritePage(_writer);
                }

                _writer.BaseStream.FlushToDisk();
            }
        }

        /// <summary>
        /// Create new datafile based in empty Stream
        /// </summary>
        private void CreateDatafile(Stream stream, long initialSize)
        {
            _writer = new BinaryWriter(stream);

            var header = new HeaderPage(0);

            header.WritePage(_writer);

            // if has initial size alocate disk space now
            if (initialSize > PAGE_SIZE)
            {
                _writer.BaseStream.SetLength(initialSize);
            }

            _writer.BaseStream.FlushToDisk();
        }

        /// <summary>
        /// Dispose all stream in pool and writer
        /// </summary>
        public void Dispose()
        {
            if (_factory.CloseOnDispose)
            {
                // dispose writer
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