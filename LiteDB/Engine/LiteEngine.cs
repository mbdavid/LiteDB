using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
    /// </summary>
    public partial class LiteEngine : IDisposable//: ILiteEngine
    {
        #region Services instances

        private readonly Logger _log;

        private readonly LockService _locker;

        private readonly MemoryFile _dataFile;

        private readonly MemoryFile _logFile;

        private readonly WalIndexService _walIndex;

        private HeaderPage _header;

        // immutable settings
        private readonly EngineSettings _settings;

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
        /// Get database initialize settings
        /// </summary>
        internal EngineSettings Settings => _settings;

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

                _settings = settings;

                // create StreamFactory and StreamPool for data file
                var dataFactory = settings.GetStreamFactory(DbFileMode.Datafile);
                var dataPool = new StreamPool(dataFactory);

                _log.Info($"initializing database '{dataFactory.Filename}'");

                AesEncryption aes = null;

                // if has password, create AES encryption
                if (settings.Password != null)
                {
                    aes = AesEncryption.CreateAes(dataPool, _settings.Password);
                }

                // create StreamFatory and StreamPool for log file
                var logFactory = settings.GetStreamFactory(DbFileMode.Logfile);
                var logPool = new StreamPool(logFactory);

                // initialize memory files
                _dataFile = new MemoryFile(dataPool, aes);
                _logFile = new MemoryFile(logPool, aes);

                // initialize another services
                _locker = new LockService(settings.Timeout, settings.ReadOnly, _log);

                // load header or create new database
                _header = this.OpenOrCreateDatabase(dataPool);

                // initialize wal-index service
                _walIndex = new WalIndexService(_locker, _log);

                // if exists log file, restore wal index references (can update full _header instance)
                if (_logFile.Length > 0)
                {
                    _walIndex.RestoreIndex(logPool, ref _header);
                }


                // register system collections
                //** this.InitializeSystemCollections();

                // if fileVersion are less than current version, must upgrade datafile
                //** if (_header.FileVersion < HeaderPage.FILE_VERSION)
                //** {
                //**     this.Upgrade();
                //** }
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
        /// Open/Create new datafile and read first header page (read direct from Stream, do not use MemoryFile)
        /// </summary>
        private HeaderPage OpenOrCreateDatabase(StreamPool pool)
        {
            // header buffer contains a own buffer instance (not shared)
            var buffer = new PageBuffer(new byte[PAGE_SIZE], 0) { ShareCounter = -2 /* static buffer */ };

            if (pool.Factory.Exists())
            {
                // load header direct from stream
                var stream = pool.Rent();

                try
                {
                    stream.Position = 0;

                    stream.Read(buffer.Array, 0, PAGE_SIZE);

                    var header = new HeaderPage(buffer);

                    return header;
                }
                finally
                {
                    pool.Return(stream);
                }
            }
            else
            {
                // create new database (empty header page)
                var header = new HeaderPage(buffer, 0);

                header.UpdateBuffer();

                pool.Writer.Write(buffer.Array, 0, PAGE_SIZE);

                if (_settings.InitialSize > 0)
                {
                    pool.Writer.SetLength(_settings.InitialSize);
                }

                pool.Writer.FlushToDisk();

                return header;
            }
        }

        /// <summary>
        /// Request a database checkpoint
        /// </summary>
//**        public int Checkpoint() => _walIndex.Checkpoint(_header, true);

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

//**            // mark all transaction as shotdown status
//**            foreach (var trans in _transactions.Values)
//**            {
//**                trans.Shutdown();
//**            }
//**
//**            if (_settings.CheckpointOnShutdown && _settings.ReadOnly == false)
//**            {
//**                // do checkpoint (with no-lock check)
//**                _walIndex.Checkpoint(null, false);
//**            }
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
                _logFile?.Dispose();

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