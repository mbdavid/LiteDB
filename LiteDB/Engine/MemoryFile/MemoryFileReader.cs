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
    /// Memory file reader - must call Dipose after use to return reader into pool
    /// 1 instance per thread - (NO thread safe)
    /// </summary>
    internal class MemoryFileReader : IDisposable
    {
        private readonly MemoryStore _store;
        private readonly ReaderWriterLockSlim _locker;
        private readonly StreamPool _pool;
        private readonly AesEncryption _aes;
        private readonly bool _writable;

        private readonly Lazy<Stream> _stream;

        private readonly List<PageBuffer> _tracker0 = new List<PageBuffer>(); // main tracker
        private readonly List<PageBuffer> _tracker1 = new List<PageBuffer>();

        public MemoryFileReader(MemoryStore store, ReaderWriterLockSlim locker, Lazy<Stream> stream, AesEncryption aes, bool writable)
        {
            _store = store;
            _locker = locker;
            _stream = stream;
            _aes = aes;
            _writable = writable;
        }

        /// <summary>
        /// Load page from Stream. If release = true, page will released at "ReleasePages()". If false, only on "Dispose()"
        /// </summary>
        public PageBuffer GetPage(long position, bool release)
        {
            ENSURE(_tracker0.Any(x => x.Position == position) == false, "only 1 page buffer instance per reader");
            ENSURE(_tracker1.Any(x => x.Position == position) == false, "only 1 page buffer instance per reader");

            var page = _writable ?
                _store.GetWritablePage(position, this.ReadStream) :
                _store.GetReadablePage(position, this.ReadStream);

            var track = release ? _tracker0 : _tracker1;

            track.Add(page);

            return page;
        }

        /// <summary>
        /// Read bytes from stream into buffer slice
        /// </summary>
        private void ReadStream(long position, BufferSlice buffer)
        {
            _stream.Value.Position = position;

            // read encrypted or plain data from Stream into buffer
            if (_aes != null)
            {
                _aes.Decrypt(_stream.Value, buffer);
            }
            else
            {
                _stream.Value.Read(buffer.Array, buffer.Offset, buffer.Count);
            }
        }

        /// <summary>
        /// Create new page in memory to be used when engine need add page into datafile (log file first)
        /// This page has position yet - will be appended on file only when WriteAsync
        /// If release = true, page will released at "ReleasePages()". If false, only on "Dispose()"
        /// </summary>
        public PageBuffer NewPage(bool release)
        {
            ENSURE(_writable, "only writable readers can create new pages");

            var page = _store.NewPage();

            var track = release ? _tracker0 : _tracker1;

            track.Add(page);

            return page;
        }

        /// <summary>
        /// Release all loaded pages that was loaded by this reader (marked as release). Decrement page share counter
        /// Release a page doesn't mean clear page - I'm only saing that this reader will not use this page anymore
        /// If page get 0 share counter will be cleaner in next cleanup thread
        /// </summary>
        public void ReleasePages()
        {
            this.ReleasePages(_tracker0);
        }

        private void ReleasePages(List<PageBuffer> track)
        {
            _locker.EnterReadLock();

            try
            {
                foreach (var page in track)
                {
                    _store.ReturnPage(page);
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }

            track.Clear();
        }

        /// <summary>
        /// Decrement share-counter for all pages used in this reader
        /// All page that was write before reader dispose was incremented, so will note be clean after here
        /// Release all pages (including pages marked as release = false)
        /// </summary>
        public void Dispose()
        {
            this.ReleasePages(_tracker0);
            this.ReleasePages(_tracker1);
        }
    }
}