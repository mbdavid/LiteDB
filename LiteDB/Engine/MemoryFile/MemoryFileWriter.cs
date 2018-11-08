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

        private readonly Stream _stream;
        private readonly bool _appendOnly;
        private long _appendPosition;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public MemoryFileWriter(Stream stream, MemoryStore store, bool appendOnly)
        {
            _stream = stream;
            _store = store;

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

        /// <summary>
        /// Add page into writer queue and will be saved in disk by another thread. If appendOnly will recive last position in stream
        /// After this method, this page will be available into reader as a clean page
        /// </summary>
        public long QueuePage(PageBuffer page)
        {
            DEBUG(page.IsWritable == false, "to queue page to write, page must be writable");

            if (_appendOnly)
            {
                // adding this page into file AS new page (at end of file)
                // must add into cache to be sure that new readers can see this page
                page.Position = Interlocked.Add(ref _appendPosition, PAGE_SIZE);

            }
            else
            {
                DEBUG(page.Position != long.MaxValue, "if writer are not appendOnly must contains disk position");

                // get highest value between new page or last page 
                // don't worry about concurrency becasue only 1 instance call this (Checkpoint)
                _appendPosition = Math.Max(_appendPosition, page.Position - PAGE_SIZE);
            }

            _store.MarkAsClean(page);

            _queue.Enqueue(page);

            return page.Position;
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
                var saved = new List<PageBuffer>();

                while (_queue.TryDequeue(out var page))
                {
                    // if buffer is null, is a file length change
                    if (page.Array == null)
                    {
                        _stream.SetLength(Math.Abs(page.Position));
                    }
                    else
                    {
                        // set stream position according to page
                        _stream.Position = page.Position;

                        // write page on disk
                        _stream.Write(page.Array, page.Offset, PAGE_SIZE);

                        // now I can assume page are already in disk 
                        // (even not 100% sure, but Strem.Read method will read new data)
                        // I can release page to be re-used by another thread
                        _store.ReturnPage(page);
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
            _stream.Dispose();
        }
    }
}