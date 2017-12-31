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

                _datafile = new FileService(options.GetDiskFactory(false), options.Timeout, options.LimitSize, _log);

                // create database if not exists
                if (_datafile.IsEmpty())
                {
                    _datafile.CreateDatabase(options.InitialSize);
                }

                // if contains password, enable encryption
                if (options.Password != null)
                {
                    _datafile.EnableEncryption(options.Password);
                }

                // create instance of WAL file (with no encryption)
                _walfile = new FileService(options.GetDiskFactory(true), options.Timeout, long.MaxValue, _log);

                // initialize wal file
                _wal = new WalService(_locker, _datafile, _walfile);

                // if WAL file have content, must run a checkpoint
                if (_walfile.IsEmpty() == false)
                {
                    _wal.Checkpoint();
                }
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
        /// Create new transaction
        /// </summary>
        private TransactionService BeginTrans()
        {
            return new TransactionService(_locker, _wal, _datafile, _walfile, _log);
        }

        public void Dispose()
        {
            // do checkpoint before exit
            if (_walfile != null && _walfile.IsEmpty() == false)
            {
                _wal.Checkpoint();
            }

            // close all Dispose services
            if (_datafile != null) _datafile.Dispose();
            if (_walfile != null) _walfile.Dispose(true);
        }
    }
}