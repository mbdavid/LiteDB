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

        private DiskService _disk;

        private CacheService _cache;

        private TransactionService _trans;

        private AesEncryption _crypto;

        private BsonReader _bsonReader;
        private BsonWriter _bsonWriter = new BsonWriter();

        private ConnectionString _options;

        /// <summary>
        /// Get log instance for debug operations
        /// </summary>
        public Logger Log { get { return _log; } }

        #endregion

        #region Ctor

        /// <summary>
        /// Initialize LiteEngine using memory database
        /// </summary>
        public LiteEngine()
            : this(new ConnectionString())
        {
        }

        /// <summary>
        /// Initialize LiteEngine using default file implementation
        /// </summary>
        public LiteEngine(string connectionString)
            : this(new ConnectionString(connectionString))
        {
        }

        /// <summary>
        /// Initialize LiteEngine using full connection string options, stream factory (if null use connection string GetDiskFactory()) and logger instance (if null create new)
        /// </summary>
        public LiteEngine(ConnectionString connectionString, IDiskFactory factory = null, Logger log = null)
        {
            _options = connectionString;
            _log = log ?? new Logger(_options.Log);

            try
            {
                this.InitializeServices(factory ?? _options.GetDiskFactory());

                // // initialize AES encryptor
                // if (_options.Password != null)
                // {
                //     _crypto = new AesEncryption(_options.Password, header.Salt);
                // }

                // initialize all services
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
        private void InitializeServices(IDiskFactory factory)
        {
            _bsonReader = new BsonReader(_options.UtcDate);
            _disk = new DiskService(factory, _options.Timeout);
            _locker = new LockService(_cache, _options.Timeout, _log);
            _trans = new TransactionService(_disk, _crypto, _pager, _locker, _cache, _log);
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

        /// <summary>
        /// Create an empty database
        /// </summary>
        private void CreateEmptyDatabase(Stream stream, string password, long initialSize)
        {
            // create a new header page in bytes (keep second page empty)
            var header = new HeaderPage
            {
                LastPageID = 1,
                Salt = AesEncryption.Salt()
            };

            stream.WritePage(0, header.WritePage());

            // if has initial size (at least 10 pages), alocate disk space now
            if (initialSize > (BasePage.PAGE_SIZE * 10))
            {
                stream.SetLength(initialSize);
            }
        }

        public void Dispose()
        {
            // dispose crypto
            if (_crypto != null) _crypto.Dispose();
        }
    }
}