using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        // immutable settings
        private readonly EngineSettings _settings;

        private bool _disposed = false;

        /// <summary>
        /// Get database initialize settings (used for Debug/UnitTest only)
        /// </summary>
        internal EngineSettings Settings => _settings;

        /// <summary>
        /// Get internal temporaty disk to be used in sort operation - will create a file "-tmp" when needed (used more than 1 container (800kb) of data to sort)
        /// </summary>
        internal TempDisk TempDisk { get; }

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
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _settings = settings;

            LOG("start initializing", "ENGINE");

            try
            {
                // initialize locker service (no dependency)
                _locker = new LockService(settings.Timeout, settings.ReadOnly);

                // initialize disk service (will create database if needed)
                _disk = new DiskService(settings);

                // read page with no cache ref (has a own PageBuffer) - do not Release() support
                var buffer = _disk.ReadFull(FileOrigin.Data).First();

                _header = new HeaderPage(buffer);

                // initialize wal-index service
                _walIndex = new WalIndexService(_disk, _locker);

                // if exists log file, restore wal index references (can update full _header instance)
                if (_disk.GetLength(FileOrigin.Log) > 0)
                {
                    _walIndex.RestoreIndex(ref _header);
                }

                // initialize temp disk
                this.TempDisk = new TempDisk(settings.CreateTempFactory(), CONTAINER_SORT_SIZE, settings.UtcDate);

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

        /// <summary>
        /// Run checkpoint command to copy log file into data file
        /// </summary>
        public void Checkpoint() => _walIndex.Checkpoint();

        /// <summary>
        /// Shutdown process:
        /// - Try do checkpoint (if defined as true)
        /// - Dispose disks (no more can even read data from disk/cache)
        /// - Dispose locker
        /// (in DEBUG mode you can get some ENSURE "Release" problems, but it's ok)
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            // this method can be called from Ctor, so many 
            // of this members can be null yet (even if are readonly). 
            if (_disposed) return;

            if (disposing)
            {
                // stop running all transactions
                _transactions.ForEach((x, i) => i.Value.Abort());

                // close all disk connections
                _disk?.Dispose();

                // delete sort temp file
                this.TempDisk?.Dispose();

                // dispose lockers
                _locker?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // dispose data file
            this.Dispose(true);

            GC.SuppressFinalize(this);

            LOG("engine disposed", "ENGINE");
        }

        ~LiteEngine()
        {
            this.Dispose(false);
        }
    }
}