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
    /// ThreadSafe
    /// </summary>
    internal class FileMemoryWriter : IDisposable
    {
        private readonly ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();
        private readonly FileMemoryCache _cache;

        private readonly Stream _stream;
        private long _appendPosition;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public FileMemoryWriter(Stream stream, FileMemoryCache cache)
        {
            _stream = stream;
            _cache = cache;

            // get append position in end of file (remove page_size length to use Interlock on write)
            _appendPosition = stream.Length - PAGE_SIZE;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);
            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_Writer";
            _thread.Start();
        }

        public void QueuePage(PageBuffer page, bool append)
        {
            // must increment page counter (will be decremented only when are real saved on disk) to avoid
            // be removed from cache during async writer time
            Interlocked.Increment(ref page.ShareCounter);

            if (append)
            {
                // adding this page into file AS new page (at end of file)
                // must add into cache to be sure that new readers can see this page

                Interlocked.Add(ref _appendPosition, PAGE_SIZE);

                page.Posistion = _appendPosition;

                // must add this page in cache because now this page are part of file
                _cache.AddPage(page);
            }

            _queue.Enqueue(page);
        }

        /// <summary>
        /// If queue contains pages and are not running, starts run queue again now
        /// </summary>
        public void RunQueue()
        {
            if (_queue.IsEmpty) return;
            if (_waiter.IsSet) return;

            _waiter.Set();
        }

        private void CreateThread()
        {
            // start thread waiting for first signal
            _waiter.Wait();

            while(_running)
            {
                while (_queue.TryDequeue(out var page))
                {
                    // set stream position according to page
                    _stream.Position = page.Posistion;

                    _stream.Write(page.Buffer.Array, page.Buffer.Offset, PAGE_SIZE);

                    // now this page are safe to be decremented (and clear by cache if needed)
                    Interlocked.Decrement(ref page.ShareCounter);
                }

                _stream.FlushToDisk();

                // suspend thread and wait another signal
                _waiter.Reset();
                _waiter.Wait();
            }
        }

        public void Dispose()
        {
            // stop running async task in next while check
            _running = false;

            if (_waiter.IsSet == false)
            {
                _waiter.Set();
            }

            // wait writer async task finish before dispose (be sync)
            _thread.Join();

            _waiter.Dispose();
            _stream.Dispose();
        }
    }
}