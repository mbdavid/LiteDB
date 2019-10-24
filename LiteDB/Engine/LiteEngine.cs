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

        private readonly LockService _locker;

        private readonly DiskService _disk;

        private readonly WalIndexService _walIndex;

        private readonly HeaderPage _header;

        private readonly TransactionMonitor _monitor;

        private readonly SortDisk _sortDisk;

        // immutable settings
        private readonly EngineSettings _settings;

        private bool _disposed = false;

        #endregion

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

            // clear checkpoint if database is readonly
            if (_settings.ReadOnly) _settings.Checkpoint = 0;

            LOG($"start initializing{(_settings.ReadOnly ? " (readonly)" : "")}", "ENGINE");

            try
            {
                // initialize locker service (no dependency)
                _locker = new LockService(settings.Timeout, settings.ReadOnly);

                // initialize disk service (will create database if needed)
                _disk = new DiskService(settings);

                // read page with no cache ref (has a own PageBuffer) - do not Release() support
                var buffer = _disk.ReadFull(FileOrigin.Data).First();

                // if first byte are 1 this datafile are encrypted but has do defined password to open
                if (buffer[0] == 1) throw new LiteException(0, "This data file is encrypted and needs a password to open");

                _header = new HeaderPage(buffer);

                // initialize wal-index service
                _walIndex = new WalIndexService(_disk, _locker);

                // if exists log file, restore wal index references (can update full _header instance)
                if (_disk.GetLength(FileOrigin.Log) > 0)
                {
                    _walIndex.RestoreIndex(ref _header);
                }

                // initialize sort temp disk
                _sortDisk = new SortDisk(settings.CreateTempFactory(), CONTAINER_SORT_SIZE, settings.UtcDate);

                // initialize transaction monitor as last service
                _monitor = new TransactionMonitor(_header, _locker, _disk, _walIndex, _settings);

                // register system collections
                this.InitializeSystemCollections();

                LOG("initialization completed", "ENGINE");
            }
            catch (Exception ex)
            {
                LOG(ex.Message, "ERROR");

                // explicit dispose (but do not run shutdown operation)
                this.Dispose(true);
                throw;
            }
        }

        #endregion

#if DEBUG
        // exposes for unit tests
        internal TransactionMonitor GetMonitor() => _monitor;
#endif

        /// <summary>
        /// Run checkpoint command to copy log file into data file
        /// </summary>
        public void Checkpoint() => _walIndex.Checkpoint(false);

        /// <summary>
        /// Shutdown process:
        /// - Stop any new transaction
        /// - Stop operation loops over database (throw in SafePoint)
        /// - Wait for writer queue
        /// - Close disks
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // this method can be called from Ctor, so many 
            // of this members can be null yet (even if are readonly). 
            if (_disposed) return;

            if (disposing)
            {
                // stop running all transactions
                _monitor?.Dispose();

                // do a soft checkpoint (only if exclusive lock is possible)
                if (_settings.Checkpoint > 0) _walIndex?.Checkpoint(true);

                // close all disk streams (and delete log if empty)
                _disk?.Dispose();

                // delete sort temp file
                _sortDisk?.Dispose();

                // dispose lockers
                _locker?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // dispose data file
            this.Dispose(true);

            LOG("engine disposed", "ENGINE");
        }
    }
}