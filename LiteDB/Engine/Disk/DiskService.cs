using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement custom fast/in memory mapped disk access
    /// [ThreadSafe]
    /// </summary>
    internal class DiskService : IDisposable
    {
        private readonly MemoryCache _cache;
        private readonly Lazy<DiskWriterQueue> _queue;

        private readonly IStreamFactory _logFactory;
        private readonly IStreamFactory _dataFactory;

        private readonly StreamPool _dataPool;
        private readonly StreamPool _logPool;

        private long _dataLength;
        private long _logLength;

        public DiskService(EngineSettings settings)
        {
            _cache = new MemoryCache(settings.MemorySegmentSize);

            // get new stream factory based on settings
            _dataFactory = settings.CreateDataFactory();
            _logFactory = settings.CreateLogFactory();

            // create stream pool
            _dataPool = new StreamPool(_dataFactory, false);
            _logPool = new StreamPool(_logFactory, true);

            var isNew = _dataFactory.Exists() == false;

            // create lazy async writer queue for log file
            _queue = new Lazy<DiskWriterQueue>(() => new DiskWriterQueue(_logPool.Writer));

            // create new database if not exist yet
            if (isNew)
            {
                LOG($"creating new database: '{Path.GetFileName(_dataFactory.Name)}'", "DISK");

                this.Initialize(_dataPool.Writer, settings.InitialSize);
            }

            // get initial data file length
            _dataLength = _dataFactory.GetLength() - PAGE_SIZE;

            // get initial log file length
            if (_logFactory.Exists())
            {
                _logLength = _logFactory.GetLength() - PAGE_SIZE;
            }
            else
            {
                _logLength = -PAGE_SIZE;
            }
        }

        /// <summary>
        /// Get async queue writer
        /// </summary>
        public DiskWriterQueue Queue => _queue.Value;

        /// <summary>
        /// Get memory cache instance
        /// </summary>
        public MemoryCache Cache => _cache;

        /// <summary>
        /// Get Stream pool used inside disk service
        /// </summary>
        public StreamPool GetPool(FileOrigin origin) => origin == FileOrigin.Data ? _dataPool : _logPool;

        /// <summary>
        /// Create a new empty database (use synced mode)
        /// </summary>
        private void Initialize(Stream stream, long initialSize)
        {
            var buffer = new PageBuffer(new byte[PAGE_SIZE], 0, 0);
            var header = new HeaderPage(buffer, 0);

            // update buffer
            header.UpdateBuffer();

            stream.Write(buffer.Array, 0, PAGE_SIZE);

            if (initialSize > 0)
            {
                stream.SetLength(initialSize);
            }

            stream.FlushToDisk();
        }

        /// <summary>
        /// Get a new instance for read data/log pages. This instance are not thread-safe - must request 1 per thread (used in Transaction)
        /// </summary>
        public DiskReader GetReader()
        {
            return new DiskReader(_cache, _dataPool, _logPool);
        }

        /// <summary>
        /// When a page are requested as Writable but not saved in disk, must be discard before release
        /// </summary>
        public void DiscardPages(IEnumerable<PageBuffer> pages, bool isDirty)
        {
            if (isDirty == false)
            {
                foreach (var page in pages)
                {
                    // if page was not modified, try move to readable list
                    if (_cache.TryMoveToReadable(page) == false)
                    {
                        // if already in readable list, just discard
                        _cache.DiscardPage(page);
                    }
                }
            }
            else
            {
                // only for ROLLBACK action
                foreach (var page in pages)
                {
                    // complete discard page and content
                    _cache.DiscardPage(page);
                }
            }
        }

        /// <summary>
        /// Request for a empty, writable non-linked page.
        /// </summary>
        public PageBuffer NewPage()
        {
            return _cache.NewPage();
        }

        /// <summary>
        /// Write pages inside file origin using async queue - WORKS ONLY FOR LOG FILE - returns how many pages are inside "pages"
        /// </summary>
        public int WriteAsync(IEnumerable<PageBuffer> pages)
        {
            var count = 0;

            foreach (var page in pages)
            {
                ENSURE(page.ShareCounter == BUFFER_WRITABLE, "to enqueue page, page must be writable");

                // adding this page into file AS new page (at end of file)
                // must add into cache to be sure that new readers can see this page
                page.Position = Interlocked.Add(ref _logLength, PAGE_SIZE);

                // should mark page origin to log because async queue works only for log file
                // if this page came from data file, must be changed before MoveToReadable
                page.Origin = FileOrigin.Log;

                // mark this page as readable and get cached paged to enqueue
                var readable = _cache.MoveToReadable(page);

                _queue.Value.EnqueuePage(readable);

                count++;
            }

            _queue.Value.Run();

            return count;
        }

        /// <summary>
        /// Get virtual file length: real file can be small but async thread can still writing on disk
        /// </summary>
        public long GetLength(FileOrigin origin)
        {
            if (origin == FileOrigin.Log)
            {
                return _logLength + PAGE_SIZE;
            }
            else
            {
                return _dataLength + PAGE_SIZE;
            }
        }

        #region Sync Read/Write operations

        /// <summary>
        /// Read all database pages inside file with no cache using. PageBuffers dont need to be Released
        /// </summary>
        public IEnumerable<PageBuffer> ReadFull(FileOrigin origin)
        {
            // do not use MemoryCache factory - reuse same buffer array (one page per time)
            // do not use BufferPool because header page can't be shared (byte[] is used inside page return)
            var buffer = new byte[PAGE_SIZE];

            var pool = origin == FileOrigin.Log ? _logPool : _dataPool;
            var stream = pool.Rent();

            try
            {
                // get length before starts (avoid grow during loop)
                var length = this.GetLength(origin);

                stream.Position = 0;

                while (stream.Position < length)
                {
                    var position = stream.Position;

                    stream.Read(buffer, 0, PAGE_SIZE);

                    yield return new PageBuffer(buffer, 0, 0)
                    {
                        Position = position,
                        Origin = origin,
                        ShareCounter = 0
                    };
                }
            }
            finally
            {
                pool.Return(stream);
            }
        }

        /// <summary>
        /// Write pages DIRECT in disk with NO queue. This pages are not cached and are not shared - WORKS FOR DATA FILE ONLY
        /// </summary>
        public void Write(IEnumerable<PageBuffer> pages, FileOrigin origin)
        {
            ENSURE(origin == FileOrigin.Data);

            var stream = origin == FileOrigin.Data ? _dataPool.Writer : _logPool.Writer;

            foreach (var page in pages)
            {
                ENSURE(page.ShareCounter == 0, "this page can't be shared to use sync operation - do not use cached pages");

                _dataLength = Math.Max(_dataLength, page.Position);

                stream.Position = page.Position;

                stream.Write(page.Array, page.Offset, PAGE_SIZE);
            }

            stream.FlushToDisk();
        }

        /// <summary>
        /// Set new length for file in sync mode. Queue must be empty before set length
        /// </summary>
        public void SetLength(long length, FileOrigin origin)
        {
            var stream = origin == FileOrigin.Log ? _logPool.Writer : _dataPool.Writer;

            if (origin == FileOrigin.Log)
            {
                ENSURE(_queue.Value.Length == 0, "queue must be empty before set new length");

                Interlocked.Exchange(ref _logLength, length - PAGE_SIZE);
            }
            else
            {
                Interlocked.Exchange(ref _dataLength, length - PAGE_SIZE);
            }

            stream.SetLength(length);
        }

        /// <summary>
        /// Get file name (or Stream name)
        /// </summary>
        public string GetName(FileOrigin origin)
        {
            return origin == FileOrigin.Data ? _dataFactory.Name : _logFactory.Name;
        }

        #endregion

        public void Dispose()
        {
            // dispose queue (wait finish)
            if (_queue.IsValueCreated) _queue.Value.Dispose();

            // get stream length from writer - is safe because only this instance
            // can change file size
            var delete = _logFactory.Exists() && _logPool.Writer.Length == 0;

            // dispose Stream pools
            _dataPool.Dispose();
            _logPool.Dispose();

            ENSURE(_dataFactory.IsLocked() == false, "datafile must be released");
            ENSURE(_dataFactory.IsLocked() == false, "logfile must be released");

            if (delete) _logFactory.Delete();

            // other disposes
            _cache.Dispose();
        }
    }
}
