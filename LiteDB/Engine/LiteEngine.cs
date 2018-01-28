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

                // open datafile (crete new if stream are empty)
                _datafile = new FileService(options.GetDiskFactory(), options.Password, options.Timeout, options.InitialSize, options.LimitSize, _log);

                // initialize wal file
                _wal = new WalService(_locker, _datafile, _log);

                // if WAL file have content, must run a checkpoint
                _wal.Checkpoint();

                // load header page
                _header = (HeaderPage)_datafile.ReadPage(0);
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
            return new LiteTransaction(_header, _locker, _wal, _datafile, _walFile, _log);
        }

        public void Dispose()
        {
            // do checkpoint (sync) before exit
            if (_walFile != null && _walFile.IsEmpty() == false)
            {
                _wal.Checkpoint();
            }

            // close all Dispose services
            if (_datafile != null) _datafile.Dispose();
            if (_walFile != null) _walFile.Dispose(true);
        }
    }
}