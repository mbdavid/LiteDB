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

        private FileService _dataFile;

        private FileService _walFile;

        private WalService _wal;

        private HeaderPage _header;

        private BsonReader _bsonReader;
        private BsonWriter _bsonWriter = new BsonWriter();

        /// <summary>
        /// Get log instance for debug operations
        /// </summary>
        public Logger Log { get { return _log; } }

        #endregion

        #region Ctor

        /// <summary>
        /// Initialize LiteEngine using connection memory database
        /// </summary>
        public LiteEngine()
            : this(new ConnectionString())
        {
        }

        /// <summary>
        /// Initialize LiteEngine using connection string using key=value; parser
        /// </summary>
        public LiteEngine(string connectionString)
            : this (new ConnectionString(connectionString))
        {
        }

        /// <summary>
        /// Initialize LiteEngine using connection string options
        /// </summary>
        public LiteEngine(ConnectionString options)
        {
            try
            {
                // create factory based on connection string if there is no factory
                _log = options.Log ?? new Logger(options.LogLevel);

                _bsonReader = new BsonReader(options.UtcDate);

                _locker = new LockService(options.Timeout, _log);

                _dataFile = new FileService(options.GetDiskFactory(false), options.Timeout, options.LimitSize, _log);

                // create database if not exists
                if (_dataFile.IsEmpty())
                {
                    _dataFile.CreateDatabase(options.InitialSize);
                }

                // if contains password, enable encryption
                if (options.Password != null)
                {
                    _dataFile.EnableEncryption(options.Password);
                }

                // create instance of WAL file (with no encryption)
                _walFile = new FileService(options.GetDiskFactory(true), options.Timeout, long.MaxValue, _log);

                // initialize wal file
                _wal = new WalService(_locker, _dataFile, _walFile);

                // if WAL file have content, must run a checkpoint
                if (_walFile.IsEmpty() == false)
                {
                    _wal.Checkpoint();
                }

                // load header page
                _header = (HeaderPage)_dataFile.ReadPage(0);
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
        /// Create new read transaction
        /// </summary>
        private TransactionService ReadTransaction(string collection)
        {
            return new TransactionService(TransactionMode.Read, collection, false, _header, _locker, _wal, _dataFile, _walFile, _log);
        }

        /// <summary>
        /// Create new write transaction in wrap try/catch error with commit/rollback calls. If collection is passed, write-lock collection
        /// </summary>
        private T WriteTransaction<T>(TransactionMode mode, string collection, bool addIfNotExists, Func<TransactionService, T> action)
        {
            var trans = new TransactionService(mode, collection, addIfNotExists, _header, _locker, _wal, _dataFile, _walFile, _log);

            try
            {
                var result = action(trans);

                // persist changes in wal file
                trans.Commit();

                return result;
            }
            //catch(Exception ex)
            //{
            //    _log.Write(Logger.ERROR, ex.Message);
            //    trans.Rollback();
            //    throw;
            //}
            finally
            {
                if (trans != null) trans.Dispose();
            }
        }

        public void Dispose()
        {
            // do checkpoint before exit
            if (_walFile != null && _walFile.IsEmpty() == false)
            {
                _wal.Checkpoint();
            }

            // close all Dispose services
            if (_dataFile != null) _dataFile.Dispose();
            if (_walFile != null) _walFile.Dispose(true);
        }
    }
}