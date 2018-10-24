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
        private bool _appendOnly;
        private long _appendPosition;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public FileMemoryWriter(Stream stream, FileMemoryCache cache, bool appendOnly)
        {
            _stream = stream;
            _cache = cache;

            // get append position in end of file (remove page_size length to use Interlock on write)
            _appendOnly = appendOnly;
            _appendPosition = stream.Length - PAGE_SIZE;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);
            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_Writer";
            _thread.Start();
        }

        public long Length => _appendPosition + PAGE_SIZE;

        public long QueuePage(PageBuffer page)
        {
            // must increment share counter (will be decremented only when are real saved on disk) to avoid
            // be removed from cache during async writer time
            Interlocked.Increment(ref page.ShareCounter);

            if (_appendOnly)
            {
                // adding this page into file AS new page (at end of file)
                // must add into cache to be sure that new readers can see this page
                page.Position = Interlocked.Add(ref _appendPosition, PAGE_SIZE);

                // must add this page in cache because now this page are part of file
                _cache.AddPage(page);
            }
            else
            {
                // get highest value between new page or last page 
                // don't worry about concurrency becasue only 1 instance call this (Checkpoint)
                _appendPosition = Math.Max(_appendPosition, page.Position - PAGE_SIZE);
            }

            _queue.Enqueue(page);

            return page.Position;
        }

        /// <summary>
        /// Create a fake page to be added in queue. When queue run this page, just set new stream length
        /// </summary>
        public void QueueLength(long length)
        {
            Interlocked.Exchange(ref _appendPosition, -PAGE_SIZE);

            _queue.Enqueue(new PageBuffer { Position = length, ShareCounter = -1 });
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
                    if (page.ShareCounter == -1)
                    {
                        // when share counter is -1 this is not a regular page, is a set length command
                        _stream.SetLength(page.Position);
                    }
                    else
                    {
                        // set stream position according to page
                        _stream.Position = page.Position;

                        // write page on disk
                        _stream.Write(page.Buffer.Array, page.Buffer.Offset, PAGE_SIZE);

                        // now this page are safe to be decremented (and clear by cache if needed)
                        Interlocked.Decrement(ref page.ShareCounter);
                    }
                }

                _stream.FlushToDisk();

                // suspend thread and wait another signal
                _waiter.Reset();

                if (!_running) return;

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