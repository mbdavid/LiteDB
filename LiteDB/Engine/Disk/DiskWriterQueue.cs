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
    /// Implement disk write queue and async writer thread
    /// [ThreadSafe]
    /// </summary>
    internal class DiskWriterQueue : IDisposable
    {
        private readonly AesEncryption _aes;

        private readonly Stream _dataStream;
        private readonly Stream _logStream;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        private ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();

        public DiskWriterQueue(Stream dataStream, Stream logStream, AesEncryption aes)
        {
            _dataStream = dataStream;
            _logStream = logStream;
            _aes = aes;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);

            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_Writer";
            _thread.Start();
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
            _queue.Enqueue(page);
        }

        /// <summary>
        /// Create a fake page to be added in queue. When queue run this page, just set new stream length
        /// </summary>
        public void EnqueueLength(long length, FileOrigin origin)
        {
            _queue.Enqueue(new PageBuffer(null, 0, 0) { Position = length });
        }

        /// <summary>
        /// If queue contains pages and are not running, starts run queue again now
        /// </summary>
        public void Run()
        {
            if (_queue.Count > 0)
            {
                _waiter.Set();
            }
        }

        private void CreateThread()
        {
            // start thread waiting for first signal
            _waiter.Wait();

            while(_running)
            {
                this.ExecuteQueue();

                _waiter.Reset();

                if (_running == false) return;

                _waiter.Wait();
            }
        }

        /// <summary>
        /// Execute all items in queue sync
        /// </summary>
        private void ExecuteQueue()
        {
            if (_queue.Count == 0) return;

            Stream stream = null;
            var count = 0;

            while (_queue.TryDequeue(out var page))
            {
                stream = page.Origin == FileOrigin.Data ? _dataStream : _logStream;

                // if array is null, is Position = file length
                if (page.Array == null)
                {
                    stream.SetLength(page.Position);
                }
                else
                {
                    ENSURE(page.ShareCounter > 0, "page must be shared at least 1");

                    // set stream position according to page
                    stream.Position = page.Position;

                    // write plain or encrypted data into strea
                    if (_aes != null)
                    {
                        _aes.Encrypt(page, stream);
                    }
                    else
                    {
                        stream.Write(page.Array, page.Offset, PAGE_SIZE);
                    }

                    // release this page to be re-used
                    page.Release();

                    count++;
                }
            }

            LOG($"flushing {count} pages on disk", "DISK");

            // after this I will have 100% sure data are safe
            stream?.FlushToDisk();
        }

        public void Dispose()
        {
            this.Run();

            // stop running async task in next while check
            _running = false;

            _waiter.Set();

            // wait async task finish before dispose
            _thread.Join();

            _waiter.Dispose();

            // if still have any page in queue, run synchronized
            this.ExecuteQueue();

            ENSURE(_queue.Count == 0, "must have no pages in queue before Dispose");
        }
    }
}