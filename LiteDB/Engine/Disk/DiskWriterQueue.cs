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
        private readonly Stream _stream;

        // async thread controls
        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private readonly ManualResetEventSlim _writing;

        private bool _running = true;

        private ConcurrentQueue<PageBuffer> _queue = new ConcurrentQueue<PageBuffer>();

        /// <summary>
        /// Callback function to notify that some transaction already saved in disk
        /// </summary>
        private readonly Action<uint> _flushed;

        public DiskWriterQueue(Stream stream, AesEncryption aes, Action<uint> flushed)
        {
            _stream = stream;
            _aes = aes;
            _flushed = flushed;

            // prepare async thread writer
            _waiter = new ManualResetEventSlim(false);
            _writing = new ManualResetEventSlim(false);

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
            ENSURE(page.Origin == FileOrigin.Log, "async writer must use only for Log file");

            _queue.Enqueue(page);
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

        /// <summary>
        /// Wait until all queue be executed and no more pending pages are waiting for write
        /// </summary>
        public void Wait()
        {
            if (_writing.IsSet == false)
            {
                _waiter.Set();
                _writing.Wait();
            }

            if (_queue.Count > 0)
            {
                _writing.Reset();
                _waiter.Set();
                _writing.Wait();
            }

            ENSURE(_queue.Count == 0, "queue should be empty after wait() call");
        }

        private void CreateThread()
        {
            while(_running)
            {
                // wait next `_waiter.Set()`
                _waiter.Wait();

                if (_running == false) return;

                // lock `_writing` in next wait
                _writing.Reset();

                this.ExecuteQueue();

                // release `_writing` lock
                _writing.Set();

                // lock `_waiter` in next wait
                _waiter.Reset();
            }
        }

        /// <summary>
        /// Execute all items in queue sync
        /// </summary>
        private void ExecuteQueue()
        {
            if (_queue.Count == 0) return;

            var transactions = new List<uint>();
            var count = 0;

            while (_queue.TryDequeue(out var page))
            {
                ENSURE(page.ShareCounter > 0, "page must be shared at least 1");

                // set stream position according to page
                _stream.Position = page.Position;

                // write plain or encrypted data into stream
                if (_aes != null)
                {
                    _aes.Encrypt(page, _stream);
                }
                else
                {
                    _stream.Write(page.Array, page.Offset, PAGE_SIZE);
                }

                // when writing in log file a confirmed page, trigger event
                if (page.Origin == FileOrigin.Log && page[BasePage.P_IS_CONFIRMED] == 1)
                {
                    transactions.Add(page.ReadUInt32(BasePage.P_TRANSACTION_ID));
                }

                // release this page to be re-used
                page.Release();

                count++;
            }

            LOG($"flushing {count} pages on log disk ({transactions.Count} transactions)", "DISK");

            // after this I will have 100% sure data are safe
            _stream.FlushToDisk();

            // notifiy all confirmed transactions that was write in disk
            foreach(var transactionID in transactions)
            {
                _flushed?.Invoke(transactionID);
            }

            // when done, if has more pages in queue, run again
            if (_queue.Count > 0) this.ExecuteQueue();
        }

        public void Dispose()
        {
            LOG($"disposing disk writer queue (in queue {_queue.Count} pages)", "DISK");

            // stop running async task in next while check
            _running = false;

            _waiter.Set();

            // wait async task finish before dispose
            _thread.Join();

            _waiter.Dispose();
            _writing.Dispose();

            // if still have any page in queue, run synchronized
            this.ExecuteQueue();

            ENSURE(_queue.Count == 0, "must have no pages in queue before Dispose");
        }
    }
}