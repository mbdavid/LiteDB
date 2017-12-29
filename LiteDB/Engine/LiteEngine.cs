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

        private FileService _datafile;

        private FileService _walfile;

        private WalService _wal;

        private BsonReader _bsonReader;
        private BsonWriter _bsonWriter = new BsonWriter();

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
        public LiteEngine(ConnectionString options)
        {
            try
            {
                // create factory based on connection string if there is no factory
                _log = options.Log;

                _bsonReader = new BsonReader(options.UtcDate);

                _locker = new LockService(options.Timeout, _log);

                _datafile = new FileService(options.GetDataFactory(), options.Timeout, options.LimitSize);

                // create database if not exists
                if (_datafile.IsEmpty())
                {
                    _datafile.CreateDatabase(options.InitialSize);
                }

                if (options.Password != null)
                {
                    _datafile.EnableEncryption(options.Password);
                }

                // create instance of WAL file (with no encryption)
                _walfile = new FileService(options.GetWalFactory(), options.Timeout, long.MaxValue);

                // inicialize wal file
                _wal = new WalService();

            }
            catch (Exception)
            {
                // explicit dispose
                this.Dispose();
                throw;
            }
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
            // close all Dispose services
            if (_crypto != null) _crypto.Dispose();
            if (_datafile != null) _datafile.Dispose();
            if (_walfile != null) _walfile.Dispose();
        }
    }
}