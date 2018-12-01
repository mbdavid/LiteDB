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
    internal class DiskWriter : IDisposable
    {
        private readonly ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();
        private readonly MemoryCache _cache;
        private readonly ReaderWriterLockSlim _locker;
        private readonly AesEncryption _aes;

        private readonly Stream _stream;

        private readonly bool _append;
        private long _appendPosition;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public DiskWriter(MemoryCache cache, ReaderWriterLockSlim locker, Stream stream, bool append, AesEncryption aes)
        {
            _cache = cache;
            _locker = locker;
            _aes = aes;

            _stream = stream;
            _append = append;

            // get append position in end of file (remove page_size length to use Interlock on write)
            _appendPosition = _stream.Length - PAGE_SIZE;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);
            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_Writer";
            _thread.Start();
        }

        /// <summary>
        /// Enqueue pages to write in async thread
        /// </summary>
        public void Write(IEnumerable<PageBuffer> pages)
        {
            _locker.EnterReadLock();

            try
            {
                foreach(var page in pages)
                {
                    this.QueuePage(page);
                }
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Add page into writer queue and will be saved in disk by another thread. If page.Position = MaxValue, store at end of file (will get final Position)
        /// After this method, this page will be available into reader as a clean page
        /// </summary>
        private void QueuePage(PageBuffer page)
        {
            ENSURE(page.ShareCounter == BUFFER_WRITABLE, "to queue page to write, page must be writable");

            if (_append || page.Position == long.MaxValue)
            {
                // adding this page into file AS new page (at end of file)
                // must add into cache to be sure that new readers can see this page
                page.Position = Interlocked.Add(ref _appendPosition, PAGE_SIZE);
            }
            else
            {
                // get highest value between new page or last page 
                // don't worry about concurrency becasue only 1 instance call this (Checkpoint)
                _appendPosition = Math.Max(_appendPosition, page.Position - PAGE_SIZE);
            }

            // mark this page as read-only and get cached paged to enqueue to write
            var readable = _cache.MoveToReadable(page);

            ENSURE(readable.ShareCounter >= 2, "cached page must be shared at least twice (becasue this method must be called before release pages)");

            _queue.Enqueue(readable);
        }

        /// <summary>
        /// Get file length
        /// </summary>
        public long Length => _appendPosition + PAGE_SIZE;

        /// <summary>
        /// Create a fake page to be added in queue. When queue run this page, just set new stream length
        /// </summary>
        public void SetLength(long length)
        {
            Interlocked.Exchange(ref _appendPosition, -PAGE_SIZE);

            _queue.Enqueue(new PageBuffer(null, 0, 0) { Position = length });
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
                    // if buffer is null, is a file length change
                    if (page.Array == null)
                    {
                        _stream.SetLength(page.Position);
                    }
                    else
                    {
                        ENSURE(page.ShareCounter > 0, "page must be shared at least 1");

                        // set stream position according to page
                        _stream.Position = page.Position;

                        // write plain or encrypted data into strea
                        if (_aes != null)
                        {
                            _aes.Encrypt(page, _stream);
                        }
                        else
                        {
                            _stream.Write(page.Array, page.Offset, PAGE_SIZE);
                        }

                        // release this page to be re-used
                        page.Release();
                    }
                }

                // after this I will have 100% sure data are safe
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
        }
    }
}