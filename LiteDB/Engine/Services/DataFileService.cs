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

        private BinaryWriter _writer;

        public DataFileService(IDiskFactory factory, long initialSize, bool utcDate, Logger log)
        {
            _factory = factory;
            _utcDate = utcDate;
            _log = log;

            // get first stream (will be used as single writer)
            var stream = factory.GetDataFileStream(false);

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
        /// Set datafile with new length
        /// </summary>
        public void SetLength(long length) => _writer.BaseStream.SetLength(length);

        /// <summary>
        /// Read page bytes from disk (use stream pool) - Always return a fresh (never used) page instance.
        /// </summary>
        public BasePage ReadPage(long position)
        {
            // try get reader from pool (if not exists, create new stream from factory)
            if (!_pool.TryTake(out var reader)) reader = new BinaryReader(_factory.GetDataFileStream(true));

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
            lock (_writer)
            {
                var stream = _writer.BaseStream;
                var reader = new BinaryReader(_writer.BaseStream);

                var position = stream.Position = 0;
                var length = stream.Length;

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
            lock(_writer)
            {
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
                _log.Info($"dispose data file (1 writer + {_pool.Count} readers)");

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