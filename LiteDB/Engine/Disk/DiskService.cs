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
    internal class DiskService
    {
        private readonly MemoryCache _cache;
        private readonly Lazy<DiskWriterQueue> _queue;

        private readonly IStreamFactory _logFactory;
        private readonly IStreamFactory _dataFactory;

        private readonly StreamPool _dataPool;
        private readonly StreamPool _logPool;

        private long _dataLength;
        private long _logLength;

        private readonly AesEncryption _aes;

        public event Action<uint> Flush;

        public DiskService(EngineSettings settings)
        {
            _cache = new MemoryCache(MEMORY_SEGMENT_SIZE);

            // get new stream factory based on settings
            _dataFactory = settings.CreateDataFactory();
            _logFactory = settings.CreateLogFactory();

            // create stream pool
            _dataPool = new StreamPool(_dataFactory, false);
            _logPool = new StreamPool(_logFactory, true);

            var isNew = _dataFactory.Exists() == false;

            // load AES encryption class instance
            _aes = settings.Password != null ? 
                new AesEncryption(settings.Password, isNew ? AesEncryption.NewSalt() : this.ReadSalt(_dataPool)) : 
                null;

            // create lazy async writer queue for log file
            _queue = new Lazy<DiskWriterQueue>(() => new DiskWriterQueue(_logPool.Writer, _aes, this.Flush));

            // create new database if not exist yet
            if (isNew)
            {
                LOG($"creating new database: '{Path.GetFileName(_dataFactory.Name)}'", "DISK");

                this.Initialize(_dataPool.Writer, _aes, settings.InitialSize);
            }

            // get initial data file length
            var dataStream = _dataPool.Rent();
            _dataLength = dataStream.Length - PAGE_SIZE;
            _dataPool.Return(dataStream);

            // get initial log file length
            if (_logFactory.Exists())
            {
                var logStream = _logPool.Rent();
                _logLength = logStream.Length - PAGE_SIZE;
                _logPool.Return(logStream);
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
        /// Create a new empty database (use synced mode)
        /// </summary>
        private void Initialize(Stream stream, AesEncryption aes, long initialSize)
        {
            var buffer = new PageBuffer(new byte[PAGE_SIZE], 0, 0);
            var header = new HeaderPage(buffer, 0);

            // encryption will use 2 pages (#0 for header, #1 for SALT)
            header.LastPageID = aes == null ? 0u : 1u;

            // update buffer
            header.GetBuffer(true);

            // compute CRC
            buffer[BasePage.P_CRC] = buffer.ComputeChecksum();

            if (aes == null)
            {
                stream.Write(buffer.Array, 0, PAGE_SIZE);
            }
            else
            {
                aes.Encrypt(buffer, stream);

                stream.Write(aes.Salt, 0, ENCRYPTION_SALT_SIZE);
            }

            if (initialSize > 0)
            {
                stream.SetLength(initialSize);
            }
        }

        /// <summary>
        /// Read SALT bytes in Page #1 (in data page)
        /// </summary>
        private byte[] ReadSalt(StreamPool pool)
        {
            var stream = pool.Rent();

            try
            {
                var salt = new byte[ENCRYPTION_SALT_SIZE];

                stream.Position = P_ENCRYPTION_SALT;
                stream.Read(salt, 0, ENCRYPTION_SALT_SIZE);

                return salt;
            }
            finally
            {
                pool.Return(stream);
            }
        }

        /// <summary>
        /// Get a new instance for read data/log pages. This instance are not thread-safe - must request 1 per thread (used in Transaction)
        /// </summary>
        public DiskReader GetReader()
        {
            return new DiskReader(_cache, _dataPool, _logPool, _aes);
        }

        /// <summary>
        /// When a page are requested as Writable but not saved in disk, must be discard before release
        /// </summary>
        public void DiscardPages(IEnumerable<PageBuffer> pages, bool isDirty)
        {
            if (isDirty == false)
            {
                // try move clean pages into _readable list (if not already there)
                foreach (var page in pages)
                {
                    _cache.TryMoveToReadable(page);
                }
            }
            else
            {
                // only rollback action - this must will be keeped into _writable and wait for new Extend
                foreach (var page in pages)
                {
                    page.ShareCounter = 1; // will be decrease on page.Release();
                    page.Timestamp = 1; // be first page to be re-used on next Extend()
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
        /// Write pages inside file origin using async queue - works only for log file
        /// </summary>
        public void WriteAsync(IEnumerable<PageBuffer> pages)
        {
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
            }

            _queue.Value.Run();
        }

        /// <summary>
        /// Get file length
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
            var buffer = new byte[PAGE_SIZE];

            var pool = origin == FileOrigin.Log ? _logPool : _dataPool;
            var stream = pool.Rent();

            try
            {
                // get length before starts (avoid grow during loop)
                var length = stream.Length;

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
        /// Write pages inside disk with no async queue. This pages are not cached and are not shared
        /// </summary>
        public void Write(IEnumerable<PageBuffer> pages, FileOrigin origin)
        {
            ENSURE(origin == FileOrigin.Data);

            var stream = origin == FileOrigin.Data ? _dataPool.Writer : _logPool.Writer;

            foreach (var page in pages)
            {
                ENSURE(page.ShareCounter == 0, "this page can't be shared to use sync operation");

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
                Interlocked.Exchange(ref _logLength, length - PAGE_SIZE);
            }
            else
            {
                Interlocked.Exchange(ref _dataLength, length - PAGE_SIZE);
            }

            ENSURE(_queue.Value.Length == 0, "queue must be empty before set new length");

            stream.SetLength(length);
        }

        /// <summary>
        /// Delete file - checks if empty (0 bytes) before delete - dispose all Stream (this Disk instance can't be used after this)
        /// </summary>
        public bool Delete(FileOrigin origin)
        {
            var factory = origin == FileOrigin.Log ? _logFactory : _dataFactory;

            if (factory.Exists() == false) return false;

            ENSURE(this.GetLength(origin) == 0, "file should be 0 length before delete");

            this.Dispose();

            LOG($"deleting `{Path.GetFileName(factory.Name)}` file", "DISK");

            factory.Delete();

            return true;
        }

        /// <summary>
        /// Get file name (or Stream name)
        /// </summary>
        public string GetName(FileOrigin origin)
        {
            return origin == FileOrigin.Data ? _dataFactory.Name : _logFactory.Name;
        }

        #endregion

        /// <summary>
        /// Return how many pages are in use when call this method (ShareCounter != 0).
        /// Used only for DEBUG propose
        /// </summary>
        public int PagesInUse => _cache.PagesInUse;

        public void Dispose()
        {
            // dispose queue (wait finish)
            if (_queue.IsValueCreated) _queue.Value.Dispose();

            // dispose Stream pools
            _dataPool.Dispose();
            _logPool.Dispose();

            // other disposes
            _cache.Dispose();
            _aes?.Dispose();
        }
    }
}
