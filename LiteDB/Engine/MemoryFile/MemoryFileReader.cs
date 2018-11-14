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
        private readonly Stream _stream;
        private readonly AesEncryption _aes;
        private readonly bool _writable;
        private readonly Action<Stream> _dispose;

        private readonly List<PageBuffer> _pages = new List<PageBuffer>();

        public MemoryFileReader(MemoryStore store, Stream stream, AesEncryption aes, bool writable, Action<Stream> dispose)
        {
            _store = store;
            _stream = stream;
            _aes = aes;
            _writable = writable;
            _dispose = dispose;
        }

        public PageBuffer GetPage(long position)
        {
            DEBUG(_pages.Any(x => x.Position == position), "only 1 page buffer instance per reader");

            var page = _writable ?
                _store.GetWritablePage(position, this.ReadStream) :
                _store.GetReadablePage(position, this.ReadStream);

            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Read bytes from stream into buffer slice
        /// </summary>
        private void ReadStream(long position, ArraySlice<byte> buffer)
        {
            _stream.Position = position;

            // read encrypted or plain data from Stream into buffer
            if (_aes != null)
            {
                _aes.Decrypt(_stream, buffer);
            }
            else
            {
                _stream.Read(buffer.Array, buffer.Offset, buffer.Count);
            }
        }

        /// <summary>
        /// Create new page in memory to be used when engine need add page into datafile (log file first)
        /// This page has position yet - will be appended on file only when WriteAsync
        /// </summary>
        public PageBuffer NewPage()
        {
            DEBUG(_writable == false, "only writable readers can create new pages");

            var page = _store.NewPage();

            _pages.Add(page);

            return page;
        }

        /// <summary>
        /// Release all loaded pages that was loaded by this reader. Decrement page share counter
        /// Release a page doesn't mean clear page - I'm only saing that this reader will not use this page anymore
        /// If page get 0 share counter will be cleaner in next cleanup thread
        /// </summary>
        public void ReleasePages()
        {
            foreach(var page in _pages)
            {
                _store.ReturnPage(page);
            }

            _pages.Clear();
        }

        /// <summary>
        /// Decrement share-counter for all pages used in this reader
        /// All page that was write before reader dispose was incremented, so will note be clean after here
        /// </summary>
        public void Dispose()
        {
            this.ReleasePages();

            // return stream back to pool
            _dispose(_stream);
        }
    }
}