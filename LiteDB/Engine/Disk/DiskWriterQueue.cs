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
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement disk write queue and async writer thread - used only for write on LOG file
    /// [ThreadSafe]
    /// </summary>
    internal class DiskWriterQueue : IDisposable
    {
        private readonly Stream _stream;

        // async thread controls
        private Task _task;

        private ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();

        private int _running = 0;

        public DiskWriterQueue(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Get how many pages are waiting for store
        /// </summary>
        public int Length => _queue.Count;

        /// <summary>
        /// Add page into writer queue and will be saved in disk by another thread. If page.Position = MaxValue, store at end of file (will get final Position)
        /// After this method, this page will be available into reader as a clean page
        /// </summary>
        public void EnqueuePage(PageBuffer page)
        {
            ENSURE(page.Origin == FileOrigin.Log, "async writer must use only for Log file");

            _queue.Enqueue(page);
        }

        /// <summary>
        /// If queue contains pages and are not running, starts run queue again now
        /// </summary>
        public void Run()
        {
            lock (_queue)
            {
                if (_queue.Count == 0) return;

                var oldValue = Interlocked.CompareExchange(ref _running, 1, 0);

                if (oldValue == 0)
                {
                    // Schedule a new thread to process the pages in the queue.
                    // https://blog.stephencleary.com/2013/08/startnew-is-dangerous.html
                    _task = Task.Run(ExecuteQueue);
                }
            }
        }

        /// <summary>
        /// Wait until all queue be executed and no more pending pages are waiting for write - be sure you do a full lock database before call this
        /// </summary>
        public void Wait()
        {
            lock (_queue)
            {
                if (_task != null)
                {
                    _task.Wait();
                }

                Run();
            }

            ENSURE(_queue.Count == 0, "queue should be empty after wait() call");
        }

        /// <summary>
        /// Execute all items in queue sync
        /// </summary>
        private void ExecuteQueue()
        {
            if (_queue.Count == 0) return;

            do
            {
                if (_queue.TryDequeue(out var page))
                {
                    WritePageToStream(page);
                }

                while (page == null)
                {
                    _stream.FlushToDisk();
                    Volatile.Write(ref _running, 0);

                    if (!_queue.Any()) return;

                    // Another item was added to the queue after we detected it was empty.
                    var oldValue = Interlocked.CompareExchange(ref _running, 1, 0);

                    if (oldValue == 1)
                    {
                        // A new thread was already scheduled for execution, this thread can return.
                        return;
                    }

                    // This thread will continue to process the queue as a new thread was not scheduled.
                    _queue.TryDequeue(out page);
                    WritePageToStream(page);
                }

            } while (true);
        }

        private void WritePageToStream(PageBuffer page)
        {
            if (page == null) return;

            ENSURE(page.ShareCounter > 0, "page must be shared at least 1");

            // set stream position according to page
            _stream.Position = page.Position;

            _stream.Write(page.Array, page.Offset, PAGE_SIZE);

            // release page here (no page use after this)
            page.Release();
        }

        public void Dispose()
        {
            LOG($"disposing disk writer queue (with {_queue.Count} pages in queue)", "DISK");

            // run all items in queue before dispose
            this.Wait();
        }
    }
}