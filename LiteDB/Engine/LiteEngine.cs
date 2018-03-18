using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only (no linq, no poco... just Bson objects)
    /// </summary>
    public partial class LiteEngine : IDisposable
    {
        /// <summary>
        /// Max transactions must be keeped in queue
        /// </summary>
        private const int MAX_TRANSACTION_BUFFER = 100;

        #region Services instances

        private Logger _log;

        private LockService _locker;

        private FileService _datafile;

        private WalService _wal;

        private HeaderPage _header;

        private BsonReader _bsonReader;

        private BsonWriter _bsonWriter;

        private bool _utcDate;

        #region TempDB

        private LiteEngine _tempdb = null;
        private bool _disposeTempdb = false;

        /// <summary>
        /// Get/Set temporary engine database used to sort data
        /// </summary>
        public LiteEngine TempDB
        {
            get
            {
                if (_tempdb == null)
                {
                    _tempdb = new LiteEngine(new ConnectionString { Filename = ":temp:" });
                }

                return _tempdb;
            }
            set
            {
                if (_tempdb != null) throw LiteException.TempEngineAlreadyDefined();

                _tempdb = value;
                _disposeTempdb = false;
            }
        }

        #endregion

        /// <summary>
        /// Get log instance for debug operations
        /// </summary>
        public Logger Log => _log;

        /// <summary>
        /// Get if BSON must read date as UTC (if false, date will be used LOCAL date)
        /// </summary>
        public bool UtcDate => _utcDate;

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

                // get utc date handler
                _utcDate = options.UtcDate;

                _bsonReader = new BsonReader(options.UtcDate);
                _bsonWriter = new BsonWriter();

                _locker = new LockService(options.Timeout, _log);

                // open datafile (crete new if stream are empty)
                var factory = options.GetDiskFactory();

                _datafile = new FileService(factory, options.Password, options.Timeout, options.InitialSize, options.LimitSize, _utcDate, _log);

                // initialize wal file
                _wal = new WalService(_locker, _datafile, _log);

                // if WAL file have content, must run a checkpoint
                if(_wal.Checkpoint())
                {
                    // sync checkpoint write operations
                    this.WaitAsyncWrite();
                }

                // load header page
                _header = _datafile.ReadPage(0, true) as HeaderPage;

                // register virtual collections
                this.InitializeVirtualCollections();
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
        /// Request a wal checkpoint
        /// </summary>
        public void Checkpoint() => _wal.Checkpoint();

        /// <summary>
        /// Execute all async queue writes on disk and flush - this method are called just before dispose datafile
        /// </summary>
        public void WaitAsyncWrite() => _datafile.WaitAsyncWrite();

        public void Dispose()
        {
            // close all Dispose services
            if (_datafile != null)
            {
                _datafile.Dispose();
            }

            if (_disposeTempdb)
            {
                _tempdb.Dispose();
            }
        }
    }
}