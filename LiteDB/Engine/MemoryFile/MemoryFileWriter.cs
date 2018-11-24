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
    internal class MemoryFileWriter : IDisposable
    {
        private readonly ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();
        private readonly MemoryStore _store;
        private readonly ReaderWriterLockSlim _locker;
        private readonly AesEncryption _aes;

        private readonly Stream _stream;
        private readonly DbFileMode _mode;
        private long _appendPosition;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public MemoryFileWriter(MemoryStore store, ReaderWriterLockSlim locker, StreamPool pool, AesEncryption aes)
        {
            _store = store;
            _locker = locker;
            _aes = aes;

            _mode = pool.Factory.FileMode;
            _stream = pool.Writer;

            // get append position in end of file (remove page_size length to use Interlock on write)
            _appendPosition = _stream.Length - PAGE_SIZE;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);
            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_Writer";
            _thread.Start();
        }

        public long Length => _appendPosition + PAGE_SIZE;

        /// <summary>
        /// Add page into writer queue and will be saved in disk by another thread. If page.Position = MaxValue, store at end of file (will get final Position)
        /// After this method, this page will be available into reader as a clean page
        /// </summary>
        public void QueuePage(PageBuffer page)
        {
            ENSURE(page.ShareCounter == BUFFER_WRITABLE, "to queue page to write, page must be writable");

            if (_mode == DbFileMode.Logfile || page.Position == long.MaxValue)
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
            var cached = _store.MarkAsReadOnly(page);

            ENSURE(page.ShareCounter >= 2, "cached page must be shared at least twice (becasue this method must be called before release pages)");
            ENSURE(page.Position == cached.Position, "cached and page position must be equals (are same updated page)");

            _queue.Enqueue(cached);
        }

        /// <summary>
        /// Create a fake page to be added in queue. When queue run this page, just set new stream length
        /// </summary>
        public void QueueLength(long length)
        {
            Interlocked.Exchange(ref _appendPosition, -PAGE_SIZE);

            _queue.Enqueue(new PageBuffer(null, 0) { Position = length });
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
                        ENSURE(_store.InCache(page), "page (instance+position) must be cache inside");

                        // set stream position according to page
                        _stream.Position = page.Position;

                        // write plain or encrypted data into strea
                        if (_aes != null)
                        {
                            _aes.Encrypt(page, _stream);

                            // in datafile, header page will store plain SALT encryption
                            if (page.Position == 0 && _mode == DbFileMode.Datafile)
                            {
                                _stream.Position = P_ENCRYPTION_SALT;
                                _stream.Write(_aes.Salt, 0, ENCRYPTION_SALT_SIZE);
                            }
                        }
                        else
                        {
                            _stream.Write(page.Array, page.Offset, PAGE_SIZE);
                        }

                        // enter in read lock before return page
                        _locker.EnterReadLock();

                        try
                        {
                            _store.ReturnPage(page);
                        }
                        finally
                        {
                            _locker.ExitReadLock();
                        }
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