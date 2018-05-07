using System;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only (no linq, no poco... just Bson objects)
    /// </summary>
    public partial class LiteEngine : IDisposable
    {
        #region Services instances

        private Logger _log;

        private LockService _locker;

        private IDiskService _disk;

        private CacheService _cache;

        private PageService _pager;

        private TransactionService _trans;

        private IndexService _indexer;

        private DataService _data;

        private CollectionService _collections;

        private AesEncryption _crypto;

        private int _cacheSize;

        private TimeSpan _timeout;

        private BsonReader _bsonReader;
        private BsonWriter _bsonWriter = new BsonWriter();

        /// <summary>
        /// Get log instance for debug operations
        /// </summary>
        public Logger Log { get { return _log; } }

        /// <summary>
        /// Get memory cache size limit. Works only with journal enabled (number in pages). If journal is disabled, pages in cache can exceed this limit. Default is 5000 pages
        /// </summary>
        public int CacheSize { get { return _cacheSize; } }

        /// <summary>
        /// Get how many pages are on cache
        /// </summary>
        public int CacheUsed { get { return _cache.CleanUsed; } }

        /// <summary>
        /// Gets time waiting write lock operation before throw LiteException timeout
        /// </summary>
        public TimeSpan Timeout { get { return _timeout; } }

        /// <summary>
        /// Instance of locker control
        /// </summary>
        public LockService Locker { get { return _locker; } }

        #endregion

        #region Ctor

        /// <summary>
        /// Initialize LiteEngine using default FileDiskService
        /// </summary>
        public LiteEngine(string filename, bool journal = true)
            : this(new FileDiskService(filename, journal))
        {
        }

        /// <summary>
        /// Initialize LiteEngine with password encryption
        /// </summary>
        public LiteEngine(string filename, string password, bool journal = true)
            : this(new FileDiskService(filename, new FileOptions { Journal = journal }), password)
        {
        }

        /// <summary>
        /// Initialize LiteEngine using StreamDiskService
        /// </summary>
        public LiteEngine(Stream stream, string password = null)
            : this(new StreamDiskService(stream), password)
        {
        }

        /// <summary>
        /// Initialize LiteEngine using custom disk service implementation and full engine options
        /// </summary>
        public LiteEngine(IDiskService disk, string password = null, TimeSpan? timeout = null, int cacheSize = 5000, Logger log = null, bool utcDate = false)
        {
            if (disk == null) throw new ArgumentNullException(nameof(disk));

            _timeout = timeout ?? TimeSpan.FromMinutes(1);
            _cacheSize = cacheSize;
            _disk = disk;
            _log = log ?? new Logger();
            _bsonReader = new BsonReader(utcDate);

            try
            {
                // initialize datafile (create) and set log instance
                _disk.Initialize(_log, password);

                // lock disk (read mode) before read header
                var position = _disk.Lock(LockState.Read, _timeout);

                var buffer = _disk.ReadPage(0);

                _disk.Unlock(LockState.Read, position);

                // create header instance from array bytes
                var header = BasePage.ReadPage(buffer) as HeaderPage;

                // hash password with sha1 or keep as empty byte[20]
                var sha1 = password == null ? new byte[20] : AesEncryption.HashSHA1(password);

                // compare header password with user password even if not passed password (datafile can have password)
                if (sha1.BinaryCompareTo(header.Password) != 0)
                {
                    throw LiteException.DatabaseWrongPassword();
                }

                // initialize AES encryptor
                if (password != null)
                {
                    _crypto = new AesEncryption(password, header.Salt);
                }

                // initialize all services
                this.InitializeServices();

                // if header are marked with recovery, do it now
                if (header.Recovery)
                {
                    _trans.Recovery();
                }
            }
            catch (Exception)
            {
                // explicit dispose
                this.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Create instances for all engine services
        /// </summary>
        private void InitializeServices()
        {
            _cache = new CacheService(_disk, _log);
            _locker = new LockService(_disk, _cache, _timeout, _log);
            _pager = new PageService(_disk, _crypto, _cache, _log);
            _indexer = new IndexService(_pager, _log);
            _data = new DataService(_pager, _log);
            _trans = new TransactionService(_disk, _crypto, _pager, _locker, _cache, _cacheSize, _log);
            _collections = new CollectionService(_pager, _indexer, _data, _trans, _log);
        }

        #endregion

        /// <summary>
        /// Get the collection page only when needed. Gets from pager always to grantee that wil be the last (in case of clear cache will get a new one - pageID never changes)
        /// </summary>
        private CollectionPage GetCollectionPage(string name, bool addIfNotExits)
        {
            if (name == null) return null;

            // search my page on collection service
            var col = _collections.Get(name);

            if (col == null && addIfNotExits)
            {
                _log.Write(Logger.COMMAND, "create new collection '{0}'", name);

                col = _collections.Add(name);
            }

            return col;
        }

        /// <summary>
        /// Encapsulate all operations in a single write transaction
        /// </summary>
        private T Transaction<T>(string collection, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            // always starts write operation locking database
            using (_locker.Write())
            {
                try
                {
                    var col = this.GetCollectionPage(collection, addIfNotExists);

                    var result = action(col);

                    _trans.PersistDirtyPages();

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);

                    // if an error occurs during an operation, rollback must be called to avoid datafile inconsistent
                    _cache.DiscardDirtyPages();

                    throw;
                }
            }
        }

        public void Dispose()
        {
            // dispose datafile and journal file
            _disk.Dispose();

            // dispose crypto
            if (_crypto != null) _crypto.Dispose();
        }

        /// <summary>
        /// Initialize new datafile with header page + lock reserved area zone
        /// </summary>
        public static void CreateDatabase(Stream stream, string password = null, long initialSize = 0)
        {
            // calculate how many empty pages will be added on disk
            var emptyPages = initialSize == 0 ? 0 : (initialSize - (2 * BasePage.PAGE_SIZE)) / BasePage.PAGE_SIZE;

            // if too small size (less than 2 pages), assume no initial size
            if (emptyPages < 0) emptyPages = 0;

            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = initialSize == 0 ? 1 : (uint)emptyPages + 1,
                FreeEmptyPageID = initialSize == 0 ? uint.MaxValue : 2
            };

            if (password != null)
            {
                header.Password = AesEncryption.HashSHA1(password);
                header.Salt = AesEncryption.Salt();
            }

            // point to begin file
            stream.Seek(0, SeekOrigin.Begin);

            // get header page in bytes
            var buffer = header.WritePage();

            stream.Write(buffer, 0, BasePage.PAGE_SIZE);

            // write second page as an empty AREA (it's not a page) just to use as lock control
            stream.Write(new byte[BasePage.PAGE_SIZE], 0, BasePage.PAGE_SIZE);

            // create crypto class if has password
            var crypto = password != null ? new AesEncryption(password, header.Salt) : null;

            // if initial size is defined, lets create empty pages in a linked list
            if (emptyPages > 0)
            {
                stream.SetLength(initialSize);

                var pageID = 1u;

                while(++pageID < (emptyPages + 2))
                {
                    var empty = new EmptyPage(pageID)
                    {
                        PrevPageID = pageID == 2 ? 0 : pageID - 1,
                        NextPageID = pageID == emptyPages + 1 ? uint.MaxValue : pageID + 1
                    };

                    var bytes = empty.WritePage();

                    if (password != null)
                    {
                        bytes = crypto.Encrypt(bytes);
                    }

                    stream.Write(bytes, 0, BasePage.PAGE_SIZE);
                }
            }

            if (crypto != null) crypto.Dispose();
        }
    }
}