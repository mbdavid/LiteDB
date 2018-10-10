using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB.Engine
{
    /// <summary>
    /// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
    /// </summary>
    public partial class LiteEngine : ILiteEngine
    {
        #region Services instances

        private readonly Logger _log;

        private readonly LockService _locker;

        private readonly DataFileService _dataFile;

        private readonly WalService _wal;

        private HeaderPage _header;

        private readonly BsonReader _bsonReader;

        private readonly BsonWriter _bsonWriter;

        // immutable settings
        private readonly IDiskFactory _factory;
        private readonly bool _utcDate;
        private readonly bool _checkpointOnShutdown;

        private bool _shutdown = false;
        private bool _disposed = false;

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
                    _tempdb = new LiteEngine(new EngineSettings { Filename = ":temp:" });
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
        /// Get if date must be read from Bson as UTC date 
        /// </summary>
        internal bool UtcDate => _utcDate;

        #endregion

        #region Ctor

        /// <summary>
        /// Initialize LiteEngine using connection memory database
        /// </summary>
        public LiteEngine()
            : this(new EngineSettings())
        {
        }

        /// <summary>
        /// Initialize LiteEngine using connection string using key=value; parser
        /// </summary>
        public LiteEngine(string filename)
            : this (new EngineSettings { Filename = filename })
        {
        }

        /// <summary>
        /// Initialize LiteEngine using initial engine settings
        /// </summary>
        public LiteEngine(EngineSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            try
            {
                // create factory based on connection string if there is no factory
                _log = settings.Log ?? new Logger(settings.LogLevel);

                // copy settings into class variables (turn values in immutable values)
                _factory = settings.GetDiskFactory();
                _utcDate = settings.UtcDate;
                _checkpointOnShutdown = settings.CheckpointOnShutdown && settings.ReadOnly == false;

                _log.Info($"initializing database '{_factory.Filename}'");

                _bsonReader = new BsonReader(settings.UtcDate);
                _bsonWriter = new BsonWriter();

                _locker = new LockService(settings.Timeout, settings.ReadOnly, _log);

                // get disk factory from engine settings and open/create datafile/walfile
                var factory = settings.GetDiskFactory();

                _dataFile = new DataFileService(factory, settings.InitialSize, settings.UtcDate, _log);

                // load header page (single instance)
                _header = _dataFile.ReadPage(0) as HeaderPage;

                // initialize wal service
                _wal = new WalService(_locker, _dataFile, factory, settings.LimitSize, settings.UtcDate, _log);

                // if exists WAL file, restore wal index references (can update full _header instance)
                _wal.RestoreWalIndex(ref _header);

                // register system collections
                this.InitializeSystemCollections();

                // if fileVersion are less than current version, must upgrade datafile
                if (_header.FileVersion < HeaderPage.FILE_VERSION)
                {
                    this.Upgrade();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                // explicit dispose (but do not run shutdown operation)
                this.Dispose(true);
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Request a WAL checkpoint
        /// </summary>
        public int Checkpoint() => _wal.Checkpoint(_header, true);

        /// <summary>
        /// Shutdown database and do not accept any other access. Wait finish all transactions
        /// </summary>
        private void Shutdown()
        {
            // here all private instances are loaded
            if (_shutdown) return;

            // start shutdown operation
            _shutdown = true;

            _log.Info("shutting down the database");

            // mark all transaction as shotdown status
            foreach (var trans in _transactions.Values)
            {
                trans.Shutdown();
            }

            if (_checkpointOnShutdown)
            {
                // do checkpoint (with no-lock check)
                _wal.Checkpoint(null, false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            // this method can be called from Ctor, so many 
            // of this members can be null yet (even if are readonly). 

            if (_disposed) return;

            if (disposing)
            {
                // dispose lockers
                _locker?.Dispose();

                // close all Dispose services
                _dataFile?.Dispose();
                _wal?.Dispose();

                if (_disposeTempdb)
                {
                    _tempdb?.Dispose(disposing);
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Shutdown database
        /// - After dispose engine, no more new transaction
        /// - All transation will throw shutdown exception and do rollback
        /// - Wait for async write with full flush to disk
        /// - Do checkpoint (sync)
        /// - Dispose disks
        /// </summary>
        public void Dispose()
        {
            // shutdown all operations
            this.Shutdown();

            // dispose data file
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        ~LiteEngine()
        {
            this.Dispose(false);
        }
    }
}