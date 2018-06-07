using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace LiteDB.Engine
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

        private DataFileService _dataFile;

        private WalService _wal;

        private HeaderPage _header;

        private BsonReader _bsonReader;

        private BsonWriter _bsonWriter;

        private bool _utcDate;

        private bool _disposing = false;

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
            try
            {
                // create factory based on connection string if there is no factory
                _log = settings.Log ?? new Logger(settings.LogLevel);

                // get utc date handler
                _utcDate = settings.UtcDate;

                _bsonReader = new BsonReader(settings.UtcDate);
                _bsonWriter = new BsonWriter();

                _locker = new LockService(settings.Timeout, _log);

                // get disk factory from engine settings and open/create datafile/walfile
                var factory = settings.GetDiskFactory();

                _dataFile = new DataFileService(factory, settings.Timeout, settings.InitialSize, _utcDate, _log);

                // initialize wal service
                _wal = new WalService(_locker, _dataFile, factory, settings.Timeout, settings.LimitSize, _utcDate, _log);

                // if WAL file have content, must run a checkpoint
                _wal.Checkpoint(false);

                // load header page
                _header = _dataFile.ReadPageDisk(0) as HeaderPage;

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
        public void Checkpoint(bool delete) => _wal?.Checkpoint(delete);

        /// <summary>
        /// Execute all async queue writes on disk and flush - this method are called just before dispose datafile
        /// </summary>
        public void WaitAsyncWrite() => _wal?.WalFile?.WaitAsyncWrite(true);

        public void Dispose()
        {
            if (_disposing) return;

            // start shutdown operation
            _disposing = true;

            // mark all transaction as shotdown status
            foreach(var trans in _transactions.Values)
            {
                trans.Shutdown();
            }

            // wait for all async task write on disk
            _wal.WalFile.WaitAsyncWrite(true);

            // do checkpoint and delete wal file
            _wal.Checkpoint(true);

            // close all Dispose services
            _dataFile.Dispose();

            // dispose wal file
            _wal.WalFile.Dispose();

            if (_disposeTempdb)
            {
                _tempdb.Dispose();
            }
        }
    }
}