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
        private readonly ConcurrentBag<BinaryReader> _pool = new ConcurrentBag<BinaryReader>();
        private readonly IDiskFactory _factory;

        private readonly Logger _log;
        private readonly bool _utcDate;

        private readonly Stream _stream;
        private readonly BinaryWriter _writer;

        public DataFileService(IDiskFactory factory, long initialSize, bool utcDate, Logger log)
        {
            _factory = factory;
            _utcDate = utcDate;
            _log = log;

            // get first stream (will be used as single writer)
            _stream = factory.GetDataFileStream(true);

            try
            {
                // create _writer only if 
                if (_stream.CanWrite)
                {
                    _writer = new BinaryWriter(_stream);
                }

                // if empty datafile, create database here
                if (_stream.Length == 0)
                {
                    // can create readonly database
                    if (_writer == null) throw new LiteException(0, $"Readonly only database can't create file {factory.Filename}");

                    this.CreateDatafile(initialSize);
                }
            }
            catch
            {
                // close stream if any error occurs
                _stream.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Get data file stream length
        /// </summary>
        public long Length { get => _stream.Length; }

        /// <summary>
        /// Set datafile with new length
        /// </summary>
        public void SetLength(long length) => _stream.SetLength(length);

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetDataFileStream(false));

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
        /// Read all zero pages inside datafile - zero pages was loaded by full empty of 
        /// </summary>
        public IEnumerable<uint> ReadZeroPages()
        {
            lock (_stream)
            {
                var reader = new BinaryReader(_stream);

                var position = _stream.Position = 0;
                var length = _stream.Length;

                while (position < length)
                {
                    // read page bytes to test if is full of zeros
                    var buffer = reader.ReadBinary(PAGE_SIZE);

                    if (buffer.IsFullZero())
                    {
                        var pageID = (uint)(position / PAGE_SIZE);

                        yield return pageID;
                    }

                    position += PAGE_SIZE;
                }
            }
        }

        /// <summary>
        /// Write all pages to disk on absolute position (flush after write)
        /// </summary>
        public void WritePages(IEnumerable<BasePage> pages)
        {
            lock(_stream)
            {
                foreach(var page in pages)
                {
                    var position = BasePage.GetPagePosition(page.PageID);

                    _stream.Position = position;

                    page.WritePage(_writer);
                }

                _stream.FlushToDisk();
            }
        }

        /// <summary>
        /// Create new datafile based in empty Stream
        /// </summary>
        private void CreateDatafile(long initialSize)
        {
            var header = new HeaderPage(0);

            header.WritePage(_writer);

            // if has initial size alocate disk space now
            if (initialSize > PAGE_SIZE)
            {
                _stream.SetLength(initialSize);
            }

            _stream.FlushToDisk();
        }

        /// <summary>
        /// Dispose all stream in pool and writer
        /// </summary>
        public void Dispose()
        {
            if (_factory.CloseOnDispose)
            {
                _log.Info($"dispose data file (1 writer + {_pool.Count} readers)");

                // dispose writer
                _stream.Dispose();

                // after, dispose all readers
                while (_pool.TryTake(out var reader))
                {
                    reader.BaseStream.Dispose();
                }
            }
        }
    }
}