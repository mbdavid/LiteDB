using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// A public class that take care of all engine data structure access - it´s basic implementation of a NoSql database
    /// Its isolated from complete solution - works on low level only (no linq, no poco... just BSON objects)
    /// [ThreadSafe]
    /// </summary>
    public partial class LiteEngine : ILiteEngine
    {
        #region Services instances

        private LockService _locker;

        private DiskService _disk;

        private WalIndexService _walIndex;

        private HeaderPage _header;

        private TransactionMonitor _monitor;

        private SortDisk _sortDisk;

        // immutable settings
        private readonly EngineSettings _settings;

        /// <summary>
        /// Indicate that database is open or not (auto-open in Ctor)
        /// </summary>
        private bool _isOpen = false;

        /// <summary>
        /// All system read-only collections for get metadata database information
        /// </summary>
        private Dictionary<string, SystemCollection> _systemCollections;

        /// <summary>
        /// Sequence cache for collections last ID (for int/long numbers only)
        /// </summary>
        private ConcurrentDictionary<string, long> _sequences;

        #endregion

        public bool IsOpen => _isOpen;

        #region Ctor

        /// <summary>
        /// Initialize LiteEngine using connection memory database
        /// </summary>
        public LiteEngine()
            : this(new EngineSettings { DataStream = new MemoryStream() })
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
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            this.Open();
        }

        #endregion

        #region Open & Close

        public bool Open()
        {
            // do not re-open database
            if (_isOpen) return false;

            LOG($"start initializing{(_settings.ReadOnly ? " (readonly)" : "")}", "ENGINE");

            _systemCollections = new Dictionary<string, SystemCollection>(StringComparer.OrdinalIgnoreCase);
            _sequences = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // initialize disk service (will create database if needed)
                _disk = new DiskService(_settings, MEMORY_SEGMENT_SIZES);

                // read page with no cache ref (has a own PageBuffer) - do not Release() support
                var buffer = _disk.ReadFull(FileOrigin.Data).First();

                // if first byte are 1 this datafile are encrypted but has do defined password to open
                if (buffer[0] == 1) throw new LiteException(0, "This data file is encrypted and needs a password to open");

                // if database is set to invalid state, try auto rebuild
                if (buffer[HeaderPage.P_DATA_INVALID_STATE] != 0)
                {
                    // dispose disk access to rebuild process
                    _disk.Dispose();

                    new RebuildService(_settings).Rebuild(
                        new RebuildOptions { 
                            Password = _settings.Password, 
                            Collation = _settings.Collation 
                        });

                    // re-initialize disk service
                    _disk = new DiskService(_settings, MEMORY_SEGMENT_SIZES);
                };

                _header = new HeaderPage(buffer);

                // test for same collation
                if (_settings.Collation != null && _settings.Collation.ToString() != _header.Pragmas.Collation.ToString())
                {
                    throw new LiteException(0, $"Datafile collation '{_header.Pragmas.Collation}' is different from engine settings. Use Rebuild database to change collation.");
                }

                // initialize locker service
                _locker = new LockService(_header.Pragmas);

                // initialize wal-index service
                _walIndex = new WalIndexService(_disk, _locker);

                // if exists log file, restore wal index references (can update full _header instance)
                if (_disk.GetLength(FileOrigin.Log) > 0)
                {
                    _walIndex.RestoreIndex(ref _header);
                }

                // initialize sort temp disk
                _sortDisk = new SortDisk(_settings.CreateTempFactory(), CONTAINER_SORT_SIZE, _header.Pragmas);

                // initialize transaction monitor as last service
                _monitor = new TransactionMonitor(_header, _locker, _disk, _walIndex);

                // register system collections
                this.InitializeSystemCollections();

                LOG("initialization completed", "ENGINE");

                _isOpen = true;

                return true;
            }
            catch (Exception ex)
            {
                LOG(ex.Message, "ERROR");

                this.Close(ex);
                throw;
            }
        }

        /// <summary>
        /// Normal close process:
        /// - Stop any new transaction
        /// - Stop operation loops over database (throw in SafePoint)
        /// - Wait for writer queue
        /// - Close disks
        /// - Clean variables
        /// </summary>
        public bool Close()
        {
            if (_isOpen == false)

            _isOpen = false;

            // stop running all transactions
            _monitor?.Dispose();

            // do a soft checkpoint (only if exclusive lock is possible)
            if (_header?.Pragmas.Checkpoint > 0) _walIndex?.TryCheckpoint();

            // close all disk streams (and delete log if empty)
            _disk?.Dispose();

            // delete sort temp file
            _sortDisk?.Dispose();

            // dispose lockers
            _locker?.Dispose();

            this.CleanServiceFields();

            return true;
        }

        /// <summary>
        /// Exception close database:
        /// - Stop diskQueue
        /// - Stop any disk read/write (dispose)
        /// - Dispos
        /// - Checks Exception type for DataCorruped to auto rebuild on open
        /// - Clean variables
        /// </summary>
        internal void Close(Exception ex)
        {
            _isOpen = false;

            _disk?.Dispose();

            this.CleanServiceFields();
        }

        internal void CleanServiceFields()
        {
            // clear all field member (to work if open method)
            _disk = null;
            _header = null;
            _locker = null;
            _walIndex = null;
            _sortDisk = null;
            _monitor = null;
            _systemCollections = null;
            _sequences = null;
        }

        #endregion

#if DEBUG
        // exposes for unit tests
        internal TransactionMonitor GetMonitor() => _monitor;
#endif

        /// <summary>
        /// Run checkpoint command to copy log file into data file
        /// </summary>
        public int Checkpoint() => _walIndex.Checkpoint();





        public void Dispose()
        {
            this.Close();
            GC.SuppressFinalize(this);
        }

        ~LiteEngine()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
            }
        }

    }
}