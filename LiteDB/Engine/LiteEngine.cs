using System;
using System.Collections.Concurrent;
using System.IO;

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

        private BsonWriter _bsonWriter = new BsonWriter();

        private ConcurrentQueue<LiteTransaction> _transactions = new ConcurrentQueue<LiteTransaction>();

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

                // open datafile (crete new if stream are empty)
                var factory = options.GetDiskFactory();

                _datafile = new FileService(factory, options.Password, options.Timeout, options.InitialSize, options.LimitSize, _log);

                // initialize wal file
                _wal = new WalService(_locker, _datafile, _log);

                // if WAL file have content, must run a checkpoint
                _wal.Checkpoint();

                // load header page
                _header = _datafile.ReadPage(0) as HeaderPage;
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
        /// Initialize a new transaction
        /// </summary>
        public LiteTransaction BeginTrans()
        {
            var transaction = new LiteTransaction(_header, _locker, _wal, _datafile, _log);

            // add transaction to queue list to be avaiable for query
            _transactions.Enqueue(transaction);

            if (_transactions.Count > MAX_TRANSACTION_BUFFER)
            {
                _transactions.TryDequeue(out var dummy);
            }

            return transaction;
        }

        /// <summary>
        /// Request a wal checkpoint
        /// </summary>
        public void Checkpoint() => _wal.Checkpoint();

        public void Dispose()
        {
            // close all Dispose services
            if (_datafile != null)
            {
                _datafile.Dispose();
            }
        }
    }
}