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
    internal class DiskService
    {
        private readonly MemoryCache _cache;
        private readonly Lazy<DiskWriterQueue> _queue;

        private readonly StreamPool _dataPool;
        private readonly StreamPool _logPool;

        private readonly AesEncryption _aes;

        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        public DiskService(EngineSettings settings)
        {
            _cache = new MemoryCache(_locker);

            var dataFactory = settings.CreateDataFactory();
            var logFactory = settings.CreateLogFactory();

            // create stream pool
            _dataPool = new StreamPool(dataFactory, false);
            _logPool = new StreamPool(logFactory, true);

            var isNew = dataFactory.Exists() == false;

            // load AES encryption class instance
            _aes = settings.Password != null ? 
                new AesEncryption(settings.Password, isNew ? AesEncryption.NewSalt() : this.ReadSalt(_dataPool)) : 
                null;

            // create lazy async writer queue
            _queue = new Lazy<DiskWriterQueue>(() => new DiskWriterQueue(_dataPool.Writer, _logPool.Writer, _aes));

            // create new database if not exist yet
            if (isNew)
            {
                LOG($"creating new database: '{Path.GetFileName(dataFactory.Name)}'", "DISK");

                this.Initialize(_dataPool.Writer, _aes, settings.InitialSize);
            }

            this.HasLogFile = logFactory.Exists();
        }

        /// <summary>
        /// Create a new empty database (use synced mode)
        /// </summary>
        private void Initialize(Stream stream, AesEncryption aes, long initialSize)
        {
            var buffer = new PageBuffer(new byte[PAGE_SIZE], 0, 0);
            var header = new HeaderPage(buffer, 0);

            if (_aes == null)
            {
                stream.Write(header.GetBuffer(true).Array, 0, PAGE_SIZE);
            }
            else
            {
                // encryption will use 2 pages (#0 for header, #1 for SALT)
                header.LastPageID = 1;

                aes.Encrypt(header.GetBuffer(true), stream);

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
        /// Indicate if disk already contains log file. Loaded only in ctor (never update this)
        /// </summary>
        public bool HasLogFile { get; }

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
        /// Write pages inside file origin
        /// </summary>
        public void Write(IEnumerable<PageBuffer> pages, FileOrigin origin)
        {
            _locker.EnterReadLock();

            try
            {
                foreach (var page in pages)
                {
                    ENSURE(page.ShareCounter == BUFFER_WRITABLE, "to enqueue page, page must be writable");

                    if (origin == FileOrigin.Log)
                    {
                        // adding this page into file AS new page (at end of file)
                        // must add into cache to be sure that new readers can see this page
                        page.Position = _queue.Value.AppendPosition();
                    }

                    // must define page origin to be saved in correct place (before move to Readable)
                    // only writable pages can change Origin
                    page.Origin = origin;

                    // mark this page as readable and get cached paged to enqueue
                    var readable = _cache.MoveToReadable(page);

                    _queue.Value.EnqueuePage(readable);
                }

                _queue.Value.Run();
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Create a fake page to be added in queue. When queue run this page, just set new stream length
        /// </summary>
        public void SetLength(long length, FileOrigin origin)
        {
            _queue.Value.EnqueueLength(length, origin);
        }

        /// <summary>
        /// Read all database pages inside file with no cache using
        /// </summary>
        public IEnumerable<PageBuffer> ReadFull(FileOrigin origin)
        {
            throw new NotImplementedException();
        }

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
            _locker.Dispose();
        }
    }
}
